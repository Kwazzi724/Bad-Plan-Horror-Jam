using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private string gameScene;
    [SerializeField] private Door menuDoor;

    private bool isStarting;

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;

        if (menuDoor == null)
        {
            LoadGameScene();
            return;
        }

        menuDoor.MenuSwingOpen(LoadGameScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameScene);
    }
}