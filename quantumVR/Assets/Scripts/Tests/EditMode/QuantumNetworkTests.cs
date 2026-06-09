// QuantumNetworkTests.cs
// Unity Test Framework — PlayMode
// ครอบคลุม: ทุก Topology (Linear, Star, Mesh, Tree, Ring) x Node Fail + Noise
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[TestFixture]
public class QuantumNetworkTests
{
    static (List<QuantumNetworkLogic.LinkPair> links, int alice, int bob) Linear5()
    {
        var links = new List<QuantumNetworkLogic.LinkPair>
        {
            new QuantumNetworkLogic.LinkPair { a=0, b=1 },
            new QuantumNetworkLogic.LinkPair { a=1, b=2 },
            new QuantumNetworkLogic.LinkPair { a=2, b=3 },
            new QuantumNetworkLogic.LinkPair { a=3, b=4 },
        };
        return (links, 0, 4);
    }

    static (List<QuantumNetworkLogic.LinkPair> links, int alice, int bob) Star5()
    {
        var links = new List<QuantumNetworkLogic.LinkPair>
        {
            new QuantumNetworkLogic.LinkPair { a=0, b=1 },
            new QuantumNetworkLogic.LinkPair { a=0, b=2 },
            new QuantumNetworkLogic.LinkPair { a=0, b=3 },
            new QuantumNetworkLogic.LinkPair { a=0, b=4 },
        };
        return (links, 1, 2);
    }

    static (List<QuantumNetworkLogic.LinkPair> links, int alice, int bob) Mesh6()
    {
        var links = new List<QuantumNetworkLogic.LinkPair>
        {
            new QuantumNetworkLogic.LinkPair { a=0, b=1 },
            new QuantumNetworkLogic.LinkPair { a=1, b=2 },
            new QuantumNetworkLogic.LinkPair { a=0, b=3 },
            new QuantumNetworkLogic.LinkPair { a=1, b=4 },
            new QuantumNetworkLogic.LinkPair { a=2, b=5 },
            new QuantumNetworkLogic.LinkPair { a=3, b=4 },
            new QuantumNetworkLogic.LinkPair { a=4, b=5 },
        };
        return (links, 0, 5);
    }

    static (List<QuantumNetworkLogic.LinkPair> links, int alice, int bob) Tree7()
    {
        var links = new List<QuantumNetworkLogic.LinkPair>
        {
            new QuantumNetworkLogic.LinkPair { a=0, b=1 },
            new QuantumNetworkLogic.LinkPair { a=0, b=2 },
            new QuantumNetworkLogic.LinkPair { a=1, b=3 },
            new QuantumNetworkLogic.LinkPair { a=1, b=4 },
            new QuantumNetworkLogic.LinkPair { a=2, b=5 },
            new QuantumNetworkLogic.LinkPair { a=2, b=6 },
        };
        return (links, 1, 6);
    }

    static (List<QuantumNetworkLogic.LinkPair> links, int alice, int bob) Ring8()
    {
        var links = new List<QuantumNetworkLogic.LinkPair>
        {
            new QuantumNetworkLogic.LinkPair { a=0, b=1 },
            new QuantumNetworkLogic.LinkPair { a=1, b=2 },
            new QuantumNetworkLogic.LinkPair { a=2, b=3 },
            new QuantumNetworkLogic.LinkPair { a=3, b=4 },
            new QuantumNetworkLogic.LinkPair { a=4, b=5 },
            new QuantumNetworkLogic.LinkPair { a=5, b=6 },
            new QuantumNetworkLogic.LinkPair { a=6, b=7 },
            new QuantumNetworkLogic.LinkPair { a=7, b=0 },
        };
        return (links, 0, 4);
    }

    static float[] UniformFids(int count, float value)
    {
        var arr = new float[count];
        for (int i = 0; i < count; i++) arr[i] = value;
        return arr;
    }

    float CalcFid(float baseFid, float noise, float dist = 111f)
    {
        float noiseFactor = Mathf.Clamp(1f - noise, 0f, 1f);
        float distFactor  = Mathf.Exp(-dist / 280f);
        return QuantumNetworkLogic.CalcLinkFidelity(baseFid, noise, noiseFactor, distFactor, 0.98f, 0.99f);
    }

    int FindSafeFailNode(int nodeCount, int aliceIdx, int bobIdx)
    {
        int candidate = nodeCount / 2;
        for (int attempt = 0; attempt < nodeCount; attempt++)
        {
            int idx = (candidate + attempt) % nodeCount;
            if (idx != aliceIdx && idx != bobIdx) return idx;
        }
        return -1;
    }

