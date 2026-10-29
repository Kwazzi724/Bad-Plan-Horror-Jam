using FMODUnity;
using UnityEngine;

public class ImageInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string imageId;
    [SerializeField] private EventReference openSound;

    public bool CanInteract => ImagePanel.Instance != null && !ImagePanel.Instance.IsOpen;

    public void OnInteract(GameObject interactor)
    {
        if (!CanInteract) return;

        PlayerInputReader reader = interactor.GetComponentInParent<PlayerInputReader>();

        if (reader == null)
            reader = interactor.transform.root.GetComponentInChildren<PlayerInputReader>();

        ImagePanel.Instance.Open(imageId, reader);

        if (ImagePanel.Instance.IsOpen)
            SoundPlayer.PlayOneShot(openSound, transform.position);
    }
}