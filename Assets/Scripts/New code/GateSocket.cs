using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class GateSocket : MonoBehaviour
{
    public int socketIndex; //0 by default
    public QuantumGate currentGate = null;
    private XRSocketInteractor socketInteractor;
    private QubitCircuit parentCircuit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentCircuit = GetComponentInParent<QubitCircuit>();

        socketInteractor = GetComponent<XRSocketInteractor>();
        if(socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnGatePlaced);
            socketInteractor.selectExited.AddListener(OnGateRemoved);
        }
    }

    void OnGatePlaced(SelectEnterEventArgs args)
    {
        Debug.Log("📊 Qubit placed!");
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();

        if(gate != null) currentGate = gate;
        
        //update circuit table

        if(gate.gatetype != QuantumGate.inputType.Single)
        {
            //remove additional gates if not single input
            socketInteractor.interactionManager.SelectExit(socketInteractor, args.interactableObject);
            return;
        }
        updateCircuit(true);
    }

    void OnGateRemoved(SelectExitEventArgs args)
    {
        currentGate = null;
        updateCircuit(false);
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
        parentCircuit.updateStatus(currentGate.gateName, socketIndex, isPlaced);
    }

    public QuantumGate getCurrentGate()
    {
        return currentGate;
    }
}
