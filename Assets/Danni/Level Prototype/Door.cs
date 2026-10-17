using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Parts")]
    [SerializeField] private Transform hinge;
    [SerializeField] private Transform panel;
    [Header("Swing")]
    [SerializeField] private float openAngle = 80f;
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private float closeSpeed = 240f;
    [Header("Lock")]
    [SerializeField] private float passDistance = 1f;
    [SerializeField] private float closeDelay = 1.5f;

    private enum DoorState { Closed, Opening, Open, WaitingToClose, Closing, Locked }

    private DoorState state = DoorState.Closed;
    private Transform passer;
    private float entrySide;
    private Coroutine swingRoutine;

    public bool IsLocked => state == DoorState.Locked;

    private void Awake()
    {
        NormaliseScale();
    }

    public void Configure(float width, float height, float thickness)
    {
        transform.localScale = Vector3.one;
        hinge.localScale = Vector3.one;
        hinge.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        hinge.localRotation = Quaternion.identity;
        panel.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f);
        panel.localRotation = Quaternion.identity;
        panel.localScale = new Vector3(width, height, thickness);
    }

    public void OnInteract(GameObject interactor)
    {
        if (state != DoorState.Closed) return;

        passer = interactor.transform;
        entrySide = Mathf.Sign(SideOf(passer.position));

        float targetAngle = entrySide > 0f ? openAngle : -openAngle;
        Swing(targetAngle, openSpeed, DoorState.Opening, DoorState.Open);
    }

    private void Update()
    {
        if (passer == null) return;
        if (state != DoorState.Opening && state != DoorState.Open) return;

        float side = SideOf(passer.position);

        if (Mathf.Sign(side) != entrySide && Mathf.Abs(side) >= passDistance)
        {
            passer = null;

            if (swingRoutine != null)
            {
                StopCoroutine(swingRoutine);
            }

            swingRoutine = StartCoroutine(CloseAfterDelay());
        }
    }

    private void NormaliseScale()
    {
        if (transform.localScale == Vector3.one && hinge.localScale == Vector3.one) return;
        Vector3 hingePosition = hinge.position;
        Quaternion hingeRotation = hinge.rotation;
        Vector3 panelPosition = panel.position;
        Quaternion panelRotation = panel.rotation;
        Vector3 panelScale = panel.lossyScale;
        transform.localScale = Vector3.one;
        hinge.localScale = Vector3.one;
        hinge.SetPositionAndRotation(hingePosition, hingeRotation);
        panel.SetPositionAndRotation(panelPosition, panelRotation);
        panel.localScale = panelScale;
    }

    private IEnumerator CloseAfterDelay()
    {
        state = DoorState.WaitingToClose;
        float targetAngle = entrySide > 0f ? openAngle : -openAngle;
        Quaternion fullyOpen = Quaternion.Euler(0f, targetAngle, 0f);

        while (Quaternion.Angle(hinge.localRotation, fullyOpen) > 0.01f)
        {
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, fullyOpen, openSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(closeDelay);
        swingRoutine = null;
        Swing(0f, closeSpeed, DoorState.Closing, DoorState.Locked);
    }

    private float SideOf(Vector3 position)
    {
        return Vector3.Dot(position - transform.position, transform.forward);
    }

    private void Swing(float targetAngle, float speed, DoorState during, DoorState after)
    {
        if (swingRoutine != null)
            StopCoroutine(swingRoutine);
        swingRoutine = StartCoroutine(SwingRoutine(targetAngle, speed, during, after));
    }

    private IEnumerator SwingRoutine(float targetAngle, float speed, DoorState during, DoorState after)
    {
        state = during;
        Quaternion target = Quaternion.Euler(0f, targetAngle, 0f);
        while (Quaternion.Angle(hinge.localRotation, target) > 0.01f)
        {
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, speed * Time.deltaTime);
            yield return null;
        }
        hinge.localRotation = target;
        state = after;
        swingRoutine = null;
    }
}