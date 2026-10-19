using FMODUnity;
using UnityEngine;

public class Torch : MonoBehaviour
{
    public static Torch Active { get; private set; }

    [Header("Parts")]
    [SerializeField] private Light torchLight;
    [SerializeField] private Transform aimSource;
    [SerializeField] private float aimFollowSpeed = 0f;

    [Header("Battery")]
    [SerializeField] private float batteryLifeSeconds = 120f;
    [SerializeField, Range(0f, 1f)] private float dimStartPercent = 0.1f;
    [SerializeField] private bool startOn = true;

    [Header("Sounds")]
    [SerializeField] private EventReference toggleSound;
    [SerializeField] private string toggleParameter = "OnOff";
    [SerializeField] private string onLabel = "On";
    [SerializeField] private string offLabel = "Off";

    public float BatteryPercent => battery;
    public bool IsOn => isOn;

    private PlayerInputReader input;
    private float battery = 1f;
    private float baseIntensity;
    private bool isOn;

    private bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused;

    private void Awake()
    {
        if (torchLight == null)
            torchLight = GetComponentInChildren<Light>();

        baseIntensity = torchLight.intensity;
        input = GetComponentInParent<PlayerInputReader>();

        if (aimSource == null)
        {
            Camera playerCamera = transform.root.GetComponentInChildren<Camera>();
            if (playerCamera != null)
                aimSource = playerCamera.transform;
        }

        isOn = startOn;
        ApplyLight();
    }

    private void OnEnable()
    {
        Active = this;

        if (input != null)
            input.TorchToggled += Toggle;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;

        if (input != null)
            input.TorchToggled -= Toggle;
    }

    private void Update()
    {
        if (IsPaused || !isOn) return;

        battery = Mathf.Max(0f, battery - Time.deltaTime / batteryLifeSeconds);

        if (battery <= 0f)
            isOn = false;

        ApplyLight();
    }

    private void LateUpdate()
    {
        if (aimSource == null) return;

        if (aimFollowSpeed <= 0f)
        {
            transform.rotation = aimSource.rotation;
            return;
        }

        float blend = 1f - Mathf.Exp(-aimFollowSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, aimSource.rotation, blend);
    }

    public void Toggle()
    {
        if (IsPaused) return;

        string label = isOn ? offLabel : onLabel;
        SoundPlayer.PlayOneShot(toggleSound, transform.position, new SoundLabel(toggleParameter, label));

        if (!isOn && battery <= 0f) return;

        isOn = !isOn;
        ApplyLight();
    }

    public void Refill()
    {
        battery = 1f;
        ApplyLight();
    }

    private void ApplyLight()
    {
        float dim = dimStartPercent > 0f ? Mathf.Clamp01(battery / dimStartPercent) : 1f;

        torchLight.enabled = isOn && battery > 0f;
        torchLight.intensity = baseIntensity * dim;
    }
}