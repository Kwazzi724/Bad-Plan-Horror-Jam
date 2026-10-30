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
    public float voiceLength = 0f;
    public bool usesDialPad;
    public string correctNumber = "";
    public int correctVoiceLoop = 5;
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

    [Header("Dial Pad")]
    [SerializeField] private DialPad dialPad;
    [SerializeField] private BossTrigger bossTrigger;
    [SerializeField] private string dialPrompt = "Right Click mouse / press [B] on controller\n to put the phone down";
    [SerializeField] private string dialLabel = "Dial";
    [SerializeField] private string dialNumberParameter = "DialNumber";

    [Header("Ringing")]
    [SerializeField] private float heldRingDuration = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

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
    private float voiceStartTime;
    private PLAYBACK_STATE lastVoiceState = PLAYBACK_STATE.STOPPED;
    private bool isDialing;
    private bool correctCallActive;
    private bool dialSolved;

    public bool CanInteract => isActive && !isHeld && !dialSolved;
    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
    public string HoldPrompt => isDialing ? dialPrompt : holdPrompt;
    public bool UsesCursor => UsesDialPad;
    public bool CanRelease => !MustKeepListening;

    private bool UsesDialPad => currentBehaviour != null && currentBehaviour.usesDialPad && dialPad != null;

    private bool MustKeepListening =>
        isHeld
        && !isDialing
        && (voiceInstance.isValid() || isRinging)
        && (correctCallActive || (currentBehaviour != null && currentBehaviour.holdUntilVoiceEnds));

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

        if (bossTrigger == null && stage != null)
            bossTrigger = stage.GetComponentInChildren<BossTrigger>(true);

        if (dialPad != null && dialPad.gameObject.activeSelf)
            dialPad.gameObject.SetActive(false);
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

        if (dialPad != null && dialPad.IsOpen)
            dialPad.Close();
    }

    private void Update()
    {
        if (isRinging && IsStopped(ringInstance))
            ringInstance.start();

        if (!isHeld || isDialing) return;

        if (voiceInstance.isValid())
        {
            if (VoiceFinished())
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

        if (UsesDialPad)
        {
            OpenDialPad();
            return;
        }

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

        if (isDialing)
        {
            isDialing = false;
            isHeld = false;
            dialPad.Close();
            return;
        }

        isHeld = false;
        StopHeldRing();

        StopInstance(ref voiceInstance);
        StopInstance(ref staticInstance);
        SetChloe(false);

        if (!PlayAnimation(hangUpState))
            PlayAnimation(idleState);

        SoundPlayer.PlayOneShot(telephoneSound, transform.position, new SoundLabel(phoneActionParameter, hangUpLabel));

        if (correctCallActive)
        {
            correctCallActive = false;
            dialSolved = true;

            if (bossTrigger != null)
                bossTrigger.TriggerBossAfterDelay();
            else
                Debug.LogWarning("RotaryPhone: correct call finished but there's no BossTrigger in this stage.", this);
        }
    }

    private void OpenDialPad()
    {
        Camera viewer = Camera.main;
        if (viewer == null) return;

        isHeld = true;
        isDialing = true;
        dialPad.Open(viewer, PlayDialTone, HandleCall);
    }

    private void PlayDialTone(int digit)
    {
        EventInstance tone = CreateInstance(telephoneSound, dialLabel);
        if (!tone.isValid()) return;

        if (!string.IsNullOrEmpty(dialNumberParameter))
            tone.setParameterByName(dialNumberParameter, digit);

        tone.start();
        tone.release();
    }

    private void HandleCall(string number)
    {
        isDialing = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayAnimation(pickUpState);

        bool correct = currentBehaviour != null
                       && !string.IsNullOrEmpty(currentBehaviour.correctNumber)
                       && number == currentBehaviour.correctNumber;

        if (debugLogs)
            Debug.Log($"RotaryPhone: dialled '{number}', {(correct ? "correct" : "wrong")}.", this);

        if (!correct)
        {
            SetChloe(false);
            SoundPlayer.PlayOneShot(telephoneSound, transform.position, new SoundLabel(phoneActionParameter, pickUpLabel));
            StartStatic();
            return;
        }

        correctCallActive = true;
        PlayVoice(currentBehaviour.correctVoiceLoop);
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

        PlayVoice(currentBehaviour.voiceLoop);
    }

    private void PlayVoice(int loopValue)
    {
        StopInstance(ref voiceInstance);
        StopInstance(ref staticInstance);

        SetChloe(true);
        voiceInstance = CreateInstance(telephoneSound, pickUpLabel);

        if (voiceInstance.isValid())
        {
            if (!string.IsNullOrEmpty(loopParameter))
            {
                FMOD.RESULT result = voiceInstance.setParameterByName(loopParameter, loopValue);
                if (result != FMOD.RESULT.OK)
                    Debug.LogWarning($"RotaryPhone: couldn't set {loopParameter} = {loopValue} ({result}).", this);
            }

            voiceInstance.start();
            voiceStartTime = Time.time;
            lastVoiceState = PLAYBACK_STATE.STARTING;
            return;
        }

        SetChloe(false);
        StartStatic();
    }

    private bool VoiceFinished()
    {
        voiceInstance.getPlaybackState(out PLAYBACK_STATE state);

        if (debugLogs && state != lastVoiceState)
        {
            voiceInstance.getTimelinePosition(out int position);
            Debug.Log($"RotaryPhone: voice is {state} at {position / 1000f:F1}s ({Time.time - voiceStartTime:F1}s since answering).", this);
        }

        lastVoiceState = state;

        if (state == PLAYBACK_STATE.STOPPED)
            return true;

        return currentBehaviour != null
               && currentBehaviour.voiceLength > 0f
               && Time.time - voiceStartTime >= currentBehaviour.voiceLength;
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