using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HideSpotTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerHideState hideState = other.GetComponentInParent<PlayerHideState>();
        if (hideState != null)
        {
            hideState.EnterHideZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerHideState hideState = other.GetComponentInParent<PlayerHideState>();
        if (hideState != null)
        {
            hideState.ExitHideZone();
        }
    }
}
