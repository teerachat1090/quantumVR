using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SocketsManager : MonoBehaviour
{
    [SerializeField] GameObject qubitSocketPrefab = null;

    [Header("Interaction setting")]
    [SerializeField] private GameObject storage = null; //disable grabbable when in animated mode
    [SerializeField] private InteractionLayerMask interactionLayer;

    private XRGrabInteractable[] sourceGates;
    private CircuitManager headManager;
    // script stationed for qubit
    private List<QubitCircuit> qubitCircuits = new List<QubitCircuit>();
    private List<int> exportIndex = new List<int>();
    private List<List<GameObject>> multiGateList = new List<List<GameObject>>();
    private List<List<int>> socketMap = new List<List<int>>();
    private int totalQubits = -1;

    void Awake()
    {
        if(storage is null) Debug.LogWarning("Warning: storage object is missing! Unable to set gate grabbable state!");
        else
        {
            sourceGates = storage.GetComponentsInChildren<XRGrabInteractable>();
            if(sourceGates is null) Debug.LogWarning("Warning: Unable to get component from source gate in storage!");

            headManager = GetComponentInParent<CircuitManager>();
            if(headManager is null) Debug.LogWarning("Warning: Unable to get component CircuitManager!");
        }
    }

    private void getAvailibleQubit()
    {
        exportIndex.Clear();
        if(totalQubits == 0) {
            Debug.LogWarning("Warning: No qubit represent!");
            return;
        }
        
        //qubitCircuits already sorted by circuitIndex
        foreach (QubitCircuit qubit in qubitCircuits)
        {
            if(!qubit.enabled) continue;

            exportIndex.Add(qubit.circuitIndex);
            //Debug.Log($"get qubit no. {qubit.circuitIndex} in index: {exportIndex.IndexOf(qubit.circuitIndex)}"); 
        }
    }

    private bool IsPrefabValid()
    {
        if(qubitSocketPrefab is null) 
        {
            Debug.LogError("Error: unable to spawn prefab - null element.");
            return false;
        }

        var circuit = qubitSocketPrefab.GetComponent<QubitCircuit>();
        if(circuit is null)
        {
            Debug.LogError("Error: Prefab has no necessary component - QubitCircuit");
            return false;
        }

        return true;
    }

    // position 0.5 to -0.5, offset 0.1, space 0.2
    // Limit to 5 qubits for now
    public void InitSocketPrefabSpawn(int qubitAmount)
    {
        if(!IsPrefabValid()) return;

        if(qubitAmount > 5)
        {
            Debug.LogWarning("Warning number of qubit is exceeding the limit (5 qubits). \nSetting to 5 qubits.");
            qubitAmount = 5;
        } else if(qubitAmount < 1)
        {
            Debug.LogWarning("Warning number of qubit is exceeding the limit (1 qubits). \nSet to 1 qubits.");
            qubitAmount = 1;
        }

        float xPos = 0.0f + (qubitAmount - 1.0f) * 0.1f + (qubitAmount%2==0? 0.1f: 0.0f);

        for(int i=0; i<qubitAmount; i++)
        {
            GameObject spawned = Instantiate(qubitSocketPrefab, gameObject.transform, false);
            spawned.transform.Translate(xPos*Vector3.right);
            spawned.name = qubitSocketPrefab.name + i;

            var qubitCircuit = spawned.GetComponent<QubitCircuit>();
            qubitCircuit.circuitIndex = i;
            qubitCircuits.Add(qubitCircuit);
            
            xPos-=0.2f;
        }
        qubitCircuits.Sort((a,b) => a.circuitIndex.CompareTo(b.circuitIndex));

        totalQubits = qubitAmount;
        getAvailibleQubit();
        
        //create socket map
    }

    public List<QubitCircuit> GetOverallCircuit()
    {
        return qubitCircuits;
    }

    public List<QuantumGate> GetGateListByQubitIndex(int qubitIndex)
    {
        int targetIndex = qubitCircuits.FindIndex( q => q.circuitIndex == qubitIndex);
        if(targetIndex == -1) return null;

        QubitCircuit targetQubit = qubitCircuits[targetIndex];
        return targetQubit.getListOfGate();
    }
    
    public List<string> GetGateAsStringList(int index)
    {
        List<QuantumGate> gates = GetGateListByQubitIndex(index);

        List<string> gateList = new List<string>();
        foreach(QuantumGate gate in gates)
        {
            if (gate == null) continue;
            gateList.Add(gate.getGateName());
        }

        return gateList;
    }

    public GameObject SearchForAvailibleSocketByIndex(int baseQubit, int columnToFind, out int qubitIndex)
    {
        foreach(QubitCircuit circuit in qubitCircuits)
        {
            if(circuit.circuitIndex == baseQubit) continue;

            GameObject target = circuit.CheckIfSocketEmpty(columnToFind);

            if(target is null) continue;
            
            qubitIndex = circuit.circuitIndex;
            return target;
        }

        qubitIndex = -1;
        return null;
    }

    public void updateCircuitByJson(string gateName, int socketIndex, int qubitIndex, bool isPlaced)
    {
        Debug.Log($"📊 CircuitManager: Qubit {qubitIndex} - Socket {socketIndex} - Gate {gateName} - Placed: {isPlaced}");

        string circuitJson = circuitToExportInit(headManager.isItBlochSphere());
        headManager.updateOverallCircuit(circuitJson);
    }

    public string circuitToExportInit(bool isBlochSphere)
    {
        Debug.Log("Creating json circuit...");
        if(!isBlochSphere) getAvailibleQubit(); //update availible qubits for Q-sphere, no need for bloch sphere
        var circuitToExport = new CircuitToExecute
        {
            blochSphere = isBlochSphere,
            qubitAmount = exportIndex.Count,
            qubits = new List<Qubit>()
        };
        //Debug.Log($"create structure, qubit amount: {circuitToExport.qubitAmount}");

        //to each avilible qubit (check by exportIndex)
        for(int i=0; i<exportIndex.Count; i++)
        {
            Qubit exportQubit = new Qubit()
            {
              qubitIndex = i,
              gateList = new List<Gate>()  
            };
            //Debug.Log($"create qubit no.{i}, access index {exportIndex.IndexOf(i)}");

            List<QuantumGate> gates = GetGateListByQubitIndex(exportIndex[i]);

            foreach(QuantumGate gate in gates)
            {
                if (gate == null) {
                    exportQubit.gateList.Add(null);
                    continue;
                }

                Gate newGate = new Gate
                {
                    gateName = gate.getGateName(),
                    targetQubit = (gate.getGateType() == QuantumGate.inputType.Single) ? -1 : exportIndex.IndexOf(gate.getTarget())+1
                };
                exportQubit.gateList.Add(newGate);
            }

            circuitToExport.qubits.Add(exportQubit);
        }

        string json = JsonUtility.ToJson(circuitToExport, true);

        Debug.Log("Creating json finished");
        return json;
    }

    public void FreezeGateBlock(bool doDisable)
    {
        foreach(QubitCircuit circuit in qubitCircuits)
        {
            circuit.FreezeGateBlock(doDisable);
        }

        if(sourceGates is null)
        {
            Debug.LogWarning("Warning: Unable to get component from source gate in storage!");
            return;
        }
        foreach(XRGrabInteractable xrGrab in sourceGates)
        {
            xrGrab.interactionLayers = doDisable ? InteractionLayerMask.GetMask("Default") : interactionLayer;
        }
    }
}

// object for saving as json file
[Serializable]
public class CircuitToExecute
{
    public bool blochSphere;
    public int qubitAmount;
    public List<Qubit> qubits;
}

[Serializable]
public class Qubit
{
    public int qubitIndex;
    public List<Gate> gateList;
}

[Serializable]
public class Gate
{
    public string gateName; //Identity gate if none specified
    public int targetQubit; //-1 if single qubit gate
}