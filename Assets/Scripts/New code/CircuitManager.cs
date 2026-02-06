using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using System.IO;
using UnityEngine.InputSystem;

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
    string dataFolder = "QuantumData", inputFolder = "QuantumInput", outputFolder = "QuantumOutput";
    private string jsonBlochInputFileName = "bloch_circuit_input.json", jsonQInputFileName = "q_circuit_input.json";
    private string jsonBlochOutputFileName = "bloch_circuit_output.json", jsonQOutputFileName = "q_circuit_output.json";
    private string pythonScriptName = "qiskit_runner.py";
    private string mainSciptsPath = Path.Combine(Application.dataPath, "Scripts");
    private string pythonScriptPath;
    
    // can check each one if enable
    private QubitCircuit[] qubitCircuits; //array of qubit circuits
    private int totalQubits;
    private int[] totalGatesPerQubit; //to track total gates per qubit
    private List<int> exportIndex = new List<int>();
    bool isBlochSphere;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get all "Qubitcircuit" in children and sort
        qubitCircuits = GetComponentsInChildren<QubitCircuit>();
        Array.Sort(qubitCircuits, (a, b) => a.circuitIndex.CompareTo(b.circuitIndex));
        totalQubits = qubitCircuits.Length;

        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;
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
            blochSphere = isBlochSphere,
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
        pythonScriptPath = Path.Combine(mainSciptsPath, pythonScriptName);
        GetJsonPath(isBlochSphere, out string inputPath, out string outputPath);
        StartCoroutine(executor.PrepareThenRunQiskit(pythonScriptPath, inputPath, outputPath));
        // send output to sphere, UI (check from json output)
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
        
        string wantedJsonFile = isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName;
        string inputPath = Path.Combine(inputFolderPath, wantedJsonFile);
        File.WriteAllText(inputPath, jsonExport);
        Debug.Log($"-----UPDATE-----\nUpdate json input: {inputPath}\n Result:{jsonExport}");
    }

    private void GetJsonPath(bool isBlochSphere, out string inputPath, out string outputPath)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        string outputFolderPath = Path.Combine(dataFolderPath, outputFolder);

        // change file path (bloch sphere / q-sphere)
        string wantedInputPath = isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName;
        string wantedOutputPath = isBlochSphere ? jsonBlochOutputFileName : jsonQOutputFileName;
        inputPath = Path.Combine(inputFolderPath, wantedInputPath);
        outputPath = Path.Combine(inputFolderPath, wantedOutputPath);
    }
}

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