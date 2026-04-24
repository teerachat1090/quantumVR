using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CircuitManager : MonoBehaviour
{
    [SerializeField] private SocketsManager socketsManager = null;
    // use component.getComponentsInChildren<thatComponent>(false, thatList);
    //      to store as List instead of array (no reallocate !) - false = not count inactive child

    public enum SphereType
    {
        BlochSphere, QSphere
    }
    [Header("Sphere setting")]
    [SerializeField] private SphereType sphereType = SphereType.BlochSphere;
    [SerializeField] private GameObject sphere = null; //check later: bloch / Q - sphere

    [Header("Qubit Setting")]
    [SerializeField] private int qubitAmount = 3;
    [SerializeField] private int socketAmount = 11;

    [Header("Display setting")]
    [SerializeField] private bool useClassical = false;
    [SerializeField] private QuantumUiStatManager uiManager = null; //showing result
    [SerializeField] private TMP_Text modeText = null; //display mode

    [Header("Interaction setting")]
    [SerializeField] private GameObject storage = null; //disable grabbable when in animated mode
    [SerializeField] private InteractionLayerMask interactionLayer;

    private XRGrabInteractable[] sourceGates;
    
    // can check each one if enable
    private bool isBlochSphere;
    private BlochSphere blochSphere = null;
    private QSphere qSphere = null;
    //private QSphere qSphere = null;
    private SequenceManager sqManager = null;
    private CircuitExecutor executor = new CircuitExecutor();
    private FileManager fileManager = new FileManager();

    public bool isItBlochSphere()
    {
        return (sphereType == SphereType.BlochSphere) ? true : false;;
    }

    private void CheckComponent()
    {
        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;
        if(isBlochSphere)   
        {
            blochSphere = sphere.GetComponent<BlochSphere>();
            if(blochSphere is null) Debug.LogWarning("Warning: Sphere model is missing!");
        } else
        {
            qSphere = sphere.GetComponent<QSphere>();
            if(qSphere is null)     Debug.LogWarning("Warning: Sphere model is missing!");          
        }

        if(uiManager is null)   Debug.LogWarning("Initialize Warning: UI script is missing!");
        else                    
        {
            uiManager.isBlochSphere = isBlochSphere;
            uiManager.useClassical = useClassical;
            Debug.Log("UI stat checking sucessful.");
        }
        
        sqManager = GetComponent<SequenceManager>();
        if(sqManager is null) Debug.LogWarning("Warning: Sequence manager component is missing!");

        if(modeText is null) Debug.LogWarning("Warning: Text for showing mode is missing!");

        if(socketsManager is null)  Debug.LogWarning("Warning: Socket manager is missing!");
        else socketsManager.useClassical = useClassical;
        

        if(storage == null) Debug.LogWarning("Warning: storage object is missing! Unable to set gate grabbable state!");
        else
        {
            sourceGates = storage.GetComponentsInChildren<XRGrabInteractable>();
            if(sourceGates is null) Debug.LogWarning("Warning: Unable to get component from source gate in storage!");
        }
    }

    void Awake()
    {
        CheckComponent();
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(isBlochSphere is false && qSphere is not null)
        {
            qSphere.ChangeQubitAmount(qubitAmount);
        }
        socketsManager.InitSocketPrefabSpawn(qubitAmount, socketAmount);
        //socketsManager.updateCircuitByJson(null, -1, -1, true);
    }

    private void updateBlochVectorInstant()
    {
        if(blochSphere is null) {
            Debug.LogWarning("Warning: Sphere model is missing. Unable to animated!");
            return;
        }
        
        List<string> gateList = socketsManager.GetGateAsStringList(0);
        Vector3 resultVector = executor.GetResultBlochVector(gateList);
        blochSphere.AnimateToStateDirectly(resultVector);
    }

    private async Task calculateAndUpdateUi(string inputPath, string outputPath)
    {
        string pythonScriptPath = fileManager.GetPythonQuantumScript();
        await Task.Run(() => executor.PrepareThenRunQiskit(pythonScriptPath, inputPath, outputPath));

        // show value
        if(uiManager is not null)   uiManager.DisplayResult(isBlochSphere);
        else                        Debug.LogWarning("Warning: ui script is missing. Unable to show stat!");

        if(!isBlochSphere) qSphere.UpdateFromJson();
    }

    // recalculate everytime the circuit change
    public void updateOverallCircuit(string circuitJson)
    {
        if(!Application.isPlaying) return;
        fileManager.updateJsonInputToFile(circuitJson, isBlochSphere);

        if (isBlochSphere)  updateBlochVectorInstant();
        
        fileManager.GetJsonSphereIOPath(isBlochSphere, out string inputPath, out string outputPath, out _);
        _ = calculateAndUpdateUi(inputPath, outputPath);
    }

    // assigned to button: Mode Button
    public void PrepareForAnimation()
    {
        if(sqManager is null)
        {
            Debug.LogWarning("Warning: Sequence manager component is missing!");
            return;
        }

        if(modeText is null)    Debug.LogWarning("Wrning: Text object is missing");
        else                    modeText.SetText("Sequence Mode");

        socketsManager.FreezeGateBlock(true);

        string pythonScriptPath = fileManager.GetPythonQuantumScript(isSequence: true);

        fileManager.GetJsonSphereIOPath(isBlochSphere, out string inputPath, out _, out string sequenceOutputPath);
                     
        _ = sqManager.prepareSequence(pythonScriptPath, inputPath, sequenceOutputPath);
    }

    // assigned to button
    public void BackToNormal()
    {
        if(modeText is null)    Debug.LogWarning("Wrning: Text object is missing");
        else                    modeText.SetText("Instant Mode");

        socketsManager.FreezeGateBlock(false);
        
        updateBlochVectorInstant();
    }

    public SocketsManager GetSocketsManager()
    {
        return socketsManager;
    }
}