using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class IOClassical : MonoBehaviour
{
    // IT WORK!!!!!!!!!!!!!!! HOW!!!!!!!!!!!!!!
    [Header("Input Settings")]
    public InputActionReference thumbstickAction;
    public float threshold = 0.5f;

    private XRBaseInteractable interactable;
    private bool isHovered = false;

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

    void DoScrollUp() => Debug.Log("Event: Thumbstick UP!");
    void DoScrollDown() => Debug.Log("Event: Thumbstick DOWN!");
}
