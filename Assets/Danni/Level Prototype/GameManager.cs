using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Camera overviewCamera;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 fallbackSpawnPosition = new Vector3(0f, 1f, 0f);
    [SerializeField] private float spawnDelay = 3f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 1f;

    // NEW: text (or any UI object) that appears when the player goes insane
    [Header("Sanity")]
    [SerializeField] private GameObject insaneText;

    private GameObject currentPlayer;

    private void Start()
    {
        // NEW: make sure the insane text starts hidden
        if (insaneText != null)
            insaneText.SetActive(false);

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeIn(fadeInDuration);

        if (stageManager == null)
            stageManager = StageManager.Instance;
        
        if (stageManager == null)
            return;
        
        StartCoroutine(SpawnPlayerAfterDelay());
    }

    private IEnumerator SpawnPlayerAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnPlayer();
    }
    
    public void Insane()
    {
        if (insaneText != null)
            insaneText.SetActive(true);
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
            return;
        if (currentPlayer != null)
            Destroy(currentPlayer);

        Vector3 position = spawnPoint != null ? spawnPoint.position : fallbackSpawnPosition;
        Quaternion rotation = spawnPoint != null
            ? Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f)
            : Quaternion.identity;
        currentPlayer = Instantiate(playerPrefab, position, rotation);
        if (overviewCamera != null)
            overviewCamera.gameObject.SetActive(false);
        
    }
}