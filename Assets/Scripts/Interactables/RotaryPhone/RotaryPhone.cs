using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;


[Serializable]
public class PhoneStageBehaviour
{
    public int stage = 2;
    public bool rings = true;
    public float ringDelay = 5f;
    //public EventReference voiceLine;
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
    [SerializeField] private StudioEventEmitter emitter;
    [Header("Parameters")]
    [SerializeField] private string phoneActionParameter = "PhoneAction";
    [SerializeField] private string loopParameter = "Loop";
    
    [SerializeField] private string ringLabel = "Ring";
    [SerializeField] private string pickUpLabel = "PickUp";
    [SerializeField] private string hangUpLabel = "PutDown";
    [SerializeField] private string staticSoundLabel = "HangUp";
    [SerializeField] private int loopValue;

    [Header("Global Parameter")]
    [SerializeField] private string chloeParameter = "Chloe";
    [SerializeField] private int chloeValue = 0;
    
    
   
    //[SerializeField] private EventReference staticSound;

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

        EventInstance instance = emitter.EventInstance;

        instance.getDescription(out EventDescription description);
        description.getPath(out string eventPath);

        instance.getParameterByName(
            phoneActionParameter,
            out float actionValue
        );

        instance.getParameterByName(
            loopParameter,
            out float currentLoopValue
        );
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered -= HandleStageEntered;
    }

    private void Update()
    {
       
    }

private void SetPhoneSound(string action, int loop)
{
    if (emitter == null || !emitter.EventInstance.isValid())
        return;

    EventInstance instance = emitter.EventInstance;

    instance.getParameterByName(loopParameter, out float before);

    FMOD.RESULT result = instance.setParameterByName(
        loopParameter,
        loop,
        true
    );

    instance.getParameterByName(
        loopParameter,
        out float after,
        out float finalValue
    );

    instance.setParameterByNameWithLabel(
        phoneActionParameter,
        action
    );
}



        private void StartRinging()
    {
        if(!isActive || isHeld|| emitter == null)
            return;

            hasRung = true;
            isRinging = true;

            if (!emitter.IsPlaying())
                emitter.Play();
            
            SetPhoneSound(ringLabel, loopValue);

    }

     private IEnumerator RingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ringRoutine = null;
        StartRinging();
    }

    public void OnInteract(GameObject interactor)
    {
        if (!CanInteract) return;

        isHeld = true;

        PlayAnimation(pickUpState);

        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(chloeParameter, chloeValue);

        if (emitter == null || !emitter.IsPlaying())
            return;

        if (!emitter.IsPlaying())
            emitter.Play();

        SetPhoneSound(pickUpLabel, loopValue);

        EventInstance instance = emitter.EventInstance;

        instance.getParameterByName(
            loopParameter,
            out float actualLoop
        );

        instance.getParameterByName(
            phoneActionParameter,
            out float actualAction
        );

        emitter.EventInstance.getParameterByName(phoneActionParameter, out float actonValue);
        emitter.EventInstance.getParameterByName(loopParameter, out float loopValueDebug);

        Debug.Log($"Action Value: {actonValue}, Loop Value: {loopValueDebug}");
   
    }

    public void EndInteract()
    {

        isHeld = false;
        SetPhoneSound(hangUpLabel, loopValue);
        if (!PlayAnimation(hangUpState))
            PlayAnimation(idleState);
        
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
        if(emitter != null && emitter.IsPlaying())
            return;

        if(ringRoutine != null)
            return;

        isActive = true;
        hasRung = false;

        currentBehaviour = stageBehaviours.Find(behaviour => behaviour.stage == stageNumber);

        if (currentBehaviour != null && currentBehaviour.rings)
            ringRoutine = StartCoroutine(RingAfterDelay(currentBehaviour.ringDelay));
    }

    private void Deactivate()
    {
        isActive = false;
        isHeld = false;
        if (emitter != null && emitter.IsPlaying())
            emitter.Stop();        
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