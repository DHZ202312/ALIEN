using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Door Parts")]
    public Transform leftDoor;
    public bool isDoubleDoor = true;
    public Transform rightDoor;

    [Header("Movement")]
    public float openDistance = 1.2f;
    public float openDuration = 1.5f;

    [Header("Auto Door")]
    public bool autoDoorEnabled = true;
    public float closeDelay = 3f;

    [Header("State")]
    private bool isMoving;
    private bool targetIsOpen;
    public bool isOpen;
    public bool isLocked;
    public Material[] mat;
    public MeshRenderer[] mr;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private Coroutine currentRoutine;

    private int playersInRange = 0;
    private Coroutine autoCloseRoutine;

    void Start()
    {
        Vector3 leftMoveDir = leftDoor.parent.InverseTransformDirection(
            leftDoor.TransformDirection(Vector3.left)
        ).normalized;

        leftClosedPos = leftDoor.localPosition;
        leftOpenPos = leftClosedPos + leftMoveDir * openDistance;

        if (isDoubleDoor)
        {
            Vector3 rightMoveDir = rightDoor.parent.InverseTransformDirection(
                rightDoor.TransformDirection(-Vector3.left)
            ).normalized;

            rightClosedPos = rightDoor.localPosition;
            rightOpenPos = rightClosedPos + rightMoveDir * openDistance;
        }
    }

    public void ToggleDoor()
    {
        if (isLocked)
            return;

        if (!isOpen) OpenDoor();
        else CloseDoor();
    }

    public void OpenDoor()
    {
        if (isLocked)
            return;

        // 已经开着，或者已经正在朝“打开”运动
        if (isOpen || (isMoving && targetIsOpen))
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        targetIsOpen = true;
        currentRoutine = StartCoroutine(MoveDoor(true));
    }

    public void CloseDoor()
    {
        // 已经关着，或者已经正在朝“关闭”运动
        if ((!isOpen && !isMoving) || (isMoving && !targetIsOpen))
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        targetIsOpen = false;
        currentRoutine = StartCoroutine(MoveDoor(false));
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    public void LockDoor()
    {
        isLocked = true;
        foreach (MeshRenderer ren in mr) ren.material = mat[1];
    }

    public void UnlockDoor()
    {
        isLocked = false;
        foreach (MeshRenderer ren in mr) ren.material = mat[0];
    }

    public void NotifyPlayerEntered()
    {
        if (!autoDoorEnabled)
            return;

        playersInRange++;

        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        if (!isLocked)
        {
            OpenDoor();
        }
    }

    public void NotifyPlayerExited()
    {
        if (!autoDoorEnabled)
            return;

        playersInRange = Mathf.Max(0, playersInRange - 1);

        if (playersInRange <= 0)
        {
            StartAutoCloseCountdown();
        }
    }

    public void NotifyPlayerStillInside()
    {
        if (!autoDoorEnabled)
            return;

        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        if (!isLocked && !isOpen)
        {
            OpenDoor();
        }
    }

    private void StartAutoCloseCountdown()
    {
        if (autoCloseRoutine != null)
            StopCoroutine(autoCloseRoutine);

        autoCloseRoutine = StartCoroutine(AutoCloseAfterDelay());
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);

        if (playersInRange <= 0)
        {
            CloseDoor();
        }

        autoCloseRoutine = null;
    }

    private IEnumerator MoveDoor(bool opening)
    {
        isMoving = true;
        targetIsOpen = opening;

        float time = 0f;

        Vector3 leftStart = leftDoor.localPosition;
        Vector3 leftTarget = opening ? leftOpenPos : leftClosedPos;

        Vector3 rightStart = isDoubleDoor ? rightDoor.localPosition : Vector3.zero;
        Vector3 rightTarget = isDoubleDoor
            ? (opening ? rightOpenPos : rightClosedPos)
            : Vector3.zero;

        while (time < openDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / openDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, easedT);

            if (isDoubleDoor)
                rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, easedT);

            yield return null;
        }

        leftDoor.localPosition = leftTarget;
        if (isDoubleDoor)
            rightDoor.localPosition = rightTarget;

        isOpen = opening;
        isMoving = false;
        currentRoutine = null;
    }
}