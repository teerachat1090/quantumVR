using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages circuit history with cached Bloch states for smooth undo/redo
/// No more replaying from |0⟩ every time!
/// </summary>
public class CircuitHistory
{
    [System.Serializable]
    public class GateOp
    {
        public int socketIndex;
        public string gateName;
        public string socketName;
        public string gateDescription;
        
        public GateOp(GateData data)
        {
            socketIndex = data.socketIndex;
            gateName = data.gateName;
            socketName = data.socketName;
            gateDescription = data.gateDescription;
        }
    }
    
    // Core state tracking
    private List<GateOp> gates = new List<GateOp>();
    private List<Vector3> states = new List<Vector3>(); // Unit vectors on Bloch sphere
    
    // Initial state (always |0⟩ in your system)
    private Vector3 initialState = Vector3.up;
    
    public CircuitHistory()
    {
        Reset();
    }
    
    /// <summary>
    /// Reset to clean slate: gates=[], states=[|0⟩]
    /// </summary>
    public void Reset()
    {
        gates.Clear();
        states.Clear();
        states.Add(initialState);
    }
    
    /// <summary>
    /// Get current state (after all gates)
    /// </summary>
    public Vector3 GetCurrentState()
    {
        return states[states.Count - 1];
    }
    
    /// <summary>
    /// Get state at specific index (0 = initial, 1 = after gate[0], etc.)
    /// </summary>
    public Vector3 GetStateAt(int index)
    {
        if (index < 0 || index >= states.Count)
            return initialState;
        return states[index];
    }
    
    /// <summary>
    /// Total number of gates
    /// </summary>
    public int GateCount => gates.Count;
    
    /// <summary>
    /// Get gate at index
    /// </summary>
    public GateOp GetGate(int index)
    {
        if (index < 0 || index >= gates.Count)
            return null;
        return gates[index];
    }
    
    /// <summary>
    /// Get all gates (read-only)
    /// </summary>
    public IReadOnlyList<GateOp> GetAllGates() => gates.AsReadOnly();
    
    /// <summary>
    /// Add gate to end → compute new state
    /// </summary>
    public Vector3 AddGate(GateData gateData)
    {
        GateOp op = new GateOp(gateData);
        gates.Add(op);
        
        // Compute next state
        Vector3 prevState = states[states.Count - 1];
        Vector3 nextState = ApplyGateToState(op.gateName, prevState);
        states.Add(nextState);
        
        return nextState;
    }
    
    /// <summary>
    /// Remove gate at index → recompute affected states
    /// Returns the new final state after removal
    /// </summary>
    public Vector3 RemoveGate(int index)
    {
        if (index < 0 || index >= gates.Count)
        {
            Debug.LogWarning($"Invalid gate index {index}");
            return GetCurrentState();
        }
        
        gates.RemoveAt(index);
        
        // Trim states array to match
        // states.Count should always be gates.Count + 1
        if (states.Count > gates.Count + 1)
            states.RemoveRange(gates.Count + 1, states.Count - gates.Count - 1);
        
        // Recompute from index onwards
        RecomputeStatesFrom(index);
        
        return GetCurrentState();
    }
    
    /// <summary>
    /// Rebuild entire history from new gate list (fallback for complex changes)
    /// </summary>
    public Vector3 RebuildFrom(List<GateData> newCircuitData)
    {
        gates.Clear();
        states.Clear();
        states.Add(initialState);
        
        foreach (var gateData in newCircuitData)
        {
            GateOp op = new GateOp(gateData);
            gates.Add(op);
            
            Vector3 prevState = states[states.Count - 1];
            Vector3 nextState = ApplyGateToState(op.gateName, prevState);
            states.Add(nextState);
        }
        
        return GetCurrentState();
    }
    
    /// <summary>
    /// Recompute states from startIndex to end
    /// </summary>
    private void RecomputeStatesFrom(int startIndex)
    {
        // startIndex is the first gate we need to reapply
        // State before it is states[startIndex]
        
        for (int i = startIndex; i < gates.Count; i++)
        {
            Vector3 prevState = states[i];
            Vector3 nextState = ApplyGateToState(gates[i].gateName, prevState);
            
            if (i + 1 < states.Count)
                states[i + 1] = nextState;
            else
                states.Add(nextState);
        }
        
        // Trim any extra states
        if (states.Count > gates.Count + 1)
            states.RemoveRange(gates.Count + 1, states.Count - gates.Count - 1);
    }
    
    /// <summary>
    /// Apply gate rotation to a Bloch state (unit vector)
    /// This is the CORE physics: gate = rotation on Bloch sphere
    /// </summary>
    private Vector3 ApplyGateToState(string gateName, Vector3 currentState)
    {
        Vector3 axis;
        float angleDeg;
        string label;
        
        // Use your existing GateRotationLibrary
        if (!GateRotationLibrary.TryGetRotation(gateName, out axis, out angleDeg, out label))
        {
            Debug.LogWarning($"Unknown gate '{gateName}' - state unchanged");
            return currentState;
        }
        
        // Rotate current state around axis by angle
        Quaternion rotation = Quaternion.AngleAxis(-angleDeg, axis);
        Vector3 newState = rotation * currentState.normalized;
        
        return newState.normalized;
    }
    
    /// <summary>
    /// Debug: Print entire history
    /// </summary>
    public void DebugPrint()
    {
        Debug.Log($"=== Circuit History ===");
        Debug.Log($"Gates: {gates.Count}, States: {states.Count}");
        Debug.Log($"State[0] (initial): {states[0]}");
        
        for (int i = 0; i < gates.Count; i++)
        {
            Debug.Log($"Gate[{i}]: {gates[i].gateName} → State[{i+1}]: {states[i+1]}");
        }
        
        Debug.Log($"Current state: {GetCurrentState()}");
    }
}