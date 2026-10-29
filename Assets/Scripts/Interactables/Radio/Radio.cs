using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class Radio : MonoBehaviour
{
    [Header("When")]
    [SerializeField] private int playOnStage = 5;
    [SerializeField] private float delay = 10f;

    [Header("Sound")]
    [SerializeField] private EventReference voiceLine;
    [SerializeField] private SoundLabel voiceLabel = new SoundLabel("RadioSetting", "News");

    private Stage stage;
    private Coroutine countdown;
    private EventInstance instance;
    private bool wasPlaying;

    public bool IsPlaying => instance.isValid();

    public event Action Finished;

    private bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused;

    private void Awake()
    {
        stage = GetComponentInParent<Stage>();
    }

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StageEntered += HandleStageEntered;

        if (gameManager.CurrentStageNumber > 0)
            HandleStageEntered(gameManager.CurrentStage, gameManager.CurrentStageNumber);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered -= HandleStageEntered;

        StopVoice();
    }

    private void Update()
    {
        if (!instance.isValid()) return;

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.getPlaybackState(out PLAYBACK_STATE playbackState);

        if (playbackState != PLAYBACK_STATE.STOPPED)
        {
            wasPlaying = true;
            return;
        }

        if (!wasPlaying) return;

        StopVoice();
        Finished?.Invoke();
    }

    private void HandleStageEntered(Stage enteredStage, int stageNumber)
    {
        bool isOurStage = stage == null || enteredStage == stage;

        if (isOurStage && stageNumber == playOnStage)
        {
            StopCountdown();
            countdown = StartCoroutine(PlayAfterDelay());
            return;
        }

        StopCountdown();
        StopVoice();
    }

    private IEnumerator PlayAfterDelay()
    {
        float elapsed = 0f;

        while (elapsed < delay)
        {
            if (!IsPaused)
                elapsed += Time.deltaTime;

            yield return null;
        }

        countdown = null;
        Play();
    }

    public void Play()
    {
        StopVoice();

        if (voiceLine.IsNull) return;

        try
        {
            instance = RuntimeManager.CreateInstance(voiceLine);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"Radio: event {voiceLine} isn't in the loaded banks.", this);
            return;
        }

        if (!string.IsNullOrEmpty(voiceLabel.parameter))
        {
            FMOD.RESULT result = instance.setParameterByNameWithLabel(voiceLabel.parameter, voiceLabel.label);

            if (result != FMOD.RESULT.OK)
                Debug.LogWarning($"Radio: couldn't set {voiceLabel.parameter} = {voiceLabel.label} ({result}).", this);
        }

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.start();
        wasPlaying = false;
    }

    public void StopVoice()
    {
        wasPlaying = false;

        if (!instance.isValid()) return;

        instance.stop(STOP_MODE.ALLOWFADEOUT);
        instance.release();
        instance.clearHandle();
    }

    private void StopCountdown()
    {
        if (countdown == null) return;

        StopCoroutine(countdown);
        countdown = null;
    }
}
