using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("Topology Buttons")]
    public Button btnLinear, btnStar, btnMesh, btnTree, btnRing;

    [Header("Sliders")]
    public Slider sliderNodes;
    public Slider sliderDist;
    public Slider sliderFidelity;

    [Header("Slider Values")]
    public TextMeshProUGUI lblNodes;
    public TextMeshProUGUI lblDist;
    public TextMeshProUGUI lblFidelity;

    [Header("Overlay Toggles")]
    public UISwitcher.UISwitcher togLabel;
    public UISwitcher.UISwitcher togDist;
    public UISwitcher.UISwitcher togFid;
    public UISwitcher.UISwitcher togFlow;

    [Header("Situation Toggles")]
    public UISwitcher.UISwitcher togNodeFail;
    public UISwitcher.UISwitcher togNoise;
    public UISwitcher.UISwitcher togHeavy;
    public UISwitcher.UISwitcher togDegrade;
    public UISwitcher.UISwitcher togCascade;

    // รายชื่อ Situation toggles ทั้งหมด สำหรับทำ exclusive (radio group)
    private UISwitcher.UISwitcher[] _sitToggles;

    // ปิด Situation toggles ทุกอันยกเว้น active
    // เรียกก่อน update GraphManager เพื่อให้ listener ของแต่ละ toggle ดับ sim state ที่ค้างอยู่
    void ExcludeSituation(UISwitcher.UISwitcher active)
    {
        foreach (var tog in _sitToggles)
            if (tog != null && tog != active && tog.isOn)
                tog.isOn = false;
    }

    void Start()
    {
        // ─── Topology Buttons ─────────────────────────────
        btnLinear.onClick.AddListener(() => GraphManager.Instance.SetTopo("linear"));
        btnStar.onClick.AddListener(()   => GraphManager.Instance.SetTopo("star"));
        btnMesh.onClick.AddListener(()   => GraphManager.Instance.SetTopo("mesh"));
        btnTree.onClick.AddListener(()   => GraphManager.Instance.SetTopo("tree"));
        btnRing.onClick.AddListener(()   => GraphManager.Instance.SetTopo("ring"));

        // ─── Sliders ──────────────────────────────────────
        sliderNodes.onValueChanged.AddListener(v => {
            GraphManager.Instance.SetNodeCount((int)v);
            lblNodes.text = ((int)v).ToString();
        });
        sliderDist.onValueChanged.AddListener(v => {
            GraphManager.Instance.SetDistKm(v);
            lblDist.text = (int)v + " km";
        });
        sliderFidelity.onValueChanged.AddListener(v => {
            GraphManager.Instance.SetFidelity(v);
            lblFidelity.text = (int)v + "%";
        });

        // ค่า default sliders
        sliderNodes.minValue = 3; sliderNodes.maxValue = 10; sliderNodes.value = 3;
        sliderDist.minValue  = 50; sliderDist.maxValue = 500; sliderDist.value = 50;
        sliderFidelity.minValue = 50; sliderFidelity.maxValue = 99; sliderFidelity.value = 95;

        // ─── Overlay Toggles ──────────────────────────────
        if (togLabel != null)
        {
            togLabel.isOn = true;
            togLabel.onValueChanged.AddListener(v => GraphManager.Instance.SetOvLabel(v));
        }
        if (togDist != null)
        {
            togDist.isOn = false;
            togDist.onValueChanged.AddListener(v => GraphManager.Instance.SetOvDist(v));
        }
        if (togFid != null)
        {
            togFid.isOn = false;
            togFid.onValueChanged.AddListener(v => GraphManager.Instance.SetOvFid(v));
        }
        if (togFlow != null)
        {
            togFlow.isOn = true;
            togFlow.onValueChanged.AddListener(v => GraphManager.Instance.SetOvFlow(v));
        }

        // ─── Situation Toggles (exclusive / radio group) ─────────────────
        _sitToggles = new[] { togNodeFail, togNoise, togHeavy, togDegrade, togCascade };

        if (togNodeFail != null)
        {
            togNodeFail.isOn = false;
            togNodeFail.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togNodeFail);   // เปิด → ปิดอันอื่น
                if (v != GraphManager.Instance.simFail)
                    GraphManager.Instance.ToggleFail();
            });
            // sync toggle เมื่อ GraphManager ปิด simFail อัตโนมัติ
            GraphManager.Instance.onSimFailChanged += v => {
                if (togNodeFail.isOn != v) togNodeFail.isOn = v;
            };
        }
        if (togNoise != null)
        {
            togNoise.isOn = false;
            togNoise.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togNoise);      // เปิด → ปิดอันอื่น
                if (v != GraphManager.Instance.simJam)
                    GraphManager.Instance.ToggleJam();
            });
            GraphManager.Instance.onSimJamChanged += v => {
                if (togNoise.isOn != v) togNoise.isOn = v;
            };
        }
        if (togHeavy != null)
        {
            togHeavy.isOn = false;
            togHeavy.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togHeavy);      // เปิด → ปิดอันอื่น
                if (v != GraphManager.Instance.simHeavy)
                    GraphManager.Instance.ToggleHeavy();
            });
        }
        if (togDegrade != null)
        {
            togDegrade.isOn = false;
            togDegrade.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togDegrade);    // เปิด → ปิดอันอื่น
                if (v != GraphManager.Instance.simDegrade)
                    GraphManager.Instance.ToggleDegrade();
            });
        }
        if (togCascade != null)
        {
            togCascade.isOn = false;
            togCascade.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togCascade);    // เปิด → ปิดอันอื่น
                if (v != GraphManager.Instance.simCascade)
                    GraphManager.Instance.ToggleCascade();
            });
            // sync toggle เมื่อ GraphManager ปิด simCascade อัตโนมัติ
            GraphManager.Instance.onSimCascadeChanged += v => {
                if (togCascade.isOn != v) togCascade.isOn = v;
            };
        }
    }
}