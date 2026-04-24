using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("Topology Buttons")]
    public Button btnLinear, btnStar, btnMesh, btnTree, btnRing;

    [Header("Distance Fine-Tune Buttons")]
    public Button btnDistMinus;
    public Button btnDistPlus;

    [Header("Tutorial")]
    public Button btnHelp;
    public TutorialManager tutorialManager;

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

    private UISwitcher.UISwitcher[] _sitToggles;

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
            float snapped = Mathf.Round(v);
            if (!Mathf.Approximately(sliderDist.value, snapped))
                sliderDist.SetValueWithoutNotify(snapped);
            GraphManager.Instance.SetDistKm(snapped);
            lblDist.text = (int)snapped + " km";
        });

        sliderFidelity.onValueChanged.AddListener(v => {
            GraphManager.Instance.SetFidelity(v);
            lblFidelity.text = (int)v + "%";
        });

        // ค่า default sliders
        sliderNodes.minValue   = 3;  sliderNodes.maxValue   = 10;  sliderNodes.value   = 3;
        sliderDist.minValue    = 50; sliderDist.maxValue    = 500; sliderDist.value    = 50;
        sliderFidelity.minValue = 50; sliderFidelity.maxValue = 99; sliderFidelity.value = 95;

        // ─── Tutorial Help Button ──────────────────────────
        if (btnHelp != null && tutorialManager != null)
            btnHelp.onClick.AddListener(() => tutorialManager.ResetTutorial());

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
        _sitToggles = new[] { togNodeFail, togNoise, togHeavy, togDegrade, togCascade };

        if (togNodeFail != null)
        {
            togNodeFail.isOn = false;
            togNodeFail.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togNodeFail);
                if (v != GraphManager.Instance.simFail)
                    GraphManager.Instance.ToggleFail();
            });
            GraphManager.Instance.onSimFailChanged += v => {
                if (togNodeFail.isOn != v) togNodeFail.isOn = v;
            };
        }
        if (togNoise != null)
        {
            togNoise.isOn = false;
            togNoise.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togNoise);
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
                if (v) ExcludeSituation(togHeavy);
                if (v != GraphManager.Instance.simHeavy)
                    GraphManager.Instance.ToggleHeavy();
            });
        }
        if (togDegrade != null)
        {
            togDegrade.isOn = false;
            togDegrade.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togDegrade);
                if (v != GraphManager.Instance.simDegrade)
                    GraphManager.Instance.ToggleDegrade();
            });
        }
        if (togCascade != null)
        {
            togCascade.isOn = false;
            togCascade.onValueChanged.AddListener(v => {
                if (v) ExcludeSituation(togCascade);
                if (v != GraphManager.Instance.simCascade)
                    GraphManager.Instance.ToggleCascade();
            });
            GraphManager.Instance.onSimCascadeChanged += v => {
                if (togCascade.isOn != v) togCascade.isOn = v;
            };
        }

        // ─── Distance Fine-Tune Buttons ───────────────────
        if (btnDistMinus != null)
            btnDistMinus.onClick.AddListener(() => {
                float next = Mathf.Max(sliderDist.value - 1f, sliderDist.minValue);
                sliderDist.value = Mathf.Round(next);
            });

        if (btnDistPlus != null)
            btnDistPlus.onClick.AddListener(() => {
                float next = Mathf.Min(sliderDist.value + 1f, sliderDist.maxValue);
                sliderDist.value = Mathf.Round(next);
            });
    }
}