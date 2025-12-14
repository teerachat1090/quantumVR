using UnityEngine;

public class QuantumGate : MonoBehaviour
{
    [Header("Gate Info")]
    public string gateName; // H, X, Y, Z, CNOT, etc.
    public enum inputType{Single,Double,Triple}; 
    public inputType gatetype;
    public string gateDescription; // (Optional)
    private CircuitSocket currentSocket;
    
    void Start()
    {
        // use gameObject name as default name
        if (string.IsNullOrEmpty(gateName)) gateName = gameObject.name;
        
        // use its name as default description
        if (string.IsNullOrEmpty(gateDescription)) gateDescription = $"{gateName} Gate";
    }
    

    // Function for external script to call -> like API
    public void SetCurrentSocket(CircuitSocket socket)
    {
        currentSocket = socket;
    }
    
    public CircuitSocket GetCurrentSocket()
    {
        return currentSocket;
    }
}