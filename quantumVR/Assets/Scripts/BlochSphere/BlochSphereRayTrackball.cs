using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;

/// <summary>
/// FINAL CLEAN: Bloch Sphere Ray Trackball (XRI 3.x friendly)
/// - กด Trigger ค้าง + เล็งโดน sphere => จุดเหลืองค้าง
/// - ระหว่างลาก => หมุนตามการขยับมือ + จุดเหลืองตาม hit point
/// - ปล่อย Trigger => หยุด + จุดเหลืองหาย (ปล่อย inertia ทำงานได้)
/// </summary>
public class BlochSphereRayTrackball : MonoBehaviour
{
    [Header("Rotation Target")]
    [SerializeField] private Transform rotatingContainer;

    [Header("Feel")]
    [SerializeField] private float rotationSpeed = 1.0f;
    [Tooltip("แปลงการขยับมือ (เมตร) -> องศาหมุน (แนะนำ 80-160)")]
    [SerializeField] private float positionToDegrees = 120f;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Raycast")]
    [SerializeField] private SphereCollider sphereCollider;
    [SerializeField] private float sphereRadius = 2.0f;
    [SerializeField] private LayerMask raycastLayerMask = ~0;
    [SerializeField] private float rayDistance = 10f;

    [Header("XR Ray Interactors (assign in Inspector)")]
    [SerializeField] private XRRayInteractor leftRay;
    [SerializeField] private XRRayInteractor rightRay;

    [Header("Indicator (Optional)")]
    [SerializeField] private GameObject hitIndicator;
    [SerializeField] private float indicatorSize = 0.08f;
    [SerializeField] private Color indicatorColor = Color.yellow;

    [Header("Inertia")]
    [SerializeField] private bool useInertia = true;
    [SerializeField, Range(0f, 0.999f)] private float inertiaDamping = 0.92f;
    [SerializeField] private float minInertiaSpeedDegPerSec = 1.0f;

    // runtime
    private bool isDragging = false;
    private XRRayInteractor activeRay = null;
    private Vector3 lastControllerPos;
    private Vector3 angularVelDegPerSec = Vector3.zero;

    private void Awake()
    {
        if (rotatingContainer == null) rotatingContainer = transform;

        SetupCollider();
        SetupIndicator();
    }

    private void Update()
    {
        HandleInput();
        ApplyInertia();
    }

    // ===================== Input =====================

    private void HandleInput()
    {
        // ถ้ากำลังลากอยู่: เช็กแค่มือเดียว (กันหลุด/กระตุก)
        if (isDragging && activeRay != null)
        {
            UpdateIndicatorWhilePressed(activeRay);

            if (IsTriggerPressed(activeRay))
            {
                ContinueDragByHand(activeRay);
            }
            else
            {
                StopDrag();
            }
            return;
        }

        // ยังไม่ลาก: เช็กสองมือเพื่อหา "มือที่เริ่มกด"
        CheckStartOnRay(leftRay);
        CheckStartOnRay(rightRay);

        // ไม่กดอะไรแล้ว -> ซ่อน indicator
        if (!isDragging && activeRay == null && hitIndicator != null)
            hitIndicator.SetActive(false);
    }

    private void CheckStartOnRay(XRRayInteractor ray)
    {
        if (ray == null) return;

        bool pressed = IsTriggerPressed(ray);

        // อยากให้ "กด trigger แล้วขึ้นเหลืองค้าง" แม้ยังไม่เริ่มลาก
        if (pressed)
            UpdateIndicatorWhilePressed(ray);

        if (!isDragging && pressed)
        {
            // เริ่ม drag ได้ก็ต่อเมื่อเล็งโดน sphere
            if (RaycastHitsSphere(ray, out Vector3 hitPoint))
                StartDrag(ray, hitPoint);
        }
    }

    // อ่าน Trigger แบบชัวร์ด้วย ActionBasedController (ไม่ใช้ isSelectActive)
    private bool IsTriggerPressed(XRRayInteractor ray)
    {
        if (ray == null) return false;

        var controller = ray.GetComponentInParent<ActionBasedController>();
        if (controller == null) return false;

        InputAction action = controller.selectAction.action;
        if (action == null) return false;

        if (!action.enabled) action.Enable();

        return action.ReadValue<float>() > 0.2f;
    }

    // ===================== Raycast / Indicator =====================

