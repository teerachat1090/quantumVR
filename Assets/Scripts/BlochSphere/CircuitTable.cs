using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

//using UnityEditor;
//using System.Data.Common;

public class CircuitTable : MonoBehaviour
{
    [Header("Sockets")]
    private CircuitSocket[] sockets; // Array ของ Socket ทั้งหมด (SC1(1) ถึง SC1(10))

    [Header("Display Settings")]
    public TextMeshProUGUI displayText; // Text แสดงผลบนหน้าจอ VR
    public float delayBetweenGates = 0.5f; // Delay ระหว่าง Gate

    [Header("Bloch Sphere Animation")]
    [SerializeField] private BlochSphere blochSphere;
    [SerializeField] private float gateAnimationDelay = 1.6f;

    [Header("Circuit Data")]
    public List<GateData> circuitData = new List<GateData>(); // เก็บข้อมูล Circuit

    [Header("History System")]
    [SerializeField] private bool enableSmartUndo = true;   //changing list using history as ref.
    [Tooltip("Animate smoothly when undoing last gate")]
    [SerializeField] private bool animateUndo = true;
    // Core state management
    private CircuitHistory history = new CircuitHistory();
 
    private bool isExecuting = false; //check if arranging is running
    private bool pendingRebuild = false; // to notice that smth. changed during executing

    // Preview cursor (0..GateCount) points into history.states
    private int previewStateIndex = -1;
  
    void Start()
    {
        // หา Sockets ทั้งหมดที่เป็น child

        if (sockets == null || sockets.Length == 0)
        {
            sockets = GetComponentsInChildren<CircuitSocket>();
            Debug.Log($"📊 Found {sockets.Length} sockets");
        }
        
        Debug.Log($"socket length: {sockets.Length}");
        Debug.Log($"📊 Found {sockets.Length} sockets");

        // sort socket by index
        System.Array.Sort(sockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));

        // introduce Bloch sphere object
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

    // Update circuit when changed + history system
    public void UpdateCircuit()
    {
        // 1) copy from lastest "circuitData" list (need for comparing when update history)
        var oldData = new List<GateData>(circuitData);

        // 2) build a new circuit list
        List<GateData> newCircuitData = new List<GateData>();
        foreach (CircuitSocket socket in sockets)
        {
            if (socket.HasGate())
            {
                newCircuitData.Add(new GateData
                {
                    socketIndex = socket.socketIndex,
                    socketName = socket.socketName,
                    gateName = socket.currentGate.getGateName(),
                    gateDescription = socket.currentGate.getGateDescription()
                });
            }
        }

        // 3) detecting change
        ChangeType change = DetectChange(circuitData, newCircuitData);

        // 4) update circuitData list
        circuitData = newCircuitData;
        Debug.Log($"📊 Circuit updated: {circuitData.Count} gates, Change: {change}");

        // 5) handle change (ครั้งเดียว)
        if (change == ChangeType.NoChange)              
            return;

        else if (change == ChangeType.EmptyCircuit)     
            HandleEmptyCircuit();

        else if (enableSmartUndo && change == ChangeType.RemovedLast)
        {
            //why need gate's name when it always remove the last one?
            var removedGate = oldData[oldData.Count - 1]; 
            HandleRemovedLast(removedGate.gateName);
        }

        else if (enableSmartUndo && change == ChangeType.AddedToEnd)
            HandleAddedToEnd();

        else    
            HandleComplexChange();
        

        // 6) sync preview cursor
        SyncPreviewCursorToEnd();
    }

    //list of status
    private enum ChangeType
    {
        NoChange, EmptyCircuit, AddedToEnd, RemovedLast, ComplexChange
    }

