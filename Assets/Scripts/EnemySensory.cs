using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour,INoiseListener
{
    public enum State { Roam, Suspicious, Chase }
    [Header("State Materials (Debug)")]
    public Renderer targetRenderer;   // 指向敌人模型的 Renderer
    public Material roamMaterial;
    public Material suspiciousMaterial;
    public Material chaseMaterial;

    Material originalMaterial; // 可选：记录初始材质

    [Header("Refs")]
    public Transform eyePos;
    public NavMeshAgent agent;

    [Header("Tick")]
    public float sensoryTick = 0.12f;

    [Header("Vision")]
    public bool canSeeThrough = false;
    public float sightRadius = 18f;
    [Range(0, 180)] public float fov = 110f;
    public float playerHeadHeight = 1.6f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Suspicious")]
    public float investigateStopDistance = 0.9f;
    public float searchDuration = 4.0f;           // 到达最后已知点后搜寻多久
    public float suspicionTimeout = 8.0f;         // 多久没新线索回到漫游
    public float noiseMinLoudness = 0.1f;         // 小于这个就忽略
    public float noiseHearingRadius = 14f;        // 噪音听力半径（可做简单衰减）

    [Header("Chase")]
    public float visionConfirmTime = 1.0f;  // 看到多久才确认追击
    public bool showVisionProgressDebug = false;
    float visionTimer = 0f;

    public float chaseStopDistance = 1.6f;
    public float loseSightGrace = 1.2f;           // 丢视野后仍追多久
    public float chaseUpdateInterval = 0.12f;     // 追击刷新目标点间隔
    public float chaseSpeed = 3.8f;
    public float roamSpeed = 2.0f;

    [Header("Roam")]
    public float roamRadius = 10f;                // 漫游取点范围
    public int segmentsPerDirectionMin = 2;       // 每个方向连续走几段
    public int segmentsPerDirectionMax = 5;
    public float segmentLengthMin = 2.0f;
    public float segmentLengthMax = 4.5f;
    public float idlePauseMin = 0.2f;
    public float idlePauseMax = 0.7f;
    public float arriveDistance = 0.6f;
    public float directionChangeCooldown = 0.6f;  // 防止频繁换方向
    [Header("TerrorRoam")]
    public bool keepNearPlayer = true;
    public Transform player;                 // 拖 Player Transform
    public float anchorUpdateInterval = 7f;  // 多久获取一次玩家位置
    public float anchorMinDistance = 6f;     // 敌人距离玩家小于这个就不需要再拉近
    public float anchorMaxDistance = 18f;    // 敌人离玩家超过这个就强制把 roamOrigin 拉到玩家附近
    public float anchorRingMin = 4f;         // 选点半径（最小）
    public float anchorRingMax = 10f;        // 选点半径（最大）
    public float anchorArriveDistance = 1.0f;// 到达锚点附近算完成
    public float anchorCooldownAfterChase = 3f; // 追击结束后多久才开始拉近（防止突兀）
    [Header("Stuck Detection (Roam)")]
    public float stuckCheckInterval = 0.35f;  // 多久检查一次
    public float stuckMinMove = 0.08f;        // 这段时间内移动少于多少算“没动”
    public float stuckTimeToRepath = 1.2f;    // 连续卡住多久才触发重选点
    public float repathCooldown = 0.6f;       // 触发一次后冷却，避免抖动
    

    float nextAnchorTime;
    float lastChaseEndTime;
    bool goingToAnchor;
    Vector3 currentAnchorPos;

    float nextStuckCheckTime;
    Vector3 lastStuckCheckPos;
    float stuckAccum;
    float lastRepathTime;

    [Header("Debug")]
    public State state = State.Roam;
    public Transform playerSeen;
    public bool debugHearing = false;

    // internal
    float nextSenseTime;
    float nextChaseSetTime;

    Vector3 lastKnownPlayerPos;
    float lastSeenTime;

    Vector3 investigateTarget;
    float suspicionStartTime;
    float searchEndTime;

    // roam internal
    Vector3 roamDir;
    int roamSegmentsLeft;
    float nextRoamDecisionTime;
    float roamPauseUntil;
    float lastDirChangeTime;
    Vector3 roamOrigin;

    void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer)
            originalMaterial = targetRenderer.material;

        if (!eyePos) eyePos = transform;
        if (!agent) agent = GetComponent<NavMeshAgent>();
        roamOrigin = transform.position;
    }

    void OnEnable()
    {
        nextSenseTime = 0f;
        nextChaseSetTime = 0f;
        PickNewRoamDirection(force: true);
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
    }

    void Update()
    {
        // 感知 Tick（不用每帧做 overlap/raycast）
        if (Time.time >= nextSenseTime)
        {
            nextSenseTime = Time.time + sensoryTick;
            SensePlayer();
            UpdateStateBySight();
        }

        // 状态行为
        switch (state)
        {
            case State.Roam: TickRoam(); break;
            case State.Suspicious: TickSuspicious(); break;
            case State.Chase: TickChase(); break;
        }
    }

    // -------------------------
    // SENSE
    // -------------------------
    void SensePlayer()
    {
        playerSeen = null;

        Collider[] hits = Physics.OverlapSphere(eyePos.position, sightRadius, playerMask);
        if (hits == null || hits.Length == 0) return;

        // 找最近/最优目标（这里简单取第一个可见；你也可以按距离排序）
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            Vector3 headPos = t.position + Vector3.up * playerHeadHeight;

            if (!InFOV(headPos)) continue;
            if (!canSeeThrough && !InSight(headPos)) continue;

            playerSeen = t;
            lastKnownPlayerPos = t.position; // 记录脚下点就够了
            lastSeenTime = Time.time;
            break;
        }
    }

    bool InSight(Vector3 pos)
    {
        Vector3 dir = pos - eyePos.position;
        float dist = dir.magnitude;
        if (dist < 0.1f) return true;

        dir /= dist;
        return !Physics.Raycast(eyePos.position, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    bool InFOV(Vector3 pos)
    {
        Vector3 dir = pos - eyePos.position;
        dir.y = 0f; // 只用水平FOV（更符合FPS）
        if (dir.sqrMagnitude < 1e-6f) return true;

        float angle = Vector3.Angle(dir, eyePos.forward);
        return angle <= fov * 0.5f;
    }

    void UpdateStateBySight()
    {
        if (playerSeen)
        {
            visionTimer += sensoryTick;

            if (showVisionProgressDebug)
                Debug.Log($"Vision Timer: {visionTimer:F2}");

            if (visionTimer >= visionConfirmTime)
            {
                EnterChase();
                visionTimer = 0f;
            }

            return;
        }
        else
        {
            visionTimer = 0f;  // 丢视野就清零
        }

        // 丢视野后的状态流转
        if (state == State.Chase)
        {
            // grace 时间内仍保持追击/去最后已知点
            if (Time.time - lastSeenTime <= loseSightGrace) return;
            lastChaseEndTime = Time.time;

            // 进入怀疑：去最后已知点搜
            EnterSuspicious(lastKnownPlayerPos);
            return;
        }

        if (state == State.Suspicious)
        {
            // 怀疑超时回漫游
            if (Time.time - suspicionStartTime > suspicionTimeout)
                EnterRoam();
        }
    }
    public void OnHeardNoise(Vector3 noisePosition, float loudness = 1f)
    {
        HearNoise(noisePosition, loudness);
    }

    // -------------------------
    // EXTERNAL: NOISE
    // -------------------------
    /// <summary>
    /// 被外部（脚步/投掷/门/终端）调用：敌人听到噪音，进入怀疑并前往该位置。
    /// loudness: 0~1+，可理解为强度
    /// </summary>
    public void HearNoise(Vector3 pos, float loudness = 1f)
    {
        if (debugHearing)
        {
            Debug.Log($"{name} heard noise at {pos}, loudness={loudness:F2}");
        }
        if (loudness < noiseMinLoudness)
            return;

        float d = Vector3.Distance(transform.position, pos);
        if (d > noiseHearingRadius * Mathf.Max(0.2f, loudness))
            return;

        // 正在追击时忽略普通噪音
        if (state == State.Chase)
            return;

        // 如果已经在怀疑状态，就刷新调查目标和计时
        if (state == State.Suspicious)
        {
            investigateTarget = pos;
            suspicionStartTime = Time.time;
            searchEndTime = 0f;

            agent.stoppingDistance = investigateStopDistance;
            SetDestinationNavSafe(investigateTarget);
            return;
        }

        EnterSuspicious(pos);
    }

    // -------------------------
    // STATE ENTER
    // -------------------------
    void EnterRoam()
    {
        state = State.Roam;
        UpdateMaterialByState();
        agent.speed = roamSpeed;
        roamOrigin = transform.position;
        PickNewRoamDirection(force: true);
        nextStuckCheckTime = Time.time + stuckCheckInterval;
        lastStuckCheckPos = transform.position;
        stuckAccum = 0f;
        lastRepathTime = -999f;
        goingToAnchor = false;
        nextAnchorTime = Time.time + Random.Range(anchorUpdateInterval * 0.6f, anchorUpdateInterval * 1.2f);
    }

    void EnterSuspicious(Vector3 target)
    {
        state = State.Suspicious;
        UpdateMaterialByState();
        agent.speed = roamSpeed * 1.15f;

        investigateTarget = target;
        suspicionStartTime = Time.time;
        searchEndTime = 0f; // 到点后再开始计时

        agent.stoppingDistance = investigateStopDistance;
        SetDestinationNavSafe(investigateTarget);
    }

    void EnterChase()
    {
        if (state != State.Chase)
        {
            state = State.Chase;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = chaseStopDistance;
            UpdateMaterialByState();
        }

        // 立即刷新目的地（追人）
        nextChaseSetTime = 0f;
    }

    // -------------------------
    // BEHAVIORS
    // -------------------------
    void TickChase()
    {
        if (!agent) return;

        // 定期更新追击目标点（避免每帧 SetDestination）
        if (Time.time >= nextChaseSetTime)
        {
            nextChaseSetTime = Time.time + chaseUpdateInterval;

            Vector3 target = playerSeen ? playerSeen.position : lastKnownPlayerPos;
            SetDestinationNavSafe(target);
        }

        // 追到停止距离就不管了（你后面可做攻击/抓捕）
    }

    void TickSuspicious()
    {
        if (!agent) return;

        // 到达最后已知点：开始“搜寻时间”
        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(arriveDistance, investigateStopDistance))
        {
            if (searchEndTime <= 0f)
            {
                searchEndTime = Time.time + searchDuration;
                // 简单搜寻：原地小转/小范围点，这里先原地停留
                agent.ResetPath();
            }
        }

        // 搜寻时间结束：回漫游
        if (searchEndTime > 0f && Time.time >= searchEndTime)
        {
            EnterRoam();
        }
    }
    void TickRoam()
    {
        if (!agent) return;

        // pause 时停住
        if (Time.time < roamPauseUntil)
        {
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;

        // ✅ 恐怖导演：周期性把漫游中心迁移到玩家附近（不等于追击）
        if (keepNearPlayer && player && Time.time >= nextAnchorTime && Time.time - lastChaseEndTime >= anchorCooldownAfterChase)
        {
            float d = Vector3.Distance(transform.position, player.position);

            // 离得太远：强制拉近；离得不远：概率性拉近（避免一直在同一区域）
            bool shouldPull = d >= anchorMaxDistance || (d >= anchorMinDistance && Random.value < 0.55f);

            if (shouldPull)
            {
                // 选一个玩家周围的环形点作为“锚点”
                if (TryPickPointNearPlayer(out currentAnchorPos))
                {
                    goingToAnchor = true;
                    agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, anchorArriveDistance);
                    SetDestinationNavSafe(currentAnchorPos);
                }
            }

            nextAnchorTime = Time.time + Random.Range(anchorUpdateInterval * 0.8f, anchorUpdateInterval * 1.3f);
        }

        // ✅ 如果正在去锚点，到达后把 roamOrigin 刷到玩家附近，然后继续正常 roam
        if (goingToAnchor)
        {
            if (!agent.pathPending && agent.remainingDistance <= anchorArriveDistance)
            {
                goingToAnchor = false;
                roamOrigin = currentAnchorPos;          // 把漫游中心迁移过去
                PickNewRoamDirection(force: true);
                agent.ResetPath();
                roamPauseUntil = Time.time + Random.Range(idlePauseMin, idlePauseMax);
            }

            // 去锚点途中不走你原来的 segment roam（避免目标打架）
            return;
        }

        // ✅ 卡住检测：只有在“有目的地且未到达”时才检查
        if (agent.hasPath && !agent.pathPending)
        {
            StuckCheckAndRepathIfNeeded();
        }
        else
        {
            // 没路径：下发一个新段
            SetNextRoamSegmentDestination();
            return;
        }

        // 正常到达判定：到点才 pause + 清路径
        bool arrived = agent.pathStatus == NavMeshPathStatus.PathComplete &&
                       agent.remainingDistance <= arriveDistance;

        if (arrived)
        {
            roamPauseUntil = Time.time + Random.Range(idlePauseMin, idlePauseMax);
            agent.ResetPath();
            return;
        }
    }

    void SetNextRoamSegmentDestination()
    {
        if (roamSegmentsLeft <= 0)
        {
            if (Time.time - lastDirChangeTime > directionChangeCooldown)
                PickNewRoamDirection(force: false);
        }

        Vector3 segTarget = transform.position + roamDir * Random.Range(segmentLengthMin, segmentLengthMax);

        // 限制在 roamRadius 内
        Vector3 to = segTarget - roamOrigin;
        if (to.magnitude > roamRadius)
            segTarget = roamOrigin + to.normalized * roamRadius;

        if (SetDestinationNavSafe(segTarget))
        {
            roamSegmentsLeft--;
            // ✅ 注意：这里不设置 pause
        }
        else
        {
            PickNewRoamDirection(force: true);
        }
    }

    void PickNewRoamDirection(bool force)
    {
        // 选一个新主方向（偏“连续性”：避免 180° 急转）
        Vector3 current = roamDir.sqrMagnitude < 1e-6f ? transform.forward : roamDir;

        Vector3 dir;
        int guard = 0;
        do
        {
            guard++;
            float angle = Random.Range(-110f, 110f); // 不要太极端
            dir = Quaternion.Euler(0f, angle, 0f) * current;
            dir.y = 0f;
            dir.Normalize();
        }
        while (!force && guard < 8 && Vector3.Dot(dir, current) < 0.2f); // 避免太反向

        roamDir = dir;
        roamSegmentsLeft = Random.Range(segmentsPerDirectionMin, segmentsPerDirectionMax + 1);
        lastDirChangeTime = Time.time;
    }

    bool SetDestinationNavSafe(Vector3 worldPos)
    {
        if (!agent) return false;

        if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 1.2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!eyePos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePos.position, sightRadius);

        // FOV lines
        Vector3 fwd = eyePos.forward;
        Vector3 left = Quaternion.Euler(0f, -fov * 0.5f, 0f) * fwd;
        Vector3 right = Quaternion.Euler(0f, fov * 0.5f, 0f) * fwd;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(eyePos.position, left.normalized * 2f);
        Gizmos.DrawRay(eyePos.position, right.normalized * 2f);
    }
