using System;
using FMODUnity;
using UnityEngine;

public class Clock : MonoBehaviour, IHoldInteractable
{
    [Header("Parts")]
    [SerializeField] private ClockMinigame minigame;
    [SerializeField] private Transform focusPoint;

    [Header("Prompt")]
    [SerializeField] private string holdPrompt = "Right click to close";

    [Header("Time (HH:mm, 24 hour)")]
    [SerializeField] private string correctTime = "21:00";
    [SerializeField] private string startTime = "00:00";

    [Header("Sounds")]
    [SerializeField] private EventReference alarmSound;

    private bool isOpen;
    private bool solved;

    public event Action Finished;
    public event Action Solved;

    public bool IsSolved => solved;

    public bool CanInteract => !isOpen && minigame != null;
    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
    public string HoldPrompt => holdPrompt;
    public bool UsesCursor => true;
    public bool CanRelease => true;

    private void Awake()
    {
        if (minigame != null && minigame.gameObject.activeSelf)
            minigame.gameObject.SetActive(false);
    }

    public void OnInteract(GameObject interactor)
    {
        if (!CanInteract) return;

        Camera viewer = Camera.main;
        if (viewer == null) return;

        isOpen = true;
        TryParseTime(startTime, out int startHour, out int startMinute);
        minigame.Open(viewer, startHour, startMinute, HandleApplied);
    }

    public void EndInteract()
    {
        if (!isOpen) return;

        isOpen = false;
        minigame.Close();
    }

    private void HandleApplied(int hour, int minute)
    {
        isOpen = false;
        Finished?.Invoke();

        bool correct = TryParseTime(correctTime, out int correctHour, out int correctMinute)
                       && hour == correctHour
                       && minute == correctMinute;

        if (correct)
            ClockTuneSuccessful();
        else
            ClockTuneFail();
    }

    public void ClockTuneSuccessful()
    {
        if (solved) return;
        solved = true;
        SoundPlayer.PlayOneShot(alarmSound, transform.position);
        Solved?.Invoke();
    }

    public void ClockTuneFail()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    private static bool TryParseTime(string time, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;

        if (string.IsNullOrEmpty(time)) return false;

        string[] parts = time.Split(':');
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out hour) && int.TryParse(parts[1], out minute)
               && hour >= 0 && hour < 24 && minute >= 0 && minute < 60;
    }
}