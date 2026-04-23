using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solves for the shortest gate sequence that transforms |0⟩ to a target Bloch vector.
/// Uses BFS over gate combinations up to maxDepth gates.
/// </summary>
public static class GateSolver
{
    // ---------- Gate definitions (axis-angle on Bloch sphere) ----------
    private static readonly Dictionary<string, (Vector3 axis, float angle)> GateInfo =
        new Dictionary<string, (Vector3, float)>
        {
            { "H",  ((Vector3.right + Vector3.up).normalized, 180f) },
            { "X",  (Vector3.right,   180f) },
            { "Y",  (Vector3.forward, 180f) },
            { "Z",  (Vector3.up,      180f) },
            { "S",  (Vector3.up,       90f) },
            { "T",  (Vector3.up,       45f) },
            { "ST", (Vector3.up,      -90f) },
            { "TT", (Vector3.up,      -45f) },
        };

    // Gate set used for search (keep small for BFS speed)
    private static readonly string[] SearchGates = { "H", "X", "Y", "Z", "S", "T" };

    // ---------- Public API ----------

    /// <summary>
    /// Find the shortest gate sequence (up to maxDepth) that maps |0⟩ → targetVec.
    /// Returns null if not found within depth limit.
    /// </summary>
    public static List<string> Solve(Vector3 targetVec, int maxDepth = 3, float threshold = 0.02f)
    {
        targetVec = targetVec.normalized;

        // BFS queue: (currentVec, gateSequence)
        var queue = new Queue<(Vector3 vec, List<string> seq)>();
        queue.Enqueue((Vector3.up, new List<string>())); // |0⟩ = (0,0,1) = Vector3.up

        while (queue.Count > 0)
        {
            var (vec, seq) = queue.Dequeue();

            // Check if current vec matches target
            if (IsMatch(vec, targetVec, threshold))
                return seq;

            // Stop expanding beyond maxDepth
            if (seq.Count >= maxDepth)
                continue;

            // Expand: try each gate
            foreach (string gate in SearchGates)
            {
                Vector3 newVec = ApplyGate(vec, gate);
                var newSeq = new List<string>(seq) { gate };
                queue.Enqueue((newVec, newSeq));
            }
        }

        // Fallback: not found in BFS → return closest greedy result
        Debug.LogWarning($"[GateSolver] Exact solution not found within depth {maxDepth}. Returning greedy result.");
        return GreedySolve(targetVec, maxDepth);
    }

    /// <summary>
    /// Greedy fallback: at each step pick the gate that brings vec closest to target.
    /// </summary>
    public static List<string> GreedySolve(Vector3 targetVec, int maxDepth = 3)
    {
        targetVec = targetVec.normalized;
        Vector3 current = Vector3.up;
        var result = new List<string>();

        for (int i = 0; i < maxDepth; i++)
        {
            if (IsMatch(current, targetVec, 0.02f)) break;

            string best = null;
            float bestDot = -2f;

            foreach (string gate in SearchGates)
            {
                Vector3 candidate = ApplyGate(current, gate);
                float d = Vector3.Dot(candidate, targetVec);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = gate;
                }
            }

            if (best == null) break;
            result.Add(best);
            current = ApplyGate(current, best);
        }

        return result;
    }

    // ---------- Helpers ----------

    public static Vector3 ApplyGate(Vector3 vec, string gate)
    {
        if (!GateInfo.TryGetValue(gate, out var info)) return vec;
        return Quaternion.AngleAxis(info.angle, info.axis) * vec;
    }

    public static Vector3 ApplySequence(List<string> gates)
    {
        Vector3 v = Vector3.up;
        foreach (string g in gates)
            v = ApplyGate(v, g);
        return v;
    }

    public static bool IsMatch(Vector3 a, Vector3 b, float threshold = 0.02f)
    {
        // dot product close to 1 → angle close to 0
        return Vector3.Dot(a.normalized, b.normalized) >= 1f - threshold;
    }

    /// <summary>
    /// Verify a sequence actually reaches the target (for debug/testing).
    /// </summary>
    public static float GetAngleError(List<string> seq, Vector3 targetVec)
    {
        Vector3 result = ApplySequence(seq);
        float dot = Mathf.Clamp(Vector3.Dot(result.normalized, targetVec.normalized), -1f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }
}