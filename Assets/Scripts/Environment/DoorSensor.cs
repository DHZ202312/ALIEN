using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AutoDoorSensor : MonoBehaviour
{
    public DoorController door;
    public DoorSoundController doorSound;
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

        if (doorSound == null)
            doorSound = GetComponentInParent<DoorSoundController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (door == null)
            return;

        // 门锁着：播放错误提示音，不执行开门
        if (door.isLocked)
        {
            if (doorSound != null)
                doorSound.PlayLockedSound();

            return;
        }

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

        if (door == null)
            return;

        // 锁着时 Stay 不重复调用开门逻辑
        if (door.isLocked)
            return;

        door.NotifyPlayerStillInside();
    }
}