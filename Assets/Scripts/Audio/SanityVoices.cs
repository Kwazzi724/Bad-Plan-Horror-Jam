using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class SanityVoices : MonoBehaviour
{
    [Header("Voices")]
    [SerializeField] private EventReference voicesEvent;
    [SerializeField] private string sanityParameter = "Sanity";
    [SerializeField] private float minSanity = 0f;
    [SerializeField] private float maxSanity = 10f;

    [Header("Timing (seconds)")]
    [SerializeField] private float darkDelay = 5f;
    [SerializeField] private float rampDuration = 10f;
    [SerializeField] private float peakDuration = 5f;
    [SerializeField] private float fadeOutDuration = 3f;

    [Header("Insane Image")]
    [SerializeField] private Graphic insaneImage;
    [SerializeField] private float imageDelay = 5f;
    [SerializeField] private float imageFadeDuration = 3f;
    [SerializeField] private float imageHoldDuration = 2f;

    [Header("Game")]
    [SerializeField] private GameManager gameManager;

    private EventInstance voices;
    private float darkTime;
    private bool hasGoneInsane;
    private bool wasDark;
    private float fadeStartDarkTime;
    private float fadeTimer;
    private float volume = 1f;
    private Coroutine insaneRoutine;

    private bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused;
    private float InsaneTime => darkDelay + rampDuration + peakDuration;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        SetImageAlpha(0f);
    }

    private void Update()
    {
        if (hasGoneInsane || IsPaused) return;

        Torch torch = Torch.Active;
        bool isDark = torch != null && !torch.IsOn;

        if (isDark)
        {
            darkTime += Time.deltaTime;
            volume = 1f;
        }
        else
        {
            FadeOut();
        }

        wasDark = isDark;
        UpdateVoices();

        if (isDark && darkTime >= InsaneTime)
            GoInsane();
    }

    private void FadeOut()
    {
        if (wasDark)
        {
            fadeStartDarkTime = Mathf.Min(darkTime, darkDelay + rampDuration);
            fadeTimer = 0f;
        }

        if (darkTime <= 0f) return;

        if (fadeStartDarkTime <= darkDelay)
        {
            darkTime = 0f;
            return;
        }

        fadeTimer += Time.deltaTime;
        float t = fadeOutDuration > 0f ? Mathf.Clamp01(fadeTimer / fadeOutDuration) : 1f;

        darkTime = Mathf.Lerp(fadeStartDarkTime, darkDelay, t);
        volume = 1f - t;

        if (t >= 1f)
            darkTime = 0f;
    }

    private void UpdateVoices()
    {
        if (darkTime < darkDelay)
        {
            StopVoices();
            return;
        }
        StartVoices();

        float progress = rampDuration > 0f ? Mathf.Clamp01((darkTime - darkDelay) / rampDuration) : 1f;
        float sanity = Mathf.Lerp(minSanity, maxSanity, progress);

        voices.setParameterByName(sanityParameter, sanity);
        voices.setVolume(volume);

        Transform listener = Camera.main != null ? Camera.main.transform : transform;
        voices.set3DAttributes(RuntimeUtils.To3DAttributes(listener));
    }

    private void StartVoices()
    {
        if (voices.isValid() || voicesEvent.IsNull) return;

        voices = RuntimeManager.CreateInstance(voicesEvent);
        voices.setParameterByName(sanityParameter, minSanity);
        voices.start();
    }

    private void StopVoices()
    {
        if (!voices.isValid()) return;

        voices.stop(STOP_MODE.ALLOWFADEOUT);
        voices.release();
        voices.clearHandle();
    }

    private void GoInsane()
    {
        hasGoneInsane = true;

        if (gameManager != null)
            gameManager.Insane();

        if (insaneRoutine != null)
            StopCoroutine(insaneRoutine);

        insaneRoutine = StartCoroutine(InsaneSequence());
    }

    private IEnumerator InsaneSequence()
    {
        yield return WaitUnpaused(imageDelay);

        float elapsed = 0f;
        while (elapsed < imageFadeDuration)
        {
            if (!IsPaused)
            {
                elapsed += Time.deltaTime;
                SetImageAlpha(Mathf.Clamp01(elapsed / imageFadeDuration));
            }

            yield return null;
        }

        SetImageAlpha(1f);
        yield return WaitUnpaused(imageHoldDuration);

        SetImageAlpha(0f);
        insaneRoutine = null;
        ResetSanity();

        if (gameManager != null)
            gameManager.GameOver();
    }

    private IEnumerator WaitUnpaused(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsPaused)
                elapsed += Time.deltaTime;

            yield return null;
        }
    }

    private void ResetSanity()
    {
        StopVoices();
        darkTime = 0f;
        fadeTimer = 0f;
        fadeStartDarkTime = 0f;
        wasDark = false;
        volume = 1f;
        hasGoneInsane = false;

        if (gameManager != null)
            gameManager.Sane();
    }

    private void SetImageAlpha(float alpha)
    {
        if (insaneImage == null) return;

        Color color = insaneImage.color;
        color.a = alpha;
        insaneImage.color = color;
    }

    private void OnDestroy()
    {
        StopVoices();
    }
}