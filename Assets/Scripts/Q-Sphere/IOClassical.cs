using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class IOClassical : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionReference thumbstickAction;
    [SerializeField] private float threshold = 0.5f;

    [Header("Text Display")]
    [SerializeField] private TMP_Text display = null;

    public ClassicalBitManager CBManager = null;
    public int maxPosition = 0;
    private XRBaseInteractable interactable;
    private bool isHovered = false;
    private int bitPosition = 0;
    
    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        //Subscribe to Hover Events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);

        //Subscribe to Input Events
        thumbstickAction.action.performed += OnThumbstickMove;
        thumbstickAction.action.canceled += OnThumbstickStop;
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);

        thumbstickAction.action.performed -= OnThumbstickMove;
        thumbstickAction.action.canceled -= OnThumbstickStop;
    }

    private void OnHoverEnter(HoverEnterEventArgs args) => isHovered = true;
    private void OnHoverExit(HoverExitEventArgs args) => isHovered = false;

    /// <summary>
    ///     เปลี่ยน ค่า bit เมื่อชี้แล้วเลื่อน thumbstick ขึ้น/ลง
    /// </summary>
    private void OnThumbstickMove(InputAction.CallbackContext context)
    {
        if (!isHovered) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.y >= threshold)       DoScrollUp();
        
        else if (input.y <= -threshold) DoScrollDown();
        
    }

    private void OnThumbstickStop(InputAction.CallbackContext context)
    {
        // Handle logic for when the thumbstick returns to zero if needed
    }

    /// <summary>
    ///     เปลี่ยนจำนวนเต็ม เป็นเลขห้อยตัวแปร
    /// </summary>
    private string IntToUnderscript(int val)
    {
        if(val == 0) return "\u2080";

        string output = "";    
        string str = val.ToString();
        foreach (char c in str)
        {
            int code = 2080 + (c - '0');
            output += $"\\u{code}";
        }
        
        return output;
    }

    private void UpdateDisplay()
    {
        if(display == null)
        {
            Debug.LogWarning("Display text on classical point is missing");
            return;
        }
        string underscript = IntToUnderscript(bitPosition);
        display.text = "b"+underscript;
    }

    private void TellCBManagerToUpdate()
    {
        if(CBManager == null)
        {
            Debug.LogWarning("Warning: ClassicalBitManager Reference is missing in IOClassical.");
            return;
        }
        CBManager.UpdateBitPositionToCircuit();
    }

    private void DoScrollUp(){
        if(bitPosition == maxPosition) return;

        bitPosition++;
        UpdateDisplay();
        TellCBManagerToUpdate();
        Debug.Log("Event: Thumbstick UP!");
    }

    private void DoScrollDown(){
        if(bitPosition == 0) return;

        bitPosition--;
        UpdateDisplay();
        TellCBManagerToUpdate();
        Debug.Log("Event: Thumbstick DOWN!");
    }

    public int GetBitPosition()
    {
        return bitPosition;
    }
}
