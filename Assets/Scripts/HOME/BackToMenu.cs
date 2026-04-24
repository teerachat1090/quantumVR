using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections;

public class BackToMenu : MonoBehaviour
{
    public string menuSceneName = "MainMenu";
    public GameObject confirmPopup;

    [Header("Audio")]
    public AudioClip popupSound;
    public AudioClip confirmSound;
    public AudioClip buttonSound;
    private AudioSource audioSource;

    private InputDevice controller;
    private bool lastBackState = false;
    private bool popupOpen = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool backPressed = false;
        controller.TryGetFeatureValue(CommonUsages.secondaryButton, out backPressed);

        if (backPressed && !lastBackState)
        {
            if (!popupOpen)
                ShowPopup();
            else
                HidePopup();
        }

        lastBackState = backPressed;

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!popupOpen) ShowPopup();
            else HidePopup();
        }
        #endif
    }

    public void ShowPopup()
    {
        Camera cam = Camera.main;

        confirmPopup.transform.position = cam.transform.position +
                                          cam.transform.forward * 0.8f +
                                          Vector3.up * -0.2f;

        confirmPopup.transform.rotation = Quaternion.LookRotation(
            confirmPopup.transform.position - cam.transform.position
        );

        if (audioSource != null && popupSound != null)
            audioSource.PlayOneShot(popupSound);

        confirmPopup.SetActive(true);
        popupOpen = true;
    }

    public void HidePopup()
    {
        if (audioSource != null && buttonSound != null)
            audioSource.PlayOneShot(buttonSound);

        confirmPopup.SetActive(false);
        popupOpen = false;
    }

    public void ConfirmExit()
{
    if (audioSource != null && confirmSound != null)
    {
        audioSource.PlayOneShot(confirmSound);
        StartCoroutine(LoadAfterSound(confirmSound.length));
    }
    else
    {
        SceneManager.LoadScene(menuSceneName);
    }
}

    private IEnumerator LoadAfterSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(menuSceneName);
    }
}