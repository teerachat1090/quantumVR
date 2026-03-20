using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class CircuitSocket : MonoBehaviour
{
    [Header("Socket Info")]
    public int socketIndex;  // SC1=1, SC2=2, ... SC11=11
    public string socketName;

    // ── เพิ่มใหม่: บอกว่า socket นี้อยู่บน qubit row ไหน ──────────────────
    [Tooltip("Row_0 = 0, Row_1 = 1, Row_2 = 2 ...")]
    public int rowIndex;     // ← set ใน Inspector ตาม Row ที่ socket นี้อยู่

    [Header("XR Socket")]
    public XRSocketInteractor socketInteractor;

    [Header("Current Gate")]
    public QuantumGate currentGate;          // single-qubit gate ที่วางอยู่

    // ── เพิ่มใหม่: CX gate state ──────────────────────────────────────────
    public CXSpawnedGate occupyingCXGate { get; private set; }  // CX gate ที่ใช้ socket นี้
    public bool isControlSocket { get; private set; }           // เป็น Control ของ CX?
    public bool isTargetSocket  { get; private set; }           // เป็น Target ของ CX?

    private CircuitTable circuitTable;

    // ── Unity lifecycle ────────────────────────────────────────────────────
    void Start()
    {
        circuitTable = GetComponentInParent<CircuitTable>();

        if (string.IsNullOrEmpty(socketName))
            socketName = $"SC{socketIndex} (Row{rowIndex})";

        if (socketInteractor == null)
            socketInteractor = GetComponent<XRSocketInteractor>();

        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnGatePlaced);
            socketInteractor.selectExited.AddListener(OnGateRemoved);
        }
    }

    void OnDestroy()
    {
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnGatePlaced);
            socketInteractor.selectExited.RemoveListener(OnGateRemoved);
        }
    }

    // ── Gate Placed ────────────────────────────────────────────────────────
    private void OnGatePlaced(SelectEnterEventArgs args)
    {
        GameObject placed = args.interactableObject.transform.gameObject;

        // ── Case 1: CX Cube จาก shelf ─────────────────────────────────────
        CXCubeOnShelf cxCube = placed.GetComponent<CXCubeOnShelf>();
        if (cxCube != null)
        {
            cxCube.OnPlacedOnSocket(this);
            // (Cube จะ Destroy ตัวเองและ Spawn prefab — ไม่ต้อง UpdateCircuit)
            return;
        }

        // ── Case 2: CX Target visual ───────────────────────────────────────
        CXTargetVisual targetVisual = placed.GetComponent<CXTargetVisual>();
        if (targetVisual != null)
        {
            // Reject ถ้าเป็น row เดียวกับ Control
            if (targetVisual.ParentGate != null &&
                targetVisual.ParentGate.ControlSocket != null &&
                targetVisual.ParentGate.ControlSocket.rowIndex == rowIndex)
            {
                Debug.LogWarning($"[CircuitSocket] ❌ Target row = Control row! Rejecting.");
                StartCoroutine(RejectNextFrame());
                return;
            }

            // Reject ถ้า socket นี้มีอะไรอยู่แล้ว
            if (IsOccupied())
            {
                Debug.LogWarning($"[CircuitSocket] ❌ Socket {socketName} already occupied!");
                StartCoroutine(RejectNextFrame());
                return;
            }

            SetOccupiedByCX(targetVisual.ParentGate, isControl: false, isTarget: true);
            targetVisual.OnPlacedOnSocket(this);
            return;
        }

        // ── Case 3: Single-qubit gate (logic เดิม) ─────────────────────────
        QuantumGate gate = placed.GetComponent<QuantumGate>();
        if (gate != null)
        {
            currentGate = gate;
            gate.SetCurrentSocket(this);

            Debug.Log($"✅ Gate '{gate.getGateName()}' placed in {socketName}");

            if (circuitTable != null)
                circuitTable.UpdateCircuit();
        }
    }
    

    // ── Gate Removed ───────────────────────────────────────────────────────
    private void OnGateRemoved(SelectExitEventArgs args)
    {
        GameObject removed = args.interactableObject.transform.gameObject;

        // CX Target ถูกดึงออก
        CXTargetVisual tv = removed.GetComponent<CXTargetVisual>();
        if (tv != null)
        {
            tv.OnRemovedFromSocket();
            ClearCXState();
            return;
        }

        // Single-qubit gate ถูกดึงออก (logic เดิม)
        if (currentGate != null)
        {
            Debug.Log($"❌ Gate '{currentGate.getGateName()}' removed from {socketName}");
            currentGate.SetCurrentSocket(null);
            currentGate = null;

            if (circuitTable != null)
                circuitTable.UpdateCircuit();
        }
    }

    // ── CX State Management ────────────────────────────────────────────────
    /// <summary>เรียกจาก CXSpawnedGate.Init() เมื่อ socket นี้กลายเป็น Control</summary>
    public void SetOccupiedByCX(CXSpawnedGate gate, bool isControl, bool isTarget)
    {
        occupyingCXGate  = gate;
        isControlSocket  = isControl;
        isTargetSocket   = isTarget;
    }

    /// <summary>ล้าง CX state เมื่อ gate ถูกเอาออก</summary>
    public void ClearCXState()
    {
        occupyingCXGate = null;
        isControlSocket = false;
        isTargetSocket  = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    public bool HasGate()    => currentGate != null;

    public bool IsOccupied() => currentGate != null
                             || occupyingCXGate != null;

    // Force-eject: คืน interactable ออกจาก socket ใน frame ถัดไป
private IEnumerator RejectNextFrame()
    {
        yield return null;
        if (socketInteractor != null)
            socketInteractor.interactionManager
                .CancelInteractorSelection((IXRSelectInteractor)socketInteractor);
    }
}