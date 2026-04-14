using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    private enum AttackState
    {
        Idle,
        ChargingMove,
        ChargedReady,
        SwingToMid,
        SwingToStrike,
        Return
    }

    [Header("Pose References")]
    public Transform weaponRoot;
    public Transform defaultPose;
    public Transform chargePose;
    public Transform midPose;
    public Transform strikePose;

    [Header("References")]
    [SerializeField] private Transform hitOrigin;
    [SerializeField] private CharacterController characterController;

    [Header("Input")]
    [SerializeField] private int attackMouseButton = 1;
    [SerializeField] private float heavyChargeTime = 1f;

    [Header("Timing")]
    [SerializeField] private float chargeMoveDuration = 0.18f;

    [Header("Swing Split")]
    [SerializeField, Range(0.1f, 0.9f)] private float swingMidRatio = 0.45f;

    [Header("Light Attack")]
    [SerializeField] private float lightSwingDuration = 0.16f;
    [SerializeField] private float lightReturnDuration = 0.12f;
    [SerializeField] private float lightHitRange = 2.0f;
    [SerializeField] private float lightHitRadius = 0.25f;
    [SerializeField] private int lightDamage = 10;
    [SerializeField, Range(0f, 1f)] private float lightHitMomentNormalized = 0.55f;

    [Header("Heavy Attack")]
    [SerializeField] private float heavySwingDuration = 0.20f;
    [SerializeField] private float heavyReturnDuration = 0.16f;
    [SerializeField] private float heavyHitRange = 2.5f;
    [SerializeField] private float heavyHitRadius = 0.35f;
    [SerializeField] private int heavyDamage = 25;
    [SerializeField, Range(0f, 1f)] private float heavyHitMomentNormalized = 0.62f;

    [Header("Charged Shake")]
    [SerializeField] private float chargedShakePosAmount = 0.01f;
    [SerializeField] private float chargedShakeRotAmount = 1.5f;
    [SerializeField] private float chargedShakeFrequency = 32f;

    [Header("Idle / Move Sway")]
    [SerializeField] private float swaySmooth = 10f;
    [SerializeField] private float moveSwayAmount = 0.035f;
    [SerializeField] private float moveSwayRotationAmount = 3f;
    [SerializeField] private float swayBobFrequency = 7f;
    [SerializeField] private float idleReturnSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool drawHitDebug = true;
    [SerializeField] private bool debugLog = false;

    private AttackState state = AttackState.Idle;

    private Vector3 defaultLocalPos;
    private Vector3 defaultLocalEuler;

    private bool isHoldingAttack;
    private float holdTimer;

    private float stateTimer;
    private bool hitTriggeredThisSwing;

    private float currentSwingDuration;
    private float currentReturnDuration;
    private float currentHitMoment;
    private float currentHitRange;
    private float currentHitRadius;
    private int currentDamage;

    private float swayTime;

    private float chargedNoiseSeedX;
    private float chargedNoiseSeedY;
    private float chargedNoiseSeedZ;

    private void Awake()
    {
        if (weaponRoot == null || defaultPose == null || chargePose == null || midPose == null || strikePose == null)
        {
            Debug.LogError("MeleeAttack: 请把 weaponRoot / defaultPose / chargePose / midPose / strikePose 都指定。", this);
            enabled = false;
            return;
        }

        if (hitOrigin == null)
            hitOrigin = Camera.main != null ? Camera.main.transform : transform;

        defaultLocalPos = defaultPose.localPosition;
        defaultLocalEuler = defaultPose.localEulerAngles;

        weaponRoot.localPosition = defaultLocalPos;
        weaponRoot.localEulerAngles = defaultLocalEuler;

        chargedNoiseSeedX = Random.Range(0f, 100f);
        chargedNoiseSeedY = Random.Range(100f, 200f);
        chargedNoiseSeedZ = Random.Range(200f, 300f);
    }

    private void Update()
    {
        HandleInput();

        switch (state)
        {
            case AttackState.Idle:
                UpdateIdle();
                break;

            case AttackState.ChargingMove:
                UpdateChargingMove();
                break;

            case AttackState.ChargedReady:
                UpdateChargedReady();
                break;

            case AttackState.SwingToMid:
                UpdateSwingToMid();
                break;

            case AttackState.SwingToStrike:
                UpdateSwingToStrike();
                break;

            case AttackState.Return:
                UpdateReturn();
                break;
        }
    }

    private void HandleInput()
    {
        if (state == AttackState.SwingToMid || state == AttackState.SwingToStrike || state == AttackState.Return)
            return;

        if (Input.GetMouseButtonDown(attackMouseButton))
        {
            isHoldingAttack = true;
            holdTimer = 0f;
            stateTimer = 0f;
            state = AttackState.ChargingMove;

            if (debugLog) Debug.Log("Melee: Start Charging");
        }

        if (isHoldingAttack)
        {
            holdTimer += Time.deltaTime;
        }

        if (Input.GetMouseButtonUp(attackMouseButton))
        {
            if (!isHoldingAttack)
                return;

            isHoldingAttack = false;
            StartSwing(holdTimer >= heavyChargeTime);
        }
    }

    private void UpdateIdle()
    {
        ApplyIdleAndMoveSway(defaultLocalPos, Quaternion.Euler(defaultLocalEuler), false);
    }

    private void UpdateChargingMove()
    {
        stateTimer += Time.deltaTime;

        float t = Mathf.Clamp01(stateTimer / chargeMoveDuration);
        t = EaseOutCubic(t);

        Vector3 chargePos = chargePose.localPosition;
        Vector3 chargeEuler = GetContinuousTargetEuler(defaultLocalEuler, chargePose.localEulerAngles);

        Vector3 basePos = Vector3.Lerp(defaultLocalPos, chargePos, t);
        Vector3 baseEuler = LerpEulerUnclamped(defaultLocalEuler, chargeEuler, t);

        bool fullyCharged = holdTimer >= heavyChargeTime;
        ApplyIdleAndMoveSway(basePos, Quaternion.Euler(baseEuler), fullyCharged && t >= 0.999f);

        if (stateTimer >= chargeMoveDuration)
        {
            state = AttackState.ChargedReady;
            stateTimer = 0f;
        }
    }

    private void UpdateChargedReady()
    {
        Vector3 chargePos = chargePose.localPosition;
        Quaternion chargeRot = Quaternion.Euler(chargePose.localEulerAngles);

        bool fullyCharged = holdTimer >= heavyChargeTime;
        ApplyIdleAndMoveSway(chargePos, chargeRot, fullyCharged);
    }

    private void StartSwing(bool heavy)
    {
        hitTriggeredThisSwing = false;
        state = AttackState.SwingToMid;
        stateTimer = 0f;

        weaponRoot.localPosition = chargePose.localPosition;
        weaponRoot.localEulerAngles = chargePose.localEulerAngles;

        if (heavy)
        {
            currentSwingDuration = heavySwingDuration;
            currentReturnDuration = heavyReturnDuration;
            currentHitMoment = heavyHitMomentNormalized;
            currentHitRange = heavyHitRange;
            currentHitRadius = heavyHitRadius;
            currentDamage = heavyDamage;

            if (debugLog) Debug.Log("Melee: Heavy Swing");
        }
        else
        {
            currentSwingDuration = lightSwingDuration;
            currentReturnDuration = lightReturnDuration;
            currentHitMoment = lightHitMomentNormalized;
            currentHitRange = lightHitRange;
            currentHitRadius = lightHitRadius;
            currentDamage = lightDamage;

            if (debugLog) Debug.Log("Melee: Light Swing");
        }
    }

    private void UpdateSwingToMid()
    {
        stateTimer += Time.deltaTime;

        float firstDuration = Mathf.Max(0.0001f, currentSwingDuration * swingMidRatio);
        float t = Mathf.Clamp01(stateTimer / firstDuration);
        float eased = EaseOutCubic(t);

        Vector3 startPos = chargePose.localPosition;
        Vector3 startEuler = chargePose.localEulerAngles;

        Vector3 targetPos = midPose.localPosition;
        Vector3 targetEuler = GetContinuousTargetEuler(startEuler, midPose.localEulerAngles);

        weaponRoot.localPosition = Vector3.Lerp(startPos, targetPos, eased);
        weaponRoot.localEulerAngles = LerpEulerUnclamped(startEuler, targetEuler, eased);

        if (t >= 1f)
        {
            state = AttackState.SwingToStrike;
            stateTimer = 0f;
        }
    }

    private void UpdateSwingToStrike()
    {
        stateTimer += Time.deltaTime;

        float secondDuration = Mathf.Max(0.0001f, currentSwingDuration * (1f - swingMidRatio));
        float t = Mathf.Clamp01(stateTimer / secondDuration);
        float eased = EaseOutQuart(t);

        Vector3 startPos = midPose.localPosition;
        Vector3 startEuler = midPose.localEulerAngles;

        Vector3 targetPos = strikePose.localPosition;
        Vector3 targetEuler = GetContinuousTargetEuler(startEuler, strikePose.localEulerAngles);

        weaponRoot.localPosition = Vector3.Lerp(startPos, targetPos, eased);
        weaponRoot.localEulerAngles = LerpEulerUnclamped(startEuler, targetEuler, eased);

        float totalT = swingMidRatio + t * (1f - swingMidRatio);

        if (!hitTriggeredThisSwing && totalT >= currentHitMoment)
        {
            hitTriggeredThisSwing = true;
            DoHit();
        }

        if (t >= 1f)
        {
            state = AttackState.Return;
            stateTimer = 0f;
        }
    }

    private void UpdateReturn()
    {
        stateTimer += Time.deltaTime;

        Vector3 startPos = strikePose.localPosition;
        Vector3 startEuler = strikePose.localEulerAngles;

        float t = Mathf.Clamp01(stateTimer / currentReturnDuration);
        float eased = EaseInOutCubic(t);

        Vector3 targetEuler = GetContinuousTargetEuler(startEuler, defaultLocalEuler);

        weaponRoot.localPosition = Vector3.Lerp(startPos, defaultLocalPos, eased);
        weaponRoot.localEulerAngles = LerpEulerUnclamped(startEuler, targetEuler, eased);

        if (t >= 1f)
        {
            state = AttackState.Idle;
            stateTimer = 0f;
            holdTimer = 0f;
            weaponRoot.localPosition = defaultLocalPos;
            weaponRoot.localEulerAngles = defaultLocalEuler;
        }
    }

    private void DoHit()
    {
        Vector3 origin = hitOrigin.position;
        Vector3 direction = hitOrigin.forward;

        if (Physics.SphereCast(
                origin,
                currentHitRadius,
                direction,
                out RaycastHit hit,
                currentHitRange,
                hitMask,
                triggerInteraction))
        {
            if (debugLog)
                Debug.Log($"Melee Hit: {hit.collider.name}");

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(currentDamage);
            }
        }
        else
        {
            if (debugLog)
                Debug.Log("Melee Miss");
        }
    }

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private void ApplyIdleAndMoveSway(Vector3 basePos, Quaternion baseRot, bool applyChargedShake)
    {
        Vector3 targetPos = basePos;
        Quaternion targetRot = baseRot;

        bool isMoving = IsPlayerMoving();

        if (isMoving)
        {
            swayTime += Time.deltaTime * swayBobFrequency;

            float sinX = Mathf.Sin(swayTime);
            float sinY = Mathf.Sin(swayTime * 2f);

            Vector3 swayPos = new Vector3(
                sinX * moveSwayAmount,
                Mathf.Abs(sinY) * moveSwayAmount * 0.7f,
                0f
            );

            Vector3 swayRot = new Vector3(
                sinY * moveSwayRotationAmount,
                sinX * moveSwayRotationAmount,
                sinX * moveSwayRotationAmount * 0.5f
            );

            targetPos += swayPos;
            targetRot *= Quaternion.Euler(swayRot);
        }
        else
        {
            swayTime = Mathf.Lerp(swayTime, 0f, Time.deltaTime * idleReturnSpeed);
        }

        if (applyChargedShake)
        {
            float time = Time.time * chargedShakeFrequency;

            float px = (Mathf.PerlinNoise(chargedNoiseSeedX, time) - 0.5f) * 2f;
            float py = (Mathf.PerlinNoise(chargedNoiseSeedY, time) - 0.5f) * 2f;
            float pz = (Mathf.PerlinNoise(chargedNoiseSeedZ, time) - 0.5f) * 2f;

            Vector3 shakePos = new Vector3(px, py, 0f) * chargedShakePosAmount;
            Vector3 shakeRot = new Vector3(py, px, pz) * chargedShakeRotAmount;

            targetPos += shakePos;
            targetRot *= Quaternion.Euler(shakeRot);
        }

        weaponRoot.localPosition = Vector3.Lerp(
            weaponRoot.localPosition,
            targetPos,
            Time.deltaTime * swaySmooth
        );

        weaponRoot.localRotation = Quaternion.Slerp(
            weaponRoot.localRotation,
            targetRot,
            Time.deltaTime * swaySmooth
        );
    }

    private Vector3 GetContinuousTargetEuler(Vector3 from, Vector3 to)
    {
        return new Vector3(
            MakeContinuousAngle(from.x, to.x),
            MakeContinuousAngle(from.y, to.y),
            MakeContinuousAngle(from.z, to.z)
        );
    }

    private float MakeContinuousAngle(float from, float to)
    {
        float delta = to - from;

        while (delta > 180f)
        {
            to -= 360f;
            delta = to - from;
        }

        while (delta < -180f)
        {
            to += 360f;
            delta = to - from;
        }

        return to;
    }

    private Vector3 LerpEulerUnclamped(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(
            Mathf.Lerp(from.x, to.x, t),
            Mathf.Lerp(from.y, to.y, t),
            Mathf.Lerp(from.z, to.z, t)
        );
    }

    private bool IsPlayerMoving()
    {
        if (characterController != null)
        {
            Vector3 horizontalVel = characterController.velocity;
            horizontalVel.y = 0f;
            return horizontalVel.sqrMagnitude > 0.01f;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 move = new Vector2(h, v);
        return move.sqrMagnitude > 0.01f;
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private float EaseInOutCubic(float x)
    {
        return x < 0.5f
            ? 4f * x * x * x
            : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }

    private float EaseOutQuart(float x)
    {
        return 1f - Mathf.Pow(1f - x, 4f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawHitDebug || hitOrigin == null)
            return;

        float gizmoRadius = Application.isPlaying ? currentHitRadius : lightHitRadius;
        float gizmoRange = Application.isPlaying ? currentHitRange : lightHitRange;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hitOrigin.position, gizmoRadius);

        Vector3 end = hitOrigin.position + hitOrigin.forward * gizmoRange;
        Gizmos.DrawLine(hitOrigin.position, end);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(end, gizmoRadius);
    }
}