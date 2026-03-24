using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Interface : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCam;
    public Canvas screenCanvas;

    [Header("Settings")]
    public LayerMask screenLayer;

    private GraphicRaycaster uiRaycaster;
    private EventSystem eventSystem;

    void Start()
    {
        if (playerCam == null)
            playerCam = Camera.main;

        uiRaycaster = screenCanvas.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        // 防止3D屏幕Collider阻挡UI
        if (uiRaycaster != null)
            uiRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
    }

    void Update()
    {
        CursorManager.Instance.SetZone(CursorManager.CursorZone.Computer);
        Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, screenLayer))
        {
            // 鼠标在电脑屏幕上

            if (Input.GetMouseButtonDown(0))
            {
                ClickUIAtWorldPoint(hit.point);
            }
        }
    }

    void ClickUIAtWorldPoint(Vector3 worldPoint)
    {
        if (!uiRaycaster || !eventSystem) return;

        Vector2 screenPos = playerCam.WorldToScreenPoint(worldPoint);

        PointerEventData ped = new PointerEventData(eventSystem)
        {
            position = screenPos,
            button = PointerEventData.InputButton.Left,
            clickCount = 1,
            pressPosition = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(ped, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[i].gameObject);

            if (handler != null)
            {
                ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
                return;
            }
        }
    }
}