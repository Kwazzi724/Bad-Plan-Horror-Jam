using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction lookAction;
    [SerializeField] private InputAction interactAction;
    [SerializeField] private InputAction pauseAction;

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction.ReadValue<Vector2>();

    public event Action Interacted;

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        interactAction.performed += HandleInteractPerformed;
        interactAction.Enable();
        pauseAction.performed += PauseActionPerformed;
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        interactAction.performed -= HandleInteractPerformed;
        interactAction.Disable();
        pauseAction.performed -= PauseActionPerformed;
        pauseAction.Disable();
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context)
    {
        Interacted?.Invoke();
    }

    private void PauseActionPerformed(InputAction.CallbackContext obj)
    {
        if(PauseManager.Instance.IsPaused)
            PauseManager.Instance.Resume();
        else
            PauseManager.Instance.Pause();
    }
}