using UnityEngine;

public class RotaryPhone : MonoBehaviour, IInteractable
{
    [SerializeField] private Triggerable testTriggerable;
    
    public void OnInteract(GameObject interactor)
    {
        testTriggerable.Trigger();
    }
}
