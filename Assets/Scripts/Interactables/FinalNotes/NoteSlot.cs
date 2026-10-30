using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class NoteSlot : MonoBehaviour
{
    private static readonly List<NoteSlot> active = new List<NoteSlot>();

    public static IReadOnlyList<NoteSlot> Active => active;

    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float detectRadius = 0.3f;
    [SerializeField] private Vector3 snapOffset = new Vector3(0f, 0.01f, 0f);
    [SerializeField] private Vector3 snapRotation = new Vector3(90f, 0f, 0f);
    [SerializeField] private EventReference placeSound;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private bool highlighted;
    private Transform snapPoint;
    private Note occupant;

    public bool IsOccupied => occupant != null;
    public Note Occupant => occupant;
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

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;

        snapPoint = new GameObject("Note Snap Point").transform;
        snapPoint.SetParent(transform, false);
        snapPoint.localPosition = snapOffset;
        snapPoint.localRotation = Quaternion.Euler(snapRotation);
    }

    private void OnEnable()
    {
        if (!active.Contains(this))
            active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
        SetHighlighted(false);
    }

    public bool Place(Note note)
    {
        if (note == null || IsOccupied) return false;

        occupant = note;
        SetHighlighted(false);

        note.Pickup.PlaceInto(snapPoint);
        note.Pickup.PickedUp += HandleTaken;

        SoundPlayer.PlayOneShot(placeSound, snapPoint.position);
        return true;
    }

    private void HandleTaken(Pickup item)
    {
        item.PickedUp -= HandleTaken;
        occupant = null;
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
}
