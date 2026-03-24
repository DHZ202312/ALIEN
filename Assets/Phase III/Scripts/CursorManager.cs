using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    public enum CursorZone
    {
        None,
        Computer,
        Notebook
    }

    [Header("Cursor Textures")]
    public Texture2D defaultCursor;
    public Texture2D computerCursor;
    public Texture2D handCursor;
    public Texture2D notebookCursor;

    [Header("Hotspots")]
    public Vector2 defaultHotspot = Vector2.zero;
    public Vector2 computerHotspot = Vector2.zero;
    public Vector2 handHotspot = Vector2.zero;
    public Vector2 notebookHotspot = Vector2.zero;

    private CursorZone currentZone = CursorZone.None;
    private bool hoveringClickable = false;

    void Awake()
    {
        Instance = this;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SetZone(CursorZone zone)
    {
        currentZone = zone;

        if (currentZone != CursorZone.Computer)
            hoveringClickable = false;

        RefreshCursor();
    }

    public void SetHoverClickable(bool hover)
    {
        hoveringClickable = hover;
        RefreshCursor();
    }

    void RefreshCursor()
    {
        switch (currentZone)
        {
            case CursorZone.Computer:

                if (hoveringClickable)
                    Cursor.SetCursor(handCursor, handHotspot, CursorMode.Auto);
                else
                    Cursor.SetCursor(computerCursor, computerHotspot, CursorMode.Auto);

                break;

            case CursorZone.Notebook:

                Cursor.SetCursor(notebookCursor, notebookHotspot, CursorMode.Auto);

                break;

            default:

                Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto);

                break;
        }
    }
}