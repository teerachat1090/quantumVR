using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// ปิด Locomotion System ทั้งหมดตอนอยู่ในหน้าเมนู
/// เปิดกลับเมื่อออกจากเมนู
/// </summary>
public class MenuLocomotionManager : MonoBehaviour
{
    [Header("Locomotion System")]
    [Tooltip("ลาก GameObject 'Locomotion' จาก XR Origin มาใส่")]
    public GameObject locomotionSystem;
    
    [Header("Components to Disable")]
    [Tooltip("ปิดการหมุน")]
    public bool disableSnapTurn = true;
    
    [Tooltip("ปิดการเดิน")]
    public bool disableWalk = true;
    
    [Tooltip("ปิดการเทเลพอร์ต")]
    public bool disableTeleport = true;
    
    // เก็บ Component เดิมไว้
    private SnapTurnProviderBase snapTurnProvider;
    private ActionBasedSnapTurnProvider actionSnapTurn;
    private ContinuousMoveProviderBase continuousMoveProvider;
    private ActionBasedContinuousMoveProvider actionContinuousMove;
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportProvider;
    
    // เก็บสถานะเดิม
    private bool originalSnapTurnState;
    private bool originalWalkState;
    private bool originalTeleportState;
    
    void Start()
    {
        // หา Locomotion System
        if (locomotionSystem == null)
        {
            Debug.LogWarning("⚠️ Locomotion System not assigned! Trying to find automatically...");
            locomotionSystem = GameObject.Find("Locomotion");
        }
        
        if (locomotionSystem == null)
        {
            Debug.LogError("❌ Cannot find Locomotion System!");
            return;
        }
        
        // หา Components
        FindLocomotionComponents();
        
        // ปิด Locomotion
        DisableLocomotion();
    }
    
    void OnDestroy()
    {
        // เปิดกลับเมื่อออกจากเมนู
        EnableLocomotion();
    }
    
    /// <summary>
    /// หา Component ทั้งหมดใน Locomotion System
    /// </summary>
    void FindLocomotionComponents()
    {
        if (locomotionSystem == null) return;
        
        // หา Snap Turn
        snapTurnProvider = locomotionSystem.GetComponentInChildren<SnapTurnProviderBase>();
        actionSnapTurn = locomotionSystem.GetComponentInChildren<ActionBasedSnapTurnProvider>();
        
        // หา Continuous Move (Walk)
        continuousMoveProvider = locomotionSystem.GetComponentInChildren<ContinuousMoveProviderBase>();
        actionContinuousMove = locomotionSystem.GetComponentInChildren<ActionBasedContinuousMoveProvider>();
        
        // หา Teleportation
        teleportProvider = locomotionSystem.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
        
        Debug.Log("✅ Found Locomotion Components:");
        Debug.Log($"  - Snap Turn: {(snapTurnProvider != null ? "✓" : "✗")}");
        Debug.Log($"  - Walk: {(continuousMoveProvider != null ? "✓" : "✗")}");
        Debug.Log($"  - Teleport: {(teleportProvider != null ? "✓" : "✗")}");
    }
    
    /// <summary>
    /// ปิด Locomotion ทั้งหมด
    /// </summary>
    void DisableLocomotion()
    {
        Debug.Log("🛑 Disabling Locomotion for Menu...");
        
        // ปิด Snap Turn
        if (disableSnapTurn)
        {
            if (snapTurnProvider != null)
            {
                originalSnapTurnState = snapTurnProvider.enabled;
                snapTurnProvider.enabled = false;
                Debug.Log("  ✓ Snap Turn disabled");
            }
            if (actionSnapTurn != null)
            {
                actionSnapTurn.enabled = false;
            }
        }
        
        // ปิด Walk
        if (disableWalk)
        {
            if (continuousMoveProvider != null)
            {
                originalWalkState = continuousMoveProvider.enabled;
                continuousMoveProvider.enabled = false;
                Debug.Log("  ✓ Walk disabled");
            }
            if (actionContinuousMove != null)
            {
                actionContinuousMove.enabled = false;
            }
        }
        
        // ปิด Teleport
        if (disableTeleport)
        {
            if (teleportProvider != null)
            {
                originalTeleportState = teleportProvider.enabled;
                teleportProvider.enabled = false;
                Debug.Log("  ✓ Teleport disabled");
            }
        }
        
        Debug.Log("✅ Menu Locomotion disabled - Thumbstick ready for card scrolling!");
    }
    
    /// <summary>
    /// เปิด Locomotion กลับ
    /// </summary>
    void EnableLocomotion()
    {
        if (locomotionSystem == null) return;
        
        Debug.Log("🔄 Re-enabling Locomotion...");
        
        // เปิด Snap Turn
        if (disableSnapTurn && snapTurnProvider != null)
        {
            snapTurnProvider.enabled = originalSnapTurnState;
            Debug.Log("  ✓ Snap Turn re-enabled");
        }
        if (actionSnapTurn != null)
        {
            actionSnapTurn.enabled = originalSnapTurnState;
        }
        
        // เปิด Walk
        if (disableWalk && continuousMoveProvider != null)
        {
            continuousMoveProvider.enabled = originalWalkState;
            Debug.Log("  ✓ Walk re-enabled");
        }
        if (actionContinuousMove != null)
        {
            actionContinuousMove.enabled = originalWalkState;
        }
        
        // เปิด Teleport
        if (disableTeleport && teleportProvider != null)
        {
            teleportProvider.enabled = originalTeleportState;
            Debug.Log("  ✓ Teleport re-enabled");
        }
        
        Debug.Log("✅ Locomotion restored!");
    }
    
    /// <summary>
    /// เรียกจากภายนอกเพื่อเปิด/ปิด Locomotion
    /// </summary>
    public void SetLocomotionActive(bool active)
    {
        if (active)
            EnableLocomotion();
        else
            DisableLocomotion();
    }
}