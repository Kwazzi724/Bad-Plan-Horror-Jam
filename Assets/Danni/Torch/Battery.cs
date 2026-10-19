using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Battery : MonoBehaviour, IInteractable
{
    public bool CanInteract => true;

    public void OnInteract(GameObject interactor)
    {
        Torch torch = interactor.transform.root.GetComponentInChildren<Torch>(true);

        if (torch == null)
            torch = Torch.Active;

        if (torch == null) return;

        torch.Refill();
        Destroy(gameObject);
    }
}
