using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// CX_Spawn_Prefab (Root) — Root > Control + Target + Cylinder
///
/// Flow:
///   1. CXCubeOnShelf วางบน Socket (rotation 90,90,0) → Init(socket)
///   2. Control snap ที่ socket ตั้งตรง, Target ลอยเหนือรอผู้เล่นจับ
///   3. ระหว่างลาก Target → Dashed Line ยืดตาม
///   4. Target วางลง Socket → Dashed Line หาย, Cylinder solid ปรากฏ
/// </summary>
public class CXSpawnedGate : MonoBehaviour
{
    [Header("Child References (auto-found by name if left empty)")]
    [SerializeField] private Transform controlVisual;
    [SerializeField] private Transform targetVisual;
    [SerializeField] private Transform cylinder;

    [Header("Target Float")]
    [SerializeField] private Vector3 targetFloatOffset = new Vector3(0f, 0.18f, 0f);

    [Header("Cylinder (solid)")]
    [SerializeField] private float    cylinderRadius = 0.03f;
    [SerializeField] private Material solidMaterial;

    [Header("Dashed Line Preview")]
    [SerializeField] private Material dashedLineMaterial;
    [SerializeField] private float    dashedLineWidth    = 0.015f;
    [SerializeField] private Color    dashedLineColor    = new Color(0.2f, 0.8f, 1f, 0.7f);
    [SerializeField] private int      dashedLineSegments = 30;
    [SerializeField] private float    dashTiling         = 6f;

    // State
    public CircuitSocket_Chap3 ControlSocket       { get; private set; }
    public CircuitSocket_Chap3 TargetSocket        { get; private set; }
    public CircuitSocket       ControlSocketLegacy { get; private set; }
    public CircuitSocket       TargetSocketLegacy  { get; private set; }

    public bool IsComplete =>
        (ControlSocket != null || ControlSocketLegacy != null) &&
        (TargetSocket  != null || TargetSocketLegacy  != null);

    private CXTargetVisual  targetVisualComponent;
    private CXControlVisual controlVisualComponent;
    private Renderer       cylinderRenderer;
    private LineRenderer   dashedLine;

    // ── Awake ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (controlVisual == null) controlVisual = transform.Find("Control");
        if (targetVisual  == null) targetVisual  = transform.Find("Target");
        if (cylinder      == null) cylinder      = transform.Find("Cylinder");

