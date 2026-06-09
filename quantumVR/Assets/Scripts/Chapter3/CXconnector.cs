using UnityEngine;
using System.Collections.Generic;

// ════════════════════════════════════════════════════════════════
//  CXConnector.cs  (v2 — ไม่ต้องใช้ texture / Dashed material)
//
//  Solid  → LineRenderer 2 points ปกติ
//  Dashed → วาดหลาย segment สลับ visible/gap
//           ใช้ Material เดียว (URP/Lit หรืออะไรก็ได้)
// ════════════════════════════════════════════════════════════════

[RequireComponent(typeof(LineRenderer))]
public class CXConnector : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────
    [Header("Material (ใช้อันเดียว — CX_Solid)")]
    [SerializeField] private Material lineMaterial;

    [Header("Line Style")]
    [SerializeField] private float lineWidth  = 0.008f;
    [SerializeField] private Color solidColor = new Color(0.22f, 0.54f, 0.87f, 1f);
    [SerializeField] private Color dashColor  = new Color(0.22f, 0.54f, 0.87f, 0.5f);

    [Header("Dashed Settings")]
    [SerializeField] private float dashLength = 0.03f;
    [SerializeField] private float gapLength  = 0.02f;

    // ─────────────────────────────────────────
    //  Runtime
    // ─────────────────────────────────────────
    private CXSpawnedGate parentGate;
    private LineRenderer  lr;
    private bool          isDashed = false;

    // ─────────────────────────────────────────
    //  Init — เรียกจาก CXSpawnedGate.Initialize()
    // ─────────────────────────────────────────
    public void Initialize(CXSpawnedGate gate)
    {
        parentGate = gate;
        lr         = GetComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.startWidth    = lineWidth;
        lr.endWidth      = lineWidth;

        if (lineMaterial != null)
            lr.material = lineMaterial;

        ApplySolid();
    }

    // ─────────────────────────────────────────
    //  Update — วาดเส้นทุก frame
    // ─────────────────────────────────────────
    void Update()
    {
        if (parentGate == null) return;
        if (parentGate.ControlVisual == null || parentGate.TargetVisual == null) return;

        Vector3 from = parentGate.ControlVisual.transform.position;
        Vector3 to   = parentGate.TargetVisual.transform.position;

        if (isDashed)
            DrawDashed(from, to);
        else
            DrawSolid(from, to);
    }

    // ─────────────────────────────────────────
    //  OnStateChanged — เรียกจาก CXSpawnedGate
    // ─────────────────────────────────────────
    public void OnStateChanged(CXSpawnedGate.GateState state)
    {
        switch (state)
        {
            case CXSpawnedGate.GateState.InHand:
            case CXSpawnedGate.GateState.PlacedFull:
                ApplySolid();
                break;

            case CXSpawnedGate.GateState.ControlPlaced:
            case CXSpawnedGate.GateState.Repositioning:
                ApplyDashed();
                break;
        }

        Debug.Log($"[CXConnector] → {(isDashed ? "Dashed" : "Solid")} (state={state})");
    }

    // ─────────────────────────────────────────
    //  Draw
    // ─────────────────────────────────────────
    private void DrawSolid(Vector3 from, Vector3 to)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    private void DrawDashed(Vector3 from, Vector3 to)
    {
        float totalLen = Vector3.Distance(from, to);
        if (totalLen < 0.001f) return;

        Vector3 dir    = (to - from).normalized;
        var     points = new List<Vector3>();
        float   t      = 0f;

        while (t < totalLen)
        {
            // dash start → dash end
            Vector3 dashStart = from + dir * t;
            float   dashEnd   = Mathf.Min(t + dashLength, totalLen);
            Vector3 dashEndPt = from + dir * dashEnd;

            points.Add(dashStart);
            points.Add(dashEndPt);

            // gap: เพิ่ม 2 point ซ้อนกันที่ตำแหน่งเดิม → ความยาว 0 = ไม่วาด
            float   gapEnd    = Mathf.Min(dashEnd + gapLength, totalLen);
            Vector3 gapEndPt  = from + dir * gapEnd;
            points.Add(gapEndPt);
            points.Add(gapEndPt);

            t = dashEnd + gapLength;
        }

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }

    // ─────────────────────────────────────────
    //  Style
    // ─────────────────────────────────────────
    private void ApplySolid()
    {
        isDashed      = false;
        lr.startColor = solidColor;
        lr.endColor   = solidColor;
    }

    private void ApplyDashed()
    {
        isDashed      = true;
        lr.startColor = dashColor;
        lr.endColor   = dashColor;
    }
}