using UnityEngine;
using System.Collections.Generic;

// ════════════════════════════════════════════════════════════════
//  CircuitRow.cs
//  จัดการ Socket ทั้งหมดใน 1 Row (= 1 Qubit wire)
//  ถูก Instantiate จาก RowPrefab โดย CircuitTableChap3
// ════════════════════════════════════════════════════════════════
public class CircuitRow : MonoBehaviour
{
    [Header("Row Info")]
    [Tooltip("กำหนดโดย CircuitTableChap3.AddRow() — ห้ามแก้มือ")]
    public int rowIndex;

    [Header("Sockets")]
    [Tooltip("เรียงตาม columnIndex — ปล่อยว่างให้ auto-find จาก children")]
    public List<CircuitSocket_Chap3> sockets = new List<CircuitSocket_Chap3>();

    // ─────────────────────────────────────────
    //  Initialize — เรียกจาก CircuitTableChap3
    // ─────────────────────────────────────────
    /// <summary>
    /// กำหนด rowIndex และ columnIndex ให้ socket ทุกตัว
    /// เรียกทันทีหลัง Instantiate
    /// </summary>
    public void Initialize(int index)
    {
        rowIndex        = index;
        gameObject.name = $"Row_{index}";

        // auto-find ถ้า sockets ยังว่าง
        if (sockets == null || sockets.Count == 0)
            sockets = new List<CircuitSocket_Chap3>(
                GetComponentsInChildren<CircuitSocket_Chap3>()
            );

        // sort ตาม socketIndex ที่ตั้งใน Inspector
        sockets.Sort((a, b) => a.socketIndex.CompareTo(b.socketIndex));

        // assign rowIndex และ columnIndex ให้ทุก socket
        for (int col = 0; col < sockets.Count; col++)
        {
            sockets[col].rowIndex    = index;
            sockets[col].columnIndex = col;
            sockets[col].socketName  = $"R{index}_C{col}";
        }

        Debug.Log($"[CircuitRow] Row {index} initialized — {sockets.Count} sockets");
    }

    // ─────────────────────────────────────────
    //  Accessors
    // ─────────────────────────────────────────
    public CircuitSocket_Chap3 GetSocket(int col)
    {
        if (col < 0 || col >= sockets.Count) return null;
        return sockets[col];
    }

    public int ColumnCount => sockets.Count;

    // ─────────────────────────────────────────
    //  Data extraction
    // ─────────────────────────────────────────
    /// <summary>คืน GateData ของทุก socket ที่มีเกตใน row นี้</summary>
    public List<GateDataChap3> GetAllGateData()
    {
        var result = new List<GateDataChap3>();
        foreach (var socket in sockets)
        {
            GateDataChap3 data = socket.ToGateData();
            if (data != null) result.Add(data);
        }
        return result;
    }
}