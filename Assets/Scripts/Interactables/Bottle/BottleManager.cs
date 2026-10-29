using System;
using UnityEngine;

public class BottleManager : MonoBehaviour
{
    [SerializeField] private GameObject revealWhenCleared;

    private Bottle[] bottles = new Bottle[0];
    private int remaining;

    public int Remaining => remaining;
    public bool IsSolved => bottles.Length > 0 && remaining == 0;

    public event Action Solved;

    private void Awake()
    {
        bottles = GetComponentsInChildren<Bottle>(true);
        remaining = 0;

        foreach (Bottle bottle in bottles)
        {
            if (bottle.IsCleared) continue;

            remaining++;
            bottle.Cleared += HandleCleared;
        }

        if (revealWhenCleared != null)
            revealWhenCleared.SetActive(false);

        if (bottles.Length == 0)
            Debug.LogWarning($"BottleManager '{name}' has no Bottle children.", this);
    }

    private void OnDestroy()
    {
        foreach (Bottle bottle in bottles)
        {
            if (bottle != null)
                bottle.Cleared -= HandleCleared;
        }
    }

    private void HandleCleared(Bottle bottle)
    {
        bottle.Cleared -= HandleCleared;
        remaining = Mathf.Max(0, remaining - 1);

        if (remaining > 0) return;

        if (revealWhenCleared != null)
            revealWhenCleared.SetActive(true);

        Solved?.Invoke();
    }
}
