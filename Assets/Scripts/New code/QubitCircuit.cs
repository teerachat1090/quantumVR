using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// For parent object that have sockets as childs object
public class QubitCircuit : MonoBehaviour
{
    private bool isEnabled = true;
    public int circuitIndex = 0; //0 by default
    public InteractionLayerMask defaultInteractionLayer;
    private GateSocket[] gateSockets; 
    private CircuitManager parentManager;
    private SocketsManager socketsManager;
    private List<QuantumGate> gatesForUnfreeze = new List<QuantumGate>();
    void Awake()
    {
        parentManager = GetComponentInParent<CircuitManager>();
        socketsManager = GetComponentInParent<SocketsManager>();
        gateSockets = GetComponentsInChildren<GateSocket>();
        Array.Sort(gateSockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));

        Debug.Log("Start function finished");
    }

    // Update is called once per frame
    public void updateStatus(string gateName, int socketIndex, bool isPlaced)
    {
        if(!isEnabled) return;
        socketsManager.updateCircuitByJson(gateName, socketIndex, circuitIndex, isPlaced);
    }

    void toggleCircuit()
    {
        isEnabled = !isEnabled;
    }

    public int getNumberOfGates()
    {
        return gateSockets.Length;
    }

    public List<QuantumGate> getListOfGate()
    {
        List<QuantumGate> gateList = new List<QuantumGate>();
        foreach(GateSocket gateSocket in gateSockets)
        {
            gateList.Add(gateSocket.getCurrentGate());
        }
        return gateList;
    }

    // get position of Nth gate
    public Vector3 GetNthGatePos(int rank)
    {
        int count = 0;
        foreach(GateSocket socket in gateSockets)
        {
            if(socket.getCurrentGate() is null) continue;

            count++;
            if(count == rank)
            {
                return socket.transform.position;
            }
        }

        return Vector3.zero;
    }

    public void FreezeGateBlock(bool flag)
    {
        toggleCircuit();

        if (flag)
        {
            foreach(GateSocket socket in gateSockets)
            {
                QuantumGate quantumGate = socket.getCurrentGate();
                if(quantumGate is null) {
                    continue;
                }
                gatesForUnfreeze.Add(quantumGate);
                XRGrabInteractable quantumGateInteractable = quantumGate.GetComponent<XRGrabInteractable>();
                quantumGateInteractable.interactionLayers = InteractionLayerMask.GetMask("Default");
            }
        }
        else
        {
            foreach(QuantumGate quantumGate in gatesForUnfreeze)
            {
                XRGrabInteractable quantumGateInteractable = quantumGate.GetComponent<XRGrabInteractable>();
                quantumGateInteractable.interactionLayers = defaultInteractionLayer;
            }
            gatesForUnfreeze.Clear();
        }
        
        toggleCircuit();
    }


}
