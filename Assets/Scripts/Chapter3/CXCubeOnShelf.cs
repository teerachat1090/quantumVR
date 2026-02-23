using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

[RequireComponent(typeof(XRGrabInteractable))]
public class CXCubeOnShelf : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("ลาก CX_Spawn_Prefab ใส่ตรงนี้")]
    public GameObject cxPrefab;

    [Header("Spawn Offset")]
    public Vector3 spawnOffset = Vector3.zero;

    // ✅ รับ CircuitSocket_Chap3 (Chap3 scene)
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        if (cxPrefab == null)
        {
            Debug.LogError("[CXCubeOnShelf] cxPrefab is not assigned!");
            return;
        }
        Debug.Log($"[CXCubeOnShelf] Placed on {socket.socketName} row={socket.rowIndex} → Spawning CX Prefab");
        Vector3 spawnPos = socket.transform.position + spawnOffset;
        GameObject spawnedObj = Instantiate(cxPrefab, spawnPos, Quaternion.identity);
        CXSpawnedGate cxGate = spawnedObj.GetComponent<CXSpawnedGate>();
        if (cxGate != null)
            cxGate.Init(socket);
        else
            Debug.LogError("[CXCubeOnShelf] Spawned prefab is missing CXSpawnedGate!");
        StartCoroutine(DestroySelf());
    }

    // ✅ รับ CircuitSocket (BlochSphere / scene อื่น)
    public void OnPlacedOnSocket(CircuitSocket socket)
    {
        if (cxPrefab == null)
        {
            Debug.LogError("[CXCubeOnShelf] cxPrefab is not assigned!");
            return;
        }
        Debug.Log($"[CXCubeOnShelf] Placed on {socket.socketName} row={socket.rowIndex} → Spawning CX Prefab");
        Vector3 spawnPos = socket.transform.position + spawnOffset;
        GameObject spawnedObj = Instantiate(cxPrefab, spawnPos, Quaternion.identity);
        CXSpawnedGate cxGate = spawnedObj.GetComponent<CXSpawnedGate>();
        if (cxGate != null)
            cxGate.Init(socket);
        else
            Debug.LogError("[CXCubeOnShelf] Spawned prefab is missing CXSpawnedGate!");
        StartCoroutine(DestroySelf());
    }

    private IEnumerator DestroySelf()
    {
        yield return null;
        Destroy(gameObject);
    }
}