using System.Collections.Generic;
using UnityEngine;
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private GameObject interactorRoot;

    private readonly List<IInteractable> interactablesInRange = new List<IInteractable>();
    private PlayerInputReader input;
    private IInteractable currentTarget;
    private InteractIndicator currentIndicator;

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
        SetTarget(null);
    }

    private void Update()
    {
        SetTarget(FindNearest());
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
        IInteractable nearest = FindNearest();
        nearest?.OnInteract(interactorRoot);
        SetTarget(FindNearest());
    }

    private IInteractable FindNearest()
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

        return nearest;
    }

    private void SetTarget(IInteractable target)
    {
        if (ReferenceEquals(target, currentTarget)) return;

        if (currentIndicator != null)
        {
            currentIndicator.Hide();
        }

        currentTarget = target;
        currentIndicator = null;

        if (currentTarget is Component component && component != null)
        {
            currentIndicator = component.GetComponentInChildren<InteractIndicator>(true);

            if (currentIndicator != null)
            {
                currentIndicator.Show();
            }
        }
    }
}