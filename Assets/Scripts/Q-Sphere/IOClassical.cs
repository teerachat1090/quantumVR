using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class IOClassical : MonoBehaviour
{
    // IT WORK!!!!!!!!!!!!!!! HOW!!!!!!!!!!!!!!
    [Header("Input Settings")]
    [SerializeField] private InputActionReference thumbstickAction;
    [SerializeField] private float threshold = 0.5f;

    [Header("Text Display")]
    [SerializeField] private TMP_Text display = null;

    private XRBaseInteractable interactable;
    private bool isHovered = false;
    private int bitPosition = 0;
    public int maxPosition = 0;

    public ClassicalBitManager CBManager = null;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        // 1. Subscribe to Hover Events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);

        // 2. Subscribe to Input Events
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

    private void OnThumbstickMove(InputAction.CallbackContext context)
    {
        // Only execute if the player is currently hovering over THIS object
        if (!isHovered) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.y >= threshold)
        {
            DoScrollUp();
        }
        else if (input.y <= -threshold)
        {
            DoScrollDown();
        }
    }

    private void OnThumbstickStop(InputAction.CallbackContext context)
    {
        // Handle logic for when the thumbstick returns to zero if needed
    }

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

    void DoScrollUp(){
        if(bitPosition == maxPosition) return;

        bitPosition++;
        UpdateDisplay();
        Debug.Log("Event: Thumbstick UP!");
    }
    void DoScrollDown(){
        if(bitPosition == 0) return;

        bitPosition--;
        UpdateDisplay();
        Debug.Log("Event: Thumbstick DOWN!");
    }

    public int GetBitPosition()
    {
        return bitPosition;
    }
}
