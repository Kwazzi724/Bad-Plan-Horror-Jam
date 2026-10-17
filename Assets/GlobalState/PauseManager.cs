using System;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    public bool IsPaused { get; private set; } = false;
    
    public event Action OnPauseStarted;
    public event Action OnPauseEnded;

    private float previousTimescale = 1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Pause()
    {
        IsPaused = true;
        
        previousTimescale = Time.timeScale;
        Time.timeScale = 0;
        
        OnPauseStarted?.Invoke();
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = previousTimescale;
        OnPauseEnded?.Invoke();
    }
}
