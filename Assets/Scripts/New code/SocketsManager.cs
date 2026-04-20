using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketsManager : MonoBehaviour
{
    [SerializeField] GameObject qubitSocketPrefab = null;
    [SerializeField] GameObject classicalSocketPrefab = null;

    [Header("Interaction setting")]
    [SerializeField] private GameObject storage = null; //disable grabbable when in animated mode
    [SerializeField] private InteractionLayerMask interactionLayer;
    [SerializeField] private float socketOffset = 0.0f;

    [Header("CNOT Gate Setting")]
    [SerializeField] private GameObject controlPrefab = null;
    [SerializeField] private GameObject targetPrefab = null;
    [SerializeField] private bool useClassical = true;
    [SerializeField] InteractionLayerMask lockLayer;
    [SerializeField] InteractionLayerMask gateLayer;

    [Header("Classical Bit Material")]
    [SerializeField] private Material classicalLineMaterial = null;

    private XRGrabInteractable[] sourceGates;
    private CircuitManager headManager;
    // script stationed for qubit
    private List<QubitCircuit> qubitCircuits = new List<QubitCircuit>();
    private ClassicalBitManager CBManager = null;
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
            Debug.LogError("Error: unable to spawn qubit socket prefab - null element.");
            return false;
        }

        var circuit = qubitSocketPrefab.GetComponent<QubitCircuit>();
        if(circuit is null)
        {
            Debug.LogError("Error: Prefab has no necessary component - QubitCircuit");
            return false;
        }

        if(classicalSocketPrefab == null && useClassical)
        {
            Debug.LogError("Error: unable to spawn classical socket prefab - null element.");
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

        float xPos = 0.1f*(qubitAmount - (useClassical ? 0: 1)) + socketOffset;

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

        if(useClassical){
            GameObject classicalRow = Instantiate(classicalSocketPrefab, gameObject.transform, false);
            classicalRow.transform.Translate(xPos*Vector3.right);
            classicalRow.name = classicalSocketPrefab.name;

            CBManager = classicalRow.GetComponent<ClassicalBitManager>();
            CBManager.socketAmount = totalSocket;
            CBManager.maxBitPosition = totalQubits;
            classicalRow.SetActive(true);
        }

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

    // change xr layer to "lock" given
    public void ToggleMultiGateColumn(int highQubit, int lowQubit, int column, bool doLock = true, bool spread = true)
    {
        if(highQubit < 0 || lowQubit < 0 || column < 0)
        {
            Debug.LogError("Error: invalid range of multi gate.");
            return;
        }

        //block gate that already placed 
        FreezeGateBlock(doLock);

        //add/delete exclusive layer
        //inside range
        for(int i = lowQubit; i<=highQubit; i++)
        {
            if(socketMap[i][column].getCurrentGate() != null) continue; //found same group
            ToggleEmptySocket(i, column, doLock);
        }

        if(!spread) return;

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
        var groupParent = new GameObject(){name = "holder"};
        var connect = groupParent.AddComponent<MultiInputGateConnect>();
        connect.classicalMaterial = classicalLineMaterial;
        connect.socketsManager = this;
        connect.column = col;
        connect.AddMember(baseObject);

        var baseObject_quantumGate = baseObject.GetComponent<QuantumGate>();
        baseObject_quantumGate.friendExist = true;
        baseObject_quantumGate.socket = socketMap[row][col];
        connect.gateName = baseObject_quantumGate.getGateName();
        

        controlNum--;   total--;
        while(total > 0)
        {
            int watch = row+offset;
            if(watch > 0 && watch < totalQubits && socketMap[watch][col].getCurrentGate() == null)  
           { 
                var targetSocket = socketMap[watch][col];
                targetSocket.beLazy = true;
                Transform socketTransform = targetSocket.transform;
                
                bool isController = controlNum > 0;
                var spawned = Instantiate(isController ? controlPrefab: targetPrefab, 
                                        socketTransform.position, quaternion.identity);
                spawned.transform.localScale *= .25f;
                connect.AddMember(spawned);

                var spawned_quantumGate = spawned.GetComponent<QuantumGate>();
                spawned_quantumGate.friendExist = true;
                spawned_quantumGate.socket = targetSocket;
                
                
                if(controlNum > 0)  controlNum--;
                else                targetNum--;
                
                offset+=change;     total--;
            } 
            else if(change < 0) { offset = 1; change = 1;}
        }

        connect.RunLineConnect();
        Debug.Log("Add <multi-gate> to multiGateList");
        multiGateList.Add(groupParent);

        // ===> do update circuit in here instead
        updateCircuitByJson(connect.gateName, col, row, true);
    }

    public bool ChecksocketSpace(GameObject baseObject, int row, int col, int controlNum = 1, int targetNum = 1)
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

    public void AssignMeasurement(GameObject baseObject, int row, int col)
    {
        //assign base and other prefab
        var groupParent = new GameObject(){name = "holder"};
        var connect = groupParent.AddComponent<MultiInputGateConnect>();
        connect.classicalMaterial = classicalLineMaterial;
        connect.socketsManager = this;
        connect.column = col;
        connect.classicalRelated = true;
        connect.AddMember(baseObject);

        var baseObject_quantumGate = baseObject.GetComponent<QuantumGate>();
        baseObject_quantumGate.friendExist = true;
        baseObject_quantumGate.socket = socketMap[row][col];
        connect.gateName = baseObject_quantumGate.getGateName();
        
        GameObject classicalConnect = CBManager.GetSocketByCol(col);
        CBManager.ShowPointByCol(col);
        connect.AddMember(classicalConnect);

        connect.RunLineConnect();
        Debug.Log("Add <measurement> to multiGateList");
        multiGateList.Add(groupParent);

        // ===> do update circuit in here instead
        updateCircuitByJson(connect.gateName, col, row, true);
    }

    public bool CheckDownward(int row, int col)
    {
        // check downward
        for(int i=row+1; i<totalQubits; i++)
        {
            if(socketMap[i][col].getCurrentGate() != null) return false;
        }

        return true;
    }

    public bool CheckPathToClassicalBit(GameObject baseObject, int row, int col)
    {
        // check downward
        if(!CheckDownward(row, col)) return false;

        //assign to group
        AssignMeasurement(baseObject, row, col);
        return true;
    }

    public void SetColumnSocketBetween(int highIndex, int lowIndex, int col, bool isEnable = false)
    {
        // 0 <= low < high < totalQ
        if(!(0 <= lowIndex && lowIndex < highIndex && highIndex < totalQubits)) return;
        
        for(int i=lowIndex+1; i<highIndex; i++)
        {
            if(socketMap[i][col].getCurrentGate() != null) continue;
            Debug.Log($"Set socket inbetween... mode:{isEnable}");
            var socketInteract = socketMap[i][col].GetComponent<XRSocketInteractor>();
            if(socketInteract != null) socketInteract.enabled = isEnable;
            else Debug.LogWarning("XRSocketInteractor is missing?");
        }
    }

    // Try to remove this function (use socketMap instead)
    public List<QubitCircuit> GetOverallCircuit()
    {
        return qubitCircuits;
    }

    // get list of gate in specific rows
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

        string circuitJson = circuitToExportInit();
        headManager.updateOverallCircuit(circuitJson);
    }

    public string circuitToExportInit()
    {
        Debug.Log("Creating json circuit...");
        var circuitInfo = new CircuitInfo
        {
            qubitAmount = totalQubits,
            socketAmount = totalSocketEach,
            CBitAmount = totalQubits,
            columnList = new List<int>(),
            gateList = new List<GateInfo>()
        };

        int count = 0;

        //single input gate
        for(int i=0; i<totalQubits; i++)
        {
            foreach(GateSocket socket in socketMap[i])
            {
                QuantumGate gate = socket.getCurrentGate();
                if(gate == null) {
                    continue;
                } 
                if(gate.getGateType() != QuantumGate.inputType.Single) {
                    continue;
                }

                var gateInfo = new GateInfo
                {
                    id = count,
                    name = gate.getGateName(),
                    column = socket.socketIndex,
                    controlRow = new List<int>() {socket.qubitIndex},
                    targetRow = null
                };
                count++;
                circuitInfo.gateList.Add(gateInfo);
                circuitInfo.columnList.Add(socket.socketIndex);
            }
        }

        //multi-input gate
        foreach(GameObject member in multiGateList)
        {
            var connect = member.GetComponent<MultiInputGateConnect>();
            if(connect == null) continue;

            //create info for multi-gate 
            var gateInfo = new GateInfo()
            {
                id = count,
                name = connect.gateName,
                column = connect.column
            };
            if (connect.classicalRelated)
            {
                gateInfo.controlRow = connect.GetIndexOneGate();
                var targetList = new List<int>() {CBManager.GetTargetClassicalBit(gateInfo.column)};
                gateInfo.targetRow = targetList;
                gateInfo.classical = true;
                gateInfo.condition = connect.conditionRelated;
            } else {
                connect.GetGateListByType(out List<int> controlRow, out List<int> targetsRow);
                gateInfo.controlRow = controlRow;
                gateInfo.targetRow = targetsRow;
            }
            count++;
            circuitInfo.gateList.Add(gateInfo);
            circuitInfo.columnList.Add(connect.column);
        }
        circuitInfo.columnList = circuitInfo.columnList.Distinct().ToList();

        // gather to convert to json string
        string json = JsonUtility.ToJson(circuitInfo, true);
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
            xrGrab.enabled = !doDisable;
        }
    }
}

// object for saving as json file
[Serializable]
public class CircuitToExecute
{
    public bool blochSphere;
    public int qubitAmount;
    public List<QubitExport> qubits;
}

[Serializable] //row
public class QubitExport
{
    public int qubitIndex;
    public List<QuantumGateExport> gateList;
}

[Serializable] //each column
public class QuantumGateExport
{
    public string gateName; //Identity gate if none specified
    public int targetQubit; //-1 if single qubit gate
}

//-----------------------------------

[Serializable]
public class CircuitInfo
{
    public int qubitAmount, socketAmount, CBitAmount;
    public List<int> columnList;
    public List<GateInfo> gateList;

}

[Serializable]
public class GateInfo
{
    public int id;
    public bool classical = false, condition = false;
    public string name;
    public int column;
    public List<int> controlRow, targetRow;
}

//if target is null => single input gate