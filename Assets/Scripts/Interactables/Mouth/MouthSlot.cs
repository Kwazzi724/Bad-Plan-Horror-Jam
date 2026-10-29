using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class MouthSlot : MonoBehaviour, IInteractable, IItemReceiver
{
    [SerializeField] private Transform[] snapPoints = new Transform[3];
    [SerializeField] private EventReference slotSound;

    private readonly List<MouthItem> attached = new List<MouthItem>();

    public bool CanInteract => false;
    public int AttachedCount => attached.Count;
    public int Capacity => snapPoints != null ? snapPoints.Length : 0;
    public bool IsSolved => Capacity > 0 && attached.Count >= Capacity;

    public event Action Solved;

    public void OnInteract(GameObject interactor)
    {
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
        mouth.FadeOutVoice();
        item.PlaceInto(point);
        item.Locked = true;

        SoundPlayer.PlayOneShot(slotSound, point.position);

        if (IsSolved)
            Solved?.Invoke();
    }
}