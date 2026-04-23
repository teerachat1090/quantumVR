using UnityEngine;

public class FindSocketPos : MonoBehaviour
{
    private bool found = false;

    void Update()
    {
        if (found) return;

        GameObject sc0 = GameObject.Find("SC0");
        if (sc0 != null)
        {
            Debug.Log($"SC0 position: {sc0.transform.position}");
            found = true;
        }
    }
}