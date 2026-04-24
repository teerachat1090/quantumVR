// QuantumNetworkLogic.cs
// Pure C# logic extracted from GraphManager — ไม่ depend on Unity MonoBehaviour
// ใช้สำหรับ unit test ใน EditMode
using System;
using System.Collections.Generic;

public static class QuantumNetworkLogic
{
    // ── โครงสร้างข้อมูลเดียวกับ TopologyBuilder ──────────
    public struct LinkPair { public int a, b; }
    public struct NodeData  { public string label; }

    // ── Noise per-link (แก้ bug swapAccumulated แล้ว) ────
    public static float CalcLinkFidelity(
        float baseFid,        // 0–1
        float noiseStrength,  // 0–0.3
        float noiseFactor,    // 0–1 (pre-computed หรือ fixed สำหรับ test)
        float distFactor,     // 0–1
        float F_swap = 0.98f,
        float F_gate = 0.99f)
    {
        if (noiseStrength <= 0f) return baseFid * 100f;
        float raw = baseFid * noiseFactor * F_swap * F_gate * distFactor * 100f;
        return Math.Max(baseFid * 0.3f * 100f, Math.Min(raw, baseFid * 100f));
    }

    // ── DistFactor (fiber attenuation) ───────────────────
    public static float CalcDistFactor(float distKm, int linkCount, float L_att = 100f)
    {
        float distPerLink = Math.Max(1f, distKm / Math.Max(1, linkCount));
        return (float)Math.Exp(-distPerLink / L_att);
    }

    // ── Accumulated fidelity จาก link list ───────────────
    public static float AccumulateFidelity(float[] linkFidelities, List<int> path)
    {
        float acc = 1f;
        foreach (int li in path)
            if (li < linkFidelities.Length)
                acc *= linkFidelities[li] / 100f;
        return acc;
    }

    // ── BFS path หา link indices จาก start → end ─────────
    public static List<int> BFSPath(
        int start, int end,
        List<LinkPair> links,
        HashSet<int> failed = null)
    {
        if (start < 0 || end < 0) return new List<int>();
        if (start == end) return new List<int>();

        var adj = BuildAdj(links);
        var prev    = new Dictionary<int, (int node, int link)>();
        var visited = new HashSet<int>(failed ?? new HashSet<int>()) { start };
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
        int c = end;
        while (prev.ContainsKey(c))
        {
            var (pn, li) = prev[c];
            path.Add(li);
            c = pn;
        }
        path.Reverse();
        return path;
    }

    // ── Dijkstra maximize fidelity สะสม ──────────────────
    public static (int hops, float bestFid) QuantumDijkstra(
        int start, int end,
        int nodeCount,
        List<LinkPair> links,
        float[] linkFidelities,   // null = ใช้ uniformFid
        float uniformFid,         // 0–1 ใช้เมื่อ linkFidelities == null
        HashSet<int> failedNodes = null)
    {
        if (start < 0 || end < 0) return (Math.Max(1, nodeCount - 1), 0f);
        if (start == end) return (0, 1f);

        int n   = nodeCount;
        var adj = BuildAdj(links);

        float LinkFid(int li)
        {
            if (linkFidelities != null && li < linkFidelities.Length)
                return linkFidelities[li] / 100f;
            return uniformFid;
        }

        var bestLogFid = new float[n];
        var hopsTo     = new int[n];
        for (int i = 0; i < n; i++) bestLogFid[i] = float.NegativeInfinity;
        bestLogFid[start] = 0f;

        var pq = new SortedSet<(float neg, int node)>(
            Comparer<(float, int)>.Create((x, y) =>
                x.Item1 != y.Item1 ? x.Item1.CompareTo(y.Item1) : x.Item2.CompareTo(y.Item2)));
        pq.Add((0f, start));

        while (pq.Count > 0)
        {
            var (_, u) = pq.Min;
            pq.Remove(pq.Min);

            if (u == end)
                return (hopsTo[end], (float)Math.Exp(bestLogFid[end]));

            if (!adj.ContainsKey(u)) continue;
            foreach (var (v, li) in adj[u])
            {
                if (failedNodes != null && failedNodes.Contains(v)) continue;
                float lf = LinkFid(li);
                if (lf <= 0f) continue;
                float newLF = bestLogFid[u] + (float)Math.Log(lf);
                if (newLF > bestLogFid[v])
                {
                    pq.Remove((-bestLogFid[v], v));
                    bestLogFid[v] = newLF;
                    hopsTo[v]     = hopsTo[u] + 1;
                    pq.Add((-newLF, v));
                }
            }
        }
        return (-1, 0f);
    }

    // ── Helper ────────────────────────────────────────────
    static Dictionary<int, List<(int nb, int li)>> BuildAdj(List<LinkPair> links)
    {
        var adj = new Dictionary<int, List<(int, int)>>();
        for (int i = 0; i < links.Count; i++)
        {
            var lp = links[i];
            if (!adj.ContainsKey(lp.a)) adj[lp.a] = new List<(int, int)>();
            if (!adj.ContainsKey(lp.b)) adj[lp.b] = new List<(int, int)>();
            adj[lp.a].Add((lp.b, i));
            adj[lp.b].Add((lp.a, i));
        }
        return adj;
    }
}
