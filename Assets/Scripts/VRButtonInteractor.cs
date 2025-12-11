using UnityEngine;
using UnityEngine.InputSystem;

public class VRButtonInteractor : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionProperty activateButton; // Input action for button press
    
    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 10f;
    [SerializeField] private LayerMask buttonLayer;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color hitColor = Color.green;
    
    private bool buttonPressed = false;
    
    void Start()
    {
        // Enable the input action - สำคัญมาก!
        if (activateButton.action != null)
        {
            activateButton.action.Enable();
            Debug.Log("✅ Input Action Enabled!");
        }
        else
        {
            Debug.LogError("❌ Activate Button is NOT SET! Please assign it in Inspector!");
        }
        
        // Create line renderer if none exists
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = normalColor;
            lineRenderer.endColor = normalColor;
        }
        
        // Set default button layer to Everything if not set
        if (buttonLayer == 0)
        {
            buttonLayer = ~0;
            Debug.LogWarning("⚠️ Button Layer set to Everything. Consider using a specific layer for better performance.");
        }
        
        Debug.Log($"🎮 VRButtonInteractor Ready! Ray Distance: {rayDistance}m");
    }
    
    void Update()
    {
        // Check if activate button is pressed
        float buttonValue = activateButton.action?.ReadValue<float>() ?? 0f;
        bool isPressed = buttonValue > 0.1f; // ลดความไวลงเล็กน้อย
        
        // Debug: แสดงเมื่อกดปุ่ม
        if (isPressed && !buttonPressed)
        {
            Debug.Log($"🎮 Button Pressed! Value: {buttonValue}");
        }
        
        // Perform raycast
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        bool hitSomething = Physics.Raycast(ray, out hit, rayDistance, buttonLayer);
        
        if (hitSomething)
        {
            // Update line renderer - สีเขียวเมื่อชี้โดน
            UpdateLineRenderer(transform.position, hit.point, true);
            
            // Debug: แสดงว่าชี้โดนอะไร
            //Debug.Log($"🎯 Ray Hit: {hit.collider.gameObject.name} (Distance: {hit.distance:F2}m)");
            
            // Check if button was just pressed and we hit a VR button
            if (isPressed && !buttonPressed)
            {
                VRPhysicalButton button = hit.collider.GetComponent<VRPhysicalButton>();
                if (button != null)
                {
                    Debug.Log($"✅ VRPhysicalButton Found on {hit.collider.gameObject.name}! Activating...");
                    button.PressButton();
                }
                else
                {
                    Debug.LogWarning($"❌ Object '{hit.collider.gameObject.name}' has no VRPhysicalButton script!");
                }
            }
        }
        else
        {
            // Update line renderer - สีฟ้าเมื่อไม่ชี้โดนอะไร
            UpdateLineRenderer(transform.position, transform.position + transform.forward * rayDistance, false);
        }
        
        buttonPressed = isPressed;
    }
    
    private void UpdateLineRenderer(Vector3 start, Vector3 end, bool hitting)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        
        Color color = hitting ? hitColor : normalColor;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
    
    void OnDisable()
    {
        // ปิด input action เมื่อ disable
        if (activateButton.action != null)
        {
            activateButton.action.Disable();
        }
    }
}