using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TerminalInteractor : MonoBehaviour
{
    public Camera playerCam;
    public float interactDistance = 2.5f;

    public LayerMask screenLayer;
    public Canvas screenCanvas;
    public RectTransform cursorUI;

    bool playerNearby;

    GraphicRaycaster uiRaycaster;
    EventSystem eventSystem;

    void Start()
    {
        uiRaycaster = screenCanvas.GetComponent<GraphicRaycaster>();
        playerCam = GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Camera>();
        eventSystem = EventSystem.current;

        // ✅ 关键：避免被 3D collider（你的屏幕）挡住 UI 点击
        if (uiRaycaster) uiRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
    }

    void Update()
    {
        float distance = Vector3.Distance(playerCam.transform.position, transform.position);
        playerNearby = distance <= interactDistance;

        cursorUI.gameObject.SetActive(playerNearby);
        if (!playerNearby) return;

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, screenLayer))
        {
            UpdateCursorPosition(hit);

            if (Input.GetMouseButtonDown(0))
            {
                ClickUIAtWorldPoint(hit.point);
            }
        }
    }

    void UpdateCursorPosition(RaycastHit hit)
    {
        Vector3 localHit = screenCanvas.transform.InverseTransformPoint(hit.point);
        RectTransform rect = screenCanvas.GetComponent<RectTransform>();

        float x = Mathf.Clamp(localHit.x, -rect.rect.width / 2, rect.rect.width / 2);
        float y = Mathf.Clamp(localHit.y, -rect.rect.height / 2, rect.rect.height / 2);

        cursorUI.anchoredPosition = new Vector2(x, y);
    }

    void ClickUIAtWorldPoint(Vector3 worldPoint)
    {
        if (!uiRaycaster || !eventSystem) return;

        Vector2 screenPos = playerCam.WorldToScreenPoint(worldPoint);

        var ped = new PointerEventData(eventSystem)
        {
            position = screenPos,
            button = PointerEventData.InputButton.Left,
            clickCount = 1,
            pressPosition = screenPos
        };

        var results = new List<RaycastResult>();
        uiRaycaster.Raycast(ped, results);

        // 🔎 调试：看看实际命中了谁
        // Debug.Log(results.Count == 0 ? "UI Raycast: none" : $"UI Raycast top: {results[0].gameObject.name}");

        for (int i = 0; i < results.Count; i++)
        {
            // ✅ 冒泡到最近的可点击父物体（Button 在父级也能点到）
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[i].gameObject);
            if (handler != null)
            {
                ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
                return;
            }
        }
    }
}
