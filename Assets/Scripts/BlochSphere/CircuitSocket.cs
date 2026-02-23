using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class CircuitSocket : MonoBehaviour
{
    [Header("Socket Info")]
    public int socketIndex; // SC1 (1), SC1 (2), etc.
    public string socketName;

    [Header("XR Socket")]
    public XRSocketInteractor socketInteractor; // Socket Interactor สำหรับรับ Gate
    
    [Header("Current Gate")]
    public QuantumGate currentGate; // Gate ที่วางอยู่ใน socket นี้
    
    private CircuitTable circuitTable;
    
    void Start()
    {
        // หา CircuitTable ที่เป็น parent
        circuitTable = GetComponentInParent<CircuitTable>();
        
        if (string.IsNullOrEmpty(socketName))
        {
            socketName = $"SC1 ({socketIndex})";
        }

        // ถ้าไม่ได้ assign Socket Interactor ให้หาจาก GameObject นี้
        if (socketInteractor == null)
        {
            socketInteractor = GetComponent<XRSocketInteractor>();
        }

        // Subscribe to socket events
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnGatePlaced);
            socketInteractor.selectExited.AddListener(OnGateRemoved);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe เมื่อทำลาย object
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnGatePlaced);
            socketInteractor.selectExited.RemoveListener(OnGateRemoved);
        }
    }
    
    // เรียกเมื่อมี Gate ถูกวางลงใน Socket (Auto-called by XR Interaction)
    private void OnGatePlaced(SelectEnterEventArgs args)
    {
        // ดึง QuantumGate component จาก object ที่ถูกวาง
        QuantumGate gate = args.interactableObject.transform.GetComponent<QuantumGate>();
        
        if (gate != null)
        {
            currentGate = gate;
            gate.SetCurrentSocket(this); // บอก Gate ว่ามันอยู่ใน Socket นี้
            
            Debug.Log($"✅ Gate '{gate.getGateName()}' placed in {socketName}");
            
            if (circuitTable != null)
            {
                circuitTable.UpdateCircuit();
            }
        }
    }
    
    // เรียกเมื่อ Gate ถูกเอาออกจาก Socket (Auto-called by XR Interaction)
    private void OnGateRemoved(SelectExitEventArgs args)
    {
        if (currentGate != null)
        {
            Debug.Log($"❌ Gate '{currentGate.getGateName()}' removed from {socketName}");
            
            currentGate.SetCurrentSocket(null);
            currentGate = null;
            
            if (circuitTable != null)
            {
                circuitTable.UpdateCircuit();
            }
        }
    }
    
    // ตรวจสอบว่ามี Gate อยู่หรือไม่
    public bool HasGate()
    {
        return (currentGate != null) ? true : false;
    }
}