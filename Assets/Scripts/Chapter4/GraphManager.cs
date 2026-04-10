using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public FlowManager flowManager;

    [Header("Cascade Settings")]
    public float cascadeInterval = 1.0f;  // วินาทีต่อ node ที่พัง

    // Simulation state
    [HideInInspector] public bool simFail, simJam, simHeavy;
    [HideInInspector] public bool simDegrade, simCascade;
    [HideInInspector] public int  failNode = -1;
    [HideInInspector] public int  selNode  = -1;

    // LinkDegrade — เก็บ index ของ link ที่ degrade
    [HideInInspector] public HashSet<int> degradedLinks = new HashSet<int>();

    // CascadeFailure — เก็บ node ที่พังแล้ว
    [HideInInspector] public HashSet<int> cascadeFailedNodes = new HashSet<int>();

    private Coroutine cascadeCoroutine;

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

    IEnumerator Start()
    {
        Rebuild();
        yield return null;
        Refresh();
    }

    // ─── Rebuild ─────────────────────────────────────────
    public void Rebuild()
    {
        // หยุด cascade ก่อน rebuild
        if (cascadeCoroutine != null) StopCoroutine(cascadeCoroutine);
        cascadeFailedNodes.Clear();
        degradedLinks.Clear();

        builder.Build(currentTopo, nodeCount, spacing);
        Refresh();
    }

    // ─── Refresh ─────────────────────────────────────────
    public void Refresh()
    {
        builder.RefreshNodeColors(failNode, selNode, cascadeFailedNodes);
        builder.RefreshLinkColors(failNode, selNode, simFail, simJam, simHeavy,
                                   simDegrade, degradedLinks, cascadeFailedNodes);
        builder.RefreshLabels(failNode, selNode);
        builder.RefreshLinkLabels();
        flowManager?.SetFidelity(fidelity);
        flowManager?.SetHeavyTraffic(simHeavy);
        flowManager?.RefreshDegradedLinks(degradedLinks);
        flowManager?.SetFlowEnabled(ovFlow);                              // ← ย้ายมาก่อน
        flowManager?.RefreshFailedLinks(failNode, cascadeFailedNodes, builder); // ← หลังสุด
        FindFirstObjectByType<MetricsPanel>()?.Refresh();
    }

    // ─── Topology ────────────────────────────────────────
    public void SetTopo(string topo)
    {
        currentTopo = topo;
        failNode = -1; selNode = -1;
        simDegrade = false; simCascade = false;
        Rebuild();
    }

    // ─── Parameters ──────────────────────────────────────
    public void SetNodeCount(int n)
    {
        nodeCount = n;
        failNode = -1; selNode = -1;
        Rebuild();
    }

    public void SetSpacing(float s)   { spacing  = s; Rebuild(); }
    public void SetDistKm(float d)    { distKm   = d; Refresh(); }
    public void SetFidelity(float f)  { fidelity = f; Refresh(); }
    public void SetRedundancy(int r)  { redundancy   = r; Refresh(); }
    public void SetHubCapacity(int h) { hubCapacity  = h; Refresh(); }

    // ─── Overlay ─────────────────────────────────────────
    public void SetOvLabel(bool v) { ovLabel = v; Refresh(); }
    public void SetOvDist(bool v)  { ovDist  = v; Refresh(); }
    public void SetOvFid(bool v)   { ovFid   = v; Refresh(); }
    public void SetOvFlow(bool v)  { ovFlow  = v; Refresh(); }

    // ─── Simulation ──────────────────────────────────────

    // Node Fail — node กลางพัง
    public void ToggleFail()
    {
        simFail  = !simFail;
        failNode = simFail ? nodeCount / 2 : -1;
        Refresh();
    }

    // Noise — สัญญาณรบกวน fidelity ลด
    public void ToggleJam()   { simJam   = !simJam;   Refresh(); }

    // Heavy Traffic — hops เพิ่ม latency สูง
    public void ToggleHeavy() { simHeavy = !simHeavy; Refresh(); }

    // Link Degrade — link สุ่ม degrade fidelity ลด 30%
    public void ToggleDegrade()
    {
        simDegrade = !simDegrade;

        if (simDegrade)
        {
            // สุ่ม ~40% ของ link ให้ degrade
            degradedLinks.Clear();
            int total = builder.links.Count;
            for (int i = 0; i < total; i++)
                if (Random.value < 0.4f) degradedLinks.Add(i);

            // ถ้าไม่มีเลยให้มีอย่างน้อย 1
            if (degradedLinks.Count == 0 && total > 0)
                degradedLinks.Add(Random.Range(0, total));
        }
        else
        {
            degradedLinks.Clear();
        }

        Refresh();
    }

    // Cascade Failure — node พังลามทีละตัวทุก cascadeInterval วินาที
    public void ToggleCascade()
    {
        simCascade = !simCascade;

        if (simCascade)
        {
            cascadeFailedNodes.Clear();
            cascadeCoroutine = StartCoroutine(CascadeRoutine());
        }
        else
        {
            if (cascadeCoroutine != null) StopCoroutine(cascadeCoroutine);
            cascadeFailedNodes.Clear();
            Refresh();
        }
    }

    IEnumerator CascadeRoutine()
    {
        // เริ่มจาก node กลาง
        int start = nodeCount / 2;
        cascadeFailedNodes.Add(start);
        Refresh();

        // ลามไปยัง neighbor ที่ยังไม่พัง ทีละตัวทุก cascadeInterval วินาที
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(start);

        while (queue.Count > 0 && simCascade)
        {
            yield return new WaitForSeconds(cascadeInterval);

            int current = queue.Dequeue();

            // หา neighbor ของ current
            foreach (var (a, b) in builder.links)
            {
                int neighbor = -1;
                if (a == current && !cascadeFailedNodes.Contains(b)) neighbor = b;
                if (b == current && !cascadeFailedNodes.Contains(a)) neighbor = a;

                if (neighbor >= 0)
                {
                    cascadeFailedNodes.Add(neighbor);
                    queue.Enqueue(neighbor);
                    Refresh();
                    yield return new WaitForSeconds(cascadeInterval);
                }
            }
        }
    }

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
            default:
                hops  = nodeCount / 2;
                fault = "ดี";
                break;
        }

        float fid    = fidelity / 100f;
        int totalFid = Mathf.RoundToInt(Mathf.Pow(fid, hops) * 100);

        if (simFail)    { hops++; totalFid = Mathf.Max(8, totalFid - 22); }
        if (simJam)     totalFid = Mathf.Max(8, totalFid - 18);
        if (simHeavy)   hops     = Mathf.RoundToInt(hops * 1.5f);
        if (simDegrade) totalFid = Mathf.Max(8, totalFid - 15);
        if (simCascade && cascadeFailedNodes.Count > 0)
        {
            hops    += cascadeFailedNodes.Count;
            totalFid = Mathf.Max(8, totalFid - cascadeFailedNodes.Count * 10);
        }

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