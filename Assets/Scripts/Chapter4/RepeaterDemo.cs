using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ════════════════════════════════════════════════════════════════
//  RepeaterDemo.cs  (v6 — Enter Next Scene after both modes seen)
//
//  Flow:
//  1. กดเลือก "No Repeater" หรือ "With Repeater"
//  2. กด Next ทีละขั้น — แต่ละขั้นมี Particle + คำอธิบาย
//  3. ดูครบทั้งสอง mode → ปุ่ม "Enter Next Scene" โชว์ที่ Select Screen
//
//  No Repeater (3 ขั้น):
//    1: Alice ส่งสัญญาณ — Particle วิ่งแล้วจางหาย
//    2: สัญญาณหาย — คำอธิบาย Fidelity
//    3: สรุป — ล้มเหลว
//
//  With Repeater (n+2 ขั้น):
//    1: Alice → R1 — Particle วิ่ง
//    2: R1 → R2 — Particle วิ่ง (ถ้ามี R2)
//    n: Rn → Bob — Particle วิ่งถึง Bob
//    n+1: สรุป — สำเร็จ
// ════════════════════════════════════════════════════════════════

public class RepeaterDemo : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector — UI
    // ─────────────────────────────────────────
    [Header("Step UI")]
    public TextMeshProUGUI txt_Title;
    public TextMeshProUGUI txt_Description;
    public TextMeshProUGUI txt_Step;
    public Button          btn_Next;
    public TextMeshProUGUI btn_Next_Label;

    [Header("Mode Buttons")]
    public Button          btn_NoRepeater;
    public Button          btn_HasRepeater;

    [Header("Next Scene")]
    public Button          btn_EnterNextScene;   // ← ลาก assign ใน Inspector

    [Header("Status Text")]
    public TextMeshProUGUI txt_Status;

    // ─────────────────────────────────────────
    //  Inspector — Scene
    // ─────────────────────────────────────────
    [Header("Scene Nodes")]
    public Transform   aliceTransform;
    public Transform   bobTransform;
    public Transform[] repeaterTransforms;

    [Header("Line Renderer")]
    public LineRenderer signalLine;

    // ─────────────────────────────────────────
    //  Inspector — Particle
    // ─────────────────────────────────────────
    [Header("Particle Settings")]
    [Range(1, 6)]         public int   particleCount = 3;
    [Range(0.3f, 4f)]     public float particleSpeed = 1.0f;
    [Range(0.02f, 0.15f)] public float particleSize  = 0.05f;
    public Color colorFail    = new Color(1f, 0.2f, 0.2f);
    public Color colorSuccess = new Color(0f, 1f, 0.3f);

    // ─────────────────────────────────────────
    //  Runtime
    // ─────────────────────────────────────────
    private enum DemoMode { None, NoRepeater, WithRepeater }
    private DemoMode mode = DemoMode.None;

    // ── Progress tracking (ดูครบแล้วหรือยัง) ──
    private bool seenNoRepeater  = false;
    private bool seenWithRepeater = false;

    // Steps สำหรับแต่ละ mode
    private List<StepData> steps     = new List<StepData>();
    private int            stepIndex = 0;

    // Particle
    private List<GameObject> particles = new List<GameObject>();
    private float[]          particleT;
    private bool             isRunning = false;
    private bool             failMode  = false;

    // path สำหรับ With Repeater
    private List<Transform> path           = new List<Transform>();
    private int             currentSegment = 0;

    // ─────────────────────────────────────────
    //  Step Data
    // ─────────────────────────────────────────
    private class StepData
    {
        public string title;
        public string description;
        public bool   runAnimation;   // true = เริ่ม particle ทันที
        public int    segment;        // segment ที่วิ่ง (-1 = fail mode)
    }

    // ─────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────
    void Start()
    {
        if (signalLine == null)
        {
            var wave = FindFirstObjectByType<QuantumWaveConnection>();
            if (wave != null) signalLine = wave.GetLineRenderer();
        }

        foreach (var r in repeaterTransforms)
            if (r != null) r.gameObject.SetActive(false);

        btn_Next.onClick.AddListener(OnNextPressed);
        btn_NoRepeater.onClick.AddListener(OnNoRepeaterPressed);
        btn_HasRepeater.onClick.AddListener(OnHasRepeaterPressed);

        if (btn_EnterNextScene != null)
            btn_EnterNextScene.onClick.AddListener(OnEnterNextScenePressed);

        SpawnParticles();
        ShowSelectScreen();
    }

    void Update()
    {
        if (!isRunning) return;
        if (failMode) UpdateFail();
        else          UpdateSegment();
    }

    // ─────────────────────────────────────────
    //  Select Screen — เริ่มต้น
    // ─────────────────────────────────────────
    void ShowSelectScreen()
    {
        mode = DemoMode.None;
        SetTitle("Quantum Repeater Demo");
        SetDesc("Select a mode to begin:\n\n" +
                "• No Repeater — see what happens without\n" +
                "• With Repeater — see how Repeaters help");
        SetStep("");
        btn_Next.gameObject.SetActive(false);
        btn_NoRepeater.gameObject.SetActive(true);
        btn_HasRepeater.gameObject.SetActive(true);
        SetStatus("");
        SetParticlesVisible(false);

        // ── แสดงปุ่ม Enter Next Scene เมื่อดูครบทั้งสอง mode ──
        if (btn_EnterNextScene != null)
            btn_EnterNextScene.gameObject.SetActive(seenNoRepeater && seenWithRepeater);
    }

    // ─────────────────────────────────────────
    //  Mode Selection
    // ─────────────────────────────────────────
    void OnNoRepeaterPressed()
    {
        mode = DemoMode.NoRepeater;

        foreach (var r in repeaterTransforms)
            if (r != null) r.gameObject.SetActive(false);

        // สร้าง steps
        steps.Clear();
        steps.Add(new StepData {
            title        = "Alice Sends a Signal",
            description  = "Alice attempts to send a Qubit directly to Bob\n" +
                           "over a long distance fiber optic cable.\n\n" +
                           "Watch what happens to the signal...",
            runAnimation = true,
            segment      = -1  // fail mode
        });
        steps.Add(new StepData {
            title       = "Signal Lost",
            description = "The quantum signal fades and disappears\n" +
                          "before reaching Bob.\n\n" +
                          "Fidelity drops from 100% to nearly 0%\n" +
                          "due to photon loss in the fiber.",
            runAnimation = false,
            segment      = -1
        });
        steps.Add(new StepData {
            title       = "Result: Failed",
            description = "Without a Quantum Repeater:\n\n" +
                          "• Signal cannot reach Bob\n" +
                          "• Communication failed\n\n" +
                          "A solution is needed for long-distance\n" +
                          "quantum communication.",
            runAnimation = false,
            segment      = -1
        });

        DrawLine(false);
        SetParticleColor(colorFail);
        btn_NoRepeater.gameObject.SetActive(false);
        btn_HasRepeater.gameObject.SetActive(false);
        btn_Next.gameObject.SetActive(true);

        // ซ่อน Enter Next Scene ขณะกำลังดู demo
        if (btn_EnterNextScene != null)
            btn_EnterNextScene.gameObject.SetActive(false);

        stepIndex = 0;
        ShowCurrentStep();
    }

    void OnHasRepeaterPressed()
    {
        mode = DemoMode.WithRepeater;

        foreach (var r in repeaterTransforms)
            if (r != null) r.gameObject.SetActive(true);

        // สร้าง path
        path.Clear();
        path.Add(aliceTransform);
        foreach (var r in repeaterTransforms)
            if (r != null) path.Add(r);
        path.Add(bobTransform);

        // สร้าง steps ตาม path
        steps.Clear();
        for (int i = 0; i < path.Count - 1; i++)
        {
            string from = GetNodeName(path[i]);
            string to   = GetNodeName(path[i + 1]);
            bool   isLast = (i == path.Count - 2);

            string desc;
            if (i == 0)
                desc = $"Alice sends a Qubit toward {to}.\n\n" +
                       $"The signal travels along the\n" +
                       $"quantum channel to the first Repeater.";
            else if (isLast)
                desc = $"The final signal travels from {from}\n" +
                       $"to Bob.\n\n" +
                       $"The Qubit arrives with high fidelity!";
            else
                desc = $"Repeater {from} performs\n" +
                       $"Entanglement Swapping.\n\n" +
                       $"The quantum state is restored\n" +
                       $"and forwarded to {to}.";

            steps.Add(new StepData {
                title        = $"{from} → {to}",
                description  = desc,
                runAnimation = true,
                segment      = i
            });
        }

        // ขั้นสุดท้าย — สรุป
        steps.Add(new StepData {
            title       = "Result: Success!",
            description = "With Quantum Repeaters:\n\n" +
                          "• Signal reached Bob\n" +
                          "• Entanglement Swapping worked\n\n" +
                          "Quantum Repeaters make long-distance\n" +
                          "quantum communication possible!",
            runAnimation = false,
            segment      = -1
        });

        DrawLine(true);
        SetParticleColor(colorSuccess);
        btn_NoRepeater.gameObject.SetActive(false);
        btn_HasRepeater.gameObject.SetActive(false);
        btn_Next.gameObject.SetActive(true);

        // ซ่อน Enter Next Scene ขณะกำลังดู demo
        if (btn_EnterNextScene != null)
            btn_EnterNextScene.gameObject.SetActive(false);

        stepIndex = 0;
        ShowCurrentStep();
    }

    // ─────────────────────────────────────────
    //  Step Navigation
    // ─────────────────────────────────────────
    void OnNextPressed()
    {
        // รอเฉพาะ With Repeater เท่านั้น
        if (isRunning && mode == DemoMode.WithRepeater)
        {
            SetStatus("Please wait for the signal to finish...");
            return;
        }

        stepIndex++;
        if (stepIndex >= steps.Count)
        {
            // ── mark ว่าดู mode นี้ครบแล้ว ──
            if (mode == DemoMode.NoRepeater)  seenNoRepeater   = true;
            if (mode == DemoMode.WithRepeater) seenWithRepeater = true;

            ShowSelectScreen();
            return;
        }
        ShowCurrentStep();
    }

    void ShowCurrentStep()
    {
        StopAllCoroutines();
        isRunning = false;
        SetParticlesVisible(false);

        StepData s = steps[stepIndex];
        SetTitle(s.title);
        SetDesc(s.description);
        SetStep($"{stepIndex + 1} / {steps.Count}");
        SetStatus("");

        bool isLast = (stepIndex == steps.Count - 1);
        if (btn_Next_Label != null)
            btn_Next_Label.text = isLast ? "Finish" : "Next";

        if (s.runAnimation)
        {
            if (s.segment == -1)
            {
                // No Repeater animation
                failMode = true;
                ResetParticles();
                isRunning = true;
                SetStatus("Signal fading...");
            }
            else
            {
                // With Repeater — วิ่ง segment นั้น
                failMode       = false;
                currentSegment = s.segment;
                ResetParticles();
                isRunning = true;
                SetStatus($"Sending signal: {GetNodeName(path[s.segment])} → {GetNodeName(path[s.segment + 1])}");
            }
        }
    }

    // ─────────────────────────────────────────
    //  Enter Next Scene
    // ─────────────────────────────────────────
    [Header("Next Scene Settings")]
    public string nextSceneName = "";   // ← ใส่ชื่อ scene ใน Inspector ได้เลย

    void OnEnterNextScenePressed()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // ─────────────────────────────────────────
    //  Update: Fail (No Repeater)
    // ─────────────────────────────────────────
    void UpdateFail()
    {
        if (aliceTransform == null || bobTransform == null) return;
        float step = particleSpeed * Time.deltaTime;

        for (int i = 0; i < particleCount; i++)
        {
            particleT[i] = Mathf.Repeat(particleT[i] + step, 1f);
            float   t   = particleT[i];
            Vector3 pos = Vector3.Lerp(aliceTransform.position, bobTransform.position, t);
            particles[i].transform.position = pos;

            float alpha = t < 0.4f  ? 1f
                        : t < 0.75f ? Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.35f)
                        : 0f;
            SetParticleAlpha(i, alpha);
        }
    }

    // ─────────────────────────────────────────
    //  Update: Segment (With Repeater)
    // ─────────────────────────────────────────
    void UpdateSegment()
    {
        if (path.Count < 2 || currentSegment >= path.Count - 1) return;

        Transform from = path[currentSegment];
        Transform to   = path[currentSegment + 1];
        if (from == null || to == null) return;

        float step       = particleSpeed * Time.deltaTime;
        bool  allArrived = true;

        for (int i = 0; i < particleCount; i++)
        {
            particleT[i] = Mathf.MoveTowards(particleT[i], 1f, step);
            particles[i].transform.position = Vector3.Lerp(from.position, to.position, particleT[i]);
            SetParticleAlpha(i, 1f);
            if (particleT[i] < 1f) allArrived = false;
        }

        if (allArrived)
        {
            isRunning = false;
            bool isLastSeg = (currentSegment >= path.Count - 2);
            SetStatus(isLastSeg ? "Signal arrived at Bob!" : "Arrived — press Next to continue");

            // Flash node ที่ถึง
            StartCoroutine(FlashNode(to, 0.5f));
        }
    }

    // ─────────────────────────────────────────
    //  Coroutine
    // ─────────────────────────────────────────
    IEnumerator FlashNode(Transform node, float duration)
    {
        if (node == null) yield break;
        var rend = node.GetComponent<Renderer>();
        if (rend == null) yield break;

        Color original = rend.material.color;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            float pulse = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;
            rend.material.color = Color.Lerp(original, Color.white, pulse * 0.6f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rend.material.color = original;
    }

    // ─────────────────────────────────────────
    //  Line Renderer
    // ─────────────────────────────────────────
    void DrawLine(bool withRepeaters)
    {
        if (signalLine == null || aliceTransform == null || bobTransform == null) return;

        if (withRepeaters && repeaterTransforms.Length > 0)
        {
            int count = 2 + repeaterTransforms.Length;
            signalLine.positionCount = count;
            signalLine.SetPosition(0, aliceTransform.position);
            for (int i = 0; i < repeaterTransforms.Length; i++)
                if (repeaterTransforms[i] != null)
                    signalLine.SetPosition(i + 1, repeaterTransforms[i].position);
            signalLine.SetPosition(count - 1, bobTransform.position);
        }
        else
        {
            signalLine.positionCount = 2;
            signalLine.SetPosition(0, aliceTransform.position);
            signalLine.SetPosition(1, bobTransform.position);
        }
    }

    // ─────────────────────────────────────────
    //  Particle Helpers
    // ─────────────────────────────────────────
    void SpawnParticles()
    {
        foreach (var p in particles) if (p) Destroy(p);
        particles.Clear();
        particleT = new float[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            particleT[i] = (float)i / particleCount * 0.3f;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<SphereCollider>());
            go.transform.localScale = Vector3.one * particleSize;
            go.name = $"SignalParticle_{i}";
            go.SetActive(false);

            var mr     = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat    = new Material(shader);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            mat.color       = colorFail;

            mr.material          = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
            particles.Add(go);
        }
    }

    void ResetParticles()
    {
        for (int i = 0; i < particleCount; i++)
            particleT[i] = (float)i / particleCount * 0.2f;
        SetParticlesVisible(true);
    }

    void SetParticlesVisible(bool v) { foreach (var p in particles) if (p) p.SetActive(v); }

    void SetParticleColor(Color c)
    {
        foreach (var p in particles)
        {
            if (p == null) continue;
            var mat = p.GetComponent<MeshRenderer>().material;
            c.a = 1f; mat.color = c;
        }
    }

    void SetParticleAlpha(int index, float alpha)
    {
        if (index >= particles.Count || particles[index] == null) return;
        var mat = particles[index].GetComponent<MeshRenderer>().material;
        Color col = mat.color; col.a = alpha; mat.color = col;
    }

    // ─────────────────────────────────────────
    //  UI Helpers
    // ─────────────────────────────────────────
    void SetTitle(string s)  { if (txt_Title != null)       txt_Title.text = s; }
    void SetDesc(string s)   { if (txt_Description != null) txt_Description.text = s; }
    void SetStep(string s)   { if (txt_Step != null)        txt_Step.text = s; }
    void SetStatus(string s) { if (txt_Status != null)      txt_Status.text = s; }
    string GetNodeName(Transform t) => t != null ? t.name : "?";

    void OnDestroy() { foreach (var p in particles) if (p) Destroy(p); }
}