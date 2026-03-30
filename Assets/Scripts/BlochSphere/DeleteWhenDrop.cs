using UnityEngine;

public class DeleteWhenDrop : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y < 0.5){
            QuantumGate gate = gameObject.GetComponent<QuantumGate>();
            if(gate != null)
            {
                gate.doDestroy();
            }
            else
            {
                Destroy(gameObject);
            }
            
        }
    }
}
