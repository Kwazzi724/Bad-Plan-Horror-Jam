using FMOD.Studio;
using FMODUnity;
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

    [Header("Credits Music")]
    [SerializeField] private EventReference creditsTrack;
    [SerializeField] private float creditsFadeInDuration = 2f;
    [SerializeField] private float creditsFadeOutDuration = 1f;

    private bool isStarting;
    private EventInstance creditsInstance;
    private float creditsVolume;

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

    private void Update()
    {
        if (!creditsInstance.isValid()) return;

        if (creditsVolume < 1f)
        {
            creditsVolume = Mathf.MoveTowards(creditsVolume, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, creditsFadeInDuration));
            creditsInstance.setVolume(creditsVolume);
        }

        creditsInstance.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
            creditsInstance.start();
    }

    private void OnDestroy()
    {
        StopCreditsTrack();
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
        StartCreditsTrack();
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        StopCreditsTrack();
    }

    private void StartCreditsTrack()
    {
        if (creditsTrack.IsNull || creditsInstance.isValid()) return;

        try
        {
            creditsInstance = RuntimeManager.CreateInstance(creditsTrack);
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"Menu: couldn't create {creditsTrack}.", this);
            return;
        }

        creditsVolume = creditsFadeInDuration > 0f ? 0f : 1f;
        creditsInstance.setVolume(creditsVolume);
        creditsInstance.start();
    }

    private void StopCreditsTrack()
    {
        if (!creditsInstance.isValid()) return;

        SoundFader.FadeOutAndRelease(creditsInstance, creditsFadeOutDuration);
        creditsInstance.clearHandle();
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