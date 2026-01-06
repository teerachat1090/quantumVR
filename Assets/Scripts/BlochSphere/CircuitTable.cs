using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
//using UnityEditor;
//using System.Data.Common;

public class CircuitTable : MonoBehaviour
{
    [Header("Sockets")]
    public CircuitSocket[] sockets;

    [Header("Display Settings")]
    public TextMeshProUGUI displayText;
    public float delayBetweenGates = 0.5f;

    [Header("Bloch Sphere Animation")]
    [SerializeField] private BlochSphere blochSphere;
    [SerializeField] private float gateAnimationDelay = 1.6f;

    [Header("Circuit Data")]
    public List<GateData> circuitData = new List<GateData>();

    [Header("History System")]
    [SerializeField] private bool enableSmartUndo = true;
    [Tooltip("Animate smoothly when undoing last gate")]
    [SerializeField] private bool animateUndo = true;

    // Core state management
    private CircuitHistory history = new CircuitHistory();
    private bool isExecuting = false;
    private bool pendingRebuild = false;

    // Preview cursor (0..GateCount) points into history.states
    private int previewStateIndex = -1;

    void Start()
    {
        if (sockets == null || sockets.Length == 0)
        {
            sockets = GetComponentsInChildren<CircuitSocket>();
            Debug.Log($"📊 Found {sockets.Length} sockets");
        }

        System.Array.Sort(sockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));

        if (blochSphere == null)
        {
            blochSphere = FindFirstObjectByType<BlochSphere>();
            if (blochSphere != null) Debug.Log("✅ Found BlochSphere automatically!");
        }

        UpdateCircuit();
    }

    private void SyncPreviewCursorToEnd()
    {
        previewStateIndex = history.GateCount;
    }

    /// <summary>
    /// CORE: Called when circuit changes (gate added/removed)
    /// Now uses history system for smart updates!
    /// </summary>
