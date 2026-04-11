using UnityEngine;
using System.Collections.Generic;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    private List<LinkFlowEffect> effects = new List<LinkFlowEffect>();

    void Awake() { Instance = this; }

    // ── Register / Clear ──────────────────────────────────
    public void Register(LinkFlowEffect effect)
    {
        if (!effects.Contains(effect)) effects.Add(effect);
    }

    public void Clear() { effects.Clear(); }

    // ── Global Controls ───────────────────────────────────
    public void SetFidelity(float fidelity)
    {
        foreach (var e in effects) if (e != null) e.SetFidelity(fidelity);
    }

    public void SetFlowEnabled(bool enabled)
    {
        foreach (var e in effects) if (e != null) e.SetFlowEnabled(enabled);
    }

    public void RefreshAll(float fidelity)
    {
        foreach (var e in effects)
            if (e != null) { e.RefreshPathAndRespawn(); e.SetFidelity(fidelity); }
    }

    // ── Noise — ส่ง fidelity ต่าง hop ให้แต่ละ link ────────
    public void SetNoisyFidelities(float[] linkFidelities)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null) continue;
            float f = (i < linkFidelities.Length) ? linkFidelities[i] : 90f;
            // set fidelity เสมอ แม้ flow disabled — เพื่อให้ currentFidelity อัปเดต
            // เมื่อ re-enable จะได้ restore สีถูกต้อง
            effects[i].SetFidelity(f);
        }
    }

    // ── Heavy Traffic ─────────────────────────────────────
    public void SetHeavyTraffic(bool heavy)
    {
        float multiplier = heavy ? 0.3f : 1.0f;
        foreach (var e in effects)
            if (e != null) e.SetSpeedMultiplier(multiplier);
    }

    // ── Link Degrade ──────────────────────────────────────
    public void RefreshDegradedLinks(HashSet<int> degradedLinks)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null) continue;
            bool deg = degradedLinks != null && degradedLinks.Contains(i);
            effects[i].SetDegraded(deg);
        }
    }

    // ── Node Fail + Cascade — topology-aware ─────────────
    public void RefreshFailedLinks(int failNode,
        HashSet<int> cascadeFailedNodes, TopologyBuilder builder)
    {
        if (effects.Count == 0) return;

        string topo  = GraphManager.Instance.currentTopo;
        int    n     = GraphManager.Instance.nodeCount;

        // รวม failNode และ cascadeFailedNodes
        var allFailed = new HashSet<int>(cascadeFailedNodes ?? new HashSet<int>());
        if (failNode >= 0) allFailed.Add(failNode);

        var stopLinks = new HashSet<int>();

        if (allFailed.Count > 0)
        {
            switch (topo)
            {
                case "linear": stopLinks = CalcLinear(allFailed, builder, n); break;
                case "star":   stopLinks = CalcStar(allFailed, builder, n);   break;
                case "tree":   stopLinks = CalcTree(allFailed, builder, n);   break;
                case "ring":   stopLinks = CalcRing(allFailed, builder, n);   break;
                case "mesh":   stopLinks = CalcMesh(allFailed, builder, n);   break;
            }
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null) continue;
            effects[i].SetFlowEnabled(!stopLinks.Contains(i));
        }
    }

    // ─── Helper: หา Bob index ─────────────────────────────
    // Bob = node สุดท้ายของ topology นั้นๆ
    int GetBobIndex(string topo, int n)
    {
        switch (topo)
        {
            case "star":  return 2;                      // Bob = leaf index 2
            case "ring":  return Mathf.CeilToInt(n/2f); // Bob = ตรงข้าม Alice
            case "tree":  return n - 1;                  // Bob = node สุดท้าย
            default:      return n - 1;                  // linear, mesh
        }
    }

    // ─── Helper: หา Alice index ───────────────────────────
    int GetAliceIndex(string topo)
    {
        switch (topo)
        {
            case "star": return 1;  // Alice = leaf index 1
            default:     return 0;  // linear, mesh, ring, tree
        }
    }

    // ─── Helper: หา link index ที่ติดกับ node ────────────
    HashSet<int> GetLinksConnectedTo(int node, TopologyBuilder builder)
    {
        var result = new HashSet<int>();
        for (int i = 0; i < builder.links.Count; i++)
        {
            var (a, b) = builder.links[i];
            if (a == node || b == node) result.Add(i);
        }
        return result;
    }

    // ─── Helper: หยุดทุก link ─────────────────────────────
    HashSet<int> StopAll(TopologyBuilder builder)
    {
        var stop = new HashSet<int>();
        for (int i = 0; i < builder.links.Count; i++) stop.Add(i);
        return stop;
    }

    // ════════════════════════════════════════════════════
    //  LINEAR
    //  Alice(0) → R1 → R2 → ... → Bob(n-1)
    //
    //  Alice พัง           → หยุดทุก link
    //  Bob พัง             → หยุดเฉพาะ link ติด Bob
    //  Repeater k พัง      → link ที่ maxNode >= k หยุด
    // ════════════════════════════════════════════════════
    HashSet<int> CalcLinear(HashSet<int> failed, TopologyBuilder builder, int n)
    {
        // Alice พัง
        if (failed.Contains(0)) return StopAll(builder);

        // Bob พัง → หยุดแค่ link ที่ติด Bob
        var stop = new HashSet<int>();
        if (failed.Contains(n - 1))
        {
            stop.UnionWith(GetLinksConnectedTo(n - 1, builder));
            return stop;
        }

        // Repeater พัง → หา failNode ที่ใกล้ Alice ที่สุด
        int firstFail = int.MaxValue;
        foreach (int f in failed)
            if (f < firstFail) firstFail = f;

        for (int i = 0; i < builder.links.Count; i++)
        {
            var (a, b) = builder.links[i];
            if (Mathf.Max(a, b) >= firstFail) stop.Add(i);
        }

        return stop;
    }

    // ════════════════════════════════════════════════════
    //  STAR
    //  Hub(0), Alice(1), Bob(2), Leaf(3+)
    //  Main path: Alice(1) → Hub(0) → Bob(2)
    //
    //  Alice(1) พัง        → หยุดทุก link
    //  Hub(0) พัง          → หยุดทุก link
    //  Bob(2) พัง          → หยุดเฉพาะ link Hub-Bob
    //  Leaf อื่นพัง        → หยุดเฉพาะ link ของ leaf นั้น
    // ════════════════════════════════════════════════════
    HashSet<int> CalcStar(HashSet<int> failed, TopologyBuilder builder, int n)
    {
        // Alice(1) หรือ Hub(0) พัง → หยุดทุก link
        if (failed.Contains(0) || failed.Contains(1))
            return StopAll(builder);

        var stop = new HashSet<int>();

        // Bob(2) พัง → หยุดเฉพาะ link Hub-Bob
        if (failed.Contains(2))
            stop.UnionWith(GetLinksConnectedTo(2, builder));

        // Leaf อื่น (3+) พัง → หยุดเฉพาะ link ของ leaf นั้น
        foreach (int f in failed)
            if (f >= 3) stop.UnionWith(GetLinksConnectedTo(f, builder));

        return stop;
    }

    // ════════════════════════════════════════════════════
    //  TREE
    //  Root(0) อยู่บนสุด, Alice(1), Bob(n-1)
    //
    //  Root(0) พัง         → หยุดทุก link
    //  Alice(1) พัง        → หยุดทุก link
    //  Bob(n-1) พัง        → หยุดเฉพาะ link ติด Bob
    //  Node กลางพัง        → หยุด subtree ใต้ node นั้น
    //  Leaf อื่นพัง        → หยุดเฉพาะ link ของ leaf นั้น
    // ════════════════════════════════════════════════════
    HashSet<int> CalcTree(HashSet<int> failed, TopologyBuilder builder, int n)
    {
        // Root(0) หรือ Alice(1) พัง → หยุดทุก link
        if (failed.Contains(0) || failed.Contains(1))
            return StopAll(builder);

        var stop = new HashSet<int>();

        // Bob(n-1) พัง → หยุดแค่ link ติด Bob
        if (failed.Contains(n - 1))
            stop.UnionWith(GetLinksConnectedTo(n - 1, builder));

        // Node กลางพัง → BFS หา subtree แล้วหยุด link ใน subtree
        var subtree = new HashSet<int>();
        foreach (int f in failed)
            if (f != n - 1) subtree.Add(f); // ยกเว้น Bob ที่จัดการแล้ว

        if (subtree.Count > 0)
        {
            // ขยาย subtree ลงมา (parent index < child index ใน tree เสมอ)
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var (a, b) in builder.links)
                {
                    int parent = Mathf.Min(a, b);
                    int child  = Mathf.Max(a, b);
                    if (subtree.Contains(parent) && !subtree.Contains(child))
                    {
                        subtree.Add(child);
                        changed = true;
                    }
                }
            }

            for (int i = 0; i < builder.links.Count; i++)
            {
                var (a, b) = builder.links[i];
                if (subtree.Contains(a) || subtree.Contains(b))
                    stop.Add(i);
            }
        }

        return stop;
    }

    // ════════════════════════════════════════════════════
    //  RING
    //  Alice(0) และ Bob(n/2) อยู่ตรงข้ามกัน
    //  มี 2 เส้นทาง: clockwise และ counterclockwise
    //
    //  Alice(0) พัง        → หยุดทุก link
    //  Bob พัง             → หยุดเฉพาะ link ติด Bob (ทั้งสองเส้น)
    //  Repeater พัง        → เส้นทางที่ผ่านหยุด อีกเส้นวิ่ง
    //  ทั้งสองเส้นถูกบล็อก → หยุดทุก link
    // ════════════════════════════════════════════════════
    HashSet<int> CalcRing(HashSet<int> failed, TopologyBuilder builder, int n)
    {
        int bobIdx = Mathf.CeilToInt(n / 2f);

        // Alice(0) พัง → หยุดทุก link
        if (failed.Contains(0)) return StopAll(builder);

        var stop = new HashSet<int>();

        // Bob พัง → หยุดเฉพาะ link ที่ติด Bob
        if (failed.Contains(bobIdx))
        {
            stop.UnionWith(GetLinksConnectedTo(bobIdx, builder));
            // ยังเช็คต่อว่า Repeater อื่นพังด้วยไหม
            var otherFailed = new HashSet<int>(failed);
            otherFailed.Remove(bobIdx);
            if (otherFailed.Count > 0)
                stop.UnionWith(CalcRingRepeater(otherFailed, builder, n, bobIdx));
            return stop;
        }

        // Repeater พัง
        stop.UnionWith(CalcRingRepeater(failed, builder, n, bobIdx));
        return stop;
    }

    HashSet<int> CalcRingRepeater(HashSet<int> failed, TopologyBuilder builder, int n, int bobIdx)
    {
        var stop = new HashSet<int>();

        var cwPath  = GetClockwisePath(bobIdx);
        var ccwPath = GetCounterClockwisePath(n, bobIdx);

        bool cwBlocked  = PathBlocked(cwPath,  failed);
        bool ccwBlocked = PathBlocked(ccwPath, failed);

        // ทั้งสองเส้นถูกบล็อก → หยุดทุก link
        if (cwBlocked && ccwBlocked)
            return StopAll(builder);

        for (int i = 0; i < builder.links.Count; i++)
        {
            var (a, b) = builder.links[i];
            if (cwBlocked  && IsOnPath(a, b, cwPath))  stop.Add(i);
            if (ccwBlocked && IsOnPath(a, b, ccwPath)) stop.Add(i);
        }

        return stop;
    }

    List<int> GetClockwisePath(int bobIdx)
    {
        var path = new List<int>();
        for (int i = 0; i <= bobIdx; i++) path.Add(i);
        return path;
    }

    List<int> GetCounterClockwisePath(int n, int bobIdx)
    {
        var path = new List<int>();
        path.Add(0);
        for (int i = n - 1; i >= bobIdx; i--) path.Add(i);
        return path;
    }

    bool PathBlocked(List<int> path, HashSet<int> failed)
    {
        foreach (int node in path)
            if (failed.Contains(node)) return true;
        return false;
    }

    bool IsOnPath(int a, int b, List<int> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
            if ((path[i] == a && path[i+1] == b) || (path[i] == b && path[i+1] == a))
                return true;
        return false;
    }

    // ════════════════════════════════════════════════════
    //  MESH
    //  Alice(0), Bob(n-1), node กลาง reroute ได้
    //
    //  Alice(0) พัง        → หยุดทุก link
    //  Bob(n-1) พัง        → หยุดเฉพาะ link ติด Bob
    //  Node กลางพัง        → หยุด link ติด failNode
    //                        ถ้าไม่มีเส้นทาง Alice→Bob → หยุดทุก link
    // ════════════════════════════════════════════════════
    HashSet<int> CalcMesh(HashSet<int> failed, TopologyBuilder builder, int n)
    {
        // Alice(0) พัง → หยุดทุก link
        if (failed.Contains(0)) return StopAll(builder);

        var stop = new HashSet<int>();

        // Bob(n-1) พัง → หยุดเฉพาะ link ติด Bob
        if (failed.Contains(n - 1))
        {
            stop.UnionWith(GetLinksConnectedTo(n - 1, builder));
            // เช็คต่อว่า node อื่นพังด้วยไหม
            var otherFailed = new HashSet<int>(failed);
            otherFailed.Remove(n - 1);
            if (otherFailed.Count > 0)
            {
                foreach (int f in otherFailed)
                    stop.UnionWith(GetLinksConnectedTo(f, builder));
            }
            return stop;
        }

        // Node กลางพัง → หยุด link ที่ติด failNode
        foreach (int f in failed)
            stop.UnionWith(GetLinksConnectedTo(f, builder));

        // เช็คว่ายังมีเส้นทาง Alice→Bob ไหม
        if (!BFSPathExists(0, n - 1, builder, stop))
            return StopAll(builder);

        return stop;
    }

    bool BFSPathExists(int src, int dst, TopologyBuilder builder, HashSet<int> stopLinks)
    {
        var visited = new HashSet<int>();
        var queue   = new Queue<int>();
        queue.Enqueue(src);
        visited.Add(src);

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            if (cur == dst) return true;

            for (int i = 0; i < builder.links.Count; i++)
            {
                if (stopLinks.Contains(i)) continue;
                var (a, b) = builder.links[i];
                int next = -1;
                if (a == cur && !visited.Contains(b)) next = b;
                if (b == cur && !visited.Contains(a)) next = a;
                if (next >= 0) { visited.Add(next); queue.Enqueue(next); }
            }
        }

        return false;
    }
}