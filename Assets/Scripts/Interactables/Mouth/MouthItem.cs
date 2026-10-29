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
    [SerializeField, Range(0f, 1f)] private float slottedVolume = 0.3f;
    [SerializeField] private float volumeChangeDuration = 1f;
    [SerializeField] private float voiceFadeOutDuration = 2f;

    private Pickup pickup;
    private EventInstance voice;
    private float volume = 1f;
    private float targetVolume = 1f;

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

    private void OnEnable()
    {
        pickup.PickedUp += HandlePickedUp;
    }

    private void OnDisable()
    {
        pickup.PickedUp -= HandlePickedUp;
        FadeOutVoice();
    }

    private void Update()
    {
        if (!voice.isValid()) return;

        voice.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));

        if (!Mathf.Approximately(volume, targetVolume))
        {
            float step = volumeChangeDuration > 0f ? Time.deltaTime / volumeChangeDuration : 1f;
            volume = Mathf.MoveTowards(volume, targetVolume, step);
            voice.setVolume(volume);
        }

        voice.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
            voice.start();
    }

    private void HandleStageEntered(Stage stage, int stageNumber)
    {
        FadeOutVoice();
    }

    private void HandlePickedUp(Pickup item)
    {
        if (GetComponentInParent<Mannequin>() != null)
            SoundPlayer.PlayOneShot(grabSound, transform.position);

        StartVoice();
    }

    public void StartVoice()
    {
        targetVolume = 1f;

        if (voice.isValid())
            return;

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

        volume = targetVolume;
        voice.setVolume(volume);
        voice.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        voice.start();
    }

    public void Quieten()
    {
        targetVolume = slottedVolume;
    }

    public void FadeOutVoice()
    {
        if (!voice.isValid()) return;

        SoundFader.FadeOutAndRelease(voice, voiceFadeOutDuration);
        voice.clearHandle();
    }

    public void StopVoice()
    {
        if (!voice.isValid()) return;

        voice.stop(STOP_MODE.IMMEDIATE);
        voice.release();
        voice.clearHandle();
    }
}