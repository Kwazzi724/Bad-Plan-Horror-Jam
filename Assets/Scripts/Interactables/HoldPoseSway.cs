using UnityEngine;

public class HoldPoseSway : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform body;
    [SerializeField, Range(0f, 1f)] private float pitchFollow = 0.35f;
    [SerializeField] private float followSpeed = 10f;

    private Vector3 restOffset;
    private Quaternion restRotation;
    private Quaternion smoothedPivot;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            Camera parentCamera = GetComponentInParent<Camera>();
            cameraTransform = parentCamera != null ? parentCamera.transform : transform.parent;
        }

        if (body == null)
            body = transform.root;

        restOffset = Quaternion.Inverse(cameraTransform.rotation) * (transform.position - cameraTransform.position);
        restRotation = Quaternion.Inverse(cameraTransform.rotation) * transform.rotation;
        smoothedPivot = TargetPivot();
    }

    private void LateUpdate()
    {
        float blend = followSpeed > 0f ? 1f - Mathf.Exp(-followSpeed * Time.deltaTime) : 1f;
        smoothedPivot = Quaternion.Slerp(smoothedPivot, TargetPivot(), blend);

        transform.SetPositionAndRotation(cameraTransform.position + smoothedPivot * restOffset, smoothedPivot * restRotation);
    }

    private Quaternion TargetPivot()
    {
        Quaternion relative = Quaternion.Inverse(body.rotation) * cameraTransform.rotation;
        float pitch = Mathf.DeltaAngle(0f, relative.eulerAngles.x);
        return body.rotation * Quaternion.Euler(pitch * pitchFollow, 0f, 0f);
    }
}
