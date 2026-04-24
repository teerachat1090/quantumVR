using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class VRCardScroller : MonoBehaviour
{
    [Header("Card Settings")]
    public List<CardData> cards = new List<CardData>();
    
    [Header("Scene Settings")]
    public bool enableSceneTransition = true;
    
    [Header("Scroll Settings")]
    public float cardSpacing = 350f;
    public float smoothTime = 0.3f;
    public float centerOffset = 0f;
    
    [Header("Visual Settings")]
    public float normalScale = 0.9f;
    public float selectedScale = 1.1f;
    public float unselectedAlpha = 0.6f;
    
    [Header("VR Input")]
    public XRNode controllerNode = XRNode.RightHand;
    public float thumbstickThreshold = 0.3f;

    [Header("Audio")]
    public AudioClip scrollSound;
    private AudioSource audioSource;
    public AudioClip confirmSound;

    private RectTransform sliderRect;
    private int currentIndex = 1;
    private float targetPosition = 0f;
    private float currentVelocity = 0f;
    private InputDevice controller;
    private float lastInputTime = 0f;
    private float inputCooldown = 0.3f;
    
    [System.Serializable]
    public class CardData
    {
        public GameObject cardObject;
        public Sprite normalSprite;
        public Sprite selectedSprite;
        public string sceneName;
        
        [HideInInspector] public Image cardImage;
        [HideInInspector] public CanvasGroup canvasGroup;
        [HideInInspector] public Canvas canvas;
    }
    
    void Start()
    {
        sliderRect = GetComponent<RectTransform>();
        InitializeCards();
        UpdateCardPosition();
        UpdateCardVisuals();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }
    
    void Update()
    {
        RefreshController();
        HandleInput();
        SmoothScroll();
        UpdateCardVisuals();
    }
    
    void RefreshController()
    {
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
    }
    
    void InitializeCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].cardObject != null)
            {
                cards[i].cardImage = cards[i].cardObject.GetComponent<Image>();
                cards[i].canvasGroup = cards[i].cardObject.GetComponent<CanvasGroup>();
                if (cards[i].canvasGroup == null)
                    cards[i].canvasGroup = cards[i].cardObject.AddComponent<CanvasGroup>();
                
                if (cards[i].cardImage != null && cards[i].normalSprite != null)
                    cards[i].cardImage.sprite = cards[i].normalSprite;
            }
        }
    }
    
    void HandleInput()
    {
        if (Time.time - lastInputTime < inputCooldown) return;
        if (!controller.isValid) return;
        
        Vector2 thumbstick = Vector2.zero;
        controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick);
        
        if (thumbstick.x < -thumbstickThreshold)
        {
            ScrollLeft();
            lastInputTime = Time.time;
        }
        else if (thumbstick.x > thumbstickThreshold)
        {
            ScrollRight();
            lastInputTime = Time.time;
        }

        // ลบ trigger check ออกจากตรงนี้แล้ว — ให้ VRCardSelector จัดการแทน
        
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftArrow)) ScrollLeft();
        if (Input.GetKeyDown(KeyCode.RightArrow)) ScrollRight();
        #endif
    }
    
    void ScrollLeft()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateCardPosition();
            PlayHaptic();
        }
    }
    
    void ScrollRight()
    {
        if (currentIndex < cards.Count - 1)
        {
            currentIndex++;
            UpdateCardPosition();
            PlayHaptic();
        }
    }
    
    void UpdateCardPosition()
    {
        targetPosition = (-currentIndex * cardSpacing) + centerOffset;
    }
    
    void SmoothScroll()
    {
        float newX = Mathf.SmoothDamp(
            sliderRect.anchoredPosition.x,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );
        sliderRect.anchoredPosition = new Vector2(newX, sliderRect.anchoredPosition.y);
    }
    
    void UpdateCardVisuals()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].cardObject == null) continue;
            bool isSelected = (i == currentIndex);
            ChangeCardImage(i, isSelected);
            ChangeCardScale(i, isSelected);
            ChangeCardAlpha(i, isSelected);
        }
    }
    
    void ChangeCardImage(int index, bool isSelected)
    {
        if (cards[index].cardImage == null) return;
        Sprite targetSprite = isSelected ? cards[index].selectedSprite : cards[index].normalSprite;
        if (targetSprite != null)
            cards[index].cardImage.sprite = targetSprite;
    }
    
    void ChangeCardScale(int index, bool isSelected)
    {
        float targetScale = isSelected ? selectedScale : normalScale;
        cards[index].cardObject.transform.localScale = Vector3.Lerp(
            cards[index].cardObject.transform.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * 10f
        );
    }
    
    void ChangeCardAlpha(int index, bool isSelected)
    {
        if (cards[index].canvasGroup == null) return;
        float targetAlpha = isSelected ? 1f : unselectedAlpha;
        cards[index].canvasGroup.alpha = Mathf.Lerp(
            cards[index].canvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * 10f
        );
    }
    
    void PlayHaptic()
    {
        if (controller.isValid)
            controller.SendHapticImpulse(0, 0.3f, 0.1f);
        
        if (audioSource != null && scrollSound != null)
            audioSource.PlayOneShot(scrollSound);
    }
    
    public void ConfirmSelection()
    {
        CardData selectedCard = GetCurrentCard();
        
        if (selectedCard == null || string.IsNullOrEmpty(selectedCard.sceneName))
        {
            Debug.LogWarning("No scene name assigned to this card!");
            return;
        }
        
        Debug.Log("Loading scene: " + selectedCard.sceneName);
        PlayHaptic();
        
        if (audioSource != null && confirmSound != null)
            audioSource.PlayOneShot(confirmSound);
        
        LoadScene(selectedCard.sceneName);
    }

    void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogError("Scene '" + sceneName + "' not found in Build Settings!");
    }
    
    public CardData GetCurrentCard()
    {
        if (currentIndex >= 0 && currentIndex < cards.Count)
            return cards[currentIndex];
        return null;
    }
    
    public int GetCurrentIndex()
    {
        return currentIndex;
    }
}