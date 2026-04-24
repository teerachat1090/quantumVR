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
    public TextMeshProUGUI txt_QBER;
    public TextMeshProUGUI txt_EntRate;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GraphManager.Instance == null) return;

        MetricsData m = GraphManager.Instance.GetMetrics();

        // Network Partitioned — แสดง "—" ทุก metric
        if (m.partitioned)
        {
            txt_Hops.text     = "—";
            txt_Fidelity.text = "0%";
            txt_Links.text    = $"{m.links}";
            txt_Distance.text = "—";
            txt_Fault.text    = FaultToEng(m.fault);
            if (txt_QBER != null)
            {
                txt_QBER.text  = "50.0%";
                txt_QBER.color = new Color(1.0f, 0.2f, 0.1f);
            }
            if (txt_EntRate != null)
                txt_EntRate.text = "~0 ebit/s";
            return;
        }

        txt_Hops.text     = m.hops > 0 ? $"{m.hops}" : "—";
        txt_Fidelity.text = string.IsNullOrEmpty(m.penaltyTag)
            ? $"{m.fidelity}%"
            : $"{m.fidelity}% <color=#FF8844><size=80%>({m.penaltyTag})</size></color>";
        txt_Links.text    = $"{m.links}";
        txt_Distance.text = $"{m.distKm} km";
        txt_Fault.text    = $"{FaultToEng(m.fault)}";

        // QBER — แสดงเป็น % พร้อม status
        if (txt_QBER != null)
        {
            float qberPct = m.qber * 100f;
            txt_QBER.text  = $"{qberPct:F1}%";
            txt_QBER.color = qberPct < 11f  ? new Color(0.0f, 0.8f, 0.2f)   // เขียว  < 11%  Secure
                           : qberPct < 25f  ? new Color(1.0f, 0.5f, 0.0f)   // ส้ม   < 25%  Warning
                           :                  new Color(1.0f, 0.2f, 0.1f);  // แดง  >= 25%  Insecure
        }

        // Entanglement Rate — แสดงหน่วยอัตโนมัติ ไม่มี floor
        if (txt_EntRate != null)
        {
            float r = m.entangleRate;
            if (r >= 1f)
                txt_EntRate.text = $"{r:F1} ebit/s";
            else if (r >= 0.001f)
                txt_EntRate.text = $"{r * 1000f:F2} mebit/s";
            else
                txt_EntRate.text = "~0 ebit/s";
        }
    }

    string FaultToEng(string fault)
    {
        switch (fault)
        {
            case "ต่ำ":  return "Very Low";
            case "กลาง": return "Medium";
            case "สูง":  return "Very High";
            case "ดี":   return "High";
            default:     return fault;
        }
    }
}