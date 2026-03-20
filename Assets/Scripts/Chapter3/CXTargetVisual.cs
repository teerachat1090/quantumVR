using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;
using System;

[RequireComponent(typeof(XRGrabInteractable))]
public class CXTargetVisual : MonoBehaviour
{
    public CXSpawnedGate ParentGate { get; private set; }

    public CircuitSocket_Chap3 PlacedSocket       { get; private set; }
    public CircuitSocket       PlacedSocketLegacy { get; private set; }

    public bool IsPlaced => PlacedSocket != null || PlacedSocketLegacy != null;

    private XRGrabInteractable grabInteractable;
    private bool isBeingHeld = false;
    private bool isSnappingToSocket = false;
    public bool IsSnappingToSocket => isSnappingToSocket;
    private Vector3 _originalScale;

    public void Init(CXSpawnedGate parentGate) => ParentGate = parentGate;

    void Awake()
    {
        _originalScale = transform.localScale;
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.trackRotation = false;
        grabInteractable.trackScale    = false;
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnDropped);
        FreezeRigidbody();
    }

    void OnDestroy()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnDropped);
    }

    void Update()
    {
        if (isBeingHeld && ParentGate != null)
            ParentGate.ShowDashedPreview(transform.position);
    }

    // ── Grab / Drop ────────────────────────────────────────────────────────
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isBeingHeld = true;

        if (IsPlaced) DetachFromSocket();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        isBeingHeld = false;

        if (IsPlaced || isSnappingToSocket) return;

        ParentGate?.HideDashedPreview();
        ReturnToFloatPosition();
    }

    // ── Placed on Socket ───────────────────────────────────────────────────

    /// <summary>เรียกจาก CircuitSocket_Chap3.OnGatePlaced()</summary>
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        if (ParentGate == null) return;

        isSnappingToSocket = true;
        PlacedSocket = socket;

        StartCoroutine(SnapToSocket(
            anchor: socket.attachTransform != null ? socket.attachTransform : socket.transform,
            onComplete: () =>
            {
                socket.SetOccupiedByCX(ParentGate, isControl: false, isTarget: true);
                ParentGate.OnTargetPlacedOnSocket(socket);
                Debug.Log($"[CXTargetVisual] Snapped to {socket.socketName}");
            }
        ));
    }

    /// <summary>เรียกจาก CircuitSocket.OnGatePlaced() (Legacy / BlochSphere scene)</summary>
    public void OnPlacedOnSocket(CircuitSocket socket)
    {
        if (ParentGate == null) return;

        PlacedSocketLegacy = socket;

        StartCoroutine(SnapToSocket(
            anchor: socket.transform,
            onComplete: () =>
            {
                socket.SetOccupiedByCX(ParentGate, isControl: false, isTarget: true);
                ParentGate.OnTargetPlaced(socket);

                Debug.Log($"[CXTargetVisual] Snapped to {socket.socketName} (Legacy)");
            }
        ));
    }

    /// <summary>
    /// Release จาก XR hand → รอ 1 frame → SetParent ไปที่ anchor → Freeze
    /// KEY FIX: SetParent ออกจาก Root ทำให้ Target ไม่โดน Root offset อีกต่อไป
    /// </summary>
    private IEnumerator SnapToSocket(Transform anchor, Action onComplete)
    {
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            grabInteractable.interactionManager
                .CancelInteractableSelection((IXRSelectInteractable)grabInteractable);
        }

        // ── KEY FIX ───────────────────────────────────────────────────────
        // Detach ออกจาก Root ก่อน yield ทันที
        // ถ้าไม่ทำ: Root ขยับตอน Control วางลง socket ใหม่ใน frame นี้
        // จะพา Target (ที่ยังเป็นลูก Root) ไปด้วยก่อนที่ SetParent(anchor) จะทำงาน
        Vector3 worldPos   = transform.position;
        Quaternion worldRot = transform.rotation;
        Vector3 worldScale = transform.lossyScale; // save world scale ก่อน detach
        transform.SetParent(null, worldPositionStays: true);
        // restore scale เพราะ SetParent(worldPositionStays:true) อาจเปลี่ยน local scale
        transform.localScale = _originalScale;

        yield return null;

        // ถ้า PlacedSocket ถูก clear ระหว่าง yield → คืนกลับ Root
        if (PlacedSocket == null && PlacedSocketLegacy == null)
        {
            isSnappingToSocket = false;
            ReturnToFloatPosition();
            yield break;
        }

        transform.SetParent(anchor, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale    = _originalScale;

        FreezeRigidbody();
        isBeingHeld        = false;
        isSnappingToSocket = false;
        ParentGate?.HideDashedPreview();

        onComplete?.Invoke();
    }

    // ── Remove ─────────────────────────────────────────────────────────────
    public void OnRemovedFromSocket()
    {
        DetachFromSocket();
        ParentGate?.OnTargetRemoved();
        ReturnToFloatPosition();
    }

    public void ForceRemove()
    {
        PlacedSocket?.ClearCXState();
        PlacedSocketLegacy?.ClearCXState();
        DetachFromSocket();
        ParentGate?.OnTargetRemoved();
        ReturnToFloatPosition();
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private void DetachFromSocket()
    {
        PlacedSocket?.ClearCXState();
        PlacedSocketLegacy?.ClearCXState();
        PlacedSocket       = null;
        PlacedSocketLegacy = null;
        // ไม่ต้อง re-parent กลับ Root ที่นี่ — ReturnToFloatPosition จะจัดการ
    }

    private void ReturnToFloatPosition()
    {
        if (ParentGate == null) return;

        // Re-parent กลับเข้า Root เสมอ (ไม่ว่า parent ปัจจุบันจะเป็นอะไร)
        transform.SetParent(ParentGate.transform, worldPositionStays: false);
        transform.localPosition = ParentGate.TargetFloatOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale    = _originalScale;
        FreezeRigidbody();
    }

    private void FreezeRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic     = true;
        rb.useGravity      = false;
        rb.constraints     = RigidbodyConstraints.FreezeAll;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}