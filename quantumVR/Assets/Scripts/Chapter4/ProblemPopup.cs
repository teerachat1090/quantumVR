using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProblemPopup : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject[] pages;

    [Header("Button")]
    [SerializeField] private Button   btnNext;
    [SerializeField] private TMP_Text btnNextLabel;

    private int _cur = 0;

    void Start()
    {
        gameObject.SetActive(false);
        btnNext.onClick.AddListener(OnNextPressed);
    }

    public void OpenPopup()
    {
        gameObject.SetActive(true);
        _cur = 0;
        Refresh();
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    void OnNextPressed()
    {
        if (_cur < pages.Length - 1)
        {
            _cur++;
            Refresh();
        }
        else
        {
            ClosePopup();
        }
    }

    void Refresh()
    {
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == _cur);

        btnNextLabel.text = _cur == pages.Length - 1 ? "Close" : "Next";
    }

    void OnDestroy()
    {
        btnNext.onClick.RemoveAllListeners();
    }
}