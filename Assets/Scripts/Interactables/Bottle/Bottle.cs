using System;
using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class Bottle : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float vanishDuration = 0.25f;
    [SerializeField] private EventReference clearSound;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Collider[] colliders;
    private Rigidbody body;
    private bool highlighted;

    public bool IsCleared { get; private set; }
    public bool CanInteract => !IsCleared;

    public event Action<Bottle> Cleared;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;

        colliders = GetComponentsInChildren<Collider>(true);
        body = GetComponent<Rigidbody>();
    }

    public void OnInteract(GameObject interactor)
    {
        Clear();
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

    public void Clear()
    {
        if (IsCleared) return;

        IsCleared = true;
        SetHighlighted(false);

        if (body != null)
            body.isKinematic = true;

        foreach (Collider bottleCollider in colliders)
            bottleCollider.enabled = false;

        SoundPlayer.PlayOneShot(clearSound, transform.position);
        Cleared?.Invoke(this);

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
