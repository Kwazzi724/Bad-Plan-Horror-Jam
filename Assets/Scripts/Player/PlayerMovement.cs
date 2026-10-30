using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using UnityEngine.Animations;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Footsteps")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private float stepDistance = 1.8f;
    [SerializeField] private SoundLabel stepFloor = new SoundLabel("Floor", "Wood");

    public bool Frozen { get; set; }

    private CharacterController controller;
    private PlayerInputReader input;
    private float verticalVelocity;
    private float distanceSinceStep;
    private EventInstance stepInstance;
    private bool footstepsLoop;
    private bool checkedFootstepType;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
    }

    private void OnDisable()
    {
        StopStep();
    }

    private void Update()
    {
        Vector2 moveInput = Frozen ? Vector2.zero : Vector2.ClampMagnitude(input.Move, 1f);
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
        UpdateFootsteps();
    }

    public void SetFloor(string floorLabel)
    {
        stepFloor.label = floorLabel;

        if (stepInstance.isValid() && !string.IsNullOrEmpty(stepFloor.parameter))
            stepInstance.setParameterByNameWithLabel(stepFloor.parameter, stepFloor.label);
    }

    private void UpdateFootsteps()
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        bool moving = controller.isGrounded && horizontalVelocity.sqrMagnitude >= 0.01f;

        if (!moving)
        {
            distanceSinceStep = stepDistance * 0.5f;
            StopStep();
            animator.SetBool("isMoving", false);
            return;
        }

        if (stepInstance.isValid())
            stepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));

        if (IsFootstepLoop())
        {
            if (!stepInstance.isValid())
                StartStep();
            animator.SetBool("isMoving", true);
            return;
        }

        distanceSinceStep += horizontalVelocity.magnitude * Time.deltaTime;

        if (distanceSinceStep >= stepDistance)
        {
            distanceSinceStep = 0f;
            StartStep();
        }
    }

    private bool IsFootstepLoop()
    {
        if (checkedFootstepType || footstepEvent.IsNull) return footstepsLoop;

        checkedFootstepType = true;

        try
        {
            EventDescription description = RuntimeManager.GetEventDescription(footstepEvent);
            description.isOneshot(out bool oneShot);
            footstepsLoop = !oneShot;
        }
        catch (EventNotFoundException)
        {
            footstepsLoop = false;
        }

        return footstepsLoop;
    }

    private void StartStep()
    {
        StopStep();

        if (footstepEvent.IsNull) return;

        try
        {
            stepInstance = RuntimeManager.CreateInstance(footstepEvent);
        }
        catch (EventNotFoundException)
        {
            return;
        }

        if (!string.IsNullOrEmpty(stepFloor.parameter))
            stepInstance.setParameterByNameWithLabel(stepFloor.parameter, stepFloor.label);

        stepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        stepInstance.start();
    }

    private void StopStep()
    {
        if (!stepInstance.isValid()) return;

        stepInstance.stop(STOP_MODE.ALLOWFADEOUT);
        stepInstance.release();
        stepInstance.clearHandle();
    }
}