    private bool RaycastHitsSphere(XRRayInteractor ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (ray == null || sphereCollider == null) return false;

        Transform rt = ray.transform;
        Ray r = new Ray(rt.position, rt.forward);

        if (Physics.Raycast(r, out RaycastHit hit, rayDistance, raycastLayerMask, QueryTriggerInteraction.Ignore))
        {
            // เคร่ง: ต้องชน collider ของ sphere เท่านั้น
            if (hit.collider == sphereCollider)
            {
                hitPoint = hit.point;
                return true;
            }
        }

        return false;
    }

    private void UpdateIndicatorWhilePressed(XRRayInteractor ray)
    {
        if (hitIndicator == null) return;

        if (RaycastHitsSphere(ray, out Vector3 hp))
        {
            hitIndicator.SetActive(true);
            hitIndicator.transform.position = hp;
        }
        else
        {
            // ถ้าอยากให้ "ค้างแม้หลุด sphere ชั่วคราว" -> คอมเมนต์บรรทัดนี้
            hitIndicator.SetActive(false);
        }
    }

    // ===================== Drag / Rotation =====================

    private void StartDrag(XRRayInteractor ray, Vector3 hitPoint)
    {
        isDragging = true;
        activeRay = ray;
        angularVelDegPerSec = Vector3.zero;

        lastControllerPos = ray.transform.position;

        if (hitIndicator != null)
        {
            hitIndicator.SetActive(true);
            hitIndicator.transform.position = hitPoint;
        }
    }

    private void ContinueDragByHand(XRRayInteractor ray)
    {
        if (rotatingContainer == null || ray == null) return;

        Vector3 curPos = ray.transform.position;
        Vector3 delta = curPos - lastControllerPos;
        lastControllerPos = curPos;

        if (delta.sqrMagnitude < 1e-8f) return;

        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 right = cam.right;
        Vector3 up = cam.up;

        float yaw = Vector3.Dot(delta, right) * positionToDegrees * rotationSpeed;
        float pitch = Vector3.Dot(delta, up) * positionToDegrees * rotationSpeed;

        if (invertX) yaw *= -1f;
        if (invertY) pitch *= -1f;

        Quaternion qYaw = Quaternion.AngleAxis(yaw, up);
        Quaternion qPitch = Quaternion.AngleAxis(-pitch, right);

        rotatingContainer.rotation = qYaw * qPitch * rotatingContainer.rotation;

        float dt = Mathf.Max(Time.deltaTime, 1e-5f);
        Vector3 approx = (up * yaw + right * (-pitch));
        angularVelDegPerSec = approx / dt;
    }

    private void StopDrag()
    {
        isDragging = false;
        activeRay = null;

        if (hitIndicator != null)
            hitIndicator.SetActive(false);
    }

    private void ApplyInertia()
    {
        if (!useInertia) { angularVelDegPerSec = Vector3.zero; return; }
        if (isDragging) return;

        float speed = angularVelDegPerSec.magnitude;
        if (speed < minInertiaSpeedDegPerSec)
        {
            angularVelDegPerSec = Vector3.zero;
            return;
        }

        Vector3 axis = angularVelDegPerSec.normalized;
        float angle = speed * Time.deltaTime;

        if (rotatingContainer != null)
            rotatingContainer.rotation = Quaternion.AngleAxis(angle, axis) * rotatingContainer.rotation;

        angularVelDegPerSec *= inertiaDamping;

        if (angularVelDegPerSec.magnitude < minInertiaSpeedDegPerSec)
            angularVelDegPerSec = Vector3.zero;
    }

    // ===================== Setup =====================

    private void SetupCollider()
    {
        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null)
                sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        sphereCollider.isTrigger = false;
        sphereCollider.radius = sphereRadius;
    }

    private void SetupIndicator()
    {
        if (hitIndicator != null) return;

        hitIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitIndicator.name = "HitIndicator (Auto)";
        hitIndicator.transform.SetParent(transform, true);
        hitIndicator.transform.localScale = Vector3.one * indicatorSize;

        var col = hitIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = hitIndicator.GetComponent<Renderer>();
        if (rend != null && rend.material != null)
            rend.material.color = indicatorColor;

        hitIndicator.SetActive(false);
    }

    // ===================== Public =====================

    public void ResetRotation()
    {
        if (rotatingContainer != null)
            rotatingContainer.localRotation = Quaternion.identity;

        angularVelDegPerSec = Vector3.zero;
    }

    public void SetSphereRadius(float radius)
    {
        sphereRadius = radius;
        if (sphereCollider != null)
            sphereCollider.radius = radius;
    }
}
