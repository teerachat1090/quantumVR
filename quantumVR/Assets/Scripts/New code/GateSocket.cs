using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.VisualScripting;


public class GateSocket : MonoBehaviour
{
    public int socketIndex = 0; //0 by default
    public static event System.Action<string> OnAnyGateRemoved;
    public int qubitIndex = -1;
    public QuantumGate currentGate = null;
    private XRSocketInteractor socketInteractor;
    private QubitCircuit parentCircuit;
    public static event System.Action<string> OnAnyGatePlaced;
    public static event System.Action OnMeasurePlaced;
     public static event System.Action OnIfHappend;
    public bool beLazy = false; 
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void CheckComponent()
    {
        parentCircuit = GetComponentInParent<QubitCircuit>();

        socketInteractor = GetComponent<XRSocketInteractor>();
        if(socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnGatePlaced);
            socketInteractor.selectExited.AddListener(OnGateRemoved);
        } else
        {
            Debug.LogWarning("Warning: there's no socket interactor here!");
        }
    }

    void Awake()
    {
        CheckComponent();
    }

    private XRGrabInteractable GetGrabInteractable(QuantumGate gate)
    {
        var grabInteractable = gate.GetComponent<XRGrabInteractable>();
        if(grabInteractable == null){
            Debug.LogWarning("Warning: component is missing (XRGrabInteractable).");   
            return null;
        }

        return grabInteractable;
    }

    public bool RegistClassicalRelated(GameObject baseObject)
    {
        var socketsManager = parentCircuit.GetSocketsManager();
        if(!socketsManager.useClassical) return false;

        Debug.Log("checking about measurement");
        bool pathAvailible = socketsManager.CheckPathToClassicalBit(baseObject, qubitIndex, socketIndex);
        if (!pathAvailible)
        {
            Debug.LogWarning("There gate block path downward.");
            return false;
        }
        OnIfHappend?.Invoke();
        return true;
    }

    void OnGatePlaced(SelectEnterEventArgs args)
    {
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();

        if(gate == null) return;

        currentGate = gate;
        gate.socket = this;

        OnAnyGatePlaced?.Invoke(gate.getGateName()); // เพิ่มบรรทัดนี้
        Debug.Log("asigning socket...");

         if(gate.getGateType() == QuantumGate.inputType.measure)
            OnMeasurePlaced?.Invoke();

        if (gate.DoesUseInput())
        {
            gate.SetInputFeature(true);
        }

        if(beLazy)
        { 
            beLazy = false;
            return;
        }

        var inputType = gate.getGateType();
        if(inputType == QuantumGate.inputType.Single)
        {
            updateCircuit(true);
            return;
        }

        var socketsManager = parentCircuit.GetSocketsManager();

        //------------------------ Classical bit related --------------------
        if(inputType == QuantumGate.inputType.measure && gate.connect == null)
        {
            bool completed = RegistClassicalRelated(gate.gameObject);
            if(!completed) Destroy(gate.gameObject);
            return;
        }
        //---------------------------------------------------------------------

        //----------------------- multiple input gate -------------------------
        Debug.Log("checking about multi-input gate");
        if (gate.friendExist) //unlock layer of placed gate
        {
            var grabInteractable = GetGrabInteractable(gate);
            if(grabInteractable == null) return;
            grabInteractable.interactionLayers = socketsManager.GetQuantumGateLayer();

            //disable socket inbetween
            gate.connect.ToggleCurrentColumn(doLock: false);
            gate.connect.SetSocketInBetween(doEnable: false);
            updateCircuit(true);
            return;
        }
        else //introduce new gate to socket manager
        {
            bool spaceAvailible = socketsManager.ChecksocketSpace(gate.gameObject, qubitIndex, socketIndex);
            if (!spaceAvailible)
            {
                Debug.LogWarning("Space Not Availible.");
                Destroy(gate.gameObject);
            }
        }
    }

    void OnGateRemoved(SelectExitEventArgs args)
    {   
        if(!Application.isPlaying) return;
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();        
        currentGate = null;
        gate.socket = null;
        OnAnyGateRemoved?.Invoke(gate.getGateName()); // เพิ่มตรงนี้

        if (gate.DoesUseInput())
        {
            gate.SetInputFeature(false);
        }

        if(gate.getGateType() == QuantumGate.inputType.Single)
        {
            updateCircuit(false);
            return;
        } 

        if(gate.connect == null) return;

        // enable socket inbetween
        gate.connect.SetSocketInBetween(useTempVal: true);

        // Remove assigned multi-input gate
        if(gate.friendExist && !gate.beingDestroyed)
        {
            var socketsManager = parentCircuit.GetSocketsManager();

            if(gate.connect == null)
            {
                Debug.LogWarning("Warning: Can't find connector between multi-input gates.");
                return;
            }

            var grabInteractable = GetGrabInteractable(gate);
            if(grabInteractable == null) return;
            grabInteractable.interactionLayers = socketsManager.GetLockLayer();

            gate.connect.ToggleCurrentColumn();
        }
        
        //case gate.beingDestroyed = true -> gate will manage that
    }

    void OnDestroy()
    {
        if(socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnGatePlaced);
            socketInteractor.selectExited.RemoveListener(OnGateRemoved);
        }
    }

    public void updateCircuit(bool isPlaced)
    {
        parentCircuit.updateStatus((currentGate != null) ? currentGate.getGateName(): null, socketIndex, isPlaced);
    }

    public QuantumGate getCurrentGate()
    {
        return currentGate;
    }
}
