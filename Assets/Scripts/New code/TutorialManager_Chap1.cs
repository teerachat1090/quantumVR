using UnityEngine;
using UnityEngine.UI;

public class TutorialManager_Chap1 : MonoBehaviour
{
    [SerializeField] private Image dialogueImage;
    [SerializeField] private Sprite[] dialogueSprites;
    [SerializeField] private GameObject start;
    [SerializeField] private RectTransform tutorialRect;
    [SerializeField] private GameObject next;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogueSound;

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
        next.SetActive(false);
    }

    void OnEnable()
    {
        GateSocket.OnAnyGatePlaced += OnGatePlaced;
        GateSocket.OnAnyGateRemoved += OnGateRemoved;
        ButtonAction.OnMeasureButtonPressed += OnMeasured;
        ButtonAction.OnNextButtonPressed += OnNextPressed;
        ButtonAction.OnPrevButtonPressed += OnPrevPressed;
    }

    void OnDisable()
    {
        GateSocket.OnAnyGatePlaced -= OnGatePlaced;
        GateSocket.OnAnyGateRemoved -= OnGateRemoved;
        ButtonAction.OnMeasureButtonPressed -= OnMeasured;
        ButtonAction.OnNextButtonPressed -= OnNextPressed;
        ButtonAction.OnPrevButtonPressed -= OnPrevPressed;
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

    private void OnGateRemoved(string gateName)
    {
        Debug.Log($"OnGateRemoved fired: gate={gateName}, currentStep={currentStep}");
        if (currentStep == 7 && gateName == "X")
            NextStep();
    }

    private void OnMeasured()
    {
        if (currentStep == 2)
            NextStep();
    }

    private void OnNextPressed()
    {
        if (currentStep == 3)
            NextStep();
    }

    private void OnPrevPressed()
    {
        if (currentStep == 4)
            NextStep();
    }

    public void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }
        public void OnGuideClicked()
    {
        currentStep = 0;
        dialogueImage.sprite = dialogueSprites[0];
        tutorialRect.localPosition = startPos;
        tutorialRect.localEulerAngles = startRot;
        start.SetActive(true);
        next.SetActive(false);
        gameObject.SetActive(true);
    }    
        public void NextStep()
    {
        currentStep++;
        if (currentStep < dialogueSprites.Length)
        {
            dialogueImage.sprite = dialogueSprites[currentStep];
            next.SetActive(currentStep == 5 || currentStep == 6);
            audioSource.PlayOneShot(dialogueSound); // เพิ่มตรงนี้
        }
        else
            gameObject.SetActive(false);
    }
}