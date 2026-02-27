using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// แนบกับ CX_Spawn_Prefab root
/// ชื่อ children ต้องตรงเป๊ะ: "Control", "Target"
/// "Cylinder" ถ้าไม่มีใน Prefab จะสร้างอัตโนมัติ
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
    // Chap3
    public CircuitSocket_Chap3 ControlSocket      { get; private set; }
    public CircuitSocket_Chap3 TargetSocket       { get; private set; }
    // Legacy (BlochSphere)
    public CircuitSocket       ControlSocketLegacy { get; private set; }
    public CircuitSocket       TargetSocketLegacy  { get; private set; }

    public bool IsComplete =>
        (ControlSocket != null || ControlSocketLegacy != null) &&
        (TargetSocket  != null || TargetSocketLegacy  != null);

    private CXTargetVisual targetVisualComponent;
    private LineRenderer   dashedLine;
    private Rigidbody      rb;

    // ─── Unity lifecycle ───────────────────────────────────────────────────
    void Start() { }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        if (controlVisual == null) controlVisual = transform.Find("Control");
        if (targetVisual  == null) targetVisual  = transform.Find("Target");
        if (cylinder      == null) cylinder      = transform.Find("Cylinder");

        if (controlVisual == null) Debug.LogError("[CXSpawnedGate] Missing child 'Control'");
        if (targetVisual  == null) Debug.LogError("[CXSpawnedGate] Missing child 'Target'");

        // ✅ สร้าง Cylinder ใน code ถ้าไม่มี child (ไม่ต้องพึ่งชื่อใน Prefab)
        if (cylinder == null)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "Cylinder";
            cyl.transform.SetParent(transform);
            cyl.transform.localPosition = Vector3.zero;
            cyl.transform.localRotation = Quaternion.identity;
            Destroy(cyl.GetComponent<Collider>());
            cylinder = cyl.transform;
            Debug.Log("[CXSpawnedGate] Cylinder created at runtime.");
        }

        targetVisualComponent = targetVisual?.GetComponent<CXTargetVisual>();
        cylinder.gameObject.SetActive(false);

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

    // ─── Init ──────────────────────────────────────────────────────────────

    // ✅ Chap3
    public void Init(CircuitSocket_Chap3 controlSocket)
    {
        ControlSocket = controlSocket;
        controlSocket.SetOccupiedByCX(this, isControl: true, isTarget: false);
        InitCommon(controlSocket.transform, controlSocket.socketName);
    }

    // ✅ Legacy (BlochSphere)
    public void Init(CircuitSocket controlSocket)
    {
        ControlSocketLegacy = controlSocket;
        controlSocket.SetOccupiedByCX(this, isControl: true, isTarget: false);
        InitCommon(controlSocket.transform, controlSocket.socketName);
    }

    private void InitCommon(Transform socketTransform, string socketName)
    {
        // ✅ snap root ให้ตรง socket ทั้ง position และ rotation (เอียงตามโต๊ะ)
        transform.SetPositionAndRotation(socketTransform.position, socketTransform.rotation);

        if (rb != null)
        {
            rb.isKinematic     = true;
            rb.useGravity      = false;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints     = RigidbodyConstraints.FreezeAll;
        }

        if (controlVisual != null)
        {
            controlVisual.localPosition = Vector3.zero;
            controlVisual.localRotation = Quaternion.identity; // ✅ FIX: reset rotation ให้ขนานโต๊ะ
            XRGrabInteractable grab = controlVisual.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.trackRotation = false;
        }

        if (targetVisual != null)
        {
            Rigidbody trb = targetVisual.GetComponent<Rigidbody>();
            if (trb != null)
            {
                trb.isKinematic     = true;
                trb.useGravity      = false;
                trb.linearVelocity  = Vector3.zero;
                trb.angularVelocity = Vector3.zero;
                trb.constraints     = RigidbodyConstraints.FreezeAll;
            }
            targetVisual.localPosition = targetFloatOffset;
            targetVisual.localRotation = Quaternion.identity; // ✅ FIX: reset rotation ให้ขนานโต๊ะ
            XRGrabInteractable grab = targetVisual.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.trackRotation = false;
            targetVisual.gameObject.SetActive(true);
            if (targetVisualComponent != null)
                targetVisualComponent.SetParentGate(this);
        }

        cylinder.gameObject.SetActive(false);

        StartCoroutine(SubscribeControlGrabNextFrame());
        Debug.Log($"[CXSpawnedGate] ✅ Init FINAL. Control={socketName}");
    }

    private IEnumerator SubscribeControlGrabNextFrame()
    {
        yield return null;
        XRGrabInteractable controlGrab = controlVisual?.GetComponent<XRGrabInteractable>();
        if (controlGrab != null)
            controlGrab.selectEntered.AddListener(_ => DetachFromSocket());
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
        Vector3 to;
        if (TargetSocket != null)
            to = TargetSocket.transform.position;
        else if (TargetSocketLegacy != null)
            to = TargetSocketLegacy.transform.position;
        else
            to = targetVisual.position;

        float dist = Vector3.Distance(from, to);
        if (dist < 0.001f) return;

        cylinder.gameObject.SetActive(true);
        cylinder.position   = (from + to) * 0.5f;
        cylinder.rotation   = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
        cylinder.localScale = new Vector3(cylinderDiameter, dist / (cylinderHalfHeight * 2f), cylinderDiameter);
    }

    // ─── Callbacks from CXTargetVisual ─────────────────────────────────────

    // ✅ Chap3
    public void OnTargetPlaced(CircuitSocket_Chap3 targetSocket)
    {
        TargetSocket = targetSocket;
        OnTargetPlacedCommon();
        Debug.Log($"[CXSpawnedGate] 🎉 Complete! Control row={ControlRow}, Target row={TargetRow}");
    }

    // ✅ Legacy
    public void OnTargetPlaced(CircuitSocket targetSocket)
    {
        TargetSocketLegacy = targetSocket;
        OnTargetPlacedCommon();
        Debug.Log($"[CXSpawnedGate] 🎉 Complete! Control row={ControlRow}, Target row={TargetRow}");
    }

    private void OnTargetPlacedCommon()
    {
        HideDashedPreview(); // ✅ FIX: เรียกตรงๆ ไม่ต้อง check null ซ้ำ (method จัดการเองอยู่แล้ว)
        UpdateCylinder();    // ✅ FIX: เรียกตรงๆ cylinder guaranteed ไม่ null แล้ว
        TeleportationCircuitManager.Instance?.RegisterCXGate(this);
    }

    public void OnTargetRemoved()
    {
        TeleportationCircuitManager.Instance?.UnregisterCXGate(this);
        TargetSocket       = null;
        TargetSocketLegacy = null;
        if (cylinder != null) cylinder.gameObject.SetActive(false);
        Debug.Log("[CXSpawnedGate] Target removed.");
    }

    public void DetachFromSocket()
    {
        if (ControlSocket != null)       ControlSocket.ClearCXState();
        if (ControlSocketLegacy != null) ControlSocketLegacy.ClearCXState();

        ControlSocket       = null;
        ControlSocketLegacy = null;

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            rb.useGravity  = true;
        }
    }

    // ─── Public accessors ───────────────────────────────────────────────────
    public string GateName => "CX";

    public int ControlRow =>
        ControlSocket?.rowIndex ?? ControlSocketLegacy?.rowIndex ?? -1;

    public int TargetRow =>
        TargetSocket?.rowIndex ?? TargetSocketLegacy?.rowIndex ?? -1;

    public int ColumnIndex =>
        ControlSocket?.socketIndex ?? ControlSocketLegacy?.socketIndex ?? -1;
}