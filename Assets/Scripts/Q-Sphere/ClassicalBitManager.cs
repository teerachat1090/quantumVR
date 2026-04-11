using System;
using System.Collections.Generic;
using UnityEngine;

public class ClassicalBitManager : MonoBehaviour
{
    public int circuitIndex = 0; //0 by default
    public int socketAmount = -1;
    [SerializeField] private GameObject ClassicalIOPrefab = null;
    [SerializeField] private float space = 0.25f;

    private SocketsManager socketsManager = null;
    private List<ClassicalBitPoint> classicalSocketList = new List<ClassicalBitPoint>();

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
            spawn.name = $"CBSC{i}";

            var bit = new ClassicalBitPoint{
                IOPoint = spawn
            };
            classicalSocketList.Add(bit);

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
        ClassicalBitPoint socket = classicalSocketList[index];
        socket.IOPoint.SetActive(true);
    }

    public GameObject GetSocketByCol(int index)
    {
        if(index < 0 && index >= socketAmount) return null;
        ClassicalBitPoint socket = classicalSocketList[index];
        return socket.IOPoint;
    }

    public int GetTargetClassicalBit(int index)
    {
        if(index < 0 && index >= socketAmount) return -1;
        ClassicalBitPoint socket = classicalSocketList[index];
        return socket.bitFocus;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Serializable]
    public class ClassicalBitPoint
    {
        public int bitFocus = 0;
        public GameObject IOPoint;
    }
}
