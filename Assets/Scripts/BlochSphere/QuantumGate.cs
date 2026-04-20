using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
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
    
    private CircuitSocket currentSocket;
    private XRSocketInteractor socketInteractor;

    [Header("Multi-input Gate Info")]
    public bool friendExist = false, beingDestroyed = false;
    public bool isController = false;
    public GateSocket socket = null;
    public MultiInputGateConnect connect = null;

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
    }

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

    void Awake()
    {
        CheckComponent();
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
        
    }
}