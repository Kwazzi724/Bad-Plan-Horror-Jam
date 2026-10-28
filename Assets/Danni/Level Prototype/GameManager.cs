using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    [Header("Sanity")]
    [SerializeField] private GameObject insaneText;

    [Header("Death")]
    [SerializeField] private DeathSequence deathSequence;

    public bool isDeathActivated;

    [Header("Interaction")]
    [SerializeField] private TMP_Text holdPromptText;

    [Header("Messages")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 1.5f;

    public int CurrentStageNumber { get; private set; }
    public int NextStageNumber => CurrentStageNumber + 1;
    public Stage CurrentStage => stageManager != null ? stageManager.CurrentStage : null;

    public event Action<Stage, int> StageEntered;
    public event Action<Stage, int> StageReset;

    public GameObject CurrentPlayer => currentPlayer;
    public bool IsDying => deathSequence != null && deathSequence.IsDying;

    private GameObject currentPlayer;
    private Coroutine messageRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (stageManager != null)
            stageManager.StageEntered -= HandleStageEntered;

        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (insaneText != null)
            insaneText.SetActive(false);

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeIn(fadeInDuration);

        if (stageManager == null)
            stageManager = StageManager.Instance;
        
        if (stageManager == null)
            return;

        HideHoldPrompt();
        if (messageText != null)
            messageText.gameObject.SetActive(false);
        stageManager.StageEntered += HandleStageEntered;
        CurrentStageNumber = 1;
        StageEntered?.Invoke(stageManager.CurrentStage, CurrentStageNumber);

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

    public void Sane()
    {
        if (insaneText != null)
            insaneText.SetActive(false);
    }

    public void GameOver()
    {
        if (!isDeathActivated) return;
        if (deathSequence == null)
            deathSequence = FindAnyObjectByType<DeathSequence>();

        if (deathSequence == null)
        {
            Debug.LogWarning("GameManager: GameOver called but there's no DeathSequence in the scene.", this);
            return;
        }

        deathSequence.Begin();
    }

    public void ResetCurrentStage()
    {
        if (stageManager == null) return;

        Sane();
        HideHoldPrompt();

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        Stage freshStage = stageManager.ResetStages();
        if (freshStage == null) return;

        PlayerSpawnPoint stageSpawn = freshStage.GetComponentInChildren<PlayerSpawnPoint>(true);

        if (stageSpawn == null)
            Debug.LogWarning($"GameManager: '{freshStage.name}' has no PlayerSpawnPoint, using the default spawn.", freshStage);

        SpawnPlayerAt(stageSpawn != null ? stageSpawn.transform : spawnPoint);
        StageReset?.Invoke(freshStage, CurrentStageNumber);
    }

    private void HandleStageEntered(Stage stage)
    {
        CurrentStageNumber++;
        StageEntered?.Invoke(stage, CurrentStageNumber);
    }

    public void ShowHoldPrompt(string message)
    {
        if (holdPromptText == null) return;

        if (string.IsNullOrEmpty(message))
        {
            HideHoldPrompt();
            return;
        }

        holdPromptText.text = message;
        holdPromptText.gameObject.SetActive(true);
    }

    public void ShowMessage(string message)
    {
        if (messageText == null || string.IsNullOrEmpty(message)) return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    public void HideHoldPrompt()
    {
        if (holdPromptText != null)
            holdPromptText.gameObject.SetActive(false);
    }

    public void SpawnPlayer()
    {
        SpawnPlayerAt(spawnPoint);
    }

    public void SpawnPlayerAt(Transform point)
    {
        if (playerPrefab == null)
            return;

        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false);
            Destroy(currentPlayer);
        }

        Vector3 position = point != null ? point.position : fallbackSpawnPosition;
        Quaternion rotation = point != null
            ? Quaternion.Euler(0f, point.eulerAngles.y, 0f)
            : Quaternion.identity;
        currentPlayer = Instantiate(playerPrefab, position, rotation);
        if (overviewCamera != null)
            overviewCamera.gameObject.SetActive(false);
        
    }
}