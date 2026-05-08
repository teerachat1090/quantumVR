using UnityEngine;
using TMPro;
using System;

public class StateVector : MonoBehaviour
{
    [SerializeField] Transform nodeTransform;
    [SerializeField] Renderer nodeRenderer;
    [SerializeField] Renderer lineRenderer;
    [SerializeField] Canvas canvas;

    public Vector3 pointToSee = Vector3.zero;
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

    void Awake()
    {
        CheckInput();
    }

    /// <summary>
    ///     บังคับให้ text บน statevector หันไปทางทิศเดียวกันตลอดเวลา
    /// </summary>
    void LateUpdate()
    {
        canvas.transform.LookAt(pointToSee);
    }

    /// <summary>
    ///     บังคับให้ text บน statevector หันไปในมุมที่ต้องการ <br/>
    ///     (อาจไม่ต้องใช้แล้ว เพราะมีฟังก์ชันบังคับ text ตลอดเวลาอยู่แล้ว)
    /// </summary>
    public void adjustText(Quaternion direction)
    {
        canvas.transform.rotation = direction;
    }

    /// <summary>
    ///     ตั้งค่า text บน statevector ให้อยู่ในรูป |...>
    /// </summary>
    private void SetStateDisplay(string state)
    {
        if(valueText is null) 
        {
            Debug.LogWarning("Warning: Text for display is missing. Unable to change state value.");
            return;
        }

        valueText.text = $"|{state}\u27E9";
    }

    /// <summary>
    ///     อัพเดทค่าของ statevector ทั้งค่าภายใน และการแสดงผล
    /// </summary>
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

    /// <summary>
    ///     อัพเดท statevector จากความน่าจะเป็น และเฟส
    /// </summary>
    /// <remarks>
    ///     ความน่าจะเป็นกำหนดขนาดของโหนด และเฟสกำหนดสีของ statevector
    /// </remarks>
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

        if (prob >= 0.5f && !slided)
        {
            Debug.Log("text got slided");
            canvas.transform.Translate(nodeTransform.up  * textSlide);
            slided = true;
        } else if (prob < 0.5f && slided)
        {
            Debug.Log("text slided back");
            canvas.transform.Translate(-nodeTransform.up* textSlide);
            slided = false;
        }

        float hue = PhaseColoring.PhaseToColor(phase);
        Color newColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
        
        nodeRenderer.material.color = newColor;
        lineRenderer.material.color = newColor;
    }
}
