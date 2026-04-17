using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI txtStep;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDesc;
    public Button btnNext;
    public Button btnSkip;
    public Button btnBack;

    private int _currentStep = 0;

    private readonly (string title, string desc)[] _steps = new[]
    {
        (
            "Choose a Topology",
            "Select how nodes connect.\nTry Linear first, then explore Star, Mesh, Tree, or Ring."
        ),
        (
            "Adjust the Nodes",
            "Change the number of Quantum Repeaters.\nMore nodes = more hops = lower Fidelity."
        ),
        (
            "Simulate a Situation",
            "Toggle Node Fail, Noise, or Heavy Traffic\nto see how the network responds."
        ),
        (
            "Read the Metrics",
            "Check Fidelity, QBER, and E-Rate to measure\nthe health of your network.\nTap ? on each metric to learn more."
        )
    };

    void Start()
    {
        //PlayerPrefs.DeleteKey("TutorialDone"); // ลบออกก่อน Build จริง

        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
        {
            tutorialPanel.SetActive(true);
            ShowStep(0);
        }
        else
        {
            tutorialPanel.SetActive(false);
        }

        btnNext.onClick.AddListener(OnNext);
        btnSkip.onClick.AddListener(OnSkip);
        btnBack.onClick.AddListener(OnBack);
    }

    void ShowStep(int index)
    {
        _currentStep  = index;
        txtStep.text  = $"Step {index + 1} / {_steps.Length}";
        txtTitle.text = _steps[index].title;
        txtDesc.text  = _steps[index].desc;

        // ซ่อน Back ที่ Step แรก
        btnBack.gameObject.SetActive(index > 0);

        // ซ่อน Next ที่ Step สุดท้าย
        btnNext.gameObject.SetActive(index < _steps.Length - 1);

        // Step สุดท้าย btnSkip เปลี่ยนเป็น "Got it!"
        var skipLabel = btnSkip.GetComponentInChildren<TextMeshProUGUI>();
        if (skipLabel != null)
            skipLabel.text = (index == _steps.Length - 1) ? "Got it!" : "Skip";
    }

    void OnNext()
    {
        if (_currentStep < _steps.Length - 1)
            ShowStep(_currentStep + 1);
    }

    void OnBack()
    {
        if (_currentStep > 0)
            ShowStep(_currentStep - 1);
    }

    void OnSkip() => FinishTutorial();

    void FinishTutorial()
    {
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        tutorialPanel.SetActive(false);
    }

    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialDone");
        tutorialPanel.SetActive(true);
        ShowStep(0);
    }
}