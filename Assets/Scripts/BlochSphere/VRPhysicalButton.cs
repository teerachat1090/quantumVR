using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class VRPhysicalButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private Transform buttonVisual;
    [SerializeField] private float pressDepth = 0.1f;
    [SerializeField] private float pressSpeed = 5f;

    [Header("Circuit Table")]
    [SerializeField] private CircuitTable circuitTable;

    [Header("Bloch Sphere")]
    [SerializeField] private BlochSphere blochSphere;

    [Header("Test Mode")]
    [SerializeField] private bool useTestMode = true;

    [Header("Python Settings")]
    [Tooltip("You can use 'Assets/..' or 'Scripts/..'. Recommended: Assets/Scripts/qiskit_runner.py")]
    [SerializeField] private string pythonScriptPath = "Assets/Scripts/qiskit_runner.py";
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Log Settings")]
    [SerializeField] private string logFolderPath = "QuantumResults";
    [SerializeField] private bool saveTimestampedLogs = true;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnDistance = 0.5f;

    [Header("Status Color")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Color idleColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color runningColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color successColor = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color errorColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

    private Material _buttonMat;

    private enum ButtonStatus { Idle, Running, Success, Error, Disabled }
    private ButtonStatus _status = ButtonStatus.Idle;

    public UnityEvent onButtonPressed;

    private Vector3 initialPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;
    private bool canPress = true;
    private string logDirectory;

    private void SetStatus(ButtonStatus s, string msg = null)
    {
        _status = s;

        if (_buttonMat != null)
        {
            Color c = idleColor;
            switch (s)
            {
                case ButtonStatus.Idle: c = idleColor; break;
                case ButtonStatus.Running: c = runningColor; break;
                case ButtonStatus.Success: c = successColor; break;
                case ButtonStatus.Error: c = errorColor; break;
                case ButtonStatus.Disabled: c = disabledColor; break;
            }

            // URP Lit: _BaseColor, Built-in: _Color
            if (_buttonMat.HasProperty("_BaseColor")) _buttonMat.SetColor("_BaseColor", c);
            if (_buttonMat.HasProperty("_Color")) _buttonMat.SetColor("_Color", c);

            // Emission (ถ้ามี) ให้ดูเด่น
            if (_buttonMat.HasProperty("_EmissionColor"))
            {
                _buttonMat.EnableKeyword("_EMISSION");
                _buttonMat.SetColor("_EmissionColor", c * 0.8f);
            }
        }

        if (statusText != null && !string.IsNullOrEmpty(msg))
            statusText.text = msg;
    }

    void Start()
    {
        if (buttonVisual == null) buttonVisual = transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();

        if (buttonRenderer != null)
        {
            _buttonMat = buttonRenderer.material; // instance material
        }

        SetStatus(ButtonStatus.Idle, useTestMode ? "🧪 Test Mode Ready" : "Ready");

        initialPosition = buttonVisual.localPosition;
        pressedPosition = initialPosition - new Vector3(0, pressDepth, 0);

        logDirectory = Path.Combine(Application.dataPath, logFolderPath);
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
            Debug.Log($"📁 Created log directory: {logDirectory}");
        }

        if (circuitTable == null)
        {
            circuitTable = FindFirstObjectByType<CircuitTable>();
            Debug.Log(circuitTable != null ? "✅ Found CircuitTable automatically!" : "⚠️ CircuitTable not found!");
        }

        if (blochSphere == null)
        {
            blochSphere = FindFirstObjectByType<BlochSphere>();
            Debug.Log(blochSphere != null ? "✅ Found BlochSphere automatically!" : "⚠️ BlochSphere not found!");
        }

        SpawnSphereForTesting();

        if (useTestMode)
            Debug.Log("🧪 TEST MODE ENABLED - Will not run Python");
    }

    void Update()
    {
        if (!isPressed)
        {
            buttonVisual.localPosition = Vector3.Lerp(buttonVisual.localPosition, initialPosition, Time.deltaTime * pressSpeed);
        }
        else
        {
            buttonVisual.localPosition = Vector3.Lerp(buttonVisual.localPosition, pressedPosition, Time.deltaTime * pressSpeed * 2f);
        }
    }

    private void SpawnSphereForTesting()
    {
        if (spherePrefab == null)
        {
            spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.transform.localScale = Vector3.one * 0.1f;
            spherePrefab.AddComponent<Rigidbody>();
            spherePrefab.SetActive(false);
        }

        if (spawnPoint == null)
        {
            GameObject spawnObj = new GameObject("SpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = Vector3.forward * spawnDistance;
            spawnPoint = spawnObj.transform;
        }
    }

    public void PressButton()
    {
        if (!canPress) return;

        Debug.Log("🔴 Button Pressed!");
        isPressed = true;
        canPress = false;

        SetStatus(ButtonStatus.Running, "⏳ Measuring...");

        if (circuitTable != null)
        {
            StartCoroutine(WaitForExecutionThenMeasure());
        }
        else
        {
            SetStatus(ButtonStatus.Error, "❌ No CircuitTable");
            Debug.LogError("❌ CircuitTable not assigned!");
        }

        onButtonPressed?.Invoke();
        Invoke(nameof(ReleaseButton), 0.2f);
    }

    private IEnumerator WaitForExecutionThenMeasure()
    {
        SetStatus(ButtonStatus.Running, "⏳ Waiting for circuit...");
        Debug.Log("⏳ Waiting for circuit execution to complete...");

        while (circuitTable.IsExecuting())
            yield return null;

        Debug.Log("✅ Circuit execution complete! Now measuring...");

        if (!useTestMode)
        {
            string circuitJSON = circuitTable.GenerateCircuitJSON();
            RunQiskitCircuit(circuitJSON);
        }
        else
        {
            TestBlochSphereDirectly();
        }
    }

    private void ReleaseButton()
    {
        isPressed = false;
        Invoke(nameof(EnablePress), 0.3f);
    }

    private void EnablePress()
    {
        SetStatus(ButtonStatus.Idle, useTestMode ? "🧪 Ready" : "Ready");
        canPress = true;
    }

    private void TestBlochSphereDirectly()
    {
        Debug.Log("🧪 Testing Bloch Sphere directly (no Python)");

        string fakeJSON = @"{
            ""success"": true,
            ""total_shots"": 1024,
            ""num_states"": 2,
            ""top_state"": ""0"",
            ""top_probability"": 50.1953125,
            ""counts"": { ""0"": 514, ""1"": 510 }
        }";

        if (blochSphere != null)
        {
            blochSphere.UpdateFromQiskitResult(fakeJSON);

            // ✅ measurement collapse animation
            var counts = ExtractCounts(fakeJSON);
            blochSphere.AnimateMeasurementCollapseFromCounts(counts);

            SetStatus(ButtonStatus.Success, "🧪 Test OK ✅");

            if (statusText != null)
                statusText.text = "🧪 Test Mode\n|0⟩: 50.2%\n|1⟩: 49.8%";
        }
        else
        {
            SetStatus(ButtonStatus.Error, "❌ No BlochSphere");
            Debug.LogError("❌ BlochSphere not found!");
        }
    }

    // ✅ รองรับทั้ง "Assets/..." และ "Scripts/..."
    private string ResolveScriptPath(string userPath)
    {
        if (string.IsNullOrWhiteSpace(userPath)) return "";

        string p = userPath.Replace("\\", "/").Trim();

        // If starts with Assets/ => convert to absolute using Application.dataPath
        if (p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            string rel = p.Substring("Assets/".Length);
            return Path.Combine(Application.dataPath, rel);
        }

        // If starts with Scripts/ (or any) => treat as relative under Assets
        return Path.Combine(Application.dataPath, p);
    }

    public void RunQiskitCircuit(string circuitJSON)
    {
        try
        {
            SetStatus(ButtonStatus.Running, "🐍 Running Qiskit...");

            string tempPath = Path.Combine(Application.dataPath, "circuit_input.json");
            File.WriteAllText(tempPath, circuitJSON);
            Debug.Log($"💾 Saved circuit to: {tempPath}");

            string pythonCommand = FindPythonCommand();
            if (string.IsNullOrEmpty(pythonCommand))
            {
                SetStatus(ButtonStatus.Error, "❌ Python not found");
                Debug.LogError("❌ Python not found! Please install Python and add it to PATH");
                return;
            }

            string fullScriptPath = ResolveScriptPath(pythonScriptPath);
            Debug.Log($"📜 Script path resolved: {fullScriptPath}");

            if (!File.Exists(fullScriptPath))
            {
                SetStatus(ButtonStatus.Error, "❌ Script not found");
                Debug.LogError($"❌ Python script not found at: {fullScriptPath}");
                return;
            }

            string arguments = $"\"{fullScriptPath}\" \"{tempPath}\"";
            Debug.Log($"🔧 Command: {pythonCommand} {arguments}");

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = pythonCommand;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Application.dataPath;

            Debug.Log("🚀 Starting Python process...");
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Debug.Log($"⏱️ Process exited with code: {process.ExitCode}");

            if (!string.IsNullOrEmpty(error))
            {
                SetStatus(ButtonStatus.Error, "❌ Qiskit Error");
                Debug.LogError($"❌ Qiskit Error:\n{error}");
                if (statusText != null) statusText.text = $"❌ Error:\n{error}";
                return;
            }

            if (string.IsNullOrEmpty(output))
            {
                SetStatus(ButtonStatus.Error, "❌ No output");
                Debug.LogError("❌ No output from Python");
                return;
            }

            string jsonResult = ExtractJSONFromOutput(output);
            if (string.IsNullOrEmpty(jsonResult))
            {
                SetStatus(ButtonStatus.Error, "⚠️ Bad output");
                Debug.LogWarning("⚠️ Could not extract JSON from output");
                return;
            }

            SaveResultLog(jsonResult, circuitJSON);
            DisplayResultInConsole(jsonResult);

            if (blochSphere != null)
            {
                Debug.Log("🎨 Bloch Sphere updated!");

                // ✅ measurement collapse animation
                var counts = ExtractCounts(jsonResult);
                blochSphere.AnimateMeasurementCollapseFromCounts(counts);
            }

            SetStatus(ButtonStatus.Success, "✅ Done!");
            if (statusText != null) statusText.text = "✅ Circuit Executed!\nCheck Bloch Sphere";
        }
        catch (Exception e)
        {
            SetStatus(ButtonStatus.Error, "❌ Exception");
            Debug.LogError($"❌ Failed to run Qiskit: {e.Message}");
        }
    }

    private string FindPythonCommand()
    {
        string[] pythonCommands = { "python", "python3", "py" };

        foreach (string cmd in pythonCommands)
        {
            try
            {
                var testProcess = new System.Diagnostics.Process();
                testProcess.StartInfo.FileName = cmd;
                testProcess.StartInfo.Arguments = "--version";
                testProcess.StartInfo.UseShellExecute = false;
                testProcess.StartInfo.RedirectStandardOutput = true;
                testProcess.StartInfo.RedirectStandardError = true;
                testProcess.StartInfo.CreateNoWindow = true;

                testProcess.Start();
                testProcess.WaitForExit();

                if (testProcess.ExitCode == 0) return cmd;
            }
            catch { }
        }

        return null;
    }

    private string ExtractJSONFromOutput(string output)
    {
        try
        {
            for (int start = output.LastIndexOf('{'); start >= 0; start = output.LastIndexOf('{', start - 1))
            {
                int braceCount = 0;
                for (int i = start; i < output.Length; i++)
                {
                    if (output[i] == '{') braceCount++;
                    else if (output[i] == '}') braceCount--;

                    if (braceCount == 0)
                    {
                        string candidate = output.Substring(start, i - start + 1);
                        if (candidate.Contains("\"total_shots\"") || candidate.Contains("\"success\""))
                            return candidate;
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error extracting JSON: {e.Message}");
        }

        return null;
    }

    // ✅ counts parser แบบไม่ต้องใช้ Newtonsoft/SimpleJSON
    // รองรับ format: "counts": {"0":526,"1":498}
    private Dictionary<string, int> ExtractCounts(string json)
    {
        var dict = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(json)) return dict;

        int idx = json.IndexOf("\"counts\"");
        if (idx < 0) return dict;

        int braceStart = json.IndexOf('{', idx);
        if (braceStart < 0) return dict;

        int braceCount = 0;
        int braceEnd = -1;
        for (int i = braceStart; i < json.Length; i++)
        {
            if (json[i] == '{') braceCount++;
            else if (json[i] == '}') braceCount--;
            if (braceCount == 0) { braceEnd = i; break; }
        }
        if (braceEnd < 0) return dict;

        string countsBlock = json.Substring(braceStart + 1, braceEnd - braceStart - 1);

        // split ด้วย comma ระดับบนสุดพอสำหรับ 0/1
        var parts = countsBlock.Split(',');
        foreach (var p in parts)
        {
            var kv = p.Split(':');
            if (kv.Length != 2) continue;

            string key = kv[0].Trim().Trim('"'); // "0" or "1"
            if (int.TryParse(kv[1].Trim(), out int val))
                dict[key] = val;
        }

        return dict;
    }

    private void SaveResultLog(string resultJSON, string inputCircuit)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = saveTimestampedLogs ? $"result_{timestamp}.json" : "latest_result.json";
            string logPath = Path.Combine(logDirectory, filename);

            QuantumResultLog log = new QuantumResultLog
            {
                timestamp = timestamp,
                input_circuit = inputCircuit,
                qiskit_result = resultJSON
            };

            File.WriteAllText(logPath, JsonUtility.ToJson(log, true));
            Debug.Log($"💾 Result saved to: {logPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save log: {e.Message}");
        }
    }

    private void DisplayResultInConsole(string jsonResult)
    {
        try
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("📊 QUANTUM CIRCUIT EXECUTION RESULT");
            Debug.Log("═══════════════════════════════════════");

            string totalShots = ExtractValue(jsonResult, "total_shots");
            string topState = ExtractValue(jsonResult, "top_state");
            string topProb = ExtractValue(jsonResult, "top_probability");

            Debug.Log($"🎯 Total Shots: {totalShots}");
            Debug.Log($"⭐ Top State: |{topState}⟩");
            Debug.Log($"📈 Probability: {topProb}%");
            Debug.Log("═══════════════════════════════════════");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to display result: {e.Message}");
        }
    }

    private string ExtractValue(string json, string key)
    {
        try
        {
            string searchKey = $"\"{key}\":";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return "N/A";

            startIndex += searchKey.Length;

            while (startIndex < json.Length && (json[startIndex] == ' ' || json[startIndex] == '\"'))
                startIndex++;

            int endIndex = startIndex;
            while (endIndex < json.Length && json[endIndex] != ',' && json[endIndex] != '}' && json[endIndex] != '\"')
                endIndex++;

            return json.Substring(startIndex, endIndex - startIndex).Trim();
        }
        catch
        {
            return "N/A";
        }
    }
}

[Serializable]
public class QuantumResultLog
{
    public string timestamp;
    public string input_circuit;
    public string qiskit_result;
}
