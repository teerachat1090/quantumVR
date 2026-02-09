using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.IO;

public class GateRotation
{
    public Vector3 axis;
    public float angle;
    public GateRotation(Vector3 Axis, float Angle)
    {
        axis = Axis;
        angle = Angle;
    }
}

public class CircuitExecutor
{
    public static readonly Dictionary<string, GateRotation> rotationInfo;
    static CircuitExecutor() // like __init__ in python
    {
        rotationInfo = new Dictionary<string, GateRotation>   // {key, value}
        {
            { "H",  new GateRotation((Vector3.right + Vector3.up).normalized,     180.0f) }, 
            { "X",  new GateRotation( Vector3.right,     180.0f)},
            { "Y",  new GateRotation( Vector3.forward,   180.0f)},
            { "Z",  new GateRotation( Vector3.up,        180.0f)},
            { "I",  new GateRotation( Vector3.up,        0.0f)},
            { "T",  new GateRotation( Vector3.up,        45.0f)},
            { "S",  new GateRotation( Vector3.up,        90.0f)},
            { "TT", new GateRotation( Vector3.up,        -45.0f)},
            { "ST", new GateRotation( Vector3.up,        -90.0f)},
            { "SQRTX",  new GateRotation( Vector3.right,  90.0f)},
            { "SQRTXT", new GateRotation( Vector3.right,  -90.0f)},
        };
    }

    private string pythonCommand = FindPythonCommand();

    private struct ProcessResult
    {
        public int exitCode;
        public string stdout;
        public string stderr;
    }

    private ProcessResult RunPythonScript(string scriptPath, string JsonInputPath, string JsonOutputPath)
    {
        var r = new ProcessResult { exitCode = -1, stdout = "", stderr = "" };
        string output = "", error = "";

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonCommand,
            Arguments = $"\"{scriptPath}\" \"{JsonInputPath}\" \"{JsonOutputPath}\"",
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

    // output name: instantBlochOutput.json
    public IEnumerator PrepareThenRunQiskit(string scriptPath, string inputFilePath, string outputPath){
        Debug.Log("-----Preparing-----");
        
        // check python script
        if (!File.Exists(scriptPath))
        {
            Debug.LogError("Python script not found!");
            yield break;
        }

        // check python command
        if (string.IsNullOrEmpty(pythonCommand))
        {
            Debug.LogError("Python command not found!");
            yield break;
        }

        Task<ProcessResult> task = Task.Run(() => RunPythonScript(scriptPath, inputFilePath, outputPath));
        while (!task.IsCompleted)  yield return null;
        
        ProcessResult pr = task.Result;
        if (!string.IsNullOrEmpty(pr.stderr))
            Debug.LogError($"Qiskit Error! Reason: {pr.stderr}");

        yield break;
    }

    private static string FindPythonCommand()
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

    //find result vector given list of gate
    public Vector3 GetResultBlochVector(List<string> gateList)
    {
        Vector3 resultVec = Vector3.up;
        // input: list of gate name (string)
        if(gateList.Count == 0)
            return resultVec;

        foreach(string gate in gateList)
        {
            if(!rotationInfo.TryGetValue(gate, out GateRotation infoResult)) continue;
            
            Quaternion rotation = Quaternion.AngleAxis(infoResult.angle, infoResult.axis);
            resultVec = rotation * resultVec;
        }

        return resultVec;
    }
}