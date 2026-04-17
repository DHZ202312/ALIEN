using System;
using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    public bool isHidden = false;

    private int hideZoneCount = 0;

    public event Action<bool> OnHiddenStateChanged;

    public void EnterHideZone()
    {
        hideZoneCount++;
        SetHiddenState(hideZoneCount > 0);
    }

    public void ExitHideZone()
    {
        hideZoneCount = Mathf.Max(0, hideZoneCount - 1);
        SetHiddenState(hideZoneCount > 0);
    }

    private void SetHiddenState(bool hidden)
    {
        if (isHidden == hidden)
            return;

        isHidden = hidden;
        OnHiddenStateChanged?.Invoke(isHidden);
    }
}