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

    [Header("UI Panels")]
    public GameObject noiseInfoCanvas;

    [Header("Cascade Settings")]
    public float cascadeInterval = 1.0f;

    [Header("Noise Settings")]
    [Range(0f, 0.5f)]
    public float noiseStrength = 0.15f; // noise ต่อ link (0 = ไม่มี, 0.5 = มาก)

    // Simulation state
    [HideInInspector] public bool simFail, simJam, simHeavy;
    [HideInInspector] public bool simDegrade, simCascade;
    [HideInInspector] public int  failNode = -1;
    [HideInInspector] public int  selNode  = -1;

    // LinkDegrade
    [HideInInspector] public HashSet<int> degradedLinks = new HashSet<int>();

    // CascadeFailure
    [HideInInspector] public HashSet<int> cascadeFailedNodes = new HashSet<int>();

    // Noise — fidelity ต่อ link หลังคำนวณ noise
    [HideInInspector] public float[] linkFidelities = new float[0];

    // Cache noise materials เพื่อไม่ให้สร้างใหม่ทุก Refresh()
    [HideInInspector] public Material[] noiseLinkMaterials = new Material[0];

    private Coroutine cascadeCoroutine;
    public  Coroutine noiseCoroutine;

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
        if (noiseInfoCanvas != null)
            noiseInfoCanvas.SetActive(false);
        Rebuild();
        yield return null;
        Refresh();
    }

    // ─── Rebuild ─────────────────────────────────────────
    public void Rebuild()
    {
        if (cascadeCoroutine != null) StopCoroutine(cascadeCoroutine);
        if (noiseCoroutine   != null) StopCoroutine(noiseCoroutine);
        cascadeFailedNodes.Clear();
        degradedLinks.Clear();
        linkFidelities    = new float[0];
        noiseLinkMaterials = new Material[0];

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

        // Flow
        flowManager?.SetHeavyTraffic(simHeavy);
        flowManager?.RefreshDegradedLinks(degradedLinks);
        flowManager?.SetFlowEnabled(ovFlow);
        flowManager?.RefreshFailedLinks(failNode, cascadeFailedNodes, builder);

        // Noise — เรียกหลังสุดเสมอ ไม่ให้ถูก override โดย RefreshFailedLinks
        if (simJam && linkFidelities.Length > 0)
            flowManager?.SetNoisyFidelities(linkFidelities);
        else
            flowManager?.SetFidelity(fidelity);

        FindFirstObjectByType<MetricsPanel>()?.Refresh();
        FindFirstObjectByType<NoiseInfoPanel>()?.Refresh();
    }

    // ─── Topology ────────────────────────────────────────
    // Event แจ้ง UIController ว่า simJam state เปลี่ยน
    public System.Action<bool> onSimJamChanged;

    public void SetTopo(string topo)
    {
        currentTopo = topo;
        failNode = -1; selNode = -1;
        simDegrade = false; simCascade = false;
        // ไม่ reset simJam เพื่อให้ noise ยังเปิดอยู่
        linkFidelities    = new float[0];
        noiseLinkMaterials = new Material[0];
        if (noiseInfoCanvas != null) noiseInfoCanvas.SetActive(simJam);
        if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);
        Rebuild();
        if (simJam)
            noiseCoroutine = StartCoroutine(NoiseRoutine());
    }

    // ─── Parameters ──────────────────────────────────────
    public void SetNodeCount(int n)
    {
        nodeCount = n;
        failNode = -1; selNode = -1;
        linkFidelities = new float[0];
        if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);
        Rebuild();
        if (simJam)
            noiseCoroutine = StartCoroutine(NoiseRoutine());
    }

    public void SetSpacing(float s)   { spacing  = s; Rebuild(); }
    public void SetDistKm(float d)    { distKm   = d; Refresh(); }
    public void SetFidelity(float f)
    {
        fidelity = f;
        if (simJam)
        {
            if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);
            noiseCoroutine = StartCoroutine(NoiseRoutine());
        }
        else Refresh();
    }
    public void SetRedundancy(int r)  { redundancy   = r; Refresh(); }
    public void SetHubCapacity(int h) { hubCapacity  = h; Refresh(); }

    // ─── Overlay ─────────────────────────────────────────
    public void SetOvLabel(bool v) { ovLabel = v; Refresh(); }
    public void SetOvDist(bool v)  { ovDist  = v; Refresh(); }
    public void SetOvFid(bool v)   { ovFid   = v; Refresh(); }
    public void SetOvFlow(bool v)  { ovFlow  = v; Refresh(); }

    // ─── Simulation ──────────────────────────────────────

    public void ToggleFail()
    {
        simFail  = !simFail;
        failNode = simFail ? nodeCount / 2 : -1;
        Refresh();
    }

    // Noise — animate fidelity ลดทีละ link จาก Alice → Bob
    public void ToggleJam()
    {
        simJam = !simJam;

        // แสดง/ซ่อน NoiseInfoCanvas
        if (noiseInfoCanvas != null)
            noiseInfoCanvas.SetActive(simJam);

        if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);

        if (simJam)
            noiseCoroutine = StartCoroutine(NoiseRoutine());
        else
        {
            linkFidelities = new float[0];
            Refresh();
        }
    }

    public IEnumerator NoiseRoutine()
    {
        int linkCount = builder.links.Count;
        linkFidelities = new float[linkCount];

        // เริ่มด้วย fidelity ปกติทุก link
        for (int i = 0; i < linkCount; i++)
            linkFidelities[i] = fidelity;

        Refresh();
        yield return new WaitForSeconds(0.3f);

        // หา link ที่เรียงตาม path จาก Alice → Bob
        var orderedLinks = GetLinksInOrder();

        // ── Distance factor (Fiber Attenuation Model) ────────────────────────
        // ระยะทางต่อ link = distKm / จำนวน link ทั้งหมด
        // distFactor อิงสูตร exponential attenuation: e^(-d / L_att)
        // L_att = 200 km (baseline สำหรับ quantum fiber)
        // ยิ่งไกล → distFactor ยิ่งน้อย → fidelity ลดมากขึ้น
        float distPerLink  = Mathf.Max(1f, distKm / Mathf.Max(1, linkCount));
        float L_att        = 200f;
        float distFactor   = Mathf.Exp(-distPerLink / L_att);  // range (0, 1]
        // ─────────────────────────────────────────────────────────────────────

        // animate ลด fidelity ทีละ link — noise สะสมตาม hop + distance
        int hopIndex = 0;
        foreach (int li in orderedLinks)
        {
            if (!simJam) yield break;

            float baseFid     = fidelity / 100f;
            // noise สะสม: ยิ่ง hop มาก noise มากขึ้น 10% ต่อ hop
            float accumFactor = 1f + hopIndex * 0.1f;
            float noiseFactor = 1f - noiseStrength * accumFactor * (1f + Random.Range(-0.2f, 0.2f));
            // คูณ distFactor: ระยะไกล → fidelity ลดเพิ่มอีกชั้น
            linkFidelities[li] = Mathf.Max(8f, baseFid * noiseFactor * distFactor * 100f);

            hopIndex++;
            Refresh();
            yield return new WaitForSeconds(0.4f);
        }

        // link นอก main path (branch ใน mesh/star/tree) — noise เบาๆ + distance
        for (int i = 0; i < linkCount; i++)
        {
            if (linkFidelities[i] >= fidelity)
            {
                float baseFid     = fidelity / 100f;
                float noiseFactor = 1f - (noiseStrength * 0.5f) * (1f + Random.Range(-0.2f, 0.2f));
                linkFidelities[i] = Mathf.Max(8f, baseFid * noiseFactor * distFactor * 100f);
            }
        }

        Refresh();

        // ── Subtle fluctuation หลัง animate จบ ──────────
        // เก็บค่า base fidelity หลัง noise ไว้ fluctuate รอบๆ
        float[] baseFidelities = new float[linkFidelities.Length];
        System.Array.Copy(linkFidelities, baseFidelities, linkFidelities.Length);

        while (simJam)
        {
            yield return new WaitForSeconds(1.5f);
            if (!simJam) break;

            // fluctuate เล็กน้อย ±5% รอบค่า base
            for (int i = 0; i < linkFidelities.Length; i++)
            {
                float fluctuation = Random.Range(-0.02f, 0.02f) * baseFidelities[i];
                linkFidelities[i] = Mathf.Max(8f, baseFidelities[i] + fluctuation);
            }

            Refresh();
        }
    }

    // หา link เรียงตาม path จาก Alice → Bob ของแต่ละ topology
    public List<int> GetLinksInOrder()
    {
        var ordered = new List<int>();
        int n       = nodeCount;

        switch (currentTopo)
        {
            case "linear":
                // link (0,1),(1,2),...,(n-2,n-1) เรียงตาม index
                for (int i = 0; i < builder.links.Count; i++)
                    ordered.Add(i);
                break;

            case "star":
                // Alice(1)→Hub(0) ก่อน แล้ว Hub(0)→Bob(2)
                for (int i = 0; i < builder.links.Count; i++)
                {
                    var (a, b) = builder.links[i];
                    if ((a == 0 && b == 1) || (a == 1 && b == 0)) { ordered.Insert(0, i); }
                    else if ((a == 0 && b == 2) || (a == 2 && b == 0)) { ordered.Add(i); }
                }
                break;

            case "ring":
                // CW path ก่อน: link (0,1),(1,2),...,(half-1,half)
                int half = Mathf.CeilToInt(n / 2f);
                for (int i = 0; i < builder.links.Count; i++)
                {
                    var (a, b) = builder.links[i];
                    if (a < half && b <= half && Mathf.Abs(a - b) == 1)
                        ordered.Add(i);
                }
                break;

            case "tree":
                // จาก Alice(1)→Root(0) แล้วลงมาถึง Bob(n-1)
                // เรียงตาม depth ของ link
                for (int i = 0; i < builder.links.Count; i++)
                    ordered.Add(i);
                break;

            case "mesh":
                // เรียงตาม index link ที่ใกล้ Alice ก่อน
                for (int i = 0; i < builder.links.Count; i++)
                    ordered.Add(i);
                break;
        }

        // ถ้าไม่มีเลยใช้ทุก link
        if (ordered.Count == 0)
            for (int i = 0; i < builder.links.Count; i++)
                ordered.Add(i);

        return ordered;
    }

    public void ToggleHeavy()   { simHeavy   = !simHeavy;   Refresh(); }

    public void ToggleDegrade()
    {
        simDegrade = !simDegrade;
        if (simDegrade)
        {
            degradedLinks.Clear();
            int total = builder.links.Count;
            for (int i = 0; i < total; i++)
                if (Random.value < 0.4f) degradedLinks.Add(i);
            if (degradedLinks.Count == 0 && total > 0)
                degradedLinks.Add(Random.Range(0, total));
        }
        else degradedLinks.Clear();
        Refresh();
    }

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
        int start = nodeCount / 2;
        cascadeFailedNodes.Add(start);
        Refresh();

        Queue<int> queue = new Queue<int>();
        queue.Enqueue(start);

        while (queue.Count > 0 && simCascade)
        {
            yield return new WaitForSeconds(cascadeInterval);
            int current = queue.Dequeue();

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
        if (!simFail) return;  // ← กดได้เฉพาะตอน Node Fail เท่านั้น
        failNode = (failNode == index) ? -1 : index;
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

        if (simFail)  { hops++; totalFid = Mathf.Max(8, totalFid - 22); }

        // Noise — ใช้ค่า fidelity สะสมจาก linkFidelities จริงๆ
        if (simJam && linkFidelities.Length > 0)
        {
            float accumulated = 1f;
            var   path        = GetLinksInOrder();
            foreach (int li in path)
                if (li < linkFidelities.Length)
                    accumulated *= linkFidelities[li] / 100f;
            totalFid = Mathf.Max(8, Mathf.RoundToInt(accumulated * 100));
        }

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