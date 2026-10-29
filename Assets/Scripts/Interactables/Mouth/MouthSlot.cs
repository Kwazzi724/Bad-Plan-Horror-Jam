using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class MouthSlot : MonoBehaviour, IInteractable, IItemReceiver
{
    [SerializeField] private Transform[] snapPoints = new Transform[3];
    [FormerlySerializedAs("slotSound")]
    [SerializeField] private EventReference attachSound;
    [SerializeField] private EventReference completeSound;

    private readonly List<MouthItem> attached = new List<MouthItem>();
    private EventInstance completeInstance;

    public bool CanInteract => false;
    public int AttachedCount => attached.Count;
    public int Capacity => snapPoints != null ? snapPoints.Length : 0;
    public bool IsSolved => Capacity > 0 && attached.Count >= Capacity;

    public event Action Solved;

    public void OnInteract(GameObject interactor)
    {
    }

    private void OnDestroy()
    {
        if (!completeInstance.isValid()) return;

        completeInstance.stop(STOP_MODE.ALLOWFADEOUT);
        completeInstance.release();
        completeInstance.clearHandle();
    }

    public bool CanReceive(Pickup item)
    {
        return !IsSolved && item != null && item.GetComponent<MouthItem>() != null;
    }

    public void Receive(Pickup item)
    {
        if (!CanReceive(item)) return;

        Transform point = snapPoints[attached.Count];
        if (point == null)
            point = transform;

        MouthItem mouth = item.GetComponent<MouthItem>();
        attached.Add(mouth);
        mouth.Quieten();
        item.PlaceInto(point);
        item.Locked = true;

        SoundPlayer.PlayOneShot(attachSound, point.position);

        if (!IsSolved) return;

        PlayCompleteSound();
        Solved?.Invoke();
    }

    private void PlayCompleteSound()
    {
        if (completeSound.IsNull || completeInstance.isValid()) return;

        try
        {
            completeInstance = RuntimeManager.CreateInstance(completeSound);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"MouthSlot: event {completeSound} isn't in the loaded banks.", this);
            return;
        }

        completeInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        completeInstance.start();
    }
}