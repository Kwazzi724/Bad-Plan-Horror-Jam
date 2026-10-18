using UnityEngine;

public class InteractionTrigger : Trigger, IInteractable
{
    public void OnInteract(GameObject interactor)
    {
        ActivateTriggers();
    }
}