using UnityEngine;

public class PowerUnit : MonoBehaviour
{
    public Material[] materials;
    public bool isDrained = false;
    public GameObject smoke;

    public GameObject ltc;
    public GameObject powerback;

    private MeshRenderer mr;
    private ThrowableItem throwable;
    private Rigidbody rb;

    public PowerSource currentPowerSource;

    [Header("Eject Force")]
    public float ejectForce = 6f;
    public float ejectUpForce = 2f;
    public ForceMode forceMode = ForceMode.Impulse;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        throwable = GetComponentInParent<ThrowableItem>();
        rb = GetComponentInParent<Rigidbody>();
    }

    public void PowerDrain()
    {
        isDrained = true;
        smoke.SetActive(true);
        ltc.SetActive(true);
        mr.material = materials[1];

        if (throwable != null)
            throwable.enabled = true;

        ApplyEjectForce();
    }
    public void PowerOff()
    {
        ltc.SetActive(true);
        if (throwable != null)
            throwable.enabled = true;
    }

    void ApplyEjectForce()
    {
        if (rb == null) return;

        // 世界空间 -Z + 一点向上（更有“炸飞感”）
        Vector3 force = Vector3.back * ejectForce + Vector3.up * ejectUpForce;

        rb.AddForce(force, forceMode);
    }

    public void RestorePower()
    {
        powerback.SetActive(true);
    }
}
