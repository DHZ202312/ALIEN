using UnityEngine;
using UnityEngine.UI;

public class StanceIndicator : MonoBehaviour
{
    [Header("Target")]
    public FpsController playerController;

    [Header("Stance Images")]
    public GameObject standImage;
    public GameObject crouchImage;
    public GameObject proneImage;

    private FpsController.Stance lastStance;

    private void Start()
    {
        if (playerController == null)
        {
            Debug.LogWarning("StanceIndicator: No FpsController found.");
            return;
        }

        lastStance = playerController.stance;
        RefreshDisplay(lastStance);
    }

    private void Update()
    {
        if (playerController == null)
            return;

        if (playerController.stance != lastStance)
        {
            lastStance = playerController.stance;
            RefreshDisplay(lastStance);
        }
    }

    private void RefreshDisplay(FpsController.Stance stance)
    {
        if (standImage != null)
            standImage.SetActive(stance == FpsController.Stance.Stand);

        if (crouchImage != null)
            crouchImage.SetActive(stance == FpsController.Stance.Crouch);

        if (proneImage != null)
            proneImage.SetActive(stance == FpsController.Stance.Prone);
    }
}
