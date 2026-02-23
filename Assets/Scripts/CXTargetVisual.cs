using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class CXTargetVisual : MonoBehaviour
{
    public CXSpawnedGate ParentGate { get; private set; }
    public CircuitSocket PlacedSocket { get; private set; }

    private XRGrabInteractable grabInteractable;
    private bool isBeingHeld = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnDropped);
        grabInteractable.trackRotation = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnDropped);
        }
    }

    void Update()
    {
        if (isBeingHeld && ParentGate != null)
            ParentGate.ShowDashedPreview(transform.position);
    }

    public void SetParentGate(CXSpawnedGate gate) => ParentGate = gate;

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isBeingHeld = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = false;
            rb.isKinematic = false;
        }
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        isBeingHeld = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && PlacedSocket != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (PlacedSocket == null)
            ParentGate?.HideDashedPreview();
    }

    public void OnPlacedOnSocket(CircuitSocket socket)
    {
        PlacedSocket = socket;
        isBeingHeld = false;

        transform.SetPositionAndRotation(
            socket.transform.position,
            socket.transform.rotation
        );

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        ParentGate?.OnTargetPlaced(socket);
    }

    public void OnRemovedFromSocket()
    {
        PlacedSocket = null;
        ParentGate?.OnTargetRemoved();
    }
}