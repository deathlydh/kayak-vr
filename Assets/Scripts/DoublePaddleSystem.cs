using UnityEngine;
using Crest;

[RequireComponent(typeof(Rigidbody))]
public class DoublePaddleSystem : MonoBehaviour
{
    [System.Serializable]
    public class Blade
    {
        public Transform bladeRoot;
        public Transform bladeTip;
    }

    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("Paddle")]
    public Transform doublePaddle;

    [Header("Blades")]
    public Blade leftBlade;
    public Blade rightBlade;

    [Header("Physics")]
    public float bladeDepthThreshold = -0.05f;
    public float maxEffectiveSpeed = 2.5f;
    public float forceMultiplier = 60f;
    public float recoveryDrag = 0.5f;
    public float minEfficiency = 0.1f;
    public float maxEfficiency = 1.0f;

    [Header("Crest Sampling")]
    public float minSpatialLength = 1f;

    private Rigidbody rb;
    private Vector3 lastLeftTip, lastRightTip;
    private bool leftInWater, rightInWater;

    private SampleHeightHelper _leftHeightHelper, _rightHeightHelper;
    private SampleFlowHelper _leftFlowHelper, _rightFlowHelper;
    private bool _initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (leftBlade?.bladeTip != null) lastLeftTip = leftBlade.bladeTip.position;
        if (rightBlade?.bladeTip != null) lastRightTip = rightBlade.bladeTip.position;
    }

    void LateUpdate()
    {
        if (leftController == null || rightController == null || doublePaddle == null) return;

        doublePaddle.position = (leftController.position + rightController.position) * 0.5f;
        Vector3 forward = rightController.position - leftController.position;
        if (forward.magnitude < 0.01f) forward = transform.forward;
        doublePaddle.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    void FixedUpdate()
    {
        if (OceanRenderer.Instance == null) return;

        if (!_initialized)
        {
            _leftHeightHelper = new SampleHeightHelper();
            _rightHeightHelper = new SampleHeightHelper();
            _leftFlowHelper = new SampleFlowHelper();
            _rightFlowHelper = new SampleFlowHelper();
            _initialized = true;
        }

        if (leftBlade?.bladeTip != null)
            ProcessBlade(leftBlade, ref lastLeftTip, ref leftInWater, _leftHeightHelper, _leftFlowHelper);

        if (rightBlade?.bladeTip != null)
            ProcessBlade(rightBlade, ref lastRightTip, ref rightInWater, _rightHeightHelper, _rightFlowHelper);
    }

    void ProcessBlade(Blade blade, ref Vector3 lastPos, ref bool inWater, SampleHeightHelper heightHelper, SampleFlowHelper flowHelper)
    {
        Transform tip = blade.bladeTip;
        Vector3 current = tip.position;
        Vector3 velocity = (current - lastPos) / Time.fixedDeltaTime;
        lastPos = current;

        heightHelper.Init(current, minSpatialLength);
        if (!heightHelper.Sample(out float waterHeight, out _, out Vector3 waterVelocity)) return;

        // Add flow to water velocity
        flowHelper.Init(current, minSpatialLength);
        if (flowHelper.Sample(out Vector2 flow2D))
        {
            waterVelocity += new Vector3(flow2D.x, 0f, flow2D.y);
        }

        bool currentlyInWater = current.y < waterHeight + bladeDepthThreshold;
        inWater = currentlyInWater;

        if (currentlyInWater)
        {
            Vector3 relVel = velocity - waterVelocity;
            Vector3 localVel = transform.InverseTransformDirection(relVel);
            float angleEff = CalculateBladeEfficiency(blade.bladeRoot.up);

            if (localVel.z < -0.1f) // Pulling back (effective stroke)
            {
                float speed = Mathf.Clamp(-localVel.z, 0f, maxEffectiveSpeed);
                Vector3 force = transform.forward * speed * forceMultiplier * angleEff;
                rb.AddForceAtPosition(force, current, ForceMode.Force);
            }
            else if (localVel.z > 0.1f) // Pushing forward (drag when submerged)
            {
                float drag = localVel.z * forceMultiplier * 0.3f * angleEff;
                rb.AddForceAtPosition(-transform.forward * drag, current, ForceMode.Force);
            }
        }
        else
        {
            // Air drag
            if (velocity.magnitude > 0.2f)
            {
                Vector3 airDrag = -velocity * recoveryDrag;
                airDrag.y = 0f;
                rb.AddForceAtPosition(airDrag, current, ForceMode.Force);
            }
        }
    }

    float CalculateBladeEfficiency(Vector3 normal)
    {
        float dot = Mathf.Abs(Vector3.Dot(normal, Vector3.up));
        return Mathf.Lerp(minEfficiency, maxEfficiency, dot * dot);
    }

    void OnDrawGizmos()
    {
        if (OceanRenderer.Instance == null) return;
        DrawBlade(leftBlade, _leftHeightHelper);
        DrawBlade(rightBlade, _rightHeightHelper);
    }

    void DrawBlade(Blade blade, SampleHeightHelper helper)
    {
        if (blade?.bladeTip == null) return;

        Vector3 pos = blade.bladeTip.position;
        float waterHeight = OceanRenderer.Instance.SeaLevel;
        bool inWater = false;

        if (Application.isPlaying && helper != null)
        {
            helper.Init(pos, minSpatialLength);
            if (helper.Sample(out waterHeight, out _, out _))
            {
                inWater = pos.y < waterHeight + bladeDepthThreshold;
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
                Gizmos.DrawLine(pos, new Vector3(pos.x, waterHeight, pos.z));
            }
        }
        else
        {
            inWater = pos.y < waterHeight + bladeDepthThreshold;
        }

        Gizmos.color = inWater ? Color.cyan : Color.yellow;
        Gizmos.DrawSphere(pos, 0.04f);
    }
}