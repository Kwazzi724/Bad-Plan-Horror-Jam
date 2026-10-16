using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private Camera overviewCamera;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 fallbackSpawnPosition = new Vector3(0f, 1f, 0f);
    [SerializeField] private float spawnDelay = 3f;

    private GameObject currentPlayer;

    private void Start()
    {
        if (mapGenerator == null) return;
        if (!mapGenerator.Generate()) return;
        StartCoroutine(SpawnPlayerAfterDelay());
    }

    private IEnumerator SpawnPlayerAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnPlayer();
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
        Debug.Log("player spawned at {position}.", this);

        if (overviewCamera != null)
        {
            overviewCamera.gameObject.SetActive(false);
        }
    }
}