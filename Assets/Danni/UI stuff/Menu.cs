using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private string gameScene;
    [SerializeField] private Door menuDoor;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject quitButton;

    private bool isStarting;

    private void Start()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (quitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
            quitButton.SetActive(false);

        if (ScreenFadeManager.Instance != null)
        {
            ScreenFadeManager.Instance.FadeIn(fadeInDuration);
        }
    }

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;

        if (menuDoor == null)
        {
            FadeOutThenLoad();
            return;
        }

        menuDoor.MenuSwingOpen(FadeOutThenLoad);
    }

    public void OpenCredits()
    {
        Debug.Log($"Menu: OpenCredits pressed. Starting: {isStarting}, panel: {(creditsPanel != null ? creditsPanel.name : "NOT ASSIGNED")}.", this);

        if (isStarting || creditsPanel == null) return;

        creditsPanel.SetActive(true);
        creditsPanel.transform.SetAsLastSibling();
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void FadeOutThenLoad()
    {
        ScreenFadeManager fadeManager = ScreenFadeManager.Instance;

        if (fadeManager == null)
        {
            LoadGameScene();
            return;
        }

        fadeManager.OnFadeOutEnded += HandleFadeOutEnded;
        fadeManager.FadeOut(fadeOutDuration);
    }

    private void HandleFadeOutEnded()
    {
        ScreenFadeManager.Instance.OnFadeOutEnded -= HandleFadeOutEnded;
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameScene);
    }
}