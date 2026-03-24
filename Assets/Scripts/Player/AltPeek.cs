using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(200)]
public class AltPeek : MonoBehaviour
{
    [Header("Refs")]
    public Transform peekPivot;                 // 只移动/旋转这个（推荐 Player/PeekPivot）
    public FpsController controller;
    public Transform cameraRoot;

    [Header("Input")]
    public Key peekModifierKey = Key.LeftAlt;

    [Header("Rules")]
    public bool requireGrounded = true;
    public bool lockJumpInPeek = true;

    [Header("Camera Collision")]
    public bool preventCameraClipping = true;
    public float cameraProbeRadius = 0.12f;     // 相机“头部”半径
    public float cameraClipMargin = 0.02f;      // 离墙保留一点缝
    public LayerMask cameraCollisionMask = ~0;  // 只勾环境，不勾Player
    public int sweepSteps = 6;           // 4~10，越大越稳但更耗
    public float sweepMinStep = 0.02f;   // 每段最小距离（米），防止超小步浪费


    [Header("Max Offsets (peekPivot local space)")]
    public float maxLeft = 0.35f;
    public float maxRight = 0.35f;
    public float maxUp = 0.25f;      // W 踮脚
    public float maxDown = 0.30f;    // S 下伏

    [Tooltip("W/S 时额外前后位移，让踮脚更像探头、下伏更像压低")]
    public float forwardWhenUp = 0.08f;   // W 额外前移
    public float backWhenDown = 0.06f;    // S 额外后移（用负Z）

    [Range(0f, 1f)]
    public float holdFraction = 0.35f;

    [Header("Roll (camera tilt)")]
    public bool enableRoll = true;
    public float maxRollDegrees = 5f;     // 左右偏身最大倾斜角
    public float rollHoldFraction = 0.6f; // 松开方向键但ALT仍按着时，Roll保留比例

    [Header("Smoothing")]
    public float engageSmoothTime = 0.06f;
    public float holdSmoothTime = 0.08f;
    public float returnSmoothTime = 0.10f;

    Vector3 pivotBaseLocalPos;
    Quaternion pivotBaseLocalRot;

    Vector3 currentOffset;
    Vector3 offsetVel;

    float currentRoll;
    float rollVel;

    Vector3 lastNonZeroDir = Vector3.zero; // 记录最后探身方向（用于保持）
    float lastLateralSign = 0f;            // 记录最后左右方向（用于保持 roll）

    SphereCollider probe;
    Transform probeT;

    void Awake()
    {
        int playerLayer = gameObject.layer;
        cameraCollisionMask &= ~(1 << playerLayer);

        if (!controller) controller = GetComponent<FpsController>();

        if (!peekPivot)
        {
            var t = transform.Find("PeekPivot");
            if (t) peekPivot = t;
        }

        if (peekPivot)
        {
            pivotBaseLocalPos = peekPivot.localPosition;
            pivotBaseLocalRot = peekPivot.localRotation;
        }
        // --- Penetration probe (for ComputePenetration) ---
        var probeGO = new GameObject("PeekCameraProbe");
        probeGO.hideFlags = HideFlags.HideInHierarchy;
        probeT = probeGO.transform;
        probeT.SetParent(null); // 不挂父级，避免缩放影响
        probe = probeGO.AddComponent<SphereCollider>();
        probe.isTrigger = true;
        probe.radius = cameraProbeRadius;
    }

    void OnEnable()
    {
        if (peekPivot)
        {
            pivotBaseLocalPos = peekPivot.localPosition;
            pivotBaseLocalRot = peekPivot.localRotation;
        }
    }

    void OnDisable()
    {
        ClearLocksAndReset();
    }

