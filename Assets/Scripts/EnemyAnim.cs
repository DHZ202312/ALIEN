using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnim : MonoBehaviour
{
    public enum AnimStepType
    {
        RotateToEuler,
        MoveToLocalPosition,
        Wait,
        Exit
    }

    [System.Serializable]
    public class AnimStep
    {
        public AnimStepType stepType = AnimStepType.RotateToEuler;

        [Header("Rotation")]
        public Vector3 targetEuler = Vector3.zero;

        [Header("Position")]
        public Vector3 targetLocalPosition = Vector3.zero;

        [Header("Timing")]
        public float duration = 1f;

        [Header("Per Step Control")]
        public bool applyControlBeforeStep = false;

        public bool setRigidbodyKinematic = false;
        public bool kinematicValue = true;

        public bool setAgentEnabled = false;
        public bool agentEnabledValue = false;

        public bool setAIEnabled = false;
        public bool aiEnabledValue = false;

        public bool lockPositionDuringStep = false;
        public bool lockX = false;
        public bool lockY = false;
        public bool lockZ = false;
    }

    [Header("Refs")]
    public Transform animRoot;
    public EnemyAI AI;
    public NavMeshAgent Agent;
    public Rigidbody rb;

    [Header("Sequence")]
    public List<AnimStep> steps = new List<AnimStep>();

    [Header("Ground Snap")]
    public bool snapToGroundAfterSequence = true;
    public LayerMask groundMask = ~0;
    public float groundCheckStartHeight = 1.0f;
    public float groundCheckDistance = 3.0f;
    public float groundOffset = 0.02f;

    [Header("Stare")]
    public Transform stareTarget;
    public float stareIdleTime = 4f;
    public float stareRotateSpeed = 120f; // 每秒旋转角度
    public bool stareLockYOnly = true;

    private Coroutine stareRoutine;
    private bool isStaring;

    [Header("Options")]
    public bool playOnStart = false;
    public bool useLocalRotation = true;

    private Coroutine currentRoutine;
    private bool isPlaying;

    private bool currentLockPosition;
    private bool currentLockX;
    private bool currentLockY;
    private bool currentLockZ;
    private Vector3 lockedWorldPosition;

    private bool originalKinematicState;
    private bool originalAgentEnabledState;
    private bool originalAIEnabledState;

    private void Reset()
    {
        if (animRoot == null)
            animRoot = transform;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (Agent == null)
            Agent = GetComponent<NavMeshAgent>();

        if (AI == null)
            AI = GetComponent<EnemyAI>();
    }

    private void Awake()
    {
        if (animRoot == null)
            animRoot = transform;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (Agent == null)
            Agent = GetComponent<NavMeshAgent>();

        if (AI == null)
            AI = GetComponent<EnemyAI>();

        if (rb != null)
            originalKinematicState = rb.isKinematic;

        if (Agent != null)
            originalAgentEnabledState = Agent.enabled;

        if (AI != null)
            originalAIEnabledState = AI.enabled;
    }

    private void Start()
    {
        if (playOnStart)
            PlaySequence();
    }

    private void LateUpdate()
    {
        if (!isPlaying || !currentLockPosition || animRoot == null)
            return;

        Vector3 pos = animRoot.position;

        if (currentLockX) pos.x = lockedWorldPosition.x;
        if (currentLockY) pos.y = lockedWorldPosition.y;
        if (currentLockZ) pos.z = lockedWorldPosition.z;

        animRoot.position = pos;
    }

    public void PlaySequence()
    {
        if (animRoot == null)
            animRoot = transform;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        CacheOriginalStates();
        currentRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    public void StopSequence()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentLockPosition = false;
        isPlaying = false;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    private void CacheOriginalStates()
    {
        if (rb != null)
            originalKinematicState = rb.isKinematic;

        if (Agent != null)
            originalAgentEnabledState = Agent.enabled;

        if (AI != null)
            originalAIEnabledState = AI.enabled;
    }

    private IEnumerator PlaySequenceRoutine()
    {
        isPlaying = true;

        for (int i = 0; i < steps.Count; i++)
        {
            yield return PlayStep(steps[i]);
        }

        currentLockPosition = false;

        if (snapToGroundAfterSequence)
        {
            SnapToGround();
        }

        isPlaying = false;
        currentRoutine = null;
    }

    private IEnumerator PlayStep(AnimStep step)
    {
        if (step == null)
            yield break;

        ApplyStepControls(step);

        float duration = Mathf.Max(0.0001f, step.duration);

        switch (step.stepType)
        {
            case AnimStepType.RotateToEuler:
                yield return RotateToEulerRoutine(step.targetEuler, duration);
                break;

            case AnimStepType.MoveToLocalPosition:
                yield return MoveToLocalPositionRoutine(step.targetLocalPosition, duration);
                break;

            case AnimStepType.Wait:
                yield return new WaitForSeconds(duration);
                break;

            case AnimStepType.Exit:
                currentLockPosition = false;
                this.enabled = false;
                yield break;
        }
    }

    private void ApplyStepControls(AnimStep step)
    {
        currentLockPosition = false;

        if (!step.applyControlBeforeStep)
            return;

        if (step.setRigidbodyKinematic && rb != null)
        {
            rb.isKinematic = step.kinematicValue;
        }

        if (step.setAgentEnabled && Agent != null)
        {
            Agent.enabled = step.agentEnabledValue;
        }

        if (step.setAIEnabled && AI != null)
        {
            AI.enabled = step.aiEnabledValue;
        }

        if (step.lockPositionDuringStep)
        {
            currentLockPosition = true;
            currentLockX = step.lockX;
            currentLockY = step.lockY;
            currentLockZ = step.lockZ;
            lockedWorldPosition = animRoot.position;
        }
    }

    private IEnumerator RotateToEulerRoutine(Vector3 targetEuler, float duration)
    {
        Quaternion startRot = useLocalRotation ? animRoot.localRotation : animRoot.rotation;
        Quaternion targetRot = Quaternion.Euler(targetEuler);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            Quaternion newRot = Quaternion.Slerp(startRot, targetRot, easedT);

            if (useLocalRotation)
                animRoot.localRotation = newRot;
            else
                animRoot.rotation = newRot;

            yield return null;
        }

        if (useLocalRotation)
            animRoot.localRotation = targetRot;
        else
            animRoot.rotation = targetRot;
    }

    private IEnumerator MoveToLocalPositionRoutine(Vector3 targetLocalPosition, float duration)
    {
        Vector3 startPos = animRoot.localPosition;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            animRoot.localPosition = Vector3.Lerp(startPos, targetLocalPosition, easedT);
            yield return null;
        }

        animRoot.localPosition = targetLocalPosition;
    }

    private void SnapToGround()
    {
        if (animRoot == null)
            return;

        Vector3 rayStart = animRoot.position + Vector3.up * groundCheckStartHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = animRoot.position;
            pos.y = hit.point.y + groundOffset;
            animRoot.position = pos;
        }
    }

    public void RestoreOriginalControlStates()
    {
        if (rb != null)
            rb.isKinematic = originalKinematicState;

        if (Agent != null)
            Agent.enabled = originalAgentEnabledState;

        if (AI != null)
            AI.enabled = originalAIEnabledState;
    }

    public void StartStare(Transform target)
    {
        if (target == null) return;

        if (stareRoutine != null)
            StopCoroutine(stareRoutine);

        CacheOriginalStates();

        stareTarget = target;
        stareRoutine = StartCoroutine(StareRoutine());
    }

    public void StopStare()
    {
        if (stareRoutine != null)
        {
            StopCoroutine(stareRoutine);
            stareRoutine = null;
        }

        isStaring = false;
        RestoreOriginalControlStates();
    }
    private IEnumerator StareRoutine()
    {
        isStaring = true;

        // ?锁控制（防止AI乱动）
        if (rb != null) rb.isKinematic = true;
        if (Agent != null) Agent.enabled = false;
        if (AI != null) AI.enabled = false;

        // ====== 1. 发呆 ======
        yield return new WaitForSeconds(stareIdleTime);

        // ====== 2. 转向玩家（一次性对准）======
        yield return RotateToTargetOnce();

        // ====== 3. 持续盯玩家 ======
        while (stareTarget != null)
        {
            RotateTowardsTargetContinuous();
            yield return null;
        }
    }
    private IEnumerator RotateToTargetOnce()
    {
        if (stareTarget == null) yield break;

        while (true)
        {
            Vector3 dir = stareTarget.position - animRoot.position;

            if (stareLockYOnly)
                dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                yield break;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            Quaternion current = animRoot.rotation;

            animRoot.rotation = Quaternion.RotateTowards(
                current,
                targetRot,
                stareRotateSpeed * Time.deltaTime
            );

            float angle = Quaternion.Angle(current, targetRot);

            if (angle < 1f)
                break;

            yield return null;
        }
    }
    private void RotateTowardsTargetContinuous()
    {
        if (stareTarget == null) return;

        Vector3 dir = stareTarget.position - animRoot.position;

        if (stareLockYOnly)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        animRoot.rotation = Quaternion.RotateTowards(
            animRoot.rotation,
            targetRot,
            stareRotateSpeed * Time.deltaTime
        );
    }
}