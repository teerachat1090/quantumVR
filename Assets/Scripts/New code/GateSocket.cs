using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class GateSocket : MonoBehaviour
{
    public int socketIndex = 0; //0 by default
    public int qubitIndex = -1;
    public QuantumGate currentGate = null;
    public QuantumGate.inputType inputType = QuantumGate.inputType.Default;
    private XRSocketInteractor socketInteractor;
    private QubitCircuit parentCircuit;

    
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

    // check if space is enough given socket's position
    private bool CheckPlaceAvailible(int numInput)
    {
        GameObject target = null;
        SocketsManager socketsManager = parentCircuit.GetSocketsManager();
        if(target == null)
        {
            Destroy(currentGate.gameObject);
            return false;
        }
        else
        {
            //create target + line + set target value

        }
        return false;
    }

    void OnGatePlaced(SelectEnterEventArgs args)
    {
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();

        if(gate != null) {
            currentGate = gate;
            inputType = currentGate.getGateType();
        }

        int numInput = gate.GetNumInput();
        if(numInput > 1) 
        {
            if(CheckPlaceAvailible(numInput) is false) return;
            gate.socketsManager = parentCircuit.GetSocketsManager();
        }

        updateCircuit(true);
    }

    void OnGateRemoved(SelectExitEventArgs args)
    {   
        currentGate = null;

        if(inputType == QuantumGate.inputType.Single)
        {
            updateCircuit(false);
        }
        
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
