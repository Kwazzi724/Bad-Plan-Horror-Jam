using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private GameObject interactorRoot;

    private readonly List<IInteractable> interactablesInRange = new List<IInteractable>();
    private PlayerInputReader input;

    private void Awake()
    {
        input = GetComponentInParent<PlayerInputReader>();

        if (interactorRoot == null)
        {
            interactorRoot = input != null ? input.gameObject : transform.root.gameObject;
        }

        GetComponent<Collider>().isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.Interacted += HandleInteracted;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.Interacted -= HandleInteracted;
        }

        interactablesInRange.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if (interactable != null && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            interactablesInRange.Remove(interactable);
        }
    }

    private void HandleInteracted()
    {
        interactablesInRange.RemoveAll(item => item as Object == null);

        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (IInteractable interactable in interactablesInRange)
        {
            Component component = interactable as Component;
            float distance = (component.transform.position - transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = interactable;
            }
        }

        nearest?.OnInteract(interactorRoot);
    }
}