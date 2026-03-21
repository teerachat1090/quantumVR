using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

// ════════════════════════════════════════════════════════════════
//  CircuitSocket_Chap3.cs  (Data Model v2)
//
//  การเปลี่ยนแปลงจาก v1:
//  [+] columnIndex  — บอกว่า socket นี้อยู่ time-step ที่เท่าไหร่
//  [+] ToGateData() — สร้าง GateData จาก socket ตัวเอง
//  [~] socketName   — ถูก override เป็น "R{row}_C{col}" โดย CircuitRow
//  [~] routing      — เพิ่ม column-check ตอน Target snap
//  เหมือนเดิม: XR socket flow, CX state, RejectNextFrame
// ════════════════════════════════════════════════════════════════

public class CircuitSocket_Chap3 : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────
    [Header("Socket Info")]
    public int    socketIndex;  // legacy — ยังเก็บไว้เพื่อ debug
    public string socketName;   // ถูก set โดย CircuitRow.Initialize()

    [Tooltip("กำหนดโดย CircuitRow.Initialize() — ห้ามแก้มือ")]
    public int rowIndex;

    [Tooltip("กำหนดโดย CircuitRow.Initialize() — ห้ามแก้มือ  ★ ใหม่")]
    public int columnIndex;

    [Header("XR Socket")]
    public XRSocketInteractor socketInteractor;

    [Header("Attach Point")]
    [Tooltip("ลาก child 'Attach' ใส่ตรงนี้ — ใช้เป็น snap position สำหรับ CX visual")]
    public Transform attachTransform;

    [Header("Current Gate  (Single-qubit)")]
    public QuantumGate_Chap3 currentGate;

    // ─────────────────────────────────────────
    //  CX State
    // ─────────────────────────────────────────
    public CXSpawnedGate occupyingCXGate { get; private set; }
    public bool          isControlSocket { get; private set; }
    public bool          isTargetSocket  { get; private set; }

    public bool IsOccupiedByCX => occupyingCXGate != null;

    // ─────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────
    private CircuitTableChap3 circuitTable;
    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────
    void Start()
    {
        circuitTable = GetComponentInParent<CircuitTableChap3>();

        // fallback ถ้า CircuitRow ยังไม่ได้ Initialize
        if (string.IsNullOrEmpty(socketName))
            socketName = $"R{rowIndex}_C{columnIndex}";

        if (socketInteractor == null)
            socketInteractor = GetComponent<XRSocketInteractor>();

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
        if (socketInteractor == null) return;
        socketInteractor.selectEntered.RemoveListener(OnGatePlaced);
        socketInteractor.selectExited.RemoveListener(OnGateRemoved);
    }

    // ─────────────────────────────────────────
    //  OnGatePlaced
    //  XR socket fire เมื่อมี Interactable snap เข้ามา
    // ─────────────────────────────────────────
    private void OnGatePlaced(SelectEnterEventArgs args)
    {
        GameObject placed = args.interactableObject.transform.gameObject;

        // ── 1. CXCubeOnShelf (หยิบมาจากชั้น) ────────────────
        CXCubeOnShelf cxCube = placed.GetComponent<CXCubeOnShelf>();
        if (cxCube != null)
        {
            cxCube.OnPlacedOnSocket(this);
            return;
        }

        // ── 2. CXControlVisual (ย้ายตำแหน่ง) ────────────────
        CXControlVisual controlVisual = placed.GetComponent<CXControlVisual>();
        if (controlVisual != null)
        {
            if (IsOccupied())
            {
                Debug.LogWarning($"[{socketName}] occupied — reject CXControl");
                StartCoroutine(RejectNextFrame());
                return;
            }
            controlVisual.OnPlacedOnSocket(this);
            return;
        }

        // ── 3. CXTargetVisual (ย้ายตำแหน่ง) ─────────────────
        CXTargetVisual targetVisual = placed.GetComponent<CXTargetVisual>();
        if (targetVisual != null)
        {
            CXSpawnedGate parentGate   = targetVisual.ParentGate;
            CircuitSocket_Chap3 ctrlSk = parentGate?.ControlSocket;

            // ห้าม row เดียวกับ Control
            if (ctrlSk != null && ctrlSk.rowIndex == rowIndex)
            {
                Debug.LogWarning($"[{socketName}] Target row == Control row — reject");
                StartCoroutine(RejectNextFrame());
                return;
            }

            // ห้าม column ต่างจาก Control  ★ ใหม่
            if (ctrlSk != null && ctrlSk.columnIndex != columnIndex)
            {
                Debug.LogWarning($"[{socketName}] Target col {columnIndex} " +
                                 $"!= Control col {ctrlSk.columnIndex} — reject");
                StartCoroutine(RejectNextFrame());
                return;
            }

            if (IsOccupied())
            {
                Debug.LogWarning($"[{socketName}] occupied — reject CXTarget");
                StartCoroutine(RejectNextFrame());
                return;
            }

            // ✅ ปิด socket ก่อน เพื่อหยุด XR snap/scale ทันที
            DisableSocket();
            SetOccupiedByCX(targetVisual.ParentGate, isControl: false, isTarget: true);
            targetVisual.OnPlacedOnSocket(this);
            return;
        }

        // ── 4. Single-qubit gate ─────────────────────────────
        QuantumGate_Chap3 gate = placed.GetComponent<QuantumGate_Chap3>();
        if (gate != null)
        {
            currentGate = gate;
            gate.SetCurrentSocket(this);
            Debug.Log($"[{socketName}] '{gate.getGateName()}' placed");
            circuitTable?.UpdateCircuit();
        }
    }

    // ─────────────────────────────────────────
    //  OnGateRemoved
    //  XR socket fire เมื่อ Interactable ถูก Grab ออก
    // ─────────────────────────────────────────
    private void OnGateRemoved(SelectExitEventArgs args)
    {
        GameObject removed = args.interactableObject.transform.gameObject;

        // ── CXTargetVisual ────────────────────────────────────
        CXTargetVisual tv = removed.GetComponent<CXTargetVisual>();
        if (tv != null)
        {
            // ignore ถ้ากำลัง snap เข้า socket ใหม่อยู่
            if (tv.IsSnappingToSocket)
            {
                Debug.Log($"[{socketName}] ignore selectExited — Target is snapping");
                return;
            }
            tv.OnRemovedFromSocket();
            ClearCXState();
            circuitTable?.UpdateCircuit();
            return;
        }

        // ── CXControlVisual ───────────────────────────────────
        CXControlVisual cv = removed.GetComponent<CXControlVisual>();
        if (cv != null)
        {
            ClearCXState();
            circuitTable?.UpdateCircuit();
            return;
        }

        // ── Single-qubit gate ─────────────────────────────────
        if (currentGate != null)
        {
            Debug.Log($"[{socketName}] '{currentGate.getGateName()}' removed");
            currentGate.SetCurrentSocket(null);
            currentGate = null;
            circuitTable?.UpdateCircuit();
        }
    }

    // ─────────────────────────────────────────
    //  CX State Management
    // ─────────────────────────────────────────
    public void SetOccupiedByCX(CXSpawnedGate gate, bool isControl, bool isTarget)
    {
        occupyingCXGate = gate;
        isControlSocket = isControl;
        isTargetSocket  = isTarget;
        DisableSocket();
        Debug.Log($"[{socketName}] CX occupied (ctrl={isControl} tgt={isTarget})");
    }

    public void ClearCXState()
    {
        occupyingCXGate = null;
        isControlSocket = false;
        isTargetSocket  = false;
        EnableSocket();
        Debug.Log($"[{socketName}] CX state cleared");
    }

    // ─────────────────────────────────────────
    //  Socket Active Toggle
    // ─────────────────────────────────────────
    private void DisableSocket()
    {
        if (socketInteractor == null) return;
        socketInteractor.socketActive               = false;
        socketInteractor.showInteractableHoverMeshes = false;
    }

    private void EnableSocket()
    {
        if (socketInteractor == null) return;
        socketInteractor.socketActive               = true;
        socketInteractor.showInteractableHoverMeshes = true;
    }

    // ─────────────────────────────────────────
    //  Query
    // ─────────────────────────────────────────
    public bool HasGate()    => currentGate != null;
    public bool IsOccupied() => currentGate != null || occupyingCXGate != null;

    // ─────────────────────────────────────────
    //  Data Extraction  ★ ใหม่
    // ─────────────────────────────────────────
    /// <summary>
    /// สร้าง GateData จาก socket นี้
    /// คืน null ถ้าไม่มี single-qubit gate อยู่
    /// </summary>
    public GateDataChap3 ToGateData()
    {
        if (!HasGate()) return null;
        return new GateDataChap3(this);
    }

    // ─────────────────────────────────────────
    //  Reject Helper
    // ─────────────────────────────────────────
    private IEnumerator RejectNextFrame()
    {
        yield return null;
        if (socketInteractor != null)
            socketInteractor.interactionManager
                .CancelInteractorSelection((IXRSelectInteractor)socketInteractor);
    }
}