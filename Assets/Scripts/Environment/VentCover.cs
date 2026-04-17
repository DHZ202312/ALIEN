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
    [SerializeField] private Collider coverCollider;
    [SerializeField] private GameObject intactVisualRoot;
    public float secondsBeforeDrop = 5f;
    public EnemyAnim animController;

    private bool isBroken = false;
    private bool hasShownDamagedState = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHp = Mathf.Clamp(currentHp, 1, maxHp);

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (coverCollider == null)
            coverCollider = GetComponent<Collider>();

        UpdateVisualState();
    }

    public void TakeDamage(int damage)
    {
        if (isBroken)
            return;

        // 重击直接碎
        if (damage >= heavyAttackDamageThreshold)
        {
            BreakCover();
            return;
        }

        // 轻击扣1点
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
        {
            targetRenderer.material = damagedMaterial;
            othersideRenderer.material = damagedMaterial;
        }
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
                othersideRenderer.material = intactMaterial;
            }
                
            
        }
        else
        {
            if (damagedMaterial != null)
            {
                targetRenderer.material = damagedMaterial;
                othersideRenderer.material= damagedMaterial;
            }
        }
    }
    public void ventDrop()
    {
        rb.isKinematic = false;
    }
    public void ventWaitDrop()
    {
        StartCoroutine(waitthendrop());
    }
    IEnumerator waitthendrop()
    {
        yield return new WaitForSeconds(secondsBeforeDrop);
        rb.isKinematic = false;
        StartCoroutine(waitthenActivateController());
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

        // 生成碎裂版
        if (brokenVersion != null)
        {
            brokenVersion.SetActive(true);

            // 如果碎裂版原本不在层级里，而是 prefab，
            // 可以改成 Instantiate 方式
            // Instantiate(brokenVersion, transform.position, transform.rotation);
        }

        // 关掉完整版碰撞
        if (coverCollider != null)
        {
            coverCollider.enabled = false;
        }

        // 隐藏完整版视觉
        if (intactVisualRoot != null)
        {
            intactVisualRoot.SetActive(false);
        }
        else if (targetRenderer != null)
        {
            targetRenderer.enabled = false;
        }

        if (destroyWholeObjectOnBreak)
        {
            Destroy(othersideRenderer.gameObject,destroyDelay);
            Destroy(gameObject, destroyDelay);
        }
    }
}