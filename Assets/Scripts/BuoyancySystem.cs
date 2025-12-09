using System.Data;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyancySystem : MonoBehaviour
{
    [System.Serializable]
    public struct BuoyancyPoint
    {
        public Vector3 localPosition;
        public float area;
    }

    // === Плавучесть ===
    public float waterLevel = 0f;
    public float waterDensity = 1000f;

    [Header("Damping")]
    public float baseVerticalDamping = 15f;
    public float baseHorizontalDamping = 3f;
    public float bowSternVerticalDamping = 25f;
    public float bowSternHorizontalDamping = 5f;
    public float bowSternThreshold = 0.3f; // Z-расстояние от центра, чтобы считать точку носом/кормой

    [Header("Buoyancy Points")]
    public BuoyancyPoint[] points;

    // === Центр масс от VR ===
    [Header("VR Center of Mass")]
    public bool useVRCenterOfMass = true;
    public Transform vrCamera; // должен быть дочерним объектом каяка
    public float horizontalInfluence = 0.2f;   // для X (крен)
    public float pitchInfluence = 0.07f;       // для Z (тангаж) — намного меньше!
    public float verticalInfluence = 0.03f;
    public Vector3 baseCenterOfMass = new Vector3(0f, -0.1f, 0f);

    [Header("Smoothing")]
    public float smoothTime = 0.25f;
    public float pitchAngularDamping = 0.92f; // 0.9–0.95, гасит качание носом

    // === Отладка ===
    [Header("Debug")]
    public bool drawGizmos = true;
    public Color aboveWaterColor = Color.yellow;
    public Color belowWaterColor = Color.cyan;
    public float gizmoRadius = 0.05f;

    private Rigidbody rb;
    private Vector3 smoothedHeadLocal;
    private Vector3 comVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (vrCamera != null)
        {
            smoothedHeadLocal = vrCamera.localPosition;
        }
        else
        {
            smoothedHeadLocal = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        // --- Плавучесть ---
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i].localPosition);
            float depth = waterLevel - worldPos.y;

            Vector3 force = Vector3.zero;
            if (depth > 0f)
            {
                float buoyancyForce = waterDensity * -Physics.gravity.y * points[i].area * depth;
                force = Vector3.up * buoyancyForce;
            }

            Vector3 vel = rb.GetPointVelocity(worldPos);

            // Определяем, нос/корма ли это
            bool isBowOrStern = Mathf.Abs(points[i].localPosition.z) > bowSternThreshold;

            float vertDamp = isBowOrStern ? bowSternVerticalDamping : baseVerticalDamping;
            float horizDamp = isBowOrStern ? bowSternHorizontalDamping : baseHorizontalDamping;

            Vector3 dampingForce = new Vector3(
                -vel.x * horizDamp * points[i].area,
                -vel.y * vertDamp * points[i].area,
                -vel.z * horizDamp * points[i].area
            );

            rb.AddForceAtPosition(force + dampingForce, worldPos);
        }

        // --- Центр масс от VR ---
        if (useVRCenterOfMass && vrCamera != null)
        {
            Vector3 targetHeadLocal = vrCamera.localPosition;
            smoothedHeadLocal = Vector3.SmoothDamp(
                smoothedHeadLocal,
                targetHeadLocal,
                ref comVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.fixedDeltaTime
            );

            Vector3 com = new Vector3(
                Mathf.Clamp(smoothedHeadLocal.x * horizontalInfluence, -0.2f, 0.2f),
                baseCenterOfMass.y + smoothedHeadLocal.y * verticalInfluence,
                Mathf.Clamp(smoothedHeadLocal.z * pitchInfluence, -0.25f, 0.15f) // назад можно чуть больше
            );
            rb.centerOfMass = com;
        }
        else
        {
            rb.centerOfMass = baseCenterOfMass;
        }

        // --- Доп. гашение тангажа ---
        if (pitchAngularDamping > 0f && pitchAngularDamping < 1f)
        {
            Vector3 angVel = rb.angularVelocity;
            angVel.x *= pitchAngularDamping; // x = pitch
            rb.angularVelocity = angVel;
        }
    }

    // === Gizmos (без создания GameObject'ов!) ===
    void OnDrawGizmos()
    {
        if (!drawGizmos || points == null) return;

        // Уровень воды
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.left * 10f + Vector3.up * waterLevel, Vector3.right * 10f + Vector3.up * waterLevel);

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i].localPosition);
            float depth = waterLevel - worldPos.y;

            Gizmos.color = depth > 0f ? belowWaterColor : aboveWaterColor;
            Gizmos.DrawSphere(worldPos, gizmoRadius);

            Vector3 waterSurface = new Vector3(worldPos.x, waterLevel, worldPos.z);
            Gizmos.DrawLine(worldPos, waterSurface);
        }
    }
}