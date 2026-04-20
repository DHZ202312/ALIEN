using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class TerminalInteractor : MonoBehaviour
{
    public Camera playerCam;
    public float interactDistance = 2.5f;

    public LayerMask screenLayer;
    public Canvas screenCanvas;
    public RectTransform cursorUI;

    [Header("Key Check")]
    public Transform leftHandHoldPoint;
    public string keyTag = "Key";
    public DoorController targetDoor;
    public Button unlockButton;

    [Header("Feedback UI")]
    public GameObject mainContent;          // 原本按钮等内容的总父物体
    public GameObject messagePanel;         // 提示文本面板
    public TextMeshProUGUI messageText;     // 提示文字
    public float messageDuration = 3f;
    public float elevatorDuration = 30f;

    private Coroutine messageRoutine;

    public bool IsHoveringScreen { get; private set; }

    bool playerNearby;

    GraphicRaycaster uiRaycaster;
    EventSystem eventSystem;

    void Start()
    {
        uiRaycaster = screenCanvas.GetComponent<GraphicRaycaster>();
        playerCam = GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Camera>();
        eventSystem = EventSystem.current;

        if (uiRaycaster) uiRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
    }

    void Update()
    {
        float distance = Vector3.Distance(playerCam.transform.position, transform.position);
        playerNearby = distance <= interactDistance;

        IsHoveringScreen = false;
        cursorUI.gameObject.SetActive(false);

        if (!playerNearby) return;

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, screenLayer))
        {
            IsHoveringScreen = true;
            cursorUI.gameObject.SetActive(true);

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

        for (int i = 0; i < results.Count; i++)
        {
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[i].gameObject);
            if (handler != null)
            {
                ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
                TryUnlockDoor(handler);
                return;
            }
        }
    }

    void TryUnlockDoor(GameObject clickedHandler)
    {
        if (targetDoor == null || unlockButton == null) return;
        if (clickedHandler != unlockButton.gameObject) return;

        if (HasKeyInLeftHand())
        {
            ShowMessage("Lockdown Lifted");
            StartCoroutine(CallElevatorRoutine("Calling Elevator..."));
        }
        else
        {
            ShowMessage("Unregistered User");
        }
    }
    void ShowMessage(string msg)
    {
        if (messageText == null || messagePanel == null || mainContent == null)
            return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ShowMessageRoutine(msg));
    }
    IEnumerator ShowMessageRoutine(string msg)
    {
        // 隐藏原本终端内容
        mainContent.SetActive(false);

        // 显示提示文本
        messagePanel.SetActive(true);
        messageText.text = msg;

        yield return new WaitForSeconds(messageDuration);

        // 隐藏提示文本
        messagePanel.SetActive(false);

        // 恢复原本终端内容
        mainContent.SetActive(true);

        messageRoutine = null;
    }
    IEnumerator CallElevatorRoutine(string msg)
    {
        yield return new WaitForSeconds(messageDuration);

        mainContent.SetActive(false);
        messagePanel.SetActive(true);
        messageText.text = msg;

        yield return new WaitForSeconds(elevatorDuration);
        messageText.text = "Thanks for your patience";
        targetDoor.UnlockDoor();

        yield return new WaitForSeconds(messageDuration);
        // 隐藏提示文本
        messagePanel.SetActive(false);

        // 恢复原本终端内容
        mainContent.SetActive(true);

        messageRoutine = null;
    }

    bool HasKeyInLeftHand()
    {
        if (leftHandHoldPoint == null) return false;

        for (int i = 0; i < leftHandHoldPoint.childCount; i++)
        {
            Transform child = leftHandHoldPoint.GetChild(i);
            if (child.CompareTag(keyTag))
                return true;
        }

        return false;
    }
}