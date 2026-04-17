using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    public bool isHidden = false;

    private int hideZoneCount = 0;

    public void EnterHideZone()
    {
        hideZoneCount++;
        isHidden = hideZoneCount > 0;
    }

    public void ExitHideZone()
    {
        hideZoneCount = Mathf.Max(0, hideZoneCount - 1);
        isHidden = hideZoneCount > 0;
    }
}