    // ════════════════════════════════════════════
    // GROUP 1: TOPOLOGY NORMAL
    // ════════════════════════════════════════════

    [Test] public void Topo_Linear_NormalPath_4Hops()
    {
        var (links, a, b) = Linear5();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, null, 0.95f);
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0.8f);
    }

    [Test] public void Topo_Star_NormalPath_2Hops()
    {
        var (links, a, b) = Star5();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, null, 0.95f);
        Assert.AreEqual(2, hops); Assert.Greater(fid, 0.9f);
    }

    [Test] public void Topo_Mesh_NormalPath_ValidHops()
    {
        var (links, a, b) = Mesh6();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 6, links, null, 0.95f);
        Assert.Greater(hops, 0); Assert.Less(hops, 6); Assert.Greater(fid, 0f);
    }

    [Test] public void Topo_Tree_NormalPath_ReachesAliceToBob()
    {
        var (links, a, b) = Tree7();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 7, links, null, 0.95f);
        Assert.Greater(hops, 0); Assert.Greater(fid, 0f);
    }

    [Test] public void Topo_Ring_NormalPath_4Hops()
    {
        var (links, a, b) = Ring8();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, null, 0.95f);
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0f);
    }

    // ════════════════════════════════════════════
    // GROUP 2: NODE FAIL x TOPOLOGY
    // ════════════════════════════════════════════

    [Test] public void NodeFail_Linear_MiddleNodeFail_Partitioned()
    {
        var (links, a, b) = Linear5();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, null, 0.95f, new HashSet<int> { 2 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFail_Linear_LastRepeaterFail_Partitioned()
    {
        var (links, a, b) = Linear5();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, null, 0.95f, new HashSet<int> { 3 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFail_Linear_AliceBobNotFailNode()
    {
        int fn = FindSafeFailNode(5, 0, 4);
        Assert.AreNotEqual(0, fn); Assert.AreNotEqual(4, fn); Assert.GreaterOrEqual(fn, 0);
    }

    [Test] public void NodeFail_Star_HubFail_Partitioned()
    {
        var (links, a, b) = Star5();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, null, 0.95f, new HashSet<int> { 0 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFail_Star_LeafFail_StillReachable()
    {
        var (links, a, b) = Star5();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, null, 0.95f, new HashSet<int> { 3 });
        Assert.AreEqual(2, hops); Assert.Greater(fid, 0f);
    }

    [Test] public void NodeFail_Star_AliceBobNotFailNode()
    {
        int fn = FindSafeFailNode(5, 1, 2);
        Assert.AreNotEqual(1, fn); Assert.AreNotEqual(2, fn);
    }

    [Test] public void NodeFail_Mesh_OneNodeFail_StillReroutes()
    {
        var (links, a, b) = Mesh6();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 6, links, null, 0.95f, new HashSet<int> { 1 });
        Assert.AreNotEqual(-1, hops); Assert.Greater(fid, 0f);
    }

    [Test] public void NodeFail_Mesh_AliceBobNotFailNode()
    {
        int fn = FindSafeFailNode(6, 0, 5);
        Assert.AreNotEqual(0, fn); Assert.AreNotEqual(5, fn);
    }

    [Test] public void NodeFail_Tree_RootFail_Partitioned()
    {
        var (links, a, b) = Tree7();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 7, links, null, 0.95f, new HashSet<int> { 0 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFail_Tree_LeafFail_OtherPathWorks()
    {
        var (links, a, b) = Tree7();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 7, links, null, 0.95f, new HashSet<int> { 3 });
        Assert.AreNotEqual(-1, hops); Assert.Greater(fid, 0f);
    }

    [Test] public void NodeFail_Tree_AliceBobNotFailNode()
    {
        int fn = FindSafeFailNode(7, 1, 6);
        Assert.AreNotEqual(1, fn); Assert.AreNotEqual(6, fn);
    }

    [Test] public void NodeFail_Ring_CWNodeFail_ReroutesCCW()
    {
        var (links, a, b) = Ring8();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, null, 0.95f, new HashSet<int> { 2 });
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0f);
    }

    [Test] public void NodeFail_Ring_CCWNodeFail_ReroutesCW()
    {
        var (links, a, b) = Ring8();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, null, 0.95f, new HashSet<int> { 6 });
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0f);
    }

    [Test] public void NodeFail_Ring_BothPathsBlocked_Partitioned()
    {
        var (links, a, b) = Ring8();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, null, 0.95f, new HashSet<int> { 2, 6 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFail_Ring_AliceBobNotFailNode()
    {
        int fn = FindSafeFailNode(8, 0, 4);
        Assert.AreNotEqual(0, fn); Assert.AreNotEqual(4, fn);
    }

    // ════════════════════════════════════════════
    // GROUP 3: NOISE x TOPOLOGY
    // ════════════════════════════════════════════

    [Test] public void Noise_Linear_AllLinksEqualFidelity()
    {
        float f = CalcFid(0.99f, 0.10f);
        var fids = UniformFids(4, f);
        Assert.AreEqual(fids[0], fids[3], 0.001f);
    }

    [Test] public void Noise_Linear_AccumulatedFidelity_CorrectProduct()
    {
        float f = CalcFid(0.99f, 0.10f);
        float acc = QuantumNetworkLogic.AccumulateFidelity(UniformFids(4, f), new List<int> { 0, 1, 2, 3 });
        Assert.AreEqual(Mathf.Pow(f / 100f, 4), acc, 0.01f);
    }

    [Test] public void Noise_Linear_ZeroNoise_FidelityUnchanged()
    {
        Assert.AreEqual(99f, CalcFid(0.99f, 0f), 0.01f);
    }

    [Test] public void Noise_Star_2HopsAccumulated_Correct()
    {
        float f = CalcFid(0.95f, 0.10f);
        float acc = QuantumNetworkLogic.AccumulateFidelity(UniformFids(4, f), new List<int> { 0, 1 });
        Assert.AreEqual(Mathf.Pow(f / 100f, 2), acc, 0.01f);
    }

    [Test] public void Noise_Mesh_DijkstraChoosesBestPath()
    {
        var (links, a, b) = Mesh6();
        var fids = new float[] { 60f, 60f, 85f, 60f, 60f, 85f, 85f };
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 6, links, fids, 0.8f);
        Assert.Greater(fid, 0.5f);
    }

    [Test] public void Noise_Tree_DijkstraPicksBestBranch()
    {
        var (links, a, b) = Tree7();
        var fids = new float[] { 50f, 85f, 50f, 50f, 85f, 85f };
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 7, links, fids, 0.8f);
        Assert.Greater(fid, 0.5f);
    }

    [Test] public void Noise_Ring_DijkstraChoosesCCW_WhenCCWHigher()
    {
        var (links, a, b) = Ring8();
        var fids = new float[] { 60f, 58f, 55f, 53f, 82f, 81f, 80f, 79f };
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, fids, 0.8f);
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0.35f);
    }

    [Test] public void Noise_Ring_DijkstraChoosesCW_WhenCWHigher()
    {
        var (links, a, b) = Ring8();
        var fids = new float[] { 82f, 81f, 80f, 79f, 60f, 58f, 55f, 53f };
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, fids, 0.8f);
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0.35f);
    }

    [Test] public void Noise_Ring_EqualFidelity_ValidPath()
    {
        var (links, a, b) = Ring8();
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, UniformFids(8, 80f), 0.8f);
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0f);
    }

    // ════════════════════════════════════════════
    // GROUP 4: NODE FAIL + NOISE
    // ════════════════════════════════════════════

    [Test] public void NodeFailAndNoise_Ring_FailCW_RoutesCCW()
    {
        var (links, a, b) = Ring8();
        var fids = new float[] { 75f, 73f, 70f, 68f, 82f, 81f, 80f, 79f };
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, fids, 0.8f, new HashSet<int> { 2 });
        Assert.AreEqual(4, hops); Assert.Greater(fid, 0.3f);
    }

    [Test] public void NodeFailAndNoise_Linear_FailMiddle_Partitioned()
    {
        var (links, a, b) = Linear5();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, UniformFids(4, 80f), 0.8f, new HashSet<int> { 2 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFailAndNoise_Star_HubFail_Partitioned()
    {
        var (links, a, b) = Star5();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 5, links, UniformFids(4, 85f), 0.85f, new HashSet<int> { 0 });
        Assert.AreEqual(-1, hops);
    }

    [Test] public void NodeFailAndNoise_Mesh_Reroute_BestPath()
    {
        var (links, a, b) = Mesh6();
        var fids = new float[] { 70f, 70f, 85f, 70f, 70f, 85f, 85f };
        var (hops, fid) = QuantumNetworkLogic.QuantumDijkstra(a, b, 6, links, fids, 0.8f, new HashSet<int> { 1 });
        Assert.AreNotEqual(-1, hops); Assert.Greater(fid, 0.5f);
    }

    [Test] public void NodeFailAndNoise_Ring_BothPathsBlocked_Partitioned()
    {
        var (links, a, b) = Ring8();
        var (hops, _) = QuantumNetworkLogic.QuantumDijkstra(a, b, 8, links, UniformFids(8, 80f), 0.8f, new HashSet<int> { 2, 6 });
        Assert.AreEqual(-1, hops);
    }

    // ════════════════════════════════════════════
    // GROUP 5: NOISE PROPERTIES
    // ════════════════════════════════════════════

    [Test] public void Noise_HigherNoise_LowerFidelity()
    {
        Assert.Greater(CalcFid(0.99f, 0.05f), CalcFid(0.99f, 0.20f));
    }

    [Test] public void Noise_LongerDistance_LowerFidelity()
    {
        Assert.Greater(CalcFid(0.99f, 0.10f, 50f), CalcFid(0.99f, 0.10f, 400f));
    }

    [Test] public void Noise_ZeroStrength_AllBaseFidelities_Unchanged()
    {
        foreach (float b in new[] { 0.80f, 0.90f, 0.95f, 0.99f })
            Assert.AreEqual(b * 100f, CalcFid(b, 0f), 0.01f);
    }

    [Test] public void Noise_PerLinkIndependent_SameResult3Calls()
    {
        float f1 = CalcFid(0.99f, 0.10f), f2 = CalcFid(0.99f, 0.10f), f3 = CalcFid(0.99f, 0.10f);
        Assert.AreEqual(f1, f2, 0.001f); Assert.AreEqual(f2, f3, 0.001f);
    }

    [Test] public void Noise_Fidelity_NeverBelowFloor()
    {
        float baseFid = 0.99f;
        float result  = QuantumNetworkLogic.CalcLinkFidelity(baseFid, 0.3f, 0.05f, 0.1f);
        Assert.GreaterOrEqual(result, baseFid * 0.3f * 100f);
    }

    // ════════════════════════════════════════════
    // GROUP 6: BFS CORRECTNESS
    // ════════════════════════════════════════════

    [Test] public void BFS_Linear_CorrectLinkOrder()
    {
        var (links, a, b) = Linear5();
        var path = QuantumNetworkLogic.BFSPath(a, b, links);
        Assert.AreEqual(4, path.Count);
        for (int i = 0; i < 4; i++) Assert.AreEqual(i, path[i]);
    }

    [Test] public void BFS_Star_2Links_ThroughHub()
    {
        var (links, a, b) = Star5();
        Assert.AreEqual(2, QuantumNetworkLogic.BFSPath(a, b, links).Count);
    }

    [Test] public void BFS_Ring_4Links()
    {
        var (links, a, b) = Ring8();
        Assert.AreEqual(4, QuantumNetworkLogic.BFSPath(a, b, links).Count);
    }

    [Test] public void BFS_WithFailedNode_AvoidsFailedNode_Ring()
    {
        var (links, a, b) = Ring8();
        var path = QuantumNetworkLogic.BFSPath(a, b, links, new HashSet<int> { 2 });
        foreach (int li in path)
        {
            Assert.AreNotEqual(2, links[li].a);
            Assert.AreNotEqual(2, links[li].b);
        }
    }

    [Test] public void BFS_Partitioned_ReturnsEmpty()
    {
        var (links, a, b) = Linear5();
        foreach (int fn in new[] { 1, 2, 3 })
            Assert.AreEqual(0, QuantumNetworkLogic.BFSPath(a, b, links, new HashSet<int> { fn }).Count);
    }

    [Test] public void BFS_EmptyPath_AccumulateReturnsOne()
    {
        Assert.AreEqual(1f, QuantumNetworkLogic.AccumulateFidelity(new float[] { 80f, 80f }, new List<int>()));
    }

    [Test] public void BFS_DistFactor_ShortDistance_CloseToOne()
    {
        Assert.Greater(QuantumNetworkLogic.CalcDistFactor(10f, 4), 0.95f);
    }

    [Test] public void BFS_DistFactor_LongDistance_LowerThanShort()
    {
        Assert.Greater(QuantumNetworkLogic.CalcDistFactor(50f, 4), QuantumNetworkLogic.CalcDistFactor(400f, 4));
    }
}