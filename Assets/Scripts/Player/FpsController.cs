using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FpsController : MonoBehaviour
{
    public GameObject flashlight;
    private bool flashOn = false;
    public bool IsMoving;
    public bool IsSprintingNow => isSprinting;
    [Header("Refs")]
    public Transform cameraRoot;          // 绑定 Camera 或 CameraRoot
    public Camera playerCamera;           // 绑定 Camera
    public PlayerInput playerInput;       // 自动抓也行
    public LayerMask obstructionMask = ~0; // 默认 Everything，后面我们会在 Awake 里剔除 Player 层
    public CapsuleCollider bodyCapsule;  // 推荐：玩家额外碰撞体
    public bool syncColliderRadius = true;
    public float colliderRadiusScale = 1f; // 需要略小一点可设 0.95
    public PlayerMovementNoise movementNoise;


    [Header("Look")]
    public float mouseSensitivity = 0.08f; // 新Input里 MouseDelta 通常数值更大，灵敏度更小
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    [Header("Move Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 3.5f;
    public float proneSpeed = 2.0f;

    [Header("Acceleration")]
    public float acceleration = 25f;   // 起步加速度
    public float deceleration = 35f;   // 松手减速度
    public float airControl = 0.35f;   // 空中水平控制倍率(0~1)

    [Header("Stamina")]
    public bool enableStamina = true;
    public float staminaMax = 5.0f;          // 最大体力（秒）
    public float staminaDrainPerSec = 1.2f;  // 冲刺每秒消耗
    public float staminaRegenPerSec = 0.9f;  // 恢复速度
    public float regenDelay = 0.8f;          // 停止冲刺后延迟多久开始恢复
    public float sprintReenableThreshold = 0.25f; // 至少恢复到 max 的 25% 才允许再次冲刺

    [Header("Gravity / Jump")]
    public float gravity = -25f;
    public float jumpHeight = 1.2f;
    public float groundStickForce = 3.5f;

    [Header("Stance Heights")]
    public float standHeight = 1.8f;
    public float crouchHeight = 1.25f;
    public float proneHeight = 0.75f;
    public float stanceLerpSpeed = 12f;

    [Header("Camera Heights (local Y)")]
    public float camStandY = 1.6f;
    public float camCrouchY = 1.1f;
    public float camProneY = 0.65f;

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobFrequency = 1.8f;
    public float bobAmplitude = 0.05f;
    public float bobSprintMultiplier = 1.35f;

    [Header("FOV Kick")]
    public bool enableFovKick = true;
    public float baseFov = 60f;
    public float sprintFov = 70f;
    public float fovLerpSpeed = 10f;

    [Header("External Locks")]
    public bool externalMovementLock;
    public bool externalLadderLock;
    public bool externalJumpLock;
    public bool externalLookLock;


    // Internal
    float stamina;
    float regenTimer;
    bool sprintLocked; // 体力耗尽后锁住冲刺直到恢复到阈值
    bool isSprinting;

    CharacterController cc;
    public bool IsGrounded => cc != null && cc.isGrounded;

    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction crouchAction;
    InputAction proneAction;
    InputAction flashlightAction;

    float yaw, pitch;

    Vector3 horizontalVelocity; // xz
    float verticalVelocity;     // y
    float bobTimer;

    float camYVel;
    public float cameraSmoothTime = 0.06f;

    public enum Stance { Stand, Crouch, Prone }
    public Stance stance = Stance.Stand;
    public bool IsProne => stance == Stance.Prone;

    float targetControllerHeight;
    float targetCameraY;
    float originalRadius;
    Vector3 cameraBaseLocalPos;

    void Awake()
    {
        int playerLayer = gameObject.layer;
        obstructionMask &= ~(1 << playerLayer); // 从检测里排除自己所在层

        cc = GetComponent<CharacterController>();

        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        if (!cameraRoot) cameraRoot = GetComponentInChildren<Camera>()?.transform;
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();

        if (cameraRoot != null) cameraBaseLocalPos = cameraRoot.localPosition;
        originalRadius = cc.radius;

        // 从 PlayerInput 的 Actions 里按名字抓（要求你按我建议的命名）
        var actions = playerInput.actions;
        moveAction = actions["Move"];
        lookAction = actions["Look"];
        jumpAction = actions["Jump"];
        sprintAction = actions["Sprint"];
        crouchAction = actions["Crouch"];
        proneAction = actions["Prone"];
        flashlightAction = actions["Flashlight"];

        stamina = staminaMax;
        regenTimer = 0f;
        sprintLocked = false;

        SetStance(Stance.Stand, instant: true);
        if (playerCamera && enableFovKick) playerCamera.fieldOfView = baseFov;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        crouchAction?.Enable();
        proneAction?.Enable();
        flashlightAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        crouchAction?.Disable();
        proneAction?.Disable();
        flashlightAction?.Disable();
    }

    void Update()
    {
        //if (enableStamina) Debug.Log($"Stamina: {stamina:F2}/{staminaMax}");
        if (flashlightAction.WasPressedThisFrame())
        {
            flashOn = !flashOn;
            if(flashOn)flashlight.SetActive(true);
            else flashlight.SetActive(false);
        }
        HandleLook();
        if (externalLadderLock)
        {
            // 清掉速度，避免退出梯子时带着旧速度“冲一下”
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            isSprinting = false;

            return;
        }
        HandleStanceInput();
        HandleMovement();

        if (movementNoise != null)
        {
            bool crouching = stance == Stance.Crouch;
            bool prone = stance == Stance.Prone;
            bool sprinting = isSprinting && !externalLadderLock;

            movementNoise.SetMovementState(sprinting, crouching, prone);
        }
    }
    private void LateUpdate()
    {
        HandleCameraEffects();
    }
    void HandleLook()
    {
        if (externalLookLock)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();
        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraRoot) cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleStanceInput()
    {
        // 你可以改成“按住蹲下/松开站起”，这里默认是 toggle
        if (crouchAction.WasPressedThisFrame())
        {
            if (stance == Stance.Crouch) TrySetStance(Stance.Stand);
            else TrySetStance(Stance.Crouch);
        }

        if (proneAction.WasPressedThisFrame())
        {
            if (stance == Stance.Prone) TrySetStance(Stance.Stand);
            else TrySetStance(Stance.Prone);
        }
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        IsMoving = !externalMovementLock && input.sqrMagnitude > 0.01f;
        Vector3 wishDir = (transform.right * input.x + transform.forward * input.y);
        wishDir = wishDir.sqrMagnitude > 1e-6f ? wishDir.normalized : Vector3.zero;

        bool grounded = cc.isGrounded;

        if (externalMovementLock)
        {
            IsMoving = false;   // ✅ Peek 时禁止脚步声判断

            // 1) 立即停掉水平速度（不然会继续滑）
            horizontalVelocity = Vector3.zero;

            // 2) 仍然允许重力/贴地（否则下楼梯会飘）
            if (grounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -groundStickForce;
            }
            verticalVelocity += gravity * Time.deltaTime;

            // 3) 只做垂直 Move（防止任何 WASD 位移）
            cc.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);

            // 4) Peek 模式下不处理其他移动逻辑
            return;
        }

        // 速度目标（根据 stance/sprint）
        bool sprintHeld = sprintAction.IsPressed();

        // 先决定“是否允许冲刺”
        bool wantsSprint = sprintHeld && wishDir != Vector3.zero && stance == Stance.Stand;

        // stamina gating
        bool canSprint = true;
        if (enableStamina)
        {
            if (sprintLocked) canSprint = false;                 // 耗尽锁住
            if (stamina <= 0.01f) canSprint = false;            // 没体力不能冲
        }

        bool sprintingThisFrame = wantsSprint && canSprint;
        isSprinting = sprintingThisFrame;

        float targetSpeed = walkSpeed;
        if (stance == Stance.Prone) targetSpeed = proneSpeed;
        else if (stance == Stance.Crouch) targetSpeed = crouchSpeed;
        else if (sprintingThisFrame) targetSpeed = sprintSpeed;

        UpdateStamina(sprintingThisFrame);


        Vector3 targetHorizontal = wishDir * targetSpeed;

        // 加速度/减速度（A）
        float accel = (wishDir != Vector3.zero) ? acceleration : deceleration;
        float control = grounded ? 1f : airControl;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetHorizontal,
            accel * control * Time.deltaTime
        );

        // 重力/跳跃
        if (grounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -groundStickForce;

            if (!externalJumpLock && stance != Stance.Prone && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        // Stance 平滑更新（B）
        //ApplyStanceLerp();

        // 最终 Move
        Vector3 move = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        cc.Move(move * Time.deltaTime);
    }

    void HandleCameraEffects()
    {
        if (!cameraRoot) return;

        // 目标相机高度（来自 stance）
        float y = targetCameraY;

        // Head Bob（C）
        if (enableHeadBob)
        {
            Vector3 hv = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
            float speed01 = Mathf.Clamp01(hv.magnitude / Mathf.Max(0.01f, walkSpeed));
            bool grounded = cc.isGrounded;

            if (grounded && hv.magnitude > 0.1f)
            {
                float freq = bobFrequency;
                // 冲刺 bob 更快更大
                if (IsSprinting()) freq *= bobSprintMultiplier;

                bobTimer += Time.deltaTime * freq * (1f + speed01);
                float bobOffset = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;

                y += bobOffset;
            }
            else
            {
                // 静止/空中时让 bobTimer 慢慢回稳
                bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 6f);
            }
        }

        // 应用相机 localPosition（只改 y，避免和你别的镜头效果打架）
        Vector3 lp = cameraRoot.localPosition;
        lp.y = Mathf.Lerp(lp.y, y, Time.deltaTime * stanceLerpSpeed);
        cameraRoot.localPosition = lp;

        // FOV Kick（C）
        if (enableFovKick && playerCamera)
        {
            float fovTarget = IsSprinting() ? sprintFov : baseFov;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fovTarget, Time.deltaTime * fovLerpSpeed);
        }
    }

    bool IsSprinting() => isSprinting;

    void TrySetStance(Stance next)
    {
        // 站起时要做“头顶空间检测”，避免卡进天花板
        if (next == Stance.Stand)
        {
            if (!CanStandUp()) return;
        }
        // 从趴下 -> 蹲下/站起也需要空间；这里统一在 CanStandUp 里处理最高目标
        if (next == Stance.Crouch && stance == Stance.Prone)
        {
            if (!CanChangeToHeight(crouchHeight)) return;
        }
        if (next == Stance.Prone && stance != Stance.Prone)
        {
            // 趴下通常没必要检测（变矮），但你也可以检测地形/坡度
        }

        SetStance(next, instant: false);
    }

    void SetStance(Stance next, bool instant)
    {
        stance = next;

        switch (stance)
        {
            case Stance.Stand:
                targetControllerHeight = standHeight;
                targetCameraY = camStandY;
                break;
            case Stance.Crouch:
                targetControllerHeight = crouchHeight;
                targetCameraY = camCrouchY;
                break;
            case Stance.Prone:
                targetControllerHeight = proneHeight;
                targetCameraY = camProneY;
                break;
        }

        // CC 立即切换（避免最后一帧碰撞解算抖动）
        cc.height = targetControllerHeight;
        cc.center = new Vector3(0f, cc.height / 2f, 0f);
        SyncBodyColliderInstant();

        // camera 仍然用平滑（见下面 HandleCameraEffects）
    }

    bool CanStandUp()
    {
        return CanChangeToHeight(standHeight);
    }

    bool CanChangeToHeight(float desiredHeight)
    {
        float currentHeight = cc.height;
        if (desiredHeight <= currentHeight) return true;

        float radius = cc.radius * 0.95f;

        // 注意：CharacterController 的“底部”在 transform.position
        // 胶囊下端中心大约在 transform.position + up * radius
        // 胶囊上端中心在 transform.position + up * (desiredHeight - radius)
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (desiredHeight - radius);

        // 找所有会阻挡的碰撞体（只查环境层）
        Collider[] hits = Physics.OverlapCapsule(
            bottom, top, radius,
            obstructionMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];

            // 过滤掉自己（以防你没分层、或有子物体碰撞体）
            if (c.transform == transform) continue;
            if (c.GetComponent<CharacterController>() == cc) continue;

            // 只要有任何环境碰撞体，说明站不起来
            return false;
        }

        return true;
    }
    void UpdateStamina(bool sprinting)
    {
        if (!enableStamina) return;

        if (sprinting)
        {
            stamina -= staminaDrainPerSec * Time.deltaTime;
            regenTimer = regenDelay;

            if (stamina <= 0f)
            {
                stamina = 0f;
                sprintLocked = true; // 耗尽锁冲刺
            }
        }
        else
        {
            // 不冲刺：先等延迟，再恢复
            if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
            }
            else
            {
                stamina += staminaRegenPerSec * Time.deltaTime;
                if (stamina > staminaMax) stamina = staminaMax;

                // 解锁条件：恢复到阈值
                if (sprintLocked && stamina >= staminaMax * sprintReenableThreshold)
                    sprintLocked = false;
            }
        }
    }
    void SyncBodyColliderInstant()
    {
        // 用 CharacterController 的高度/中心作为“权威值”
        float h = cc.height;
        Vector3 c = cc.center;

        if (bodyCapsule)
        {
            // CapsuleCollider 默认沿 Y 轴：direction = 1
            bodyCapsule.direction = 1;
            bodyCapsule.height = h;

            if (syncColliderRadius)
                bodyCapsule.radius = cc.radius * colliderRadiusScale;

            // CapsuleCollider 的 center 是 local space
            bodyCapsule.center = c;
        }
    }

}

