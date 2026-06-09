using UnityEngine;

// ════════════════════════════════════════════════════════════════
//  CXSpawnedGate.cs
//
//  ตัวกลางของ CX Gate ทั้งหมด
//  ─ ถือ reference ของ Control/Target socket
//  ─ บอก state ให้ CXConnector อัปเดตเส้น
//  ─ CircuitTableChap3 อ่าน ControlSocket / TargetSocket เพื่อสร้าง CXGateData
//
//  State machine:
//    InHand       → ถือในมือ ยังไม่ได้วาง
//    ControlPlaced → Control snap แล้ว รอ Target
//    PlacedFull   → ทั้งคู่ snap แล้ว
//    Repositioning → กำลังลาก Control หรือ Target ออก
// ════════════════════════════════════════════════════════════════

public class CXSpawnedGate : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────
    public enum GateState { InHand, ControlPlaced, PlacedFull, Repositioning }

    public GateState State { get; private set; } = GateState.InHand;

    // ─────────────────────────────────────────
    //  Children (set ตอน Spawn โดย CXCubeOnShelf)
    // ─────────────────────────────────────────
    public CXControlVisual ControlVisual { get; private set; }
    public CXTargetVisual  TargetVisual  { get; private set; }
    public CXConnector     Connector     { get; private set; }

    // ─────────────────────────────────────────
    //  Socket refs (set โดย Visual ตอน snap)
    // ─────────────────────────────────────────
    public CircuitSocket_Chap3 ControlSocket { get; private set; }
    public CircuitSocket_Chap3 TargetSocket  { get; private set; }

    // ─────────────────────────────────────────
    //  Init — เรียกจาก CXCubeOnShelf ทันทีหลัง Instantiate
    // ─────────────────────────────────────────
    public void Initialize(CXControlVisual ctrl, CXTargetVisual tgt, CXConnector conn)
    {
        ControlVisual = ctrl;
        TargetVisual  = tgt;
        Connector     = conn;

        ControlVisual.Initialize(this);
        TargetVisual.Initialize(this);
        Connector.Initialize(this);

        SetState(GateState.InHand);
        Debug.Log("[CXSpawnedGate] Initialized — InHand");
    }

    // ─────────────────────────────────────────
    //  Called by CXControlVisual
    // ─────────────────────────────────────────
    public void OnControlSnapped(CircuitSocket_Chap3 socket)
    {
        ControlSocket = socket;

        // ถ้า Target ยังไม่ได้ snap → ControlPlaced
        if (TargetSocket == null)
            SetState(GateState.ControlPlaced);
        else
            SetState(GateState.PlacedFull);

        socket.SetOccupiedByCX(this, isControl: true, isTarget: false);
        NotifyCircuitTable();
        Debug.Log($"[CXSpawnedGate] Control snapped → {socket.socketName}");
    }

    public void OnControlReleased()
    {
        if (ControlSocket != null)
        {
            ControlSocket.ClearCXState();
            ControlSocket = null;
        }

        SetState(GateState.Repositioning);
        NotifyCircuitTable();
        Debug.Log("[CXSpawnedGate] Control released");
    }

    // ─────────────────────────────────────────
    //  Called by CXTargetVisual
    // ─────────────────────────────────────────
    public void OnTargetSnapped(CircuitSocket_Chap3 socket)
    {
        TargetSocket = socket;

        if (ControlSocket != null)
            SetState(GateState.PlacedFull);
        else
            SetState(GateState.Repositioning);

        socket.SetOccupiedByCX(this, isControl: false, isTarget: true);
        NotifyCircuitTable();
        Debug.Log($"[CXSpawnedGate] Target snapped → {socket.socketName}");
    }

    public void OnTargetReleased()
    {
        if (TargetSocket != null)
        {
            TargetSocket.ClearCXState();
            TargetSocket = null;
        }

        SetState(GateState.Repositioning);
        NotifyCircuitTable();
        Debug.Log("[CXSpawnedGate] Target released");
    }

    // ─────────────────────────────────────────
    //  State Machine
    // ─────────────────────────────────────────
    private void SetState(GateState newState)
    {
        State = newState;
        Connector?.OnStateChanged(newState);
        Debug.Log($"[CXSpawnedGate] State → {newState}");
    }

    // ─────────────────────────────────────────
    //  Destroy — เรียกถ้าต้องการลบ CX ออกจาก circuit
    // ─────────────────────────────────────────
    public void DestroyGate()
    {
        if (ControlSocket != null) ControlSocket.ClearCXState();
        if (TargetSocket  != null) TargetSocket.ClearCXState();
        NotifyCircuitTable();
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────
    private void NotifyCircuitTable()
    {
        var table = GetComponentInParent<CircuitTableChap3>();
        table?.UpdateCircuit();
    }

    public bool IsFullyPlaced => State == GateState.PlacedFull;

    public bool IsSameColumn(CircuitSocket_Chap3 a, CircuitSocket_Chap3 b)
    {
        if (a == null || b == null) return false;
        return a.columnIndex == b.columnIndex;
    }
}