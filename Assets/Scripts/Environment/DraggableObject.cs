using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DraggableObject : MonoBehaviour
{
    public float dragForceMultiplier = 150f;
    public bool lockX;
    public bool lockY;
    public bool lockZ;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ApplyDragForce(Vector3 worldForce, Vector3 hitPoint)
    {
        Vector3 force = worldForce;

        if (lockX) force.x = 0;
        if (lockY) force.y = 0;
        if (lockZ) force.z = 0;

        rb.AddForceAtPosition(force * dragForceMultiplier, hitPoint, ForceMode.Force);
    }
}

