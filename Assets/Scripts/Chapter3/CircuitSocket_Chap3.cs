using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class CircuitSocket_Chap3 : MonoBehaviour
{
    [Header("Socket Info")]
    public int socketIndex;
    public string socketName;

    [Tooltip("Row_0 = 0, Row_1 = 1, Row_2 = 2 ...")]
    public int rowIndex;

    [Header("XR Socket")]
    public XRSocketInteractor socketInteractor;

    [Header("Current Gate")]
    public QuantumGate_Chap3 currentGate; // ✅ Chap3

    // CX gate state
    public CXSpawnedGate occupyingCXGate { get; private set; }
    public bool isControlSocket { get; private set; }
    public bool isTargetSocket  { get; private set; }

    // alias ที่ CXSpawnedGate เรียกใช้
    public CXSpawnedGate OccupyingCXGate => occupyingCXGate;
    public bool IsOccupiedByCX => occupyingCXGate != null;

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

        // Case 1: CX Cube จาก shelf
        CXCubeOnShelf cxCube = placed.GetComponent<CXCubeOnShelf>();
        if (cxCube != null)
        {
            cxCube.OnPlacedOnSocket(this);
            return;
        }

        // Case 2: CX Target visual
        CXTargetVisual targetVisual = placed.GetComponent<CXTargetVisual>();
        if (targetVisual != null)
        {
            // Reject ถ้า row เดียวกับ Control
            if (targetVisual.ParentGate != null &&
                targetVisual.ParentGate.ControlSocket != null &&
                targetVisual.ParentGate.ControlSocket.rowIndex == rowIndex)
            {
                Debug.LogWarning($"[CircuitSocket_Chap3] ❌ Target row = Control row! Rejecting.");
                StartCoroutine(RejectNextFrame());
                return;
            }

            // Reject ถ้า socket นี้มีอะไรอยู่แล้ว
            if (IsOccupied())
            {
                Debug.LogWarning($"[CircuitSocket_Chap3] ❌ Socket {socketName} already occupied!");
                StartCoroutine(RejectNextFrame());
                return;
            }

            SetOccupiedByCX(targetVisual.ParentGate, isControl: false, isTarget: true);
            targetVisual.OnPlacedOnSocket(this);
            return;
        }

        // Case 3: Single-qubit gate (QuantumGate_Chap3) ✅
        QuantumGate_Chap3 gate = placed.GetComponent<QuantumGate_Chap3>();
        if (gate != null)
        {
            currentGate = gate;
            gate.SetCurrentSocket(this);
            Debug.Log($"✅ Gate '{gate.getGateName()}' placed in {socketName}");
            circuitTable?.UpdateCircuit();
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

        // Single-qubit gate ถูกดึงออก
        if (currentGate != null)
        {
            Debug.Log($"❌ Gate '{currentGate.getGateName()}' removed from {socketName}");
            currentGate.SetCurrentSocket(null);
            currentGate = null;
            circuitTable?.UpdateCircuit();
        }
    }

    // ── CX State Management ────────────────────────────────────────────────
    public void SetOccupiedByCX(CXSpawnedGate gate, bool isControl, bool isTarget)
    {
        occupyingCXGate = gate;
        isControlSocket = isControl;
        isTargetSocket  = isTarget;

        if (socketInteractor != null)
            socketInteractor.socketActive = false;

        Debug.Log($"[CircuitSocket_Chap3] {socketName} occupied by CX (control={isControl}, target={isTarget})");
    }

    public void ClearCXState()
    {
        occupyingCXGate = null;
        isControlSocket = false;
        isTargetSocket  = false;

        if (socketInteractor != null)
            socketInteractor.socketActive = true;

        Debug.Log($"[CircuitSocket_Chap3] {socketName} CX state cleared");
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    public bool HasGate()    => currentGate != null;
    public bool IsOccupied() => currentGate != null || occupyingCXGate != null;

    private IEnumerator RejectNextFrame()
    {
        yield return null;
        if (socketInteractor != null)
            socketInteractor.interactionManager
                .CancelInteractorSelection((IXRSelectInteractor)socketInteractor);
    }
}