using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Serializable]
    public class StageHint
    {
        public int stage = 1;
        [TextArea(2, 6)] public string hint;
    }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [FormerlySerializedAs("tutorialCloseButton")]
    [FormerlySerializedAs("resumeButton")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button resumeGameButton;
    [SerializeField] private bool showOnStart = true;

    [Header("Main Menu")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private float menuFadeOutDuration = 1f;

    [Header("Hints")]
    [SerializeField] private Button hintButton;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private string noHintText = "No hint for this room.";
    [SerializeField] private List<StageHint> hints = new List<StageHint>();

    private bool isShowing;
    private bool isLeaving;
    private bool hasClosedOnce;
    private CursorLockMode previousLockMode = CursorLockMode.Locked;
    private bool previousCursorVisible;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(Resume);

        if (resumeGameButton != null)
            resumeGameButton.onClick.AddListener(Resume);

        if (hintButton != null)
            hintButton.onClick.AddListener(ToggleHint);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        Hide();
    }

    private void Start()
    {
        if (!showOnStart)
        {
            hasClosedOnce = true;
            return;
        }

        if (PauseManager.Instance != null && !PauseManager.Instance.IsPaused)
            PauseManager.Instance.Pause();

        Show();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(Resume);

        if (resumeGameButton != null)
            resumeGameButton.onClick.RemoveListener(Resume);

        if (hintButton != null)
            hintButton.onClick.RemoveListener(ToggleHint);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.OnFadeOutEnded -= LoadMainMenu;
    }

    private void Update()
    {
        if (isLeaving) return;

        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;

        if (paused && !isShowing)
            Show();
        else if (!paused && isShowing)
            Hide();
    }

    private void LateUpdate()
    {
        if (!isShowing) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        hasClosedOnce = true;

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            PauseManager.Instance.Resume();

        Hide();
    }

    public void ReturnToMainMenu()
    {
        if (isLeaving) return;
        isLeaving = true;

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            PauseManager.Instance.Resume();

        if (panel != null)
            panel.SetActive(false);

        GameObject player = GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null;
        if (player != null)
        {
            PlayerMovement movement = player.GetComponentInChildren<PlayerMovement>();
            if (movement != null)
                movement.Frozen = true;

            PlayerLook look = player.GetComponentInChildren<PlayerLook>();
            if (look != null)
                look.Frozen = true;
        }

        ScreenFadeManager fadeManager = ScreenFadeManager.Instance;

        if (fadeManager == null || menuFadeOutDuration <= 0f)
        {
            LoadMainMenu();
            return;
        }

        fadeManager.OnFadeOutEnded += LoadMainMenu;
        fadeManager.FadeOut(menuFadeOutDuration);
    }

    private void LoadMainMenu()
    {
        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.OnFadeOutEnded -= LoadMainMenu;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void ToggleHint()
    {
        if (hintText == null) return;

        bool show = !hintText.gameObject.activeSelf;

        if (show)
            hintText.text = GetHint();

        hintText.gameObject.SetActive(show);
    }

    private string GetHint()
    {
        int stageNumber = GameManager.Instance != null ? GameManager.Instance.CurrentStageNumber : 0;
        StageHint match = hints.Find(entry => entry.stage == stageNumber);
        return match != null && !string.IsNullOrEmpty(match.hint) ? match.hint : noHintText;
    }

    private void Show()
    {
        if (!isShowing)
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
        }

        isShowing = true;

        if (panel != null)
            panel.SetActive(true);

        if (hintButton != null)
            hintButton.gameObject.SetActive(hasClosedOnce);

        if (startButton != null)
            startButton.gameObject.SetActive(!hasClosedOnce);

        if (resumeGameButton != null)
            resumeGameButton.gameObject.SetActive(hasClosedOnce);

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        Button selected = hasClosedOnce ? resumeGameButton : startButton;

        if (selected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }

    private void Hide()
    {
        bool wasShowing = isShowing;
        isShowing = false;

        if (panel != null)
            panel.SetActive(false);

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (!wasShowing) return;

        hasClosedOnce = true;

        bool playerExists = GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null;
        Cursor.lockState = playerExists ? CursorLockMode.Locked : previousLockMode;
        Cursor.visible = playerExists ? false : previousCursorVisible;
    }
}