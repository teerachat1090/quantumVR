using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Threading.Tasks;

public class MultiInputGateConnect : MonoBehaviour
{
    public SocketsManager socketsManager = null;
    private List<GameObject> gateMember = new List<GameObject>();
    private List<LineConnecting> targetConnect = new List<LineConnecting>();
    private bool memberSet = false;
    public int column = -1;
    public string gateName = "";
    public bool classicalRelated = false;

    private GateSocket GetCurrentGatesocket(QuantumGate gate)
    {
        XRGrabInteractable grabInteractable = gate.GetComponent<XRGrabInteractable>();
        if(grabInteractable == null) {
            Debug.LogWarning("Warning: component is missing (XRGrabInteractable).");
            return null;
        }

        IXRSelectInteractor selectingInteractor = grabInteractable.interactorsSelecting.FirstOrDefault();
        if (selectingInteractor != null && selectingInteractor is XRSocketInteractor socket)
        {
            GateSocket gateSocket = socket.GetComponent<GateSocket>();
            if(gateSocket == null)
            {
                Debug.LogWarning("Warning: socket's component is missing (GateSocket).");
                return null;
            }

            return gateSocket;
        }

        return null;
    }

    public List<int> GetOneGate()
    {
        var control = new List<int>();
        foreach(GameObject gate in gateMember)
        {
            var quantumGate = gate.GetComponent<QuantumGate>();
            if(quantumGate == null) continue;
            GateSocket gateSocket = quantumGate.socket;
            if(gateSocket == null) continue;

            control.Add(gateSocket.qubitIndex);
            break;
        }
        return control;
    }

    public void GetGateListByType(out List<int> controlsRow, out List<int> targetsRow)
    {
        var control_temp = new List<int>();
        var target_temp = new List<int>();

        foreach(GameObject gate in gateMember)
        {
            var quantumGate = gate.GetComponent<QuantumGate>();
            if(quantumGate == null) continue;

            GateSocket gateSocket = quantumGate.socket;
            if(gateSocket == null) {
                Debug.LogWarning("no socket attach to this gate");
                continue;
            }

            int row = gateSocket.qubitIndex;
            if (quantumGate.isController)  control_temp.Add(row);
            else target_temp.Add(row);
        }

        controlsRow = control_temp;
        targetsRow = target_temp;
    }

    public void getHighLow(out int highQ, out int lowQ, out int col)
    {
        // iterate through member and get most high-low index
        highQ = -1; lowQ = -1; col = column;

        foreach(GameObject gate in gateMember)
        {
            var QuantumGate = gate.GetComponent<QuantumGate>();
            if(QuantumGate == null) continue;

            var grabInteractable = gate.gameObject.GetComponent<XRGrabInteractable>();

            if(grabInteractable == null)
            {
                Debug.LogWarning("Warning: component is missing (XRGrabInteractable).");
                continue;
            }

            if(classicalRelated && QuantumGate.getGateType() != QuantumGate.inputType.measure) continue;

            GateSocket gateSocket = QuantumGate.socket;
            if(gateSocket == null) continue;

            int val = gateSocket.qubitIndex;
            if(highQ < 0)        highQ = lowQ = val;
            else if(val > highQ) highQ = val;
            else if(val < lowQ)  lowQ = val;
        }
    }

    public void ToggleCurrentColumn(bool doLock = true)
    {
        if(!Application.isPlaying) return;
        
        int highQubit, lowQubit, column;
        getHighLow(out highQubit, out lowQubit, out column);
        if (classicalRelated)
        {
            lowQubit = socketsManager.totalQubits - 1;
            if(highQubit < 0) highQubit = lowQubit;
        }
        if(socketsManager == null)
        {
            Debug.LogError("Error: socketsManager is null. can't proceed further.");
            return;
        }

        if(highQubit < 0 || lowQubit < 0 || column < 0)
        {
            Debug.LogError("Error: invalid range of multi gate.");
            return;
        }

        socketsManager.ToggleMultiGateColumn(highQubit, lowQubit, column, doLock);
    }

    public void AddMember(GameObject memberGate)
    {
        gateMember.Add(memberGate);
    }

    public void RunLineConnect()
    {
        foreach(GameObject gate in gateMember)
        {
            var QuantumGate = gate.GetComponent<QuantumGate>();
            if(QuantumGate == null) continue;

            if(QuantumGate != null) QuantumGate.connect = this;
            Debug.Log($"member name: {gate.name}");
        }
        ConnectLine();
        memberSet = true;
    }

    void ConnectLine()
    {
        // pair each child to create line
        int count = gateMember.Count;
        for(int i=0; i<count-1; i++)
        {
            GameObject target0 = gateMember[i];
            GameObject target1 = gateMember[i+1];

            var connect = new LineConnecting(target0, target1, gameObject);
            targetConnect.Add(connect);   
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!memberSet) return;

        foreach(LineConnecting line in targetConnect)
        {
            line.UpdateLine();
        }
    }

    public void deleteItself()
    {
        ToggleCurrentColumn(doLock: false);
        foreach(GameObject gate in gateMember)
        {
            var QuantumGate = gate.GetComponent<QuantumGate>();
            if(QuantumGate == null)
            {
                gate.SetActive(false);
                continue;
            }

            QuantumGate.beingDestroyed = true;
            Debug.Log($"delete member name: {gate.name}");
            Destroy(gate);
        }
        socketsManager.RemoveFromMultiGateList(this);

        Debug.Log("Connetion removed...");
        Destroy(gameObject);
    }

    public class LineConnecting
    {
        GameObject line = null;
        public float lineWidth = 0.05f;
        GameObject target0, target1;
        Vector3 offset, linePosition;
        float distance;

        public LineConnecting(GameObject Target0, GameObject Target1, GameObject Manager)
        {
            target0 = Target0; target1 = Target1;
            
            line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Renderer lineRen = line.GetComponent<Renderer>();
            line.transform.SetParent(Manager.transform);

            Renderer ren = target0.GetComponentInChildren<Renderer>();
            Material mat = ren.material;
            
            lineRen.material = mat;
            
            UpdateLine();
        }

        public void UpdateLine()
        {
            if(target0 == null || target1 == null) return;

            offset = target1.transform.position - target0.transform.position;
            distance = offset.magnitude;

            linePosition = target0.transform.position + (offset/2f);
            line.transform.position = linePosition;

            line.transform.up = offset.normalized;

            line.transform.localScale = new Vector3(
                lineWidth, 
                distance / 2.0f, 
                lineWidth
            );
        }
    }
}
