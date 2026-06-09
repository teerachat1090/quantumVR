using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class AlarmButton : MonoBehaviour
{
    [Header("References")]
    public Button alarmBtn;

    [Header("Pulse Animation")]
    public float pulseScale = 1.15f;
    public float pulseSpeed = 0.6f;

    [Header("Alarm Sound")]
    public AudioClip alarmSound;
    public bool loopSound = true;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Problem Cards")]
    public GameObject problemPopup;
    public GameObject cardDisplay;
    public Sprite[] cardImages;
    public Button nextButton;
    public Button backButton;
    public Button closeButton;
    public Button enterNextSceneButton;  // ✅ เพิ่มปุ่มใหม่

    [Header("Hide When Popup Opens")]
    public GameObject canvasNumpad;   // ← ลาก CanvasNumpad ใส่
    public GameObject canvasResult;   // ← ลาก CanvasResult ใส่

    private AudioSource audioSource;
    private bool isAlarming = false;
    private int currentCardIndex = 0;
    private Coroutine blinkCoroutine;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = alarmSound;
        audioSource.loop = loopSound;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;

        alarmBtn.onClick.AddListener(OnAlarmPressed);
        nextButton.onClick.AddListener(OnNextCard);
        backButton.onClick.AddListener(OnBackCard);
        closeButton.onClick.AddListener(ClosePopup);

        problemPopup.SetActive(false);
        gameObject.SetActive(false);

        if (enterNextSceneButton)
        enterNextSceneButton.onClick.AddListener(OnEnterNextScene);
    }

        void OnEnterNextScene()
    {
        SceneManager.LoadScene("Chapter4.1"); // เปลี่ยนเป็นชื่อ Scene จริงๆ
    }
     public void ShowAlarm()
    {
        gameObject.SetActive(true);
        isAlarming = false;
        problemPopup.SetActive(false);

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkAlarm());

        if (alarmSound != null)
        {
            audioSource.clip = alarmSound;
            audioSource.loop = loopSound;
            audioSource.Play();
            isAlarming = true;
        }
    }

    IEnumerator BlinkAlarm()
    {
        Image btnImage = alarmBtn.GetComponent<Image>();
        Color original = btnImage.color;
        Color highlight = Color.red;

        while (isAlarming)
        {
            btnImage.color = highlight;
            yield return new WaitForSeconds(0.3f);
            btnImage.color = original;
            yield return new WaitForSeconds(0.3f);
        }

        btnImage.color = original;
    }

    void OnAlarmPressed()
    {
        if (isAlarming)
        {
            audioSource.Stop();
            isAlarming = false;

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                alarmBtn.GetComponent<Image>().color = Color.white;
            }

            ShowCards();
        }
    }

    void ShowCards()
{
    if (cardImages == null || cardImages.Length == 0) return;

    if (canvasNumpad) canvasNumpad.SetActive(false);
    if (canvasResult) canvasResult.SetActive(false);

    currentCardIndex = 0;
    if (enterNextSceneButton)
        enterNextSceneButton.gameObject.SetActive(false);

    UpdateCard();
    problemPopup.SetActive(true);
}
    void OnNextCard()
    {
        if (currentCardIndex < cardImages.Length - 1)
        {
            currentCardIndex++;
            UpdateCard();
        }
    }

    void OnBackCard()
    {
        if (currentCardIndex > 0)
        {
            currentCardIndex--;
            UpdateCard();
        }
    }

    void ClosePopup()
    {
        problemPopup.SetActive(false);
        currentCardIndex = 0;

        // คืน Numpad และ Result
        if (canvasNumpad) canvasNumpad.SetActive(true);
        if (canvasResult) canvasResult.SetActive(true);

        // ✅ ซ่อนตัวเอง (Alarm Button หายไป รอกรอกรหัสใหม่)
        gameObject.SetActive(false);
    }   

    void UpdateCard()
    {
        Image img = cardDisplay.GetComponentInChildren<Image>();
        if (img != null)
            img.sprite = cardImages[currentCardIndex];

        bool isLast = currentCardIndex == cardImages.Length - 1;

        nextButton.gameObject.SetActive(!isLast);
        backButton.gameObject.SetActive(currentCardIndex > 0);

        // ✅ การ์ดสุดท้าย → ซ่อน Cancel, แสดง Enter Next Scene
        closeButton.gameObject.SetActive(!isLast);
        if (enterNextSceneButton)
            enterNextSceneButton.gameObject.SetActive(isLast);
    }
}