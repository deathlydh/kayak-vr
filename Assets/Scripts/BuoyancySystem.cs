using UnityEngine;
using Crest;

[RequireComponent(typeof(Rigidbody))]
public class BuoyancySystem : MonoBehaviour
{
    [System.Serializable]
    public struct BuoyancyPoint
    {
        public Vector3 localPosition;
        public float area;
    }

    public float waterDensity = 1000f;

    [Header("Damping")]
    public float baseVerticalDamping = 15f;
    public float baseHorizontalDamping = 3f;
    public float bowSternVerticalDamping = 25f;
    public float bowSternHorizontalDamping = 5f;
    public float bowSternThreshold = 0.3f;

    [Header("Buoyancy Points")]
    public BuoyancyPoint[] points;

    [Header("Crest Sampling")]
    public float minSpatialLength = 2f;

    [Header("VR Center of Mass")]
    public bool useVRCenterOfMass = true;
    public Transform vrCamera;
    public float horizontalInfluence = 0.2f;
    public float pitchInfluence = 0.07f;
    public float verticalInfluence = 0.03f;
    public Vector3 baseCenterOfMass = new Vector3(0f, -0.1f, 0f);

    [Header("Smoothing")]
    public float smoothTime = 0.25f;
    public float pitchAngularDamping = 0.92f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color aboveWaterColor = Color.yellow;
    public Color belowWaterColor = Color.cyan;
    public float gizmoRadius = 0.05f;

    private Rigidbody rb;
    private Vector3 smoothedHeadLocal;
    private Vector3 comVelocity;

    // Crest Sampling
    private SampleHeightHelper[] _heightHelpers;
    private SampleFlowHelper[] _flowHelpers;
    private bool _initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        smoothedHeadLocal = vrCamera ? vrCamera.localPosition : Vector3.zero;
    }

    void FixedUpdate()
    {
        if (OceanRenderer.Instance == null) return;

        // Lazy initialization
        if (!_initialized)
        {
            _heightHelpers = new SampleHeightHelper[points.Length];
            _flowHelpers = new SampleFlowHelper[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                _heightHelpers[i] = new SampleHeightHelper();
                _flowHelpers[i] = new SampleFlowHelper();
            }
            _initialized = true;
        }

        // Apply buoyancy and damping per point
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i].localPosition);
            _heightHelpers[i].Init(worldPos, minSpatialLength);

            if (_heightHelpers[i].Sample(out float waterHeight, out Vector3 waterNormal, out Vector3 waterVelocity))
            {
                float depth = waterHeight - worldPos.y;
                Vector3 force = Vector3.zero;

                // Buoyancy force
                if (depth > 0f)
                {
                    float buoyancyForce = waterDensity * -Physics.gravity.y * points[i].area * depth;
                    force = Vector3.up * buoyancyForce;
                }

                // Damping (relative to water motion)
                Vector3 vel = rb.GetPointVelocity(worldPos) - waterVelocity;
                bool isBowOrStern = Mathf.Abs(points[i].localPosition.z) > bowSternThreshold;
                float vertDamp = isBowOrStern ? bowSternVerticalDamping : baseVerticalDamping;
                float horizDamp = isBowOrStern ? bowSternHorizontalDamping : baseHorizontalDamping;

                Vector3 damping = new Vector3(
                    -vel.x * horizDamp * points[i].area,
                    -vel.y * vertDamp * points[i].area,
                    -vel.z * horizDamp * points[i].area
                );
                force += damping;

                rb.AddForceAtPosition(force, worldPos, ForceMode.Force);
            }
        }

        // Apply flow (current) to the whole kayak
        Vector3 totalFlow = Vector3.zero;
        int validFlowSamples = 0;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i].localPosition);
            _flowHelpers[i].Init(worldPos, minSpatialLength);
            if (_flowHelpers[i].Sample(out Vector2 flow2D))
            {
                totalFlow += new Vector3(flow2D.x, 0f, flow2D.y);
                validFlowSamples++;
            }
        }

        if (validFlowSamples > 0)
        {
            Vector3 avgFlow = totalFlow / validFlowSamples;
            // Tune this multiplier to balance realism and gameplay
            float flowForceMultiplier = 100f;
            rb.AddForce(avgFlow * flowForceMultiplier, ForceMode.Force);
        }

        // Dynamic center of mass from VR head
        if (useVRCenterOfMass && vrCamera != null)
        {
            smoothedHeadLocal = Vector3.SmoothDamp(smoothedHeadLocal, vrCamera.localPosition, ref comVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
            Vector3 com = new Vector3(
                Mathf.Clamp(smoothedHeadLocal.x * horizontalInfluence, -0.2f, 0.2f),
                baseCenterOfMass.y + smoothedHeadLocal.y * verticalInfluence,
                Mathf.Clamp(smoothedHeadLocal.z * pitchInfluence, -0.25f, 0.15f)
            );
            rb.centerOfMass = com;
        }
        else
        {
            rb.centerOfMass = baseCenterOfMass;
        }

        // Pitch damping
        if (pitchAngularDamping > 0f && pitchAngularDamping < 1f)
        {
            Vector3 angVel = rb.angularVelocity;
            angVel.x *= pitchAngularDamping;
            rb.angularVelocity = angVel;
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || points == null || OceanRenderer.Instance == null) return;

        float seaLevel = OceanRenderer.Instance.SeaLevel;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i].localPosition);
            float waterHeight = seaLevel;
            bool sampled = false;

            if (Application.isPlaying && _initialized)
            {
                _heightHelpers[i].Init(worldPos, minSpatialLength);
                if (_heightHelpers[i].Sample(out waterHeight, out _, out _))
                    sampled = true;
            }

            float depth = waterHeight - worldPos.y;
            Gizmos.color = depth > 0 ? belowWaterColor : aboveWaterColor;
            Gizmos.DrawSphere(worldPos, gizmoRadius);
            Gizmos.DrawLine(worldPos, new Vector3(worldPos.x, waterHeight, worldPos.z));
        }
    }
}