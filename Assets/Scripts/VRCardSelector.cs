using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Component เสริมสำหรับเลือกการ์ดด้วย Trigger
/// </summary>
public class VRCardSelector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก VRCardScroller มาใส่")]
    public VRCardScroller cardScroller;
    
    [Header("Input")]
    public XRNode controllerNode = XRNode.RightHand;
    
    private InputDevice controller;
    private bool lastTriggerState = false;
    
    void Start()
    {
        // Auto-find ถ้าไม่ได้กำหนด
        if (cardScroller == null)
        {
            cardScroller = GetComponent<VRCardScroller>();
        }
    }
    
    void Update()
    {
        // อัปเดต Controller ทุกเฟรม
        RefreshController();
        
        // ตรวจสอบว่า Controller พร้อมหรือไม่
        if (!controller.isValid)
        {
            return;
        }
        
        // อ่านค่า Trigger
        bool triggerPressed = false;
        controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        
        // ตรวจจับขณะกดลง (Edge Detection)
        if (triggerPressed && !lastTriggerState)
        {
            SelectCurrentCard();
        }
        
        lastTriggerState = triggerPressed;
        
        // ทดสอบด้วย Spacebar
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SelectCurrentCard();
        }
        #endif
    }
    
    /// <summary>
    /// อัปเดต Controller ให้แน่ใจว่าพร้อมใช้งาน
    /// </summary>
    void RefreshController()
    {
        if (!controller.isValid)
        {
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
            
            // Debug เพื่อดูสถานะ
            if (controller.isValid)
            {
                Debug.Log($"✅ Selector Controller connected: {controller.name}");
            }
        }
    }
    
    void SelectCurrentCard()
    {
        if (cardScroller == null)
        {
            Debug.LogWarning("⚠️ VRCardScroller is not assigned!");
            return;
        }
        
        int selectedIndex = cardScroller.GetCurrentIndex();
        var selectedCard = cardScroller.GetCurrentCard();
        
        Debug.Log($"✅ Selected: Card {selectedIndex + 1}");
        
        // เล่น Haptic แรงขึ้น
        if (controller.isValid)
        {
            controller.SendHapticImpulse(0, 0.8f, 0.2f);
        }
        
        // เรียก Function ตาม Card ที่เลือก
        LoadSceneForCard(selectedIndex);
    }
    
    void LoadSceneForCard(int index)
    {
        switch (index)
        {
            case 0:
                Debug.Log("🚀 Loading: Qubit State & Bloch Sphere");
                // UnityEngine.SceneManagement.SceneManager.LoadScene("QubitScene");
                break;
                
            case 1:
                Debug.Log("🚀 Loading: Entanglement");
                // UnityEngine.SceneManagement.SceneManager.LoadScene("EntanglementScene");
                break;
                
            case 2:
                Debug.Log("🚀 Loading: Quantum Teleportation");
                // UnityEngine.SceneManagement.SceneManager.LoadScene("TeleportationScene");
                break;
                
            default:
                Debug.LogWarning($"⚠️ Unknown card index: {index}");
                break;
        }
    }
}