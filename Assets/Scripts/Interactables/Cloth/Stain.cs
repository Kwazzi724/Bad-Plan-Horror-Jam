using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Stain : MonoBehaviour
{
    private static readonly List<Stain> active = new List<Stain>();

    public static IReadOnlyList<Stain> Active => active;

    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float detectRadius = 0.15f;
    [SerializeField] private float vanishDuration = 0.25f;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private bool highlighted;

    public bool IsClean { get; private set; }
    public float DetectRadius => detectRadius;

    public Vector3 Center
    {
        get
        {
            if (renderers == null || renderers.Length == 0) return transform.position;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.center;
        }
    }

    public event Action<Stain> Cleaned;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;
    }

    private void OnEnable()
    {
        if (!IsClean && !active.Contains(this))
            active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    public void SetHighlighted(bool on)
    {
        if (highlighted == on || outlineMaterial == null) return;

        highlighted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Material[] original = originalMaterials[i];

            if (!on)
            {
                renderers[i].sharedMaterials = original;
                continue;
            }

            Material[] withOutline = new Material[original.Length + 1];
            original.CopyTo(withOutline, 0);
            withOutline[original.Length] = outlineMaterial;
            renderers[i].sharedMaterials = withOutline;
        }
    }

    public void Clean()
    {
        if (IsClean) return;

        IsClean = true;
        SetHighlighted(false);
        active.Remove(this);
        Cleaned?.Invoke(this);

        if (vanishDuration <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.DOScale(Vector3.zero, vanishDuration)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
