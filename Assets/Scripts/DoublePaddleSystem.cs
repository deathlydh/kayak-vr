using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DoublePaddleSystem : MonoBehaviour
{
    [System.Serializable]
    public class Blade
    {
        public Transform bladeRoot;     // корень лопасти (для определения угла)
        public Transform bladeTip;      // точка погружения
        public bool isLeft;
    }

    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("Paddle Object")]
    public Transform doublePaddle; // корневой объект весла (с левой и правой лопастью)

    [Header("Blades")]
    public Blade leftBlade;
    public Blade rightBlade;

    [Header("Paddle Physics")]
    public float waterLevel = 0f;
    public float bladeDepthThreshold = -0.05f;
    public float maxEffectiveSpeed = 2.5f;
    public float forceMultiplier = 60f;
    public float recoveryDrag = 0.5f;

    [Header("Angle Efficiency")]
    public float minEfficiency = 0.1f; // минимальная эффективность (даже "пером")
    public float maxEfficiency = 1.0f; // когда лопасть плоская

    private Rigidbody rb;
    private Vector3 lastLeftTip, lastRightTip;
    private bool leftInWater, rightInWater;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (leftBlade.bladeTip != null) lastLeftTip = leftBlade.bladeTip.position;
        if (rightBlade.bladeTip != null) lastRightTip = rightBlade.bladeTip.position;
    }

    void LateUpdate()
    {
        if (leftController == null || rightController == null || doublePaddle == null)
            return;
    
        // --- Положение: середина между контроллерами ---
        Vector3 avgPosition = (leftController.position + rightController.position) * 0.5f;
        doublePaddle.position = avgPosition;
    
        // --- Поворот: ось X весла направлена от левой руки к правой ---
        Vector3 forwardDir = rightController.position - leftController.position;
        if (forwardDir.magnitude < 0.01f)
        {
            // Если руки рядом — используем стандартное направление
            forwardDir = transform.forward; // или Vector3.right
        }
    
        // Ось Y — вверх (можно использовать мировой Up, или нормаль к воде)
        Vector3 upDir = Vector3.up;
    
        // Строим матрицу поворота: forward = X, up = Y
        Quaternion newRotation = Quaternion.LookRotation(forwardDir, upDir);
    
        // Применяем поворот
        doublePaddle.rotation = newRotation;
    }

    void FixedUpdate()
    {
        if (leftBlade.bladeTip != null)
            ProcessBlade(leftBlade, ref lastLeftTip, ref leftInWater);

        if (rightBlade.bladeTip != null)
            ProcessBlade(rightBlade, ref lastRightTip, ref rightInWater);
    }

    void ProcessBlade(Blade blade, ref Vector3 lastPos, ref bool inWater)
    {
        Transform tip = blade.bladeTip;
        Vector3 currentPos = tip.position;
        Vector3 velocity = (currentPos - lastPos) / Time.fixedDeltaTime;
        lastPos = currentPos;

        bool currentlyInWater = currentPos.y < waterLevel + bladeDepthThreshold;
        inWater = currentlyInWater;

        if (currentlyInWater)
        {
            // Направление движения в локальных координатах КАЯКА
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            bool isPullingBack = localVelocity.z < -0.1f; // движение назад

            // === Учёт угла лопасти ===
            // Нормаль к лопасти: считаем, что "вверх" лопасти = направление, перпендикулярное плоскости
            Vector3 bladeNormal = blade.bladeRoot.up; // или .forward — зависит от модели
            float angleEfficiency = CalculateBladeEfficiency(bladeNormal);

            if (isPullingBack)
            {
                float speed = Mathf.Clamp(-localVelocity.z, 0f, maxEffectiveSpeed);
                Vector3 force = transform.forward * speed * forceMultiplier * angleEfficiency;
                rb.AddForceAtPosition(force, currentPos, ForceMode.Force);
            }
            else if (localVelocity.z > 0.1f)
            {
                // Сопротивление при движении вперёд под водой
                float drag = localVelocity.z * forceMultiplier * 0.3f * angleEfficiency;
                Vector3 dragForce = -transform.forward * drag;
                rb.AddForceAtPosition(dragForce, currentPos, ForceMode.Force);
            }
        }
        else
        {
            // Воздушное сопротивление (слабое)
            if (velocity.magnitude > 0.2f)
            {
                Vector3 airDrag = -velocity * recoveryDrag;
                airDrag.y = 0f;
                rb.AddForceAtPosition(airDrag, currentPos, ForceMode.Force);
            }
        }
    }

    float CalculateBladeEfficiency(Vector3 bladeNormal)
    {
        // Эффективность = насколько лопасть "смотрит вверх"
        // Если bladeNormal ≈ (0,1,0) → плоскость горизонтальна → max эффективность
        // Если bladeNormal ≈ (0,0,1) → ребро → min эффективность
        float dot = Mathf.Abs(Vector3.Dot(bladeNormal, Vector3.up));
        return Mathf.Lerp(minEfficiency, maxEfficiency, dot * dot); // квадрат для более резкого перехода
    }

    // Gizmos
    void OnDrawGizmos()
    {
        DrawBladeGizmo(leftBlade);
        DrawBladeGizmo(rightBlade);
    }

    void DrawBladeGizmo(Blade blade)
    {
        if (blade.bladeTip == null) return;
        bool inWater = blade.bladeTip.position.y < waterLevel + bladeDepthThreshold;
        Gizmos.color = inWater ? Color.cyan : Color.yellow;
        Gizmos.DrawSphere(blade.bladeTip.position, 0.04f);
    }
}