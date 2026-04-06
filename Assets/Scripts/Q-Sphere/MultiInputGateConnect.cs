using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MultiInputGateConnect : MonoBehaviour
{
    public SocketsManager socketsManager = null;
    private List<QuantumGate> gateMember = new List<QuantumGate>();
    private List<LineConnecting> targetConnect = new List<LineConnecting>();
    private bool memberSet = false;
    public int column = -1;
    public string gateName = "";

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

    public void GetGateListByType(out List<int> controlsRow, out List<int> targetsRow)
    {
        var control_temp = new List<int>();
        var target_temp = new List<int>();

        foreach(QuantumGate gate in gateMember)
        {
            GateSocket gateSocket = gate.socket;
            if(gateSocket == null) {
                Debug.LogWarning("no socket attach to this gate");
                continue;
            }

            int row = gateSocket.qubitIndex;
            if (gate.isController)  control_temp.Add(row);
            else target_temp.Add(row);
        }

        controlsRow = control_temp;
        targetsRow = target_temp;
    }

    public void getHighLow(out int highQ, out int lowQ, out int col)
    {
        // iterate through member and get most high-low index
        highQ = -1; lowQ = -1; col = column;

        foreach(QuantumGate gate in gateMember)
        {
            var grabInteractable = gate.gameObject.GetComponent<XRGrabInteractable>();

            if(grabInteractable == null)
            {
                Debug.LogWarning("Warning: component is missing (XRGrabInteractable).");
                continue;
            }

            GateSocket gateSocket = gate.socket;
            if(gateSocket == null) continue;

            int val = gateSocket.qubitIndex;
            if(highQ < 0)        highQ = lowQ = val;
            else if(val > highQ) highQ = val;
            else if(val < lowQ)  lowQ = val;
        }
    }

    public void ToggleCurrentColumn(bool doLock = true)
    {
        int highQubit, lowQubit, column;
        getHighLow(out highQubit, out lowQubit, out column);
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

    public void AddMember(QuantumGate memberGate)
    {
        gateMember.Add(memberGate);
    }

    public void RunLineConnect()
    {
        foreach(QuantumGate gate in gateMember)
        {
            gate.connect = this;
            Debug.Log($"member name: {gate.gameObject.name}");
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
            GameObject target0 = gateMember[i].gameObject;
            GameObject target1 = gateMember[i+1].gameObject;

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
        foreach(QuantumGate gate in gateMember)
        {
            gate.beingDestroyed = true;
            Destroy(gate.gameObject);
            Debug.Log($"delete member name: {gate.gameObject.name}");
        }
        socketsManager.RemoveFromMultiGateList(this);

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
