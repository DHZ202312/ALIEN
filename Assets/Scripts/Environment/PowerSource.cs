using System.Collections;
using UnityEngine;

public class PowerSource : MonoBehaviour
{
    [Header("Refs")]
    public Transform socketPoint;
    //public Collider triggerCollider;

    [Header("Options")]
    public bool disableThrowableItemOnInsert = true;
    public bool makeInsertedBodyKinematic = true;
    public bool disableInsertedColliders = false;
    public bool snapRotation = true;
    public bool allowOnlyOneUnit = true;
    public bool parentToSocketPoint = false;

    [Header("Debug")]
    public bool debugLog = false;

    public PowerUnit insertedUnit;

    private void Reset()
    {
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && cols[i] != GetComponent<Collider>())
            {
                if (cols[i].isTrigger)
                {
                    //triggerCollider = cols[i];
                    break;
                }
            }
        }
    }

    public bool TryInsertHeldItem(ThrowablePickupController pickupController)
    {
        if (pickupController == null || pickupController.heldItem == null)
            return false;

        ThrowableItem held = pickupController.heldItem;
        PowerUnit unit = held.GetComponentInChildren<PowerUnit>(true);

        if (unit == null)
            return false;

        // 按你这次的要求：只有 isDrained == false 才能插
        if (unit.isDrained)
            return false;

        if (allowOnlyOneUnit && insertedUnit != null)
            return false;

        // 先让物体脱手
        ThrowableItem releasedItem = pickupController.ReleaseHeldItemForSocket();
        if (releasedItem == null)
            return false;

        Transform itemTransform = releasedItem.transform;

        if (parentToSocketPoint && socketPoint != null)
        {
            itemTransform.SetParent(socketPoint);
            itemTransform.localPosition = Vector3.zero;

            if (snapRotation)
                itemTransform.localRotation = Quaternion.identity;
        }
        else if (socketPoint != null)
        {
            itemTransform.position = socketPoint.position;

            if (snapRotation)
                itemTransform.rotation = socketPoint.rotation;
        }

        if (disableThrowableItemOnInsert)
        {
            releasedItem.enabled = false;
        }

        Rigidbody rb = releasedItem.GetComponent<Rigidbody>();
        if (rb == null)
            rb = releasedItem.GetComponentInParent<Rigidbody>();

        if (rb == null)
            rb = releasedItem.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (makeInsertedBodyKinematic)
                rb.isKinematic = true;
        }

        //if (disableInsertedColliders)
        //{
        //    Collider[] colliders = releasedItem.GetComponentsInChildren<Collider>(true);
        //    for (int i = 0; i < colliders.Length; i++)
        //    {
        //        if (colliders[i] != null && colliders[i] != triggerCollider)
        //            colliders[i].enabled = false;
        //    }
        //}

        insertedUnit = unit;
        StartCoroutine(waitthenrestore());

        if (debugLog)
            Debug.Log($"{name}: Inserted held item {releasedItem.name}");

        return true;
    }

    public PowerUnit GetInsertedUnit()
    {
        return insertedUnit;
    }

    public void ClearInsertedUnit()
    {
        insertedUnit = null;
    }
    public void OnInsertedUnitRemoved(PowerUnit unit)
    {
        insertedUnit.PowerOff();
        insertedUnit = null;

        if (debugLog)
            Debug.Log($"{name}: Inserted unit removed: {unit.name}");
    }
    IEnumerator waitthenrestore()
    {
        yield return new WaitForSeconds(1.5f);
        insertedUnit.RestorePower();
    }
}
