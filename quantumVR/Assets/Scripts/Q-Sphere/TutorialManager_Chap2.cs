using UnityEngine;
using UnityEngine.UI;

public class TutorialManager_Chap2 : MonoBehaviour
{
    [SerializeField] private Image         dialogueImage;
    [SerializeField] private Sprite[]      dialogueSprites;
    [SerializeField] private GameObject    start;
    [SerializeField] private RectTransform tutorialRect;
    [SerializeField] private GameObject    next;
    [SerializeField] private AudioSource   audioSource;
    [SerializeField] private AudioClip     dialogueSound;

    private int currentStep = 0;

    private Vector3 startPos      = new Vector3(3f,  1.8f,  0f);
    private Vector3 afterStartPos = new Vector3(3f,  0.855f, -3.2f);
    private Vector3 startRot      = new Vector3(0f,  90f,  0f);
    private Vector3 afterStartRot = new Vector3(0f, 135f,  0f);

    private bool hPlaced            = false;
    private bool cnotStepDone       = false;
    private bool modeUsedOnce       = false;
    private bool modeExitedSequence = false;
    private bool measureUsedOnce    = false;
    private bool modeStepHandled    = false; // ← กัน Coroutine fire ซ้ำที่ step 10
    private int  nextPressCount     = 0;

    // Guard: ป้องกัน NextStep() ถูกเรียกซ้อนกัน (double-fire)
    private bool isAdvancing = false;

    /*
     * STEP MAP — 14 sprites (Element 0–13)
     * ──────────────────────────────────────────────────────────────
     *  0  Intro / welcome                        → Start button
     *  1  วาง H gate บน Qubit 0                  → OnAnyGatePlaced("H")
     *  2  H = superposition, วาง CNOT            → OnAnyGatePlaced("CNOT")
     *  3  CNOT links qubits → Press MODE         → OnModeButtonPressed
     *  4  Bell State formula                     → OnNextButtonPressed x2
     *  5  Q-sphere — dot = state                 → Next button (manual)
     *  6  Q-sphere — dot size = probability      → Next button (manual)
     *  7  Empty dots |01⟩ |10⟩ + phase           → Next button (manual)
     *  8  Entangled — one system                 → OnModeButtonPressed (exit seq)
     *                                              + OnMeasurePlaced
     *  9  Collapse → one outcome                 → OnMeasurePlaced
     * 10  Correlated result                      → OnModeButtonPressed (clear)
     * 11  50/50 many times                       → Next button (manual)
     * 12  Spooky action                          → Next button (manual)
     * 13  Final Challenge                        → Next button (manual)
     * ──────────────────────────────────────────────────────────────
     */
private readonly System.Collections.Generic.HashSet<int> manualNextSteps
    = new System.Collections.Generic.HashSet<int> { 5, 6, 7, 9, 11, 12, 13 };

    // ─────────────────────────── Unity lifecycle ──────────────────────────

    void Start()
    {
        tutorialRect.localPosition    = startPos;
        tutorialRect.localEulerAngles = startRot;
        dialogueImage.sprite          = dialogueSprites[0];
        next.SetActive(false);
    }

    void OnEnable()
    {
        GateSocket.OnAnyGatePlaced       += OnGatePlaced;
        GateSocket.OnMeasurePlaced       += OnMeasurePlaced;
        ButtonAction.OnModeButtonPressed += OnModePressed;
        ButtonAction.OnNextButtonPressed += OnNextPressed;
    }

    void OnDisable()
    {
        GateSocket.OnAnyGatePlaced       -= OnGatePlaced;
        GateSocket.OnMeasurePlaced       -= OnMeasurePlaced;
        ButtonAction.OnModeButtonPressed -= OnModePressed;
        ButtonAction.OnNextButtonPressed -= OnNextPressed;
    }

    // ─────────────────────────── Button callbacks ─────────────────────────

    public void OnStartClicked()
    {
        tutorialRect.localPosition    = afterStartPos;
        tutorialRect.localEulerAngles = afterStartRot;
        start.SetActive(false);
        NextStep(); // → step 1
    }

