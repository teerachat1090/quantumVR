using UnityEngine;
using TMPro;
using System;

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

    public void adjustText(Quaternion direction)
    {
        canvas.transform.rotation = direction;
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

    private float textSlide = 0.1f;
    private bool slided = false;

    //change node size and color
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

        if (prob > 0.5f && !slided)
        {
            canvas.transform.Translate(Vector3.up * textSlide);
            slided = true;
        } else if (prob < 0.5f && slided)
        {
            canvas.transform.Translate(Vector3.down * textSlide);
            slided = false;
        }

        float hue = PhaseColoring.PhaseToColor(phase);
        Color newColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
        
        nodeRenderer.material.color = newColor;
        lineRenderer.material.color = newColor;
    }
}
