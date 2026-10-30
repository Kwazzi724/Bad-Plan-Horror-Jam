using UnityEngine;

[RequireComponent(typeof(Pickup))]
public class Note : MonoBehaviour
{
    [SerializeField] private Collider detectionZone;
    [SerializeField] private Vector3 heldRotationOffset = Vector3.zero;

    private Pickup pickup;
    private NoteSlot target;

    public Pickup Pickup => pickup != null ? pickup : pickup = GetComponent<Pickup>();
    public NoteSlot Target => target;

    private void Awake()
    {
        pickup = GetComponent<Pickup>();

        if (detectionZone == null)
        {
            foreach (Collider child in GetComponentsInChildren<Collider>(true))
            {
                if (child.isTrigger)
                {
                    detectionZone = child;
                    break;
                }
            }
        }

        if (detectionZone != null)
            detectionZone.isTrigger = true;
        else
            Debug.LogWarning($"Note '{name}' has no trigger collider to detect slots with.", this);
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

    private void Update()
    {
        SetTarget(pickup.IsHeld ? FindSlot() : null);
    }

    private void LateUpdate()
    {
        if (!pickup.IsHeld) return;

        Camera viewer = Camera.main;
        if (viewer == null) return;

        transform.rotation = viewer.transform.rotation * Quaternion.Euler(heldRotationOffset);
    }

    public bool PlaceOnTarget()
    {
        if (target == null) return false;

        NoteSlot slot = target;
        SetTarget(null);
        return slot.Place(this);
    }

    private NoteSlot FindSlot()
    {
        if (detectionZone == null || !detectionZone.enabled || !detectionZone.gameObject.activeInHierarchy) return null;

        Vector3 zoneCenter = detectionZone.bounds.center;
        NoteSlot best = null;
        float bestDistance = float.MaxValue;

        foreach (NoteSlot slot in NoteSlot.Active)
        {
            if (slot == null || slot.IsOccupied) continue;

            Vector3 center = slot.Center;
            Vector3 closest = detectionZone.ClosestPoint(center);
            float radius = slot.DetectRadius;

            if ((closest - center).sqrMagnitude > radius * radius) continue;

            float distance = (center - zoneCenter).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = slot;
            }
        }

        return best;
    }

    private void SetTarget(NoteSlot slot)
    {
        if (slot == target) return;

        if (target != null)
            target.SetHighlighted(false);

        target = slot;

        if (target != null)
            target.SetHighlighted(true);
    }
}
