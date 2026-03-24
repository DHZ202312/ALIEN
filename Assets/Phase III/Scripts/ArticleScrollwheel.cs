using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ArticleScrollwheel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Scrollbar verticalScrollbar;
    public float scrollSpeed = 0.15f;

    private bool isHovering;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    private void Update()
    {
        if (!isHovering || verticalScrollbar == null) return;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            verticalScrollbar.value = Mathf.Clamp01(verticalScrollbar.value + wheel * scrollSpeed);
        }
    }
}
