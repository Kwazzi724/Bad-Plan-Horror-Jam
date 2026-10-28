using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Interaction")]
    [SerializeField] private TMP_Text holdPromptText;

    [FormerlySerializedAs("messageText")]
    [Header("Messages")]
    [SerializeField] private TMP_Text lockedText;
    [SerializeField] private float messageDuration = 1.5f;

    public int CurrentStageNumber { get; private set; }
    public int NextStageNumber => CurrentStageNumber + 1;
    public Stage CurrentStage => stageManager != null ? stageManager.CurrentStage : null;

    public event Action<Stage, int> StageEntered;

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
        if (lockedText != null)
            lockedText.gameObject.SetActive(false);
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
        if (lockedText == null || string.IsNullOrEmpty(message)) return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        lockedText.text = message;
        lockedText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        lockedText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    public void HideHoldPrompt()
    {
        if (holdPromptText != null)
            holdPromptText.gameObject.SetActive(false);
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