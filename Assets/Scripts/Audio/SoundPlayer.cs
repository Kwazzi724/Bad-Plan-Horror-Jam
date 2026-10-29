using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[Serializable]
public struct SoundLabel
{
    public string parameter;
    public string label;

    public SoundLabel(string parameter, string label)
    {
        this.parameter = parameter;
        this.label = label;
    }
}

public static class SoundPlayer
{
    private static readonly HashSet<string> warnedLooping = new HashSet<string>();

    public static void PlayOneShot(EventReference sound, Vector3 position, params SoundLabel[] labels)
    {
        if (sound.IsNull) return;

        EventInstance instance;

        try
        {
            instance = RuntimeManager.CreateInstance(sound);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"SoundPlayer: event {sound} isn't in the loaded banks, skipping it. Rebuild the FMOD banks.");
            return;
        }
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

        foreach (SoundLabel soundLabel in labels)
        {
            if (string.IsNullOrEmpty(soundLabel.parameter)) continue;

            FMOD.RESULT result = instance.setParameterByNameWithLabel(soundLabel.parameter, soundLabel.label);

            if (result != FMOD.RESULT.OK)
                Debug.LogWarning($"SoundPlayer: couldn't set {soundLabel.parameter} = {soundLabel.label} on {sound} ({result}). Check the spelling against FMOD.");
        }

        WarnIfLooping(sound, instance);
        instance.start();
        instance.release();
    }

    private static void WarnIfLooping(EventReference sound, EventInstance instance)
    {
        if (instance.getDescription(out EventDescription description) != FMOD.RESULT.OK) return;

        description.isOneshot(out bool oneShot);
        if (oneShot) return;

        description.getPath(out string path);
        string key = string.IsNullOrEmpty(path) ? sound.ToString() : path;

        if (!warnedLooping.Add(key)) return;

        Debug.LogWarning($"SoundPlayer: '{key}' loops or has a sustain point in FMOD, so playing it as a one-shot will never stop. Remove the loop region / Loop Playback in FMOD, or play it from a script that stops it.");
    }
}