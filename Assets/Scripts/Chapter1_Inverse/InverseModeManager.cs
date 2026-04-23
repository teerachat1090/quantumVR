using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// ตัวกลางเชื่อม Bloch Sphere input → GateSolver → GateSpawner
/// แขวนไว้บน InverseManager GameObject ใน Chapter1_Inverse
/// </summary>
public class InverseModeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GateSpawner gateSpawner;
    [SerializeField] private BlochSphere blochSphere;       // Bloch sphere ใน scene
    [SerializeField] private CircuitManager circuitManager; // ของเพื่อน

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;           // แสดง gate sequence ที่ได้

    [Header("Solver Setting")]
    [SerializeField] private int maxDepth = 3;              // จำนวน gate สูงสุด
    [SerializeField] private float matchThreshold = 0.02f;  // ความแม่นยำ

    private Vector3 targetVec = Vector3.up;                 // เริ่มที่ |0⟩
    private bool isReady = false;

    // ---------- Unity ----------

    IEnumerator Start()
    {
        // รอให้ socket spawn ก่อน
        yield return new WaitForSeconds(1f);
        isReady = true;
        UpdateStatusText("หมุน Bloch Sphere แล้วกด Generate");
        Debug.Log("[InverseModeManager] Ready!");
    }

    // ---------- Public API ----------

    /// <summary>
    /// เรียกจากปุ่ม Generate
    /// </summary>
    public void OnGenerate()
    {
        if (!isReady)
        {
            Debug.LogWarning("[InverseModeManager] Not ready yet!");
            return;
        }

        // อ่าน target vector จาก Bloch Sphere
        if (blochSphere != null)
            targetVec = blochSphere.GetCurrentStateUnit();
        
        Debug.Log($"[InverseModeManager] Target vector: {targetVec}");

        // คำนวณ gate sequence
        List<string> gateSeq = GateSolver.Solve(targetVec, maxDepth, matchThreshold);

        if (gateSeq == null || gateSeq.Count == 0)
        {
            UpdateStatusText("ไม่พบ gate sequence");
            Debug.LogWarning("[InverseModeManager] No solution found!");
            return;
        }

        float errorDeg = GateSolver.GetAngleError(gateSeq, targetVec);
        Debug.Log($"[InverseModeManager] Solution: {string.Join(" → ", gateSeq)} (error: {errorDeg:F1}°)");

        // Spawn gate บนโต๊ะ
        gateSpawner.SpawnGates(gateSeq);

        // อัปเดต circuit ให้ Bloch Sphere แสดงผล
        if (circuitManager != null)
            circuitManager.updateOverallCircuit(BuildCircuitJson(gateSeq));

        UpdateStatusText($"Gate: {string.Join(" → ", gateSeq)}\nError: {errorDeg:F1}°");
    }

    /// <summary>
    /// เรียกจาก BlochSphereInput เมื่อ sphere ถูกหมุน
    /// </summary>
    public void SetTargetVector(Vector3 vec)
    {
        targetVec = vec.normalized;
    }

    // ---------- Helper ----------

    private void UpdateStatusText(string msg)
    {
        if (statusText != null)
            statusText.SetText(msg);
    }

    /// <summary>
    /// สร้าง circuit JSON จาก gate sequence เพื่อส่งให้ CircuitManager
    /// </summary>
    private string BuildCircuitJson(List<string> gateSeq)
    {
        // format ตาม JSON structure ที่ CircuitManager ใช้
        var gateList = new System.Text.StringBuilder();
        for (int i = 0; i < gateSeq.Count; i++)
        {
            gateList.Append($@"{{
                ""name"": ""{gateSeq[i]}"",
                ""column"": {i},
                ""controlRow"": [0],
                ""targetRow"": null,
                ""classical"": false
            }}");
            if (i < gateSeq.Count - 1) gateList.Append(",");
        }

        string json = $@"{{
            ""qubitAmount"": 1,
            ""socketAmount"": {gateSeq.Count},
            ""CBitAmount"": 0,
            ""blochSphere"": true,
            ""columnList"": [{string.Join(",", System.Linq.Enumerable.Range(0, gateSeq.Count))}],
            ""gateList"": [{gateList}]
        }}";

        return json;
    }
}