using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ════════════════════════════════════════════════════════════════
//  CXTargetVisual.cs  (v3)
//
//  เพิ่มจาก v2:
//  [+] ตอน Grab Target → วน loop ทุก socket
//      ถูก column = ใช้ Hover Material เขียว (Socket_Valid)
//      ผิด column = swap เป็น Material แดง (Socket_Invalid)
//  [+] ตอนวางแล้ว → restore material ทุก socket กลับปกติ
//  ไม่ต้องสร้าง SocketIndicator child object เพิ่ม
// ════════════════════════════════════════════════════════════════

[RequireComponent(typeof(XRGrabInteractable))]
public class CXTargetVisual : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Runtime
    // ─────────────────────────────────────────
    private CXSpawnedGate      parentGate;
    private XRGrabInteractable grabInteractable;
    private CircuitTableChap3  circuitTable;

    public bool IsSnappingToSocket { get; private set; }
    public CXSpawnedGate ParentGate => parentGate;

    // ─────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────
    public void Initialize(CXSpawnedGate gate)
    {
        parentGate       = gate;
        grabInteractable = GetComponent<XRGrabInteractable>();
        circuitTable     = Object.FindFirstObjectByType<CircuitTableChap3>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    // ─────────────────────────────────────────
    //  XR Events
    // ─────────────────────────────────────────
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        UpdateSocketHighlights();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (IsSnappingToSocket) return;
        RestoreSocketHighlights();
        parentGate.OnTargetReleased();
    }

    // ─────────────────────────────────────────
    //  Called by CircuitSocket_Chap3.OnGatePlaced
    // ─────────────────────────────────────────
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        CircuitSocket_Chap3 ctrlSocket = parentGate?.ControlSocket;

        if (ctrlSocket != null && ctrlSocket.columnIndex != socket.columnIndex)
        {
            Debug.LogWarning($"[CXTargetVisual] Column mismatch — reject");
            return;
        }

        if (ctrlSocket != null && ctrlSocket.rowIndex == socket.rowIndex)
        {
            Debug.LogWarning($"[CXTargetVisual] Same row as Control — reject");
            return;
        }

        if (socket.IsOccupied())
        {
            Debug.LogWarning($"[CXTargetVisual] Socket {socket.socketName} occupied");
            return;
        }

        IsSnappingToSocket = true;
        RestoreSocketHighlights();
        SnapToSocketDirect(socket);
        IsSnappingToSocket = false;
    }

    // ─────────────────────────────────────────
    //  Snap
    // ─────────────────────────────────────────
    public void SnapToSocketDirect(CircuitSocket_Chap3 socket)
    {
        Transform attach   = socket.attachTransform ?? socket.transform;
        transform.position = attach.position;

        parentGate.OnTargetSnapped(socket);
        Debug.Log($"[CXTargetVisual] Snapped → {socket.socketName}");
    }

    public void OnRemovedFromSocket()
    {
        parentGate.OnTargetReleased();
    }

    // ─────────────────────────────────────────
    //  Socket Highlight — swap Hover Material
    // ─────────────────────────────────────────
    private void UpdateSocketHighlights()
    {
        if (circuitTable == null) return;

        int controlCol = parentGate?.ControlSocket?.columnIndex ?? -1;

        for (int r = 0; r < circuitTable.RowCount; r++)
        {
            for (int c = 0; c < circuitTable.ColumnCount; c++)
            {
                CircuitSocket_Chap3 socket = circuitTable.GetSocket(r, c);
                if (socket?.socketInteractor == null) continue;

                bool isValid = (controlCol < 0 || c == controlCol);

                // ถูก column → hover ได้ (เขียว), ผิด column → hover ไม่ได้ (แดง)
                socket.socketInteractor.allowHover = isValid;
            }
        }
    }

    private void RestoreSocketHighlights()
    {
        if (circuitTable == null) return;

        for (int r = 0; r < circuitTable.RowCount; r++)
        {
            for (int c = 0; c < circuitTable.ColumnCount; c++)
            {
                CircuitSocket_Chap3 socket = circuitTable.GetSocket(r, c);
                if (socket?.socketInteractor == null) continue;

                // restore — เปิด hover กลับปกติ
                socket.socketInteractor.allowHover = true;
            }
        }
    }
}