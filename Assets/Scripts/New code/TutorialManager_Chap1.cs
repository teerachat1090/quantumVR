using UnityEngine;
using UnityEngine.UI;

public class TutorialManager_Chap1 : MonoBehaviour
{
    [SerializeField] private Image dialogueImage;
    [SerializeField] private Sprite[] dialogueSprites;
    [SerializeField] private GameObject start;
    [SerializeField] private RectTransform tutorialRect;
    [SerializeField] private GameObject next;

    private int currentStep = 0;
    private Vector3 startPos = new Vector3(3f, 1.8f, 0f);
    private Vector3 afterStartPos = new Vector3(3f, 1.2f, -3.2f);
    private Vector3 startRot = new Vector3(0f, 90f, 0f);
    private Vector3 afterStartRot = new Vector3(0f, 135f, 0f);

    void Start()
    {
        tutorialRect.localPosition = startPos;
        tutorialRect.localEulerAngles = startRot;
        dialogueImage.sprite = dialogueSprites[0];
        next.SetActive(false); // ซ่อนไว้ก่อน
    }

    void OnEnable()
    {
        GateSocket.OnAnyGatePlaced += OnGatePlaced;
        ButtonAction.OnMeasureButtonPressed += OnMeasured; 
    }

    void OnDisable()
    {
        GateSocket.OnAnyGatePlaced -= OnGatePlaced;
        ButtonAction.OnMeasureButtonPressed -= OnMeasured;
    }

    public void OnStartClicked()
    {
        tutorialRect.localPosition = afterStartPos;
        tutorialRect.localEulerAngles = afterStartRot;
        start.SetActive(false);
        NextStep();
    }

        public void OnNextClicked()
    {
        NextStep();
    }
    private void OnGatePlaced(string gateName)
    {
        if (currentStep == 1 && gateName == "X")
            NextStep();
    }

    private void OnMeasured()
    {
        if (currentStep == 2)
            NextStep();
    }

        public void NextStep()
    {
        currentStep++;
        if (currentStep < dialogueSprites.Length)
        {
            dialogueImage.sprite = dialogueSprites[currentStep];
            // แสดงปุ่ม next เฉพาะ step 3, 4 (Watch Bloch Sphere, See result)
            next.SetActive(currentStep == 3 || currentStep == 4);
        }
        else
            gameObject.SetActive(false);
    }
}