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
    [SerializeField] private string pickUpState = "PickUp_Fixed";
    [SerializeField] private string holdingState = "HoldingPhone";
    [SerializeField] private string hangUpState = "HangUp";
    [SerializeField] private string dialOneState = "Dial1";
    [SerializeField] private string dialOtherState = "Dial2Through9";
    [SerializeField] private string clearState = "Dial2Through9";

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

    [Header("Voice End")]
    [SerializeField] private float voiceEndSilence = 0.4f;

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
    private float voiceSilentTime;
    private readonly List<EventInstance> dialTones = new List<EventInstance>();
    private bool isDialing;
    private bool correctCallActive;
    private bool dialSolved;
    private bool waitingForHoldPose;

    public bool CanInteract => isActive && !isHeld && !dialSolved;
    public Transform FocusPoint => isDialing && dialPad != null ? dialPad.transform : focusPoint != null ? focusPoint : transform;
    public string HoldPrompt => isDialing ? dialPrompt : holdPrompt;
    public bool UsesCursor => UsesDialPad;
    public bool CanRelease => true;

    private bool UsesDialPad => currentBehaviour != null && currentBehaviour.usesDialPad && dialPad != null;

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

        StopDialTones();
    }

    private void Update()
    {
        UpdateHoldPose();

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

        if (debugLogs)
            Debug.Log($"RotaryPhone: interacted, stage entry {(currentBehaviour != null ? currentBehaviour.stage.ToString() : "none")}, uses dial pad {(currentBehaviour != null && currentBehaviour.usesDialPad)}, dial pad {(dialPad != null ? dialPad.name : "not assigned")}.", this);

        if (currentBehaviour != null && currentBehaviour.usesDialPad && dialPad == null)
            Debug.LogWarning($"RotaryPhone '{name}': this stage uses the dial pad but no Dial Pad is assigned.", this);

        if (UsesDialPad)
        {
            OpenDialPad();
            return;
        }

        isHeld = true;
        PlayPickUp();

        if (isRinging)
        {
            AnswerRing();
            return;
        }

        if (HasVoice && (hasRung || !currentBehaviour.rings))
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
            StopDialTones();
            return;
        }

        isHeld = false;
        waitingForHoldPose = false;
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
        if (viewer == null)
        {
            Debug.LogWarning("RotaryPhone: no Main Camera found, can't open the dial pad.", this);
            return;
        }

        isHeld = true;
        isDialing = true;
        dialPad.Open(viewer, HandleDigit, HandleClear, HandleCall);
    }

    private void HandleDigit(int digit)
    {
        PlayAnimation(digit == 1 ? dialOneState : dialOtherState);
        PlayDialTone(digit);
    }

    private void HandleClear()
    {
        PlayAnimation(clearState);
    }

    private void PlayDialTone(int digit)
    {
        if (string.IsNullOrEmpty(dialLabel)) return;

        EventInstance tone = CreateInstance(telephoneSound, dialLabel);
        if (!tone.isValid()) return;

        if (!string.IsNullOrEmpty(dialNumberParameter))
            tone.setParameterByName(dialNumberParameter, digit);

        tone.start();
        dialTones.Add(tone);
    }

    private void StopDialTones()
    {
        for (int i = 0; i < dialTones.Count; i++)
        {
            EventInstance tone = dialTones[i];
            StopInstance(ref tone);
        }

        dialTones.Clear();
    }

    private void HandleCall(string number)
    {
        StopDialTones();
        isDialing = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayPickUp();

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

                voiceInstance.getParameterByName(loopParameter, out float readBack);
                int gameStage = GameManager.Instance != null ? GameManager.Instance.CurrentStageNumber : -1;
                Debug.Log($"RotaryPhone '{name}' in '{(stage != null ? stage.name : "no stage")}': game stage {gameStage}, using entry for stage {(currentBehaviour != null ? currentBehaviour.stage : -1)}, sending {loopParameter} = {loopValue}, FMOD reads back {readBack}.", this);
            }

            voiceInstance.start();
            voiceStartTime = Time.time;
            voiceSilentTime = 0f;
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

        if (state != PLAYBACK_STATE.SUSTAINING)
        {
            voiceSilentTime = 0f;
            return false;
        }

        if (IsMakingSound(voiceInstance))
        {
            voiceSilentTime = 0f;
            return false;
        }

        voiceSilentTime += Time.deltaTime;

        if (voiceSilentTime < voiceEndSilence)
            return false;

        if (debugLogs)
            Debug.Log($"RotaryPhone: voice went silent {Time.time - voiceStartTime:F1}s after answering, treating it as finished.", this);

        return true;
    }

    private static bool IsMakingSound(EventInstance instance)
    {
        if (instance.getChannelGroup(out FMOD.ChannelGroup group) != FMOD.RESULT.OK)
            return true;

        return IsGroupPlaying(group, 0);
    }

    private static bool IsGroupPlaying(FMOD.ChannelGroup group, int depth)
    {
        if (depth > 8) return false;

        if (group.getNumChannels(out int channelCount) == FMOD.RESULT.OK)
        {
            for (int i = 0; i < channelCount; i++)
            {
                if (group.getChannel(i, out FMOD.Channel channel) != FMOD.RESULT.OK) continue;

                channel.isPlaying(out bool playing);
                if (playing) return true;
            }
        }

        if (group.getNumGroups(out int groupCount) == FMOD.RESULT.OK)
        {
            for (int i = 0; i < groupCount; i++)
            {
                if (group.getGroup(i, out FMOD.ChannelGroup child) != FMOD.RESULT.OK) continue;
                if (IsGroupPlaying(child, depth + 1)) return true;
            }
        }

        return false;
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

    private void PlayPickUp()
    {
        waitingForHoldPose = PlayAnimation(pickUpState);

        if (!waitingForHoldPose)
            PlayAnimation(holdingState);
    }

    private void UpdateHoldPose()
    {
        if (!waitingForHoldPose || animator == null) return;

        if (!isHeld)
        {
            waitingForHoldPose = false;
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash != Animator.StringToHash(pickUpState)) return;
        if (info.normalizedTime < 1f) return;

        waitingForHoldPose = false;
        PlayAnimation(holdingState);
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