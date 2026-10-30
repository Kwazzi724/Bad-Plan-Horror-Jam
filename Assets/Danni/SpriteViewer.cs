using FMODUnity;
using UnityEngine;

public class SpriteViewer : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private EventReference openSound;

    public bool CanInteract => sprite != null && ImagePanel.Instance != null && !ImagePanel.Instance.IsOpen;

    public void OnInteract(GameObject interactor)
    {
        if (!CanInteract) return;

        PlayerInputReader reader = interactor.GetComponentInParent<PlayerInputReader>();

        if (reader == null)
            reader = interactor.transform.root.GetComponentInChildren<PlayerInputReader>();

        if (ImagePanel.Instance.Open(sprite, reader))
            SoundPlayer.PlayOneShot(openSound, transform.position);
    }
}