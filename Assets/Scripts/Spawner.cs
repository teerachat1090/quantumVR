using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class Spawner : MonoBehaviour
{
    // INPUT:
    //------------------------------------------------------------------------

    [Header("Spawn Settings")]
    [Tooltip("Use itself as spawn prefab")]
    [SerializeField] private bool usePrefab = false;

    [Tooltip("Prefab for spawning")]
    [SerializeField] private GameObject spawnPrefab;
    
    [Tooltip("Prefab size multiplier")]
    [SerializeField] private float scaleMultiplier = 0.5f;
    
    //------------------------------------------------------------------------
    [Header("Other Settings")]
    [Tooltip("Spawn distance (meters)")]
    [SerializeField] private float spawnDistance = 0.3f;
    
    [Tooltip("Despawn on specific height")]
    [SerializeField] private bool destroyOnFloorHit = true;
    
    [Tooltip("Despawn height (meters)")]
    [SerializeField] private float despawnHeight = 0.1f;
    
    [Tooltip("Spawn Cooldown (seconds)")]
    [SerializeField] private float spawnCooldown = 0.5f;
    //--------------------------------------------------------------------------
    //__________________________________________________________________________
    
    private XRGrabInteractable grabInteractable;
    private XRInteractionManager grabManager;
    private bool isSpawner = true; //true for ori, change to false when spawned
    
    // Check XRGrabInteractable component, and add event when grabbed for original object
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null) //happen when component missing, not when disabled
        {
            Debug.LogError("Component \"XRGrabInteractable\" is missing!");
            return;
        }

        if(grabInteractable.interactionManager == null)
        {
            Debug.LogError("XR Interaction Manager is not assigned in XRGrabInteractable!");
            return;
        }

        grabManager = grabInteractable.interactionManager;
        if(grabManager is null) Debug.LogWarning("Can't found interaction manager.");
        
        if (spawnCooldown < 0.01f) spawnCooldown = 0.01f; //cooldown minimun at 10ms

        // if spawn point -> add event when grabbed
        if (isSpawner) grabInteractable.selectEntered.AddListener(OnSelectEnter);

        if(usePrefab && spawnPrefab == null)   Debug.LogWarning("Spawn Prefab is not assigned.");
    }

    // when grabbed: spawner will spawn prefab, and grab it instead + disable spawner temporarily
    private void OnSelectEnter(SelectEnterEventArgs args)
    { 
        if(!isSpawner) return;
        
        grabManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);

        GameObject spawnedObject = SpawnPrefab(args);
        if (spawnedObject == null) return;

        XRGrabInteractable spawnedGrab = spawnedObject.GetComponent<XRGrabInteractable>();
        StartCoroutine(GrabSpawnedObject(args.interactorObject, spawnedGrab));
        
        StartCoroutine(DisableOriginalTemporarily());
    }
    
    // Disable original grab temporarily to avoid immediate re-grab
    IEnumerator DisableOriginalTemporarily()
    {
        grabInteractable.enabled = false; // disable grab ability
        yield return new WaitForSeconds(spawnCooldown);
        grabInteractable.enabled = true;  // re-enable after delay
    }

    // Spawn prefab and grab it
    private GameObject SpawnPrefab(SelectEnterEventArgs args)
    {
        GameObject prefabToSpawn = usePrefab ?  spawnPrefab : gameObject; 
        if (prefabToSpawn == null) {
            Debug.LogWarning("No prefab assigned for spawning.");
            return null;
        }

        // Get Hand model position
        Transform handTransform = args.interactorObject.transform;
        Vector3 spawnPosition = handTransform.position + handTransform.forward * spawnDistance;
        
        // Spawn Prefab
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);
        setObjectInit(spawnedObject);
        
        return spawnedObject;
    }
    
    //setting prefab components after spawn
    //has components: XRGrabInteractable, Rigidbody, Spawner
    private void setObjectInit(GameObject spawnedObject)
    {
        // set scale
        spawnedObject.transform.localScale = transform.localScale * scaleMultiplier;

        // Set XRGrabInteractable
        XRGrabInteractable spawnedGrab = spawnedObject.GetComponent<XRGrabInteractable>();
        if (spawnedGrab == null)    spawnedGrab = spawnedObject.AddComponent<XRGrabInteractable>();
        spawnedGrab.enabled = true;
        spawnedGrab.throwOnDetach = true; //can throw when release
        spawnedGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        
        // Set Rigidbody -> interact with physics + object
        Rigidbody spawnedRb = spawnedObject.GetComponent<Rigidbody>();
        if (spawnedRb == null)      spawnedRb = spawnedObject.AddComponent<Rigidbody>();
        spawnedRb.isKinematic = false;
        spawnedRb.useGravity = true;

        if(usePrefab) {
            var quantumgate = spawnedObject.GetComponent<QuantumGate>();
            string name = spawnedObject.name;
            int sepIndex = name.IndexOf("_");
            quantumgate.name = name.Remove(sepIndex);
            return;
        }

        // Edit spawn script (if has one): Prefab must not spawn another
        Spawner spawnedScript = spawnedObject.GetComponent<Spawner>();
        if(spawnedScript == null) return;
        
        spawnedScript.isSpawner = false;
        if (destroyOnFloorHit)
        {
            spawnedScript.enabled = true;
            spawnedScript.StartCheckingFloor();
        }
        else    Destroy(spawnedScript);

    }

    // Grab spawned object
    private IEnumerator GrabSpawnedObject(IXRSelectInteractor interactor, XRGrabInteractable grabbable)
    {
        // wait for object to be ready
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // check interactor and grabbable
        if(interactor == null || grabbable == null || grabManager == null)
            yield return null;

        // Grab the spawned one
        grabManager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)grabbable);
    }
    
    private void StartCheckingFloor()
    {
        if (!isSpawner)    
            InvokeRepeating(nameof(CheckFloorCollision), 0.5f, 0.5f);   
            //check every 0.5 second
    }
    
    private void CheckFloorCollision()
    {
        if (transform.position.y <= despawnHeight)    Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        // Destroy event listener for spawner (prevent memory leak)
        if (grabInteractable != null && isSpawner)
            grabInteractable.selectEntered.RemoveListener(OnSelectEnter);
    }
}