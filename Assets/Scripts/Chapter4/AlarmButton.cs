using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AlarmButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button        alarmBtn;
    [SerializeField] private ProblemPopup  problemPopup;

    [Header("Pulse Animation")]
    [SerializeField] private float pulseScale  = 1.15f;
    [SerializeField] private float pulseSpeed  = 0.6f;

    private RectTransform _rect;
    private Coroutine     _pulseCoroutine;

    void Start()
    {
        _rect = alarmBtn.GetComponent<RectTransform>();
        alarmBtn.gameObject.SetActive(false);
        alarmBtn.onClick.AddListener(OnAlarmPressed);
    }

    // เรียกจาก AliceNumpad หลัง Send สำเร็จ
    public void ShowAlarm()
    {
        alarmBtn.gameObject.SetActive(true);
        _pulseCoroutine = StartCoroutine(PulseLoop());
    }

    public void HideAlarm()
    {
        alarmBtn.gameObject.SetActive(false);
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _rect.localScale = Vector3.one;
    }

    void OnAlarmPressed()
    {
        HideAlarm();
        if (problemPopup) problemPopup.OpenPopup();
    }

    IEnumerator PulseLoop()
    {
        while (true)
        {
            // scale ขึ้น
            yield return ScaleTo(Vector3.one * pulseScale, pulseSpeed / 2f);
            // scale ลง
            yield return ScaleTo(Vector3.one, pulseSpeed / 2f);
        }
    }

    IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = _rect.localScale;
        float   t     = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            _rect.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        _rect.localScale = target;
    }

    void OnDestroy()
    {
        alarmBtn.onClick.RemoveAllListeners();
    }
}