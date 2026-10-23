using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Parts")]
    [SerializeField] private Transform hinge;
    [SerializeField] private Transform panel;

    [Header("Swing")]
    [SerializeField] private float openAngle = 80f;
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private float closeSpeed = 360f;

    [Header("Menu")]
    [SerializeField] private float menuOpenAngle = 30f;
    [SerializeField] private float menuOpenSpeed = 40f;
    
    [Header("Sounds")]
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;
    [SerializeField] private EventReference squeakSound;
    [SerializeField] private SoundLabel openStyle = new SoundLabel("HardSoft", "Soft");
    [SerializeField] private SoundLabel closeStyle = new SoundLabel("HardSoft", "Hard");
    [SerializeField] private SoundLabel squeakStyle = new SoundLabel("FastMedSlow", "Fast");

    [Header("Lock")]
    [SerializeField] private float passClearance = 0.6f;

    private enum DoorState { Closed, Opening, Open, Closing, Locked }

    private DoorState state = DoorState.Closed;
    private Transform passer;
    private float passerRadius;
    private float entrySide;
    private Coroutine swingRoutine;
    private Vector3 localCrossingAxis = Vector3.forward;
    private Vector3 localPlanePoint;
    private Collider[] panelColliders;
    private Transform referenceSpace;
    private Quaternion closedHingeRotation;

    private Transform ReferenceSpace => referenceSpace;

    public bool IsLocked => state == DoorState.Locked;
    public bool CanInteract => state == DoorState.Closed;

    public event Action Locked;

    private void Awake()
    {
        WarnAboutRootScale();
        closedHingeRotation = hinge.localRotation;
        panelColliders = panel.GetComponentsInChildren<Collider>();
        referenceSpace = hinge == transform || transform.IsChildOf(hinge) ? hinge.parent : transform.parent;
        localCrossingAxis = ToReferenceDirection(FindCrossingAxis());
        localPlanePoint = ToReferencePoint(hinge.position);
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

        passer = FindBody(interactor, out passerRadius);
        entrySide = FindEntrySide(passer);

        float targetAngle = entrySide > 0f ? openAngle : -openAngle;
        // open + squeak as it starts swinging open
        PlaySound(openSound, openStyle);
        PlaySound(squeakSound, squeakStyle);
        Swing(targetAngle, openSpeed, DoorState.Opening, DoorState.Open);
    }

    public void MenuSwingOpen()
    {
        MenuSwingOpen(null);
    }

    public void MenuSwingOpen(Action onComplete)
    {
        if (state == DoorState.Locked)
        {
            onComplete?.Invoke();
            return;
        }

        passer = null;
        // same sounds for the menu door
        PlaySound(openSound, openStyle);
        PlaySound(squeakSound, squeakStyle);
        Swing(menuOpenAngle, menuOpenSpeed, DoorState.Opening, DoorState.Open, onComplete);
    }

    private void Update()
    {
        if (passer == null) return;
        if (state != DoorState.Opening && state != DoorState.Open) return;

        float side = SideOf(passer.position);
        float clearance = Mathf.Max(passClearance, passerRadius + 0.05f);

        if (side * entrySide < 0f && Mathf.Abs(side) >= clearance)
        {
            Debug.Log($"Door: '{name}' passed by '{passer.name}' ({side:F2} from the door), closing.", this);
            passer = null;
            // squeak as it starts swinging shut
            PlaySound(squeakSound, squeakStyle);
            Swing(0f, closeSpeed, DoorState.Closing, DoorState.Locked);
        }
    }

    private Transform FindBody(GameObject interactor, out float radius)
    {
        CharacterController body = interactor.GetComponentInParent<CharacterController>();

        if (body == null)
            body = interactor.transform.root.GetComponentInChildren<CharacterController>();

        if (body == null)
        {
            radius = 0.5f;
            return interactor.transform.root;
        }

        radius = body.radius * Mathf.Max(body.transform.lossyScale.x, body.transform.lossyScale.z);
        return body.transform;
    }

    private float FindEntrySide(Transform body)
    {
        float side = SideOf(body.position);

        if (Mathf.Abs(side) > 0.01f)
            return Mathf.Sign(side);

        Vector3 crossingAxis = FromReferenceDirection(localCrossingAxis);
        return -Mathf.Sign(Vector3.Dot(body.forward, crossingAxis));
    }

    private void OnDrawGizmosSelected()
    {
        if (hinge == null || !Application.isPlaying) return;

        Vector3 crossingAxis = FromReferenceDirection(localCrossingAxis);
        Vector3 planePoint = FromReferencePoint(localPlanePoint);
        float clearance = Mathf.Max(passClearance, passerRadius + 0.05f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(planePoint, planePoint + crossingAxis * clearance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(planePoint, planePoint - crossingAxis * clearance);

        if (passer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(passer.position, 0.15f);
        }
    }

    private void WarnAboutRootScale()
    {
        Vector3 scale = transform.localScale;
        bool uniform = Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.y, scale.z);
    }

    private Vector3 FindCrossingAxis()
    {
        Vector3 hingeUp = hinge.up;
        Vector3 alongDoor = Vector3.ProjectOnPlane(GetPanelCenter() - hinge.position, hingeUp);

        if (alongDoor.sqrMagnitude < 0.0001f)
            return transform.forward;
        return Vector3.Cross(hingeUp, alongDoor).normalized;
    }

    private Vector3 GetPanelCenter()
    {
        Renderer[] renderers = panel.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds.center;
        }

        if (panelColliders.Length > 0)
        {
            return panelColliders[0].bounds.center;
        }

        return panel.position;
    }

    private float SideOf(Vector3 position)
    {
        Vector3 crossingAxis = FromReferenceDirection(localCrossingAxis);
        Vector3 planePoint = FromReferencePoint(localPlanePoint);
        return Vector3.Dot(position - planePoint, crossingAxis);
    }


    private Vector3 ToReferenceDirection(Vector3 worldDirection)
    {
        return ReferenceSpace != null ? ReferenceSpace.InverseTransformDirection(worldDirection) : worldDirection;
    }

    private Vector3 FromReferenceDirection(Vector3 referenceDirection)
    {
        return ReferenceSpace != null ? ReferenceSpace.TransformDirection(referenceDirection).normalized : referenceDirection;
    }

    private Vector3 ToReferencePoint(Vector3 worldPoint)
    {
        return ReferenceSpace != null ? ReferenceSpace.InverseTransformPoint(worldPoint) : worldPoint;
    }

    private Vector3 FromReferencePoint(Vector3 referencePoint)
    {
        return ReferenceSpace != null ? ReferenceSpace.TransformPoint(referencePoint) : referencePoint;
    }
    private void PlaySound(EventReference sound, SoundLabel style)
    {
        SoundPlayer.PlayOneShot(sound, hinge.position, style);
    }

    private void SetPanelSolid(bool solid)
    {
        foreach (Collider panelCollider in panelColliders)
        {
            panelCollider.enabled = solid;
        }
    }

    private void Swing(float targetAngle, float speed, DoorState during, DoorState after, Action onComplete = null)
    {
        if (swingRoutine != null)
        {
            StopCoroutine(swingRoutine);
        }

        swingRoutine = StartCoroutine(SwingRoutine(targetAngle, speed, during, after, onComplete));
    }

    private IEnumerator SwingRoutine(float targetAngle, float speed, DoorState during, DoorState after, Action onComplete)
    {
        state = during;
        SetPanelSolid(false);

        Quaternion target = closedHingeRotation * Quaternion.Euler(0f, targetAngle, 0f);

        while (Quaternion.Angle(hinge.localRotation, target) > 0.01f)
        {
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, speed * Time.deltaTime);
            yield return null;
        }

        hinge.localRotation = target;
        SetPanelSolid(true);
        state = after;
        swingRoutine = null;

        onComplete?.Invoke();

        if (state == DoorState.Locked)
        {
            // slam sound once it's fully shut
            PlaySound(closeSound, closeStyle);
            Locked?.Invoke();
        }
    }
}