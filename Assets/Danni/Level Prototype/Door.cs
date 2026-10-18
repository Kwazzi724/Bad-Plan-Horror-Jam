using System;
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
    [SerializeField] private float closeSpeed = 360f;

    [Header("Lock")]
    [SerializeField] private float passClearance = 0.6f;

    private enum DoorState { Closed, Opening, Open, Closing, Locked }

    private DoorState state = DoorState.Closed;
    private Transform passer;
    private float entrySide;
    private Coroutine swingRoutine;
    private Vector3 localCrossingAxis = Vector3.forward;
    private Vector3 localPlanePoint;
    private Collider[] panelColliders;
    private Transform referenceSpace;

    private Transform ReferenceSpace => referenceSpace;

    public bool IsLocked => state == DoorState.Locked;

    public event Action Locked;

    private void Awake()
    {
        NormaliseScale();
        panelColliders = panel.GetComponentsInChildren<Collider>();
        referenceSpace = hinge == transform || transform.IsChildOf(hinge) ? hinge.parent : transform.parent;
        localCrossingAxis = ToReferenceDirection(FindCrossingAxis());
        localPlanePoint = ToReferencePoint(hinge.position);

        if (hinge == transform || transform.IsChildOf(hinge))
        {
            Debug.LogWarning($"Door: '{name}' has the Door script on the object that rotates. It still works, but ideally put it on a parent of the Hinge.", this);
        }
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

        if (Mathf.Sign(side) != entrySide && Mathf.Abs(side) >= passClearance)
        {
            passer = null;
            Swing(0f, closeSpeed, DoorState.Closing, DoorState.Locked);
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

    private void SetPanelSolid(bool solid)
    {
        foreach (Collider panelCollider in panelColliders)
        {
            panelCollider.enabled = solid;
        }
    }

    private void Swing(float targetAngle, float speed, DoorState during, DoorState after)
    {
        if (swingRoutine != null)
        {
            StopCoroutine(swingRoutine);
        }

        swingRoutine = StartCoroutine(SwingRoutine(targetAngle, speed, during, after));
    }

    private IEnumerator SwingRoutine(float targetAngle, float speed, DoorState during, DoorState after)
    {
        state = during;
        SetPanelSolid(false);

        Quaternion target = Quaternion.Euler(0f, targetAngle, 0f);

        while (Quaternion.Angle(hinge.localRotation, target) > 0.01f)
        {
            hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, target, speed * Time.deltaTime);
            yield return null;
        }

        hinge.localRotation = target;
        SetPanelSolid(true);
        state = after;
        swingRoutine = null;

        if (state == DoorState.Locked)
        {
            Debug.Log($"Door: '{name}' locked.", this);
            Locked?.Invoke();
        }
    }
}