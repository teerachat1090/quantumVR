using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SwitchToggle : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Image background;
    public Image handle;

    [Header("Colors")]
    public Color colorOn  = new Color(0.42f, 0.39f, 0.83f); // ม่วงเข้ม
    public Color colorOff = new Color(0.55f, 0.55f, 0.55f); // เทา
    public Color handleColor = Color.white;

    [Header("Settings")]
    public bool isOn = false;
    public float animDuration = 0.15f;

    // Event ให้ UIController subscribe
    public System.Action<bool> onValueChanged;

    // ตำแหน่ง handle
    private float handleOnX;   // ชิดขวา
    private float handleOffX;  // ชิดซ้าย
    private Coroutine animRoutine;

    void Awake()
    {
        // คำนวณตำแหน่งจาก Background width
        float bgW = background.rectTransform.rect.width;
        float hdW = handle.rectTransform.rect.width;
        float pad = 2f;

        handleOnX  =  (bgW / 2f) - (hdW / 2f) - pad;
        handleOffX = -(bgW / 2f) + (hdW / 2f) + pad;

        handle.color = handleColor;

        // set ค่าเริ่มต้นโดยไม่ animate
        ApplyImmediate(isOn);
    }

    // ─── คลิก ────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        SetValue(!isOn);
    }

    // ─── Set จากภายนอก (UIController) ───────────────────
    public void SetValue(bool value, bool animate = true)
    {
        isOn = value;
        onValueChanged?.Invoke(isOn);

        if (animRoutine != null) StopCoroutine(animRoutine);

        if (animate && gameObject.activeInHierarchy)
            animRoutine = StartCoroutine(Animate(isOn));
        else
            ApplyImmediate(isOn);
    }

    // ─── Animate ─────────────────────────────────────────
    IEnumerator Animate(bool on)
    {
        float targetX    = on ? handleOnX : handleOffX;
        Color targetBg   = on ? colorOn   : colorOff;
        Vector2 startPos = handle.rectTransform.anchoredPosition;
        Color   startBg  = background.color;
        float   t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // ease out cubic

            handle.rectTransform.anchoredPosition =
                new Vector2(Mathf.Lerp(startPos.x, targetX, e), 0);
            background.color = Color.Lerp(startBg, targetBg, e);
            yield return null;
        }

        ApplyImmediate(on);
    }

    void ApplyImmediate(bool on)
    {
        handle.rectTransform.anchoredPosition = new Vector2(on ? handleOnX : handleOffX, 0);
        background.color = on ? colorOn : colorOff;
    }
}