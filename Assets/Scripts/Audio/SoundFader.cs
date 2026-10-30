using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class SoundFader : MonoBehaviour
{
    private class Fade
    {
        public EventInstance sound;
        public float startVolume;
        public float duration;
        public float elapsed;
    }

    private static SoundFader instance;
    private static bool quitting;

    private readonly List<Fade> fades = new List<Fade>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        quitting = false;
        Application.quitting -= HandleQuitting;
        Application.quitting += HandleQuitting;
    }

    private static void HandleQuitting()
    {
        quitting = true;
    }

    public static void FadeOutAndRelease(EventInstance sound, float duration)
    {
        if (!sound.isValid()) return;

        if (quitting || duration <= 0f || !Application.isPlaying)
        {
            StopNow(sound);
            return;
        }

        if (instance == null)
            instance = new GameObject("Sound Fader").AddComponent<SoundFader>();

        sound.getVolume(out float volume);
        instance.fades.Add(new Fade { sound = sound, startVolume = volume, duration = duration });
    }

    private void Update()
    {
        for (int i = fades.Count - 1; i >= 0; i--)
        {
            Fade fade = fades[i];
            fade.elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fade.elapsed / fade.duration);

            if (fade.sound.isValid())
                fade.sound.setVolume(Mathf.Lerp(fade.startVolume, 0f, t));

            if (t < 1f) continue;

            StopNow(fade.sound);
            fades.RemoveAt(i);
        }
    }

    private void OnDestroy()
    {
        foreach (Fade fade in fades)
            StopNow(fade.sound);

        fades.Clear();

        if (instance == this)
            instance = null;
    }

    private static void StopNow(EventInstance sound)
    {
        if (!sound.isValid()) return;

        sound.stop(STOP_MODE.IMMEDIATE);
        sound.release();
    }
}
