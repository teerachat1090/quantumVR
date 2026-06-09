using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class TutorialManager_Chap3 : MonoBehaviour
{
    [SerializeField] private Image dialogueImage;
    [SerializeField] private Sprite[] dialogueSprites;
    [SerializeField] private GameObject start;
    [SerializeField] private RectTransform tutorialRect;
    [SerializeField] private GameObject next;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogueSound;

    [Header("Entanglement Effect")]
    [SerializeField] private QuantumWaveConnection entanglementEffect;

    [Header("Circuit Reference")]
    [SerializeField] private SocketsManager socketsManager;

        /*
     * STEP MAP — sprites (Element 0–34)
     * ── SECTION 1 : Preparation ──
     *  0   Intro / welcome                              → Start button
     *  1   "Creating entangled qubit pair"              → manual Next
     *  2   "Gates needed: Hadamard(H) and CNOT"         → manual Next
     *  3   Checklist 0/2                                → OnGatePlaced("H")
     *  4   Checklist 1/2 (H ✓)                          → OnGatePlaced("CNOT")
     *  5   Checklist 2/2 formula shown                  → manual Next
     *  6   "One qubit stays with Alice. One goes to Bob" → manual Next
     *  7   "Entangled pair ready!"                      → manual Next
     *  8   "Entangled qubits are mysteriously linked"   → manual Next
     *  9   "Step 1 Complete"                            → manual Next
     *
     * ── SECTION 2 : Bell Measurement ──
     * 10   "Alice will do Bell measurement"             → manual Next
     * 11   "Gates needed: CNOT and H"                   → manual Next
     * 12   Checklist 0/3                                → OnGatePlaced("CNOT")
     * 13   Checklist 1/3 (CNOT ✓)                       → OnGatePlaced("H")
     * 14   "Now, measure both Qubit 0 and Qubit 1"      → OnMeasurePlaced
     * 15   "Set classical bit b0 / b1"                  → Check Classical bit
     * 16   "⚠️ This measurement collapses the qubit"    → manual Next 
     * 17   "These 2 bits tell Bob what to do next."     → manual Next  ← Next ธรรมดา
     * 18  "Original qubit no longer exists."           → manual Next
     * 19   "Step 2 Complete"                            → manual Next
     *
     * ── SECTION 3 : Classical Communication ──
     * 20   "Sending 2 Classical bits to Bob..."         → manual Next
     * 21   "Bob received 2 Classical bits"              → manual Next
     * 22   "Step 3 Complete"                            → manual Next
     *
     * ── SECTION 4 : Applying Corrections ──
     * 23   "Creating condition circuit"                 → manual Next
     * 24   Concept table                                → manual Next
     * 25   X-gate checklist 0/3                         → OnGatePlaced("X")  → step 28
     * 26   X-gate checklist 1/3 (X ✓)                  → OnGatePlaced("IF") → step 29
     * 27   X-gate checklist 2/3 (X✓ IF✓)               → manual Next (thumbstick b1)
     * 28   X-gate checklist 3/3 (all ✓)                → OnGatePlaced("Z")  → step 31
     * 29   Z-gate checklist 1/3 (Z ✓)                  → OnGatePlaced("IF") → step 32
     * 30   Z-gate checklist 2/3 (Z✓ IF✓)               → manual Next (thumbstick b0)
     * 31   Z-gate checklist 3/3 (all ✓)                → manual Next
     * 32   "Step 4 Complete – Gates applied"            → manual Next → END
     */

    // =========================
    // SECTION 1
    // =========================

    private const int H_QUBIT = 1;
    private const int H_COL = 0;

    private const int CNOT_QUBIT = 1;
    private const int CNOT_TGT_QUBIT = 2;
    private const int CNOT_COL = 1;

    // =========================
    // SECTION 2
    // =========================

    private const int S2_CNOT_QUBIT = 0;
    private const int S2_CNOT_TGT_QUBIT = 1;
    private const int S2_CNOT_COL = 2;

    private const int S2_H_QUBIT = 0;
    private const int S2_H_COL = 3;

    // =========================
    // Measurement
    // =========================

    private const int MEASURE_1_QUBIT = 0;
    private const int MEASURE_2_QUBIT = 1;
    private const int MEASURE_1_COL = 4;
    private const int MEASURE_2_COL = 5;

    // =========================
    // Condition
    // =========================

    private const int X_COND_QUBIT = 2;
    private const int X_COND_COL = 6;
    private const int Z_COND_QUBIT = 2;
    private const int Z_COND_COL = 7;

    private const int X_CBIT_VAL = 1;
    private const int Z_CBIT_VAL = 0;


    // =========================

    private int currentStep = 0;
    private bool isAdvancing = false;

    private bool s1_hPlaced = false;
    private bool s1_cnotPlaced = false;

    private bool s2_cnotPlaced = false;
    private bool s2_hPlaced = false;
    private bool s2_measured = false;

    private bool s4_xPlaced = false;
    private bool s4_xIfPlaced = false;

    private bool s4_zPlaced = false;
    private bool s4_zIfPlaced = false;

    private Vector3 startPos = new Vector3(3f, 1.8f, 0f);
    private Vector3 afterStartPos = new Vector3(3f, 0.855f, -3.2f);

    private Vector3 startRot = new Vector3(0f, 90f, 0f);
    private Vector3 afterStartRot = new Vector3(0f, 135f, 0f);


    
    private readonly System.Collections.Generic.HashSet<int> manualNextSteps
        = new System.Collections.Generic.HashSet<int>
    {
        // SECTION 1
        1, 2,
        5, 6, 7, 8, 9,

        // SECTION 2
        10, 11,
        16, 17, 18, 19,

        // SECTION 3
        20, 21, 22,

        // SECTION 4
        23, 24,
        27,   // thumbstick b1
        30,   // thumbstick b0
        31,
        32
    };

    void Start()
    {
        tutorialRect.localPosition = startPos;
        tutorialRect.localEulerAngles = startRot;

        dialogueImage.sprite = dialogueSprites[0];

        next.SetActive(false);

        if (entanglementEffect != null)
            entanglementEffect.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        GateSocket.OnAnyGatePlaced += OnGatePlaced;
        GateSocket.OnMeasurePlaced += OnMeasurePlaced;

        ClassicalBitManager.OnClassicalBitChanged += OnClassicalBitChanged;

        GateSocket.OnIfHappend += OnIfHappend;
    }

    void OnDisable()
    {
        GateSocket.OnAnyGatePlaced -= OnGatePlaced;
        GateSocket.OnMeasurePlaced -= OnMeasurePlaced;

        ClassicalBitManager.OnClassicalBitChanged -= OnClassicalBitChanged;

        GateSocket.OnIfHappend -= OnIfHappend;
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
        if (manualNextSteps.Contains(currentStep))
            NextStep();
    }

    private void OnGatePlaced(string gateName)
{
    Debug.Log($"[Tutorial] gate={gateName} step={currentStep}");

    // =========================
    // SECTION 1
    // =========================

    if (currentStep == 3 && gateName == "H" && !s1_hPlaced)
    {
        bool hPlaced =
            socketsManager.socketMap[H_QUBIT][H_COL].currentGate != null;

        if (hPlaced)
        {
            s1_hPlaced = true;
            NextStep(); // → step 4
        }
        return;
    }

    if (currentStep == 4 && gateName == "CNOT" && !s1_cnotPlaced)
    {
        Debug.LogWarning("checking CNOT gate");
        StartCoroutine(CheckSection1CNOT()); // → step 5
        return;
    }

    // =========================
    // SECTION 2
    // =========================

    if (currentStep == 12 && gateName == "CNOT" && !s2_cnotPlaced)
    {
        StartCoroutine(CheckSection2CNOT()); // → step 13
        return;
    }

    if (currentStep == 13 && gateName == "H" && !s2_hPlaced)
    {
        bool hPlaced =
            socketsManager.socketMap[S2_H_QUBIT][S2_H_COL].currentGate != null;

        if (hPlaced)
        {
            s2_hPlaced = true;
            NextStep(); // → step 14
        }
        return;
    }

    // =========================
    // SECTION 4
    // =========================

    // step 25 → OnGatePlaced("X") → step 26
    if (currentStep == 25 && gateName == "X" && !s4_xPlaced)
    {
        s4_xPlaced = true;
        NextStep(); // → step 26
        return;
    }

    // step 26 → OnGatePlaced("IF") → step 27
    if (currentStep == 26 && gateName == "IF" && !s4_xIfPlaced)
    {
        s4_xIfPlaced = true;
        NextStep(); // → step 27 (manual Next - thumbstick b1)
        return;
    }

    // step 28 → OnGatePlaced("Z") → step 29
    if (currentStep == 28 && gateName == "Z" && !s4_zPlaced)
    {
        s4_zPlaced = true;
        NextStep(); // → step 29
        return;
    }

    // step 29 → OnGatePlaced("IF") → step 30
    if (currentStep == 29 && gateName == "IF" && !s4_zIfPlaced)
    {
        s4_zIfPlaced = true;
        NextStep(); // → step 30 (manual Next - thumbstick b0)
        return;
    }
}
    // =========================
    // SECTION 1 CNOT CHECK
    // =========================

    private IEnumerator CheckSection1CNOT()
    {
        // รอ 1 frame ให้ socket update ก่อน
        yield return null;

        bool ctrlPlaced =
            socketsManager.socketMap[CNOT_QUBIT][CNOT_COL].currentGate != null;
        yield return DoDelay(0.1f);
        bool tgtPlaced =
            socketsManager.socketMap[CNOT_TGT_QUBIT][CNOT_COL].currentGate != null;

        Debug.Log($"[S1 CNOT] ctrl={ctrlPlaced} tgt={tgtPlaced}");

        // ต้องครบทั้ง control + target
        if (ctrlPlaced && tgtPlaced)
        {
            s1_cnotPlaced = true;

            // STEP 5
            NextStep();
        }
    }

    // =========================
    // SECTION 2 CNOT CHECK
    // =========================

    IEnumerator DoDelay(float second)
    {
        float now = Time.time;
        Debug.LogWarning($"IEnum - start delay: {now}");
        yield return new WaitForSeconds(second);
        Debug.LogWarning($"IEnum - end delay: {Time.time-now}: ({now})");
    }

    private IEnumerator CheckSection2CNOT()
    {
        // รอ 1 frame ให้ socket update ก่อน
        yield return null;

        bool ctrlPlaced =
            socketsManager.socketMap[S2_CNOT_QUBIT][S2_CNOT_COL].currentGate != null;
        yield return DoDelay(0.1f);
        bool tgtPlaced =
            socketsManager.socketMap[S2_CNOT_TGT_QUBIT][S2_CNOT_COL].currentGate != null;

        Debug.Log($"[S2 CNOT] ctrl={ctrlPlaced} tgt={tgtPlaced}");

        // ต้องครบทั้ง control + target
        if (ctrlPlaced && tgtPlaced)
        {
            s2_cnotPlaced = true;

            // STEP 13
            NextStep();
        }
    }

    // =========================
    // MEASUREMENT
    // =========================

    private void OnMeasurePlaced()
    {
        bool checkMeasure_1 = socketsManager.socketMap[MEASURE_1_QUBIT][MEASURE_1_COL].currentGate != null;
        bool checkMeasure_2 = socketsManager.socketMap[MEASURE_2_QUBIT][MEASURE_2_COL].currentGate != null;

        Debug.LogWarning($"check (1): {checkMeasure_1}\ncheck (2): {checkMeasure_2}");

        // step 14 → OnMeasurePlaced → step 15 (auto) → step 16 (manual Next)
        if (currentStep == 14 && !s2_measured && checkMeasure_1 && checkMeasure_2)
        {
            s2_measured = true;
            NextStep(); // → step 15 "Set classical bit b0/b1"
        }
    }

    // =========================
    // Classical bit
    // =========================
    private void OnClassicalBitChanged()
    {
        ClassicalBitManager cBMananger = socketsManager.CBManager;
        

        if(currentStep == 15)
        {
            int cBit0Target = cBMananger.GetTargetClassicalBit(4);
            int cBit1Target = cBMananger.GetTargetClassicalBit(5);
            if(cBit0Target == 0 && cBit1Target == 1) NextStep();
        }

        if(currentStep == 27)
        {
            int cBitXTarget = cBMananger.GetTargetClassicalBit(X_COND_COL);
            if(cBitXTarget == X_CBIT_VAL) NextStep();
        }
    }

    // =========================
    // condition
    // =========================
    private void OnIfHappend()
    {
        Debug.Log("Check condition placing");
        StartCoroutine(CheckIfCondition());
    }
    private IEnumerator CheckIfCondition()
    {
        yield return null;

        if(currentStep == 26)
        {
            bool XPlaced =
            socketsManager.socketMap[X_COND_QUBIT][X_COND_COL].currentGate != null;
            if(!XPlaced) yield return null;

            QuantumGate gate = socketsManager.socketMap[X_COND_QUBIT][X_COND_COL].currentGate;

            bool XCond = gate.getGateType() == QuantumGate.inputType.condition;
            if (XCond)
            {
                NextStep();
            }
        }

        if(currentStep == 29)
        {
            bool ZPlaced =
            socketsManager.socketMap[Z_COND_QUBIT][Z_COND_COL].currentGate != null;
            if(!ZPlaced) yield return null;

            QuantumGate gate = socketsManager.socketMap[Z_COND_QUBIT][Z_COND_COL].currentGate;

            bool ZCond = gate.getGateType() == QuantumGate.inputType.condition;
            if (ZCond)
            {
                NextStep();
                yield return StartCoroutine(DoDelay(1f));
                NextStep();
                
            }
        }
    }

    // =========================
    // NEXT STEP
    // =========================

    public void NextStep()
    {
        if (isAdvancing)
            return;

        if (currentStep + 1 >= dialogueSprites.Length)
        {
            gameObject.SetActive(false);
            return;
        }

        isAdvancing = true;

        next.SetActive(false);

        currentStep++;

        Debug.Log($"[Tutorial] STEP → {currentStep}");

        // Entanglement ON
        if (currentStep == 7 && entanglementEffect != null)
        {
            entanglementEffect.gameObject.SetActive(true);
        }

        // Entanglement OFF
        if (currentStep == 15 && entanglementEffect != null)
        {
            entanglementEffect.gameObject.SetActive(false);
        }
        if (currentStep == 24 && entanglementEffect != null)
        {
            entanglementEffect.gameObject.SetActive(true);
        }
        dialogueImage.sprite = dialogueSprites[currentStep];

        next.SetActive(manualNextSteps.Contains(currentStep));

        if (audioSource != null && dialogueSound != null)
        {
            audioSource.PlayOneShot(dialogueSound);
        }

        isAdvancing = false;
    }

    // =========================
    // RESET
    // =========================

    private void ResetFlags()
    {
        s1_hPlaced = false;
        s1_cnotPlaced = false;

        s2_cnotPlaced = false;
        s2_hPlaced = false;
        s2_measured = false;

        s4_xPlaced = false;
        s4_xIfPlaced = false;

        s4_zPlaced = false;
        s4_zIfPlaced = false;
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

        dialogueImage.sprite = dialogueSprites[0];

        tutorialRect.localPosition = startPos;
        tutorialRect.localEulerAngles = startRot;

        start.SetActive(true);

        next.SetActive(false);

        if (entanglementEffect != null)
        {
            entanglementEffect.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }
}