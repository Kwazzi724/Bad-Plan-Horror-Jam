using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSequence : MonoBehaviour
{
    [Header("When")]
    [SerializeField] private int endingStage = 7;
    [SerializeField] private float delay = 5f;

    [Header("Fade To Black")]
    [SerializeField] private float fadeToBlackDuration = 3f;

    [Header("Ending Panel")]
    [SerializeField] private GameObject TorchPanel;
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private float panelDelay = 1f;
    [SerializeField] private float panelFadeInDuration = 2f;
    [SerializeField] private float panelHoldDuration = 5f;
    [SerializeField] private float panelFadeOutDuration = 2f;

    [Header("Rolling Credits")]
    [SerializeField] private CanvasGroup creditsPanel;
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private float creditsDelay = 1f;
    [SerializeField] private float creditsFadeInDuration = 1f;
    [SerializeField] private float creditsScrollSpeed = 60f;

    [Header("Menu Button")]
    [SerializeField] private Button menuButton;
    [SerializeField] private float menuButtonDelay = 5f;
    [SerializeField] private float menuButtonFadeDuration = 1f;
    [SerializeField] private string mainMenuScene = "Menu";

    private bool started;
    private bool menuButtonReady;
    private CanvasGroup menuButtonGroup;

    private void Awake()
    {
        HideGroup(panel);
        HideGroup(creditsPanel);

        if (menuButton != null)
        {
            menuButtonGroup = menuButton.GetComponent<CanvasGroup>();
            if (menuButtonGroup == null)
                menuButtonGroup = menuButton.gameObject.AddComponent<CanvasGroup>();

            menuButtonGroup.alpha = 0f;
            menuButtonGroup.interactable = false;
            menuButtonGroup.blocksRaycasts = false;
            menuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StageEntered += HandleStageEntered;

        if (gameManager.CurrentStageNumber == endingStage)
            Begin();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered -= HandleStageEntered;

        if (menuButton != null)
            menuButton.onClick.RemoveListener(ReturnToMenu);
    }

    private void LateUpdate()
    {
        if (!menuButtonReady) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
    }

    private void HandleStageEntered(Stage stage, int stageNumber)
    {
        if (stageNumber == endingStage)
            Begin();
    }

    private void Begin()
    {
        if (started) return;

        started = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        yield return new WaitForSeconds(delay);
        if(TorchPanel != null) TorchPanel.SetActive(false);

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeOut(fadeToBlackDuration);

        yield return new WaitForSeconds(fadeToBlackDuration);

        LockPlayer();

        if (panel != null)
        {
            yield return new WaitForSecondsRealtime(panelDelay);

            panel.gameObject.SetActive(true);
            yield return Fade(panel, 0f, 1f, panelFadeInDuration);
            yield return new WaitForSecondsRealtime(panelHoldDuration);
            yield return Fade(panel, 1f, 0f, panelFadeOutDuration);
            panel.gameObject.SetActive(false);
        }

        if (creditsPanel == null || creditsContent == null)
        {
            StartCoroutine(ShowMenuButton());
            yield break;
        }

        yield return new WaitForSecondsRealtime(creditsDelay);
        yield return RollCredits();
    }

    private IEnumerator RollCredits()
    {
        creditsPanel.gameObject.SetActive(true);
        creditsPanel.alpha = 0f;

        RectTransform area = (RectTransform)creditsPanel.transform;
        creditsContent.anchorMin = new Vector2(0.5f, 0f);
        creditsContent.anchorMax = new Vector2(0.5f, 0f);
        creditsContent.pivot = new Vector2(0.5f, 1f);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

        float startY = 0f;
        float endY = area.rect.height + creditsContent.rect.height;
        creditsContent.anchoredPosition = new Vector2(0f, startY);

        float duration = creditsScrollSpeed > 0f ? (endY - startY) / creditsScrollSpeed : 0f;
        float elapsed = 0f;
        bool buttonStarted = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            creditsContent.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, endY, Mathf.Clamp01(elapsed / duration)));
            creditsPanel.alpha = creditsFadeInDuration > 0f ? Mathf.Clamp01(elapsed / creditsFadeInDuration) : 1f;

            if (!buttonStarted && elapsed >= menuButtonDelay)
            {
                buttonStarted = true;
                StartCoroutine(ShowMenuButton());
            }

            yield return null;
        }

        creditsPanel.alpha = 1f;

        if (!buttonStarted)
            StartCoroutine(ShowMenuButton());
    }

    private IEnumerator ShowMenuButton()
    {
        if (menuButtonGroup == null) yield break;

        yield return Fade(menuButtonGroup, 0f, 1f, menuButtonFadeDuration);

        menuButtonGroup.interactable = true;
        menuButtonGroup.blocksRaycasts = true;
        menuButtonReady = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = to;
    }

    private static void HideGroup(CanvasGroup group)
    {
        if (group == null) return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }

    private void LockPlayer()
    {
        GameObject player = GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null;
        if (player == null) return;

        PlayerInteractor interactor = player.GetComponentInChildren<PlayerInteractor>();
        if (interactor != null)
            interactor.enabled = false;

        PlayerMovement movement = player.GetComponentInChildren<PlayerMovement>();
        if (movement != null)
            movement.Frozen = true;

        PlayerLook look = player.GetComponentInChildren<PlayerLook>();
        if (look != null)
            look.Frozen = true;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }
}