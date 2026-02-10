using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using Unity.Mathematics;

public class EditingHistChild : MonoBehaviour
{
    [SerializeField]    private Image image = null;
    [SerializeField]    private TMP_Text state = null, prob = null;

    private RectTransform rectTransform, imageRect, probRect;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imageRect = image.GetComponent<RectTransform>();
        probRect = prob.GetComponent<RectTransform>();
    }

    public void setState(int stateVal)
    {
        state.SetText($"|{stateVal}\u27e9");
    }

    public void setProb(double probVal)
    {
        string probStr = probVal.ToString("F3");
        prob.SetText($"{probStr}");

        if(imageRect is null)
        {
            Debug.LogWarning("Warning: this \"image\" in prefab not have RectTransform component!");
            return;
        }

        // start at y-negative and full at y=0
        float maxHeight = math.abs(rectTransform.localPosition.y) - 1;
        float resultHeight = ((float)probVal) * maxHeight;
        float barWidth = imageRect.rect.width;
        imageRect.sizeDelta = new Vector2(barWidth, resultHeight);

        if(probRect is null)
        {
            Debug.LogWarning("Warning: this \"prob\" in prefab not have RectTransform component!");
            return;
        }

        float currentX = probRect.localPosition.x;
        probRect.localPosition = new Vector2(currentX, resultHeight+1.0f);
    }

    public void setHist(float xPosition, float width)
    {
        //set width, height
        if(rectTransform is null)
        {
            Debug.LogWarning("Warning: this prefab not have RectTransform component!");
            return;
        }

        float currentY = rectTransform.localPosition.y;
        rectTransform.localPosition = new Vector2(xPosition, currentY);


        if(imageRect is null)
        {
            Debug.LogWarning("Warning: this \"image\" in prefab not have RectTransform component!");
            return;
        }

        float currentHeight = imageRect.rect.height;
        imageRect.sizeDelta = new Vector2(width, currentHeight);
    }
}
