using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

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
    [SerializeField] private float alarmDuration = 0f;

    private bool isOpen;
    private bool solved;
    private EventInstance alarmInstance;
    private float alarmStopTime = float.PositiveInfinity;

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

    private void Update()
    {
        if (alarmInstance.isValid() && Time.time >= alarmStopTime)
            StopAlarm();
    }

    private void OnDisable()
    {
        StopAlarm();
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
        StartAlarm();
        Solved?.Invoke();
    }

    private void StartAlarm()
    {
        StopAlarm();
        if (alarmSound.IsNull) return;

        try
        {
            alarmInstance = RuntimeManager.CreateInstance(alarmSound);
        }
        catch (Exception)
        {
            Debug.LogWarning($"Clock: couldn't create {alarmSound}.", this);
            return;
        }

        alarmInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        alarmInstance.start();
        alarmStopTime = alarmDuration > 0f ? Time.time + alarmDuration : float.PositiveInfinity;
    }

    private void StopAlarm()
    {
        alarmStopTime = float.PositiveInfinity;

        if (!alarmInstance.isValid()) return;

        alarmInstance.stop(STOP_MODE.ALLOWFADEOUT);
        alarmInstance.release();
        alarmInstance.clearHandle();
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