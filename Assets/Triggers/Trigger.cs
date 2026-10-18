using System;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    public bool HasActivated { get; private set; } = false;
    
    [SerializeField] private List<Triggerable> triggerables;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool startsLocked = false;

    private bool isLocked = false;

    private void Start()
    {
        isLocked = startsLocked;
    }

    protected void Unlock(bool triggerImmediately = false)
    {
        isLocked = false;
        
        if(triggerImmediately)
            ActivateTriggers();
    }

    protected void Lock()
    {
        isLocked = true;
    }

    protected void ActivateTriggers()
    {
        if (isLocked)
            return;
        
        if (triggerOnce && HasActivated)
            return;
        
        foreach (Triggerable triggerable in triggerables)
        {
            if(triggerable == null)
                continue;
            
            triggerable.Trigger();
        }
        HasActivated = true;
    }
}