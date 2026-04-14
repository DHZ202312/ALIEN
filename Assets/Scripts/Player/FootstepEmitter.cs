using UnityEngine;

public class PlayerMovementNoise : MonoBehaviour
{
    public enum MovementNoiseMode
    {
        StandWalk,
        Sprint,
        CrouchWalk,
        ProneCrawl
    }

    [Header("References")]
    public CharacterController characterController;

    [Header("Enemy Detection")]
    public LayerMask enemyMask;

    [Header("Movement Check")]
    public float minHorizontalSpeed = 0.1f;

    [Header("Current State (set by controller)")]
    public bool isSprinting;
    public bool isCrouching;
    public bool isProne;

    [Header("Stand Walk Noise")]
    public float standWalkRadius = 6f;
    public float standWalkLoudness = 1f;
    public float standWalkInterval = 0.55f;

    [Header("Sprint Noise")]
    public float sprintRadius = 11f;
    public float sprintLoudness = 1f;
    public float sprintInterval = 0.32f;

    [Header("Crouch Walk Noise")]
    public float crouchRadius = 3.5f;
    public float crouchLoudness = 1f;
    public float crouchInterval = 0.75f;

    [Header("Prone Crawl Noise")]
    public float proneRadius = 2f;
    public float proneLoudness = 1f;
    public float proneInterval = 0.95f;

    [Header("Debug")]
    public bool debugNoise;
    public bool drawCurrentNoiseRadius = true;

    private float nextNoiseTime;
    private MovementNoiseMode currentMode;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsActuallyMoving())
            return;

        currentMode = GetCurrentMode();

        if (Time.time >= nextNoiseTime)
        {
            EmitCurrentMovementNoise();
        }
    }

    public void SetMovementState(bool sprinting, bool crouching, bool prone)
    {
        isSprinting = sprinting;
        isCrouching = crouching;
        isProne = prone;
    }

    private bool IsActuallyMoving()
    {
        if (characterController == null)
            return false;

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        return horizontalVelocity.magnitude >= minHorizontalSpeed;
    }

    private MovementNoiseMode GetCurrentMode()
    {
        if (isProne)
            return MovementNoiseMode.ProneCrawl;

        if (isCrouching)
            return MovementNoiseMode.CrouchWalk;

        if (isSprinting)
            return MovementNoiseMode.Sprint;

        return MovementNoiseMode.StandWalk;
    }

    private void EmitCurrentMovementNoise()
    {
        float radius;
        float loudness;
        float interval;

        switch (currentMode)
        {
            case MovementNoiseMode.Sprint:
                radius = sprintRadius;
                loudness = sprintLoudness;
                interval = sprintInterval;
                break;

            case MovementNoiseMode.CrouchWalk:
                radius = crouchRadius;
                loudness = crouchLoudness;
                interval = crouchInterval;
                break;

            case MovementNoiseMode.ProneCrawl:
                radius = proneRadius;
                loudness = proneLoudness;
                interval = proneInterval;
                break;

            default:
                radius = standWalkRadius;
                loudness = standWalkLoudness;
                interval = standWalkInterval;
                break;
        }

        EmitNoise(transform.position, radius, loudness);
        nextNoiseTime = Time.time + interval;
    }

    private void EmitNoise(Vector3 position, float radius, float loudness)
    {
        Collider[] hits = Physics.OverlapSphere(
            position,
            radius,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            INoiseListener listener = hits[i].GetComponentInParent<INoiseListener>();
            if (listener != null)
            {
                listener.OnHeardNoise(position, loudness);
            }
        }

        if (debugNoise)
        {
            Debug.Log($"Movement noise: {currentMode}, radius={radius:F1}, loudness={loudness:F2}");
        }
    }

    private float GetCurrentRadius()
    {
        switch (GetCurrentMode())
        {
            case MovementNoiseMode.Sprint:
                return sprintRadius;
            case MovementNoiseMode.CrouchWalk:
                return crouchRadius;
            case MovementNoiseMode.ProneCrawl:
                return proneRadius;
            default:
                return standWalkRadius;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawCurrentNoiseRadius)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetCurrentRadius());
    }
}