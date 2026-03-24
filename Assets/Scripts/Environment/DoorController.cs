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

    [Header("State")]
    public bool isOpen;

    Vector3 leftClosedPos;
    Vector3 rightClosedPos;

    Vector3 leftOpenPos;
    Vector3 rightOpenPos;

    Coroutine currentRoutine;
    void Start()
    {
        Vector3 leftMoveDir = leftDoor.parent.InverseTransformDirection(leftDoor.TransformDirection(Vector3.left)).normalized;
        
        leftClosedPos = leftDoor.localPosition;
        leftOpenPos = leftClosedPos + leftMoveDir * openDistance;
        if(isDoubleDoor)
        {
            Vector3 rightMoveDir = rightDoor.parent.InverseTransformDirection(rightDoor.TransformDirection(-Vector3.left)).normalized;
            rightClosedPos = rightDoor.localPosition;
            rightOpenPos = rightClosedPos + rightMoveDir * openDistance;
        }
    }
    public void ToggleDoor()
    {
        Debug.Log("ToggleDoor called", this);

        if (!isOpen) OpenDoor();
        else CloseDoor();

        Debug.Log("Door toggled: " + isOpen);
    }
    public void OpenDoor()
    {
        if (isOpen) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(MoveDoor(true));
    }

    public void CloseDoor()
    {
        if (!isOpen) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(MoveDoor(false));
    }
    
    IEnumerator MoveDoor(bool opening)
    {
        float time = 0f;

        Vector3 leftStart = leftDoor.localPosition;
        Vector3 leftTarget = opening ? leftOpenPos : leftClosedPos;

            Vector3 rightStart = isDoubleDoor ? rightDoor.localPosition:Vector3.zero;
            Vector3 rightTarget = isDoubleDoor ? (opening ? rightOpenPos : rightClosedPos):Vector3.zero;

        while (time < openDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / openDuration);

            // Âý ¡ú ¿ì ¡ú Âý
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, easedT);
            if(isDoubleDoor) rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, easedT);

            yield return null;
        }

        leftDoor.localPosition = leftTarget;
        if(isDoubleDoor) rightDoor.localPosition = rightTarget;

        isOpen = opening;
        currentRoutine = null;
    }
}

