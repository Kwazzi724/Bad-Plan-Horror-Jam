using System;
using UnityEngine;

public interface IHoldInteractable : IInteractable
{
    Transform FocusPoint { get; }
    string HoldPrompt { get; }
    bool UsesCursor { get; }
    bool CanRelease { get; }
    event Action Finished;
    void EndInteract();
}