using Unity.VisualScripting;
using UnityEngine;

public class QuantumGate_Chap3 : MonoBehaviour
{
    [Header("Gate Info")]
    [SerializeField] public string gateName;
    public enum inputType { Single, Double, Triple };
    [SerializeField] private inputType gatetype;
    [SerializeField] private string gateDescription;

    private CircuitSocket_Chap3 currentSocket;

    void Start()
    {
        if (string.IsNullOrEmpty(gateName)) gateName = gameObject.name;
        if (string.IsNullOrEmpty(gateDescription)) gateDescription = $"{gateName} Gate";
    }

    public void SetCurrentSocket(CircuitSocket_Chap3 socket)
    {
        currentSocket = socket;
    }

    public CircuitSocket_Chap3 GetCurrentSocket()
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