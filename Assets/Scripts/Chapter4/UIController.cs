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
        sliderNodes.minValue = 3; sliderNodes.maxValue = 10; sliderNodes.value = 5;
        sliderDist.minValue  = 50; sliderDist.maxValue = 500; sliderDist.value = 150;
        sliderFidelity.minValue = 60; sliderFidelity.maxValue = 99; sliderFidelity.value = 90;

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

        // ─── Situation Toggles ────────────────────────────
        if (togNodeFail != null)
        {
            togNodeFail.isOn = false;
            togNodeFail.onValueChanged.AddListener(v => {
                // sync state กับ GraphManager
                if (v != GraphManager.Instance.simFail)
                    GraphManager.Instance.ToggleFail();
            });
        }
        if (togNoise != null)
        {
            togNoise.isOn = false;
            togNoise.onValueChanged.AddListener(v => {
                if (v != GraphManager.Instance.simJam)
                    GraphManager.Instance.ToggleJam();
            });
        }
        if (togHeavy != null)
        {
            togHeavy.isOn = false;
            togHeavy.onValueChanged.AddListener(v => {
                if (v != GraphManager.Instance.simHeavy)
                    GraphManager.Instance.ToggleHeavy();
            });
        }
        if (togDegrade != null)
        {
            togDegrade.isOn = false;
            togDegrade.onValueChanged.AddListener(v => {
                if (v != GraphManager.Instance.simDegrade)
                    GraphManager.Instance.ToggleDegrade();
            });
        }
        if (togCascade != null)
        {
            togCascade.isOn = false;
            togCascade.onValueChanged.AddListener(v => {
                if (v != GraphManager.Instance.simCascade)
                    GraphManager.Instance.ToggleCascade();
            });
        }
    }
}