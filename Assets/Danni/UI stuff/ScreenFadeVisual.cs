using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeVisual : MonoBehaviour
{
    [SerializeField] private ScreenFadeManager fadeManager;
    [SerializeField] private bool startBlack;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (fadeManager == null)
        {
            fadeManager = FindAnyObjectByType<ScreenFadeManager>();
        }

        SetAlpha(startBlack ? 1f : 0f);
    }

    private void OnEnable()
    {
        if (fadeManager == null) return;

        fadeManager.OnFadeOutStarted += HandleFadeOutStarted;
        fadeManager.OnFadeInStarted += HandleFadeInStarted;
    }

    private void OnDisable()
    {
        if (fadeManager == null) return;

        fadeManager.OnFadeOutStarted -= HandleFadeOutStarted;
        fadeManager.OnFadeInStarted -= HandleFadeInStarted;
    }

    private void HandleFadeOutStarted(float duration)
    {
        FadeTo(1f, duration);
    }

    private void HandleFadeInStarted(float duration)
    {
        FadeTo(0f, duration);
    }

    private void FadeTo(float targetAlpha, float duration)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0.01f;
    }
}