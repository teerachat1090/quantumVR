using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.IO;

public static class PhaseColoring
{
    private static float[] hueBoundRange = {
        35f, 25f, 15f, 20f,  10f,  55f,
        30f, 15f, 10f, 55f, 100f,   0f
        };

    private static float[] hueLowerBound = {
        235f, 270f, 295f, 310f, 330f, 340f,
        355f, 025f, 040f, 050f, 105f, 205f
    };

    private static float angleRef = 30f;

    public static float PhaseToColor(float phase)
    {
        int angleOffSet = (int) phase % (int) angleRef;
        int boundindex = (int) (phase/angleRef);
        float hueRange = hueBoundRange[boundindex];
        float lowerBound = hueLowerBound[boundindex];

        float offset =  hueRange * angleOffSet / angleRef;

        float newHue = (lowerBound + offset)/360f;
        if (newHue > 1) newHue-=1f;

        return newHue;
    }
}

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

    // object initialization function
    static CircuitExecutor()
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
    public void PrepareThenRunQiskit(string scriptPath, string inputFilePath, string outputPath){
        // check python script
        if (!File.Exists(scriptPath))
        {
            Debug.LogError("Python script not found!");
            return;
        }

        // check python command
        if (string.IsNullOrEmpty(pythonCommand))
        {
            Debug.LogError("Python command not found!");
            return;
        }

        //Task<ProcessResult> task = Task.Run(() => RunPythonScript(scriptPath, inputFilePath, outputPath));
        ProcessResult pr = RunPythonScript(scriptPath, inputFilePath, outputPath);
        
        if (!string.IsNullOrEmpty(pr.stderr))
            Debug.LogError($"Qiskit Error! Reason: {pr.stderr}");

        return;
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

    public Vector3 DoRotate(Vector3 vector, string gate, bool isInverse)
    {
        if(!rotationInfo.TryGetValue(gate, out GateRotation infoResult)) return vector;
            
        Quaternion rotation = Quaternion.AngleAxis(infoResult.angle * (isInverse ? -1 : 1), infoResult.axis);
        vector = rotation * vector;
        return vector;
    }
}