using System.Collections.Generic;
using UnityEngine;

public class MultiInputGateConnect : MonoBehaviour
{
    public SocketsManager socketsManager = null;
    private List<QuantumGate> gateMember = new List<QuantumGate>();
    private List<LineConnecting> targetConnect = new List<LineConnecting>();
    private bool memberSet = false;
    public int column = -1;

    public void getHighLow(out int high, out int low)
    {
        // iterate through member and get most high-low index
        high = 0; low = 0;
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
        foreach(QuantumGate gate in gateMember)
        {
            gate.beingDestroyed = true;
            Destroy(gate.gameObject);
            Debug.Log($"delete member name: {gate.gameObject.name}");
        }
        socketsManager.RemoveFromMultiGateList(this);

        //tell socket manager to free lock socket!

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
