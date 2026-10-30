using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class LevelAmbience : MonoBehaviour
{
    [SerializeField] private EventReference levelMusic;
    [SerializeField] private string loopParameter = "Loop";
    [SerializeField] private List<int> loopPerStage = new List<int> { 0, 1, 2, 3 };
    [SerializeField] private bool restartWhenFinished = true;
    [SerializeField] private bool fadeOutOnStop = true;

    [Header("Ending Track")]
    [SerializeField] private EventReference endingTrack;
    [SerializeField] private int endingStage = 7;
    [SerializeField] private float endingFadeInDuration = 3f;

    private EventInstance instance;
    private GameManager gameManager;
    private int currentLoop = -1;
    private EventInstance endingInstance;
    private float endingVolume;

    private void Start()
    {
        if (!levelMusic.IsNull)
        {
            instance = RuntimeManager.CreateInstance(levelMusic);
            UpdatePosition();
            instance.start();
        }

        gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StageEntered += HandleStageEntered;
        SetLoopForStage(Mathf.Max(1, gameManager.CurrentStageNumber));

        if (gameManager.CurrentStageNumber == endingStage)
            StartEndingTrack();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.StageEntered -= HandleStageEntered;

        StopInstance(ref endingInstance);
        StopInstance(ref instance);
    }

    private void StopInstance(ref EventInstance sound)
    {
        if (!sound.isValid()) return;

        sound.stop(fadeOutOnStop ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        sound.release();
        sound.clearHandle();
    }

    private void Update()
    {
        UpdateEndingTrack();

        if (!instance.isValid()) return;

        UpdatePosition();

        if (!restartWhenFinished) return;

        instance.getPlaybackState(out PLAYBACK_STATE playbackState);

        if (playbackState == PLAYBACK_STATE.STOPPED)
            instance.start();
    }

    private void HandleStageEntered(Stage stage, int stageNumber)
    {
        SetLoopForStage(stageNumber);

        if (stageNumber == endingStage)
            StartEndingTrack();
    }

    private void StartEndingTrack()
    {
        if (endingTrack.IsNull || endingInstance.isValid()) return;

        try
        {
            endingInstance = RuntimeManager.CreateInstance(endingTrack);
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"LevelAmbience: couldn't create {endingTrack}.", this);
            return;
        }

        endingVolume = endingFadeInDuration > 0f ? 0f : 1f;
        endingInstance.setVolume(endingVolume);
        endingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(ListenerTransform()));
        endingInstance.start();
    }

    private void UpdateEndingTrack()
    {
        if (!endingInstance.isValid()) return;

        endingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(ListenerTransform()));

        if (endingVolume < 1f)
        {
            endingVolume = Mathf.MoveTowards(endingVolume, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, endingFadeInDuration));
            endingInstance.setVolume(endingVolume);
        }

        endingInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
        if (playbackState == PLAYBACK_STATE.STOPPED)
            endingInstance.start();
    }

    private void SetLoopForStage(int stageNumber)
    {
        if (loopPerStage.Count == 0) return;

        int index = Mathf.Clamp(stageNumber - 1, 0, loopPerStage.Count - 1);
        int loop = loopPerStage[index];

        if (loop == currentLoop) return;
        currentLoop = loop;

        if (instance.isValid())
            instance.setParameterByName(loopParameter, loop);
    }

    private void UpdatePosition()
    {
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(ListenerTransform()));
    }

    private Transform ListenerTransform()
    {
        return Camera.main != null ? Camera.main.transform : transform;
    }
}