        if (controlVisual == null) Debug.LogError("[CXSpawnedGate] Missing child 'Control'");
        if (targetVisual  == null) Debug.LogError("[CXSpawnedGate] Missing child 'Target'");
        if (cylinder      == null) Debug.LogError("[CXSpawnedGate] Missing child 'Cylinder'");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; rb.constraints = RigidbodyConstraints.FreezeAll; }

        if (controlVisual != null)
            controlVisualComponent = controlVisual.GetComponent<CXControlVisual>();

        if (cylinder != null)
        {
            cylinderRenderer = cylinder.GetComponent<Renderer>();
            cylinder.gameObject.SetActive(false);
        }

        if (targetVisual != null)
        {
            targetVisualComponent = targetVisual.GetComponent<CXTargetVisual>();
            FreezeRigidbody(targetVisual);
            targetVisual.gameObject.SetActive(false);
        }

        SetupDashedLine();
    }

    private void SetupDashedLine()
    {
        dashedLine = gameObject.AddComponent<LineRenderer>();
        dashedLine.positionCount     = dashedLineSegments;
        dashedLine.startWidth        = dashedLineWidth;
        dashedLine.endWidth          = dashedLineWidth;
        dashedLine.startColor        = dashedLineColor;
        dashedLine.endColor          = dashedLineColor;
        dashedLine.useWorldSpace     = true;
        dashedLine.numCornerVertices = 4;
        dashedLine.numCapVertices    = 4;
        dashedLine.textureMode       = LineTextureMode.Tile;
        dashedLine.alignment         = LineAlignment.View;

        Material m = dashedLineMaterial != null
            ? dashedLineMaterial
            : new Material(Shader.Find("Sprites/Default")) { color = dashedLineColor };
        dashedLine.material = m;
        dashedLine.material.SetTextureScale("_MainTex", new Vector2(dashTiling, 1f));
        dashedLine.enabled = false;
    }

    // ── Init ───────────────────────────────────────────────────────────────
    public void Init(CircuitSocket_Chap3 controlSocket)
    {
        ControlSocket = controlSocket;
        controlSocket.SetOccupiedByCX(this, isControl: true, isTarget: false);
        SetupAfterInit(controlSocket.attachTransform ?? controlSocket.transform);
        Debug.Log($"[CXSpawnedGate] Init OK Control={controlSocket.socketName}");
    }

    public void Init(CircuitSocket controlSocket)
    {
        ControlSocketLegacy = controlSocket;
        controlSocket.SetOccupiedByCX(this, isControl: true, isTarget: false);
        SetupAfterInit(controlSocket.transform);
        Debug.Log($"[CXSpawnedGate] Init OK Control(Legacy)={controlSocket.socketName}");
    }

    private void SetupAfterInit(Transform socketTransform)
    {
        // ── Position ───────────────────────────────────────────────────
        transform.position = socketTransform.position;

        // ── Rotation ───────────────────────────────────────────────────
        // Attach child มี rotation (90,90,0):
        //   attachTransform.up    → ชี้ขึ้นในโลก (World Up) พอดี ✅
        //   attachTransform.right → วิ่งตามแนวนอนของโต๊ะ
        //
        // ดังนั้น: gate ตั้งตรงโดยใช้ attachTransform.up เป็น World Up
        // และ project right ลงบน horizontal plane เป็น forward
        Vector3 worldUp = Vector3.up;
        Vector3 fwd = Vector3.ProjectOnPlane(socketTransform.right, worldUp);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        transform.rotation = Quaternion.LookRotation(fwd.normalized, worldUp);

        // ── Control ────────────────────────────────────────────────────
        if (controlVisual != null)
        {
            controlVisual.localPosition = Vector3.zero;
            controlVisual.localRotation = Quaternion.identity;
            FreezeRigidbody(controlVisual);

            if (controlVisualComponent != null)
                controlVisualComponent.Init(this);

            XRGrabInteractable grab = controlVisual.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.trackRotation = false;
                grab.selectEntered.AddListener(OnControlGrabbed);
                grab.selectExited.AddListener(OnControlDropped);
            }
        }

        // ── Target ────────────────────────────────────────────────────
        if (targetVisual != null)
        {
            targetVisual.localPosition = targetFloatOffset;
            targetVisual.localRotation = Quaternion.identity;
            targetVisual.gameObject.SetActive(true);

            XRGrabInteractable grab = targetVisual.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.trackRotation = false;

            if (targetVisualComponent != null)
                targetVisualComponent.Init(this);
        }
    }

    // ── Dashed Line ────────────────────────────────────────────────────────
    public void ShowDashedPreview(Vector3 targetWorldPos)
    {
        if (dashedLine == null || controlVisual == null) return;

        dashedLine.enabled = true;
        Vector3 from = controlVisual.position;

        for (int i = 0; i < dashedLineSegments; i++)
        {
            float t = (float)i / (dashedLineSegments - 1);
            dashedLine.SetPosition(i, Vector3.Lerp(from, targetWorldPos, t));
        }

        float length = Vector3.Distance(from, targetWorldPos);
        dashedLine.material.SetTextureScale("_MainTex", new Vector2(dashTiling * length, 1f));
    }

    public void HideDashedPreview()
    {
        if (dashedLine != null) dashedLine.enabled = false;
    }

    // ── Cylinder ───────────────────────────────────────────────────────────
    public void FinalizeCylinder()
    {
        if (cylinder == null || controlVisual == null) return;

        Vector3 from = controlVisual.position;
        Vector3 to   = TargetSocket       != null ? TargetSocket.transform.position
                     : TargetSocketLegacy != null ? TargetSocketLegacy.transform.position
                     : targetVisual.position;

        float dist = Vector3.Distance(from, to);
        if (dist < 0.001f) return;

        // ── Position & Rotation ────────────────────────────────────────
        cylinder.position = (from + to) * 0.5f;
        cylinder.rotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);

        // ── Scale ──────────────────────────────────────────────────────
        // Unity default Cylinder: height = 2 units เมื่อ localScale.y = 1
        // ดังนั้น localScale.y = dist / 2 เพื่อให้ความยาวตรงกับ dist จริง
        // (เหมือนความยาว Dashed Line พอดี)
        float diameter = cylinderRadius * 2f;
        cylinder.localScale = new Vector3(diameter, dist / 2f, diameter);

        cylinder.gameObject.SetActive(true);

        if (cylinderRenderer != null && solidMaterial != null)
            cylinderRenderer.material = solidMaterial;
    }

    public void HideCylinder()
    {
        if (cylinder != null) cylinder.gameObject.SetActive(false);
    }

    // ── Callbacks ──────────────────────────────────────────────────────────
    public void OnTargetPlaced(CircuitSocket_Chap3 socket)
    {
        TargetSocket = socket;
        HideDashedPreview();
        FinalizeCylinder();
        TeleportationCircuitManager.Instance?.RegisterCXGate(this);
        Debug.Log($"[CXSpawnedGate] Complete! Control={ControlRow} Target={TargetRow}");
    }

    public void OnTargetPlaced(CircuitSocket socket)
    {
        TargetSocketLegacy = socket;
        HideDashedPreview();
        FinalizeCylinder();
        TeleportationCircuitManager.Instance?.RegisterCXGate(this);
        Debug.Log($"[CXSpawnedGate] Complete! Control={ControlRow} Target={TargetRow}");
    }

    public void OnTargetRemoved()
    {
        TeleportationCircuitManager.Instance?.UnregisterCXGate(this);
        TargetSocket       = null;
        TargetSocketLegacy = null;
        HideCylinder();
        Debug.Log("[CXSpawnedGate] Target removed.");
    }

    private void OnControlGrabbed(SelectEnterEventArgs args)
    {
        // ตอน grab: แค่ unfreeze root ให้เคลื่อนได้, ยังไม่ detach socket
        // (รอดูก่อนว่า drop ลง socket ใหม่หรือกลางอากาศ)
        if (targetVisualComponent != null && targetVisualComponent.IsPlaced)
            targetVisualComponent.ForceRemove();

        ControlSocket?.ClearCXState();
        ControlSocketLegacy?.ClearCXState();
        ControlSocket       = null;
        ControlSocketLegacy = null;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.constraints = RigidbodyConstraints.None; rb.isKinematic = true; rb.useGravity = false; }

        HideDashedPreview();
        HideCylinder();
        Debug.Log("[CXSpawnedGate] Control grabbed.");
    }

    private void OnControlDropped(SelectExitEventArgs args)
    {
        // ตรวจว่า drop ลง XRSocket หรือเปล่า
        // ถ้า drop กลางอากาศ → ให้ gravity ดึง root ลง
        bool droppedOnSocket = ControlSocket != null || ControlSocketLegacy != null;

        if (!droppedOnSocket)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.constraints = RigidbodyConstraints.None; rb.isKinematic = false; rb.useGravity = true; }
            Debug.Log("[CXSpawnedGate] Control dropped in air, falling.");
        }
        else
        {
            Debug.Log("[CXSpawnedGate] Control dropped on socket, snapped.");
        }
    }

    /// <summary>เรียกจาก CXTargetVisual หลังจาก Detach ตัวเองออกจาก Root และ Snap ไปที่ socket แล้ว</summary>
    public void OnTargetPlacedOnSocket(CircuitSocket_Chap3 newSocket)
    {
        // NOTE: CXTargetVisual.OnPlacedOnSocket() จัดการ SetParent + position แล้ว
        //       ที่นี่แค่อัพเดต state และวาด Cylinder
        OnTargetPlaced(newSocket);
        Debug.Log($"[CXSpawnedGate] Target confirmed at {newSocket.socketName}");
    }

    /// <summary>เรียกจาก CXControlVisual เมื่อ Control วางลง socket ใหม่</summary>
    public void OnControlPlacedOnSocket(CircuitSocket_Chap3 newSocket)
    {
        ControlSocket = newSocket;
        newSocket.SetOccupiedByCX(this, isControl: true, isTarget: false);

        // ถ้า Target วางอยู่แล้ว → ไม่ขยับ Root เพราะจะพา Target ไปด้วย
        if (targetVisualComponent != null && targetVisualComponent.IsPlaced)
        {
            Debug.Log($"[CXSpawnedGate] Control→{newSocket.socketName} Root NOT moved (Target already placed)");
            return;
        }

        // ย้าย Root ไปที่ Socket ใหม่ เฉพาะตอน Target ยังไม่ได้วาง
        Transform t = newSocket.attachTransform ?? newSocket.transform;
        transform.position = t.position;
        transform.rotation = t.rotation;

        if (controlVisual != null)
        {
            controlVisual.localPosition = Vector3.zero;
            controlVisual.localRotation = Quaternion.identity;
        }

        Debug.Log($"[CXSpawnedGate] Root moved to {newSocket.socketName}");
    }
    // ── Helpers ────────────────────────────────────────────────────────────
    private static void FreezeRigidbody(Transform t)
    {
        Rigidbody rb = t.GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = true; rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
    }

    // ── Public accessors ───────────────────────────────────────────────────
    public Vector3 TargetFloatOffset => targetFloatOffset;
    public string  GateName          => "CX";
    public int     ControlRow        => ControlSocket?.rowIndex    ?? ControlSocketLegacy?.rowIndex    ?? -1;
    public int     TargetRow         => TargetSocket?.rowIndex     ?? TargetSocketLegacy?.rowIndex     ?? -1;
    public int     ColumnIndex       => ControlSocket?.socketIndex ?? ControlSocketLegacy?.socketIndex ?? -1;
}