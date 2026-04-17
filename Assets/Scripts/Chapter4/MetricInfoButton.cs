using UnityEngine;
using UnityEngine.UI;

public class MetricInfoButton : MonoBehaviour
{
    [TextArea(2, 5)]
    public string metricTitle;
    [TextArea(3, 8)]
    public string metricDescription;

    void Start()
    {
       GetComponent<Button>().onClick.AddListener(() =>
            MetricInfoPopup.Instance.Toggle(metricTitle, metricDescription));
    }
}