    void LateUpdate()
    {
        if (!peekPivot) return;

        bool altHeld = Keyboard.current != null &&
                       Keyboard.current[peekModifierKey].isPressed;

        bool grounded = controller != null ? controller.IsGrounded : true;
        bool peekActive = altHeld && (!requireGrounded || grounded);

        if (controller)
        {
            controller.externalMovementLock = peekActive;
            controller.externalJumpLock = peekActive && lockJumpInPeek;
        }

        // === 计算目标 offset & roll ===
        Vector3 targetOffset = Vector3.zero;
        float targetRoll = 0f;
        float smoothTime = returnSmoothTime;

        if (peekActive)
        {
            Vector3 dir = Vector3.zero;

            if (Keyboard.current.aKey.isPressed) dir.x -= 1f;
            if (Keyboard.current.dKey.isPressed) dir.x += 1f;

            bool isProne = controller != null && controller.IsProne;
            if (isProne && lastNonZeroDir.y < 0f)
            {
                lastNonZeroDir.y = 0f; // 清掉向下保持
            }

            if (Keyboard.current.wKey.isPressed) dir.y += 1f;

            // Prone 时禁用向下 peek（S）
            if (!isProne)
            {
                if (Keyboard.current.sKey.isPressed) dir.y -= 1f;
            }


            bool hasInput = dir != Vector3.zero;

            if (hasInput)
            {
                dir.Normalize();
                lastNonZeroDir = dir;
                smoothTime = engageSmoothTime;

                // 记录最后左右方向，用于 roll 保持
                if (Mathf.Abs(dir.x) > 0.001f)
                    lastLateralSign = Mathf.Sign(dir.x);

                targetOffset = CalculateMaxOffset(dir);
                targetOffset = NormalizeDiagonalOffset(targetOffset);

                // W/S 前后补偿（更像探头/压低）
                // 用 local Z：前 = +Z，后 = -Z
                if (dir.y > 0f) targetOffset.z += forwardWhenUp;
                else if (dir.y < 0f) targetOffset.z -= backWhenDown;

                // roll（左右偏身时）
                if (enableRoll)
                    targetRoll = -Mathf.Clamp(dir.x, -1f, 1f) * maxRollDegrees; // 左偏镜头左倾：负号更自然
            }
            else
            {
                // 保持：回到一个较小的 offset + 小幅 roll
                smoothTime = holdSmoothTime;

                targetOffset = CalculateMaxOffset(lastNonZeroDir) * holdFraction;

                // 保持时也保留前后补偿（取最后方向的 y）
                if (lastNonZeroDir.y > 0f) targetOffset.z += forwardWhenUp * holdFraction;
                else if (lastNonZeroDir.y < 0f) targetOffset.z -= backWhenDown * holdFraction;

                if (enableRoll && Mathf.Abs(lastLateralSign) > 0.001f)
                    targetRoll = -(lastLateralSign * maxRollDegrees) * rollHoldFraction;
            }
        }
        else
        {
            // 退出 peek：回正
            smoothTime = returnSmoothTime;
            lastNonZeroDir = Vector3.zero;
            lastLateralSign = 0f;
            targetOffset = Vector3.zero;
            targetRoll = 0f;
        }

        // === 平滑应用 offset ===
        Vector3 desiredTarget = targetOffset;

        // 1) 先约束目标（防止左右进墙）
        if (peekActive && preventCameraClipping)
            desiredTarget = SweepOffsetSegmented(currentOffset, desiredTarget);

        // 2) 平滑追目标
        Vector3 before = currentOffset;
        currentOffset = Vector3.SmoothDamp(currentOffset, desiredTarget, ref offsetVel, smoothTime);

        if (peekActive && preventCameraClipping)
        {
            currentOffset = ResolveOffsetBySweptAxes(before, currentOffset);
        }

        peekPivot.localPosition = pivotBaseLocalPos + currentOffset;

        // === 平滑应用 roll ===
        currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVel, smoothTime);
        peekPivot.localRotation = pivotBaseLocalRot * Quaternion.Euler(0f, 0f, currentRoll);
    }

    void ClearLocksAndReset()
    {
        if (controller)
        {
            controller.externalMovementLock = false;
            controller.externalJumpLock = false;
        }

        if (peekPivot)
        {
            peekPivot.localPosition = pivotBaseLocalPos;
            peekPivot.localRotation = pivotBaseLocalRot;
        }

        currentOffset = Vector3.zero;
        offsetVel = Vector3.zero;
        currentRoll = 0f;
        rollVel = 0f;
        lastNonZeroDir = Vector3.zero;
        lastLateralSign = 0f;
    }
    Vector3 SweepOffsetSegmented(Vector3 fromOffset, Vector3 toOffset)
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (!cam) return toOffset;

        // 相机相对 peekPivot 的世界偏移（固定）
        Vector3 camRelWorld = cam.transform.position - peekPivot.position;

        // 计算 from/to 的相机世界位置
        Vector3 fromPivotWorld = peekPivot.parent
            ? peekPivot.parent.TransformPoint(pivotBaseLocalPos + fromOffset)
            : (pivotBaseLocalPos + fromOffset);
        Vector3 toPivotWorld = peekPivot.parent
            ? peekPivot.parent.TransformPoint(pivotBaseLocalPos + toOffset)
            : (pivotBaseLocalPos + toOffset);

        Vector3 fromCamWorld = fromPivotWorld + camRelWorld;
        Vector3 toCamWorld = toPivotWorld + camRelWorld;

        Vector3 delta = toCamWorld - fromCamWorld;
        float dist = delta.magnitude;
        if (dist < 1e-5f) return toOffset;

        // 按距离决定步数：至少 sweepSteps，但也别让每步太小
        int steps = Mathf.Max(1, sweepSteps);
        float stepLen = dist / steps;
        if (stepLen < sweepMinStep)
            steps = Mathf.Max(1, Mathf.CeilToInt(dist / sweepMinStep));

        Vector3 currentCam = fromCamWorld;

        for (int i = 0; i < steps; i++)
        {
            Vector3 nextCam = Vector3.Lerp(fromCamWorld, toCamWorld, (i + 1f) / steps);
            Vector3 seg = nextCam - currentCam;
            float segDist = seg.magnitude;
            if (segDist < 1e-5f) continue;

            Vector3 dir = seg / segDist;

            if (Physics.SphereCast(currentCam, cameraProbeRadius, dir, out RaycastHit hit,
                segDist, cameraCollisionMask, QueryTriggerInteraction.Ignore))
            {
                float safe = Mathf.Max(0f, hit.distance - cameraClipMargin);

                Vector3 safeCam = currentCam + dir * safe;

                // safeCam -> offset
                Vector3 safePivotWorld = safeCam - camRelWorld;
                Vector3 safePivotLocal = peekPivot.parent
                    ? peekPivot.parent.InverseTransformPoint(safePivotWorld)
                    : safePivotWorld;

                return safePivotLocal - pivotBaseLocalPos; // ✅ 直接停下，不再继续走，绝不会穿
            }

            currentCam = nextCam;
        }

        return toOffset;
    }

    Vector3 CalculateMaxOffset(Vector3 dir)
    {
        Vector3 result = Vector3.zero;

        if (dir.x < 0f) result.x = -maxLeft;
        else if (dir.x > 0f) result.x = maxRight;

        if (dir.y > 0f) result.y = maxUp;
        else if (dir.y < 0f) result.y = -maxDown;

        return result;

    }
    Vector3 NormalizeDiagonalOffset(Vector3 o)
    {
        // 把 X/Y 的组合限制在一个“椭圆”里： (x/maxX)^2 + (y/maxY)^2 <= 1
        float maxX = o.x >= 0 ? maxRight : maxLeft;
        float maxY = o.y >= 0 ? maxUp : maxDown;

        maxX = Mathf.Max(0.0001f, maxX);
        maxY = Mathf.Max(0.0001f, maxY);

        float nx = o.x / maxX;
        float ny = o.y / maxY;

        float len2 = nx * nx + ny * ny;
        if (len2 > 1f)
        {
            float k = 1f / Mathf.Sqrt(len2);
            o.x *= k;
            o.y *= k;
        }
        return o;
    }
    Vector3 ResolveOffsetBySweptAxes(Vector3 prevOffset, Vector3 newOffset)
    {
        if (!cameraRoot) return newOffset;

        // 我们从 prevOffset 分两步走到 newOffset：先 X，再 Y（你也可以换成先Y再X）
        Vector3 step1 = prevOffset;
        step1.x = newOffset.x;

        Vector3 resolved = prevOffset;
        resolved = SweepTo(resolved, step1);

        Vector3 step2 = resolved;
        step2.y = newOffset.y;

        resolved = SweepTo(resolved, step2);

        // 可选：如果你也用到了 Z（forwardWhenUp/backWhenDown），最后再 sweep 一次 Z
        step2 = resolved;
        step2.z = newOffset.z;
        resolved = SweepTo(resolved, step2);

        return resolved;
    }
    Vector3 SweepTo(Vector3 fromOffset, Vector3 toOffset)
    {
        if (!cameraRoot) return toOffset;

        // 计算相机（cameraRoot）从哪到哪
        Vector3 fromWorld = GetCameraWorldAtOffset(fromOffset);
        Vector3 toWorld = GetCameraWorldAtOffset(toOffset);

        Vector3 delta = toWorld - fromWorld;
        float dist = delta.magnitude;
        if (dist < 1e-5f) return toOffset;

        Vector3 dir = delta / dist;

        // SphereCast 沿路径扫
        if (Physics.SphereCast(fromWorld, cameraProbeRadius, dir, out RaycastHit hit,
            dist, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Max(0f, hit.distance - cameraClipMargin);

            Vector3 safeWorld = fromWorld + dir * safeDist;

            // 把 safeWorld 转回 offset
            return OffsetFromCameraWorld(safeWorld);
        }

        return toOffset;
    }
    Vector3 GetCameraWorldAtOffset(Vector3 offsetLocal)
    {
        // peekPivot 的目标世界位置
        Vector3 pivotWorld = peekPivot.parent
            ? peekPivot.parent.TransformPoint(pivotBaseLocalPos + offsetLocal)
            : (pivotBaseLocalPos + offsetLocal);

        // cameraRoot 相对 peekPivot 的世界偏移（固定）
        Vector3 camRelWorld = cameraRoot.position - peekPivot.position;
        return pivotWorld + camRelWorld;
    }

    Vector3 OffsetFromCameraWorld(Vector3 camWorld)
    {
        Vector3 camRelWorld = cameraRoot.position - peekPivot.position;
        Vector3 pivotWorld = camWorld - camRelWorld;

        Vector3 pivotLocal = peekPivot.parent
            ? peekPivot.parent.InverseTransformPoint(pivotWorld)
            : pivotWorld;

        return pivotLocal - pivotBaseLocalPos;
    }
}
