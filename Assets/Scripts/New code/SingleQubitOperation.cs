using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug; //there're class "Debug" in both UnityEngine and System.Diagnostics



public class SingleQubitOperation : MonoBehaviour
{
    [SerializeField] private GameObject buttonObject;
    private ButtonAction button;

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
    string inputPath, pythonCommand;

    string dataFolder = "QuantumData", inputFolder = "QuantumInput", outputFolder = "QuantumOutput";

    // file structure:
    //      Assets
    //       └  Quantum Data
    //             └    Quantum Input: single, multiple
    //             └    Quantum Result: single, multiple
    void CheckFileStructure()
    {
        
    }

    void Start()
    {
        inputPath = Path.Combine(Application.dataPath, jsonInputFileName);
        pythonCommand = FindPythonCommand();

        button = buttonObject.GetComponent<ButtonAction>();

        if(button is null)
        {
            Debug.LogWarning("Need button to make this script work!");
            return;
        }

        button.setAction(StartOp);
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
            string circuitJSON = circuitTable.GenerateCircuitJSON();
            // run qiskit background
        } else
        {
            //testing
        }
    }

    private IEnumerator RunQiskit(string JSONstring)
    {
        // 1.create JSON file for input
        File.WriteAllText(inputPath, JSONstring);

        

        yield return null;
    }
}
