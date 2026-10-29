using System;
using DG.Tweening;
using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private float snapDuration = 0.15f;

    private Rigidbody body;
    private Collider[] colliders;
    private bool originalKinematic;
    private Transform originalParent;
    private Tween moveTween;
    private Tween rotateTween;

    public event Action<Pickup> PickedUp;

    public bool IsHeld { get; private set; }
    public bool Locked { get; set; }
    public bool CanInteract => !IsHeld && !Locked;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = Array.FindAll(GetComponentsInChildren<Collider>(), itemCollider => !itemCollider.isTrigger);
        originalParent = transform.parent;

        if (body != null)
            originalKinematic = body.isKinematic;
    }

    public void OnInteract(GameObject interactor)
    {
    }

    public void PickUp(Transform holdPose)
    {
        if (IsHeld || holdPose == null) return;

        PickedUp?.Invoke(this);

        IsHeld = true;

        if (body != null)
            body.isKinematic = true;

        SetCollidersEnabled(false);
        SnapTo(holdPose);
    }

    public void Drop()
    {
        if (!IsHeld) return;

        IsHeld = false;
        KillTweens();

        Transform dropParent = null;

        if (GameManager.Instance != null && GameManager.Instance.CurrentStage != null)
            dropParent = GameManager.Instance.CurrentStage.transform;
        else if (originalParent != null)
            dropParent = originalParent;

        transform.SetParent(dropParent, true);
        SetCollidersEnabled(true);

        if (body != null)
        {
            body.isKinematic = originalKinematic;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }

    public void PlaceInto(Transform point)
    {
        if (point == null) return;

        IsHeld = false;

        if (body != null)
            body.isKinematic = true;

        SetCollidersEnabled(true);
        SnapTo(point);
    }

    private void SnapTo(Transform point)
    {
        transform.SetParent(point, true);
        KillTweens();

        if (snapDuration <= 0f)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            return;
        }

        moveTween = transform.DOLocalMove(Vector3.zero, snapDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        rotateTween = transform.DOLocalRotateQuaternion(Quaternion.identity, snapDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider itemCollider in colliders)
            itemCollider.enabled = enabled;
    }

    private void KillTweens()
    {
        moveTween?.Kill();
        rotateTween?.Kill();
        moveTween = null;
        rotateTween = null;
    }
}