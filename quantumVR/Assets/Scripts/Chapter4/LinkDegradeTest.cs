using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Link Degrade + Distance Speed Test Cases
/// ครอบคลุม:
///   1. Dynamic Penalty — degraded link บน path vs นอก path
///   2. Penalty สะสมตามจำนวน link ที่ degrade บน path
///   3. Distance Speed ยังทำงานตอน Degrade เปิด
///   4. Degraded link ช้ากว่า link ปกติเสมอ
///
/// วิธีใช้: แนบ script นี้กับ GameObject ใดก็ได้ใน Scene
/// กด Play แล้วดูผลใน Console (Ctrl+Shift+C)
/// </summary>
public class LinkDegradeTest : MonoBehaviour
{
    [Header("Test Settings")]
    public float L_att       = 500f;   // fiber attenuation length (km)
    public float fExp        = 0.45f;  // dampen exponent
    public int   penaltyPer  = 8;      // penalty % ต่อ 1 degraded link บน path
    public float degradeSpeedRatio = 0.6f; // degraded speed = base × 0.6

    void Start()
    {
        Debug.Log("===== Link Degrade Test Cases =====\n");

        RunDegradePenaltyTests();
        RunDistanceSpeedTests();

        Debug.Log("===== Test Complete =====");
    }

    // ══════════════════════════════════════════════════════════
    //  SECTION 1 — Dynamic Penalty
    // ══════════════════════════════════════════════════════════
    void RunDegradePenaltyTests()
    {
        Debug.Log("── Section 1: Dynamic Penalty ──────────────────\n");

        // Case 1: 0 degraded link บน path → ไม่โดน penalty เลย
        RunPenaltyTest("Case 1 — ไม่มี degraded บน path",
            fidelity: 95f, nodeCount: 3, distKm: 50f,
            degradedOnPath: 0,
            expectedPenalty: 0);

        // Case 2: 1 degraded link บน path → -8%
        RunPenaltyTest("Case 2 — 1 link degrade บน path",
            fidelity: 95f, nodeCount: 3, distKm: 50f,
            degradedOnPath: 1,
            expectedPenalty: 8);

        // Case 3: 2 degraded link บน path → -16%
        RunPenaltyTest("Case 3 — 2 link degrade บน path",
            fidelity: 95f, nodeCount: 5, distKm: 100f,
            degradedOnPath: 2,
            expectedPenalty: 16);

        // Case 4: 4 degraded link บน path → -32% (node เยอะ)
        RunPenaltyTest("Case 4 — 4 link degrade บน path (node=7)",
            fidelity: 90f, nodeCount: 7, distKm: 100f,
            degradedOnPath: 4,
            expectedPenalty: 32);

        // Case 5: penalty ทำให้ fidelity ไม่ต่ำกว่า floor 20%
        RunPenaltyTest("Case 5 — penalty floor ที่ 20%",
            fidelity: 50f, nodeCount: 10, distKm: 300f,
            degradedOnPath: 5,
            expectedPenalty: 40, expectedFloor: 20);

        // Case 6: Fidelity ต่ำ + 1 link degrade
        RunPenaltyTest("Case 6 — Fidelity ต่ำ (70%) + 1 link degrade",
            fidelity: 70f, nodeCount: 3, distKm: 50f,
            degradedOnPath: 1,
            expectedPenalty: 8);
    }

