using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Mathematics;

public class StateVectorDisplay : MonoBehaviour
{
    [SerializeField]    private TMP_Text realPartText = null;
    [SerializeField]    private TMP_Text imagPartText = null;
    [SerializeField]    private TMP_Text valueText = null;
    [SerializeField]    private TMP_Text probText = null;
    [SerializeField]    private TMP_Text phaseText = null;
    [SerializeField]    private Image phaseColor = null;
    
    public bool CheckDisplayComponent()
    {
        if(realPartText == null || imagPartText == null || valueText == null || 
            probText == null || phaseText == null || phaseColor == null) 
            return false;
        return true;
    }

    public void AssignInfomation(double realPart, double imagPart, int value, double prob ,double phase)
    {
        if (!CheckDisplayComponent())
        {
            Debug.LogWarning("Warning: Some display component is missing");
            return;
        }

        string sign = realPart < 0 ? "- ": "";
        realPart = math.abs(realPart);
        realPartText.text = sign + $"{realPart:F3}";

        sign = imagPart < 0 ? "- ": "+ ";
        imagPart = math.abs(imagPart);
        imagPartText.text = sign + $"{imagPart:F3} j";
        valueText.text = $"|{value}\u27e9";
        probText.text = $"Prob. : {prob:F3}";
        phaseText.text = $"Phase: {phase:F1}\u00B0";

        float hue = PhaseColoring.PhaseToColor((float) phase);
        Color newColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
        phaseColor.color = newColor;
    }
}
