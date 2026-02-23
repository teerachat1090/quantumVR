using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ButtonAction : MonoBehaviour
{
    [SerializeField] private float pressDepth = 1f;
    [SerializeField] private float pressTime = .25f;

    [SerializeField] private List<UnityEvent> whenOnPressed = new List<UnityEvent>();

    [SerializeField] private int funcIndex = 0;

    private bool pressed = false;
    private float funcDelay = 1f;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }
    
    private void OnEnable()
    {
        if(grabInteractable == null)
        {
            Debug.LogWarning("XRGrabInteractable component is missing!");
            return;
        }
        grabInteractable.selectEntered.AddListener(OnSelectedEntered);
    }

    private void OnDisable()
    {
        if(grabInteractable == null)
        {
            Debug.LogWarning("XRGrabInteractable component is missing!");
            return;
        }
        grabInteractable.selectEntered.RemoveListener(OnSelectedEntered);
    }

    private void OnSelectedEntered(SelectEnterEventArgs args)
    {
        if(pressed) return;
        StartCoroutine(PressingButton());
    }

    private IEnumerator MoveButton(float depth, float time)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos - depth*Vector3.up;
        float elapsedTime = 0f;

        while(elapsedTime < time)
        {
            float t = elapsedTime/(time/2);
            if (elapsedTime > time/2) t -= 1;

            if(elapsedTime < time/2) transform.position = Vector3.Lerp(startPos, endPos, t);
            else transform.position = Vector3.Lerp(endPos, startPos, t);
            
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = startPos;
    }

    private void CycleIndex()
    {
        int functionAmount = whenOnPressed.Count;
        Debug.Log($"amount function: {functionAmount}");
        if(funcIndex != functionAmount - 1) funcIndex++;
        else funcIndex = 0;
    }

    // routine
    private IEnumerator PressingButton()
    {
        pressed = true;
        yield return MoveButton(pressDepth, pressTime);

        whenOnPressed[funcIndex].Invoke();
        if(whenOnPressed.Count != 1)CycleIndex();

        pressed = false;
        yield return new WaitForSeconds(funcDelay);
    }
}
