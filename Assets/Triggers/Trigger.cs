using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    public bool HasActivated { get; private set; } = false;
    
    [SerializeField] private List<Triggerable> triggerables;
    [SerializeField] private bool activateOnce = true;
    
    protected void ActivateTriggers()
    {
        if (activateOnce && HasActivated)
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