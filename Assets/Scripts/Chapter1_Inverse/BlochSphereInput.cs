using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// แขวนบน StateVector
/// Ray hover → เปิด preset canvas
/// Ray ออก → ปิด canvas
/// </summary>
public class BlochSphereInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlochSphere blochSphere;
    [SerializeField] private InverseModeManager inverseModeManager;
    [SerializeField] private GameObject presetCanvas;   // BlochPresetCanvas
    [SerializeField] private float sphereRadius = 2f;

    private bool canvasOpen = false;

    // ---------- เรียกจากปุ่มใน Canvas ----------

    public void SetState_Zero()  => SetState(new Vector3(0, 1, 0),  "|0⟩");
    public void SetState_One()   => SetState(new Vector3(0, -1, 0), "|1⟩");
    public void SetState_Plus()  => SetState(new Vector3(1, 0, 0),  "|+⟩");
    public void SetState_Minus() => SetState(new Vector3(-1, 0, 0), "|−⟩");
    public void SetState_i()     => SetState(new Vector3(0, 0, 1),  "|i⟩");
    public void SetState_T()
    {
        float t = Mathf.PI / 4f;
        SetState(new Vector3(Mathf.Sin(t)*Mathf.Cos(t), Mathf.Cos(t), Mathf.Sin(t)*Mathf.Sin(t)), "|T⟩");
    }

    // ---------- XR Hover Events ----------
    // ผูก OnHoverEntered และ OnHoverExited จาก XRGrabInteractable ใน Inspector

    public void OnHoverEnter()
    {
        if (presetCanvas != null)
        {
            presetCanvas.SetActive(true);
            canvasOpen = true;
        }
    }

    public void OnHoverExit()
    {
        // หน่วงปิดนิดนึง เผื่อผู้เล่นกำลังขยับ ray ไปกดปุ่ม
        Invoke(nameof(DelayClose), 0.5f);
    }

    private void DelayClose()
    {
        if (presetCanvas != null)
            presetCanvas.SetActive(false);
        canvasOpen = false;
    }

    // ---------- Helper ----------

    private void SetState(Vector3 vec, string label)
    {
        vec = vec.normalized;
        blochSphere?.AnimateToStateDirectly(vec);
        inverseModeManager?.SetTargetVector(vec);
        Debug.Log($"[BlochSphereInput] Set state: {label}");

        // ปิด canvas หลังเลือก
        CancelInvoke(nameof(DelayClose));
        if (presetCanvas != null) presetCanvas.SetActive(false);
        canvasOpen = false;
    }
}