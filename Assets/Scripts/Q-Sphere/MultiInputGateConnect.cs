using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MultiInputGateConnect : MonoBehaviour
{
    public SocketsManager socketsManager = null;
    private List<GameObject> gateMember = new List<GameObject>();
    private List<LineConnecting> targetConnect = new List<LineConnecting>();
    private bool memberSet = false;
    public int column = -1;
    public string gateName = "";
    public bool classicalRelated = false;
    public bool conditionRelated = false;
    public Material classicalMaterial = null;
    private int highNote = -1, lowNote = -1;

    public List<int> GetIndexOneGate()
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
        int highTemp = -1, lowTemp = -1;

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
            if(gateSocket == null) {
                Debug.LogWarning("socket is empty");
                continue;}

            
            int val = gateSocket.qubitIndex;
            Debug.Log($"high-low value assigned: {val}");
            if(highTemp < 0)        highTemp = lowTemp = val;
            else if(val > highTemp) highTemp = val;
            else if(val < lowTemp)  lowTemp = val;
            Debug.Log($"Value change: high->{highTemp}, low->{lowTemp}");
        }

        highNote = highTemp; lowNote = lowTemp;

        highQ = highTemp; lowQ = lowTemp;
        col = column;
    }

    public void SetSocketInBetween(bool useTempVal = false, bool doEnable = true)
    {
        if(socketsManager == null)
        {
            Debug.LogWarning("Warning: socket manager is missing");
            return;
        }

        Debug.Log($"status: tempVal-{useTempVal}");
        if (useTempVal)
        {
            Debug.Log($"Set socket temp : high->{highNote}, low->{lowNote}");
            socketsManager.SetColumnSocketBetween(highNote, lowNote, column, isEnable: doEnable);
        } 
        else
        {
            getHighLow(out int highQ, out int lowQ, out _);
            Debug.Log($"Set socket real : high->{highQ}, low->{lowQ}");
            socketsManager.SetColumnSocketBetween(highQ, lowQ, column, isEnable: doEnable);
        }
    }

    //apply exclusive gate to column
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
            Debug.LogWarning($"Error: invalid range of multi gate: high-{highQubit}, low-{lowQubit}, col-{column}");
            return;
        }

        socketsManager.ToggleMultiGateColumn(highQubit, lowQubit, column, doLock);
    }

    public void AddMember(GameObject memberGate)
    {
        gateMember.Add(memberGate);
        var quantumGate = memberGate.GetComponent<QuantumGate>();
        if(quantumGate is null) return;
        quantumGate.setConditionSocket(false);
        if(quantumGate.getGateType() == QuantumGate.inputType.condition) conditionRelated = true;
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

            var connect = new LineConnecting(target0, target1, gameObject, classicalMat: classicalMaterial);
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

        public LineConnecting(GameObject Target0, GameObject Target1, GameObject Manager, Material classicalMat = null)
        {
            target0 = Target0; target1 = Target1;
            
            line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Renderer lineRen = line.GetComponent<Renderer>();
            line.transform.SetParent(Manager.transform);

            if(classicalMat == null)
            {
                Renderer ren = target0.GetComponentInChildren<Renderer>();
                Material mat = ren.material;
                lineRen.material =  mat;
            }
            else
            {
                lineRen.material = classicalMat;
            }
            
            
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
