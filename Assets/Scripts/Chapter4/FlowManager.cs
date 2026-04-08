using UnityEngine;
using System.Collections.Generic;

// ════════════════════════════════════════════════════════════════
//  FlowManager.cs
//
//  ติดกับ GameObject เดียวกับ GraphManager
//  เก็บ list ของ LinkFlowEffect ทุกตัว
//  GraphManager เรียก SetFidelity / SetFlowEnabled ผ่าน FlowManager
// ════════════════════════════════════════════════════════════════

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    private List<LinkFlowEffect> effects = new List<LinkFlowEffect>();

    void Awake()
    {
        Instance = this;
    }

    // ─────────────────────────────────────────
    //  Register / Unregister
    //  เรียกจาก TopologyBuilder ตอน SpawnObjects
    // ─────────────────────────────────────────
    public void Register(LinkFlowEffect effect)
    {
        if (!effects.Contains(effect))
            effects.Add(effect);
    }

    public void Clear()
    {
        effects.Clear();
    }

    // ─────────────────────────────────────────
    //  Control
    // ─────────────────────────────────────────
    public void SetFidelity(float fidelity)
    {
        foreach (var e in effects)
            if (e != null) e.SetFidelity(fidelity);
    }

    public void SetFlowEnabled(bool enabled)
    {
        foreach (var e in effects)
            if (e != null) e.SetFlowEnabled(enabled);
    }

    public void RefreshAll(float fidelity)
    {
        foreach (var e in effects)
            if (e != null)
            {
                e.RefreshPathAndRespawn();
                e.SetFidelity(fidelity);
            }
    }
}