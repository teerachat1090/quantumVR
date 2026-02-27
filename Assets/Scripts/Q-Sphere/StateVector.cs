using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class StateVector : MonoBehaviour
{
    [SerializeField] Transform nodeTransform;
    [SerializeField] Renderer nodeRenderer;
    [SerializeField] Renderer line;
    [SerializeField] TMP_Text valueText;

    private int stateVal;
    private float stateProb;

    private void CheckInput()
    {
        if(nodeTransform is null) Debug.LogWarning("Warning: Node transform is missing. Unable to rescale by state probability.");
        
        if(nodeRenderer is null) Debug.LogWarning("Warning: Node renderer is missing. Unable to change its color by state phase.");

        if(line is null) Debug.LogWarning("Warning: Line is missing. Unable to change its color by state phase.");

        if(valueText is null) Debug.LogWarning("Warning: Text for display is missing. Unable to change state value.");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckInput();
    }

    private void SetStateDisplay(string state)
    {
        if(valueText is null) 
        {
            Debug.LogWarning("Warning: Text for display is missing. Unable to change state value.");
            return;
        }

        valueText.text = $"|{state}\u27E9";
    }

    public void SetStateValue(int value, int qubitAmount)
    {
        stateVal = value;
        string valStr = Convert.ToString(value, 2).PadLeft(qubitAmount, '0');

        SetStateDisplay(valStr);
    }

    public int GetStateVal()
    {
        return stateVal;
    }
}