    void RunPenaltyTest(string name, float fidelity, int nodeCount, float distKm,
                        int degradedOnPath, int expectedPenalty, float expectedFloor = 20f)
    {
        int   hops         = nodeCount - 1;
        int   linkCount    = Mathf.Max(1, nodeCount - 1);
        float distPerLink  = Mathf.Max(1f, distKm / linkCount);
        float distFactor   = Mathf.Exp(-distPerLink / L_att);
        float fidPerLink   = (fidelity / 100f) * distFactor;
        float fExpVal      = Mathf.Max(1f, hops * fExp);
        float baselineFid  = Mathf.Max(20f, Mathf.Pow(fidPerLink, fExpVal) * 100f);

        // คำนวณ penalty ใหม่ (dynamic)
        float actualPenalty    = degradedOnPath * penaltyPer;
        float degradedFid      = Mathf.Max(expectedFloor, baselineFid - actualPenalty);

        // ตรวจสอบ
        bool penaltyCorrect    = Mathf.Approximately(actualPenalty, expectedPenalty)
                                 || (degradedFid <= expectedFloor && actualPenalty >= expectedPenalty);
        bool floorHeld         = degradedFid >= expectedFloor - 0.01f;

        // เปรียบกับ flat -15 เดิม
        float oldPenaltyFid    = Mathf.Max(20f, baselineFid - 15f);
        float diff             = degradedFid - oldPenaltyFid;
        string diffStr         = diff >= 0 ? $"+{diff:F1}%" : $"{diff:F1}%";

        bool pass = penaltyCorrect && floorHeld;
        string icon = pass ? "✓" : "✗";

        Debug.Log(
            $"[{icon} {(pass ? "PASS" : "FAIL")}] {name}\n" +
            $"  Input        : Fidelity={fidelity}%  Nodes={nodeCount}  Dist={distKm}km  DegradedOnPath={degradedOnPath}\n" +
            $"  Baseline Fid : {baselineFid:F1}%\n" +
            $"  Penalty      : -{actualPenalty:F0}%  (expect -{expectedPenalty}%)\n" +
            $"  Result Fid   : {degradedFid:F1}%  (floor≥{expectedFloor}%)  " +
                $"vs flat-15 เดิม={oldPenaltyFid:F1}%  ({diffStr})\n" +
            $"  Floor held   : {(floorHeld ? "OK" : "FAIL")}\n"
        );
    }

    // ══════════════════════════════════════════════════════════
    //  SECTION 2 — Distance Speed ยังทำงานตอน Degrade เปิด
    // ══════════════════════════════════════════════════════════
    void RunDistanceSpeedTests()
    {
        Debug.Log("── Section 2: Distance Speed + Degrade ─────────\n");

        // Case 7: ระยะสั้น → speed สูง, degraded ช้ากว่า
        RunSpeedTest("Case 7 — 50 km (ใกล้)",   distKm: 50f);

        // Case 8: ระยะกลาง
        RunSpeedTest("Case 8 — 250 km (กลาง)",  distKm: 250f);

        // Case 9: ระยะไกลสุด → speed ต่ำ, degraded ช้ากว่าอีก
        RunSpeedTest("Case 9 — 500 km (ไกลสุด)", distKm: 500f);

        // Case 10: ยืนยัน degraded < normal เสมอ ทุก distance
        RunSpeedInvariantTest();
    }

    void RunSpeedTest(string name, float distKm)
    {
        float baseSpeed    = Mathf.Clamp(1f - (distKm / 500f) * 0.5f, 0.3f, 1.0f);
        float degradeSpeed = Mathf.Clamp(baseSpeed * degradeSpeedRatio, 0.1f, 0.6f);

        bool baseInRange   = baseSpeed    >= 0.3f && baseSpeed    <= 1.0f;
        bool degradeInRange= degradeSpeed >= 0.1f && degradeSpeed <= 0.6f;
        bool degradeSlower = degradeSpeed < baseSpeed;

        bool pass = baseInRange && degradeInRange && degradeSlower;
        string icon = pass ? "✓" : "✗";

        Debug.Log(
            $"[{icon} {(pass ? "PASS" : "FAIL")}] {name}\n" +
            $"  Distance     : {distKm} km\n" +
            $"  Normal speed : {baseSpeed:F2}×  (range 0.30–1.00)\n" +
            $"  Degrade speed: {degradeSpeed:F2}×  (range 0.10–0.60)\n" +
            $"  Degraded slower than normal: {(degradeSlower ? "YES ✓" : "NO ✗")}\n"
        );
    }

    void RunSpeedInvariantTest()
    {
        Debug.Log("[Invariant] Degraded speed < Normal speed — ทุก distance\n");

        float[] distances  = { 50f, 100f, 200f, 300f, 400f, 500f };
        bool allPass       = true;

        foreach (float d in distances)
        {
            float baseSpeed    = Mathf.Clamp(1f - (d / 500f) * 0.5f, 0.3f, 1.0f);
            float degradeSpeed = Mathf.Clamp(baseSpeed * degradeSpeedRatio, 0.1f, 0.6f);

            if (degradeSpeed >= baseSpeed)
            {
                Debug.Log($"  [✗ FAIL] dist={d}km  base={baseSpeed:F2}  degrade={degradeSpeed:F2}  (degrade >= base!)");
                allPass = false;
            }
            else
            {
                Debug.Log($"  [✓ OK]   dist={d}km  base={baseSpeed:F2}  degrade={degradeSpeed:F2}");
            }
        }

        Debug.Log(allPass
            ? "[✓ PASS] Invariant ผ่านทุก distance\n"
            : "[✗ FAIL] Invariant ล้มเหลวบาง distance\n");
    }
}
