using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class IOClassical : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionReference leftThumbstickAction;
    [SerializeField] private InputActionReference rightThumbstickAction;
    [SerializeField] private float threshold = 0.4f;
    [SerializeField] private float cooldownTime = .3f;
    private float lastTimeStamp = 0f;

    private InputActionReference activeThumbstick;

    [Header("Text Display")]
    [SerializeField] private TMP_Text display = null;

    public ClassicalBitManager CBManager = null;
    public int maxPosition = 0;
    public int index = 0;
    private XRBaseInteractable interactable = null;
    private int bitPosition = 0;
    
    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        //Subscribe to Hover Events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    public void SetInteractable(bool enable)
    {
        if(interactable == null) return;

        interactable.enabled = enable;
    }

    private bool IsLeftController(IXRInteractor interactor)
    {
        // Match by GameObject name (default XR Rig naming)
        string name = interactor.transform.name.ToLower();
        return name.Contains("left");
    }

    private void OnHoverEnter(HoverEnterEventArgs args)  
    {
        // Identify which controller is hovering
        activeThumbstick = IsLeftController(args.interactorObject) 
            ? leftThumbstickAction 
            : rightThumbstickAction;

        // Subscribe only that controller's thumbstick
        activeThumbstick.action.performed += OnThumbstickMove;
    }
    
    private void OnHoverExit(HoverExitEventArgs args) 
    {
        if (activeThumbstick != null)
        {
            activeThumbstick.action.performed -= OnThumbstickMove;
            activeThumbstick = null;
        }

        Debug.Log("Hover exited — thumbstick unsubscribed");
    }

    void OnDestroy()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);

        if (activeThumbstick != null)
        {
            activeThumbstick.action.performed -= OnThumbstickMove;
            //activeThumbstick.action.canceled  -= OnThumbstickStop;
        }
    }

    /// <summary>
    ///     เปลี่ยน ค่า bit เมื่อชี้แล้วเลื่อน thumbstick ขึ้น/ลง
    /// </summary>
    private void OnThumbstickMove(InputAction.CallbackContext context)
    {
        if(Time.time - lastTimeStamp < cooldownTime) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.y >= threshold)       {
            lastTimeStamp = Time.time;
            DoScrollUp();
            }
        else if (input.y <= -threshold) {
            lastTimeStamp = Time.time;
            DoScrollDown();
            }
    }

    private void DoScrollUp(){
        if(bitPosition == maxPosition) return;
        //Debug.Log($"DoScrollUp");
        bitPosition++;
        UpdateDisplay();
        TellCBManagerToUpdate();
        //Debug.Log("Event: Thumbstick UP!");
    }

    private void DoScrollDown(){
        if(bitPosition == 0) return;
        //Debug.Log($"DoScrollDown");
        bitPosition--;
        UpdateDisplay();
        TellCBManagerToUpdate();
        //Debug.Log("Event: Thumbstick DOWN!");
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
        CBManager.UpdateBitPositionToCircuit(index);
    }

    public int GetBitPosition()
    {
        return bitPosition;
    }
}
