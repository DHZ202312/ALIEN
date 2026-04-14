using UnityEngine;

public class ThrowableItem : MonoBehaviour
{
    [Header("Held Pose")]
    public Vector3 heldLocalPosition;
    public Vector3 heldLocalEuler;

    [Header("Throw")]
    public float throwForce = 12f;
    public float upwardForce = 1.5f;
    public float minThrowMultiplier = 0.35f;
    public float maxThrowMultiplier = 1f;

    [Header("Noise")]
    public float noiseRadius = 8f;
    public float minImpactSpeedForNoise = 2f;

    [Header("References")]
    public Rigidbody rb;
    public Collider[] colliders;

    [HideInInspector] public bool isHeld;
    [HideInInspector] public bool suppressNextImpactNoise;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void OnPickedUp(Transform holdAnchor)
    {
        isHeld = true;
        suppressNextImpactNoise = false;

        transform.SetParent(holdAnchor);
        transform.localPosition = heldLocalPosition;
        transform.localEulerAngles = heldLocalEuler;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(false);
    }

    public void OnThrown(Vector3 velocity)
    {
        isHeld = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 8f;
        }

        suppressNextImpactNoise = false;
        SetCollidersEnabled(true);
    }

    public void OnDroppedSilently()
    {
        isHeld = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        suppressNextImpactNoise = true;
        SetCollidersEnabled(true);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled;
        }
    }
}
