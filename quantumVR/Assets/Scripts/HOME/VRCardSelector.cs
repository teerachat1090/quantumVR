using UnityEngine;
using UnityEngine.XR;

public class VRCardSelector : MonoBehaviour
{
    [Header("References")]
    public VRCardScroller cardScroller;
    
    [Header("Input")]
    public XRNode controllerNode = XRNode.RightHand;
    
    private InputDevice controller;
    private bool lastTriggerState = false;
    
    void Start()
    {
        if (cardScroller == null)
            cardScroller = GetComponent<VRCardScroller>();
    }
    
    void Update()
    {
        RefreshController();
        if (!controller.isValid) return;
        
        bool triggerPressed = false;
        controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        
        if (triggerPressed && !lastTriggerState)
            SelectCurrentCard();
        
        lastTriggerState = triggerPressed;
        
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
            SelectCurrentCard();
        #endif
    }
    
    void RefreshController()
    {
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
    }
    
    void SelectCurrentCard()
    {
        if (cardScroller == null)
        {
            Debug.LogWarning("⚠️ VRCardScroller is not assigned!");
            return;
        }

        if (controller.isValid)
            controller.SendHapticImpulse(0, 0.8f, 0.2f);

        cardScroller.ConfirmSelection();
    }
}