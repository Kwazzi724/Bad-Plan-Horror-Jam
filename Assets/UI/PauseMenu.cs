using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    private void Start()
    {
        PauseManager.Instance.OnPauseStarted += Show;
        PauseManager.Instance.OnPauseEnded += Hide;
        
        gameObject.SetActive(false);
    }

    private void Show()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
