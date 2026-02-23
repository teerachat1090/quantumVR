using Unity.VisualScripting;
using UnityEngine;

public class QuantumGate : MonoBehaviour
{
    [Header("Gate Info")]
    [SerializeField] private string gateName; // H, X, Y, Z, CNOT, etc.
    public enum inputType{Single, Double, Triple}; 
    [SerializeField] private inputType gatetype;
    [SerializeField] private string gateDescription; // (Optional)
    
    private CircuitSocket currentSocket;
    
    void Start()
    {
        // use gameObject name as default name
        if (string.IsNullOrEmpty(gateName)) gateName = gameObject.name;
        
        // use its name as default description
        if (string.IsNullOrEmpty(gateDescription)) gateDescription = $"{gateName} Gate";
    }
    
    public void SetCurrentSocket(CircuitSocket socket)
    {
        currentSocket = socket;
    }
    
    public CircuitSocket GetCurrentSocket()
    {
        return currentSocket;
    }

    public int getTarget()
    {
        return 0;
    }

    public string getGateName()
    {
        return gateName;
    }

    public string getGateDescription()
    {
        return gateDescription;
    }

    public inputType getGateType()
    {
        return gatetype; 
    }
}