using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HideVignetteUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHideState playerHideState;
    public Image vignetteImage;

    [Header("Fade")]
    public float fadeDuration = 0.25f;
    [Range(0f, 1f)] public float shownAlpha = 0.7f;

    private Coroutine fadeRoutine;

    private void Reset()
    {
        vignetteImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (vignetteImage == null)
            vignetteImage = GetComponent<Image>();

        if (playerHideState == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerHideState = playerObj.GetComponent<PlayerHideState>();
        }

        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0f;
            vignetteImage.color = c;
        }
    }

    private void OnEnable()
    {
        if (playerHideState != null)
            playerHideState.OnHiddenStateChanged += HandleHiddenStateChanged;
    }

    private void OnDisable()
    {
        if (playerHideState != null)
            playerHideState.OnHiddenStateChanged -= HandleHiddenStateChanged;
    }

    private void HandleHiddenStateChanged(bool hidden)
    {
        float targetAlpha = hidden ? shownAlpha : 0f;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToAlpha(targetAlpha));
    }

    private IEnumerator FadeToAlpha(float targetAlpha)
    {
        if (vignetteImage == null)
            yield break;

        Color color = vignetteImage.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, easedT);
            vignetteImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        vignetteImage.color = color;

        fadeRoutine = null;
    }
}
