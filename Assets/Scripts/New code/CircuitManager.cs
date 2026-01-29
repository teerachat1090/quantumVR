using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using System.IO;

public class CircuitManager : MonoBehaviour
{
    private enum SphereType
    {
        BlochSphere, QSphere
    }
    [SerializeField]
    private SphereType sphereType = SphereType.BlochSphere;

    [SerializeField]
    private GameObject sphere = null; //check later: bloch / Q - sphere

    // Folder and file name
    string dataFolder = "QuantumData", inputFolder = "QuantumInput";
    private string jsonInputFileName = "circuit_input.json";

    // can check each one if enable
    private QubitCircuit[] qubitCircuits; //array of qubit circuits
    private int totalQubits;
    private int[] totalGatesPerQubit; //to track total gates per qubit
    private List<int> exportIndex = new List<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get all "Qubitcircuit" in children and sort
        qubitCircuits = GetComponentsInChildren<QubitCircuit>();
        Array.Sort(qubitCircuits, (a, b) => a.circuitIndex.CompareTo(b.circuitIndex));
        totalQubits = qubitCircuits.Length;
    }

    void initSetup()
    {
        //getGatePerQubit();
    }

    void getGatePerQubit()
    {
        totalGatesPerQubit = new int[totalQubits];
        for(int i=0; i<totalQubits; i++)
        {
            totalGatesPerQubit[i] = qubitCircuits[i].getNumberOfGates();
        } //in case of each qubit has different number of gates
    }

    //create object for export as JSON to python
    public string circuitToExportInit()
    {
        getAvailibleQubit();
        var circuitToExport = new CircuitToExecute
        {
            qubitAmount = exportIndex.Count,
            qubits = new List<Qubit>()
        };
        Debug.Log($"create structure, qubit amount: {circuitToExport.qubitAmount}");

        //to each avilible qubit (check by exportIndex)
        for(int i=0; i<exportIndex.Count; i++)
        {
            Qubit exportQubit = new Qubit()
            {
              qubitIndex = i,
              gateList = new List<Gate>()  
            };
            Debug.Log($"create qubit no.{i}, access index {exportIndex.IndexOf(i)}");

            QubitCircuit targetQubit = qubitCircuits[Array.FindIndex(qubitCircuits, q => q.circuitIndex == exportIndex[i])];
            List<QuantumGate> gates = targetQubit.getListOfGate();

            foreach(QuantumGate gate in gates)
            {
                if (gate == null) {
                    exportQubit.gateList.Add(null);
                    continue;
                }

                Gate newGate = new Gate
                {
                    gateName = gate.gateName,
                    targetQubit = (gate.gatetype == QuantumGate.inputType.Single) ? -1 : exportIndex.IndexOf(gate.getTarget())+1
                };
                Debug.Log($"create gate: {gate}");
                exportQubit.gateList.Add(newGate);
            }

            circuitToExport.qubits.Add(exportQubit);
        }

        // convert [Serializable] object to json
        string json = JsonUtility.ToJson(circuitToExport, true);
        Debug.Log($"📤 Generated JSON:\n{json}");
        return json;
    }

    //get list of indexes of availible qubit
    void getAvailibleQubit()
    {
        exportIndex.Clear();
        if(totalQubits == 0) return;
        foreach (QubitCircuit qubit in qubitCircuits)
        {
            if(qubit.enabled) {
                exportIndex.Add(qubit.circuitIndex);
                Debug.Log($"get qubit no. {qubit.circuitIndex} in index: {exportIndex.IndexOf(qubit.circuitIndex)}");
            }
        }
    }

    public void updateOverallCircuit(string gateName, int socketIndex, int qubitIndex, bool isPlaced)
    {
        Debug.Log($"📊 CircuitManager: Qubit {qubitIndex} - Socket {socketIndex} - Gate {gateName} - Placed: {isPlaced}");
        string jsonExport = circuitToExportInit();
        updateJsonInputToFile(jsonExport);

        var executor = new CircuitExecutor();
        StartCoroutine(executor.PrepareToRunQiskit());
        // run calculation
    }

    // NEED coordination function: dictionary => (info -> ui, vector -> sphere)

    private void updateJsonInputToFile(string jsonExport)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        if(!Directory.Exists(dataFolderPath))
            Directory.CreateDirectory(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        if(!Directory.Exists(inputFolderPath))
            Directory.CreateDirectory(inputFolderPath);
        
        string inputPath = Path.Combine(inputFolderPath, jsonInputFileName);
        File.WriteAllText(inputPath, jsonExport);
        Debug.Log($"-----UPDATE-----\nUpdate json input: {inputPath}\n Result:{jsonExport}");
    }
}

[Serializable]
public class CircuitToExecute
{
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