public void UpdateCircuit()
{
    // 1) snapshot ของ circuit เดิม
    var oldData = new List<GateData>(circuitData);

    // 2) build circuit ใหม่จาก sockets
    List<GateData> newCircuitData = new List<GateData>();
    foreach (CircuitSocket socket in sockets)
    {
        if (socket.HasGate())
        {
            newCircuitData.Add(new GateData
            {
                socketIndex = socket.socketIndex,
                socketName = socket.socketName,
                gateName = socket.currentGate.gateName,
                gateDescription = socket.currentGate.gateDescription
            });
        }
    }

    // 3) detect change
    ChangeType change = DetectChange(circuitData, newCircuitData);

    // 4) update circuitData
    circuitData = newCircuitData;

    Debug.Log($"📊 Circuit updated: {circuitData.Count} gates, Change: {change}");

    // 5) handle change (ครั้งเดียว)
    if (change == ChangeType.NoChange)
    {
        return;
    }
    else if (change == ChangeType.EmptyCircuit)
    {
        HandleEmptyCircuit();
    }
    else if (enableSmartUndo && change == ChangeType.RemovedLast)
    {
        // เกตที่ถูกถอด = ตัวสุดท้ายของ oldData
        var removedGate = oldData[oldData.Count - 1];
        HandleRemovedLast(removedGate.gateName);
    }
    else if (enableSmartUndo && change == ChangeType.AddedToEnd)
    {
        HandleAddedToEnd();
    }
    else
    {
        HandleComplexChange();
    }

    // 6) sync preview cursor
    SyncPreviewCursorToEnd();
}


    private enum ChangeType
    {
        NoChange,
        EmptyCircuit,
        AddedToEnd,
        RemovedLast,
        ComplexChange
    }

    private ChangeType DetectChange(List<GateData> oldData, List<GateData> newData)
    {
        if (newData.Count == 0)
            return ChangeType.EmptyCircuit;

        if (oldData.Count == newData.Count)
        {
            bool same = true;
            for (int i = 0; i < oldData.Count; i++)
            {
                if (oldData[i].socketIndex != newData[i].socketIndex ||
                    oldData[i].gateName != newData[i].gateName)
                {
                    same = false;
                    break;
                }
            }
            return same ? ChangeType.NoChange : ChangeType.ComplexChange;
        }

        if (newData.Count == oldData.Count + 1)
        {
            bool addedToEnd = true;
            for (int i = 0; i < oldData.Count; i++)
            {
                if (oldData[i].socketIndex != newData[i].socketIndex ||
                    oldData[i].gateName != newData[i].gateName)
                {
                    addedToEnd = false;
                    break;
                }
            }
            return addedToEnd ? ChangeType.AddedToEnd : ChangeType.ComplexChange;
        }

        if (newData.Count == oldData.Count - 1)
        {
            bool removedLast = true;
            for (int i = 0; i < newData.Count; i++)
            {
                if (oldData[i].socketIndex != newData[i].socketIndex ||
                    oldData[i].gateName != newData[i].gateName)
                {
                    removedLast = false;
                    break;
                }
            }
            return removedLast ? ChangeType.RemovedLast : ChangeType.ComplexChange;
        }

        return ChangeType.ComplexChange;
    }

    private void HandleEmptyCircuit()
    {
        StopAllCoroutines();
        isExecuting = false;
        pendingRebuild = false;

        history.Reset();

        if (blochSphere != null)
        {
            Debug.Log("🧹 No gates left → Reset Bloch Sphere to |0⟩");
            blochSphere.ResetToZero();
        }

        SyncPreviewCursorToEnd();
    }

    private void HandleRemovedLast(string removedGateName)
    {
        if (isExecuting) { pendingRebuild = true; return; }

        int lastIndex = history.GateCount - 1;
        if (lastIndex < 0) return;

        Vector3 previousState = history.RemoveGate(lastIndex);

        // ✅ หาค่าแกน/มุมของเกตที่ถูกถอด
        if (blochSphere != null && animateUndo &&
            GateRotationLibrary.TryGetRotation(removedGateName, out Vector3 axis, out float angleDeg, out _))
        {
            // ApplyGate ใช้ usedAngle = -angleDeg
            // Undo ต้องเป็น inverse => -usedAngle = +angleDeg
            float undoUsedAngle = +angleDeg;

            blochSphere.AnimateToStateWithAxis(previousState, axis, undoUsedAngle);
        }
        else if (blochSphere != null && animateUndo)
        {
            // fallback: ยังไงก็ย้อนตำแหน่งถูก แต่จะลัดทาง
            blochSphere.AnimateToStateDirectly(previousState);
        }
        else if (blochSphere != null)
        {
            blochSphere.SetStateDirectly(previousState);
        }

        SyncPreviewCursorToEnd();
    }


    private void HandleAddedToEnd()
    {
        if (isExecuting)
        {
            pendingRebuild = true;
            return;
        }

        GateData lastGate = circuitData[circuitData.Count - 1];

        Vector3 newState = history.AddGate(lastGate);

        Debug.Log($"➡️ Added gate '{lastGate.gateName}' → New state: {newState}");

        if (blochSphere != null)
        {
            blochSphere.ApplyGate(lastGate.gateName);
        }

        SyncPreviewCursorToEnd();
    }

    private void HandleComplexChange()
    {
        if (isExecuting)
        {
            pendingRebuild = true;
            return;
        }

        Debug.Log("🔄 Complex circuit change → Rebuilding history");

        Vector3 finalState = history.RebuildFrom(circuitData);

        if (blochSphere != null)
        {
            blochSphere.AnimateToStateDirectly(finalState);
        }

        SyncPreviewCursorToEnd();
    }

    public void ExecuteCircuit()
    {
        if (isExecuting)
        {
            Debug.LogWarning("⚠️ Circuit is already executing!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ExecuteCircuitSequence());
    }

    IEnumerator ExecuteCircuitSequence()
    {
        isExecuting = true;
        Debug.Log("🚀 Start circuit execution");

        if (blochSphere != null)
        {
            Debug.Log("🔄 Resetting Bloch Sphere to |0⟩");
            blochSphere.ResetToZero();
            yield return new WaitForSeconds(1.0f);
        }

        List<GateData> gatesSnapshot = new List<GateData>(circuitData);

        if (gatesSnapshot.Count == 0)
        {
            Debug.LogWarning("⚠️ No gates to execute!");
            isExecuting = false;
            yield break;
        }

        if (displayText != null)
        {
            displayText.text = $"Executing {gatesSnapshot.Count} gates...";
        }

        for (int i = 0; i < gatesSnapshot.Count; i++)
        {
            var gate = gatesSnapshot[i];
            Debug.Log($"▶ Gate {i + 1}/{gatesSnapshot.Count}: {gate.gateName}");

            if (displayText != null)
            {
                displayText.text = $"Gate {i + 1}/{gatesSnapshot.Count}: {gate.gateName}";
            }

            if (blochSphere != null)
            {
                blochSphere.ApplyGate(gate.gateName);
            }

            yield return new WaitForSeconds(gateAnimationDelay);
        }

        Debug.Log("✅ All gates applied → ready for measurement");

        if (displayText != null)
        {
            displayText.text = "Circuit executed!\nReady for measurement";
        }

        isExecuting = false;

        if (pendingRebuild)
        {
            pendingRebuild = false;
            Debug.Log("🔁 pendingRebuild detected → re-executing latest circuit");
            ExecuteCircuit();
        }

        SyncPreviewCursorToEnd();
    }

    public void PreviewUndoStep()
    {
        if (isExecuting) return;

        if (previewStateIndex < 0) previewStateIndex = history.GateCount;
        if (previewStateIndex <= 0) return; // already at |0⟩

        previewStateIndex--;

        Vector3 state = history.GetStateAt(previewStateIndex);
        if (blochSphere != null) blochSphere.AnimateToStateDirectly(state);
    }

    public void PreviewRedoStep()
    {
        if (isExecuting) return;

        if (previewStateIndex < 0) previewStateIndex = history.GateCount;
        if (previewStateIndex >= history.GateCount) return; // already at latest

        previewStateIndex++;

        Vector3 state = history.GetStateAt(previewStateIndex);
        if (blochSphere != null) blochSphere.AnimateToStateDirectly(state);
    }

    public bool IsExecuting() => isExecuting;

    public Vector3 GetCurrentBlochState() => history.GetCurrentState();

    public string GenerateCircuitJSON()
    {
        Debug.Log("🔵 GenerateCircuitJSON called!");
        Debug.Log($"📊 Gates count: {circuitData.Count}");

        CircuitExport export = new CircuitExport
        {
            num_qubits = 1, // ✅ โต๊ะ 1 แถว = 1 qubit
            gates = new List<GateExport>()
        };

        foreach (GateData gate in circuitData)
        {
            export.gates.Add(new GateExport
            {
                gate_type = gate.gateName,
                qubit = 0,          // ✅ ทุกเกตอยู่ qubit 0
                target_qubit = -1
            });
        }

        string json = JsonUtility.ToJson(export, true);
        Debug.Log($"📤 Generated JSON:\n{json}");
        return json;
    }


}

[System.Serializable]
public class GateData
{
    public int socketIndex;
    public string socketName;
    public string gateName;
    public string gateDescription;
}

[System.Serializable]
public class CircuitExport
{
    public int num_qubits;
    public List<GateExport> gates;
}

[System.Serializable]
public class GateExport
{
    public string gate_type;
    public int qubit;
    public int target_qubit;
}
