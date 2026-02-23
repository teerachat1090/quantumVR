using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Central manager สำหรับ Quantum Teleportation scene (Flow B)
///
/// SETUP ใน Inspector:
///   - waveConnection  → QuantumWaveConnection component (Alice→Bob)
///   - waveDuration    → วินาที (0 = manual stop)
///   - statusText      → UI text (optional)
///   - rowNames        → ชื่อแต่ละ qubit row เช่น { "Alice", "EPR", "Bob" }
/// </summary>
public class TeleportationCircuitManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static TeleportationCircuitManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Wave Effect")]
    [Tooltip("QuantumWaveConnection component (Alice → Bob). เริ่มต้น inactive")]
    public QuantumWaveConnection waveConnection;

    [Tooltip("0 = ค้างจนกว่าจะกด Stop")]
    public float waveDuration = 5f;

    [Header("UI")]
    public TextMeshProUGUI statusText;

    [Header("Debug")]
    public bool printCircuitOnChange = true;

    [Header("Qubit Row Labels")]
    public string[] rowNames = { "Alice", "EPR", "Bob" };

    // ── Runtime ───────────────────────────────────────────────────────────
    private readonly List<CXSpawnedGate> placedCXGates = new List<CXSpawnedGate>();
    private bool waveActive = false;

    // ── CX Gate Registration ───────────────────────────────────────────────
    public void RegisterCXGate(CXSpawnedGate gate)
    {
        if (!placedCXGates.Contains(gate))
        {
            placedCXGates.Add(gate);
            Debug.Log($"[TeleportMgr] ✅ CX registered: " +
                      $"Control={RowLabel(gate.ControlRow)}, Target={RowLabel(gate.TargetRow)}, Column={gate.ColumnIndex}");
        }

        if (printCircuitOnChange) PrintCircuitSummary();
        UpdateStatusText();
    }

    public void UnregisterCXGate(CXSpawnedGate gate)
    {
        if (placedCXGates.Remove(gate))
        {
            Debug.Log($"[TeleportMgr] ❌ CX removed from column {gate.ColumnIndex}");
            if (printCircuitOnChange) PrintCircuitSummary();
            UpdateStatusText();
        }
    }

    // ── Wave trigger (เรียกจากปุ่ม) ───────────────────────────────────────
    /// <summary>Wire กับ TeleportationSendButton หรือ VRPhysicalButton.onButtonPressed</summary>
    public void TriggerWave()
    {
        if (waveConnection == null)
        {
            Debug.LogWarning("[TeleportMgr] waveConnection not assigned!");
            return;
        }
        if (waveActive) { Debug.Log("[TeleportMgr] Wave already running."); return; }

        Debug.Log("[TeleportMgr] 🌊 Alice → Bob wave triggered!");
        StartCoroutine(PlayWave());
    }

    private IEnumerator PlayWave()
    {
        waveActive = true;
        waveConnection.gameObject.SetActive(true);
        UpdateStatusText("🌊 Sending quantum state: Alice → Bob...");

        if (waveDuration > 0f)
        {
            yield return new WaitForSeconds(waveDuration);
            StopWave();
        }
    }

    public void StopWave()
    {
        if (!waveActive) return;
        waveActive = false;
        if (waveConnection != null) waveConnection.gameObject.SetActive(false);
        UpdateStatusText("✅ Teleportation complete!");
        Debug.Log("[TeleportMgr] 🌊 Wave stopped.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private string RowLabel(int idx) =>
        (idx >= 0 && idx < rowNames.Length) ? $"{rowNames[idx]}({idx})" : $"row{idx}";

    private void UpdateStatusText(string msg = null)
    {
        if (statusText == null) return;
        statusText.text = msg ?? (placedCXGates.Count == 0
            ? "Place CX gates to build the circuit."
            : $"{placedCXGates.Count} CNOT gate(s) placed. Press Send to transmit.");
    }

    public void PrintCircuitSummary()
    {
        Debug.Log("═══════════════════════════════");
        Debug.Log("📊 TELEPORTATION CIRCUIT");
        Debug.Log("═══════════════════════════════");
        if (placedCXGates.Count == 0)
            Debug.Log("  (no CX gates)");
        else
            foreach (var g in placedCXGates)
                Debug.Log($"  CX  Control={RowLabel(g.ControlRow)}  Target={RowLabel(g.TargetRow)}  Col={g.ColumnIndex}");
        Debug.Log("═══════════════════════════════");
    }

    public IReadOnlyList<CXSpawnedGate> PlacedCXGates => placedCXGates.AsReadOnly();
}