using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

[RequireComponent(typeof(Pickup))]
public class MouthItem : MonoBehaviour
{
    [SerializeField] private int id = 1;
    [SerializeField] private EventReference grabSound;
    [SerializeField] private string voicePathFormat = "event:/SFX/Objects/Mouth_{0:00}";
    [SerializeField] private float voiceFadeOutDuration = 1f;

    private Pickup pickup;
    private EventInstance voice;
    private bool isSpeaking;
    private bool isFading;
    private float fadeTimer;

    public int Id => id;
    public Pickup Pickup => pickup != null ? pickup : pickup = GetComponent<Pickup>();

    private void Awake()
    {
        if (pickup == null)
            pickup = GetComponent<Pickup>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered += HandleStageEntered;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered -= HandleStageEntered;
    }

    private void HandleStageEntered(Stage stage, int stageNumber)
    {
        FadeOutVoice();
    }

    private void OnEnable()
    {
        pickup.PickedUp += HandlePickedUp;
    }

    private void OnDisable()
    {
        pickup.PickedUp -= HandlePickedUp;
        ReleaseVoice();
    }

    private void Update()
    {
        if (!voice.isValid()) return;

        voice.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));

        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            float t = voiceFadeOutDuration > 0f ? Mathf.Clamp01(fadeTimer / voiceFadeOutDuration) : 1f;
            voice.setVolume(1f - t);

            if (t >= 1f)
                ReleaseVoice();

            return;
        }

        if (!isSpeaking) return;

        voice.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
            voice.start();
    }

    private void HandlePickedUp(Pickup item)
    {
        if (GetComponentInParent<Mannequin>() != null)
            SoundPlayer.PlayOneShot(grabSound, transform.position);

        StartVoice();
    }

    public void StartVoice()
    {
        if (isSpeaking && !isFading) return;

        ReleaseVoice();

        string path = string.Format(voicePathFormat, id);

        try
        {
            voice = RuntimeManager.CreateInstance(path);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"MouthItem '{name}': no FMOD event at '{path}'.", this);
            return;
        }

        voice.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        voice.start();
        isSpeaking = true;
    }

    public void FadeOutVoice()
    {
        if (!voice.isValid() || isFading) return;

        isSpeaking = false;
        isFading = true;
        fadeTimer = 0f;
    }

    private void ReleaseVoice()
    {
        isSpeaking = false;
        isFading = false;

        if (!voice.isValid()) return;

        voice.stop(STOP_MODE.IMMEDIATE);
        voice.release();
        voice.clearHandle();
    }
}