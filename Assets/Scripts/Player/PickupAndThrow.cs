using UnityEngine;

public class ThrowablePickupController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform leftHandAnchor;
    public LineRenderer trajectoryLine;

    [Header("Pickup")]
    public float pickupDistance = 3f;
    public LayerMask pickupMask;

    [Header("Throw Charge")]
    public float maxChargeTime = 1f;
    public int trajectorySteps = 30;
    public float trajectoryTimeStep = 0.05f;

    [Header("Drop")]
    public float silentDropDownOffset = 0.25f;

    [Header("Trajectory Collision")]
    public LayerMask trajectoryCollisionMask;
    public float trajectoryCollisionRadius = 0.02f;

    public ThrowableItem heldItem;
    private bool isChargingThrow;
    private float throwChargeStartTime;
    [Header("else")]
    public GameObject Pcrowbar;
    private MeleeAttack ma;

    private void Awake()
    {
        ma = GetComponent<MeleeAttack>();
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    private void Update()
    {
        HandlePowerSourceInteract();
        HandlePickup();
        HandleSilentDrop();
        HandleThrowInput();
        UpdateTrajectoryPreview();
    }
    private void HandlePowerSourceInteract()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (heldItem == null)
            return;

        if (!Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                pickupDistance,
                pickupMask))
            return;

        PowerSource powerSource = hit.collider.GetComponentInParent<PowerSource>();
        if (powerSource == null)
            return;

        powerSource.TryInsertHeldItem(this);
    }
    public ThrowableItem ReleaseHeldItemForSocket()
    {
        if (heldItem == null)
            return null;

        CancelThrowCharge();
        HideTrajectory();

        ThrowableItem item = heldItem;
        heldItem = null;

        item.isHeld = false;
        item.transform.SetParent(null);

        return item;
    }
    private void HandlePickup()
    {
        if (heldItem != null)
            return;

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (!Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                pickupDistance,
                pickupMask))
            return;
        pickupCrowbar crowbar = hit.collider.GetComponentInParent<pickupCrowbar>();
        if (crowbar != null)
        {
            Pcrowbar.SetActive(true);
            ma.enabled = true;
            crowbar.selfdes();
        }
        ThrowableItem item = hit.collider.GetComponentInParent<ThrowableItem>();
        if (item == null || item.isHeld)
            return;

        PowerUnit unit = item.GetComponentInChildren<PowerUnit>(true);
        if (unit != null && unit.currentPowerSource != null)
        {
            unit.currentPowerSource.OnInsertedUnitRemoved(unit);
            unit.currentPowerSource = null;
        }

        heldItem = item;
        heldItem.OnPickedUp(leftHandAnchor);
    }

    private void HandleSilentDrop()
    {
        if (heldItem == null)
            return;

        if (!Input.GetKeyDown(KeyCode.G))
            return;

        CancelThrowCharge();

        Vector3 dropPos = leftHandAnchor.position + Vector3.down * silentDropDownOffset;
        heldItem.transform.position = dropPos;

        heldItem.OnDroppedSilently();
        heldItem = null;

        HideTrajectory();
    }

    private void HandleThrowInput()
    {
        if (heldItem == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            isChargingThrow = true;
            throwChargeStartTime = Time.time;
        }

        if (Input.GetMouseButtonUp(0) && isChargingThrow)
        {
            ThrowHeldItem();
        }
    }

    private void ThrowHeldItem()
    {
        if (heldItem == null)
            return;

        float charge01 = GetThrowCharge01();
        float forceMultiplier = Mathf.Lerp(
            heldItem.minThrowMultiplier,
            heldItem.maxThrowMultiplier,
            charge01
        );

        Vector3 throwVelocity =
            playerCamera.transform.forward * (heldItem.throwForce * forceMultiplier) +
            Vector3.up * heldItem.upwardForce;

        ThrowableItem itemToThrow = heldItem;
        heldItem = null;

        itemToThrow.OnThrown(throwVelocity);

        CancelThrowCharge();
        HideTrajectory();
    }

    private void UpdateTrajectoryPreview()
    {
        if (!isChargingThrow || heldItem == null || trajectoryLine == null)
        {
            HideTrajectory();
            return;
        }

        trajectoryLine.enabled = true;

        float charge01 = GetThrowCharge01();
        float forceMultiplier = Mathf.Lerp(
            heldItem.minThrowMultiplier,
            heldItem.maxThrowMultiplier,
            charge01
        );

        Vector3 startPos = leftHandAnchor.position;
        Vector3 startVel =
            playerCamera.transform.forward * (heldItem.throwForce * forceMultiplier) +
            Vector3.up * heldItem.upwardForce;

        Vector3 prevPos = startPos;

        trajectoryLine.positionCount = trajectorySteps;
        trajectoryLine.SetPosition(0, startPos);

        int finalCount = 1;

        for (int i = 1; i < trajectorySteps; i++)
        {
            float t = i * trajectoryTimeStep;
            Vector3 nextPos = startPos + startVel * t + 0.5f * Physics.gravity * t * t;

            bool hitSomething = false;

            if (trajectoryCollisionRadius <= 0f)
            {
                if (Physics.Linecast(prevPos, nextPos, out RaycastHit hit, trajectoryCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    nextPos = hit.point;
                    hitSomething = true;
                }
            }
            else
            {
                Vector3 segment = nextPos - prevPos;
                float distance = segment.magnitude;

                if (distance > 0.0001f)
                {
                    if (Physics.SphereCast(
                            prevPos,
                            trajectoryCollisionRadius,
                            segment.normalized,
                            out RaycastHit hit,
                            distance,
                            trajectoryCollisionMask,
                            QueryTriggerInteraction.Ignore))
                    {
                        nextPos = hit.point;
                        hitSomething = true;
                    }
                }
            }

            trajectoryLine.SetPosition(i, nextPos);
            finalCount = i + 1;

            if (hitSomething)
                break;

            prevPos = nextPos;
        }

        trajectoryLine.positionCount = finalCount;
    }

    private float GetThrowCharge01()
    {
        return Mathf.Clamp01((Time.time - throwChargeStartTime) / maxChargeTime);
    }

    private void CancelThrowCharge()
    {
        isChargingThrow = false;
    }

    private void HideTrajectory()
    {
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    public bool HasHeldItem()
    {
        return heldItem != null;
    }
}
