using System;
using System.Collections;
using UnityEngine;

public class ScreenFadeManager : MonoBehaviour
{
    public static ScreenFadeManager Instance;
    
    public event Action<float> OnFadeInStarted;
    public event Action OnFadeInEnded;
    public event Action<float> OnFadeOutStarted;
    public event Action OnFadeOutEnded;

    private bool isFadingOut = false;
    private bool isFadeInQueued = false;

    private float queuedFadeInDuration = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void FadeIn(float duration)
    {
        if(isFadingOut)
        {
            if (isFadeInQueued)
                return;
            
            QueueFadeIn(duration);
            return;
        }

        OnFadeInStarted?.Invoke(duration);
        StartCoroutine(WaitForFadeIn(duration));
    }

    public void FadeOut(float duration)
    {
        if (isFadingOut)
            return;
        
        isFadingOut = true;
        OnFadeOutStarted?.Invoke(duration);
        StartCoroutine(WaitForFadeOut(duration));
    }

    private IEnumerator WaitForFadeIn(float duration)
    {
        yield return new WaitForSeconds(duration);
        OnFadeInEnded?.Invoke();
    }

    private IEnumerator WaitForFadeOut(float duration)
    {
        yield return new WaitForSeconds(duration);
        isFadingOut = false;
        OnFadeOutEnded?.Invoke();
    }

    private void QueueFadeIn(float duration)
    {
        isFadeInQueued = true;
        queuedFadeInDuration = duration;
        OnFadeOutEnded += StartQueuedFadeIn;
    }

    private void StartQueuedFadeIn()
    {
        OnFadeOutEnded -= StartQueuedFadeIn;
        isFadeInQueued = false;
        FadeIn(queuedFadeInDuration);
        queuedFadeInDuration = 0f;
    }
}
