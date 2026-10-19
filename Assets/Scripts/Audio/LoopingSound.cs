using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class LoopingSound : MonoBehaviour
{
    [SerializeField] private EventReference soundEvent;
    [SerializeField] private bool restartWhenFinished = true;
    [SerializeField] private bool fadeOutOnStop = true;

    private EventInstance instance;

    private void Start()
    {
        if (soundEvent.IsNull) return;

        instance = RuntimeManager.CreateInstance(soundEvent);
        instance.start();
    }

    private void Update()
    {
        if (!restartWhenFinished || !instance.isValid()) return;

        instance.getPlaybackState(out PLAYBACK_STATE playbackState);

        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            instance.start();
        }
    }

    private void OnDestroy()
    {
        if (!instance.isValid()) return;

        instance.stop(fadeOutOnStop ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        instance.release();
    }
}
