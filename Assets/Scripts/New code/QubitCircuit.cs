using System;
using UnityEngine;

public class QubitCircuit : MonoBehaviour
{
    private bool isEnabled = true;
    public int circuitIndex; //0 by default
    private GateSocket[] gateSockets; 
    private CircuitManager parentManager;
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
}
