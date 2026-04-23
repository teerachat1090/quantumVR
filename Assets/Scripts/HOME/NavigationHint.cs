using UnityEngine;
using TMPro;

public class NavigationHint : MonoBehaviour
{
    public CanvasGroup hintGroup;
    public float displayDuration = 5f;
    public float fadeDuration = 1f;

    private float timer = 0f;
    private bool fading = false;

    void Start()
    {
        hintGroup.alpha = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // เริ่ม fade หลัง displayDuration วินาที
        if (timer >= displayDuration && !fading)
        {
            fading = true;
        }

        if (fading)
        {
            hintGroup.alpha -= Time.deltaTime / fadeDuration;

            if (hintGroup.alpha <= 0f)
            {
                hintGroup.alpha = 0f;
                gameObject.SetActive(false);
            }
        }
    }
}