using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AutoDoorSensor : MonoBehaviour
{
    public DoorController door;
    public string playerTag = "Player";
    public float stayCheckInterval = 0.2f;

    private Collider triggerCol;
    private float nextStayCheckTime;

    private void Awake()
    {
        triggerCol = GetComponent<Collider>();
        triggerCol.isTrigger = true;

        if (door == null)
            door = GetComponentInParent<DoorController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (door != null)
            door.NotifyPlayerEntered();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (door != null)
            door.NotifyPlayerExited();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (Time.time < nextStayCheckTime)
            return;

        nextStayCheckTime = Time.time + stayCheckInterval;

        if (door != null)
            door.NotifyPlayerStillInside();
    }
}
