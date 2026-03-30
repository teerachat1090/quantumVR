using System.Collections.Generic;
using UnityEngine;

public class MultiInputGateConnect : MonoBehaviour
{
    private List<QuantumGate> targetGate = new List<QuantumGate>();
    private List<LineConnecting> targetConnect = new List<LineConnecting>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.GetComponentsInChildren(targetGate);

        // pair each child to create line
        int count = targetGate.Count;
        for(int i=0; i<count-1; i++)
        {
            GameObject target0 = targetGate[i].gameObject;
            GameObject target1 = targetGate[i+1].gameObject;

            var connect = new LineConnecting(target0, target1, gameObject);
            targetConnect.Add(connect);   
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach(LineConnecting line in targetConnect)
        {
            line.UpdateLine();
        }
    }

    public void deleteItself(QuantumGate starter)
    {
        
        Destroy(gameObject);
        
    }

    void OnDestroy()
    {
        Debug.Log($"Parent is about to be destroyed. Setting children ({targetGate.Count}).");
        targetGate.RemoveAll(g => g == null);
        // foreach(QuantumGate gate in targetGate)
        // {
        //     gate.parentAct = true;
        //     Destroy(gate.gameObject);
        // }
        // targetGate = null;
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
