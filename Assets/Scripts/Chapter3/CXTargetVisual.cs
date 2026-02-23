using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class CXTargetVisual : MonoBehaviour
{
    public CXSpawnedGate ParentGate { get; private set; }

    // ✅ เก็บได้ทั้งสองแบบ
    public CircuitSocket_Chap3 PlacedSocket       { get; private set; }
    public CircuitSocket       PlacedSocketLegacy { get; private set; }

    public bool IsPlaced => PlacedSocket != null || PlacedSocketLegacy != null;

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
            rb.useGravity  = false;
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
            rb.useGravity  = false;
            rb.isKinematic = false;
        }
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        isBeingHeld = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && IsPlaced)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (!IsPlaced)
            ParentGate?.HideDashedPreview();
    }

    // ✅ Chap3
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        PlacedSocket = socket;
        isBeingHeld  = false;
        SnapToSocket(socket.transform.position, socket.transform.rotation);
        ParentGate?.OnTargetPlaced(socket);
    }

    // ✅ Legacy (BlochSphere)
    public void OnPlacedOnSocket(CircuitSocket socket)
    {
        PlacedSocketLegacy = socket;
        isBeingHeld        = false;
        SnapToSocket(socket.transform.position, socket.transform.rotation);
        ParentGate?.OnTargetPlaced(socket);
    }

    private void SnapToSocket(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void OnRemovedFromSocket()
    {
        PlacedSocket       = null;
        PlacedSocketLegacy = null;
        ParentGate?.OnTargetRemoved();
    }
}