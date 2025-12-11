using System;
using UnityEngine;
using System.Collections.Generic;

public class CircuitManager : MonoBehaviour
{
    private QubitCircuit[] qubitCircuits; //array of qubit circuits
    private int totalQubits;
    private CircuitToExecute circuitToExport;
    private int[] totalGatesPerQubit; //to track total gates per qubit 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        qubitCircuits = GetComponentsInChildren<QubitCircuit>();
        Array.Sort(qubitCircuits, (a, b) => a.circuitIndex.CompareTo(b.circuitIndex));
        totalQubits = qubitCircuits.Length;
        
    }

    void initSetup()
    {
        totalGatesPerQubit = new int[totalQubits];
        for(int i=0; i<totalQubits; i++)
        {
            totalGatesPerQubit[i] = qubitCircuits[i].getNumberOfGates();
        } //in case of each qubit has different number of gates
    }

    void circuitToExportInit()
    {
        //create circuitToExport structure
    }

    public void updateOverallCircuit(string gateName, int socketIndex, int qubitIndex, bool isPlaced)
    {
        Debug.Log($"📊 CircuitManager: Qubit {qubitIndex} - Socket {socketIndex} - Gate {gateName} - Placed: {isPlaced}");
    }
}

[Serializable]
public class CircuitToExecute
{
    public int qubitAmount;
    public List<Qubit> qubits;
}

[Serializable]
public class Qubit
{
    public int qubitIndex;
    public List<Gate> gates;
}

[Serializable]
public class Gate
{
    string gateName; //Identity gate if none specified
    int targetQubit; //-1 if single qubit gate
}