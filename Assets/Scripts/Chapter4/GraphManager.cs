using UnityEngine;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance;

    [Header("References")]
    public TopologyBuilder builder;

    [Header("Default Settings")]
    public string currentTopo = "linear";
    public int nodeCount = 5;
    public float spacing = 2f;

    [Header("Flow")]
    public FlowManager flowManager;  // ลาก FlowManager ใน Inspector

    // Simulation state
    [HideInInspector] public bool simFail, simJam, simHeavy;
    [HideInInspector] public int failNode = -1;
    [HideInInspector] public int selNode  = -1;

    // Overlay state
    [HideInInspector] public bool ovLabel = true;
    [HideInInspector] public bool ovDist  = false;
    [HideInInspector] public bool ovFid   = false;
    [HideInInspector] public bool ovFlow  = true;

    // Parameters
    [HideInInspector] public float distKm    = 150f;
    [HideInInspector] public float fidelity  = 90f;
    [HideInInspector] public int redundancy  = 2;
    [HideInInspector] public int hubCapacity = 4;

    void Awake() { Instance = this; }

        System.Collections.IEnumerator Start()
    {
        Rebuild();
        yield return null;  // รอ 1 frame ให้ LinkFlowEffect.Start() รันก่อน
        Refresh();          // แล้วค่อย SetFidelity อีกครั้ง
    }
    // ─── เรียกทุกครั้งที่มีการเปลี่ยนแปลง topology / nodeCount / spacing ───
    public void Rebuild()
    {
        builder.Build(currentTopo, nodeCount, spacing);
        Refresh();
    }

    // Refresh เฉพาะสี + label — ไม่ต้อง rebuild ทั้งหมด
    public void Refresh()
{
    builder.RefreshNodeColors(failNode, selNode);
    builder.RefreshLinkColors(failNode, selNode, simFail, simJam, simHeavy);
    builder.RefreshLabels(failNode, selNode);
    builder.RefreshLinkLabels();
    flowManager?.SetFidelity(fidelity);
    flowManager?.SetFlowEnabled(ovFlow);
    FindFirstObjectByType<MetricsPanel>()?.Refresh();  // ← เพิ่มบรรทัดนี้
}

    // ─── Topology ────────────────────────────────────────
    public void SetTopo(string topo)
    {
        currentTopo = topo;
        failNode = -1;
        selNode  = -1;
        Rebuild();
    }

    // ─── Parameters ──────────────────────────────────────
    public void SetNodeCount(int n)
    {
        nodeCount = n;
        failNode = -1;
        selNode  = -1;
        Rebuild();
    }

    public void SetSpacing(float s)   { spacing   = s; Rebuild(); }

    // distKm และ fidelity → Refresh() เพื่ออัปเดต link labels ด้วย
    public void SetDistKm(float d)    { distKm    = d; Refresh(); }
    public void SetFidelity(float f)  { fidelity  = f; Refresh(); }

    public void SetRedundancy(int r)  { redundancy   = r; Refresh(); }
    public void SetHubCapacity(int h) { hubCapacity  = h; Refresh(); }

    // ─── Overlay ─────────────────────────────────────────
    public void SetOvLabel(bool v) { ovLabel = v; Refresh(); }
    public void SetOvDist(bool v)  { ovDist  = v; Refresh(); }   // toggle → แสดง/ซ่อน Distance labels
    public void SetOvFid(bool v)   { ovFid   = v; Refresh(); }   // toggle → แสดง/ซ่อน Fidelity labels
    public void SetOvFlow(bool v)  { ovFlow  = v; Refresh(); }

    // ─── Simulation ──────────────────────────────────────
    public void ToggleFail()
    {
        simFail  = !simFail;
        failNode = simFail ? nodeCount / 2 : -1;
        Refresh();
    }

    public void ToggleJam()   { simJam   = !simJam;   Refresh(); }
    public void ToggleHeavy() { simHeavy = !simHeavy; Refresh(); }

    // ─── Node Click ──────────────────────────────────────
    public void OnNodeClicked(int index)
    {
        if (simFail)
            failNode = (failNode == index) ? -1 : index;
        else
            selNode  = (selNode  == index) ? -1 : index;
        Refresh();
    }

    // ─── Metrics ─────────────────────────────────────────
    public MetricsData GetMetrics()
    {
        int L = builder.links.Count;
        int hops;
        string fault;

        switch (currentTopo)
        {
            case "linear": hops = nodeCount - 1; fault = "ต่ำ";  break;
            case "star":   hops = 2;             fault = "กลาง"; break;
            case "mesh":
                hops  = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(nodeCount)) - 1);
                fault = "สูง";
                break;
            case "tree":
                hops  = Mathf.CeilToInt(Mathf.Log(nodeCount + 1, 2));
                fault = "กลาง";
                break;
            default: // ring
                hops  = nodeCount / 2;
                fault = "ดี";
                break;
        }

        float fid      = fidelity / 100f;
        int totalFid   = Mathf.RoundToInt(Mathf.Pow(fid, hops) * 100);

        if (simFail)  { hops++; totalFid = Mathf.Max(8, totalFid - 22); }
        if (simJam)   totalFid = Mathf.Max(8, totalFid - 18);
        if (simHeavy) hops     = Mathf.RoundToInt(hops * 1.5f);

        return new MetricsData
        {
            hops     = hops,
            links    = L,
            fidelity = totalFid,
            distKm   = Mathf.RoundToInt(distKm * hops),
            fault    = fault
        };
    }
}

public class MetricsData
{
    public int    hops;
    public int    links;
    public int    fidelity;
    public int    distKm;
    public string fault;
}