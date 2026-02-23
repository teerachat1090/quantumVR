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

    /// <summary>
    /// เรียกจาก CircuitSocket.OnGatePlaced() เมื่อ cube ถูกวางบน socket
    /// </summary>
    public void OnPlacedOnSocket(CircuitSocket socket)   // ← เปลี่ยนจาก QubitWireSocket
    {
        if (cxPrefab == null)
        {
            Debug.LogError("[CXCubeOnShelf] cxPrefab is not assigned!");
            return;
        }

        Debug.Log($"[CXCubeOnShelf] Placed on {socket.socketName} row={socket.rowIndex} → Spawning CX Prefab");

        // Spawn prefab ที่ตำแหน่ง socket
        Vector3 spawnPos = socket.transform.position + spawnOffset;
        GameObject spawnedObj = Instantiate(cxPrefab, spawnPos, Quaternion.identity);
        // Init CX gate
        CXSpawnedGate cxGate = spawnedObj.GetComponent<CXSpawnedGate>();
        if (cxGate != null)
            cxGate.Init(socket);                         // ← ส่ง CircuitSocket
        else
            Debug.LogError("[CXCubeOnShelf] Spawned prefab is missing CXSpawnedGate!");

        // Destroy cube หลัง 1 frame (ให้ XRI release ก่อน)
        StartCoroutine(DestroySelf());
    }

    private IEnumerator DestroySelf()
    {
        yield return null;
        Destroy(gameObject);
    }
}