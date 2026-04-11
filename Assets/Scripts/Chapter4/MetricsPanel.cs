using UnityEngine;
using TMPro;

public class MetricsPanel : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI txt_Hops;
    public TextMeshProUGUI txt_Fidelity;
    public TextMeshProUGUI txt_Links;
    public TextMeshProUGUI txt_Distance;
    public TextMeshProUGUI txt_Fault;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GraphManager.Instance == null) return;

        MetricsData m = GraphManager.Instance.GetMetrics();

        txt_Hops.text     = $"{m.hops}";
        txt_Fidelity.text = $"{m.fidelity}%";
        txt_Links.text    = $"{m.links}";
        txt_Distance.text = $"{m.distKm} km";
        txt_Fault.text    = $"{FaultToEng(m.fault)}";
    }

    string FaultToEng(string fault)
    {
        switch (fault)
        {
            case "ต่ำ":  return "Low";
            case "กลาง": return "Medium";
            case "สูง":  return "High";
            case "ดี":   return "Good";
            default:     return fault;
        }
    }
}