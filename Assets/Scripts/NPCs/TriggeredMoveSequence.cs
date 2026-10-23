using System.Collections;
using UnityEngine;

public class TriggeredMoveSequence : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private GameObject target;
    [SerializeField] private PlayerTrigger activateTrigger;
    [SerializeField] private PlayerTrigger moveTrigger;

    [Header("Move")]
    [SerializeField] private Vector3 moveOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private Space moveSpace = Space.Self;
    [SerializeField] private float moveSpeed = 1f;

    private bool isActivated;
    private bool isMoving;

    private void Awake()
    {
        if (target != null)
            target.SetActive(false);
    }

    private void OnEnable()
    {
        if (activateTrigger != null)
            activateTrigger.Entered += HandleActivateEntered;

        if (moveTrigger != null)
            moveTrigger.Entered += HandleMoveEntered;
    }

    private void OnDisable()
    {
        if (activateTrigger != null)
            activateTrigger.Entered -= HandleActivateEntered;

        if (moveTrigger != null)
            moveTrigger.Entered -= HandleMoveEntered;
    }

    private void HandleActivateEntered()
    {
        if (isActivated || target == null) return;

        isActivated = true;
        target.SetActive(true);
    }

    private void HandleMoveEntered()
    {
        if (!isActivated)
        {
            moveTrigger.ResetTrigger();
            return;
        }

        if (isMoving) return;

        isMoving = true;
        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        Transform targetTransform = target.transform;
        Vector3 offset = moveSpace == Space.Self ? targetTransform.TransformDirection(moveOffset) : moveOffset;
        Vector3 end = targetTransform.position + offset;

        while ((targetTransform.position - end).sqrMagnitude > 0.000001f)
        {
            targetTransform.position = Vector3.MoveTowards(targetTransform.position, end, moveSpeed * Time.deltaTime);
            yield return null;
        }

        targetTransform.position = end;

        target.SetActive(false);

        if (activateTrigger != null)
            activateTrigger.gameObject.SetActive(false);

        if (moveTrigger != null)
            moveTrigger.gameObject.SetActive(false);
    }
}
