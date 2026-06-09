using UnityEngine;
using TMPro;

public class MetricInfoPopup : MonoBehaviour
{
    public static MetricInfoPopup Instance;

    [Header("UI References")]
    public GameObject  popupPanel;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDescription;

    private string _currentTitle; // เก็บไว้เช็คว่ากด ? ปุ่มเดิมหรือเปล่า

    void Awake()
    {
        Instance = this;
        popupPanel.SetActive(false);
    }

    public void Toggle(string title, string description)
    {
        bool sameButton = _currentTitle == title;

        // กด ? ปุ่มเดิมที่เปิดอยู่ → ปิด
        if (popupPanel.activeSelf && sameButton)
        {
            popupPanel.SetActive(false);
            return;
        }

        // กด ? ปุ่มอื่น หรือปิดอยู่ → เปิด/อัปเดต content
        _currentTitle           = title;
        txtTitle.text           = title;
        txtDescription.text     = description;
        popupPanel.SetActive(true);
    }

    public void Hide()
    {
        _currentTitle = null;
        popupPanel.SetActive(false);
    }
}