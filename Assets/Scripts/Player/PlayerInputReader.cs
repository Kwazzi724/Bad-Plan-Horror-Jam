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
    [SerializeField] private InputAction torchAction;
    [SerializeField] private InputAction releaseAction;

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction.ReadValue<Vector2>();
    public bool LookFromGamepad => lookAction.activeControl != null && lookAction.activeControl.device is Gamepad;

    public event Action Interacted;
    public event Action TorchToggled;
    public event Action Released;

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        interactAction.performed += HandleInteractPerformed;
        interactAction.Enable();
        pauseAction.performed += PauseActionPerformed;
        pauseAction.Enable();
        torchAction.performed += HandleTorchPerformed;
        torchAction.Enable();
        releaseAction.performed += HandleReleasePerformed;
        releaseAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        interactAction.performed -= HandleInteractPerformed;
        interactAction.Disable();
        pauseAction.performed -= PauseActionPerformed;
        pauseAction.Disable();
        torchAction.performed -= HandleTorchPerformed;
        torchAction.Disable();
        releaseAction.performed -= HandleReleasePerformed;
        releaseAction.Disable();
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context)
    {
        Interacted?.Invoke();
    }

    private void HandleTorchPerformed(InputAction.CallbackContext context)
    {
        TorchToggled?.Invoke();
    }

    private void HandleReleasePerformed(InputAction.CallbackContext context)
    {
        Released?.Invoke();
    }

    private void PauseActionPerformed(InputAction.CallbackContext obj)
    {
        if(PauseManager.Instance.IsPaused)
            PauseManager.Instance.Resume();
        else
            PauseManager.Instance.Pause();
    }
}