using UnityEngine;

// ════════════════════════════════════════════════════════════════
//  QuantumGate_Chap3.cs
//  แก้จาก QuantumGate.cs เดิม
//  เปลี่ยน CircuitSocket → CircuitSocket_Chap3
//  ไม่แตะ QuantumGate.cs เดิมเพราะเพื่อนใช้อยู่
// ════════════════════════════════════════════════════════════════

public class QuantumGate_Chap3 : MonoBehaviour
{
    [Header("Gate Info")]
    [SerializeField] private string gateName;
    [SerializeField] private string gateDescription;

    public enum InputType { Single, Double, Triple }
    [SerializeField] private InputType gateType;

    private CircuitSocket_Chap3 currentSocket;

    // ─────────────────────────────────────────
    void Start()
    {
        if (string.IsNullOrEmpty(gateName))
            gateName = gameObject.name;

        if (string.IsNullOrEmpty(gateDescription))
            gateDescription = $"{gateName} Gate";
    }

    // ─────────────────────────────────────────
    //  Socket
    // ─────────────────────────────────────────
    public void SetCurrentSocket(CircuitSocket_Chap3 socket)
    {
        currentSocket = socket;
    }

    public CircuitSocket_Chap3 GetCurrentSocket()
    {
        return currentSocket;
    }

    // ─────────────────────────────────────────
    //  Getters
    // ─────────────────────────────────────────
    public string    getGateName()        => gateName;
    public string    getGateDescription() => gateDescription;
    public InputType getGateType()        => gateType;
    public int       getTarget()          => 0;
}