using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    [SerializeField] private List<Triggerable> triggerables;

    protected void ActivateTriggers()
    {
        foreach (Triggerable triggerable in triggerables)
        {
            if(triggerable == null)
                continue;
            
            triggerable.Trigger();
        }
    }
}