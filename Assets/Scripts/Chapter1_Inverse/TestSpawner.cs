using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestSpawner : MonoBehaviour
{
    [SerializeField] private GateSpawner gateSpawner;
    private bool hasSpawned = false;

    void Start()
    {
        StartCoroutine(SpawnAfterDelay());
    }

    IEnumerator SpawnAfterDelay()
{
    Debug.Log($"[TestSpawner] Coroutine started, hasSpawned={hasSpawned}");
    if (hasSpawned) yield break;

    yield return new WaitForSeconds(1f);

    Debug.Log("[TestSpawner] About to spawn!");
    hasSpawned = true;
    var testSequence = new List<string> { "H", "T", "X" };
    gateSpawner.SpawnGates(testSequence);
}
}