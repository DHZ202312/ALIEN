using UnityEngine;

public class DragStuff : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 2f;
    public float mouseSensitivity = 5f;

    DraggableObject currentTarget;
    Vector3 hitPoint;

    bool isDragging;

    void Update()
    {
        if (!isDragging)
        {
            TryStartDrag();
        }
        else
        {
            HandleDrag();
        }
    }

    void TryStartDrag()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = cam.ViewportPointToRay(Vector3.one * 0.5f);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            DraggableObject draggable = hit.collider.GetComponentInParent<DraggableObject>();

            if (draggable != null)
            {
                currentTarget = draggable;
                hitPoint = hit.point;
                isDragging = true;

                LockPlayerLook(true);
            }
        }
    }

    void HandleDrag()
    {
        // 1️⃣ 松开鼠标自动退出
        if (Input.GetMouseButtonUp(0))
        {
            StopDrag();
            return;
        }

        // 2  超出距离自动退出
        float distance = Vector3.Distance(cam.transform.position, currentTarget.transform.position);
        if (distance > interactDistance)
        {
            StopDrag();
            return;
        }

        // ===== 正常拖拽逻辑 =====

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 force =
            cam.transform.forward * mouseY +
            cam.transform.right * mouseX;

        currentTarget.ApplyDragForce(force, hitPoint);
    }


    void LockPlayerLook(bool state)
    {
        var controller = GetComponent<FpsController>();
        if (controller != null)
            controller.externalLookLock = state;
    }
    void StopDrag()
    {
        isDragging = false;
        LockPlayerLook(false);
        currentTarget = null;
    }

}