    public void OnNextClicked()
    {
        if (manualNextSteps.Contains(currentStep))
            NextStep();
    }

    // ─────────────────────────── Event handlers ───────────────────────────

    private void OnGatePlaced(string gateName)
    {
        if (currentStep == 1 && gateName == "H" && !hPlaced)
        {
            hPlaced = true;
            NextStep(); // → step 2
        }
        else if (currentStep == 2 && gateName == "CNOT" && hPlaced && !cnotStepDone)
        {
            cnotStepDone = true;
            NextStep(); // → step 3
        }
    }

    private void OnMeasurePlaced()
    {
        int stepAtCall = currentStep;
        Debug.Log($"[Tutorial] OnMeasurePlaced fired — currentStep={stepAtCall}, modeExited={modeExitedSequence}, measureUsedOnce={measureUsedOnce}");

        if (stepAtCall == 8 && modeExitedSequence)
        {
            NextStep(); // → step 9
        }
        // step 9 → 10 ใช้ปุ่ม Next แทน (อยู่ใน manualNextSteps แล้ว)
    }

    private void OnModePressed()
    {
        Debug.Log($"[Tutorial] OnModePressed fired — currentStep={currentStep}, modeUsedOnce={modeUsedOnce}, modeExited={modeExitedSequence}, modeStepHandled={modeStepHandled}");

        if (currentStep == 3 && !modeUsedOnce)
        {
            modeUsedOnce = true;
            NextStep(); // → step 4
        }
        else if (currentStep == 8 && !modeExitedSequence)
        {
            modeExitedSequence = true; // รอ Measure ต่อ ยังอยู่ step 8
            Debug.Log("[Tutorial] modeExitedSequence = true, waiting for Measure...");
        }
        else if (currentStep == 10 && !modeStepHandled)
        {
            modeStepHandled = true; // ← กัน Coroutine fire ซ้ำ
            ResetFlags();
            NextStep(); // → step 11
        }
    }

    private void OnNextPressed()
    {
        if (currentStep == 4)
        {
            nextPressCount++;
            Debug.Log($"[Tutorial] OnNextPressed — nextPressCount={nextPressCount}");
            if (nextPressCount >= 2)
            {
                nextPressCount = 0;
                NextStep(); // → step 5
            }
        }
    }

    // ─────────────────────────── Core logic ───────────────────────────────

    public void NextStep()
    {
        // Guard: ป้องกัน double-fire ทำให้ step กระโดด
        if (isAdvancing)
        {
            Debug.LogWarning($"[Tutorial] NextStep blocked (isAdvancing=true) at step {currentStep}");
            return;
        }

        isAdvancing = true;

        currentStep++;
        Debug.Log($"[Tutorial] → currentStep={currentStep}, showNext={manualNextSteps.Contains(currentStep)}");

        if (currentStep < dialogueSprites.Length)
        {
            dialogueImage.sprite = dialogueSprites[currentStep];
            next.SetActive(manualNextSteps.Contains(currentStep));
            audioSource.PlayOneShot(dialogueSound);
        }
        else
        {
            Debug.Log("[Tutorial] All steps complete → deactivating");
            gameObject.SetActive(false);
        }

        isAdvancing = false;
    }

    // ─────────────────────────── Guide / Reset ────────────────────────────

    private void ResetFlags()
    {
        hPlaced            = false;
        cnotStepDone       = false;
        modeUsedOnce       = false;
        modeExitedSequence = false;
        measureUsedOnce    = false;
        modeStepHandled    = false; // ← reset ด้วย
        nextPressCount     = 0;
    }

    public void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }

    public void OnGuideClicked()
    {
        currentStep = 0;
        isAdvancing = false;
        ResetFlags();

        dialogueImage.sprite          = dialogueSprites[0];
        tutorialRect.localPosition    = startPos;
        tutorialRect.localEulerAngles = startRot;
        start.SetActive(true);
        next.SetActive(false);
        gameObject.SetActive(true);
    }
}