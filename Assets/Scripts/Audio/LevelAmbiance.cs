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

    private EventInstance instance;
    private GameManager gameManager;
    private int currentLoop = -1;

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
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.StageEntered -= HandleStageEntered;

        if (!instance.isValid()) return;

        instance.stop(fadeOutOnStop ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        instance.release();
    }

    private void Update()
    {
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
        Transform listener = Camera.main != null ? Camera.main.transform : transform;
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(listener));
    }
}