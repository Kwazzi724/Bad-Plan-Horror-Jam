using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(StudioEventEmitter), typeof(Triggerable))]
public class TriggerableSoundEmitter : MonoBehaviour
{
    private StudioEventEmitter eventEmitter;

    private void Start()
    {
        eventEmitter = GetComponent<StudioEventEmitter>();
        GetComponent<Triggerable>().OnTriggered += Trigger;
    }
    
    public void Trigger()
    {
        eventEmitter.Play();
    }
}