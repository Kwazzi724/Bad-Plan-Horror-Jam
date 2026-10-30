using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

[Serializable]
public class PhoneStageBehaviour
{
    public int stage = 2;
    public bool rings = true;
    public float ringDelay = 5f;
    public bool playsVoice = true;
    public int voiceLoop = 0;
    public bool holdUntilVoiceEnds;
}

public class RotaryPhone : MonoBehaviour, IHoldInteractable
{
    [Header("Parts")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform focusPoint;

    [Header("Animation States")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string pickUpState = "PickUp";
    [SerializeField] private string hangUpState = "HangUp";

    [Header("Prompt")]
    [SerializeField] private string holdPrompt = "Right Click mouse / press [B] on controller\n to hang up phone";

    [Header("Sounds")]
    [SerializeField] private EventReference telephoneSound;
    [SerializeField] private string phoneActionParameter = "PhoneAction";
    [SerializeField] private string ringLabel = "Ring";
    [SerializeField] private string pickUpLabel = "PickUp";
    [SerializeField] private string hangUpLabel = "PutDown";
    [SerializeField] private string staticSoundLabel = "HangUp";
    [SerializeField] private EventReference staticSound;
    [SerializeField] private string loopParameter = "Loop";
    [SerializeField] private string chloeGlobalParameter = "Chloe";

    [Header("Ringing")]
    [SerializeField] private float heldRingDuration = 2f;

    [Header("Stages")]
    [SerializeField] private List<PhoneStageBehaviour> stageBehaviours = new List<PhoneStageBehaviour> { new PhoneStageBehaviour() };

    private Stage stage;
    private PhoneStageBehaviour currentBehaviour;
    private bool isActive;
    private bool isHeld;
    private bool hasRung;
    private bool isRinging;
    private Coroutine ringRoutine;
    private Coroutine heldRingRoutine;
    private EventInstance ringInstance;
    private EventInstance voiceInstance;
    private EventInstance staticInstance;

    public bool CanInteract => isActive && !isHeld;
    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
    public string HoldPrompt => holdPrompt;
    public bool UsesCursor => false;
    public bool CanRelease => !MustKeepListening;

    private bool MustKeepListening =>
        isHeld
        && currentBehaviour != null
        && currentBehaviour.holdUntilVoiceEnds
        && (voiceInstance.isValid() || isRinging);

    private bool HasVoice => currentBehaviour != null && currentBehaviour.playsVoice;

    public event Action Finished
    {
        add { }
        remove { }
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        stage = GetComponentInParent<Stage>();
        PlayAnimation(idleState);
    }

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StageEntered += HandleStageEntered;

        if (gameManager.CurrentStageNumber > 0 && (stage == null || gameManager.CurrentStage == stage))
            Activate(gameManager.CurrentStageNumber);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered -= HandleStageEntered;

        StopRinging();
        StopInstance(ref voiceInstance);
        StopInstance(ref staticInstance);
        SetChloe(false);
    }

    private void Update()
    {
        if (isRinging && IsStopped(ringInstance))
            ringInstance.start();

        if (!isHeld) return;

        if (voiceInstance.isValid())
        {
            if (IsStopped(voiceInstance))
            {
                StopInstance(ref voiceInstance);
                SetChloe(false);
                StartStatic();
            }
        }
        else if (IsStopped(staticInstance))
        {
            staticInstance.start();
        }
    }

    public void OnInteract(GameObject interactor)
    {
        if (!CanInteract) return;

        isHeld = true;
        PlayAnimation(pickUpState);

        if (isRinging)
        {
            AnswerRing();
            return;
        }

        if (hasRung && HasVoice)
        {
            StartListening();
            return;
        }

        SetChloe(false);
        SoundPlayer.PlayOneShot(telephoneSound, transform.position, new SoundLabel(phoneActionParameter, pickUpLabel));
        StartStatic();
    }

    public void EndInteract()
    {
        if (!isHeld) return;
        isHeld = false;
        StopHeldRing();

        StopInstance(ref voiceInstance);
        StopInstance(ref staticInstance);
        SetChloe(false);

        if (!PlayAnimation(hangUpState))
            PlayAnimation(idleState);

        SoundPlayer.PlayOneShot(telephoneSound, transform.position, new SoundLabel(phoneActionParameter, hangUpLabel));
    }

    private void HandleStageEntered(Stage enteredStage, int stageNumber)
    {
        if (stage == null || enteredStage == stage)
            Activate(stageNumber);
        else
            Deactivate();
    }

    private void Activate(int stageNumber)
    {
        isActive = true;
        hasRung = false;
        StopRinging();

        currentBehaviour = stageBehaviours.Find(behaviour => behaviour.stage == stageNumber);

        if (currentBehaviour != null && currentBehaviour.rings)
            ringRoutine = StartCoroutine(RingAfterDelay(currentBehaviour.ringDelay));
    }

    private void Deactivate()
    {
        isActive = false;
        StopRinging();
    }

    private IEnumerator RingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ringRoutine = null;
        StartRinging();
    }

