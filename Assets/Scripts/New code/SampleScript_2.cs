using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SampleScript_2 : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null) 
        {
            Debug.LogWarning("No EventSystem found!");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
            Debug.Log("Ray hit: " + result.gameObject.name);
    }
}
