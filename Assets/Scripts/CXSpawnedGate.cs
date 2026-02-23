using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// แนบกับ CX_Spawn_Prefab root
/// ชื่อ children ต้องตรงเป๊ะ: "Control", "Target", "Cylinder"
/// </summary>
public class CXSpawnedGate : MonoBehaviour
{
    [Header("Child Visuals (auto-found by name)")]
    [SerializeField] private Transform controlVisual;
    [SerializeField] private Transform targetVisual;
    [SerializeField] private Transform cylinder;

    [Header("Cylinder Settings")]
    [SerializeField] private float cylinderHalfHeight = 1f;
    [SerializeField] private float cylinderDiameter   = 0.06f;

    [Header("Dashed Preview Line")]
    [SerializeField] private Material dashedLineMaterial;
    [SerializeField] private float  dashedLineWidth    = 0.015f;
    [SerializeField] private Color  dashedLineColor    = new Color(0.2f, 0.8f, 1f, 0.7f);
    [SerializeField] private int    dashedLineSegments = 30;
    [SerializeField] private float  dashTiling         = 6f;

    [Header("Target Float Offset")]
    [SerializeField] private Vector3 targetFloatOffset = new Vector3(0f, 0.15f, 0f);

    // ─── State ─────────────────────────────────────────────────────────────
    public CircuitSocket ControlSocket { get; private set; }  // ← CircuitSocket
    public CircuitSocket TargetSocket  { get; private set; }  // ← CircuitSocket
    public bool IsComplete => ControlSocket != null && TargetSocket != null;

    private CXTargetVisual targetVisualComponent;
    private LineRenderer   dashedLine;
    private Rigidbody rb;
    // ─── Unity lifecycle ───────────────────────────────────────────────────

        void Start()
    {
        XRGrabInteractable controlGrab = controlVisual?.GetComponent<XRGrabInteractable>();
        if (controlGrab != null)
            controlGrab.selectEntered.AddListener(_ => DetachFromSocket());
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }
        if (controlVisual == null) controlVisual = transform.Find("Control");
        if (targetVisual  == null) targetVisual  = transform.Find("Target");
        if (cylinder      == null) cylinder      = transform.Find("Cylinder");

        if (controlVisual == null) Debug.LogError("[CXSpawnedGate] Missing child 'Control'");
        if (targetVisual  == null) Debug.LogError("[CXSpawnedGate] Missing child 'Target'");
        if (cylinder      == null) Debug.LogError("[CXSpawnedGate] Missing child 'Cylinder'");

        targetVisualComponent = targetVisual?.GetComponent<CXTargetVisual>();

        if (cylinder != null) cylinder.gameObject.SetActive(false);

        SetupDashedLine();
    }

    // ─── Dashed Line ───────────────────────────────────────────────────────
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

    // ─── Init (เรียกจาก CXCubeOnShelf) ────────────────────────────────────
    public void Init(CircuitSocket controlSocket)
{
    // ─────────────────────────────────────────────
    // Bind socket
    ControlSocket = controlSocket;
    controlSocket.SetOccupiedByCX(this, isControl: true, isTarget: false);

    // ─────────────────────────────────────────────
    // ROOT: snap ให้ตรง socket (ตัวเดียวพอ)
    transform.SetPositionAndRotation(
        controlSocket.transform.position,
        controlSocket.transform.rotation
    );

    // 🔒 LOCK ROOT physics
    if (rb != null)
    {
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    // ─────────────────────────────────────────────
    // CONTROL (ใช้ local จาก prefab)
    if (controlVisual != null)
    {
        controlVisual.localPosition = Vector3.zero;
        controlVisual.localRotation = Quaternion.identity;

        XRGrabInteractable grab = controlVisual.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.trackRotation = false;
    }

    // ─────────────────────────────────────────────
    // TARGET (local space เท่านั้น ❗)
    if (targetVisual != null)
    {
        // 🔒 Physics lock
        Rigidbody trb = targetVisual.GetComponent<Rigidbody>();
        if (trb != null)
        {
            trb.isKinematic = true;
            trb.useGravity  = false;
            trb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // ✅ ใช้ local offset จาก prefab
        targetVisual.localPosition = targetFloatOffset;
        targetVisual.localRotation = Quaternion.identity;

        XRGrabInteractable grab = targetVisual.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.trackRotation = false;

        targetVisual.gameObject.SetActive(true);

        if (targetVisualComponent != null)
            targetVisualComponent.SetParentGate(this);
    }

    // ─────────────────────────────────────────────
    // CYLINDER
    if (cylinder != null)
        cylinder.gameObject.SetActive(false);

    Debug.Log($"[CXSpawnedGate] ✅ Init FINAL. Control={controlSocket.socketName}");
}
     // ─── Dashed preview ────────────────────────────────────────────────────
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

    // ─── Cylinder ──────────────────────────────────────────────────────────
    public void UpdateCylinder()
    {
        if (controlVisual == null || cylinder == null) return;

        Vector3 from = controlVisual.position;
        Vector3 to   = TargetSocket != null
                       ? TargetSocket.transform.position
                       : targetVisual.position;

        float dist = Vector3.Distance(from, to);
        if (dist < 0.001f) return;

        cylinder.gameObject.SetActive(true);
        cylinder.position   = (from + to) * 0.5f;
        cylinder.rotation   = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
        cylinder.localScale = new Vector3(cylinderDiameter, dist / (cylinderHalfHeight * 2f), cylinderDiameter);
    }

    // ─── Callbacks from CXTargetVisual ─────────────────────────────────────
    public void OnTargetPlaced(CircuitSocket targetSocket)
    {
        TargetSocket = targetSocket;

        // 🔒 กัน dashedLine ยังไม่พร้อม
        if (dashedLine != null)
            HideDashedPreview();

        // 🔒 กัน cylinder / controlVisual ยังไม่พร้อม
        if (controlVisual != null && cylinder != null)
            UpdateCylinder();

        // 🔒 กัน Manager ยังไม่ init
        if (TeleportationCircuitManager.Instance != null)
            TeleportationCircuitManager.Instance.RegisterCXGate(this);

        // 🔒 กัน socket หลุด
        int controlRow = ControlSocket != null ? ControlSocket.rowIndex : -1;
        int targetRow  = TargetSocket  != null ? TargetSocket.rowIndex  : -1;

        Debug.Log($"[CXSpawnedGate] 🎉 Complete! Control row={controlRow}, Target row={targetRow}");
    }

    public void OnTargetRemoved()
    {
        TeleportationCircuitManager.Instance?.UnregisterCXGate(this);
        TargetSocket = null;
        if (cylinder != null) cylinder.gameObject.SetActive(false);
        // ไม่ต้องเปิด gravity ตรงนี้ — gate ยังติด socket อยู่
        Debug.Log("[CXSpawnedGate] Target removed.");
    }

    // เรียกเมื่อต้องการเอา gate ออกจาก circuit จริงๆ
    public void DetachFromSocket()
    {
        if (ControlSocket != null)
            ControlSocket.ClearCXState();

        ControlSocket = null;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    // ─── Public accessors ───────────────────────────────────────────────────
    public string GateName    => "CX";
    public int    ControlRow  => ControlSocket?.rowIndex  ?? -1;
    public int    TargetRow   => TargetSocket?.rowIndex   ?? -1;
    public int    ColumnIndex => ControlSocket?.socketIndex ?? -1;
}