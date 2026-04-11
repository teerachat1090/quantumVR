using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class LinkFlowEffect : MonoBehaviour
{
    [Header("Photon — จำนวนและขนาด")]
    [Range(1, 12)]
    [SerializeField] private int particleCount = 6;

    [Range(0.01f, 0.3f)]
    [SerializeField] private float dotSize = 0.05f;

    [Header("Photon — ความเร็ว")]
    [Range(0.2f, 8f)]
    [SerializeField] private float flowSpeed = 0.8f;

    [Header("Photon — หาง (Tail)")]
    [Range(0, 5)]
    [SerializeField] private int tailCount = 3;

    [Range(0.01f, 0.3f)]
    [SerializeField] private float tailSpacing = 0.06f;

    [Range(0f, 0.8f)]
    [SerializeField] private float tailEndAlpha = 0.05f;

    [Header("Fiber Glow")]
    [Range(0f, 0.5f)]
    [SerializeField] private float glowAlphaMin = 0.10f;

    [Range(0f, 0.5f)]
    [SerializeField] private float glowAlphaMax = 0.25f;

    [Range(0.1f, 3f)]
    [SerializeField] private float glowPulseSpeed = 0.8f;

    [Header("Fidelity Color")]
    [SerializeField] private Color colorHigh = new Color(0.0f, 1.0f, 0.0f);  // เขียวสด
    [SerializeField] private Color colorMid  = new Color(1.0f, 0.5f, 0.0f);  // ส้ม
    [SerializeField] private Color colorLow  = new Color(1.0f, 0.1f, 0.0f);  // แดงส้ม

    [Range(0f, 100f)]
    [SerializeField] private float fidLow  = 50f;

    [Range(0f, 100f)]
    [SerializeField] private float fidHigh = 85f;

    // ── Runtime ──────────────────────────────────────────
    private LineRenderer     lr;
    private float[]          headOffsets;
    private List<GameObject> dots            = new List<GameObject>();
    private bool             flowEnabled     = true;
    private bool             reversed        = false;
    private float            speedMultiplier = 1f;
    private float            currentFidelity = 90f;
    private Color            particleColor;

    private Vector3[] pathPoints;
    private float[]   pathSegLengths;
    private float     totalLength;

    // ── Unity ─────────────────────────────────────────────
    void Awake()
    {
        lr            = GetComponent<LineRenderer>();
        particleColor = colorHigh;
    }

    void Start()
    {
        RefreshPath();
        SpawnDots();
        SetFidelity(currentFidelity);
    }

    // ── Path ──────────────────────────────────────────────
    void RefreshPath()
    {
        int n = lr.positionCount;
        if (n < 2) n = 2;

        pathPoints     = new Vector3[n];
        pathSegLengths = new float[n - 1];
        totalLength    = 0f;

        lr.GetPositions(pathPoints);

        if (!lr.useWorldSpace)
            for (int i = 0; i < n; i++)
                pathPoints[i] = lr.transform.TransformPoint(pathPoints[i]);

        for (int i = 0; i < n - 1; i++)
        {
            float d = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            pathSegLengths[i] = d;
            totalLength      += d;
        }

        if (totalLength < 0.001f) totalLength = 0.001f;
    }

    Vector3 SamplePath(float t)
    {
        t = Mathf.Repeat(t, 1f);
        float dist = t * totalLength;
        float acc  = 0f;

        for (int i = 0; i < pathSegLengths.Length; i++)
        {
            float seg = pathSegLengths[i];
            if (acc + seg >= dist)
            {
                float local = (dist - acc) / Mathf.Max(seg, 0.0001f);
                return Vector3.Lerp(pathPoints[i], pathPoints[i + 1], local);
            }
            acc += seg;
        }
        return pathPoints[pathPoints.Length - 1];
    }

    // ── Spawn ─────────────────────────────────────────────
    void SpawnDots()
    {
        foreach (var d in dots) if (d) Destroy(d);
        dots.Clear();

        headOffsets = new float[particleCount];

        for (int p = 0; p < particleCount; p++)
        {
            headOffsets[p] = (float)p / particleCount;
            dots.Add(CreateDot(dotSize, 1.0f));

            for (int t = 0; t < tailCount; t++)
            {
                float ratio    = (float)(t + 1) / (tailCount + 1);
                float sizeMul  = Mathf.Lerp(0.85f, 0.35f, ratio);
                float alphaMul = Mathf.Lerp(0.6f, tailEndAlpha, ratio);
                dots.Add(CreateDot(dotSize * sizeMul, alphaMul));
            }
        }
    }

    GameObject CreateDot(float size, float alphaMul)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<SphereCollider>());
        go.transform.localScale = Vector3.one * size;
        go.name = "Photon";

        var mr     = go.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);

        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(particleColor.r, particleColor.g, particleColor.b, alphaMul);

        mr.material          = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        return go;
    }

    // ── Update ────────────────────────────────────────────
    void Update()
    {
        if (!flowEnabled) return;
        if (headOffsets == null || dots.Count == 0) return;

        float glowT   = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
        float alpha   = Mathf.Lerp(glowAlphaMin, glowAlphaMax, glowT);
        Color lineCol = lr.startColor;
        lineCol.a     = alpha;
        lr.startColor = lineCol;
        lr.endColor   = lineCol;

        float step = (flowSpeed * speedMultiplier / totalLength) * Time.deltaTime;
        if (reversed) step = -step;
        int dpp = 1 + tailCount;

        for (int p = 0; p < particleCount; p++)
        {
            headOffsets[p] = Mathf.Repeat(headOffsets[p] + step, 1f);

            int baseIdx = p * dpp;
            if (baseIdx < dots.Count && dots[baseIdx] != null)
                dots[baseIdx].transform.position = SamplePath(headOffsets[p]);

            for (int t = 0; t < tailCount; t++)
            {
                int   di    = baseIdx + 1 + t;
                float tailT = headOffsets[p] - (t + 1) * tailSpacing / totalLength;
                if (di < dots.Count && dots[di] != null)
                    dots[di].transform.position = SamplePath(tailT);
            }
        }
    }

    // ── Public API ────────────────────────────────────────
    public void SetFidelity(float fidelity)
    {
        currentFidelity = fidelity;
        float t = Mathf.InverseLerp(fidLow, fidHigh, fidelity);

        // ใช้ threshold ที่กว้างขึ้นเพื่อไม่ให้สีกระพริบตาม fluctuation
        // เขียว >= 75, เหลือง >= 55, แดง < 55
        float fidHigh2 = 75f, fidLow2 = 55f;
        float t2 = Mathf.InverseLerp(fidLow2, fidHigh2, fidelity);
        Color col = t2 >= 0.5f
            ? Color.Lerp(colorMid, colorHigh, (t2 - 0.5f) * 2f)
            : Color.Lerp(colorLow, colorMid,  t2 * 2f);

        ApplyColor(col);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
    }

    public void SetReversed(bool rev) { reversed = rev; }

    public void SetDegraded(bool degraded)
    {
        if (degraded) ApplyColor(colorLow);
        else SetFidelity(currentFidelity);
    }

    public void SetFlowEnabled(bool enabled)
    {
        flowEnabled = enabled;
        foreach (var d in dots) if (d) d.SetActive(enabled);

        if (!enabled)
        {
            Color c   = lr.startColor;
            c.a       = 0.15f;
            lr.startColor = c;
            lr.endColor   = c;
        }
        else
        {
            // restore สีตาม currentFidelity เมื่อ re-enable
            SetFidelity(currentFidelity);
        }
    }

    public bool IsFlowEnabled() => flowEnabled;

    public void RefreshPathAndRespawn()
    {
        RefreshPath();
        SpawnDots();
        SetFidelity(currentFidelity);
    }

    void ApplyColor(Color col)
    {
        particleColor = col;
        int dpp = 1 + tailCount;
        for (int p = 0; p < particleCount; p++)
        {
            for (int d = 0; d < dpp; d++)
            {
                int idx = p * dpp + d;
                if (idx >= dots.Count || dots[idx] == null) continue;
                float alphaMul = (d == 0) ? 1.0f : Mathf.Lerp(0.6f, tailEndAlpha, (float)d / dpp);
                var mat = dots[idx].GetComponent<MeshRenderer>().material;
                mat.color = new Color(col.r, col.g, col.b, alphaMul);
            }
        }
        Color lc = new Color(col.r, col.g, col.b, glowAlphaMin);
        lr.startColor = lc;
        lr.endColor   = lc;
    }

    void OnDestroy()
    {
        foreach (var d in dots) if (d) Destroy(d);
    }
}