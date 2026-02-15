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
    private List<QuantumGate> quantumGates = new List<QuantumGate>();
    void Start()
    {
        parentManager = GetComponentInParent<CircuitManager>();
        gateSockets = GetComponentsInChildren<GateSocket>();
        Array.Sort(gateSockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));
    }

    // Update is called once per frame
    public void updateStatus(string gateName, int socketIndex, bool isPlaced)
    {
        if(!isEnabled) return;
        parentManager.updateOverallCircuit(gateName, socketIndex, circuitIndex, isPlaced);
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
                quantumGates.Add(quantumGate);
                XRGrabInteractable quantumGateInteractable = quantumGate.GetComponent<XRGrabInteractable>();
                quantumGateInteractable.interactionLayers = flag ? 0 : defaultInteractionLayer;
            }
        }
        else
        {
            foreach(QuantumGate quantumGate in quantumGates)
            {
                XRGrabInteractable quantumGateInteractable = quantumGate.GetComponent<XRGrabInteractable>();
                quantumGateInteractable.interactionLayers = flag ? 0 : defaultInteractionLayer;
            }
            quantumGates.Clear();
        }
        
        toggleCircuit();
    }
}
