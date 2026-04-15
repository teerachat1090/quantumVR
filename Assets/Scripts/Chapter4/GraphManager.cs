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
        if (heavyCoroutine   != null) StopCoroutine(heavyCoroutine);
        cascadeFailedNodes.Clear();
        degradedLinks.Clear();
        linkFidelities      = new float[0];
        noiseLinkMaterials  = new float[0].Length > 0 ? noiseLinkMaterials : new Material[0];
        heavyBaseFidelities = null;

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
        flowManager?.SetFlowEnabled(ovFlow);
        flowManager?.RefreshFailedLinks(failNode, cascadeFailedNodes, builder);

        // Distance speed (ถ้า Heavy และ Degrade ไม่เปิด)
        if (!simHeavy && !simDegrade) flowManager?.SetDistanceSpeed(distKm);

        // Noise / Heavy — สีตาม fidelity จริง
        if ((simJam || simHeavy) && linkFidelities.Length > 0)
            flowManager?.SetNoisyFidelities(linkFidelities);
        else
            flowManager?.SetFidelity(fidelity);

        // Heavy speed — เรียกหลัง SetFidelity
        if (simHeavy) flowManager?.SetHeavyTraffic(true);

        // Degrade — เรียกหลังสุด ไม่ให้ถูก SetFidelity ทับสีและ speed
        flowManager?.RefreshDegradedLinks(simDegrade ? degradedLinks : new System.Collections.Generic.HashSet<int>());

        FindFirstObjectByType<MetricsPanel>()?.Refresh();
        FindFirstObjectByType<NoiseInfoPanel>()?.Refresh();
    }

    // ─── Topology ────────────────────────────────────────
    // Event แจ้ง UIController ว่า simJam state เปลี่ยน
    // Events แจ้ง UIController เมื่อ state เปลี่ยน (สำหรับ mutex UI sync)
    public System.Action<bool> onSimJamChanged;
    public System.Action<bool> onSimFailChanged;
    public System.Action<bool> onSimCascadeChanged;

    public void SetTopo(string topo)
    {
        currentTopo = topo;
        failNode = -1; selNode = -1;
        simCascade = false;
        linkFidelities     = new float[0];
        noiseLinkMaterials = new Material[0];
        if (noiseInfoCanvas != null) noiseInfoCanvas.SetActive(simJam);
        if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);
        bool wasDegrade = simDegrade;
        simDegrade = false;
        Rebuild();
        if (simJam)
            noiseCoroutine = StartCoroutine(NoiseRoutine());
        // re-random degrade บน topology ใหม่
        if (wasDegrade) ToggleDegrade();
    }

    // ─── Parameters ──────────────────────────────────────
    public void SetNodeCount(int n)
    {
        nodeCount = n;
        failNode = -1; selNode = -1;
        linkFidelities = new float[0];
        if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);
        bool wasDegrade = simDegrade;
        simDegrade = false;
        Rebuild();
        if (simJam)
            noiseCoroutine = StartCoroutine(NoiseRoutine());
        // re-random degrade บน node count ใหม่
        if (wasDegrade) ToggleDegrade();
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
        // Mutex — ปิด Cascade ก่อนถ้าเปิดอยู่
        if (!simFail && simCascade)
        {
            simCascade = false;
            if (cascadeCoroutine != null) StopCoroutine(cascadeCoroutine);
            cascadeFailedNodes.Clear();
            onSimCascadeChanged?.Invoke(false);
        }
        simFail  = !simFail;
        if (simFail)
        {
            int aliceIdx = builder.nodeDataList.FindIndex(d => d.label == "Alice");
            int bobIdx   = builder.nodeDataList.FindIndex(d => d.label == "Bob");
            // หา node กลาง path ที่ไม่ใช่ Alice/Bob
            int candidate = nodeCount / 2;
            for (int attempt = 0; attempt < nodeCount; attempt++)
            {
                int idx = (candidate + attempt) % nodeCount;
                if (idx != aliceIdx && idx != bobIdx) { failNode = idx; break; }
            }
        }
        else failNode = -1;
        onSimFailChanged?.Invoke(simFail);
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

        // หา path จาก Alice → Bob โดยข้าม failNode และ cascadeFailedNodes
        var allFailed = new HashSet<int>(cascadeFailedNodes ?? new HashSet<int>());
        if (simFail && failNode >= 0) allFailed.Add(failNode);

        List<int> orderedLinks;
        if (allFailed.Count > 0)
        {
            // BFS หา path จริงที่ข้าม failed nodes
            int aliceIdx = builder.nodeDataList.FindIndex(d => d.label == "Alice");
            int bobIdx   = builder.nodeDataList.FindIndex(d => d.label == "Bob");
            orderedLinks = BFSPathExcluding(aliceIdx, bobIdx, allFailed);
            // ถ้าหา path ไม่ได้เลย (partitioned) ให้ set fidelity = 0 ทุก link แล้วออก
            if (orderedLinks.Count == 0)
            {
                for (int i = 0; i < linkCount; i++) linkFidelities[i] = 0f;
                Refresh();
                yield break;
            }
        }
        else
            orderedLinks = GetLinksInOrder();

        // ── Quantum Channel Model ─────────────────────────────────────────────
        // distFactor: fiber attenuation per link — e^(-d/L_att)
        // L_att = 280 km — ตรงกับ GetMetrics() และ QuantumDijkstra()
        float distPerLink = Mathf.Max(1f, distKm / Mathf.Max(1, linkCount));
        const float L_att = 280f;
        float distFactor  = Mathf.Exp(-distPerLink / L_att);

        // F_swap: fidelity ของ Bell State Measurement ที่ repeater แต่ละตัว
        // อ้างอิงจาก state-of-the-art trapped-ion / NV-center (~98%)
        const float F_swap = 0.98f;

        // F_gate: fidelity ของ single qubit gate operation (~99%)
        // ใช้แยกออกจาก F_swap เพื่อความสมจริง
        const float F_gate = 0.99f;
        // ─────────────────────────────────────────────────────────────────────

        // animate ลด fidelity ทีละ link
        // สูตร: F_link_i = F_src × noise_i × distFactor
        //       F_total  = F_link1 × F_link2 × ... × F_swap^(hops-1) × F_gate^hops
        float swapAccumulated  = 1f;  // สะสม F_swap และ noise
        foreach (int li in orderedLinks)
        {
            if (!simJam) yield break;

            float baseFid = fidelity / 100f;

            if (noiseStrength <= 0f)
            {
                // Noise = 0 → แสดง fidelity ตามค่าตั้ง ไม่มี attenuation
                linkFidelities[li] = fidelity;
            }
            else
            {
                // noise per link ±5% jitter
                float jitter      = 1f + Random.Range(-0.05f, 0.05f);
                float noiseFactor = Mathf.Clamp(1f - noiseStrength * jitter, 0f, 1f);

                // F_swap สะสมตาม hop (Entanglement Swapping loss)
                // F_gate ใช้ per link (gate operation ที่ repeater)
                // distFactor ใช้ per link (fiber attenuation)
                swapAccumulated *= noiseFactor * F_swap;
                linkFidelities[li] = Mathf.Max(50f,
                    baseFid * swapAccumulated * distFactor * F_gate * 100f);
            }

            Refresh();
            yield return new WaitForSeconds(0.4f);
        }

        // link นอก main path (branch ใน mesh/star/tree) — noise เบาๆ
        for (int i = 0; i < linkCount; i++)
        {
            if (linkFidelities[i] >= fidelity)
            {
                if (noiseStrength <= 0f)
                {
                    linkFidelities[i] = fidelity;
                }
                else
                {
                    float baseFid     = fidelity / 100f;
                    float noiseFactor = Mathf.Clamp(1f - (noiseStrength * 0.5f) *
                                        (1f + Random.Range(-0.1f, 0.1f)), 0f, 1f);
                    linkFidelities[i] = Mathf.Max(50f,
                        baseFid * noiseFactor * distFactor * F_swap * F_gate * 100f);
                }
            }
        }

        Refresh();

        // ── Subtle fluctuation หลัง animate จบ ──────────
        float[] baseFidelities = new float[linkFidelities.Length];
        System.Array.Copy(linkFidelities, baseFidelities, linkFidelities.Length);

        while (simJam)
        {
            yield return new WaitForSeconds(1.5f);
            if (!simJam) break;

            // Noise = 0 → ไม่มี fluctuation ไม่ต้อง Refresh
            if (noiseStrength <= 0f) continue;

            for (int i = 0; i < linkFidelities.Length; i++)
            {
                float fluctuation = Random.Range(-0.02f, 0.02f) * baseFidelities[i];
                linkFidelities[i] = Mathf.Max(50f, baseFidelities[i] + fluctuation);
            }

            Refresh();
        }
    }

    // ─── BFS Path ข้าม failed nodes ─────────────────────
    public List<int> BFSPathExcluding(int start, int end, HashSet<int> failed)
    {
        if (start < 0 || end < 0) return new List<int>();

        var adj = new Dictionary<int, List<(int nb, int li)>>();
        for (int i = 0; i < builder.links.Count; i++)
        {
            var (a, b) = builder.links[i];
            if (!adj.ContainsKey(a)) adj[a] = new List<(int, int)>();
            if (!adj.ContainsKey(b)) adj[b] = new List<(int, int)>();
            adj[a].Add((b, i));
            adj[b].Add((a, i));
        }

        var prev    = new Dictionary<int, (int node, int link)>();
        var visited = new HashSet<int>(failed) { start };
        var queue   = new Queue<int>();
        queue.Enqueue(start);
        bool found  = false;

        while (queue.Count > 0 && !found)
        {
            int cur = queue.Dequeue();
            if (!adj.ContainsKey(cur)) continue;
            foreach (var (nb, li) in adj[cur])
            {
                if (visited.Add(nb))
                {
                    prev[nb] = (cur, li);
                    if (nb == end) { found = true; break; }
                    queue.Enqueue(nb);
                }
            }
        }

        if (!found) return new List<int>();

        var path = new List<int>();
        int c    = end;
        while (prev.ContainsKey(c))
        {
            var (prevNode, li) = prev[c];
            path.Add(li);
            c = prevNode;
        }
        path.Reverse();
        return path;
    }

    // ─── BFS Path ข้าม failed nodes ─────────────────────

    // หา link เรียงตาม path จาก Alice → Bob ของแต่ละ topology
    public List<int> GetLinksInOrder()
    {
        var ordered = new List<int>();
        int n       = nodeCount;

        switch (currentTopo)
        {
            case "linear":
                for (int i = 0; i < builder.links.Count; i++)
                    ordered.Add(i);
                break;

            case "star":
                // Alice(1)→Hub(0) ก่อน แล้ว Hub(0)→Bob(2)
                for (int i = 0; i < builder.links.Count; i++)
                {
                    var (a, b) = builder.links[i];
                    if ((a == 0 && b == 1) || (a == 1 && b == 0)) ordered.Insert(0, i);
                    else if ((a == 0 && b == 2) || (a == 2 && b == 0)) ordered.Add(i);
                }
                break;

            case "ring":
                // CW path: link (0,1),(1,2),...,(half-1,half)
                int half = Mathf.CeilToInt(n / 2f);
                for (int i = 0; i < builder.links.Count; i++)
                {
                    var (a, b) = builder.links[i];
                    if (a < half && b <= half && Mathf.Abs(a - b) == 1)
                        ordered.Add(i);
                }
                break;

            case "tree":
            case "mesh":
            {
                // BFS trace จาก Alice → Bob หา link ตาม path จริง
                int aliceIdx = builder.nodeDataList.FindIndex(d => d.label == "Alice");
                int bobIdx   = builder.nodeDataList.FindIndex(d => d.label == "Bob");

                if (aliceIdx >= 0 && bobIdx >= 0)
                {
                    var adj = new Dictionary<int, List<(int nb, int li)>>();
                    for (int i = 0; i < builder.links.Count; i++)
                    {
                        var (a, b) = builder.links[i];
                        if (!adj.ContainsKey(a)) adj[a] = new List<(int, int)>();
                        if (!adj.ContainsKey(b)) adj[b] = new List<(int, int)>();
                        adj[a].Add((b, i));
                        adj[b].Add((a, i));
                    }

                    var prev    = new Dictionary<int, (int node, int link)>();
                    var visited = new HashSet<int> { aliceIdx };
                    var queue   = new Queue<int>();
                    queue.Enqueue(aliceIdx);
                    bool found  = false;

                    while (queue.Count > 0 && !found)
                    {
                        int cur = queue.Dequeue();
                        if (!adj.ContainsKey(cur)) continue;
                        foreach (var (nb, li) in adj[cur])
                        {
                            if (visited.Add(nb))
                            {
                                prev[nb] = (cur, li);
                                if (nb == bobIdx) { found = true; break; }
                                queue.Enqueue(nb);
                            }
                        }
                    }

                    if (found)
                    {
                        var path = new List<int>();
                        int cur  = bobIdx;
                        while (prev.ContainsKey(cur))
                        {
                            var (prevNode, li) = prev[cur];
                            path.Add(li);
                            cur = prevNode;
                        }
                        path.Reverse();
                        ordered = path;
                    }
                }
                break;
            }
        }

        // fallback
        if (ordered.Count == 0)
            for (int i = 0; i < builder.links.Count; i++)
                ordered.Add(i);

        return ordered;
    }

    // ─── Heavy Traffic ───────────────────────────────────
    // Heavy Traffic จำลอง quantum channel แออัดจากหลาย request พร้อมกัน
    // ผล: photon ไหลช้า + fidelity ลด + QBER สูง + entanglement rate ตก

    [Header("Heavy Traffic Settings")]
    [Range(0.1f, 0.9f)]
    public float heavySpeedMultiplier = 0.35f;   // ชะลอ photon (ค่าน้อย = ช้ากว่า)

    [Range(0f, 0.4f)]
    public float heavyFidelityPenalty = 0.06f;   // ลด fidelity ต่อ link (0.06 = ลง 6%)

    private Coroutine heavyCoroutine;
    private float[]   heavyBaseFidelities;        // เก็บ fidelity ก่อน heavy เพื่อ restore

    public void ToggleHeavy()
    {
        simHeavy = !simHeavy;

        if (heavyCoroutine != null) StopCoroutine(heavyCoroutine);

        if (simHeavy)
            heavyCoroutine = StartCoroutine(HeavyRoutine());
        else
        {
            // restore — ถ้า noise เปิดอยู่ด้วย ให้ re-run NoiseRoutine
            // ถ้าไม่มี noise ให้ reset linkFidelities กลับเป็นค่าปกติ
            if (simJam)
            {
                if (noiseCoroutine != null) StopCoroutine(noiseCoroutine);
                noiseCoroutine = StartCoroutine(NoiseRoutine());
            }
            else
            {
                linkFidelities = new float[0];
            }
            Refresh();
        }
    }

    IEnumerator HeavyRoutine()
    {
        int linkCount = builder.links.Count;

        // ── ชะลอ photon ทุก link ──────────────────────────
        flowManager?.SetHeavyTraffic(true);

        // ── คำนวณ fidelity ลงทีละ link (animate เหมือน NoiseRoutine) ──
        // ถ้า noise เปิดอยู่ด้วย ให้ใช้ค่าจาก linkFidelities เป็น base
        // ถ้าไม่มี noise ใช้ค่า fidelity ปกติเป็น base
        bool hasNoise = simJam && linkFidelities.Length == linkCount;

        heavyBaseFidelities = new float[linkCount];
        for (int i = 0; i < linkCount; i++)
            heavyBaseFidelities[i] = hasNoise ? linkFidelities[i] : fidelity;

        // init linkFidelities ถ้ายังว่าง
        if (linkFidelities.Length != linkCount)
        {
            linkFidelities = new float[linkCount];
            for (int i = 0; i < linkCount; i++)
                linkFidelities[i] = fidelity;
        }

        Refresh();
        yield return new WaitForSeconds(0.2f);

        // หา path จาก Alice → Bob (ข้าม failed nodes)
        var allFailed = new HashSet<int>(cascadeFailedNodes ?? new HashSet<int>());
        if (simFail && failNode >= 0) allFailed.Add(failNode);

        List<int> orderedLinks = allFailed.Count > 0
            ? BFSPathExcluding(
                builder.nodeDataList.FindIndex(d => d.label == "Alice"),
                builder.nodeDataList.FindIndex(d => d.label == "Bob"),
                allFailed)
            : GetLinksInOrder();

        if (orderedLinks.Count == 0) orderedLinks = GetLinksInOrder();

        // animate ลด fidelity ทีละ link บน main path
        foreach (int li in orderedLinks)
        {
            if (!simHeavy) yield break;

            float jitter  = 1f + Random.Range(-0.04f, 0.04f);
            float penalty = Mathf.Clamp(heavyFidelityPenalty * jitter, 0f, 0.5f);
            linkFidelities[li] = Mathf.Max(40f,
                heavyBaseFidelities[li] * (1f - penalty));

            Refresh();
            yield return new WaitForSeconds(0.35f);
        }

        // link นอก main path — penalty เบากว่าครึ่ง
        for (int i = 0; i < linkCount; i++)
        {
            if (!orderedLinks.Contains(i))
            {
                float penalty = heavyFidelityPenalty * 0.5f *
                                (1f + Random.Range(-0.05f, 0.05f));
                linkFidelities[i] = Mathf.Max(40f,
                    heavyBaseFidelities[i] * (1f - penalty));
            }
        }

        Refresh();

        // fluctuation เล็กน้อยขณะ heavy เปิดอยู่ (คล้าย NoiseRoutine)
        float[] baseHeavy = new float[linkCount];
        System.Array.Copy(linkFidelities, baseHeavy, linkCount);

        while (simHeavy)
        {
            yield return new WaitForSeconds(2f);
            if (!simHeavy) break;

            for (int i = 0; i < linkFidelities.Length; i++)
            {
                float f = Random.Range(-0.015f, 0.015f) * baseHeavy[i];
                linkFidelities[i] = Mathf.Max(40f, baseHeavy[i] + f);
            }
            Refresh();
        }
    }

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
        // Mutex — ปิด Node Fail ก่อนถ้าเปิดอยู่
        if (!simCascade && simFail)
        {
            simFail  = false;
            failNode = -1;
            onSimFailChanged?.Invoke(false);
        }
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
        onSimCascadeChanged?.Invoke(simCascade);
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
        if (!simFail) return;
        // ไม่ให้ Alice หรือ Bob เป็น failNode
        int aliceIdx = builder.nodeDataList.FindIndex(d => d.label == "Alice");
        int bobIdx   = builder.nodeDataList.FindIndex(d => d.label == "Bob");
        if (index == aliceIdx || index == bobIdx) return;
        failNode = (failNode == index) ? -1 : index;
        Refresh();
    }   

    // ─── Quantum Routing: Dijkstra บน fidelity สูงสุด ────
    // ใน Quantum Network จริง เลือก path ที่ fidelity สะสมสูงสุด
    // ไม่ใช่ hop น้อยสุด เพราะ entanglement เสื่อมตาม hop + ระยะทาง
    // คืนค่า (hops, accumulatedFidelity0to1) ของ best path
    (int hops, float bestFid) QuantumDijkstra(int start, int end)
    {
        if (start < 0 || end < 0) return (Mathf.Max(1, nodeCount - 1), 0f);
        if (start == end)         return (0, 1f);

        int n = builder.nodeDataList.Count;

        // สร้าง adjacency: node → list of (neighbor, linkIndex)
        var adj = new Dictionary<int, List<(int nb, int li)>>();
        for (int i = 0; i < builder.links.Count; i++)
        {
            var (a, b) = builder.links[i];
            if (!adj.ContainsKey(a)) adj[a] = new List<(int, int)>();
            if (!adj.ContainsKey(b)) adj[b] = new List<(int, int)>();
            adj[a].Add((b, i));
            adj[b].Add((a, i));
        }

        // fidelity ของแต่ละ link — ใช้ linkFidelities ถ้า Noise เปิด
        // มิเช่นนั้นใช้ค่า global fidelity
        bool useNoise = simJam && linkFidelities != null
                     && linkFidelities.Length == builder.links.Count;

        float LinkFid(int li)
        {
            // Noise เปิด → ใช้ fidelity จริงต่อ link
            if (useNoise) return linkFidelities[li] / 100f;
            // ปกติ → คำนวณจาก distFactor + F_swap + F_gate
            float dpl        = Mathf.Max(1f, distKm / Mathf.Max(1, builder.links.Count));
            float distFactor = Mathf.Exp(-dpl / 280f);
            const float F_swap = 0.98f;
            const float F_gate = 0.99f;
            return (fidelity / 100f) * distFactor * F_swap * F_gate;
        }

        // Dijkstra — maximize fidelity สะสม (เก็บ log เพื่อบวกแทนคูณ)
        // bestLogFid[i] = log(fidelity สะสมดีสุดถึง node i)
        var bestLogFid = new float[n];
        var hopsTo     = new int[n];
        for (int i = 0; i < n; i++) { bestLogFid[i] = float.NegativeInfinity; hopsTo[i] = 0; }
        bestLogFid[start] = 0f; // log(1) = 0

        // priority queue เรียงตาม logFid มากสุดก่อน (ใช้ SortedSet)
        // (negLogFid, node) — negate เพื่อให้ค่าน้อยสุด = fidelity ดีสุด
        var pq = new SortedSet<(float negLogFid, int node)>(
            Comparer<(float, int)>.Create((x, y) =>
                x.Item1 != y.Item1 ? x.Item1.CompareTo(y.Item1) : x.Item2.CompareTo(y.Item2)
            ));
        pq.Add((0f, start));

        while (pq.Count > 0)
        {
            var (negLF, u) = pq.Min;
            pq.Remove(pq.Min);

            if (u == end)
                return (hopsTo[end], Mathf.Exp(bestLogFid[end]));

            if (!adj.ContainsKey(u)) continue;

            foreach (var (v, li) in adj[u])
            {
                // ข้าม node ที่ fail
                if (simFail && v == failNode) continue;
                if (cascadeFailedNodes != null && cascadeFailedNodes.Contains(v)) continue;

                float lf    = LinkFid(li);
                if (lf <= 0f) continue;
                float newLF = bestLogFid[u] + Mathf.Log(lf);

                if (newLF > bestLogFid[v])
                {
                    pq.Remove((- bestLogFid[v], v));
                    bestLogFid[v] = newLF;
                    hopsTo[v]     = hopsTo[u] + 1;
                    pq.Add((-newLF, v));
                }
            }
        }

        // ไม่พบเส้นทาง (network partitioned) — คืน -1 เป็น flag
        return (-1, 0f);
    }

    // ─── Metrics ─────────────────────────────────────────
    public MetricsData GetMetrics()
    {
        int L = builder.links.Count;
        int hops;
        string fault;

        // fault tolerance ตาม topology
        switch (currentTopo)
        {
            case "linear": fault = "ต่ำ";  break;
            case "star":   fault = "กลาง"; break;
            case "mesh":   fault = "สูง";  break;
            case "tree":   fault = "กลาง"; break;
            case "ring":   fault = "ดี";   break;
            default:       fault = "ดี";   break;
        }

        // Quantum Routing — หา path fidelity สูงสุด จาก Alice → Bob
        // QuantumDijkstra จะข้าม failNode และ cascadeFailedNodes อัตโนมัติ
        int aliceIdx = builder.nodeDataList.FindIndex(d => d.label == "Alice");
        int bobIdx   = builder.nodeDataList.FindIndex(d => d.label == "Bob");

        float bestPathFid;
        if (aliceIdx >= 0 && bobIdx >= 0)
            (hops, bestPathFid) = QuantumDijkstra(aliceIdx, bobIdx);
        else
        {
            hops        = Mathf.Max(1, nodeCount - 1);
            bestPathFid = Mathf.Pow(fidelity / 100f, hops);
        }

        // Network Partitioned — ไม่มีเส้นทาง Alice → Bob
        bool partitioned = (hops == -1);
        if (partitioned)
        {
            return new MetricsData
            {
                hops         = -1,   // flag = partitioned
                links        = L,
                fidelity     = 0,
                distKm       = 0,
                fault        = fault,
                qber         = 0.5f, // max QBER
                entangleRate = 0f,
            };
        }

        int baseHops = hops;

        int   linkCount      = Mathf.Max(1, builder.links.Count);
        float distPerLink    = Mathf.Max(1f, distKm / linkCount);
        float distFactorNorm = Mathf.Exp(-distPerLink / 500f);
        float fidPerLink     = (fidelity / 100f) * distFactorNorm;

        // Baseline — dampen exponent 0.45 แทน 0.6 ไม่ให้ตกหนักเมื่อ node เยอะ
        float fExp   = Mathf.Max(1f, hops * 0.45f);
        int totalFid = simJam
            ? Mathf.Max(20, Mathf.RoundToInt(bestPathFid * 100))
            : Mathf.Max(20, Mathf.RoundToInt(Mathf.Pow(fidPerLink, fExp) * 100));

        // Noise — accumulated product จาก linkFidelities จริง
        if (simJam && linkFidelities.Length > 0)
        {
            var allFailed = new HashSet<int>(cascadeFailedNodes ?? new HashSet<int>());
            if (simFail && failNode >= 0) allFailed.Add(failNode);

            List<int> noisePath = allFailed.Count > 0
                ? BFSPathExcluding(
                    builder.nodeDataList.FindIndex(d => d.label == "Alice"),
                    builder.nodeDataList.FindIndex(d => d.label == "Bob"),
                    allFailed)
                : GetLinksInOrder();
            if (noisePath.Count == 0) noisePath = GetLinksInOrder();

            float acc = 1f;
            foreach (int li in noisePath)
                if (li < linkFidelities.Length)
                    acc *= linkFidelities[li] / 100f;
            totalFid = Mathf.Max(20, Mathf.RoundToInt(acc * 100));
        }

        // Heavy Traffic — geometric mean แทน accumulated product
        // avgLinkFid ^ (hops * 0.5) ป้องกัน compound เกินจริง
        if (simHeavy && linkFidelities.Length > 0)
        {
            var allFailed = new HashSet<int>(cascadeFailedNodes ?? new HashSet<int>());
            if (simFail && failNode >= 0) allFailed.Add(failNode);

            List<int> heavyPath = allFailed.Count > 0
                ? BFSPathExcluding(
                    builder.nodeDataList.FindIndex(d => d.label == "Alice"),
                    builder.nodeDataList.FindIndex(d => d.label == "Bob"),
                    allFailed)
                : GetLinksInOrder();
            if (heavyPath.Count == 0) heavyPath = GetLinksInOrder();

            // geometric mean ของ link fidelities บน path
            float sum = 0f;
            int   cnt = 0;
            foreach (int li in heavyPath)
                if (li < linkFidelities.Length)
                { sum += linkFidelities[li]; cnt++; }

            float avgLinkFid = cnt > 0 ? sum / cnt / 100f : fidPerLink;
            float heavyExp   = Mathf.Max(1f, hops * 0.5f);
            totalFid = Mathf.Max(20, Mathf.RoundToInt(Mathf.Pow(avgLinkFid, heavyExp) * 100));
        }

        // Node Fail / Degrade / Cascade penalties
        if (simFail && failNode >= 0 && hops > baseHops)
            totalFid = Mathf.Max(20, totalFid - 5);
        if (simDegrade)
            totalFid = Mathf.Max(20, totalFid - 15);
        if (simCascade && cascadeFailedNodes.Count > 0)
            totalFid = Mathf.Max(20, totalFid - cascadeFailedNodes.Count * 5);

        int totalDistKm = Mathf.RoundToInt(distKm * baseHops);

        // QBER = (1 - F) / 2
        float qber = (1f - totalFid / 100f) / 2f;

        // E-Rate — ใช้ totalFid เสมอ หาร 1.5^hops
        float R0   = 1000f;
        float rate = R0 * Mathf.Pow(totalFid / 100f, baseHops)
                       / Mathf.Pow(1.5f, baseHops);

        return new MetricsData
        {
            hops            = hops,
            links           = L,
            fidelity        = totalFid,
            distKm          = totalDistKm,
            fault           = fault,
            qber            = qber,
            entangleRate    = rate,
        };
    }
}

public class MetricsData
{
    public int    hops;          // -1 = network partitioned
    public int    links;
    public int    fidelity;
    public int    distKm;
    public string fault;
    public float  qber;          // Quantum Bit Error Rate (0–0.5)
    public float  entangleRate;  // Entanglement Generation Rate (ebit/s)
    public bool   partitioned => hops == -1;  // shorthand
}