    private void StartRinging()
    {
        hasRung = true;
        isRinging = true;

        StopInstance(ref ringInstance);
        ringInstance = CreateInstance(telephoneSound, ringLabel);
        if (ringInstance.isValid())
            ringInstance.start();

        if (isHeld)
        {
            StopInstance(ref voiceInstance);
            StopInstance(ref staticInstance);
            StopHeldRing();
            heldRingRoutine = StartCoroutine(HeldRing());
        }
    }

    private IEnumerator HeldRing()
    {
        yield return new WaitForSeconds(heldRingDuration);
        heldRingRoutine = null;

        if (isHeld && isRinging)
            AnswerRing();
    }

    private void AnswerRing()
    {
        StopHeldRing();
        isRinging = false;
        StopInstance(ref ringInstance);
        StopInstance(ref staticInstance);
        StartListening();
    }

    private void StopHeldRing()
    {
        if (heldRingRoutine == null) return;

        StopCoroutine(heldRingRoutine);
        heldRingRoutine = null;
    }

    private void StopRinging()
    {
        if (ringRoutine != null)
        {
            StopCoroutine(ringRoutine);
            ringRoutine = null;
        }

        StopHeldRing();
        isRinging = false;
        StopInstance(ref ringInstance);
    }

    private void StartListening()
    {
        StopInstance(ref voiceInstance);
        StopInstance(ref staticInstance);

        if (!HasVoice)
        {
            SetChloe(false);
            SoundPlayer.PlayOneShot(telephoneSound, transform.position, new SoundLabel(phoneActionParameter, pickUpLabel));
            StartStatic();
            return;
        }

        SetChloe(true);
        voiceInstance = CreateInstance(telephoneSound, pickUpLabel);

        if (voiceInstance.isValid())
        {
            if (!string.IsNullOrEmpty(loopParameter))
            {
                FMOD.RESULT result = voiceInstance.setParameterByName(loopParameter, currentBehaviour.voiceLoop);
                if (result != FMOD.RESULT.OK)
                    Debug.LogWarning($"RotaryPhone: couldn't set {loopParameter} = {currentBehaviour.voiceLoop} ({result}).", this);
            }

            voiceInstance.start();
            return;
        }

        SetChloe(false);
        StartStatic();
    }

    private void SetChloe(bool on)
    {
        if (string.IsNullOrEmpty(chloeGlobalParameter)) return;

        try
        {
            RuntimeManager.StudioSystem.setParameterByName(chloeGlobalParameter, on ? 1f : 0f);
        }
        catch (Exception)
        {
        }
    }

    private void StartStatic()
    {
        StopInstance(ref staticInstance);
        staticInstance = CreateInstance(staticSound, staticSoundLabel);
        if (staticInstance.isValid())
            staticInstance.start();
    }

    private EventInstance CreateInstance(EventReference sound, string label)
    {
        if (sound.IsNull) return default;

        EventInstance instance;

        try
        {
            instance = RuntimeManager.CreateInstance(sound);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RotaryPhone: couldn't create {sound} ({exception.GetType().Name}).", this);
            return default;
        }

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));

        if (!string.IsNullOrEmpty(label))
            instance.setParameterByNameWithLabel(phoneActionParameter, label);

        return instance;
    }

    private static bool IsStopped(EventInstance instance)
    {
        if (!instance.isValid()) return false;

        instance.getPlaybackState(out PLAYBACK_STATE playbackState);
        return playbackState == PLAYBACK_STATE.STOPPED;
    }

    private static void StopInstance(ref EventInstance instance)
    {
        if (!instance.isValid()) return;

        instance.stop(STOP_MODE.IMMEDIATE);
        instance.release();
        instance.clearHandle();
    }

    private bool PlayAnimation(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash)) return false;

        animator.Play(stateHash, 0, 0f);
        return true;
    }
}