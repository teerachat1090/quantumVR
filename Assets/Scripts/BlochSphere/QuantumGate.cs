using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class QuantumGate : MonoBehaviour
{
    [Header("Gate Info")]
    [SerializeField] private string gateName; // H, X, Y, Z, CNOT, etc.
    public enum inputType{Default, Single, Double, Triple, target}; 
    [SerializeField] private inputType gatetype;
    [SerializeField] private string gateDescription; // (Optional)
    
    public SocketsManager socketsManager = null;
    private CircuitSocket currentSocket;
    public bool friendExist = false; //for multi-input gate
    
    void Start()
    {
        if (string.IsNullOrEmpty(gateName)) gateName = gameObject.name;
        if (string.IsNullOrEmpty(gateDescription)) gateDescription = $"{gateName} Gate";
    }

    void OnDestroy()
    {
        if(gatetype == inputType.Single) return;

        // Multiple input gate case:
        // Reject new gate case
        if(socketsManager is null) return;

        // check if directly destroy case

        //Delete existed group case
        //access socketManager to delete related gate
    }

    public void SetCurrentSocket(CircuitSocket socket)  {currentSocket = socket;}
    
    public CircuitSocket GetCurrentSocket() {return currentSocket;}

    public int getTarget()  {return 0;}

    public string getGateName() {return gateName;}

    public string getGateDescription()  {return gateDescription;}

    public inputType getGateType()  {return gatetype; }

    public int GetNumInput()
    {
        if(gatetype == inputType.Single) return 3;

        if(gatetype == inputType.Double) return 2;

        if(gatetype == inputType.Triple) return 3;

        return 0;
    }

    public void AddTarget(int qubitIndex, Position targetPosition)
    {
        
    }

    public class TargetGate
    {
        GameObject target;
        int qubitIndex;
    }
}