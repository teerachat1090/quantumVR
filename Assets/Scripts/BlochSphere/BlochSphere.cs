using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;


public class BlochSphere : MonoBehaviour
{
    [Header("Sphere Settings")]
    [SerializeField] private GameObject sphereMesh;
    [SerializeField] private float sphereRadius = 2f;
    [SerializeField][Range(0f, 1f)] private float sphereAlpha = 0.3f;
    [SerializeField] private Color sphereBaseColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private bool autoPosition = true;
    [SerializeField] private Vector3 targetPosition = new Vector3(2f, 1.5f, 0f);

    [Header("State Vector")]
    [SerializeField] private GameObject stateVector;
    [SerializeField] private GameObject stateVectorLine;
    [SerializeField] private Color stateColor = new Color(0.2f, 0.3f, 0.8f);
    [SerializeField] private float stateSphereSize = 0.15f;
    [SerializeField] private float stateVectorWidth = 0.06f;

    [Header("Axes Settings")]
    [SerializeField] private GameObject xAxisObj;
    [SerializeField] private GameObject yAxisObj;
    [SerializeField] private GameObject zAxisObj;
    [SerializeField] private float axisLength = 2.4f;
    [SerializeField] private float axisRadius = 0.03f;
    [SerializeField] private Color xAxisColor = Color.red;
    [SerializeField] private Color yAxisColor = new Color(0.2f, 0.3f, 0.8f);
    [SerializeField] private Color zAxisColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Labels (Optional)")]
    [SerializeField] private bool createLabels = true;
    [SerializeField] private float axisLabelSize = 1.2f;
    [SerializeField] private Color axisLabelColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private float stateLabelSize = 1.2f;
    [SerializeField] private Color stateLabelColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Animation (Blochy Smooth)")]
    [SerializeField] private float animationDuration = 1.0f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("ถ้าเปิด: หมุนด้วยความเร็วเชิงมุมคงที่ (เหมือน Blochy มากขึ้น)")]
    [SerializeField] private bool constantAngularSpeed = true;
    [SerializeField] private float degreesPerSecond = 180f;

    [Header("Arc Renderer")]
    [SerializeField] private BlochRotationArcRenderer arcRenderer;
    [SerializeField] private bool showRotationArc = true;
    [SerializeField] private float arcFadeDelay = 0.5f;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Editor/Runtime Build")]
    [SerializeField] private bool rebuildOnPlay = true;

    private Vector3 currentStatePosition = Vector3.up; // |0> = +Y
    private Coroutine currentAnimation;
    private Coroutine arcFadeCoroutine;

    // ✨ เก็บข้อมูลการหมุนสำหรับ animation
    private Vector3 lastRotationAxis = Vector3.up;
    private float lastRotationAngle = 0f;
    private bool useAxisRotation = false;


    void Start()
    {
        if (autoPosition)
        {
            transform.position = targetPosition;
            Debug.Log($"📍 Bloch Sphere positioned at {targetPosition}");
        }

        if (rebuildOnPlay)
            InitializeBlochSphere();
        else
            ApplyVisualsOnly();

        SetupArcRenderer();
    }

    void OnValidate()
    {
        if (!Application.isPlaying && sphereMesh != null)
        {
            ApplySphereMaterial();
            if (xAxisObj != null) UpdateAxisVisual(xAxisObj, xAxisColor);
            if (yAxisObj != null) UpdateAxisVisual(yAxisObj, yAxisColor);
            if (zAxisObj != null) UpdateAxisVisual(zAxisObj, zAxisColor);
        }
    }

    void SetupArcRenderer()
    {
        if (arcRenderer == null)
        {
            arcRenderer = GetComponentInChildren<BlochRotationArcRenderer>();

            if (arcRenderer == null)
            {
                GameObject arcObj = new GameObject("ArcRenderer");
                arcObj.transform.SetParent(transform);
                arcObj.transform.localPosition = Vector3.zero;
                arcObj.transform.localRotation = Quaternion.identity;
                arcObj.transform.localScale = Vector3.one;

                arcRenderer = arcObj.AddComponent<BlochRotationArcRenderer>();
                Debug.Log("✅ Created BlochRotationArcRenderer automatically");
            }
        }

        if (arcRenderer != null)
            arcRenderer.SetSphereRadius(sphereRadius);
    }

    // =========================
    // Public API
        // =========================
    public void AnimateMeasurementCollapseFromCounts(Dictionary<string,int> counts)
{
    if (counts == null || counts.Count == 0) return;

    int total = 0;
    foreach (var kv in counts) total += kv.Value;

    int r = UnityEngine.Random.Range(1, total + 1);
    string picked = "0";
    int acc = 0;
    foreach (var kv in counts)
    {
        acc += kv.Value;
        if (r <= acc) { picked = kv.Key; break; }
    }

    // รองรับ key แบบ "0", "1", "0 0"
    picked = picked.Replace(" ", "").Trim();

    Vector3 targetUnit = (picked == "0")
        ? Vector3.up       // |0⟩
        : -Vector3.up;     // |1⟩

    StartCoroutine(CoMeasurementCollapse(targetUnit));

    Debug.Log($"🎯 Measurement collapsed to |{picked}⟩");
}


    private IEnumerator CoMeasurementCollapse(Vector3 targetUnit)
    {
        // ถ้ามี coroutine อื่นอยู่ ก็หยุดก่อน
        StopMotionOnly();

        // (1) หดเล็กลงนิดหนึ่ง (เหมือนกำลังวัด)
        float t = 0f;
        float pre = 0.12f;
        Vector3 startScale = stateVector.transform.localScale;
        Vector3 shrinkScale = startScale * 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / pre;
            stateVector.transform.localScale = Vector3.Lerp(startScale, shrinkScale, t);
            yield return null;
        }

        // (2) ค่อย ๆ ย้ายปลายเวกเตอร์ไปที่ pole (collapse)
        float dur = 0.35f;
        t = 0f;

        Vector3 startUnit = currentStatePosition.normalized;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            // ใช้ Slerp จะดูเป็นโค้งบนผิว sphere (สวยกว่า Lerp)
            Vector3 unit = Vector3.Slerp(startUnit, targetUnit, t).normalized;

            // อัปเดต state จริง ๆ
            SetStateDirectly(unit * sphereRadius);

            yield return null;
        }

        // (3) ขยายกลับ
        t = 0f;
        float post = 0.12f;
        while (t < 1f)
        {
            t += Time.deltaTime / post;
            stateVector.transform.localScale = Vector3.Lerp(shrinkScale, startScale, t);
            yield return null;
        }
    }

        public void AnimateToStateWithAxis(Vector3 targetUnitState, Vector3 axis, float usedAngleDeg)
    {
        lastRotationAxis = axis;
        lastRotationAngle = usedAngleDeg;
        useAxisRotation = true;

        StopMotionOnly();
        currentAnimation = StartCoroutine(AnimateToState(targetUnitState * sphereRadius, -1f, -1f));
    }


    public void SetStateDirectly(Vector3 stateUnit)
    {
        StopAllMotion();
        if (arcRenderer != null) arcRenderer.Clear();
        
        Vector3 targetPosition = stateUnit.normalized * sphereRadius;
        UpdateStateVector(targetPosition);
        
        Debug.Log($"📍 State set directly to: {stateUnit}");
    }
    
    public void AnimateToStateDirectly(Vector3 targetStateUnit)
    {
        Vector3 startUnit = currentStatePosition.normalized;
        Vector3 endUnit = targetStateUnit.normalized;
        
        // วาด arc แบบ Slerp (ไม่มีแกนหมุนที่แน่นอน)
        if (showRotationArc && arcRenderer != null)
        {
            arcRenderer.DrawGateArc(startUnit, endUnit);
        }
        
        if (infoText != null)
        {
            infoText.text = "State transition";
        }
        
        StopMotionOnly();
        useAxisRotation = false; // ใช้ Slerp ธรรมดา
        currentAnimation = StartCoroutine(AnimateToState(endUnit * sphereRadius, -1f, -1f));
        
        Debug.Log($"🎬 Animating to state: {targetStateUnit}");
    }
    
    public Vector3 GetCurrentStateUnit()
    {
        return currentStatePosition.normalized;
    }

    public void ResetToZero()
    {
        StopAllMotion();
        if (arcRenderer != null) arcRenderer.Clear();
        useAxisRotation = false;
        currentAnimation = StartCoroutine(AnimateToState(Vector3.up * sphereRadius, 1f, 0f));
    }

    /// <summary>
    /// ✨ Apply gate with axis rotation - state vector จะไปตาม arc!
    /// </summary>
    public void ApplyGate(string gateType)
{
    string raw = (gateType ?? "").Trim();

    if (!GateRotationLibrary.TryGetRotation(raw, out Vector3 axis, out float angleDeg, out string label))
    {
        Debug.LogWarning($"⚠️ Unknown gate '{raw}'");
        return;
    }

    Vector3 startUnit = currentStatePosition.normalized;
    Vector3 localAxis = axis.normalized;

    // ✅ สำคัญ: กลับทิศหมุนให้ตรง Bloch convention
    float usedAngleDeg = -angleDeg;

    Quaternion q = Quaternion.AngleAxis(usedAngleDeg, localAxis);
    Vector3 endUnit = (q * startUnit).normalized;

    // ✨ วาด arc ตามแกนหมุนจริง (ใช้มุมเดียวกัน)
    if (showRotationArc && arcRenderer != null)
    {
        arcRenderer.DrawGateArcWithAxis(startUnit, endUnit, axis, usedAngleDeg);
    }

    if (infoText != null)
        infoText.text = $"{label}\nAxis: ({axis.x:F2},{axis.y:F2},{axis.z:F2})\nAngle: {usedAngleDeg:F0}°";

    // ✨ เก็บข้อมูลการหมุนสำหรับ animation (ใช้มุมเดียวกัน)
    lastRotationAxis = axis;
    lastRotationAngle = usedAngleDeg;
    useAxisRotation = true;

    StopMotionOnly();
    currentAnimation = StartCoroutine(AnimateToState(endUnit * sphereRadius, -1f, -1f));
}


    public void UpdateFromQiskitResult(string jsonResult)
    {
        try
        {
            int count0 = ExtractCount(jsonResult, "0");
            int count1 = ExtractCount(jsonResult, "1");
            int totalShots = ExtractInt(jsonResult, "total_shots");

            if (totalShots > 0)
                VisualizeFromMeasurement(count0, count1, totalShots);
            else
                Debug.LogWarning("⚠️ Total shots is 0, cannot visualize");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to parse Qiskit result: {e.Message}");
        }
    }

    public void VisualizeFromMeasurement(int count0, int count1, int totalShots)
    {
        float prob0 = (float)count0 / totalShots;
        float prob1 = (float)count1 / totalShots;

        float y = Mathf.Clamp(prob0 - prob1, -1f, 1f);
        float x = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        Vector3 targetUnit = new Vector3(x, y, 0f).normalized;

        if (showRotationArc && arcRenderer != null)
            arcRenderer.DrawMeasurementArc(currentStatePosition.normalized, targetUnit);

        StopMotionOnly();
        useAxisRotation = false; // measurement ใช้ Slerp
        currentAnimation = StartCoroutine(AnimateToState(targetUnit * sphereRadius, prob0, prob1));
    }

    // =========================
    // ✨ Animation ไปตาม Arc
    // =========================
    IEnumerator AnimateToState(Vector3 targetPosition, float prob0, float prob1)
    {
        Vector3 startUnit = currentStatePosition.normalized;
        Vector3 endUnit = targetPosition.normalized;

        float angle = Vector3.Angle(startUnit, endUnit);
        if (angle < 0.05f)
        {
            UpdateStateVector(endUnit * sphereRadius);
            currentAnimation = null;
            yield break;
        }

        float duration = animationDuration;

        if (constantAngularSpeed && degreesPerSecond > 1f)
        {
            duration = Mathf.Max(0.05f, angle / degreesPerSecond);
        }

        float elapsed = 0f;

        // ✨ เลือกวิธี animate ตามประเภทการหมุน
        if (useAxisRotation)
        {
            // 🎯 หมุนรอบแกนจริง - ตาม arc ที่วาด!
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float t = animationCurve.Evaluate(u);

                // หมุนรอบแกนตามมุมที่กำหนด
                float currentAngle = lastRotationAngle * t;
                Quaternion rotation = Quaternion.AngleAxis(currentAngle, lastRotationAxis);
                Vector3 newUnit = rotation * startUnit;

                UpdateStateVector(newUnit.normalized * sphereRadius);
                yield return null;
            }
        }
        else
        {
            // 🔄 Slerp ธรรมดา (สำหรับ measurement หรือ direct animation)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float t = animationCurve.Evaluate(u);

                Vector3 newUnit = Vector3.Slerp(startUnit, endUnit, t);
                UpdateStateVector(newUnit * sphereRadius);
                yield return null;
            }
        }

        UpdateStateVector(endUnit * sphereRadius);
        currentAnimation = null;

        if (prob0 >= 0 && prob1 >= 0 && infoText != null)
        {
            infoText.text = $"Measurement\n|0⟩: {prob0:P1}\n|1⟩: {prob1:P1}";
        }

        if (showRotationArc && arcRenderer != null)
        {
            if (arcFadeCoroutine != null) StopCoroutine(arcFadeCoroutine);
            arcFadeCoroutine = StartCoroutine(FadeOutArc());
        }
    }

    IEnumerator FadeOutArc()
    {
        yield return new WaitForSeconds(arcFadeDelay);
        if (arcRenderer != null) arcRenderer.Clear();
        arcFadeCoroutine = null;
    }

    void StopMotionOnly()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        if (arcFadeCoroutine != null)
        {
            StopCoroutine(arcFadeCoroutine);
            arcFadeCoroutine = null;
        }
    }

    void StopAllMotion()
    {
        StopMotionOnly();
        if (arcRenderer != null) arcRenderer.Clear();
    }

    // =========================
    // Build visuals (same as your style)
    // =========================
    void InitializeBlochSphere()
    {
        if (sphereMesh == null)
        {
            sphereMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh.name = "SphereMesh";
            sphereMesh.transform.SetParent(transform);
            var col = sphereMesh.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);
        }

        sphereMesh.transform.localPosition = Vector3.zero;
        sphereMesh.transform.localRotation = Quaternion.identity;
        sphereMesh.transform.localScale = Vector3.one * sphereRadius * 2f;

        ApplySphereMaterial();

        if (stateVector == null)
        {
            stateVector = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stateVector.name = "StateVector";
            stateVector.transform.SetParent(transform);
            var col = stateVector.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);
        }

        stateVector.transform.localRotation = Quaternion.identity;
        stateVector.transform.localScale = Vector3.one * stateSphereSize;

        ApplyStateVectorMaterial();

        if (stateVectorLine == null)
        {
            stateVectorLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stateVectorLine.name = "StateVectorLine";
            stateVectorLine.transform.SetParent(transform);
            var col = stateVectorLine.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);
        }

        CreateAxes();
        if (createLabels) CreateLabels();

        UpdateStateVector(Vector3.up * sphereRadius);
        Debug.Log("✅ Bloch Sphere initialized!");
    }

    void ApplyVisualsOnly()
    {
        ApplySphereMaterial();
        ApplyStateVectorMaterial();
        UpdateStateVector(Vector3.up * sphereRadius);
    }

    void ApplySphereMaterial()
    {
        if (sphereMesh == null) return;

        var renderer = sphereMesh.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Material mat = renderer.sharedMaterial;
        if (mat == null || mat.shader != shader)
            mat = new Material(shader);

        Color finalColor = new Color(sphereBaseColor.r, sphereBaseColor.g, sphereBaseColor.b, sphereAlpha);

        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetColor("_BaseColor", finalColor);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_ZWrite", 0);
            mat.SetFloat("_Cull", 2);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetColor("_Color", finalColor);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void ApplyStateVectorMaterial()
    {
        if (stateVector == null) return;

        var renderer = stateVector.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Material mat = renderer.sharedMaterial;
        if (mat == null || mat.shader != shader)
            mat = new Material(shader);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", stateColor);
        else mat.SetColor("_Color", stateColor);

        renderer.sharedMaterial = mat;
    }

    void CreateAxes()
    {
        xAxisObj = CreateCylinderAxis("XAxis", -Vector3.right, axisLength, xAxisColor);
        yAxisObj = CreateCylinderAxis("YAxis", Vector3.forward, axisLength, yAxisColor);
        zAxisObj = CreateCylinderAxis("ZAxis", Vector3.up, axisLength, zAxisColor);
    }

    GameObject CreateCylinderAxis(string name, Vector3 direction, float length, Color color)
    {
        Transform existing = transform.Find(name);
        GameObject axisObj = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        axisObj.name = name;
        axisObj.transform.SetParent(transform);
        axisObj.transform.localPosition = Vector3.zero;

        var col = axisObj.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);

        if (direction == Vector3.right) axisObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
        else if (direction == Vector3.forward) axisObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        else axisObj.transform.localRotation = Quaternion.identity;

        axisObj.transform.localScale = new Vector3(axisRadius * 2f, length, axisRadius * 2f);
        UpdateAxisVisual(axisObj, color);
        return axisObj;
    }

    void UpdateAxisVisual(GameObject axisObj, Color color)
    {
        var renderer = axisObj.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Material mat = renderer.sharedMaterial;
        if (mat == null || mat.shader != shader)
            mat = new Material(shader);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.SetColor("_Color", color);

        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void CreateLabels()
    {
        CreateSimpleLabel("x", new Vector3(axisLength + 0.4f, 0, 0), axisLabelColor, axisLabelSize);
        CreateSimpleLabel("y", new Vector3(0, 0, axisLength + 0.4f), axisLabelColor, axisLabelSize);
        CreateSimpleLabel("|0>", new Vector3(0, axisLength + 0.6f, 0), stateLabelColor, stateLabelSize);
        CreateSimpleLabel("|1>", new Vector3(0, -axisLength - 0.6f, 0), stateLabelColor, stateLabelSize);
    }

    void CreateSimpleLabel(string text, Vector3 position, Color color, float fontSize)
    {
        string name = $"Label_{text}";
        Transform existing = transform.Find(name);

        GameObject labelObj;
        TextMeshPro tmpText;

        if (existing != null)
        {
            labelObj = existing.gameObject;
            tmpText = labelObj.GetComponent<TextMeshPro>();
        }
        else
        {
            labelObj = new GameObject(name);
            labelObj.transform.SetParent(transform);
            tmpText = labelObj.AddComponent<TextMeshPro>();
        }

        labelObj.transform.localPosition = position;
        labelObj.transform.localRotation = Quaternion.identity;
        labelObj.transform.localScale = Vector3.one;

        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.color = color;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableAutoSizing = false;
    }

    void UpdateStateVector(Vector3 position)
    {
        currentStatePosition = position.normalized * sphereRadius;

        if (stateVector != null)
            stateVector.transform.localPosition = currentStatePosition;

        if (stateVectorLine != null)
        {
            Vector3 center = currentStatePosition * 0.5f;
            float length = currentStatePosition.magnitude;

            stateVectorLine.transform.localPosition = center;
            if (length > 0.001f)
                stateVectorLine.transform.localRotation = Quaternion.FromToRotation(Vector3.up, currentStatePosition);

            stateVectorLine.transform.localScale = new Vector3(stateVectorWidth, length * 0.5f, stateVectorWidth);
        }
    }

    // =========================
    // Simple parsers
    // =========================
    int ExtractCount(string json, string state)
    {
        try
        {
            string search = $"\"{state}\":";
            int index = json.IndexOf(search);
            if (index == -1) return 0;

            index += search.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;

            int endIndex = index;
            while (endIndex < json.Length && char.IsDigit(json[endIndex])) endIndex++;

            if (endIndex > index)
                return int.Parse(json.Substring(index, endIndex - index));

            return 0;
        }
        catch { return 0; }
    }

    int ExtractInt(string json, string key)
    {
        try
        {
            string search = $"\"{key}\":";
            int index = json.IndexOf(search);
            if (index == -1) return 0;

            index += search.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;

            int endIndex = index;
            while (endIndex < json.Length && char.IsDigit(json[endIndex])) endIndex++;

            if (endIndex > index)
                return int.Parse(json.Substring(index, endIndex - index));

            return 0;
        }
        catch { return 0; }
    }

    void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}

