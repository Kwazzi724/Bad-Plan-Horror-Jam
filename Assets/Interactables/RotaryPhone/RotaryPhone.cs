using System;
using UnityEngine;

public class RotaryPhone : MonoBehaviour, IInteractable
{
    public Action TestAction;
    
    public void OnInteract(GameObject interactor)
    {
        Debug.Log(interactor.name + " interacted with a cute little phone");
    }
}
