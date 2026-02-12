using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using UnityEditor;
using System;
using Unity.VisualScripting;
using System.Collections.Generic;
using Newtonsoft.Json;
//using System.Text.Json;
//using Debug = UnityEngine.Debug; //there're class "Debug" in both UnityEngine and System.Diagnostics



public class SingleQubitOperation : MonoBehaviour
{
    [SerializeField] private GameObject buttonObject;

    // related script: table, sphere
    [SerializeField] private CircuitTable circuitTable;

    [SerializeField] private BlochSphere blochSphere;
    [SerializeField] private bool testMode = false;

    private string testJSON = @"{
        ""success"": true,
        ""total_shots"": 1024,
        ""num_states"": 2,
        ""top_state"": ""0"",
        ""top_probability"": 50.1953125,
        ""counts"": { ""0"": 514, ""1"": 510 }
    }";

    private string jsonInputFileName = "circuit_input.json";
    private string pythonScriptName = "qiskit_runner.py";
    
    string inputPath, pythonCommand;

    string dataFolder = "QuantumData", inputFolder = "QuantumInput", outputFolder = "QuantumOutput";
    string dataFolderPath, inputFolderPath, outputFolderPath;

    private struct ProcResult
    {
        public int exitCode;
        public string stdout;
        public string stderr;
    }

    // file structure:
    //      <persistent_data_path>
    //       └  Quantum Data
    //             └    Quantum Input: single, multiple
    //             └    Quantum Result: single, multiple
    //
    // <persistent_data_path> = C:\Users\<usernameS>\AppData\LocalLow\DefaultCompany\VR quantum
    public void CheckFileStructure()
    {
        // check folder of writable file
        dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        outputFolderPath = Path.Combine(dataFolderPath, outputFolder);

        if(!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
            Directory.CreateDirectory(inputFolderPath);
            Directory.CreateDirectory(outputFolderPath);
            return;
        }

        if(!Directory.Exists(inputFolderPath)) Directory.CreateDirectory(inputFolderPath);
        if(!Directory.Exists(outputFolderPath)) Directory.CreateDirectory(outputFolderPath);
    }

    void Start()
    {
        Debug.Log($"Persistant data path is {Application.persistentDataPath}");
        Debug.Log($"Main input path: {Path.Combine(Application.persistentDataPath, dataFolder)}");
        Debug.Log($"Game asset: {Application.dataPath}");
        CheckFileStructure();

        pythonCommand = FindPythonCommand();
    }

    private string FindPythonCommand()
    {
        string[] pythonCommands = { "python", "python3", "py" };

        foreach (string cmd in pythonCommands)
        {
            try //running simple command: get python version
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var testProcess = Process.Start(startInfo))
                {
                    testProcess.WaitForExit();
                    if (testProcess.ExitCode == 0) return cmd;
                }
            }
            catch { }
        }
        return null;
    }

    private ProcResult RunPythonScript(string scriptPath, string JsonPath)
    {
        var r = new ProcResult { exitCode = -1, stdout = "", stderr = "" };
        string output = "", error = "";

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonCommand,
            Arguments = $"\"{scriptPath}\" \"{JsonPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Application.dataPath
        };

        using (var process = new Process())
        {
            process.StartInfo = startInfo;

            process.OutputDataReceived += (sender, e) =>
            {
                if(!string.IsNullOrEmpty(e.Data))   output += e.Data + "\n";
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if(!string.IsNullOrEmpty(e.Data))   error += e.Data + "\n";
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            r.exitCode = process.ExitCode;
        }

        r.stdout = output;
        r.stderr = error;
        return r;
    }

    string CheckOutput(string output)
    {
        Debug.Log($"From output: {output}");
        try{
        Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
        if(values.ContainsKey("success") || values.ContainsKey("total_shots"))
        {
            Debug.Log("output is OK");
            return output;
        }

        Debug.Log("output is invalid");
        return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"❌ Error extracting JSON: {e.Message}");
            return null;
        }
    }

    private void saveResult(string resultJson, string inputCircuit)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"result.json";
            string logPath = Path.Combine(outputFolderPath, filename);

            Debug.Log($"Result JSON file:\n {resultJson}");
            //var log = new Dictionary<string, object>(resultJson);
            //log["timestamp"] = timestamp;
            //log["input_circuit"] = inputCircuit;

            File.WriteAllText(logPath, JsonUtility.ToJson(resultJson, true));
            Debug.Log($"File content: {JsonUtility.ToJson(resultJson, true)}");
            //Debug.Log($"💾 Result saved to: {logPath}\n Log:\n{log}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save log: {e.Message}");
        }
    }

    // file structure (Reminder):
    //      <persistent_data_path>
    //       └  Quantum Data
    //             └    Quantum Input: single, multiple
    //             └    Quantum Result: single, multiple
    //
    // <persistent_data_path> = C:\Users\esicl\AppData\LocalLow\DefaultCompany\VR quantum
    private IEnumerator RunQiskitBackground(string circuitJSON)
    {
        Debug.Log("Try to Running Operation");
        // <Persistent_data_path>\QuantumData
        string mainInputPath = Path.Combine(Application.persistentDataPath, dataFolder);
        //      => \\QuantumVR\Assets\Scripts
        string mainSciptsPath = Path.Combine(Application.dataPath, "Scripts");

        // 1.Put JSON to file -> need "Application.persistentDataPath" for write-able file
        //      => \\QuantumData\QuantumInput\circuit_input.json
        inputPath = Path.Combine(Path.Combine(mainInputPath, inputFolder), jsonInputFileName);
        File.WriteAllText(inputPath, circuitJSON);
        Debug.Log($"1. put json to file -> path: {inputPath}");

        // 2.Check python command, script
        if (string.IsNullOrEmpty(pythonCommand))
        {
            Debug.LogError("Python command not found!");
            yield break;
        }
        string scriptPath = Path.Combine(mainSciptsPath, pythonScriptName);
        if (!File.Exists(scriptPath))
        {
            Debug.LogError("Python script not found!");
            yield break;
        }

        // 3.Run background process, wait, and get the result
        Task<ProcResult> task = Task.Run(() => RunPythonScript(scriptPath, inputPath));
        while (!task.IsCompleted)  yield return null;

        ProcResult pr = task.Result;
        if (!string.IsNullOrEmpty(pr.stderr))
        {
            Debug.LogError($"Qiskit Error! Reason: {pr.stderr}");
            yield break;
        }
        if (string.IsNullOrEmpty(pr.stdout))
        {
            Debug.LogWarning("Empty Result");
            yield break;
        }

        string jsonResult = CheckOutput(pr.stdout);
        if (jsonResult is null)
        {
            Debug.LogWarning("Invalid result");
            yield break;
        }
        Debug.Log("result end sucessfully");

        saveResult(jsonResult, circuitJSON);

        yield break;
    }

    public void CoRoutineOnStartOp()
    {
        StartCoroutine("StartOp");
    }

    private IEnumerator StartOp()
    {
        Debug.Log("Start single qubit operation");

        if(circuitTable is null)
        {
            Debug.LogError("❌ CircuitTable not assigned!");
            yield return null;
        }

        while (circuitTable.IsExecuting())  yield return null;

        if (!testMode)
        {
            Debug.Log("start calculating");
            string circuitJSON = circuitTable.GenerateCircuitJSON();
            Debug.Log($"start python with JSON: \n{circuitJSON}");
            StartCoroutine(RunQiskitBackground(circuitJSON));
        } else
        {
            //testing
        }

        yield return null;
    }
}
