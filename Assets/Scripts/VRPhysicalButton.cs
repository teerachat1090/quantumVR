using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.Events;
using System.IO;
using System;
using System.Collections.Generic;

public class VRPhysicalButton : MonoBehaviour
{
    //-------------component input---------------------------------------
    [Header("Button Settings")]
    [SerializeField] private Transform buttonVisual;
    [SerializeField] private float pressDepth = 0.1f;
    [SerializeField] private float pressSpeed = 5f;
    
    [Header("Circuit Table")]
    [SerializeField] private CircuitTable circuitTable;
    
    [Header("Python Settings")]
    [SerializeField] private string pythonScriptPath = "Assets/Scripts/qiskit_runner.py";
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Log Settings")]
    [SerializeField] private string logFolderPath = "QuantumResults"; // โฟลเดอร์เก็บ log
    [SerializeField] private bool saveTimestampedLogs = true; // เก็บ log แยกตามเวลา
    
    [Header("Spawn Settings")]
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnDistance = 0.5f;
    //------------------------------------------------------------------

    public UnityEvent onButtonPressed;

    private Vector3 initialPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;
    private bool canPress = true;
    private string logDirectory;
    
    void Start()
    {
        if (buttonVisual == null)   buttonVisual = transform;
            
        initialPosition = buttonVisual.localPosition;
        pressedPosition = initialPosition - new Vector3(0, pressDepth, 0);
        
        // สร้างโฟลเดอร์สำหรับเก็บ log
        logDirectory = Path.Combine(Application.dataPath, logFolderPath);
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
            Debug.Log($"📁 Created log directory: {logDirectory}");
        }
        
        if (circuitTable == null)
        {
            circuitTable = FindFirstObjectByType<CircuitTable>();
            if (circuitTable != null) Debug.Log("✅ Found CircuitTable automatically!");
            else Debug.LogWarning("⚠️ CircuitTable not found!");
        }
        
