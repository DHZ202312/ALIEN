using System.Collections;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour, INoiseListener
{
    public enum State { Roam, Suspicious, Chase }

    [Header("State Materials (Debug)")]
    public Renderer targetRenderer;
    public Material roamMaterial;
    public Material suspiciousMaterial;
    public Material chaseMaterial;

    Material originalMaterial;

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
    public float searchDuration = 4.0f;
    public float suspicionTimeout = 8.0f;
    public float noiseMinLoudness = 0.1f;
    public float noiseHearingRadius = 14f;

    [Header("Chase")]
    public float visionConfirmTime = 1.0f;
    public bool showVisionProgressDebug = false;
    float visionTimer = 0f;

    public float chaseStopDistance = 1.6f;
    public float loseSightGrace = 1.2f;
    public float chaseUpdateInterval = 0.12f;
    public float chaseSpeed = 3.8f;
    public float roamSpeed = 2.0f;

    [Header("Roam")]
    public float roamRadius = 10f;
    public int segmentsPerDirectionMin = 2;
    public int segmentsPerDirectionMax = 5;
    public float segmentLengthMin = 2.0f;
    public float segmentLengthMax = 4.5f;
    public float idlePauseMin = 0.2f;
    public float idlePauseMax = 0.7f;
    public float arriveDistance = 0.6f;
    public float directionChangeCooldown = 0.6f;

    [Header("TerrorRoam")]
    public bool keepNearPlayer = true;
    public Transform player;
    public float anchorUpdateInterval = 7f;
    public float anchorMinDistance = 6f;
    public float anchorMaxDistance = 18f;
    public float anchorRingMin = 4f;
    public float anchorRingMax = 10f;
    public float anchorArriveDistance = 1.0f;
    public float anchorCooldownAfterChase = 3f;

    [Header("Stuck Detection (Roam)")]
    public float stuckCheckInterval = 0.35f;
    public float stuckMinMove = 0.08f;
    public float stuckTimeToRepath = 1.2f;
    public float repathCooldown = 0.6f;

    [Header("Scripted Patrol")]
    public bool useScriptedPatrol = false;
    public Transform[] patrolPoints;
    public float patrolPointStopTime = 5f;
    public float patrolLookAroundSpeed = 60f;
    public float patrolLookAroundAngle = 45f;

    [Header("Patrol Exit")]
    
    public bool patrolReturnToFreeRoam = false;
    public bool patrolExitLevel = false;
    public Transform patrolExitPoint;
    public bool disableGameObjectOnExit = true;
    public bool disableAgentOnExit = true;
    public bool disableAIOnExit = true;
    public GameObject PowerBack;

    float nextAnchorTime;
    float lastChaseEndTime;
    bool goingToAnchor;
    Vector3 currentAnchorPos;

    float nextStuckCheckTime;
    Vector3 lastStuckCheckPos;
    float stuckAccum;
    float lastRepathTime;

    [Header("Jump And Leave")]
    public float leaveJumpUpDistance = 1.2f;
    public float leaveJumpUpDuration = 0.22f;
    public float leaveHangDuration = 0.5f;
    public float leaveClimbUpDistance = 1.0f;
    public float leaveClimbUpDuration = 0.8f;

    public bool disableGameObjectOnLeave = true;
    public bool disableRendererOnLeave = false;
    public Renderer[] renderersToHide;

    [Header("Debug")]
    public State state = State.Roam;
    public Transform playerSeen;
    public bool debugHearing = false;

    float nextSenseTime;
    float nextChaseSetTime;

    Vector3 lastKnownPlayerPos;
    float lastSeenTime;

    Vector3 investigateTarget;
    float suspicionStartTime;
    float searchEndTime;

    Vector3 roamDir;
    int roamSegmentsLeft;
    float roamPauseUntil;
    float lastDirChangeTime;
    Vector3 roamOrigin;

    int currentPatrolIndex = 0;
    bool isWaitingAtPatrolPoint = false;
    float patrolWaitEndTime = 0f;
    bool patrolGoingToExit = false;
    float patrolLookBaseYaw = 0f;
    bool scriptedPatrolInterrupted = false;
    bool isLeavingLevel = false;
    Coroutine leavingRoutine;

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

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (useScriptedPatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1);
            patrolGoingToExit = false;
            isWaitingAtPatrolPoint = false;
        }
    }

    void Update()
    {
        if (isLeavingLevel)
            return;
        if (Time.time >= nextSenseTime)
        {
            nextSenseTime = Time.time + sensoryTick;
            SensePlayer();
            UpdateStateBySight();
        }

        switch (state)
        {
            case State.Roam: TickRoam(); break;
            case State.Suspicious: TickSuspicious(); break;
            case State.Chase: TickChase(); break;
        }
    }

    void SensePlayer()
    {
        playerSeen = null;

        Collider[] hits = Physics.OverlapSphere(eyePos.position, sightRadius, playerMask);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;

            PlayerHideState hideState = t.GetComponentInParent<PlayerHideState>();
            if (hideState != null && hideState.isHidden)
                continue;

            Vector3 headPos = t.position + Vector3.up * playerHeadHeight;

            if (!InFOV(headPos)) continue;
            if (!canSeeThrough && !InSight(headPos)) continue;

            playerSeen = t;
            lastKnownPlayerPos = t.position;
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
        dir.y = 0f;
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
                if (useScriptedPatrol)
                {
                    scriptedPatrolInterrupted = true;
                    isWaitingAtPatrolPoint = false;
                    patrolGoingToExit = false;
                }

                EnterChase();
                visionTimer = 0f;
            }

            return;
        }
        else
        {
            visionTimer = 0f;
        }

        if (state == State.Chase)
        {
            if (Time.time - lastSeenTime <= loseSightGrace) return;

            lastChaseEndTime = Time.time;
            EnterSuspicious(lastKnownPlayerPos);
            return;
        }

        if (state == State.Suspicious)
        {
            if (Time.time - suspicionStartTime > suspicionTimeout)
                EnterRoam();
        }
    }

    public void OnHeardNoise(Vector3 noisePosition, float loudness = 1f)
    {
        HearNoise(noisePosition, loudness);
    }

    public void HearNoise(Vector3 pos, float loudness = 1f)
    {
        if (debugHearing)
            Debug.Log($"{name} heard noise at {pos}, loudness={loudness:F2}");

        if (loudness < noiseMinLoudness)
            return;

        float d = Vector3.Distance(transform.position, pos);
        if (d > noiseHearingRadius * Mathf.Max(0.2f, loudness))
            return;

        if (state == State.Chase)
            return;

        if (state == State.Suspicious)
        {
            investigateTarget = pos;
            suspicionStartTime = Time.time;
            searchEndTime = 0f;

            agent.stoppingDistance = investigateStopDistance;
            SetDestinationNavSafe(investigateTarget);
            return;
        }

        if (useScriptedPatrol)
        {
            scriptedPatrolInterrupted = true;
            isWaitingAtPatrolPoint = false;
            patrolGoingToExit = false;
        }

        EnterSuspicious(pos);
    }

    void EnterRoam()
    {
        state = State.Roam;
        UpdateMaterialByState();
        agent.speed = roamSpeed;

        if (useScriptedPatrol)
        {
            agent.stoppingDistance = arriveDistance;
            return;
        }

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
        searchEndTime = 0f;

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

        nextChaseSetTime = 0f;
    }

    void TickChase()
    {
        if (!agent) return;

        if (Time.time >= nextChaseSetTime)
        {
            nextChaseSetTime = Time.time + chaseUpdateInterval;
            Vector3 target = playerSeen ? playerSeen.position : lastKnownPlayerPos;
            SetDestinationNavSafe(target);
        }
    }

    void TickSuspicious()
    {
        if (!agent) return;

        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(arriveDistance, investigateStopDistance))
        {
            if (searchEndTime <= 0f)
            {
                searchEndTime = Time.time + searchDuration;
                agent.ResetPath();
            }
        }

        if (searchEndTime > 0f && Time.time >= searchEndTime)
        {
            EnterRoam();
        }
    }

    void TickRoam()
    {
        if (!agent) return;

        if (useScriptedPatrol)
        {
            if (!isLeavingLevel)
                TickScriptedPatrol();
            return;
        }

        if (Time.time < roamPauseUntil)
        {
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;

        if (keepNearPlayer && player && Time.time >= nextAnchorTime && Time.time - lastChaseEndTime >= anchorCooldownAfterChase)
        {
            float d = Vector3.Distance(transform.position, player.position);
            bool shouldPull = d >= anchorMaxDistance || (d >= anchorMinDistance && Random.value < 0.55f);

            if (shouldPull)
            {
                if (TryPickPointNearPlayer(out currentAnchorPos))
                {
                    goingToAnchor = true;
                    agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, anchorArriveDistance);
                    SetDestinationNavSafe(currentAnchorPos);
                }
            }

            nextAnchorTime = Time.time + Random.Range(anchorUpdateInterval * 0.8f, anchorUpdateInterval * 1.3f);
        }

        if (goingToAnchor)
        {
            if (!agent.pathPending && agent.remainingDistance <= anchorArriveDistance)
            {
                goingToAnchor = false;
                roamOrigin = currentAnchorPos;
                PickNewRoamDirection(force: true);
                agent.ResetPath();
                roamPauseUntil = Time.time + Random.Range(idlePauseMin, idlePauseMax);
            }

            return;
        }

        if (agent.hasPath && !agent.pathPending)
        {
            StuckCheckAndRepathIfNeeded();
        }
        else
        {
            SetNextRoamSegmentDestination();
            return;
        }

        bool arrived = agent.pathStatus == NavMeshPathStatus.PathComplete &&
                       agent.remainingDistance <= arriveDistance;

        if (arrived)
        {
            roamPauseUntil = Time.time + Random.Range(idlePauseMin, idlePauseMax);
            agent.ResetPath();
            return;
        }
    }
    IEnumerator JumpAndLeaveRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 jumpPeakPos = startPos + Vector3.up * leaveJumpUpDistance;
        Vector3 climbEndPos = jumpPeakPos + Vector3.up * leaveClimbUpDistance;

        // 1. 快速上跳
        yield return MoveWorldPositionRoutine(startPos, jumpPeakPos, leaveJumpUpDuration, true);

        // 2. 扒住边缘悬停
        yield return new WaitForSeconds(leaveHangDuration);

        // 3. 慢速把自己拖进去
        yield return MoveWorldPositionRoutine(jumpPeakPos, climbEndPos, leaveClimbUpDuration, false);

        // 4. 消失
        if (disableRendererOnLeave)
        {
            HideRenderers();
        }

        if (disableGameObjectOnLeave)
        {
            gameObject.SetActive(false);
        }
        PowerBack.SetActive(true);

        isLeavingLevel = false;
        leavingRoutine = null;
    }
    IEnumerator MoveWorldPositionRoutine(Vector3 from, Vector3 to, float duration, bool easeOut)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            float easedT = easeOut
                ? EaseOutCubic(t)
                : Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(from, to, easedT);
            yield return null;
        }

        transform.position = to;
    }
    void HideRenderers()
    {
        if (renderersToHide != null && renderersToHide.Length > 0)
        {
            for (int i = 0; i < renderersToHide.Length; i++)
            {
                if (renderersToHide[i] != null)
                    renderersToHide[i].enabled = false;
            }
            return;
        }

        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] != null)
                rs[i].enabled = false;
        }
    }
    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    void TickScriptedPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.isStopped = false;
        agent.speed = roamSpeed;
        agent.stoppingDistance = arriveDistance;

        if (patrolGoingToExit)
        {
            if (patrolExitPoint == null)
                return;

            if (!agent.hasPath && !agent.pathPending)
            {
                SetDestinationNavSafe(patrolExitPoint.position);
            }

            if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            {
                HandlePatrolExitLevelReached();
            }

            return;
        }
        void HandlePatrolExitLevelReached()
        {
            useScriptedPatrol = false;
            patrolGoingToExit = false;
            isWaitingAtPatrolPoint = false;
            scriptedPatrolInterrupted = false;

            if (agent != null)
                agent.enabled = false;

            // 如果有 EnemyAnim，就播离场动画
            HowEnemyLeaves();

            // 没有动画的话，直接消失
        }
        if (isWaitingAtPatrolPoint)
        {
            agent.ResetPath();
            TickPatrolLookAround();

            if (Time.time >= patrolWaitEndTime)
            {
                isWaitingAtPatrolPoint = false;
                AdvancePatrolTarget();
            }

            return;
        }

        if (!agent.hasPath && !agent.pathPending)
        {
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint != null)
            {
                SetDestinationNavSafe(targetPoint.position);
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            BeginPatrolPointWait();
        }
    }

    void BeginPatrolPointWait()
    {
        isWaitingAtPatrolPoint = true;
        patrolWaitEndTime = Time.time + patrolPointStopTime;
        patrolLookBaseYaw = transform.eulerAngles.y;
        agent.ResetPath();
    }

    void TickPatrolLookAround()
    {
        float sin = Mathf.Sin(Time.time * patrolLookAroundSpeed * Mathf.Deg2Rad);
        float offset = sin * patrolLookAroundAngle;
        float targetYaw = patrolLookBaseYaw + offset;

        Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            patrolLookAroundSpeed * Time.deltaTime
        );
    }

    void AdvancePatrolTarget()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        int nextIndex = currentPatrolIndex + 1;

        if (nextIndex < patrolPoints.Length)
        {
            currentPatrolIndex = nextIndex;
            Transform nextPoint = patrolPoints[currentPatrolIndex];
            if (nextPoint != null)
            {
                SetDestinationNavSafe(nextPoint.position);
            }
            return;
        }

        if (patrolExitLevel)
        {
            if (patrolExitPoint != null)
            {
                patrolGoingToExit = true;
                SetDestinationNavSafe(patrolExitPoint.position);
            }
            else
            {
                Debug.LogWarning($"{name}: patrolExitLevel 为 true，但 patrolExitPoint 未指定。将改为普通自由巡逻。", this);
                useScriptedPatrol = false;
                EnterRoam();
            }

            return;
        }

        if (patrolReturnToFreeRoam)
        {
            useScriptedPatrol = false;
            EnterRoam();
            return;
        }

        // 默认循环
        currentPatrolIndex = 0;
        Transform firstPoint = patrolPoints[currentPatrolIndex];
        if (firstPoint != null)
        {
            SetDestinationNavSafe(firstPoint.position);
        }
    }

    public void StartScriptedPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        useScriptedPatrol = true;
        patrolGoingToExit = false;
        isWaitingAtPatrolPoint = false;
        scriptedPatrolInterrupted = false;
        currentPatrolIndex = 0;

        agent.ResetPath();
        SetDestinationNavSafe(patrolPoints[currentPatrolIndex].position);
    }

    public void StopScriptedPatrol()
    {
        useScriptedPatrol = false;
        patrolGoingToExit = false;
        isWaitingAtPatrolPoint = false;
        scriptedPatrolInterrupted = false;
    }

    void SetNextRoamSegmentDestination()
    {
        if (roamSegmentsLeft <= 0)
        {
            if (Time.time - lastDirChangeTime > directionChangeCooldown)
                PickNewRoamDirection(force: false);
        }

        Vector3 segTarget = transform.position + roamDir * Random.Range(segmentLengthMin, segmentLengthMax);

        Vector3 to = segTarget - roamOrigin;
        if (to.magnitude > roamRadius)
            segTarget = roamOrigin + to.normalized * roamRadius;

        if (SetDestinationNavSafe(segTarget))
        {
            roamSegmentsLeft--;
        }
        else
        {
            PickNewRoamDirection(force: true);
        }
    }

    void PickNewRoamDirection(bool force)
    {
        Vector3 current = roamDir.sqrMagnitude < 1e-6f ? transform.forward : roamDir;

        Vector3 dir;
        int guard = 0;
        do
        {
            guard++;
            float angle = Random.Range(-110f, 110f);
            dir = Quaternion.Euler(0f, angle, 0f) * current;
            dir.y = 0f;
            dir.Normalize();
        }
        while (!force && guard < 8 && Vector3.Dot(dir, current) < 0.2f);

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

        Vector3 fwd = eyePos.forward;
        Vector3 left = Quaternion.Euler(0f, -fov * 0.5f, 0f) * fwd;
        Vector3 right = Quaternion.Euler(0f, fov * 0.5f, 0f) * fwd;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(eyePos.position, left.normalized * 2f);
        Gizmos.DrawRay(eyePos.position, right.normalized * 2f);

        if (useScriptedPatrol && patrolPoints != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
            }

            if (patrolExitLevel && patrolExitPoint != null && patrolPoints.Length > 0 && patrolPoints[patrolPoints.Length - 1] != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(patrolExitPoint.position, 0.35f);
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolExitPoint.position);
            }
        }
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

            PickNewRoamDirection(force: true);
            agent.ResetPath();
            SetNextRoamSegmentDestination();
        }
    }

    public void HowEnemyLeaves()
    {
        StartCoroutine(JumpAndLeaveRoutine());
    }
    bool TryPickPointNearPlayer(out Vector3 result)
    {
        result = player != null ? player.position : transform.position;

        if (player == null)
            return false;

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