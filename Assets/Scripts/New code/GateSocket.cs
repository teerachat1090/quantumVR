using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GateSocket : MonoBehaviour
{
    public int socketIndex = 0; //0 by default
    public int qubitIndex = -1;
    public QuantumGate currentGate = null;
    public QuantumGate.inputType inputType = QuantumGate.inputType.Default;
    private XRSocketInteractor socketInteractor;
    private QubitCircuit parentCircuit;
    public bool beLazy = false; 
    private MultiInputGateConnect connect = null;

    
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

    void OnGatePlaced(SelectEnterEventArgs args)
    {
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();

        if(gate != null) {
            currentGate = gate;
            inputType = currentGate.getGateType();
        }

        if(beLazy)
        { 
            beLazy = false;
            return;
        }

        int numInput = gate.GetNumInput();
        if(numInput > 1) 
        {
            if (gate.friendExist) //unlock layer given connector
            {
                var socketsManager = parentCircuit.GetSocketsManager();
                //unlocking
                return;
            }
            else //introduce new gate to socket manager
            {
                var socketsManager = parentCircuit.GetSocketsManager();
                bool spaceAvailible = socketsManager.LookSocketMap(gate.gameObject, qubitIndex, socketIndex);
                if (!spaceAvailible)
                {
                    Debug.Log("Space Not Availible.");
                    Destroy(gate.gameObject);
                }
            }
            return;
        }

        updateCircuit(true);
    }

    void OnGateRemoved(SelectExitEventArgs args)
    {   
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();        
        currentGate = null;

        int numInput = gate.GetNumInput();
        if(numInput == 1)
        {
            updateCircuit(false);
            return;
        } 

        if(gate.friendExist && !gate.beingDestroyed)
        {
            var socketsManager = parentCircuit.GetSocketsManager();
            socketsManager.LockMultiGate(gate.connect);
        }
        //num input > 1

        
        // updating multi input gate => socketsManager's task
            // => make gate tell connecter instead
            //change 'gate' layer to 'lock'
            //change space in column layer to 'lock'
            //lock socket for column
        

        //update circuit table
        
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
