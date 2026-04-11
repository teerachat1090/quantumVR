using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoiseInfoPanel : MonoBehaviour
{
    [Header("Noise Slider")]
    public Slider          noiseSlider;
    public TextMeshProUGUI noiseValueText;

    [Header("Link Rows — Row_0 ถึง Row_7")]
    public GameObject[]      rows;
    public Slider[]           bars;
    public TextMeshProUGUI[] labelTexts;
    public TextMeshProUGUI[] valueTexts;

    [Header("Summary")]
    public TextMeshProUGUI fidAfterNoiseText;
    public TextMeshProUGUI statusText;

    [Header("Colors")]
    public Color colorHigh = new Color(0.11f, 0.62f, 0.46f);
    public Color colorMid  = new Color(0.73f, 0.46f, 0.09f);
    public Color colorLow  = new Color(0.85f, 0.35f, 0.19f);

    private float lastNoiseValue = -1f;

    void Start()
    {
        // ตั้งค่า slider range — ไม่ใช้ AddListener
        if (noiseSlider != null)
        {
            noiseSlider.minValue = 0f;
            noiseSlider.maxValue = 0.3f;
            noiseSlider.value    = GraphManager.Instance != null
                                   ? GraphManager.Instance.noiseStrength
                                   : 0.15f;
            lastNoiseValue = noiseSlider.value;
            UpdateNoiseValueText(noiseSlider.value);
        }

        HideAllRows();
        Refresh();
    }

    void Update()
    {
        if (noiseSlider == null) return;
        if (GraphManager.Instance == null) return;

        float v = noiseSlider.value;

        // อัปเดต text ทุก frame
        UpdateNoiseValueText(v);

        // ถ้าค่า slider เปลี่ยน → อัปเดต GraphManager
        if (Mathf.Abs(v - lastNoiseValue) > 0.001f)
        {
            lastNoiseValue = v;
            GraphManager.Instance.noiseStrength = v;

            // ถ้า noise เปิดอยู่ re-run routine
            if (GraphManager.Instance.simJam)
            {
                if (GraphManager.Instance.noiseCoroutine != null)
                    GraphManager.Instance.StopCoroutine(
                        GraphManager.Instance.noiseCoroutine);
                GraphManager.Instance.noiseCoroutine =
                    GraphManager.Instance.StartCoroutine(
                        GraphManager.Instance.NoiseRoutine());
            }
        }
    }

    // ─── เรียกจาก GraphManager.Refresh() ────────────────
    public void Refresh()
    {
        if (GraphManager.Instance == null) return;

        var gm      = GraphManager.Instance;
        var builder = gm.builder;

        if (!gm.simJam || gm.linkFidelities == null || gm.linkFidelities.Length == 0)
        {
            HideAllRows();
            if (fidAfterNoiseText != null) fidAfterNoiseText.text = "Fidelity after noise: —";
            if (statusText != null)        statusText.text = "";
            return;
        }

        var orderedLinks = gm.GetLinksInOrder();
        HideAllRows();

        float accumulated = 1f;
        int   rowIdx      = 0;

        foreach (int li in orderedLinks)
        {
            if (rowIdx >= rows.Length) break;
            if (li >= gm.linkFidelities.Length) continue;

            float  f       = gm.linkFidelities[li];
            var    (a, b)  = builder.links[li];
            string fromLbl = builder.nodeDataList[a].label;
            string toLbl   = builder.nodeDataList[b].label;

            accumulated *= f / 100f;
            Color col = FidColor(f);

            rows[rowIdx].SetActive(true);

            if (labelTexts[rowIdx] != null)
                labelTexts[rowIdx].text = fromLbl + " → " + toLbl;

            if (bars[rowIdx] != null)
            {
                bars[rowIdx].value = f;
                var fill = bars[rowIdx].fillRect;
                if (fill != null)
                {
                    var img = fill.GetComponent<Image>();
                    if (img != null) img.color = col;
                }
            }

            if (valueTexts[rowIdx] != null)
            {
                valueTexts[rowIdx].text  = Mathf.RoundToInt(f) + "%";
                valueTexts[rowIdx].color = col;
            }

            rowIdx++;
        }

        int totalFid = Mathf.Max(1, Mathf.RoundToInt(accumulated * 100));
        Color totalCol = FidColor(totalFid);

        if (fidAfterNoiseText != null)
        {
            fidAfterNoiseText.text  = "Fidelity after noise: " + totalFid + "%";
            fidAfterNoiseText.color = totalCol;
        }

        if (statusText != null)
        {
            if (totalFid >= 70)
            { statusText.text = "Good";     statusText.color = colorHigh; }
            else if (totalFid >= 40)
            { statusText.text = "Moderate"; statusText.color = colorMid;  }
            else
            { statusText.text = "Too low";  statusText.color = colorLow;  }
        }
    }

    // ─── Helpers ─────────────────────────────────────────
    void HideAllRows()
    {
        if (rows == null) return;
        foreach (var r in rows) if (r != null) r.SetActive(false);
    }

    void UpdateNoiseValueText(float v)
    {
        if (noiseValueText != null)
            noiseValueText.text = Mathf.RoundToInt(v * 100) + "%";
    }

    Color FidColor(float f)
    {
        if (f >= 80) return colorHigh;
        if (f >= 60) return colorMid;
        return colorLow;
    }
}