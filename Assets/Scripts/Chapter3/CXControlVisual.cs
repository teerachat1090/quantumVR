using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// แนบกับ child "Control" ใน CX_Spawn_Prefab
/// จัดการการวาง Control ลง socket ใหม่หลังถูก grab
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class CXControlVisual : MonoBehaviour
{
    public CXSpawnedGate ParentGate { get; private set; }

    public void Init(CXSpawnedGate parentGate)
    {
        ParentGate = parentGate;
    }

    /// <summary>เรียกจาก CircuitSocket_Chap3 เมื่อ Control วางลง socket</summary>
    public void OnPlacedOnSocket(CircuitSocket_Chap3 socket)
    {
        if (ParentGate == null) return;

        // Snap root ไปที่ socket ใหม่
        ParentGate.OnControlPlacedOnSocket(socket);
    }
}