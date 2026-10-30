using System;
using System.Collections;
using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private float delay = 3f;
    [SerializeField] private GameObject boss;

    private Coroutine countdown;

    public bool IsTriggered { get; private set; }

    public event Action Triggered;

    private void Awake()
    {
        if (boss != null)
            boss.SetActive(false);
    }

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

        if (boss != null)
            boss.SetActive(true);
        else
            Debug.LogWarning($"BossTrigger '{name}': no Boss assigned to spawn.", this);
        Triggered?.Invoke();
    }
}