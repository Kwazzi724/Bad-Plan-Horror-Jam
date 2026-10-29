using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Pickup))]
public class MouthItem : MonoBehaviour
{
    [SerializeField] private EventReference grabSound;

    private Pickup pickup;

    public Pickup Pickup => pickup != null ? pickup : pickup = GetComponent<Pickup>();

    private void Awake()
    {
        if (pickup == null)
            pickup = GetComponent<Pickup>();
    }

    private void OnEnable()
    {
        pickup.PickedUp += HandlePickedUp;
    }

    private void OnDisable()
    {
        pickup.PickedUp -= HandlePickedUp;
    }

    private void HandlePickedUp(Pickup item)
    {
        if (GetComponentInParent<Mannequin>() != null)
            SoundPlayer.PlayOneShot(grabSound, transform.position);
    }
}