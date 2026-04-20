using UnityEngine;

public class FootstepController : MonoBehaviour
{
    public enum FootstepVariationMode
    {
        SingleClipWithPitch,
        MultiClipNoRepeat
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;

    [Header("Landing Audio")]
    public bool enableLandingSound = true;
    public AudioClip landingClip;
    public float landingVolumeMultiplier = 1f;
    public float minAirTimeForLandingSound = 0.08f;
    private float airTimer;
    [Header("Variation")]
    public FootstepVariationMode variationMode = FootstepVariationMode.SingleClipWithPitch;

    [Header("Pitch Variation")]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Header("Volume Variation")]
    public bool randomizeVolume = true;
    public float minVolume = 0.9f;
    public float maxVolume = 1f;

    [Header("Player Stance Audio")]
    public bool useCrouchVolumeModifier = false;
    [Range(0f, 1f)]
    public float crouchVolumeMultiplier = 0.5f;
    public FpsController PlayerController;

    [Header("Step Settings")]
    public float stepInterval = 0.5f;
    public float sprintStepInterval = 0.3f;
    public float crouchStepInterval = 0.7f;
    public float minMoveSpeed = 0.1f;

    [Header("Ground Check")]
    public bool requireGrounded = true;
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.2f;

    private int lastClipIndex = -1;
    private float stepTimer;
    private Vector3 lastPosition;
    private bool wasMovingLastFrame;

    private bool wasGroundedLastFrame;

    private void Start()
    {
        if (PlayerController == null)
            PlayerController = GetComponent<FpsController>();

        lastPosition = transform.position;
        wasGroundedLastFrame = IsGrounded();
    }

    private void Update()
    {
        HandleLandingSound();
        HandleFootsteps();
    }

    private void HandleLandingSound()
    {
        bool groundedNow = IsGrounded();

        if (!groundedNow)
        {
            airTimer += Time.deltaTime;
        }

        if (enableLandingSound && landingClip != null && audioSource != null)
        {
            if (!wasGroundedLastFrame && groundedNow)
            {
                if (airTimer >= minAirTimeForLandingSound)
                {
                    PlayLandingSound();
                }

                airTimer = 0f;
            }
        }

        if (groundedNow)
        {
            airTimer = 0f;
        }

        wasGroundedLastFrame = groundedNow;
    }

    private void HandleFootsteps()
    {
        if (PlayerController != null)
        {
            if (PlayerController.stance == FpsController.Stance.Prone)
            {
                stepTimer = 0f;
                wasMovingLastFrame = false;
                return;
            }

            if (requireGrounded && !PlayerController.IsGrounded)
            {
                stepTimer = 0f;
                wasMovingLastFrame = false;
                return;
            }

            bool isMovingNow = PlayerController.IsMoving;

            if (!isMovingNow)
            {
                stepTimer = 0f;
                wasMovingLastFrame = false;
                return;
            }

            if (!wasMovingLastFrame && isMovingNow)
            {
                stepTimer = 0f;
            }

            wasMovingLastFrame = true;

            float interval = stepInterval;

            if (PlayerController.stance == FpsController.Stance.Crouch)
                interval = crouchStepInterval;
            else if (PlayerController.IsSprintingNow)
                interval = sprintStepInterval;

            stepTimer += Time.deltaTime;

            if (stepTimer >= interval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }

            return;
        }

        float speed = GetSpeed();

        if (requireGrounded && !IsGrounded())
        {
            stepTimer = 0f;
            wasMovingLastFrame = false;
            return;
        }

        if (speed < minMoveSpeed)
        {
            stepTimer = 0f;
            wasMovingLastFrame = false;
            return;
        }

        if (!wasMovingLastFrame)
            stepTimer = 0f;

        wasMovingLastFrame = true;

        stepTimer += Time.deltaTime;

        if (stepTimer >= stepInterval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }

    private float GetSpeed()
    {
        Vector3 delta = transform.position - lastPosition;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
        return speed;
    }

    private bool IsGrounded()
    {
        if (PlayerController != null)
            return PlayerController.IsGrounded;

        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    public void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        if (PlayerController != null && PlayerController.stance == FpsController.Stance.Prone)
            return;

        AudioClip clipToPlay = null;

        switch (variationMode)
        {
            case FootstepVariationMode.SingleClipWithPitch:
                clipToPlay = GetSingleClipWithPitch();
                break;

            case FootstepVariationMode.MultiClipNoRepeat:
                clipToPlay = GetMultiClipNoRepeat();
                break;
        }

        if (clipToPlay == null)
            return;

        float finalVolume = randomizeVolume
            ? Random.Range(minVolume, maxVolume)
            : 1f;

        if (PlayerController != null &&
            useCrouchVolumeModifier &&
            PlayerController.stance == FpsController.Stance.Crouch)
        {
            finalVolume *= crouchVolumeMultiplier;
        }

        audioSource.volume = finalVolume;
        audioSource.PlayOneShot(clipToPlay);
    }

    public void PlayLandingSound()
    {
        if (audioSource == null || landingClip == null)
            return;

        float finalVolume = landingVolumeMultiplier;

        if (PlayerController != null &&
            useCrouchVolumeModifier &&
            PlayerController.stance == FpsController.Stance.Crouch)
        {
            finalVolume *= crouchVolumeMultiplier;
        }

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(landingClip, finalVolume);
    }

    private AudioClip GetSingleClipWithPitch()
    {
        if (footstepClips == null || footstepClips.Length == 0 || footstepClips[0] == null)
            return null;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        return footstepClips[0];
    }

    private AudioClip GetMultiClipNoRepeat()
    {
        audioSource.pitch = 1f;

        if (footstepClips == null || footstepClips.Length == 0)
            return null;

        if (footstepClips.Length == 1)
        {
            lastClipIndex = 0;
            return footstepClips[0];
        }

        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return footstepClips[index];
    }
}