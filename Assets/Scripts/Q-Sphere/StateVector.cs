using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;

public class StateVector : MonoBehaviour
{
    [SerializeField] Transform nodeTransform;
    [SerializeField] Renderer nodeRenderer;
    [SerializeField] Renderer lineRenderer;
    [SerializeField] Canvas canvas;

    private TMP_Text valueText;
    private int stateVal;

    private void CheckInput()
    {
        if(nodeTransform is null) Debug.LogWarning("Warning: Node transform is missing. Unable to rescale by state probability.");
        
        if(nodeRenderer is null) Debug.LogWarning("Warning: Node renderer is missing. Unable to change its color by state phase.");

        if(lineRenderer is null) Debug.LogWarning("Warning: Line is missing. Unable to change its color by state phase.");

        if(canvas is null) Debug.LogWarning("Warning: Canvas for display is missing. Unable to change state value.");
        else
        {
            valueText = canvas.GetComponentInChildren<TMP_Text>();
            if(valueText is null) Debug.LogWarning("Warning: Text for display is missing. Unable to change state value.");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CheckInput();
    }

    void Update()
    {
        
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

    private float[] hueBoundRange = {
        35f, 25f, 15f, 20f,  10f,  55f,
        30f, 15f, 10f, 55f, 100f,   0f
        };

    private float[] hueLowerBound = {
        235f, 270f, 295f, 310f, 330f, 340f,
        355f, 025f, 040f, 050f, 105f, 205f
    };

    private float angleRef = 30f;

    private float PhaseToColor(float phase)
    {
        int angleOffSet = (int) phase % (int) angleRef;
        int boundindex = (int) (phase/angleRef);
        float hueRange = hueBoundRange[boundindex];
        float lowerBound = hueLowerBound[boundindex];

        float offset =  hueRange * angleOffSet / angleRef;

        float newHue = (lowerBound + offset)/360f;
        if (newHue > 1) newHue-=1f;

        Debug.Log(@$"phase: {phase}, angleOffset: {angleOffSet}, boundindex: {boundindex}, 
                    boundOffset: {boundOffset}, lowerBound: {lowerBound}, newHue: {newHue}");
        return newHue;
    }

    //change node size (0-0.2) and color
    public void UpdateStateVector(float prob, float phase)
    {
        if(prob < 0.00001f)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // scale 0.025 - 0.225
        float ratio = 0.025f + prob * 0.2f;
        nodeTransform.localScale = new Vector3(ratio, ratio, ratio);

        float hue = PhaseToColor(phase);
        Color newColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
        
        nodeRenderer.material.color = newColor;
        lineRenderer.material.color = newColor;
    }
}
