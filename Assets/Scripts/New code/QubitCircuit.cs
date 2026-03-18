using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// For parent object that have sockets as childs object
public class QubitCircuit : MonoBehaviour
{
    private bool isEnabled = true;
    public int circuitIndex = 0; //0 by default
    public int socketAmount = 0;
    [SerializeField] private InteractionLayerMask defaultInteractionLayer;
    [SerializeField] private GameObject socketPrefab = null;
    [SerializeField] private float space = 0.25f;
    private GateSocket[] gateSockets; 
    private SocketsManager socketsManager;
    private List<QuantumGate> gatesForUnfreeze = new List<QuantumGate>();
    
    private void SocketPrefabInit(int amount)
    {
        if(amount < 1)
        {
            Debug.LogWarning("Warning: integer error. Value less than 1.");
            return;
        }
        if(socketPrefab is null)
        {
            Debug.LogWarning("Warning: No socket prefab to use.");
            return;
        }

        float startZ;
        if(amount == 1) startZ = 0f;
        else            startZ = (amount/2 + (amount%2==0? - 0.5f: 0f)) * space;

        Quaternion rotation = Quaternion.Euler(90f, 90f, 0f);
        Vector3 position = new Vector3(0f, 0f, startZ);
        
        for(int i=0; i<amount; i++)
        {
            GameObject spawn = Instantiate(socketPrefab, gameObject.transform);
            spawn.transform.localPosition = position;
            spawn.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
            spawn.name = $"SC{i}";
            position.z -= space;
        }
    }

    void Awake()
    {
        SocketPrefabInit(socketAmount);

        socketsManager = GetComponentInParent<SocketsManager>();
        gateSockets = GetComponentsInChildren<GateSocket>();
        if(gateSockets is null || gateSockets.Length == 0)
        {
            Debug.LogWarning("Warning: Can't access children gateSocket.");
            return;
        }
        Array.Sort(gateSockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));
    }

    void Start()
    {
        foreach(GateSocket socket in gateSockets)
        {
            socket.qubitIndex = circuitIndex;
        }
    }

    public SocketsManager GetSocketsManager()
    {
        return socketsManager;
    }

    public GameObject CheckIfSocketEmpty(int index)
    {
        if(gateSockets[index].getCurrentGate() is null) return gateSockets[index].gameObject;
        else return null;
    }

    // find empty socket in other qubit in the same column
    public GameObject FindEmptySocketInColumn(int wantIndex, out int qubitIndex)
    {
        GameObject target = socketsManager.SearchForAvailibleSocketByIndex(circuitIndex, wantIndex, out int qubit_index);
        qubitIndex = qubit_index;
        return target;
        
    }

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
