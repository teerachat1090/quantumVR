using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
// ════════════════════════════════════════════════════════════════
//  CXCubeOnShelf.cs
//
//  Cube ที่อยู่บน Shelf
//  ─ มี XRGrabInteractable
//  ─ ตอน Grab → Spawn CXGatePrefab (ที่มี Control+Target+Connector)
//    แล้วส่ง XR interaction ไปยัง prefab นั้นทันที (XRI Transfer)
//  ─ ตัวเองยัง active อยู่บน shelf (ไม่ destroy) เพื่อให้ grab ได้อีก
// ════════════════════════════════════════════════════════════════

[RequireComponent(typeof(XRGrabInteractable))]
public class CXCubeOnShelf : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────
    [Header("Prefab")]
    [Tooltip("Prefab ที่มี CXSpawnedGate + CXControlVisual + CXTargetVisual + CXConnector")]
    [SerializeField] private GameObject cxGatePrefab;

    [Header("Layout (ใน Prefab)")]
    [Tooltip("ระยะ offset ของ Target ลงจาก Control ตอนถือในมือ (world units)")]
    [SerializeField] private float targetOffsetY = -0.15f;

    // ─────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────
    private XRGrabInteractable grabInteractable;

    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────
    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    // ─────────────────────────────────────────
    //  OnGrabbed
    //  ตอน user Grab cube ออกจากชั้น
    // ─────────────────────────────────────────
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (cxGatePrefab == null)
        {
            Debug.LogError("[CXCubeOnShelf] cxGatePrefab ไม่ได้ assign!");
            return;
        }

        // Spawn prefab ที่ตำแหน่งมือ
        Transform handTransform = args.interactorObject.transform;
        GameObject spawned = Instantiate(cxGatePrefab, handTransform.position, Quaternion.identity);

        // Setup CXSpawnedGate
        CXSpawnedGate spawnedGate     = spawned.GetComponent<CXSpawnedGate>();
        CXControlVisual controlVisual = spawned.GetComponentInChildren<CXControlVisual>();
        CXTargetVisual  targetVisual  = spawned.GetComponentInChildren<CXTargetVisual>();
        CXConnector     connector     = spawned.GetComponentInChildren<CXConnector>();

        if (spawnedGate == null || controlVisual == null || targetVisual == null || connector == null)
        {
            Debug.LogError("[CXCubeOnShelf] CXGatePrefab ขาด component!");
            Destroy(spawned);
            return;
        }

        // กำหนด offset ของ Target ให้อยู่ด้านล่าง Control
        targetVisual.transform.localPosition = new Vector3(0f, targetOffsetY, 0f);

        // Initialize gate
        spawnedGate.Initialize(controlVisual, targetVisual, connector);

        // Transfer grab ไปยัง CXControlVisual
        // (user จะถือ Control และ Target ลอยตาม offset)
        XRGrabInteractable controlGrab = controlVisual.GetComponent<XRGrabInteractable>();
        if (controlGrab != null)
        {
            var interactionManager = args.manager;
            var interactor         = args.interactorObject;

            // Release cube ออกจากมือก่อน
            interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);

            // แล้ว grab Control แทน
            interactionManager.SelectEnter(
                (IXRSelectInteractor)interactor,
                (IXRSelectInteractable)controlGrab
            );
        }

        Debug.Log("[CXCubeOnShelf] Spawned CXGatePrefab and transferred grab to Control");
    }

    // ─────────────────────────────────────────
    //  Called by CircuitSocket_Chap3.OnGatePlaced
    //  (กรณีที่ cube ถูก snap เข้า socket โดยตรง — ไม่ใช้ path นี้แล้ว)
    // ─────────────────────────────────────────
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        // ไม่ทำอะไร — CXCubeOnShelf ไม่ได้ snap ลง socket โดยตรง
        // การ snap จัดการโดย CXControlVisual แทน
        Debug.LogWarning("[CXCubeOnShelf] OnPlacedOnSocket called — unexpected path");
    }
}