using UnityEngine;

public class ThrowableNoiseEmitter : MonoBehaviour
{
    public ThrowableItem item;
    public LayerMask enemyMask;

    private void Reset()
    {
        item = GetComponent<ThrowableItem>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (item == null || item.isHeld)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < item.minImpactSpeedForNoise)
            return;

        if (item.suppressNextImpactNoise)
        {
            item.suppressNextImpactNoise = false;
            return;
        }

        MakeNoise(transform.position, item.noiseRadius);
    }

    private void MakeNoise(Vector3 position, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, enemyMask);

        for (int i = 0; i < hits.Length; i++)
        {
            INoiseListener listener = hits[i].GetComponentInParent<INoiseListener>();
            if (listener != null)
            {
                listener.OnHeardNoise(position);
            }
        }
    }
}