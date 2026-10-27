using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private GameObject interactorRoot;
    [SerializeField] private Transform holdPose;

    private readonly List<IInteractable> interactablesInRange = new List<IInteractable>();
    private PlayerInputReader input;
    private IInteractable currentTarget;
    private InteractIndicator currentIndicator;
    private IHoldInteractable heldInteractable;
    private bool cursorUnlocked;
    private PlayerMovement movement;
    private PlayerLook look;
    private Pickup carried;

    public Pickup Carried => carried;

    private void Awake()
    {
        input = GetComponentInParent<PlayerInputReader>();
        movement = GetComponentInParent<PlayerMovement>();
        look = GetComponentInParent<PlayerLook>();

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
            input.Released += HandleReleased;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.Interacted -= HandleInteracted;
            input.Released -= HandleReleased;
        }

        EndHold();
        interactablesInRange.Clear();
        SetTarget(null);
    }

    private void Update()
    {
        if (heldInteractable != null)
        {
            if (heldInteractable as Object == null)
                EndHold();

            SetTarget(null);
            return;
        }

        if (carried != null)
        {
            SetTarget(FindNearestReceiver() as IInteractable);
            return;
        }

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
        if (heldInteractable != null) return;

        if (carried != null)
        {
            ItemReceiver receiver = FindNearestReceiver();

            if (receiver != null)
                receiver.Receive(carried);
            else
                carried.Drop();

            carried = null;
            SetTarget(FindNearest());
            return;
        }

        IInteractable nearest = FindNearest();
        if (nearest == null) return;

        if (nearest is Pickup pickup)
        {
            if (holdPose == null)
            {
                Debug.LogWarning("PlayerInteractor: no Hold Pose assigned, can't pick things up.", this);
                return;
            }

            carried = pickup;
            pickup.PickUp(holdPose);
            SetTarget(null);
            return;
        }

        nearest.OnInteract(interactorRoot);

        if (nearest is IHoldInteractable holdable)
        {
            BeginHold(holdable);
            return;
        }

        SetTarget(FindNearest());
    }

    private void HandleReleased()
    {
        if (heldInteractable == null) return;

        IHoldInteractable released = heldInteractable;
        EndHold();
        released.EndInteract();
    }

    private void BeginHold(IHoldInteractable holdable)
    {
        heldInteractable = holdable;
        heldInteractable.Finished += HandleHoldFinished;
        SetTarget(null);

        if (holdable.UsesCursor)
        {
            cursorUnlocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (movement != null)
            movement.Frozen = true;

        if (look != null)
        {
            look.Frozen = true;
            look.FaceTowards(holdable.FocusPoint.position);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ShowHoldPrompt(holdable.HoldPrompt);
    }

    private void HandleHoldFinished()
    {
        EndHold();
    }

    private void EndHold()
    {
        if (heldInteractable != null)
            heldInteractable.Finished -= HandleHoldFinished;

        if (cursorUnlocked)
        {
            cursorUnlocked = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        heldInteractable = null;

        if (movement != null)
            movement.Frozen = false;

        if (look != null)
            look.Frozen = false;

        if (GameManager.Instance != null)
            GameManager.Instance.HideHoldPrompt();
    }

    private ItemReceiver FindNearestReceiver()
    {
        interactablesInRange.RemoveAll(item => item as Object == null);

        ItemReceiver nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (IInteractable interactable in interactablesInRange)
        {
            if (!(interactable is ItemReceiver receiver) || !receiver.CanReceive(carried)) continue;

            Component component = interactable as Component;
            float distance = (component.transform.position - transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = receiver;
            }
        }

        return nearest;
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