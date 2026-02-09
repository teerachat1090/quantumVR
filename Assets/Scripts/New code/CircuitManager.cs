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
    private string pythonScriptFolder = "New code",pythonScriptName = "sample.py";
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get all "Qubitcircuit" in children and sort
        qubitCircuits = GetComponentsInChildren<QubitCircuit>();
        Array.Sort(qubitCircuits, (a, b) => a.circuitIndex.CompareTo(b.circuitIndex));
        totalQubits = qubitCircuits.Length;

        
        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;

        if(isBlochSphere)   blochSphere = sphere.GetComponent<BlochSphere>();
        if(blochSphere is null) Debug.LogWarning("Warning: Sphere model is missing!");

        if(canvas is null) Debug.LogWarning("Warning: UI canvas is missing!");
        else
        {
            uiManager = canvas.GetComponent<QuantumUiStatManager>();
            if(uiManager is null) Debug.LogWarning("Initialize Warning: UI script is missing is missing!");
            else Debug.Log("UI stat checking sucessful.");
        }
        

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
                Debug.Log($"create gate: {gate}");
                exportQubit.gateList.Add(newGate);
            }

            circuitToExport.qubits.Add(exportQubit);
        }

        // convert [Serializable] object to json
        string json = JsonUtility.ToJson(circuitToExport, true);
        //Debug.Log($"📤 Generated JSON:\n{json}");
        return json;
    }

    // find result bloch vector given gate
    public Vector3 GetBlochVectorResult(CircuitExecutor executor)
    {
        List<string> gateList = new List<string>();
        QubitCircuit targetQubit = qubitCircuits[Array.FindIndex(qubitCircuits, q => q.circuitIndex == exportIndex[0])];
        List<QuantumGate> gates = targetQubit.getListOfGate();
        foreach(QuantumGate gate in gates)
        {
            if (gate == null) continue;
            gateList.Add(gate.gateName);
        }
        // we gate list of string
        return executor.GetResultBlochVector(gateList);
    }

    private void updateBlochVectorInstant(CircuitExecutor executor)
    {
        //update bloch sphere vector
        if(blochSphere is not null) 
        {
            Vector3 resultVector = GetBlochVectorResult(executor);
            blochSphere.AnimateToStateDirectly(resultVector);
        }
        else
            Debug.LogWarning("Warning: Sphere model is missing. Unable to animated!");
    }

    private void calculateAndUpdateUiStarter(CircuitExecutor executor, string inputPath, string outputPath)
    {
        Debug.Log($"Start Async: {DateTime.Now}");
        _ = calculateAndUpdateUi(executor, inputPath, outputPath);
        Debug.Log("NO WAIT.");
    }

    private async Task calculateAndUpdateUi(CircuitExecutor executor, string inputPath, string outputPath)
    {
        Debug.Log("Asysnchronous task: Task start");
        await Task.Run(() => executor.PrepareThenRunQiskit(pythonScriptPath, inputPath, outputPath));

        // show value
        Debug.Log("Showing Result");
        if(uiManager is not null)
            uiManager.ShowBlochResult(outputPath);
        else
            Debug.LogWarning("Warning: ui script is missing. Unable to show stat!");
        Debug.Log("Showing Finished!");

        //await Task.Delay(1000);

        Debug.Log($"Asysnchronous task: Task complete {DateTime.Now}");
    }

    // --------------------------------dummies
    void OnPlayerEnterZone()
{
    Debug.Log($"Triggered! {DateTime.Now}");
    
    // 2. Call the async function. 
    // Because this function isn't 'async', it won't wait. 
    // It starts the task and immediately moves to the next line.
    _ = RunComplexLogicAsync(); 
    
    Debug.Log("The regular function has already finished, but the logic is running.");
}

// 3. The async logic stays in its own "lane"
    async Task RunComplexLogicAsync()
    {
    await Task.Delay(1000);
    Debug.Log($"Logic complete. {DateTime.Now}");
}   
    // ---------------------------------------dummies

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
        GetJsonPath(isBlochSphere, out string inputPath, out string outputPath);
        //OnPlayerEnterZone();
        calculateAndUpdateUiStarter(executor, inputPath, outputPath);
        
    }

    private void createFolderIfNotExist(string folderPath)
    {
        if(!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

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
    private void GetJsonPath(bool isBlochSphere, out string inputPath, out string outputPath)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        createFolderIfNotExist(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        createFolderIfNotExist(inputFolderPath);


        string outputFolderPath = Path.Combine(dataFolderPath, outputFolder);
        createFolderIfNotExist(outputFolderPath);

        // change file path (bloch sphere / q-sphere)
        string wantedInputPath = isBlochSphere ? jsonBlochInputFileName : jsonQInputFileName;
        string wantedOutputPath = isBlochSphere ? jsonBlochOutputFileName : jsonQOutputFileName;
        

        inputPath = Path.Combine(inputFolderPath, wantedInputPath);
        outputPath = Path.Combine(outputFolderPath, wantedOutputPath);

        createEmptyJsonIfNotExist(inputPath);
        createEmptyJsonIfNotExist(outputPath);
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