using System;
using UnityEngine;

public class StainManager : MonoBehaviour
{
    private Stain[] stains = new Stain[0];
    private int remaining;

    public int Remaining => remaining;
    public bool IsSolved => stains.Length > 0 && remaining == 0;

    public event Action Solved;

    private void Awake()
    {
        stains = GetComponentsInChildren<Stain>(true);
        remaining = 0;

        foreach (Stain stain in stains)
        {
            if (stain.IsClean) continue;

            remaining++;
            stain.Cleaned += HandleCleaned;
        }

        if (stains.Length == 0)
            Debug.LogWarning($"StainManager '{name}' has no Stain children.", this);
    }

    private void OnDestroy()
    {
        foreach (Stain stain in stains)
        {
            if (stain != null)
                stain.Cleaned -= HandleCleaned;
        }
    }

    private void HandleCleaned(Stain stain)
    {
        stain.Cleaned -= HandleCleaned;
        remaining = Mathf.Max(0, remaining - 1);

        if (remaining == 0)
            Solved?.Invoke();
    }
}
