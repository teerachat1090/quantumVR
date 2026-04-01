using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketsManager : MonoBehaviour
{
    [SerializeField] GameObject qubitSocketPrefab = null;

    [Header("Interaction setting")]
    [SerializeField] private GameObject storage = null; //disable grabbable when in animated mode
    [SerializeField] private InteractionLayerMask interactionLayer;

    [Header("CNOT Gate Setting")]
    [SerializeField] private GameObject controlPrefab = null;
    [SerializeField] private GameObject targetPrefab = null;
    [SerializeField] InteractionLayerMask lockLayer;
    [SerializeField] InteractionLayerMask gateLayer;

    private XRGrabInteractable[] sourceGates;
    private CircuitManager headManager;
    // script stationed for qubit
    private List<QubitCircuit> qubitCircuits = new List<QubitCircuit>();
    private List<int> exportIndex = new List<int>();
    private List<GameObject> multiGateList = new List<GameObject>();
    private List<List<GateSocket>> socketMap = new List<List<GateSocket>>();
    
    [Header("Information")]
    public int totalQubits = -1;
    public int totalSocketEach = -1;

    public InteractionLayerMask GetLockLayer() {return lockLayer;}
    public InteractionLayerMask GetQuantumGateLayer() {return gateLayer;}

    private void CheckComponent()
    {
        if(storage == null) Debug.LogWarning("Warning: storage object is missing! Unable to set gate grabbable state!");
        else
        {
            sourceGates = storage.GetComponentsInChildren<XRGrabInteractable>();
            if(sourceGates is null) Debug.LogWarning("Warning: Unable to get component from source gate in storage!");

            headManager = GetComponentInParent<CircuitManager>();
            if(headManager is null) Debug.LogWarning("Warning: Unable to get component CircuitManager!");
        }
    }

    void Awake()
    {
        CheckComponent();
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
        if(qubitSocketPrefab == null) 
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

    private void initSocketMap()
    {
        foreach(QubitCircuit row in qubitCircuits)
        {
            var eachRow = new List<GateSocket>();
            row.GetComponentsInChildren(eachRow);
            eachRow.Sort((a,b) => a.socketIndex.CompareTo(b.socketIndex));
            socketMap.Add(eachRow);
        }
    }

    // position 0.5 to -0.5, offset 0.1, space 0.2
    // Limit to 5 qubits for now
    public void InitSocketPrefabSpawn(int qubitAmount, int totalSocket)
    {
        totalQubits = qubitAmount;
        totalSocketEach = totalSocket;
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
            qubitCircuit.socketAmount = totalSocket;
            qubitCircuit.circuitIndex = i;
            spawned.SetActive(true);
            qubitCircuits.Add(qubitCircuit);
            
            xPos-=0.2f;
        }
        qubitCircuits.Sort((a,b) => a.circuitIndex.CompareTo(b.circuitIndex));

        totalQubits = qubitAmount;
        getAvailibleQubit();
        initSocketMap();
        updateCircuitByJson(null, -1, -1, true);
    }

    private void ToggleEmptySocket(int row, int col, bool isLock)
    {
        if(socketMap[row][col].getCurrentGate() != null) return;

        var socketInteractor = socketMap[row][col].GetComponent<XRSocketInteractor>();

        if(isLock) socketInteractor.interactionLayers |= GetLockLayer();    //add layer
        else       socketInteractor.interactionLayers &= ~GetLockLayer();   //remove layer
    }

    // change xr layer to "lock"
    public void ToggleMultiGateColumn(MultiInputGateConnect wantedConnect, bool doLock = true)
    {
        int highQubit, lowQubit, column;
        wantedConnect.getHighLow(out highQubit, out lowQubit, out column);

        if(highQubit < 0 || lowQubit < 0 || column < 0)
        {
            Debug.LogError("Error: invalid range of multi gate.");
            return;
        }

        for(int i = lowQubit; i<=highQubit; i++)
        {
            if(socketMap[i][column].getCurrentGate() != null) continue; //found same group
            ToggleEmptySocket(i, column, doLock);
        }

        //expand range
        for(int i = lowQubit-1; i>=0; i--)
        {
            if(socketMap[i][column].getCurrentGate() != null) break; //reach wall
            ToggleEmptySocket(i, column, doLock);
        }
        for(int i = highQubit+1; i<totalQubits; i++)
        {
            if(socketMap[i][column].getCurrentGate() != null) break; //reach wall
            ToggleEmptySocket(i, column, doLock);
        }
    }

    public void RemoveFromMultiGateList(MultiInputGateConnect wantedObj)
    {
        multiGateList.Remove(wantedObj.gameObject);
        updateCircuitByJson("multi_input_gate", wantedObj.column, -1, false);
    }

    private void AssignMutiInputGate(GameObject baseObject, int row, int col, int controlNum = 1, int targetNum = 1)
    {
        //check if availible: down then up
        int total = controlNum + targetNum;
        if(totalQubits < total) return;
        
        int offset = -1, change = -1;

        //assign base and other prefab
        var groupParent = new GameObject();
        groupParent.name = "holder";
        var connect = groupParent.AddComponent<MultiInputGateConnect>();
        connect.socketsManager = this;
        connect.column = col;
        
        var baseObject_quantumGate = baseObject.GetComponent<QuantumGate>();
        baseObject_quantumGate.friendExist = true;
        connect.AddMember(baseObject_quantumGate);

        controlNum--;   total--;
        while(total > 0)
        {
            int watch = row+offset;
            if(watch > 0 && watch < totalQubits && socketMap[watch][col].getCurrentGate() == null)  
           { 
                var targetSocket = socketMap[watch][col];
                targetSocket.beLazy = true;
                Transform socketTransform = targetSocket.transform;
                
                var spawned = Instantiate(controlNum > 0 ? controlPrefab: targetPrefab, 
                                        socketTransform.position, quaternion.identity);
                
                spawned.transform.localScale *= .25f;
                var spawned_quantumGate = spawned.GetComponent<QuantumGate>();
                spawned_quantumGate.friendExist = true;
                connect.AddMember(spawned_quantumGate);
                
                if(controlNum > 0)  controlNum--;
                else                targetNum--;
                
                offset+=change;     total--;
            } 
            else if(change < 0) { offset = 1; change = 1;}
        }

        connect.RunLineConnect();
        multiGateList.Add(groupParent);

        // ===> do update circuit in here instead
        updateCircuitByJson(baseObject_quantumGate.getGateName(), col, row, true);
    }

    // only target = NOT gate
    public bool LookSocketMap(GameObject baseObject, int row, int col, int controlNum = 1, int targetNum = 1)
    {
        if(targetPrefab == null || controlPrefab == null){
            Debug.LogWarning("Warning: prefab for control or target is missing.");
            return false;
        }

        if(targetPrefab.GetComponent<QuantumGate>() == null || controlPrefab.GetComponent<QuantumGate>() == null)
        {
            Debug.LogWarning("Warning: wnated component (QuantumGate) in prefab is missing.");
            return false;
        }

        //check if availible: down then up
        int total = controlNum + targetNum;
        if(totalQubits < total) return false;
        
        int count = total-1, offset = -1, change = -1;
        while (count > 0)
        {
            int watch = row+offset;
            if(watch > 0 && watch < totalQubits && socketMap[watch][col].getCurrentGate() == null)  
            { 
                count--;    offset+=change;
            } 
            else if(change < 0) { offset = 1; change = 1;}  //can't go down
            else                return false;               //can't go up either
        }

        AssignMutiInputGate(baseObject, row, col, controlNum, targetNum);

        return true;
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
        List<QuantumGate> gateList = targetQubit.getListOfGate();
        if(gateList is null)
        {
            Debug.LogWarning("Warning: list is empty");
        }
        return gateList;
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

    // prevent player from grabbing quantum gates both on circuit and storage
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

[Serializable]
public class SocketStatus
{
    public GameObject socket;
    public bool isEmpty;
}