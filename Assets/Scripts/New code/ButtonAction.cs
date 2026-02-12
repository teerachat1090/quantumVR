using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ButtonAction : MonoBehaviour
{
    public float pressDepth = 1f;
    public float pressTime = .25f;

    //public UnityEvent onPressed; //connect to other file and use coroutine in that file

    private bool pressed = false;
    private float funcDelay = 1f;
    private XRGrabInteractable grabInteractable;

    private Func<IEnumerator> onPressed; //any function that: no input, and return IEnumerator
    [SerializeField] private UnityEvent whenOnPressed;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    // outside script need to use this function to connect to the button
    public void setAction(Func<IEnumerator> action) => onPressed = action;
    
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

    // routine
    private IEnumerator PressingButton()
    {
        pressed = true;
        yield return MoveButton(pressDepth, pressTime);

        if (onPressed != null)  whenOnPressed.Invoke();
        else                    Debug.LogWarning("No function connect to this button!");

        pressed = false;
        yield return new WaitForSeconds(funcDelay);
    }
}
