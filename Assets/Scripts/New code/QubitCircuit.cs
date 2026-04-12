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
    public int socketAmount = -1;
    [SerializeField] private InteractionLayerMask defaultInteractionLayer;
    [SerializeField] private GameObject socketPrefab = null;
    [SerializeField] private float space = 0.25f;
    public GateSocket[] gateSockets; 
    private SocketsManager socketsManager;
    private List<QuantumGate> gatesForUnfreeze = new List<QuantumGate>();
    
    private void SetSocketPrefab(int amount)
    {
        if(amount < 1)
        {
            Debug.LogWarning("Warning: integer error. Value less than 1.");
            return;
        }
        if(socketPrefab == null)
        {
            Debug.LogWarning("Warning: No socket prefab to use.");
            return;
        }

        float startZ;
        if(amount == 1) startZ = 0f;
        else            startZ = (amount/2 + (amount%2==0? - 0.5f: 0f)) * space;

        Vector3 position = new Vector3(0f, 0f, startZ);
        
        for(int i=0; i<amount; i++)
        {
            GameObject spawn = Instantiate(socketPrefab, gameObject.transform);
            spawn.transform.localPosition = position;
            //spawn.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
            spawn.name = $"SC{i}";

            var socket = spawn.GetComponent<GateSocket>();
            if(socket is null) {
                Debug.LogWarning("Warning: This missing GateSocket Component!");
                continue;
            }

            socket.qubitIndex = circuitIndex;
            socket.socketIndex = i;

            position.z -= space;
        }
    }

    private void InitSockets()
    {
        SetSocketPrefab(socketAmount);
        gateSockets = GetComponentsInChildren<GateSocket>();
        if(gateSockets is null || gateSockets.Length == 0)
        {
            Debug.LogWarning("Warning: Can't access children gateSocket.");
            return;
        }
        Array.Sort(gateSockets, (a, b) => a.socketIndex.CompareTo(b.socketIndex));
    }

    void Awake()
    {
        socketsManager = GetComponentInParent<SocketsManager>();
        InitSockets();
    }

    void Start()
    {
        
    }

    public SocketsManager GetSocketsManager()
    {
        return socketsManager;
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

    public List<QuantumGate> getListOfGate()
    {
        List<QuantumGate> gateList = new List<QuantumGate>();
        if(gateSockets is null)
        {
            Debug.LogWarning("Warning: gateSockets is empty!");
            return null;
        }

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

    // prevent player from grabbing placed gates
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
