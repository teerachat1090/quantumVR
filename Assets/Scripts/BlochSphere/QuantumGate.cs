using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class QuantumGate : MonoBehaviour
{
    [Header("Gate Info")]
    [SerializeField] private string gateName; // H, X, Y, Z, CNOT, etc.
    public enum inputType{Single, Double, Triple, target, measure, condition, Default}; 
    [SerializeField] private inputType gatetype;
    [SerializeField] private string gateDescription; // (Optional)
    
    private CircuitSocket currentSocket; // not used
    private XRGrabInteractable grabInteractable;
    private XRSocketInteractor socketInteractor;

    [Header("Multi-input Gate Info")]
    public bool friendExist = false, beingDestroyed = false;
    public bool isController = false;
    public GateSocket socket = null;
    public MultiInputGateConnect connect = null;

    [Header("Input Display")]
    [SerializeField] private bool useInput = false;
    [SerializeField] private TMP_Text nameText = null;
    [SerializeField] private TMP_Text displayText = null;
    [SerializeField] private InputActionReference leftThumbstickAction;
    [SerializeField] private InputActionReference rightThumbstickAction;
    [SerializeField] private float textSlideOffset = 5f;
    [SerializeField] private float threshold = 0.4f;
    [SerializeField] private float cooldownTime = .3f;
    private InputActionReference activeThumbstick;
    private int phaseAngle = 0, angleStep = 15; //degree
    private float lastTimeStamp = 0f;

    public void setConditionSocket(bool state)
    {
        if(socketInteractor == null) return;
        socketInteractor.enabled = state;
    }

    void CheckComponent()
    {
        socketInteractor = GetComponent<XRSocketInteractor>();
        if(socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnGatePlaced);
        }
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    /// <summary>
    ///     Change gate property to rely on classical bit (condition) 
    /// </summary>
    /// <param name="args"></param>
    void OnGatePlaced(SelectEnterEventArgs args)
    {
        GameObject trigger = args.interactableObject.transform.gameObject;
        Destroy(trigger);

        if(socket == null) return;

        gatetype = inputType.condition;
        bool completed = socket.RegistClassicalRelated(gameObject);
        if(!completed) {
            gatetype = inputType.Single;
            return;
        }
        
        socketInteractor.enabled = false;
    }

    public void AdjustInputText()
    {
        if (!useInput) return;
        if(nameText == null || displayText == null)
        {
            Debug.LogWarning("Warning: name text or display text is missing. Can't adjust text.");
            return;
        }

        nameText.rectTransform.anchoredPosition += new Vector2(0f, textSlideOffset);
        displayText.gameObject.SetActive(true);
    }

    void Awake()
    {
        CheckComponent();

        if(useInput && displayText == null)
        {
            Debug.LogWarning("Warning: This gate use input but not assign display text.");
        }
    }

    void Start()
    {
        if (string.IsNullOrEmpty(gateName)) gateName = gameObject.name;
        if (string.IsNullOrEmpty(gateDescription)) gateDescription = $"{gateName} Gate";
    }

    public void doDestroy()
    {
        if(connect != null)
        {
            connect.deleteItself();
            return;
        }

        if(gatetype == inputType.Single) {
            Debug.Log("Delete single input");
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    //-----------------------------Input feature-------------------------------------
    public bool DoesUseInput()
    {
        return useInput;
    }

    public int GetInputData()
    {
        return phaseAngle;
    }

    public void SetInputFeature(bool enable)
    {
        if(!useInput) return;

        if(enable){
            grabInteractable.hoverEntered.AddListener(OnHoverEnter);
            grabInteractable.hoverExited.AddListener(OnHoverExit);
        }
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

    private void OnThumbstickMove(InputAction.CallbackContext context)
    {
        if(Time.time - lastTimeStamp < cooldownTime) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.y >= threshold)       {
            lastTimeStamp = Time.time;
            UpdatePhase();
            }
        else if (input.y <= -threshold) {
            lastTimeStamp = Time.time;
            UpdatePhase(increase: false);
            }
    }

    private void UpdatePhase(bool increase = true)
    {
        if (increase)
        {
            phaseAngle += angleStep;
            if(phaseAngle >= 360) phaseAngle -= 360;
        }
        else
        {
            phaseAngle -= angleStep;
            if(phaseAngle < 0) phaseAngle += 360;
        }
        UpdateInputText();

        if(socket != null)
        {
            socket.updateCircuit(false);
            Debug.Log("Updating when display changed.");
        }
    }

    private void UpdateInputText()
    {
        if(displayText == null)
        {
            Debug.LogWarning("Warning: displaytext is missing. Unable to update text.");
            return;
        }
  
        displayText.text = phaseAngle.ToString() + "\u00B0";
    }
    //----------------------------------------------------------------------------------

    //-------------- Not used ---------------------------
    public void SetCurrentSocket(CircuitSocket socket)  {currentSocket = socket;}
    public CircuitSocket GetCurrentSocket() {return currentSocket;}
    //-------------- Not used ---------------------------

    public int getTarget()  {return 0;}

    public string getGateName() {return gateName;}

    public string getGateDescription()  {return gateDescription;}

    public inputType getGateType()  {return gatetype; }

    void OnDestroy()
    {
        if(socketInteractor != null) socketInteractor.selectEntered.RemoveListener(OnGatePlaced);
        
        grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
        grabInteractable.hoverExited.RemoveListener(OnHoverExit);
        if (activeThumbstick != null)
        {
            activeThumbstick.action.performed -= OnThumbstickMove;
        }
    }
}