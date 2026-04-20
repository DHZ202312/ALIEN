using UnityEngine;

public class VentCrawlSoundPatrol : MonoBehaviour
{
    [Header("Path")]
    public Transform[] patrolPoints;
    public float moveSpeed = 1.5f;
    public float arriveDistance = 0.05f;
    public bool snapToPointOnArrive = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] crawlClips;

    [Header("Step Timing")]
    public float soundInterval = 0.45f;
    public bool playSoundOnlyWhileMoving = true;

    [Header("Pitch Variation")]
    public bool randomizePitch = true;
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Header("Volume Variation")]
    public bool randomizeVolume = false;
    public float minVolume = 0.9f;
    public float maxVolume = 1f;

    [Header("End Behaviour")]
    public bool disableGameObjectAtEnd = true;
    public bool stopAudioAtEnd = true;

    private int currentPointIndex = 0;
    private float soundTimer = 0f;
    private int lastClipIndex = -1;
    private bool finished = false;

    [Header("Vent Event")]
    public VentCover targetVentCover;

    private void Awake()
    {
        if (audioSource == null)
            TryGetComponent(out audioSource);
    }
    private void Start()
    {
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
        {
            transform.position = patrolPoints[0].position;
            currentPointIndex = 1;
        }
    }

    private void Update()
    {
        if (finished)
            return;

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (currentPointIndex >= patrolPoints.Length)
        {
            FinishPatrol();
            return;
        }

        Transform target = patrolPoints[currentPointIndex];
        if (target == null)
        {
            currentPointIndex++;
            return;
        }

        bool isMovingThisFrame = MoveTowardsTarget(target);

        if (!playSoundOnlyWhileMoving || isMovingThisFrame)
        {
            soundTimer += Time.deltaTime;

            if (soundTimer >= soundInterval)
            {
                PlayCrawlSound();
                soundTimer = 0f;
            }
        }
        else
        {
            soundTimer = 0f;
        }
    }

    private bool MoveTowardsTarget(Transform target)
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = target.position;

        Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
        transform.position = newPos;

        Vector3 flatDir = targetPos - currentPos;
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            transform.forward = flatDir.normalized;
        }

        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance <= arriveDistance)
        {
            if (snapToPointOnArrive)
                transform.position = targetPos;

            currentPointIndex++;

            if (currentPointIndex >= patrolPoints.Length)
            {
                FinishPatrol();
            }

            return false;
        }

        return true;
    }

    private void PlayCrawlSound()
    {
        if (audioSource == null || crawlClips == null || crawlClips.Length == 0)
            return;

        AudioClip clip = GetClipNoImmediateRepeat();
        if (clip == null)
            return;

        audioSource.pitch = randomizePitch
            ? Random.Range(minPitch, maxPitch)
            : 1f;

        audioSource.volume = randomizeVolume
            ? Random.Range(minVolume, maxVolume)
            : 1f;

        audioSource.PlayOneShot(clip);
    }

    private AudioClip GetClipNoImmediateRepeat()
    {
        if (crawlClips == null || crawlClips.Length == 0)
            return null;

        if (crawlClips.Length == 1)
        {
            lastClipIndex = 0;
            return crawlClips[0];
        }

        int index;
        do
        {
            index = Random.Range(0, crawlClips.Length);
        }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return crawlClips[index];
    }

    private void FinishPatrol()
    {
        if (finished)
            return;

        finished = true;

        if (targetVentCover != null)
            targetVentCover.ventWaitDrop();

        if (stopAudioAtEnd && audioSource != null)
            audioSource.Stop();

        if (disableGameObjectAtEnd)
            gameObject.SetActive(false);
    }

    public void StartPatrolFromFirstPoint()
    {
        finished = false;
        currentPointIndex = 0;
        soundTimer = 0f;
        lastClipIndex = -1;

        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
            transform.position = patrolPoints[0].position;

        gameObject.SetActive(true);
    }
}