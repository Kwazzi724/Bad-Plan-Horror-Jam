using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraPos;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool invertY;
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private float faceDuration = 0.35f;

    public bool Frozen { get; set; }

    private PlayerInputReader input;
    private float pitch;
    private Coroutine faceRoutine;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
    }

    private void OnEnable()
    {
        if (!lockCursor) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if(PauseManager.Instance.IsPaused || Frozen) 
            return;
        Vector2 look = input.Look * lookSensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch += invertY ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPos.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void FaceTowards(Vector3 point)
    {
        if (faceRoutine != null)
            StopCoroutine(faceRoutine);

        faceRoutine = StartCoroutine(FaceRoutine(point));
    }

    private IEnumerator FaceRoutine(Vector3 point)
    {
        Vector3 flatDirection = point - transform.position;
        flatDirection.y = 0f;

        Quaternion startYaw = transform.rotation;
        Quaternion targetYaw = flatDirection.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flatDirection) : startYaw;

        Vector3 fromCamera = point - cameraPos.position;
        float flatDistance = new Vector2(fromCamera.x, fromCamera.z).magnitude;
        float targetPitch = Mathf.Clamp(-Mathf.Atan2(fromCamera.y, flatDistance) * Mathf.Rad2Deg, minPitch, maxPitch);
        float startPitch = pitch;

        float elapsed = 0f;

        while (elapsed < faceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / faceDuration);

            transform.rotation = Quaternion.Slerp(startYaw, targetYaw, t);
            pitch = Mathf.Lerp(startPitch, targetPitch, t);
            cameraPos.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            yield return null;
        }

        transform.rotation = targetYaw;
        pitch = targetPitch;
        cameraPos.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        faceRoutine = null;
    }
}