using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// ════════════════════════════════════════════════════════════════
//  CircuitTableChap3.cs
//
//  ศูนย์กลางของระบบ Circuit ทั้งหมด
//  เปลี่ยนจาก CircuitTable (v1) ดังนี้:
//  [+] เก็บ Socket เป็น 2D grid ผ่าน List<CircuitRow>
//  [+] UpdateCircuit() สร้าง CircuitSnapshot แทน List<GateDataChap3>
//  [+] รู้จัก CXGateData — เก็บ CX gate แยกจาก single-qubit
//  [+] GenerateCircuitJSON() อ่าน row/col ตรงๆ ไม่ต้องคำนวณย้อน
//  [+] AddRow() — เพิ่ม Qubit runtime จากปุ่ม
//  [~] DetectChange() ปรับให้ทำงานกับ CircuitSnapshot
//  [~] ExecuteCircuit() วน column แทนวน flat list
//  ลบออก: BlochSphere (ย้ายออกไปจัดการใน script แยก)
//  ลบออก: History/Preview system (ย้ายออกไปเช่นกัน)
// ════════════════════════════════════════════════════════════════

public class CircuitTableChap3 : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────
    [Header("Row Setup")]
    [Tooltip("Prefab ของ 1 Row — ต้องมี CircuitRow.cs และ Socket ลูก")]
    [SerializeField] private GameObject rowPrefab;

    [Tooltip("Parent transform ที่ Row จะถูก Instantiate ใต้")]
    [SerializeField] private Transform rowContainer;

    [Tooltip("ระยะห่างระหว่าง Row ตามแกน -Y")]
    [SerializeField] private float rowSpacing = 0.15f;

    [Tooltip("จำนวน Row ตอนเริ่มต้น (ถ้า Row วางใน Scene ครบแล้วไม่ต้องตั้ง)")]
    [SerializeField] private int initialRowCount = 2;

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI displayText;

    [Header("Execution")]
    [SerializeField] private float gateAnimationDelay = 1.0f;

    // ─────────────────────────────────────────
    //  Runtime Data
    // ─────────────────────────────────────────
    private List<CircuitRow> rows = new List<CircuitRow>();

    /// <summary>
    /// Snapshot ล่าสุด — อัปเดตทุกครั้งที่ UpdateCircuit() ถูกเรียก
    /// Script อื่นอ่านได้ผ่าน property นี้
    /// </summary>
    public CircuitSnapshot CurrentSnapshot { get; private set; } = new CircuitSnapshot();

    private CircuitSnapshot previousSnapshot = new CircuitSnapshot();

    private bool isExecuting    = false;
    private bool pendingRebuild = false;

    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────
    void Awake()
    {
        // 1. Register Rows ที่วางใน Scene ล่วงหน้า
        Transform parent = rowContainer != null ? rowContainer : transform;
        foreach (CircuitRow existing in parent.GetComponentsInChildren<CircuitRow>())
        {
            existing.Initialize(rows.Count);
            rows.Add(existing);
        }

        // 2. Spawn Row เพิ่มจาก Prefab ถ้ายังไม่ครบ initialRowCount
        while (rows.Count < initialRowCount)
            AddRow();

        Debug.Log($"[CircuitTableChap3] Ready — {rows.Count} rows, {ColumnCount} cols");

        UpdateCircuit();
    }

    // ─────────────────────────────────────────
    //  UpdateCircuit  ★ เปลี่ยนใหม่ทั้งหมด
    // ─────────────────────────────────────────
    /// <summary>
    /// สแกน 2D grid ทั้งหมด สร้าง CircuitSnapshot ใหม่
    /// เรียกโดย CircuitSocket_Chap3 ทุกครั้งที่มีการวาง/ถอดเกต
    /// </summary>
    public void UpdateCircuit()
    {
        if (isExecuting)
        {
            pendingRebuild = true;
            return;
        }

        // เก็บ snapshot เก่าไว้เปรียบเทียบ
        previousSnapshot = CurrentSnapshot;

        // สร้าง snapshot ใหม่
        var snapshot = new CircuitSnapshot
        {
            numQubits  = rows.Count,
            numColumns = ColumnCount
        };

        var seenCX = new HashSet<CXSpawnedGate>(); // ป้องกันนับ CX ซ้ำ (control+target = gate เดียว)

        for (int row = 0; row < rows.Count; row++)
        {
            for (int col = 0; col < rows[row].ColumnCount; col++)
            {
                CircuitSocket_Chap3 socket = rows[row].GetSocket(col);
                if (socket == null) continue;

                // Single-qubit gate
                if (socket.HasGate())
                {
                    snapshot.singleGates.Add(new GateDataChap3(socket));
                    continue;
                }

                // CX gate — นับเฉพาะฝั่ง Control ไม่นับซ้ำ
                if (socket.isControlSocket && socket.occupyingCXGate != null)
                {
                    CXSpawnedGate cx = socket.occupyingCXGate;
                    if (!seenCX.Contains(cx))
                    {
                        seenCX.Add(cx);
                        var cxData = new CXGateData(cx);
                        if (cxData.IsValid())
                            snapshot.cxGates.Add(cxData);
                    }
                }
            }
        }

        // คำนวณ numColumns จริง (column ที่ใช้จริงสุดท้าย + 1)
        int maxCol = snapshot.GetMaxUsedColumn();
        snapshot.numColumns = maxCol >= 0 ? maxCol + 1 : 0;

        CurrentSnapshot = snapshot;

        // ตรวจการเปลี่ยนแปลง
        ChangeType change = DetectChange(previousSnapshot, CurrentSnapshot);
        Debug.Log($"[CircuitTableChap3] Updated — {CurrentSnapshot} | Change: {change}");

        // อัปเดต display
        UpdateDisplayText(BuildSummaryText(CurrentSnapshot));

        // เรียก handler ตามประเภทการเปลี่ยน
        HandleChange(change);
    }

    // ─────────────────────────────────────────
    //  Change Detection
    // ─────────────────────────────────────────
    private enum ChangeType
    {
        NoChange,
        EmptyCircuit,
        AddedGate,
        RemovedGate,
        ComplexChange
    }

    private ChangeType DetectChange(CircuitSnapshot oldSnap, CircuitSnapshot newSnap)
    {
        int oldTotal = oldSnap.singleGates.Count + oldSnap.cxGates.Count;
        int newTotal = newSnap.singleGates.Count + newSnap.cxGates.Count;

        if (newTotal == 0)                       return ChangeType.EmptyCircuit;
        if (oldTotal == newTotal && SnapshotsEqual(oldSnap, newSnap))
                                                 return ChangeType.NoChange;
        if (newTotal == oldTotal + 1)            return ChangeType.AddedGate;
        if (newTotal == oldTotal - 1)            return ChangeType.RemovedGate;

        return ChangeType.ComplexChange;
    }

    private bool SnapshotsEqual(CircuitSnapshot a, CircuitSnapshot b)
    {
        if (a.singleGates.Count != b.singleGates.Count) return false;
        if (a.cxGates.Count     != b.cxGates.Count)     return false;

        for (int i = 0; i < a.singleGates.Count; i++)
            if (!a.singleGates[i].EqualsTo(b.singleGates[i])) return false;

        for (int i = 0; i < a.cxGates.Count; i++)
            if (!a.cxGates[i].EqualsTo(b.cxGates[i])) return false;

        return true;
    }

    private void HandleChange(ChangeType change)
    {
        switch (change)
        {
            case ChangeType.NoChange:
                break;

            case ChangeType.EmptyCircuit:
                Debug.Log("[CircuitTableChap3] Circuit cleared");
                // TODO: reset BlochSpheres, reset simulator
                break;

            case ChangeType.AddedGate:
                Debug.Log("[CircuitTableChap3] Gate added");
                // TODO: BlochSphere.ApplyLatestGate()
                break;

            case ChangeType.RemovedGate:
                Debug.Log("[CircuitTableChap3] Gate removed");
                // TODO: BlochSphere.UndoLastGate()
                break;

            case ChangeType.ComplexChange:
                Debug.Log("[CircuitTableChap3] Complex change — rebuild");
                // TODO: BlochSphere.RebuildFromSnapshot()
                break;
        }
    }

    // ─────────────────────────────────────────
    //  AddRow  ★ ใหม่
    // ─────────────────────────────────────────
    /// <summary>
    /// เพิ่ม Qubit ใหม่ — เรียกจากปุ่ม "+ Add Qubit"
    /// Instantiate RowPrefab และวาง position อัตโนมัติ
    /// </summary>
    public void AddRow()
    {
        if (rowPrefab == null)
        {
            Debug.LogError("[CircuitTableChap3] rowPrefab ไม่ได้ assign!");
            return;
        }

        Transform parent  = rowContainer != null ? rowContainer : transform;
        GameObject newGO  = Instantiate(rowPrefab, parent);
        CircuitRow newRow = newGO.GetComponent<CircuitRow>();

        if (newRow == null)
        {
            Debug.LogError("[CircuitTableChap3] rowPrefab ไม่มี CircuitRow component!");
            Destroy(newGO);
            return;
        }

        int index = rows.Count;
        newRow.Initialize(index);

        // วาง position ตาม rowSpacing (ลงมาตาม -Y)
        newGO.transform.localPosition = new Vector3(0f, -index * rowSpacing, 0f);

        rows.Add(newRow);

        Debug.Log($"[CircuitTableChap3] Added Row {index} — total {rows.Count} rows");

        UpdateCircuit();
    }

    // ─────────────────────────────────────────
    //  ExecuteCircuit  ★ เปลี่ยนเป็น column-based
    // ─────────────────────────────────────────
    /// <summary>
    /// Execute circuit ทีละ column (= time step)
    /// เรียกจากปุ่ม Run
    /// </summary>
    public void ExecuteCircuit()
    {
        if (isExecuting)
        {
            Debug.LogWarning("[CircuitTableChap3] Already executing");
            return;
        }

        if (CurrentSnapshot.IsEmpty())
        {
            Debug.LogWarning("[CircuitTableChap3] No gates to execute");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ExecuteCoroutine());
    }

    private IEnumerator ExecuteCoroutine()
    {
        isExecuting = true;

        // snapshot ณ ตอนเริ่ม run (ป้องกันการเปลี่ยนระหว่าง execute)
        CircuitSnapshot snap    = CurrentSnapshot;
        int             maxCol  = snap.GetMaxUsedColumn();

        UpdateDisplayText($"Running {maxCol + 1} steps...");
        Debug.Log($"[CircuitTableChap3] Execute start — {maxCol + 1} columns");

        // reset simulator / BlochSpheres ก่อน
        // TODO: foreach blochSphere → ResetToZero()
        yield return new WaitForSeconds(0.5f);

        // วน column ซ้ายไปขวา
        for (int col = 0; col <= maxCol; col++)
        {
            List<GateDataChap3>   singles = snap.GetSingleAtColumn(col);
            List<CXGateData> cxList  = snap.GetCXAtColumn(col);

            if (singles.Count == 0 && cxList.Count == 0)
                continue; // column ว่าง ข้ามไป

            string label = $"Step {col + 1}/{maxCol + 1}";
            UpdateDisplayText(label);
            Debug.Log($"[Execute] Col {col}: {singles.Count} single, {cxList.Count} CX");

            // Apply single-qubit gates
            foreach (GateDataChap3 g in singles)
            {
                Debug.Log($"  → {g}");
                // TODO: blochSpheres[g.rowIndex]?.ApplyGate(g.gateName);
            }

            // Apply CX gates
            foreach (CXGateData cx in cxList)
            {
                Debug.Log($"  → {cx}");
                // TODO: quantumSim?.ApplyCX(cx.controlRow, cx.targetRow);
            }

            yield return new WaitForSeconds(gateAnimationDelay);
        }

        UpdateDisplayText("Done — ready for measurement");
        Debug.Log("[CircuitTableChap3] Execute complete");

        isExecuting = false;

        if (pendingRebuild)
        {
            pendingRebuild = false;
            UpdateCircuit();
        }
    }

    // ─────────────────────────────────────────
    //  GenerateCircuitJSON  ★ เปลี่ยนใหม่
    // ─────────────────────────────────────────
    /// <summary>
    /// สร้าง JSON สำหรับส่งไป Python / Qiskit
    /// row/col อ่านตรงจาก GateData ไม่ต้องคำนวณย้อน
    /// </summary>
    public string GenerateCircuitJSON()
    {
        UpdateCircuit();

        var export = new CircuitExportChap3
        {
            num_qubits  = CurrentSnapshot.numQubits,
            num_columns = CurrentSnapshot.numColumns
        };

        // Single-qubit gates
        foreach (GateDataChap3 g in CurrentSnapshot.singleGates)
        {
            export.gates.Add(new GateExportChap3
            {
                gate_type    = g.gateName,
                qubit        = g.rowIndex,
                column       = g.columnIndex,
                target_qubit = -1
            });
        }

        // CX gates
        foreach (CXGateData cx in CurrentSnapshot.cxGates)
        {
            export.gates.Add(new GateExportChap3
            {
                gate_type    = "CX",
                qubit        = cx.controlRow,
                column       = cx.controlCol,
                target_qubit = cx.targetRow
            });
        }

        string json = JsonUtility.ToJson(export, true);
        Debug.Log($"[CircuitTableChap3] JSON:\n{json}");
        return json;
    }

    // ─────────────────────────────────────────
    //  Grid Accessors
    // ─────────────────────────────────────────
    /// <summary>ดึง Socket จาก (row, col) — คืน null ถ้าไม่มี</summary>
    public CircuitSocket_Chap3 GetSocket(int rowIdx, int colIdx)
    {
        if (rowIdx < 0 || rowIdx >= rows.Count) return null;
        return rows[rowIdx].GetSocket(colIdx);
    }

    public CircuitRow GetRow(int rowIdx)
    {
        if (rowIdx < 0 || rowIdx >= rows.Count) return null;
        return rows[rowIdx];
    }

    public int RowCount    => rows.Count;
    public int ColumnCount => rows.Count > 0 ? rows[0].ColumnCount : 0;

    // ─────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────
    private string BuildSummaryText(CircuitSnapshot s)
    {
        if (s.IsEmpty()) return "Circuit empty";
        int total = s.singleGates.Count + s.cxGates.Count;
        return $"{s.numQubits} qubits  •  {total} gates";
    }

    private void UpdateDisplayText(string msg)
    {
        if (displayText != null) displayText.text = msg;
    }

    // เรียกจาก Inspector / Debug button
    public void PrintCircuit()
    {
        Debug.Log("══════════════════════════════════");
        Debug.Log($"  {CurrentSnapshot}");
        foreach (var g  in CurrentSnapshot.singleGates) Debug.Log($"  {g}");
        foreach (var cx in CurrentSnapshot.cxGates)     Debug.Log($"  {cx}");
        Debug.Log("══════════════════════════════════");
    }
}