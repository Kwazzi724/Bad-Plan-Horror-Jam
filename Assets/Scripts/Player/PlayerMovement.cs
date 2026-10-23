using FMODUnity;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;
    
    [Header("Footsteps")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private float stepDistance = 1.8f;
    [SerializeField] private SoundLabel stepPace = new SoundLabel("WalkRun", "Walk");
    [SerializeField] private SoundLabel stepFloor = new SoundLabel("Floor", "Wood");

    public bool Frozen { get; set; }

    private CharacterController controller;
    private PlayerInputReader input;
    private float verticalVelocity;
    private float distanceSinceStep; // how far walked since the last step sound

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
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

    // plays a step every stepDistance metres actually travelled on the ground
    private void UpdateFootsteps()
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        if (!controller.isGrounded || horizontalVelocity.sqrMagnitude < 0.01f)
        {
            distanceSinceStep = stepDistance * 0.5f;
            return;
        }

        distanceSinceStep += horizontalVelocity.magnitude * Time.deltaTime;

        if (distanceSinceStep >= stepDistance)
        {
            distanceSinceStep = 0f;

            SoundPlayer.PlayOneShot(footstepEvent, transform.position, stepPace, stepFloor);
        }
    }
}