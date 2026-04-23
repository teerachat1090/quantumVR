using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Spawns gate prefabs directly into sockets based on GateSolver output.
/// Gates are locked (not grabbable) after spawn.
/// Call SpawnGates() again to clear old gates and spawn new ones.
/// </summary>
public class GateSpawner : MonoBehaviour
{
    [Header("Gate Prefabs")]
    [SerializeField] private GameObject H_Gate;
    [SerializeField] private GameObject X_Gate;
    [SerializeField] private GameObject Y_Gate;
    [SerializeField] private GameObject Z_Gate;
    [SerializeField] private GameObject S_Gate;
    [SerializeField] private GameObject T_Gate;

    [Header("Spawn Adjustment")]
    [SerializeField] private float scaleMultiplier = 0.5f;
    [SerializeField] private float rotationY = 90f;

    private List<GameObject> spawnedGates = new List<GameObject>();

    // ---------- Public API ----------

    public void SpawnGates(List<string> gateSequence)
    {
        ClearSpawnedGates();

        for (int i = 0; i < gateSequence.Count; i++)
        {
            GameObject prefab = GetPrefabByName(gateSequence[i]);
            if (prefab == null)
            {
                Debug.LogWarning($"[GateSpawner] No prefab for gate: {gateSequence[i]}");
                continue;
            }

            // หา Socket SC{i}
            GameObject socket = GameObject.Find($"SC{i}");
            if (socket == null)
            {
                Debug.LogWarning($"[GateSpawner] Socket SC{i} not found!");
                continue;
            }

            // ✅ เซ็ต beLazy ก่อน Instantiate เสมอ
            GateSocket gateSocket = socket.GetComponent<GateSocket>();
            if (gateSocket != null) gateSocket.beLazy = true;

            // หา Attach point
            Transform attachPoint = socket.transform.Find("Attach");
            Vector3 spawnPos = attachPoint != null ? attachPoint.position : socket.transform.position;

            // Spawn gate
            GameObject spawned = Instantiate(prefab, spawnPos, socket.transform.rotation);

            // ✅ Destroy Spawner ไม่ให้ spawn ซ้ำ
            Spawner spawnerScript = spawned.GetComponent<Spawner>();
            if (spawnerScript != null) Destroy(spawnerScript);

            // ✅ Lock gate ไม่ให้ผู้เล่นหยิบ
            XRGrabInteractable grab = spawned.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = false;

            // ปรับขนาดและหมุน
            spawned.transform.localScale = prefab.transform.localScale * scaleMultiplier;
            spawned.transform.Rotate(0f, rotationY, 0f);

            spawnedGates.Add(spawned);
            Debug.Log($"[GateSpawner] Spawned {gateSequence[i]} at SC{i}");
        }
    }

    public void ClearSpawnedGates()
    {
        foreach (GameObject g in spawnedGates)
        {
            if (g == null) continue;

            // ✅ reset socket ก่อน destroy
            QuantumGate qg = g.GetComponent<QuantumGate>();
            if (qg != null && qg.socket != null)
                qg.socket.currentGate = null;

            Destroy(g);
        }
        spawnedGates.Clear();

        // ✅ ลบ gate clone ที่เหลือค้างใน scene
        foreach (string gateName in new string[] { "H_Gate", "T_Gate", "X_Gate", "Y_Gate", "Z_Gate", "S_Gate" })
        {
            foreach (var obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (obj.name.Contains(gateName) && obj.name.Contains("Clone"))
                    Destroy(obj);
            }
        }
    }

    // ---------- Helper ----------

    private GameObject GetPrefabByName(string gateName)
    {
        switch (gateName.ToUpper())
        {
            case "H":  return H_Gate;
            case "X":  return X_Gate;
            case "Y":  return Y_Gate;
            case "Z":  return Z_Gate;
            case "S":  return S_Gate;
            case "T":  return T_Gate;
            default:   return null;
        }
    }
}