using System;
using System.Collections.Generic;
using UnityEngine;

public class ClassicalBitManager : MonoBehaviour
{
    [SerializeField] private GameObject ClassicalIOPrefab = null;
    [SerializeField] private float space = 0.25f;

    public int socketAmount = -1;
    public int maxBitPosition = -1;
    
    private SocketsManager socketsManager = null;
    private List<IOClassical> classicalSocketList = new List<IOClassical>();

    private void SetSocketPrefab(int amount)
    {
        if(amount < 1)
        {
            Debug.LogWarning("Warning: integer error. Value less than 1.");
            return;
        }
        if(ClassicalIOPrefab == null)
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
            GameObject spawn = Instantiate(ClassicalIOPrefab, gameObject.transform);
            spawn.transform.localPosition = position;
            spawn.transform.localRotation = Quaternion.Euler(0, 90, 0);
            spawn.name = $"CBSC{i}";

            var IOBit = spawn.GetComponent<IOClassical>();
            if(IOBit!=null) {
                IOBit.index = i;
                IOBit.CBManager = this;
                IOBit.maxPosition = maxBitPosition;
                classicalSocketList.Add(IOBit);
            }

            position.z -= space;
        }
    }

    private void InitSockets()
    {
        SetSocketPrefab(socketAmount);
    }

    void Awake()
    {
        socketsManager = GetComponentInParent<SocketsManager>();
        InitSockets();
    }

    public void ShowPointByCol(int index)
    {
        if(index < 0 && index >= socketAmount) return;
        classicalSocketList[index].gameObject.SetActive(true);
    }

    public GameObject GetSocketByCol(int index)
    {
        if(index < 0 && index >= socketAmount) return null;
        return classicalSocketList[index].gameObject;
    }

    public int GetTargetClassicalBit(int index)
    {
        if(index < 0 && index >= socketAmount) return -1;
        IOClassical socket = classicalSocketList[index];
        return socket.GetBitPosition();
    }

    public static event System.Action OnClassicalBitChanged;

    public void UpdateBitPositionToCircuit(int cBitIndex)
    {
        OnClassicalBitChanged?.Invoke();
        Debug.LogWarning($"bit change to {GetTargetClassicalBit(cBitIndex)}");
        if(socketsManager == null)
        {
            Debug.LogWarning("Warning: SocketsManager Reference is missing");
            return;
        }

        socketsManager.UpdateClassicalBit(cBitIndex);
    }

    public void SetClassicalBit(bool enable)
    {
        foreach(IOClassical cBit in classicalSocketList)
        {
            cBit.SetInteractable(enable);
        }
    }
}
