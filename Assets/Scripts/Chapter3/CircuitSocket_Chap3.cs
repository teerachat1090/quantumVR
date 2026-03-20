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

    [Header("Attach Point")]
    [Tooltip("ลาก child 'Attach' ใส่ตรงนี้ — ใช้เป็น reference transform สำหรับ CX gate spawn")]
    public Transform attachTransform;

    [Header("Current Gate")]
    public QuantumGate_Chap3 currentGate;

    // CX gate state
    public CXSpawnedGate occupyingCXGate { get; private set; }
    public bool isControlSocket { get; private set; }
    public bool isTargetSocket  { get; private set; }

    public CXSpawnedGate OccupyingCXGate => occupyingCXGate;
    public bool IsOccupiedByCX => occupyingCXGate != null;

    private CircuitTable circuitTable;

    void Start()
    {
        circuitTable = GetComponentInParent<CircuitTable>();

        if (string.IsNullOrEmpty(socketName))
            socketName = $"SC{socketIndex} (Row{rowIndex})";

        if (socketInteractor == null)
            socketInteractor = GetComponent<XRSocketInteractor>();

        // Auto-find Attach child ถ้าไม่ได้ assign ใน Inspector
        if (attachTransform == null)
            attachTransform = transform.Find("Attach") ?? transform;

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

    private void OnGatePlaced(SelectEnterEventArgs args)
    {
        GameObject placed = args.interactableObject.transform.gameObject;

        CXCubeOnShelf cxCube = placed.GetComponent<CXCubeOnShelf>();
        if (cxCube != null)
        {
            cxCube.OnPlacedOnSocket(this);
            return;
        }

        // Case: CX Control visual วางลง socket ใหม่หลังถูก grab
        CXControlVisual controlVisual = placed.GetComponent<CXControlVisual>();
        if (controlVisual != null)
        {
            if (IsOccupied())
            {
                Debug.LogWarning($"[CircuitSocket_Chap3] Socket {socketName} already occupied!");
                StartCoroutine(RejectNextFrame());
                return;
            }
            controlVisual.OnPlacedOnSocket(this);
            return;
        }

        CXTargetVisual targetVisual = placed.GetComponent<CXTargetVisual>();
        if (targetVisual != null)
        {
            if (targetVisual.ParentGate != null &&
                targetVisual.ParentGate.ControlSocket != null &&
                targetVisual.ParentGate.ControlSocket.rowIndex == rowIndex)
            {
                Debug.LogWarning($"[CircuitSocket_Chap3] Target row = Control row! Rejecting.");
                StartCoroutine(RejectNextFrame());
                return;
            }

            if (IsOccupied())
            {
                Debug.LogWarning($"[CircuitSocket_Chap3] Socket {socketName} already occupied!");
                StartCoroutine(RejectNextFrame());
                return;
            }

            // ✅ ปิด socket ก่อน เพื่อให้ XR หยุด snap/scale object ทันที
            if (socketInteractor != null)
            {
                socketInteractor.socketActive = false;
                socketInteractor.showInteractableHoverMeshes = false;
            }
            SetOccupiedByCX(targetVisual.ParentGate, isControl: false, isTarget: true);
            targetVisual.OnPlacedOnSocket(this);
            return;
        }

        QuantumGate_Chap3 gate = placed.GetComponent<QuantumGate_Chap3>();
        if (gate != null)
        {
            currentGate = gate;
            gate.SetCurrentSocket(this);
            Debug.Log($"Gate '{gate.getGateName()}' placed in {socketName}");
            circuitTable?.UpdateCircuit();
        }
    }

    private void OnGateRemoved(SelectExitEventArgs args)
    {
        GameObject removed = args.interactableObject.transform.gameObject;

        CXTargetVisual tv = removed.GetComponent<CXTargetVisual>();
        if (tv != null)
        {
            // ถ้า Target กำลัง snap เข้า socket นี้อยู่ (isSnappingToSocket=true)
            // XR จะ fire selectExited จาก CancelInteractableSelection ใน coroutine
            // ต้อง ignore ไม่งั้น ClearCXState จะ reset PlacedSocket → ReturnToFloat
            if (tv.IsSnappingToSocket)
            {
                Debug.Log($"[CircuitSocket_Chap3] Ignoring selectExited — Target is snapping to {socketName}");
                return;
            }
            tv.OnRemovedFromSocket();
            ClearCXState();
            return;
        }

        if (currentGate != null)
        {
            Debug.Log($"Gate '{currentGate.getGateName()}' removed from {socketName}");
            currentGate.SetCurrentSocket(null);
            currentGate = null;
            circuitTable?.UpdateCircuit();
        }
    }

    public void SetOccupiedByCX(CXSpawnedGate gate, bool isControl, bool isTarget)
    {
        occupyingCXGate = gate;
        isControlSocket = isControl;
        isTargetSocket  = isTarget;

        if (socketInteractor != null)
        {
            socketInteractor.socketActive = false;
            socketInteractor.showInteractableHoverMeshes = false;
        }

        Debug.Log($"[CircuitSocket_Chap3] {socketName} occupied by CX (control={isControl}, target={isTarget})");
    }

    public void ClearCXState()
    {
        occupyingCXGate = null;
        isControlSocket = false;
        isTargetSocket  = false;

        if (socketInteractor != null)
        {
            socketInteractor.socketActive = true;
            socketInteractor.showInteractableHoverMeshes = true;
        }

        Debug.Log($"[CircuitSocket_Chap3] {socketName} CX state cleared");
    }

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