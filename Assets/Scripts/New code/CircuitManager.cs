using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using System.IO;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

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

    [SerializeField]
    private GameObject canvas = null;

    // Folder and file name
    string dataFolder = "QuantumData", inputFolder = "QuantumInput", outputFolder = "QuantumOutput";
    private string jsonBlochInputFileName = "bloch_circuit_input.json", jsonQInputFileName = "q_circuit_input.json";
    private string jsonBlochOutputFileName = "bloch_circuit_output.json", jsonQOutputFileName = "q_circuit_output.json";
    private string jsonBlochSequenceFileName = "bloch_circuit_sequence.json", jsonQSequenceFileName = "q_circuit_sequence.json";
    private string pythonScriptFolder = "New code",pythonScriptName = "sample.py", pythonAnimateName = "QuantumSequence.py";
    private string mainSciptsPath = Path.Combine(Application.dataPath, "Scripts");
    private string pythonScriptPath;
    
    // can check each one if enable
    private QubitCircuit[] qubitCircuits; //array of qubit circuits
    private int totalQubits;
    private int[] totalGatesPerQubit; //to track total gates per qubit
    private List<int> exportIndex = new List<int>();
    bool isBlochSphere;
    private BlochSphere blochSphere = null;
    private QuantumUiStatManager uiManager = null;
    //private QSphere qSphere = null;
    private SequenceManager sqManager = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get all "Qubitcircuit" in children and sort
        qubitCircuits = GetComponentsInChildren<QubitCircuit>();
        Array.Sort(qubitCircuits, (a, b) => a.circuitIndex.CompareTo(b.circuitIndex));
        totalQubits = qubitCircuits.Length;

        
        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;
        if(isBlochSphere)   
        {
            blochSphere = sphere.GetComponent<BlochSphere>();
            if(blochSphere is null) Debug.LogWarning("Warning: Sphere model is missing!");
        } else
        {
            //try to get Q-sphere
        }

        if(canvas is null) Debug.LogWarning("Warning: UI canvas is missing!");
        else
        {
            uiManager = canvas.GetComponent<QuantumUiStatManager>();
            if(uiManager is null) Debug.LogWarning("Initialize Warning: UI script is missing is missing!");
            else Debug.Log("UI stat checking sucessful.");
        }

        sqManager = GetComponent<SequenceManager>();
        if(sqManager is null)
            Debug.LogWarning("Warning: Sequence manager component is missing!");
        
        

        getAvailibleQubit();
        updateOverallCircuit(null, -1, -1, true);
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

    //create object to save as JSON for python
    public string circuitToExportInit()
    {
        if(!isBlochSphere) getAvailibleQubit();
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
                exportQubit.gateList.Add(newGate);
            }

            circuitToExport.qubits.Add(exportQubit);
        }

        string json = JsonUtility.ToJson(circuitToExport, true);
        return json;
    }

    // find result bloch vector given gate
    public List<string> GetGateList()
    {
        CircuitExecutor executor = new CircuitExecutor();
        List<string> gateList = new List<string>();
        QubitCircuit targetQubit = qubitCircuits[Array.FindIndex(qubitCircuits, q => q.circuitIndex == exportIndex[0])];
        List<QuantumGate> gates = targetQubit.getListOfGate();
        foreach(QuantumGate gate in gates)
        {
            if (gate == null) continue;
            gateList.Add(gate.gateName);
        }

        return gateList;
    }

    private void updateBlochVectorInstant(CircuitExecutor executor)
    {
        if(blochSphere is null) {
            Debug.LogWarning("Warning: Sphere model is missing. Unable to animated!");
            return;
        }
        
        List<string> gateList = GetGateList();
        Vector3 resultVector = executor.GetResultBlochVector(gateList);
        blochSphere.AnimateToStateDirectly(resultVector);
    }

    private async Task calculateAndUpdateUi(CircuitExecutor executor, string inputPath, string outputPath)
    {
        await Task.Run(() => executor.PrepareThenRunQiskit(pythonScriptPath, inputPath, outputPath));

        // show value
        if(uiManager is not null)
            uiManager.ShowBlochResult(outputPath);
        else
            Debug.LogWarning("Warning: ui script is missing. Unable to show stat!");
    }

    // recalculate everytinm the circuit change
    public void updateOverallCircuit(string gateName, int socketIndex, int qubitIndex, bool isPlaced)
    {
        Debug.Log($"📊 CircuitManager: Qubit {qubitIndex} - Socket {socketIndex} - Gate {gateName} - Placed: {isPlaced}");
        string jsonExport = circuitToExportInit();
        updateJsonInputToFile(jsonExport);

        var executor = new CircuitExecutor();
        updateBlochVectorInstant(executor);
        
        // calculate value
        pythonScriptPath = Path.Combine(mainSciptsPath, pythonScriptFolder, pythonScriptName);

        GetJsonPath( isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName, 
                     isBlochSphere ? jsonBlochOutputFileName : jsonQOutputFileName, 
                     out string inputPath, out string outputPath);

        _ = calculateAndUpdateUi(executor, inputPath, outputPath);
    }

    private void createFolderIfNotExist(string folderPath)
    {
        if(!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    // TODO: add list input for checking any file
    private void updateJsonInputToFile(string jsonExport)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        createFolderIfNotExist(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        createFolderIfNotExist(inputFolderPath);
        
        string wantedJsonFile = isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName;
        string inputPath = Path.Combine(inputFolderPath, wantedJsonFile);

        File.WriteAllText(inputPath, jsonExport);
        //Debug.Log($"-----UPDATE-----\nUpdate json input: {inputPath}\n Result:{jsonExport}");
    }

    private void createEmptyJsonIfNotExist(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            File.WriteAllText(jsonPath, "{}");
    }

    // create full path of input and output json
    private void GetJsonPath(string inputFile, string outputFile, out string inputPath, out string outputPath)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        createFolderIfNotExist(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        createFolderIfNotExist(inputFolderPath);

        string outputFolderPath = Path.Combine(dataFolderPath, outputFolder);
        createFolderIfNotExist(outputFolderPath);   

        inputPath = Path.Combine(inputFolderPath, inputFile);
        outputPath = Path.Combine(outputFolderPath, outputFile);

        createEmptyJsonIfNotExist(inputPath);
        createEmptyJsonIfNotExist(outputPath);
    }

    public void PrepareForAnimation()
    {
        if(sqManager is null)
        {
            Debug.LogWarning("Warning: Sequence manager component is missing!");
            return;
        }

        pythonScriptPath = Path.Combine(mainSciptsPath, pythonScriptFolder, pythonAnimateName);
        GetJsonPath( isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName, 
                     isBlochSphere ? jsonBlochSequenceFileName : jsonQSequenceFileName, 
                     out string inputPath, out string outputPath);
                     
        _ = sqManager.prepareSequence(pythonScriptPath, inputPath, outputPath);
    }

    public void BackToNormal()
    {
        // get vector and ui back
        Debug.Log("Back to normal");
    }

    public string getJsonInputPath()
    {
        GetJsonPath( isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName, 
                     isBlochSphere ? jsonBlochOutputFileName : jsonQOutputFileName, 
                     out string inputPath, out string _);
        
        return inputPath;
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