        spawnSphereForTesting();
    }
    
    void spawnSphereForTesting()
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

    void Update()
    {
        if (!isPressed)
        {
            buttonVisual.localPosition = Vector3.Lerp(
                buttonVisual.localPosition, 
                initialPosition, 
                Time.deltaTime * pressSpeed
            );
        }
        else
        {
            buttonVisual.localPosition = Vector3.Lerp(
                buttonVisual.localPosition, 
                pressedPosition, 
                Time.deltaTime * pressSpeed * 2f
            );
        }
    }
    
    
    public void PressButton()
    {
        if (!canPress) return;
        
        Debug.Log("🔴 Button Pressed!");
        
        isPressed = true;
        canPress = false;
        
        if (circuitTable != null)
        {
            circuitTable.ExecuteCircuit();
            
            string circuitJSON = circuitTable.GenerateCircuitJSON();
            RunQiskitCircuit(circuitJSON);
        }
        else
        {
            Debug.LogError("❌ CircuitTable not assigned!");
        }
        
        onButtonPressed.Invoke();
        
        Invoke(nameof(ReleaseButton), 0.2f);
    }
    
    private void ReleaseButton()
    {
        isPressed = false;
        Invoke(nameof(EnablePress), 0.3f);
    }
    
    private void EnablePress()
    {
        canPress = true;
    }
    
    public void RunQiskitCircuit(string circuitJSON)
    {
        try
        {
            string tempPath = Path.Combine(Application.dataPath, "circuit_input.json");
            File.WriteAllText(tempPath, circuitJSON);
            Debug.Log($"💾 Saved circuit to: {tempPath}");
            
            // ลองทั้ง python และ python3
            string pythonCommand = FindPythonCommand();
            if (string.IsNullOrEmpty(pythonCommand))
            {
                Debug.LogError("❌ Python not found! Please install Python and add it to PATH");
                return;
            }
            
            Debug.Log($"🐍 Using Python command: {pythonCommand}");
            
            // ใช้ full path สำหรับ Python script
            string fullScriptPath = Path.GetFullPath(pythonScriptPath);
            Debug.Log($"📜 Script path: {fullScriptPath}");
            Debug.Log($"📂 Circuit JSON path: {tempPath}");
            
            // เช็คว่าไฟล์มีจริงหรือไม่
            if (!File.Exists(fullScriptPath))
            {
                Debug.LogError($"❌ Python script not found at: {fullScriptPath}");
                return;
            }
            
            if (!File.Exists(tempPath))
            {
                Debug.LogError($"❌ Circuit JSON not found at: {tempPath}");
                return;
            }
            
            string arguments = $"\"{fullScriptPath}\" \"{tempPath}\"";
            Debug.Log($"📝 Command: {pythonCommand} {arguments}");
            
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = pythonCommand;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Application.dataPath;
            
            Debug.Log($"🚀 Starting Python process...");
            process.Start();
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            process.WaitForExit();
            
            Debug.Log($"⏱️ Process exited with code: {process.ExitCode}");
            
            if (!string.IsNullOrEmpty(output))
            {
                Debug.Log($"🐍 Qiskit Output:\n{output}");
                
                // แยก JSON result ออกมา
                string jsonResult = ExtractJSONFromOutput(output);
                
                if (!string.IsNullOrEmpty(jsonResult))
                {
                    // บันทึก JSON log
                    SaveResultLog(jsonResult, circuitJSON);
                    
                    // แสดงผลใน Console แบบสวยงาม
                    DisplayResultInConsole(jsonResult);
                    
                    // แสดงบน UI
                    if (statusText != null)
                    {
                        statusText.text = $"✅ Circuit Executed!\nCheck Console for results";
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Could not extract JSON from output");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No output from Python script");
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"❌ Qiskit Error:\n{error}");
                if (statusText != null)
                {
                    statusText.text = $"❌ Error:\n{error}";
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to run Qiskit: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            if (statusText != null)
            {
                statusText.text = $"❌ Failed:\n{e.Message}";
            }
        }
    }
    
    // หา Python command ที่ใช้ได้
    private string FindPythonCommand()
    {
        string[] pythonCommands = { "python", "python3", "py" };
        
        foreach (string cmd in pythonCommands)
        {
            try
            {
                System.Diagnostics.Process testProcess = new System.Diagnostics.Process();
                testProcess.StartInfo.FileName = cmd;
                testProcess.StartInfo.Arguments = "--version";
                testProcess.StartInfo.UseShellExecute = false;
                testProcess.StartInfo.RedirectStandardOutput = true;
                testProcess.StartInfo.RedirectStandardError = true;
                testProcess.StartInfo.CreateNoWindow = true;
                
                testProcess.Start();
                testProcess.WaitForExit();
                
                if (testProcess.ExitCode == 0)
                {
                    return cmd;
                }
            }
            catch
            {
                continue;
            }
        }
        
        return null;
    }
    
    // แยก JSON ออกจาก output ของ Python
    private string ExtractJSONFromOutput(string output)
    {
        try
        {
            // หา JSON ระหว่าง "📤 JSON Result:" และ "✅ Execution completed"
            int startIndex = output.IndexOf("📤 JSON Result:");
            if (startIndex == -1) return null;
            
            int jsonStart = output.IndexOf("{", startIndex);
            int jsonEnd = output.LastIndexOf("}");
            
            if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
            {
                return output.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error extracting JSON: {e.Message}");
        }
        
        return null;
    }
    
    // บันทึก result เป็น JSON log file
    private void SaveResultLog(string resultJSON, string inputCircuit)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename;
            
            if (saveTimestampedLogs)
            {
                filename = $"result_{timestamp}.json";
            }
            else
            {
                filename = "latest_result.json";
            }
            
            string logPath = Path.Combine(logDirectory, filename);
            
            // สร้าง log object ที่มีทั้ง input และ output
            QuantumResultLog log = new QuantumResultLog
            {
                timestamp = timestamp,
                input_circuit = inputCircuit,
                qiskit_result = resultJSON
            };
            
            string fullLog = JsonUtility.ToJson(log, true);
            File.WriteAllText(logPath, fullLog);
            
            Debug.Log($"💾 Result saved to: {logPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save log: {e.Message}");
        }
    }
    
    // แสดงผลใน Console แบบสวยงาม
    private void DisplayResultInConsole(string jsonResult)
    {
        try
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("📊 QUANTUM CIRCUIT EXECUTION RESULT");
            Debug.Log("═══════════════════════════════════════");
            
            // Parse แบบง่ายๆ จาก JSON string
            if (jsonResult.Contains("\"success\"") && jsonResult.Contains("true"))
            {
                Debug.Log("✅ Success: True");
                
                // Extract values ด้วย string parsing
                string totalShots = ExtractValue(jsonResult, "total_shots");
                string numStates = ExtractValue(jsonResult, "num_states");
                string topState = ExtractValue(jsonResult, "top_state");
                string topProb = ExtractValue(jsonResult, "top_probability");
                
                Debug.Log($"🎯 Total Shots: {totalShots}");
                Debug.Log($"🔢 Number of States: {numStates}");
                Debug.Log($"⭐ Top State: |{topState}⟩");
                Debug.Log($"📈 Top Probability: {topProb}%");
                Debug.Log("─────────────────────────────────────");
                Debug.Log("🏆 Results saved to log file!");
            }
            else
            {
                Debug.LogWarning("⚠️ Execution may have failed");
            }
            
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"Raw JSON Result:\n{jsonResult}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to display result: {e.Message}");
            Debug.Log($"Raw JSON:\n{jsonResult}");
        }
    }
    
    // Helper: Extract value from JSON string
    private string ExtractValue(string json, string key)
    {
        try
        {
            string searchKey = $"\"{key}\":";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return "N/A";
            
            startIndex += searchKey.Length;
            
            // Skip whitespace and quotes
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

// Class สำหรับ parse Qiskit result
[Serializable]
public class QiskitResult
{
    public bool success;
    public int total_shots;
    public int num_states;
    public string top_state;
    public float top_probability;
    public CountsWrapper counts;
}

// Wrapper สำหรับ counts dictionary
[Serializable]
public class CountsWrapper
{
    // Python จะส่งมาเป็น dictionary, เราจะ parse เอง
}

// Class สำหรับเก็บ log ทั้งหมด
[Serializable]
public class QuantumResultLog
{
    public string timestamp;
    public string input_circuit;
    public string qiskit_result;
}