#endif
    void UpdateMaterialByState()
    {
        if (!targetRenderer) return;

        switch (state)
        {
            case State.Roam:
                if (roamMaterial) targetRenderer.material = roamMaterial;
                break;

            case State.Suspicious:
                if (suspiciousMaterial) targetRenderer.material = suspiciousMaterial;
                break;

            case State.Chase:
                if (chaseMaterial) targetRenderer.material = chaseMaterial;
                break;
        }
    }
    void StuckCheckAndRepathIfNeeded()
    {
        if (Time.time < nextStuckCheckTime) return;
        nextStuckCheckTime = Time.time + stuckCheckInterval;

        float moved = Vector3.Distance(transform.position, lastStuckCheckPos);
        lastStuckCheckPos = transform.position;

        // 只在“还没到”且“移动很小”时累积卡住时间
        bool notArrived = agent.remainingDistance > Mathf.Max(arriveDistance, 0.5f);

        if (notArrived && moved < stuckMinMove)
        {
            stuckAccum += stuckCheckInterval;
        }
        else
        {
            stuckAccum = 0f;
        }

        if (stuckAccum >= stuckTimeToRepath && Time.time - lastRepathTime >= repathCooldown)
        {
            lastRepathTime = Time.time;
            stuckAccum = 0f;

            // 强制换方向并选新段目标（更容易从边缘/角落脱困）
            PickNewRoamDirection(force: true);
            agent.ResetPath();
            SetNextRoamSegmentDestination();
        }
    }
    bool TryPickPointNearPlayer(out Vector3 result)
    {
        result = player.position;

        // 在玩家周围环形取样
        for (int i = 0; i < 10; i++)
        {
            float r = Random.Range(anchorRingMin, anchorRingMax);
            float a = Random.Range(0f, 360f);
            Vector3 offset = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad)) * r;
            Vector3 candidate = player.position + offset;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        return false;
    }

}
