using System;
using UnityEngine;

public class Triggerable : MonoBehaviour
{
    public event Action OnTriggered;
    
    public void Trigger()
    {
        OnTriggered?.Invoke();
    }
}