    private ChangeType DetectChange(List<GateData> oldData, List<GateData> newData)
    {
        if (newData.Count == 0)
            return ChangeType.EmptyCircuit;

        if (oldData.Count == newData.Count)
        {
            //check sequence
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

        //insert
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

        //remove
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
        string result_text = null;
        // ✅ หาค่าแกน/มุมของเกตที่ถูกถอด
        if (blochSphere != null && animateUndo &&
            GateRotationLibrary.TryGetRotation(removedGateName, out Vector3 axis, out float angleDeg, out result_text))
        {
            // ApplyGate ใช้ usedAngle = -angleDeg
            // Undo ต้องเป็น inverse => -usedAngle = +angleDeg
            float undoUsedAngle = +angleDeg;
            Debug.Log($"Remove {result_text} with axis: {axis} angle{undoUsedAngle}");
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

    // Rebuild from the start
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



    // เรียกจากปุ่ม - แสดงผล Circuit ทีละ Gate
    public void ExecuteCircuit()
    {
        if (isExecuting)
        {
            Debug.LogWarning("⚠️ Circuit is already executing!");
            return;
        }

        if (circuitData.Count == 0)
        {
            Debug.LogWarning("⚠️ No gates placed in circuit!");
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

        //copy current gates list
        List<GateData> gatesSnapshot = new List<GateData>(circuitData);

        if (gatesSnapshot.Count == 0)
        {
            Debug.LogWarning("⚠️ No gates to execute!");
            isExecuting = false;
            yield break;
        }

        UpdateDisplayText($"Executing {gatesSnapshot.Count} gates...");

        for (int i = 0; i < gatesSnapshot.Count; i++)
        {
            var gate = gatesSnapshot[i];
            Debug.Log($"▶ Gate {i + 1}/{gatesSnapshot.Count}: {gate.gateName}");

            UpdateDisplayText($"Gate {i + 1}/{gatesSnapshot.Count}: {gate.gateName}");

            if (blochSphere != null)
            {
                blochSphere.ApplyGate(gate.gateName);
            }

            yield return new WaitForSeconds(gateAnimationDelay); //some delay
        }

        Debug.Log("✅ All gates applied → ready for measurement");

        UpdateDisplayText("Circuit executed!\nReady for measurement");

        isExecuting = false;

        if (pendingRebuild)
        {
            pendingRebuild = false;
            Debug.Log("🔁 pendingRebuild detected → re-executing latest circuit");
            UpdateCircuit();
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

    // แสดงผลบน Text UI
    private void UpdateDisplayText(string message)
    {
        if (displayText != null)    displayText.text = message;
    }

    // สร้าง JSON สำหรับส่งไป Python - ✨ แก้ไขแล้ว
    // NOTE: why don't just update the file?
    public string GenerateCircuitJSON()
    {
        UpdateCircuit();
    
        Debug.Log("🔵 GenerateCircuitJSON called!");
        Debug.Log($"📊 Gates count: {circuitData.Count}");
    
        if (circuitData.Count == 0)
        {
            Debug.LogWarning("⚠️ No gates in circuit!");
            CircuitExport emptyExport = new CircuitExport
            {
                num_qubits = 1,
                gates = new List<GateExport>()
            };
            return JsonUtility.ToJson(emptyExport, true);
        }
    
        // 🔍 Debug: แสดง socket indices ทั้งหมด
        foreach (GateData gate in circuitData)
        {
            Debug.Log($"🔍 Gate: {gate.gateName} at socket {gate.socketIndex}");
        }
    
        // คำนวณ max qubit
        int maxQubit = 0;
        foreach (GateData gate in circuitData)
        {
            int qubitIndex = (gate.socketIndex - 1) / 3;
            Debug.Log($"🔍 Socket {gate.socketIndex} → Qubit {qubitIndex}");
        
            if (qubitIndex > maxQubit)
            {
                maxQubit = qubitIndex;
            }
        }
    
        int numQubitsNeeded = maxQubit + 1;
    
        Debug.Log($"🔢 Max qubit: {maxQubit}");
        Debug.Log($"🔢 Qubits needed: {numQubitsNeeded}");
    
        CircuitExport export = new CircuitExport
        {
            num_qubits = numQubitsNeeded,
            gates = new List<GateExport>()
        };
    
        foreach (GateData gate in circuitData)
        {
            //int qubitIndex = (gate.socketIndex - 1) / 3;

            export.gates.Add(new GateExport
            {
                gate_type = gate.gateName,
                qubit = 0,
                target_qubit = -1
            });
        }
    
        string json = JsonUtility.ToJson(export, true);
        Debug.Log($"📤 Generated JSON:\n{json}");

        return json;
    }

    // แสดง Circuit ทั้งหมดแบบไม่มี delay (สำหรับ debug)
    public void PrintCircuit()
    {
        UpdateCircuit();
        
        Debug.Log("═══════════════════════════════");
        Debug.Log($"📊 CIRCUIT TABLE ({circuitData.Count} gates)");
        Debug.Log("═══════════════════════════════");
        
        foreach (GateData gate in circuitData)
        {
            Debug.Log($"• {gate.socketName}: [{gate.gateName}] {gate.gateDescription}");
        }
        
        Debug.Log("═══════════════════════════════");
    }
}

// Class สำหรับเก็บข้อมูล Gate
[System.Serializable]
public class GateData
{
    public int socketIndex;
    public string socketName;
    public string gateName;
    public string gateDescription;
}

// Class สำหรับ Export ไป Python
[System.Serializable]
public class CircuitExport
{
    public int num_qubits;
    public List<GateExport> gates;
}

[System.Serializable]
public class GateExport
{
    public string gate_type; // "H", "X", "Y", "Z", "CNOT"
    public int qubit; // Qubit index (0-based)
    public int target_qubit; // สำหรับ 2-qubit gates เช่น CNOT (-1 ถ้าไม่ใช้)
}
