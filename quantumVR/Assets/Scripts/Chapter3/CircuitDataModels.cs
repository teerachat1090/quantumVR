using System.Collections.Generic;
using UnityEngine;

// ════════════════════════════════════════════════════════════════
//  CircuitDataModels_Chap3.cs
//  Data classes สำหรับ Chapter 3 — แยกจาก CircuitTable.cs เดิม
//  ทุก class ใส่ suffix "Chap3" เพื่อไม่ชนกับ Chapter อื่น
// ════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────
//  GateDataChap3
// ────────────────────────────────────────────────────────────────
[System.Serializable]
public class GateDataChap3
{
    public int    rowIndex;
    public int    columnIndex;
    public string gateName;
    public string gateDescription;

    public int    socketIndex;
    public string socketName;

    public GateDataChap3() { }

    public GateDataChap3(CircuitSocket_Chap3 socket)
    {
        rowIndex        = socket.rowIndex;
        columnIndex     = socket.columnIndex;
        socketIndex     = socket.socketIndex;
        socketName      = socket.socketName;
        gateName        = socket.currentGate?.getGateName()        ?? "";
        gateDescription = socket.currentGate?.getGateDescription() ?? "";
    }

    public bool EqualsTo(GateDataChap3 other)
    {
        if (other == null) return false;
        return rowIndex    == other.rowIndex
            && columnIndex == other.columnIndex
            && gateName    == other.gateName;
    }

    public override string ToString() =>
        $"[{gateName}] R{rowIndex}C{columnIndex} ({socketName})";
}

// ────────────────────────────────────────────────────────────────
//  CXGateData  (ไม่ชนกับ CircuitTable.cs เดิม)
// ────────────────────────────────────────────────────────────────
[System.Serializable]
public class CXGateData
{
    public int controlRow;
    public int controlCol;
    public int targetRow;
    public int targetCol;

    [System.NonSerialized]
    public CXSpawnedGate spawnedGate;

    public CXGateData() { }

    public CXGateData(CXSpawnedGate gate)
    {
        spawnedGate = gate;
        controlRow  = gate.ControlSocket?.rowIndex    ?? -1;
        controlCol  = gate.ControlSocket?.columnIndex ?? -1;
        targetRow   = gate.TargetSocket?.rowIndex     ?? -1;
        targetCol   = gate.TargetSocket?.columnIndex  ?? -1;
    }

    public bool IsValid()
    {
        if (controlRow < 0 || controlCol < 0) return false;
        if (targetRow  < 0 || targetCol  < 0) return false;
        if (controlRow == targetRow)           return false;
        if (controlCol != targetCol)           return false;
        return true;
    }

    public bool EqualsTo(CXGateData other)
    {
        if (other == null) return false;
        return controlRow == other.controlRow && controlCol == other.controlCol
            && targetRow  == other.targetRow  && targetCol  == other.targetCol;
    }

    public override string ToString() =>
        $"[CX] ctrl=R{controlRow}C{controlCol}  tgt=R{targetRow}C{targetCol}";
}

// ────────────────────────────────────────────────────────────────
//  CircuitSnapshot  (ไม่ชนกับ CircuitTable.cs เดิม)
// ────────────────────────────────────────────────────────────────
[System.Serializable]
public class CircuitSnapshot
{
    public int numQubits;
    public int numColumns;

    public List<GateDataChap3> singleGates = new List<GateDataChap3>();
    public List<CXGateData>    cxGates     = new List<CXGateData>();

    public List<GateDataChap3> GetSingleAtColumn(int col)
    {
        var result = new List<GateDataChap3>();
        foreach (var g in singleGates)
            if (g.columnIndex == col) result.Add(g);
        return result;
    }

    public List<CXGateData> GetCXAtColumn(int col)
    {
        var result = new List<CXGateData>();
        foreach (var cx in cxGates)
            if (cx.controlCol == col) result.Add(cx);
        return result;
    }

    public int GetMaxUsedColumn()
    {
        int max = -1;
        foreach (var g  in singleGates) if (g.columnIndex  > max) max = g.columnIndex;
        foreach (var cx in cxGates)     if (cx.controlCol  > max) max = cx.controlCol;
        return max;
    }

    public bool IsEmpty() => singleGates.Count == 0 && cxGates.Count == 0;

    public override string ToString() =>
        $"Snapshot {numQubits}q/{numColumns}col — " +
        $"{singleGates.Count} single, {cxGates.Count} CX";
}

// ────────────────────────────────────────────────────────────────
//  CircuitExportChap3 + GateExportChap3
// ────────────────────────────────────────────────────────────────
[System.Serializable]
public class CircuitExportChap3
{
    public int                    num_qubits;
    public int                    num_columns;
    public List<GateExportChap3>  gates = new List<GateExportChap3>();
}

[System.Serializable]
public class GateExportChap3
{
    public string gate_type;
    public int    qubit;
    public int    target_qubit;
    public int    column;
}