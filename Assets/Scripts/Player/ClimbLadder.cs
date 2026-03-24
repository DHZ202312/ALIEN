using UnityEngine;

[DefaultExecutionOrder(150)]
public class PlayerLadderMotor : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController cc;
    public Camera playerCam;
    public FpsController fps; // 可选：用于锁普通移动/跳跃

    [Header("Climb")]
    public float climbSpeed = 2.6f;
    public float climbAccel = 18f;
    public float stickToLadder = 6f;     // 保持贴梯子的力度（防止被推出去）

    [Header("Exit")]
    public KeyCode jumpKey = KeyCode.Space;
    public float exitJumpUp = 4.5f;
    public float exitJumpOut = 2.0f;

    [Header("Look Based Invert")]
    [Tooltip("向下看超过这个角度就反转 W/S（单位度）。例如 10~20° 比较自然。")]
    public float invertPitchThreshold = 12f;

    [Header("Grounded Dismount")]
    public float bottomDismountBand = 0.9f; // 离梯子底端多少米以内算“底部区域”
    public float dismountNudge = 0.25f;     // 退出时向外推一点，避免马上又进梯子
    public float exitCooldown = 0.2f;   // 退出后多久内不允许再次自动上梯
    float exitCooldownUntil;

    bool onLadder;
    Ladder ladder;
    float climbVel;
    float lastEnterTime;

    // 输入：你也可以换成 InputSystem
    float RawWS()
    {
        float v = 0;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        return v;
    }

    void Awake()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        if (!playerCam) playerCam = GetComponentInChildren<Camera>();
        if (!fps) fps = GetComponent<FpsController>();
    }

    void Update()
    {
        if (onLadder)
        {
            TickLadder();
        }
    }

    void TickLadder()
    {
        if (ladder == null)
        {
            ExitLadder(false);
            return;
        }

        Transform lt = ladder.ladderTransform ? ladder.ladderTransform : ladder.transform;

        // 1) 吸附居中（贴到梯子平面 + 居中线）
        SnapToLadder(lt);

        bool grounded = cc.isGrounded;
        // 2) 视线决定 W/S 的方向
        float ws = RawWS();
        float signedPitch = GetSignedPitch(playerCam.transform.eulerAngles.x); // 上看为负，下看为正（Unity的惯例）
        bool lookingDown = signedPitch > invertPitchThreshold;

        // 规则：向下看时反转 W/S
        if (lookingDown) ws = -ws;

        bool lookingUp = signedPitch < -invertPitchThreshold;

        // ✅ Grounded 直接走出梯子：
        // 向上看：按 S 退出
        // 向下看：按 W 退出
        if (grounded)
        {
            // 用梯子触发器的 bounds 估算底部位置
            float bottomY = ladder.GetComponent<Collider>().bounds.min.y;
            bool inBottomBand = transform.position.y <= (bottomY + bottomDismountBand);

            if (inBottomBand)
            {
                bool wHeld = Input.GetKey(KeyCode.W);
                bool sHeld = Input.GetKey(KeyCode.S);

                // 你原来的规则保持不变：
                // 向上看：按 S 退出
                // 向下看：按 W 退出
                if ((lookingUp && sHeld) || (lookingDown && wHeld))
                {
                    Vector3 outDir = lt.forward.normalized;

                    ExitLadder(false);
                    cc.Move(outDir * dismountNudge);
                    return;
                }
            }
        }

        float targetVel = ws * climbSpeed;
        climbVel = Mathf.MoveTowards(climbVel, targetVel, climbAccel * Time.deltaTime);

        // 3) 上下爬（沿梯子 up）
        Vector3 move = lt.up * climbVel;

        // 4) 贴梯子：持续把玩家推回梯子平面附近，避免抖/弹开
        Vector3 n = lt.forward.normalized; // 梯子外侧法线
        move += -n * stickToLadder * Time.deltaTime;

        cc.Move(move * Time.deltaTime);

        // 5) 跳跃退出
        if (Input.GetKeyDown(jumpKey))
        {
            ExitLadder(true);
        }
    }

    void SnapToLadder(Transform lt)
    {
        // 把玩家投影到梯子平面（去掉沿 forward 的偏移）
        Vector3 n = lt.forward.normalized;
        Vector3 p = transform.position;
        Vector3 planePoint = lt.position;

        float d = Vector3.Dot(p - planePoint, n);

        if (Mathf.Abs(d) > ladder.maxSnapDistance)
        {
            // 离得太远，认为失去梯子（比如被推开/走出）
            ExitLadder(false);
            return;
        }

        Vector3 target = p - n * d;

        // 可选：也可以做“居中线”吸附（沿 lt.right 把横向归零）
        Vector3 r = lt.right.normalized;
        float lateral = Vector3.Dot(target - planePoint, r);
        target -= r * lateral;

        Vector3 delta = Vector3.Lerp(Vector3.zero, target - p, ladder.snapSpeed * Time.deltaTime);
        cc.Move(delta);
    }

    // Unity eulerAngles.x: 0..360，这里转为 -180..180
    float GetSignedPitch(float x)
    {
        if (x > 180f) x -= 360f;
        return x;
    }

    void EnterLadder(Ladder l)
    {
        ladder = l;
        onLadder = true;
        climbVel = 0f;
        lastEnterTime = Time.time;

        // 锁普通移动/跳跃（不锁视野）
        if (fps)
        {
            fps.externalMovementLock = true;
            fps.externalLadderLock = true;
            fps.externalJumpLock = true;
        }
    }

    void ExitLadder(bool byJump)
    {
        Transform lt = ladder ? (ladder.ladderTransform ? ladder.ladderTransform : ladder.transform) : null;

        onLadder = false;
        ladder = null;
        exitCooldownUntil = Time.time + exitCooldown;
        climbVel = 0f;

        if (fps)
        {
            fps.externalMovementLock = false;
            fps.externalLadderLock = false;
            fps.externalJumpLock = false;
        }

        if (byJump && lt != null)
        {
            Vector3 outDir = lt.forward.normalized;
            Vector3 impulse = outDir * exitJumpOut + Vector3.up * exitJumpUp;
            cc.Move(impulse * Time.deltaTime);
        }
    }

    // 自动上梯：进入 Trigger 并且“面向梯子 + 按W向前”即可
    void OnTriggerStay(Collider other)
    {
        if (onLadder) return;

        Ladder l = other.GetComponent<Ladder>();
        if (!l) return;

        // 退出/进入冷却
        if (Time.time < exitCooldownUntil) return;
        if (Time.time - lastEnterTime < l.enterCooldown) return;

        // ------- 顶端进入（宽松）：在顶部带宽内就允许入梯 -------
        if (l.enableTopAutoEnter)
        {
            // 用触发器的 bounds 计算顶部高度
            float topY = other.bounds.max.y;
            float playerY = transform.position.y;

            bool inTopBand = playerY >= (topY - l.topEnterBand);

            // 顶端进入建议加一个“有输入意图”的门槛，否则你路过梯子口也可能被吸上梯
            bool hasIntent = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);

            if (inTopBand && hasIntent)
            {
                EnterLadder(l);
                return;
            }
        }

        // ------- 底端进入（严格）：保持你原来的逻辑（靠近/朝梯子移动） -------
        // 必须按住 W（向前）
        if (!Input.GetKey(KeyCode.W)) return;

        Transform lt = l.ladderTransform ? l.ladderTransform : l.transform;

        // 面向梯子：玩家 forward 与 -ladder.forward 的夹角足够小
        Vector3 wantDir = (-lt.forward).normalized;
        float ang = Vector3.Angle(transform.forward, wantDir);
        if (ang > l.enterAngle) return;

        EnterLadder(l);
    }


    void OnTriggerExit(Collider other)
    {
        if (!onLadder) return;
        if (!ladder) return;

        if (other.GetComponent<Ladder>() == ladder)
            ExitLadder(false);
    }
}
