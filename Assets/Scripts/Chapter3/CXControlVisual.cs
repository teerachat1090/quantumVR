using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ════════════════════════════════════════════════════════════════
//  CXControlVisual.cs
//
//  ● Control dot ของ CX Gate
//  ─ XRGrabInteractable — user Grab ได้ตอน Reposition
//  ─ ตอนถือในมือ: ติดตาม hand พร้อมกับ Target
//  ─ ตอน Drop บน Socket: snap เข้า socket, Target auto-snap row+1
//  ─ ตอน Reposition: Grab ออกจาก socket ได้, เส้นเปลี่ยนเป็น Dashed
// ════════════════════════════════════════════════════════════════

[RequireComponent(typeof(XRGrabInteractable))]
public class CXControlVisual : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────
    [Header("Snap Settings")]
    [Tooltip("ระยะ detect socket ที่ใกล้ที่สุดตอน Drop (world units)")]
    [SerializeField] private float snapRadius = 0.12f;

    [Header("Target Auto-Snap")]
    [Tooltip("row offset ของ Target ตอน auto-snap (ปกติ = +1)")]
    [SerializeField] private int targetRowOffset = 1;

    // ─────────────────────────────────────────
    //  Runtime
    // ─────────────────────────────────────────
    private CXSpawnedGate parentGate;
    private XRGrabInteractable grabInteractable;
    private CircuitTableChap3 circuitTable;

    private bool isInHand    = true;
    private bool isSnapping  = false;   // ป้องกัน selectExited ยิงผิด

    // ─────────────────────────────────────────
    //  Init — เรียกจาก CXSpawnedGate.Initialize()
    // ─────────────────────────────────────────
    public void Initialize(CXSpawnedGate gate)
    {
        parentGate    = gate;
        circuitTable  = FindFirstObjectByType<CircuitTableChap3>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    // ─────────────────────────────────────────
    //  Called by CircuitSocket_Chap3.OnGatePlaced
    //  XR socket ดัก grab เข้า socket → redirect มาที่นี่
    // ─────────────────────────────────────────
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        if (socket.IsOccupied())
        {
            Debug.LogWarning($"[CXControlVisual] Socket {socket.socketName} occupied");
            return;
        }

        isSnapping = true;

        // snap ตัวเองเข้า socket
        SnapToSocket(socket);
        parentGate.OnControlSnapped(socket);

        // auto-snap Target ที่ row+1 column เดียวกัน
        AutoSnapTarget(socket);

        isSnapping = false;
        isInHand   = false;

        Debug.Log($"[CXControlVisual] Placed on {socket.socketName}");
    }

    // ─────────────────────────────────────────
    //  OnReleased — เมื่อ user Grab ออกจาก Socket (Reposition)
    // ─────────────────────────────────────────
    private void OnReleased(SelectExitEventArgs args)
    {
        if (isSnapping) return;     // กำลัง snap เข้า socket ใหม่ — ignore

        parentGate.OnControlReleased();
        Debug.Log("[CXControlVisual] Grabbed out — Repositioning");
    }

    // ─────────────────────────────────────────
    //  Snap Helpers
    // ─────────────────────────────────────────
    private void SnapToSocket(CircuitSocket_Chap3 socket)
    {
        Transform attach   = socket.attachTransform ?? socket.transform;
        transform.position = attach.position;
        // ไม่ copy rotation — attachTransform มี 90 90 0 สำหรับ XR socket
        // Control visual ควรคง rotation ของตัวเองไว้
    }

    private void AutoSnapTarget(CircuitSocket_Chap3 controlSocket)
    {
        if (circuitTable == null || parentGate?.TargetVisual == null) return;

        int col        = controlSocket.columnIndex;
        bool isLastRow = controlSocket.rowIndex >= circuitTable.RowCount - 1;

        // row สุดท้าย → Target ขึ้นบน (row-1)
        // ทุก row อื่น → Target ลงล่าง (row+1)
        int offset    = isLastRow ? -1 : targetRowOffset;
        int targetRow = controlSocket.rowIndex + offset;

        CircuitSocket_Chap3 targetSocket = circuitTable.GetSocket(targetRow, col);
        if (targetSocket == null)
        {
            Debug.LogWarning($"[CXControlVisual] No socket at R{targetRow}C{col} for auto-snap");
            return;
        }

        if (targetSocket.IsOccupied())
        {
            Debug.LogWarning($"[CXControlVisual] Target socket R{targetRow}C{col} occupied");
            return;
        }

        parentGate.TargetVisual.SnapToSocketDirect(targetSocket);
        Debug.Log($"[CXControlVisual] Target auto-snapped → R{targetRow}C{col} " +
                  $"({(isLastRow ? "last row up" : "normal down")})");
    }

    // ─────────────────────────────────────────
    //  Public Query
    // ─────────────────────────────────────────
    public bool IsInHand       => isInHand;
    public CXSpawnedGate ParentGate => parentGate;
}