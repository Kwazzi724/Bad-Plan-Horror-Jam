using System.Collections;
using FMODUnity;
using UnityEngine;

public class TriggeredMoveSequence : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private GameObject target;
    [SerializeField] private PlayerTrigger activateTrigger;
    [SerializeField] private PlayerTrigger moveTrigger;
    [SerializeField] private PlayerTrigger soundTrigger;

    [Header("Move")]
    [SerializeField] private Vector3 moveOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private Space moveSpace = Space.Self;
    [SerializeField] private float moveSpeed = 1f;

    [Header("Sounds")]
    [SerializeField] private EventReference discoverySound;

    private bool isActivated;
    private bool isMoving;
    private bool soundPlayed;

    private void Awake()
    {
        if (target != null)
            target.SetActive(false);

        if (soundTrigger != null)
            soundTrigger.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (activateTrigger != null)
            activateTrigger.Entered += HandleActivateEntered;

        if (moveTrigger != null)
            moveTrigger.Entered += HandleMoveEntered;

        if (soundTrigger != null)
            soundTrigger.Entered += HandleSoundEntered;
    }

    private void OnDisable()
    {
        if (activateTrigger != null)
            activateTrigger.Entered -= HandleActivateEntered;

        if (moveTrigger != null)
            moveTrigger.Entered -= HandleMoveEntered;

        if (soundTrigger != null)
            soundTrigger.Entered -= HandleSoundEntered;
    }

    private void HandleActivateEntered()
    {
        if (isActivated || target == null) return;

        isActivated = true;
        target.SetActive(true);

        if (soundTrigger != null && !soundPlayed)
        {
            soundTrigger.gameObject.SetActive(true);
            soundTrigger.Entered -= HandleSoundEntered;
            soundTrigger.Entered += HandleSoundEntered;
        }
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

    private void HandleSoundEntered()
    {
        if (soundPlayed) return;

        soundPlayed = true;

        Vector3 position = target != null ? target.transform.position : soundTrigger.transform.position;
        SoundPlayer.PlayOneShot(discoverySound, position);

        if (soundTrigger != null)
            soundTrigger.gameObject.SetActive(false);
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