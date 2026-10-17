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

    private PlayerInputReader input;
    private float pitch;

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
        if(PauseManager.Instance.IsPaused) 
            return;
        
        Vector2 look = input.Look * lookSensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch += invertY ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPos.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
