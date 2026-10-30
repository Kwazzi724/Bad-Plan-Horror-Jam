using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSequence : MonoBehaviour
{
    [Header("When")]
    [SerializeField] private int endingStage = 7;
    [SerializeField] private float delay = 5f;

    [Header("Fade")]
    [SerializeField] private float fadeToBlackDuration = 3f;
    [SerializeField] private float panelDelay = 1f;
    [SerializeField] private float panelFadeDuration = 2f;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button menuButton;
    [SerializeField] private string mainMenuScene = "Menu";

    private bool started;
    private bool showingPanel;

    private void Awake()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
        }

        if (menuButton != null)
            menuButton.onClick.AddListener(ReturnToMenu);
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
        if (!showingPanel) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeOut(fadeToBlackDuration);

        yield return new WaitForSeconds(fadeToBlackDuration);

        LockPlayer();

        yield return new WaitForSecondsRealtime(panelDelay);

        if (panel == null) yield break;

        panel.gameObject.SetActive(true);
        showingPanel = true;

        float elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Clamp01(elapsed / panelFadeDuration);
            yield return null;
        }

        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
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
