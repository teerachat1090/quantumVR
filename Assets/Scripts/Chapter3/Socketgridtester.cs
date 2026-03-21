using UnityEngine;

// ════════════════════════════════════════════════════════════════
//  SocketGridTester.cs
//  ใช้ทดสอบ Socket Grid ก่อนทำ CX Gate
//  ลาก script นี้ใส่ CircuitTable_Chap3 แล้วกด Test ใน Inspector
//  ลบออกได้หลังจาก confirm ว่า grid ทำงานถูก
// ════════════════════════════════════════════════════════════════

public class SocketGridTester : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private CircuitTableChap3 circuitTable;

    // กด button ใน Inspector เพื่อ test
    [Header("Test — กด button ด้านล่างใน Inspector")]
    [SerializeField] private bool runTestOnStart = true;

    void Start()
    {
        if (circuitTable == null)
            circuitTable = GetComponent<CircuitTableChap3>();

        if (runTestOnStart)
            TestGrid();
    }

    // ─────────────────────────────────────────
    //  Test ทุก socket ใน grid
    // ─────────────────────────────────────────
    [ContextMenu("Test Grid")]
    public void TestGrid()
    {
        if (circuitTable == null)
        {
            Debug.LogError("[GridTester] circuitTable ไม่ได้ assign!");
            return;
        }

        int rows = circuitTable.RowCount;
        int cols = circuitTable.ColumnCount;

        Debug.Log($"[GridTester] Grid size: {rows} rows × {cols} cols");
        Debug.Log("─────────────────────────────────────");

        int found   = 0;
        int missing = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var socket = circuitTable.GetSocket(row, col);

                if (socket != null)
                {
                    // ตรวจว่า rowIndex / columnIndex ถูกต้องไหม
                    bool rowOK = socket.rowIndex    == row;
                    bool colOK = socket.columnIndex == col;

                    if (rowOK && colOK)
                    {
                        Debug.Log($"  ✅ ({row},{col}) → {socket.socketName}");
                        found++;
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠ ({row},{col}) → {socket.socketName} " +
                                         $"แต่ socket บอก row={socket.rowIndex} col={socket.columnIndex} — ไม่ตรง!");
                        missing++;
                    }
                }
                else
                {
                    Debug.LogWarning($"  ❌ ({row},{col}) → null — ไม่มี socket!");
                    missing++;
                }
            }
        }

        Debug.Log("─────────────────────────────────────");
        Debug.Log($"[GridTester] Result: {found} OK, {missing} missing/wrong");

        // ทดสอบ edge cases
        TestEdgeCases(rows, cols);
    }

    // ─────────────────────────────────────────
    //  ทดสอบ out-of-range — ต้องคืน null ทุกกรณี
    // ─────────────────────────────────────────
    private void TestEdgeCases(int rows, int cols)
    {
        Debug.Log("[GridTester] Edge cases:");

        var s1 = circuitTable.GetSocket(-1, 0);
        Debug.Log($"  GetSocket(-1, 0) = {(s1 == null ? "null ✅" : "NOT null ❌")}");

        var s2 = circuitTable.GetSocket(0, -1);
        Debug.Log($"  GetSocket(0, -1) = {(s2 == null ? "null ✅" : "NOT null ❌")}");

        var s3 = circuitTable.GetSocket(rows, 0);
        Debug.Log($"  GetSocket({rows}, 0) = {(s3 == null ? "null ✅" : "NOT null ❌")}");

        var s4 = circuitTable.GetSocket(0, cols);
        Debug.Log($"  GetSocket(0, {cols}) = {(s4 == null ? "null ✅" : "NOT null ❌")}");

        // ทดสอบ row+1 สำหรับ CX gate
        Debug.Log("[GridTester] CX readiness (row+1 same col):");
        for (int col = 0; col < cols; col++)
        {
            var ctrl = circuitTable.GetSocket(0, col);
            var tgt  = circuitTable.GetSocket(1, col);

            bool ready = ctrl != null && tgt != null;
            Debug.Log($"  Col {col}: ctrl={ctrl?.socketName ?? "null"} " +
                      $"tgt={tgt?.socketName ?? "null"} " +
                      $"{(ready ? "✅ CX ready" : "❌")}");
        }
    }
}