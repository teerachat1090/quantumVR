using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GateSocket : MonoBehaviour
{
    public int socketIndex = 0; //0 by default
    public int qubitIndex = -1;
    public QuantumGate currentGate = null;
    private XRSocketInteractor socketInteractor;
    private QubitCircuit parentCircuit;
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

    void OnGatePlaced(SelectEnterEventArgs args)
    {
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();

        if(gate == null) return;

        currentGate = gate;
        gate.socket = this;
        Debug.Log("asigning socket...");

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
        
        //------------------------ Classical bit related --------------------
        var socketsManager = parentCircuit.GetSocketsManager();
        if(inputType == QuantumGate.inputType.measure)
        {
            Debug.Log("checling about measurement");
            bool pathAvailible = socketsManager.CheckPathToClassicalBit(gate.gameObject, qubitIndex, socketIndex);
            if (!pathAvailible)
            {
                Debug.LogWarning("There gate block path downward.");
                Destroy(gate.gameObject);
            }
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

            gate.connect.ToggleCurrentColumn(doLock: false);
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
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();        
        currentGate = null;
        gate.socket = null;

        int numInput = gate.GetNumInput();
        if(numInput == 1)
        {
            updateCircuit(false);
            return;
        } 

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

    void updateCircuit(bool isPlaced)
    {
        parentCircuit.updateStatus((currentGate != null) ? currentGate.getGateName(): null, socketIndex, isPlaced);
    }

    public QuantumGate getCurrentGate()
    {
        return currentGate;
    }

    public void setQubitIndex(int index)
    {
        socketIndex = index;
    }
}
