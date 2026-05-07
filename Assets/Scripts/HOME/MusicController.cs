using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class MusicController : MonoBehaviour
{
    [Header("References")]
    public AudioSource musicSource;
    public GameObject musicPanel;
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioClip openSound;
    private AudioSource audioSource;

    private bool isPanelOpen = false;
    private InputDevice controller;
    private bool lastButtonState = false;

    void Start()
    {
        musicPanel.SetActive(false);

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = musicSource.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

   void Update()
{
    if (!controller.isValid)
    {
        controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        Debug.Log("[Music] controller valid: " + controller.isValid);
    }

    bool buttonPressed = false;
    controller.TryGetFeatureValue(CommonUsages.primaryButton, out buttonPressed);

    #if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.A))
    {
        Debug.Log("[Music] Editor A key pressed");
        TogglePanel();
    }
    #endif

    if (buttonPressed && !lastButtonState)
    {
        Debug.Log("[Music] A button pressed on controller");
        TogglePanel();
    }

    lastButtonState = buttonPressed;
}

void TogglePanel()
{
    isPanelOpen = !isPanelOpen;
    Debug.Log("[Music] TogglePanel called, isPanelOpen = " + isPanelOpen);
    musicPanel.SetActive(isPanelOpen);

    if (openSound != null)
        audioSource.PlayOneShot(openSound);
    else
        Debug.LogWarning("[Music] openSound is NULL");
}

    void OnVolumeChanged(float value)
    {
        musicSource.volume = value;
    }

    public void SetMute(bool isMuted)
    {
        musicSource.mute = isMuted;
    }
}