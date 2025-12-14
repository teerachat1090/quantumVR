using UnityEngine;

public class BlochRotationArcRenderer : MonoBehaviour
{
    [Header("Arc Style")]
    [SerializeField] private int segments = 64;
    [SerializeField] private float radiusOffset = 0.01f;
    [SerializeField] private float width = 0.02f;

    [Header("Colors")]
    [SerializeField] private Color gateColor = Color.cyan;
    [SerializeField] private Color measurementColor = new Color(1f, 1f, 1f, 0.8f);

    private float sphereRadius = 2f;
    private GameObject arcRoot;

    public void SetSphereRadius(float r) => sphereRadius = Mathf.Max(0.01f, r);

    public void Clear()
    {
        if (arcRoot != null)
        {
            Destroy(arcRoot);
            arcRoot = null;
        }
    }

    /// <summary>✨ วาด arc ตามแกนหมุนจริงของ gate</summary>
    public void DrawGateArcWithAxis(Vector3 startUnit, Vector3 endUnit, Vector3 rotationAxis, float angleDeg)
    {
        DrawArcAroundAxis(startUnit, rotationAxis, angleDeg, gateColor);
    }

    /// <summary>🎯 วาด arc สำหรับ gate (fallback - ไม่รู้แกนหมุน)</summary>
    public void DrawGateArc(Vector3 startUnit, Vector3 endUnit)
        => DrawArcInternal(startUnit, endUnit, gateColor);

    /// <summary>📊 วาด arc สำหรับ measurement</summary>
    public void DrawMeasurementArc(Vector3 startUnit, Vector3 endUnit)
        => DrawArcInternal(startUnit, endUnit, measurementColor);

    // ===== Axis-accurate arc =====
    private void DrawArcAroundAxis(Vector3 startUnit, Vector3 axis, float angleDeg, Color color)
    {
        startUnit = startUnit.normalized;
        axis = axis.normalized;

        if (Mathf.Abs(angleDeg) < 0.1f) return;

        Clear();

        arcRoot = new GameObject("RotationArc");
        arcRoot.transform.SetParent(transform, false);
        arcRoot.transform.localPosition = Vector3.zero;
        arcRoot.transform.localRotation = Quaternion.identity;
        arcRoot.transform.localScale = Vector3.one;

        var lr = arcRoot.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = segments + 1;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = angleDeg * t;

            Vector3 pointOnArc = Quaternion.AngleAxis(currentAngle, axis) * startUnit;
            Vector3 p = pointOnArc.normalized * (sphereRadius + radiusOffset);
            lr.SetPosition(i, p);
        }
    }

    // ===== Fallback arc (slerp) =====
    private void DrawArcInternal(Vector3 startUnit, Vector3 endUnit, Color color)
    {
        startUnit = startUnit.normalized;
        endUnit = endUnit.normalized;

        float ang = Vector3.Angle(startUnit, endUnit);
        if (ang < 0.1f) return;

        Clear();

        arcRoot = new GameObject("RotationArc");
        arcRoot.transform.SetParent(transform, false);
        arcRoot.transform.localPosition = Vector3.zero;
        arcRoot.transform.localRotation = Quaternion.identity;
        arcRoot.transform.localScale = Vector3.one;

        var lr = arcRoot.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = segments + 1;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Slerp(startUnit, endUnit, t) * (sphereRadius + radiusOffset);
            lr.SetPosition(i, p);
        }
    }
}
