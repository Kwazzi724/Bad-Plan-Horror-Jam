using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FadeOverlay : MonoBehaviour
{
    private Animator animator;
    
    private void Start()
    {
        ScreenFadeManager.Instance.OnFadeOutStarted += StartFadeOut;
        ScreenFadeManager.Instance.OnFadeInStarted += StartFadeIn;
        
        animator = GetComponent<Animator>();
    }

    private void StartFadeOut(float duration)
    {
        animator.speed = GetPlaybackSpeed(duration);
        animator.Play("FadeOut");
    }

    private void StartFadeIn(float duration)
    {
        animator.speed = GetPlaybackSpeed(duration);
        animator.Play("FadeIn");
    }

    private float GetPlaybackSpeed(float duration)
    {
        if (duration <= 0.0f)
        {
            return 10000f;
        }
     
        return 1.0f / duration;
    }
}
