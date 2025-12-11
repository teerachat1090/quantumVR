using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CircuitTable : MonoBehaviour
{
    [Header("Sockets")]
    public CircuitSocket[] sockets; // Array ของ Socket ทั้งหมด (SC1(1) ถึง SC1(10))
    
    [Header("Display Settings")]
    public TextMeshProUGUI displayText; // Text แสดงผลบนหน้าจอ VR
    public float delayBetweenGates = 0.5f; // Delay ระหว่าง Gate
    
    [Header("Circuit Data")]
    public List<GateData> circuitData = new List<GateData>(); // เก็บข้อมูล Circuit
    
    private bool isExecuting = false;
    
    void Start()
    {
        // หา Sockets ทั้งหมดที่เป็น child
        if (sockets == null || sockets.Length == 0)
        {
            sockets = GetComponentsInChildren<CircuitSocket>();
            Debug.Log($"📊 Found {sockets.Length} sockets");
        }
        
        // เรียงลำดับ Socket ตาม index
        System.Array.Sort(sockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));
        
        UpdateCircuit();
    }
    
    // อัปเดต Circuit Data เมื่อมีการเปลี่ยนแปลง
    public void UpdateCircuit()
    {
        circuitData.Clear();
        
        foreach (CircuitSocket socket in sockets)
        {
            if (socket.HasGate())
            {
                circuitData.Add(new GateData
                {
                    socketIndex = socket.socketIndex,
                    socketName = socket.socketName,
                    gateName = socket.currentGate.gateName,
                    gateDescription = socket.currentGate.gateDescription
                });
            }
        }
        
        Debug.Log($"📊 Circuit updated: {circuitData.Count} gates placed");
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
        
        StartCoroutine(ExecuteCircuitSequence());
    }
    
    // Execute Circuit แบบมี delay
    private IEnumerator ExecuteCircuitSequence()
    {
        isExecuting = true;
        
        Debug.Log("═══════════════════════════════");
        Debug.Log("🚀 EXECUTING QUANTUM CIRCUIT");
        Debug.Log("═══════════════════════════════");
        
        yield return new WaitForSeconds(0.3f);
        
        for (int i = 0; i < circuitData.Count; i++)
        {
            GateData gate = circuitData[i];
            
            // แสดงใน Console Log
            Debug.Log($"⚡ Step {i + 1}/{circuitData.Count}: {gate.socketName} → [{gate.gateName}]");
            
            // รอ delay
            yield return new WaitForSeconds(delayBetweenGates);
        }
        
        Debug.Log("═══════════════════════════════");
        Debug.Log($"✅ CIRCUIT COMPLETED - Total: {circuitData.Count} Gates");
        Debug.Log("═══════════════════════════════");
        
        isExecuting = false;
    }
    
    // แสดงผลบน Text UI
    private void UpdateDisplayText(string message)
    {
        if (displayText != null)
        {
            displayText.text = message;
        }
    }
    
    // สร้าง JSON สำหรับส่งไป Python - ✨ แก้ไขแล้ว
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