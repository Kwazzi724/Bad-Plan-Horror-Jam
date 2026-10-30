using System;
using System.Collections;
using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private float delay = 3f;

    private Coroutine countdown;

    public bool IsTriggered { get; private set; }

    public event Action Triggered;

    public void TriggerBossAfterDelay()
    {
        if (IsTriggered || countdown != null) return;

        countdown = StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        yield return new WaitForSeconds(delay);
        countdown = null;
        TriggerBoss();
    }

    public void TriggerBoss()
    {
        if (IsTriggered) return;

        IsTriggered = true;
        Debug.Log($"BossTrigger '{name}': boss triggered.", this);
        Triggered?.Invoke();
    }
}
