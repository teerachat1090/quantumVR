using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AliceNumpad : MonoBehaviour
{
    [Header("Alice - Display")]
    [SerializeField] private TMP_Text textDisplay;
    [SerializeField] private Image[] dots;
    [SerializeField] private Color dotEmpty  = new Color(0.81f, 0.79f, 0.96f);
    [SerializeField] private Color dotFilled = new Color(0.33f, 0.29f, 0.72f);

    [Header("Bob - Output")]
    [SerializeField] private TMP_Text[] bobCells;
    [SerializeField] private TMP_Text   bobStatusText;
    [SerializeField] private GameObject bobStatusPanel;

    [Header("Alice - Status")]
    [SerializeField] private TMP_Text aliceStatusText;

    // ✅ จุดที่ 1 — เพิ่ม field ตรงนี้
    [Header("Alarm")]
    [SerializeField] private AlarmButton alarmButton;

    private string _code = "";
    private bool   _sending = false;
    private const int MAX = 4;

    void Start()
    {
        if (bobStatusPanel) bobStatusPanel.SetActive(false);
        ResetBobCells();
        RefreshDisplay();
    }

    public void PressDigit(int digit)
    {
        if (_code.Length >= MAX || _sending) return;
        _code += digit.ToString();
        RefreshDisplay();
    }

    public void PressDelete()
    {
        if (_code.Length == 0 || _sending) return;
        _code = _code.Substring(0, _code.Length - 1);
        RefreshDisplay();
    }

    public void PressSend()
    {
        if (_code.Length == 0 || _sending) return;
        StartCoroutine(SendRoutine(_code));
        _code = "";
        RefreshDisplay();
    }

    IEnumerator SendRoutine(string sent)
    {
        _sending = true;

        if (aliceStatusText) aliceStatusText.text = "Sending: " + sent;

        ResetBobCells();
        if (bobStatusPanel) bobStatusPanel.SetActive(true);
        if (bobStatusText)  bobStatusText.text = "Receiving...";

        for (int i = 0; i < sent.Length; i++)
        {
            yield return new WaitForSeconds(0.2f);
            if (i < bobCells.Length && bobCells[i] != null)
                bobCells[i].text = sent[i].ToString();
        }

        yield return new WaitForSeconds(0.3f);
        if (bobStatusText)  bobStatusText.text = "Received: " + sent;
        if (aliceStatusText) aliceStatusText.text = "Sent! Enter new code.";

        // ✅ จุดที่ 2 — เพิ่มบรรทัดนี้
        if (alarmButton) alarmButton.ShowAlarm();

        _sending = false;
    }

    void ResetBobCells()
    {
        foreach (var c in bobCells)
            if (c != null) c.text = "?";
    }

    void RefreshDisplay()
    {
        if (textDisplay)
            textDisplay.text = _code.Length > 0
                ? _code[_code.Length - 1].ToString() : "_";

        for (int i = 0; i < dots.Length; i++)
            if (dots[i] != null)
                dots[i].color = i < _code.Length ? dotFilled : dotEmpty;

        if (aliceStatusText && !_sending)
            aliceStatusText.text = _code.Length > 0
                ? "Entering... (" + _code.Length + "/4)" : "Enter code...";
    }
}