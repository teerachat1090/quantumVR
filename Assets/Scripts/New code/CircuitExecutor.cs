using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.IO;

public class CircuitExecutor
{
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
        // <start coroutine>
        // run python background
        // receive data from python (check error)

        // save rusult as dictionary
        yield break;
    }

    private static void checkJsonInput()
    {
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
}