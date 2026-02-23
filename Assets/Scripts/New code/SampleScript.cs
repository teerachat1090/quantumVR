using UnityEngine;

public class SampleScript : MonoBehaviour
{

    private Material mat = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();

        if(renderer is null)
        {
            Debug.LogWarning("can't find renderer");
            return;
        }
        renderer.material.color = Color.red;
        Debug.Log("change success");

        if(renderer.material.color == Color.red) Debug.Log("color correct");
        else Debug.LogWarning("color incorrect");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void func()
    {
        Debug.Log("*************************************");
        Debug.Log("FROM NEW SCRIPT");
        Debug.Log("====================================");
        Debug.Log("Button has pressed");
        Debug.Log("====================================");
        Debug.Log("*************************************");
    }
}
