using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DragToRotate : MonoBehaviour
{
    private XRBaseInteractor interactor;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private float rotationSpeed = 500f;
    private float twistRotationSpeed = 1f;

    void Awake()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject as XRBaseInteractor;
        if(interactor == null) {
            Debug.LogWarning("NULL!");
            return;
        }
        lastPos = interactor.transform.position;
        lastRot = interactor.transform.rotation;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        interactor = null;
    }

    void Update()
    {
        if(interactor == null) return;

        //-------------------dragging-------------------------------------
        Vector3 currentPos = interactor.transform.position;
        Vector3 delta = currentPos - lastPos;
        lastPos = interactor.transform.position;

        //prevent minor movement
        if(delta.magnitude > 0.0005f)
        {
        float rotZ = -delta.y * rotationSpeed; //Drag DOWN
        float rotY = delta.z * rotationSpeed; //Drag LEFT

        transform.Rotate(Vector3.forward, rotZ, Space.World);
        transform.Rotate(Vector3.up, rotY, Space.World);
        }

        //------------------twisting------------------------------------
        Quaternion rot = interactor.transform.rotation;
        Quaternion deltaRot = rot * Quaternion.Inverse(lastRot); //subtract angle
        lastRot = rot; //update

        // convert Quaternion -> group of (angle:axis)
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);

        //prevent minor twist
        if(axis.x > 0.5f || axis.x < -0.5f)
        {
            float xRotation = angle * Mathf.Sign(axis.x) * twistRotationSpeed;
            transform.Rotate(Vector3.right, xRotation, Space.World);
        }
    }

    void resetRotation()
    {
        transform.Rotate(0,180,0);
    }
}
