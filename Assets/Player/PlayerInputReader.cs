using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction lookAction;
    [SerializeField] private InputAction interactAction;

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction.ReadValue<Vector2>();

    public event Action Interacted;

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        interactAction.performed += HandleInteractPerformed;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        interactAction.performed -= HandleInteractPerformed;
        interactAction.Disable();
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context)
    {
        Interacted?.Invoke();
    }
}