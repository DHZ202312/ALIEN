using System.Collections;
using UnityEngine;

public class VentCover : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private int currentHp = 2;

    [Header("Damage Threshold")]
    [SerializeField] private int heavyAttackDamageThreshold = 25;

    [Header("Visuals")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material intactMaterial;
    [SerializeField] private Material damagedMaterial;
    public Renderer othersideRenderer;

    [Header("Break Settings")]
    [SerializeField] private GameObject brokenVersion;
    [SerializeField] private bool destroyWholeObjectOnBreak = false;
    [SerializeField] private float destroyDelay = 0f;

    [Header("Optional")]
    [SerializeField] private Collider[] coverCollider;
    [SerializeField] private GameObject intactVisualRoot;
    public float secondsBeforeDrop = 5f;
    public EnemyAnim animController;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] audioClips;
    // audioClips[0] = hit sound
    // audioClips[1] = land sound

    [Header("Audio Pitch Variation")]
    [SerializeField] private bool randomizeHitPitch = true;
    [SerializeField] private float hitMinPitch = 0.95f;
    [SerializeField] private float hitMaxPitch = 1.05f;

    [SerializeField] private bool randomizeLandingPitch = true;
    [SerializeField] private float landingMinPitch = 0.92f;
    [SerializeField] private float landingMaxPitch = 1.08f;

    [Header("Landing Detection")]
    [SerializeField] private bool enableLandingSound = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private bool useGroundTagCheck = false;

    private bool isBroken = false;
    private bool hasShownDamagedState = false;
    private Rigidbody rb;

    private bool hasPlayedLandingSound = false;
    private bool hasStartedFalling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHp = Mathf.Clamp(currentHp, 1, maxHp);

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (coverCollider == null || coverCollider.Length == 0)
        {
            Collider selfCol = GetComponent<Collider>();
            if (selfCol != null)
                coverCollider = new Collider[] { selfCol };
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        UpdateVisualState();
    }

    public void TakeDamage(int damage)
    {
        if (isBroken)
            return;

        PlayHitSound();

        if (damage >= heavyAttackDamageThreshold)
        {
            BreakCover();
            return;
        }

        currentHp -= 1;

        if (currentHp <= 0)
        {
            BreakCover();
        }
        else
        {
            ShowDamagedState();
        }
    }

    private void ShowDamagedState()
    {
        if (hasShownDamagedState)
            return;

        hasShownDamagedState = true;

        if (targetRenderer != null && damagedMaterial != null)
            targetRenderer.material = damagedMaterial;

        if (othersideRenderer != null && damagedMaterial != null)
            othersideRenderer.material = damagedMaterial;
    }

    private void UpdateVisualState()
    {
        if (targetRenderer == null)
            return;

        if (currentHp >= maxHp)
        {
            if (intactMaterial != null)
            {
                targetRenderer.material = intactMaterial;

                if (othersideRenderer != null)
                    othersideRenderer.material = intactMaterial;
            }
        }
        else
        {
            if (damagedMaterial != null)
            {
                targetRenderer.material = damagedMaterial;

                if (othersideRenderer != null)
                    othersideRenderer.material = damagedMaterial;
            }
        }
    }

    public void ventDrop()
    {
        if (rb == null)
            return;

        hasStartedFalling = true;
        hasPlayedLandingSound = false;
        rb.isKinematic = false;
    }

    public void ventWaitDrop()
    {
        StartCoroutine(waitthendrop());
    }

    IEnumerator waitthendrop()
    {
        yield return new WaitForSeconds(secondsBeforeDrop);

        if (rb != null)
        {
            hasStartedFalling = true;
            hasPlayedLandingSound = false;
            rb.isKinematic = false;
        }
    }

    IEnumerator waitthenActivateController()
    {
        yield return new WaitForSeconds(3f);
        animController.enabled = true;
    }

    private void BreakCover()
    {
        if (isBroken)
            return;

        isBroken = true;

        if (brokenVersion != null)
        {
            brokenVersion.SetActive(true);
            // 如果 brokenVersion 是 prefab，可改成 Instantiate
        }

        if (coverCollider != null)
        {
            foreach (Collider cc in coverCollider)
            {
                if (cc != null)
                    cc.enabled = false;
            }
        }

        if (intactVisualRoot != null)
        {
            intactVisualRoot.SetActive(false);
        }
        else if (targetRenderer != null)
        {
            targetRenderer.enabled = false;

            if (othersideRenderer != null)
                othersideRenderer.enabled = false;
        }

        if (destroyWholeObjectOnBreak)
        {
            if (othersideRenderer != null)
                Destroy(othersideRenderer.gameObject, destroyDelay);

            Destroy(gameObject, destroyDelay);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableLandingSound)
            return;

        if (!hasStartedFalling)
            return;

        if (hasPlayedLandingSound)
            return;

        if (IsGroundCollision(collision))
        {
            PlayLandingSound();
            hasPlayedLandingSound = true;
            hasStartedFalling = false;
        }
    }

    private bool IsGroundCollision(Collision collision)
    {
        if (useGroundTagCheck)
            return collision.collider.CompareTag(groundTag);

        return ((1 << collision.gameObject.layer) & groundLayer) != 0;
    }

    private void PlayHitSound()
    {
        if (audioSource == null)
            return;

        if (audioClips == null || audioClips.Length <= 0 || audioClips[0] == null)
            return;

        audioSource.pitch = randomizeHitPitch
            ? Random.Range(hitMinPitch, hitMaxPitch)
            : 1f;

        audioSource.PlayOneShot(audioClips[0]);
    }

    private void PlayLandingSound()
    {
        if (audioSource == null)
            return;

        if (audioClips == null || audioClips.Length <= 1 || audioClips[1] == null)
            return;

        audioSource.pitch = randomizeLandingPitch
            ? Random.Range(landingMinPitch, landingMaxPitch)
            : 1f;

        audioSource.PlayOneShot(audioClips[1]);
    }
}