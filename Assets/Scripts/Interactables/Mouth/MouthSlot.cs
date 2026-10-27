using FMODUnity;
using UnityEngine;

public class ItemSlot : MonoBehaviour, IInteractable, ItemReceiver
{
    [SerializeField] private Transform slotPoint;
    [SerializeField] private EventReference slotSound;

    private MouthItem slotted;

    public bool CanInteract => false;
    public MouthItem Slotted => slotted;

    public void OnInteract(GameObject interactor)
    {
    }

    public bool CanReceive(Pickup item)
    {
        return slotted == null && item != null && item.GetComponent<MouthItem>() != null;
    }

    public void Receive(Pickup item)
    {
        if (!CanReceive(item)) return;

        slotted = item.GetComponent<MouthItem>();
        item.PlaceInto(slotPoint != null ? slotPoint : transform);
        item.PickedUp += HandlePickedOut;

        SoundPlayer.PlayOneShot(slotSound, transform.position);

        if (slotted.IsCorrect)
            Correct();
        else
            Incorrect();
    }

    private void HandlePickedOut(Pickup item)
    {
        item.PickedUp -= HandlePickedOut;
        slotted = null;
    }

    public void Correct()
    {
    }

    public void Incorrect()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
}
