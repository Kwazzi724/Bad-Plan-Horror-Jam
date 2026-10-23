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
    public EventReference voiceLine;
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
    [SerializeField] private string holdPrompt = "Right click to hang up";

    [Header("Sounds")]
    [SerializeField] private EventReference telephoneSound;
    [SerializeField] private string phoneActionParameter = "PhoneAction";
    [SerializeField] private string ringLabel = "Ring";
    [SerializeField] private string pickUpLabel = "PickUp";
    [SerializeField] private string hangUpLabel = "HungUp";
    [SerializeField] private EventReference staticSound;

    [Header("Stages")]
    [SerializeField] private List<PhoneStageBehaviour> stageBehaviours = new List<PhoneStageBehaviour> { new PhoneStageBehaviour() };

    private Stage stage;
    private PhoneStageBehaviour currentBehaviour;
    private bool isActive;
    private bool isHeld;
    private bool hasAnswered;
    private Coroutine ringRoutine;
    private EventInstance ringInstance;
    private EventInstance voiceInstance;
    private EventInstance staticInstance;

    public bool CanInteract => isActive && !isHeld;
    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
    public string HoldPrompt => holdPrompt;
    public bool UsesCursor => false;

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
    }

    private void Update()
    {
        if (IsStopped(ringInstance))
            ringInstance.start();

        if (!isHeld) return;

        if (voiceInstance.isValid())
        {
            if (IsStopped(voiceInstance))
            {
                StopInstance(ref voiceInstance);
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
        hasAnswered = true;
        StopRinging();

        PlayAnimation(pickUpState);
        SoundPlayer.PlayOneShot(telephoneSound, transform.position, new SoundLabel(phoneActionParameter, pickUpLabel));
        StartListening();
    }

    public void EndInteract()
    {
        if (!isHeld) return;
        isHeld = false;

        StopInstance(ref voiceInstance);
        StopInstance(ref staticInstance);

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
        hasAnswered = false;
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

        if (!isHeld && !hasAnswered)
        {
            ringInstance = CreateInstance(telephoneSound, ringLabel);
            if (ringInstance.isValid())
                ringInstance.start();
        }
    }

    private void StopRinging()
    {
        if (ringRoutine != null)
        {
            StopCoroutine(ringRoutine);
            ringRoutine = null;
        }

        StopInstance(ref ringInstance);
    }

    private void StartListening()
    {
        if (currentBehaviour != null && !currentBehaviour.voiceLine.IsNull)
        {
            voiceInstance = CreateInstance(currentBehaviour.voiceLine, null);
            if (voiceInstance.isValid())
            {
                voiceInstance.start();
                return;
            }
        }

        StartStatic();
    }

    private void StartStatic()
    {
        staticInstance = CreateInstance(staticSound, null);
        if (staticInstance.isValid())
            staticInstance.start();
    }

    private EventInstance CreateInstance(EventReference sound, string label)
    {
        if (sound.IsNull) return default;

        EventInstance instance = RuntimeManager.CreateInstance(sound);
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