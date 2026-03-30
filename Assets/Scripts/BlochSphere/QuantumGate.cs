using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class QuantumGate : MonoBehaviour
{
    [Header("Gate Info")]
    [SerializeField] private string gateName; // H, X, Y, Z, CNOT, etc.
    public enum inputType{Default, Single, Double, Triple, target}; 
    [SerializeField] private inputType gatetype;
    [SerializeField] private string gateDescription; // (Optional)
    
    public SocketsManager _socketsManager = null;
    private CircuitSocket currentSocket;
    public bool friendExist = false; //for multi-input gate
    public bool beingDestroyed = false;
    public MultiInputGateConnect connect = null;

    void Start()
    {
        if (string.IsNullOrEmpty(gateName)) gateName = gameObject.name;
        if (string.IsNullOrEmpty(gateDescription)) gateDescription = $"{gateName} Gate";
    }

    public void doDestroy()
    {
        if(connect != null)
        {
            connect.deleteItself();
            return;
        }

        if(gatetype == inputType.Single) {
            Debug.Log("Delete single input");
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    //-------------- Not used ---------------------------
    public void SetCurrentSocket(CircuitSocket socket)  {currentSocket = socket;}
    public CircuitSocket GetCurrentSocket() {return currentSocket;}
    //-------------- Not used ---------------------------

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