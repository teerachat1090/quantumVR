using UnityEngine;

/// <summary>
/// Heavy Traffic Test Cases
/// วิธีใช้: แนบ script นี้กับ GameObject ใดก็ได้ใน Scene
/// กด Play แล้วดูผลใน Console (Ctrl+Shift+C)
/// </summary>
public class HeavyTrafficTest : MonoBehaviour
{
    [Header("Test Settings")]
    public float heavyFidelityPenalty = 0.06f;
    public float L_att = 500f;   // fiber attenuation length (km)
    public float fExp  = 0.45f;  // dampen exponent multiplier

    void Start()
    {
        Debug.Log("===== Heavy Traffic Test Cases =====\n");
        RunAllTests();
    }

    void RunAllTests()
    {
        RunTest("Case 1", fidelity: 95f, nodeCount: 3, distKm: 50f,
                expectedBaselineMin: 88f, expectedBaselineMax: 94f,
                expectedHeavyMin: 78f,    expectedHeavyMax: 88f);

        RunTest("Case 2", fidelity: 95f, nodeCount: 5, distKm: 100f,
                expectedBaselineMin: 82f, expectedBaselineMax: 90f,
                expectedHeavyMin: 70f,    expectedHeavyMax: 82f);

        RunTest("Case 3", fidelity: 95f, nodeCount: 7, distKm: 50f,
                expectedBaselineMin: 76f, expectedBaselineMax: 92f,
                expectedHeavyMin: 65f,    expectedHeavyMax: 82f);

        RunTest("Case 4", fidelity: 70f, nodeCount: 5, distKm: 100f,
                expectedBaselineMin: 52f, expectedBaselineMax: 66f,
                expectedHeavyMin: 42f,    expectedHeavyMax: 56f);

        RunTest("Case 5", fidelity: 50f, nodeCount: 7, distKm: 163f,
                expectedBaselineMin: 20f, expectedBaselineMax: 40f,
                expectedHeavyMin: 20f,    expectedHeavyMax: 36f);

        RunTest("Case 6 (extreme)", fidelity: 95f, nodeCount: 10, distKm: 500f,
                expectedBaselineMin: 58f, expectedBaselineMax: 78f,
                expectedHeavyMin: 48f,    expectedHeavyMax: 68f);

        Debug.Log("===== Test Complete =====");
    }

    void RunTest(string name, float fidelity, int nodeCount, float distKm,
                 float expectedBaselineMin, float expectedBaselineMax,
                 float expectedHeavyMin,    float expectedHeavyMax)
    {
        int hops      = nodeCount - 1;
        int linkCount = Mathf.Max(1, nodeCount - 1);

        // ── Baseline ──────────────────────────────────────────
        float distPerLink    = Mathf.Max(1f, distKm / linkCount);
        float distFactor     = Mathf.Exp(-distPerLink / L_att);
        float fidPerLink     = (fidelity / 100f) * distFactor;
        float fExpVal        = Mathf.Max(1f, hops * fExp);
        float baselineFid    = Mathf.Max(20f, Mathf.Pow(fidPerLink, fExpVal) * 100f);

        // ── Heavy — geometric mean ────────────────────────────
        float linkFidAfterPenalty = fidelity * (1f - heavyFidelityPenalty);
        float avgLinkFid          = linkFidAfterPenalty / 100f;
        float heavyExp            = Mathf.Max(1f, hops * 0.35f);
        float heavyFidRaw         = Mathf.Max(25f, Mathf.Pow(avgLinkFid, heavyExp) * 100f);
        float heavyFid            = Mathf.Max(25f, Mathf.Min(heavyFidRaw, baselineFid - 1f));

        // ── QBER ─────────────────────────────────────────────
        float baselineQBER = (1f - baselineFid / 100f) / 2f * 100f;
        float heavyQBER    = (1f - heavyFid    / 100f) / 2f * 100f;

        // ── E-Rate ───────────────────────────────────────────
        float R0           = 1000f;
        float baselineRate = R0 * Mathf.Pow(baselineFid / 100f, hops) / Mathf.Pow(1.5f, hops);
        float heavyRate    = R0 * Mathf.Pow(heavyFid    / 100f, hops) / Mathf.Pow(1.5f, hops);
        float rateDropPct  = baselineRate > 0f ? (1f - heavyRate / baselineRate) * 100f : 0f;

        // ── Pass / Fail ───────────────────────────────────────
        bool baselinePass = baselineFid >= expectedBaselineMin && baselineFid <= expectedBaselineMax;
        bool heavyPass    = heavyFid    >= expectedHeavyMin    && heavyFid    <= expectedHeavyMax;
        bool overallPass  = baselinePass && heavyPass;

        string status = overallPass ? "PASS" : "FAIL";
        string icon   = overallPass ? "✓" : "✗";

        Debug.Log(
            $"[{icon} {status}] {name}\n" +
            $"  Input     : Fidelity={fidelity}%  Nodes={nodeCount}  Dist={distKm}km  Hops={hops}\n" +
            $"  Baseline  : Fid={baselineFid:F1}%  (expect {expectedBaselineMin}–{expectedBaselineMax}%)  " +
                $"QBER={baselineQBER:F1}%  E-Rate={baselineRate:F1}\n" +
            $"  Heavy     : Fid={heavyFid:F1}%   (expect {expectedHeavyMin}–{expectedHeavyMax}%)  " +
                $"QBER={heavyQBER:F1}%  E-Rate={heavyRate:F1}  (drop {rateDropPct:F0}%)\n" +
            $"  Fid drop  : {baselineFid - heavyFid:F1}%  |  " +
            $"Baseline {(baselinePass ? "OK" : "OUT OF RANGE")}  |  " +
            $"Heavy {(heavyPass ? "OK" : "OUT OF RANGE")}\n"
        );
    }
}