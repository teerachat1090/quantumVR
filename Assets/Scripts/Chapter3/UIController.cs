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

    void Start()
    {
        // Topology buttons
        btnLinear.onClick.AddListener(() => GraphManager.Instance.SetTopo("linear"));
        btnStar.onClick.AddListener(()   => GraphManager.Instance.SetTopo("star"));
        btnMesh.onClick.AddListener(()   => GraphManager.Instance.SetTopo("mesh"));
        btnTree.onClick.AddListener(()   => GraphManager.Instance.SetTopo("tree"));
        btnRing.onClick.AddListener(()   => GraphManager.Instance.SetTopo("ring"));

        // Sliders
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

        // ค่า default
        sliderNodes.minValue = 3;
        sliderNodes.maxValue = 10;
        sliderNodes.value    = 5;

        sliderDist.minValue = 50;
        sliderDist.maxValue = 500;
        sliderDist.value    = 150;

        sliderFidelity.minValue = 60;
        sliderFidelity.maxValue = 99;
        sliderFidelity.value    = 90;
    }
}