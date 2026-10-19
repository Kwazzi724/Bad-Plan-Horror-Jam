using System;
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
    public static void PlayOneShot(EventReference sound, Vector3 position, params SoundLabel[] labels)
    {
        if (sound.IsNull) return;
        EventInstance instance = RuntimeManager.CreateInstance(sound);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

        foreach (SoundLabel soundLabel in labels)
        {
            if (string.IsNullOrEmpty(soundLabel.parameter)) continue;
            FMOD.RESULT result = instance.setParameterByNameWithLabel(soundLabel.parameter, soundLabel.label);

            if (result != FMOD.RESULT.OK)
                Debug.LogWarning($"sound player couldn't set {soundLabel.parameter} = {soundLabel.label} on {sound} ({result}). Check spelling");
        }
        instance.start();
        instance.release();
    }
}
