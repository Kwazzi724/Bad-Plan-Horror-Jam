using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private string gameScene;
    [SerializeField] private Door menuDoor;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    private bool isStarting;

    private void Start()
    {
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