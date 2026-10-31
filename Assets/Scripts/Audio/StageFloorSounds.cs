using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageFloorSounds : MonoBehaviour
{
    [Serializable]
    public class StageFloor
    {
        public int stage = 1;
        public string floor = "Wood";
    }

    public static StageFloorSounds Instance { get; private set; }

    [SerializeField] private string defaultFloor = "Wood";
    [SerializeField] private List<StageFloor> stageFloors = new List<StageFloor> { new StageFloor { stage = 4, floor = "Tiles" } };

    [Header("Blood")]
    [SerializeField] private string bloodFloor = "Blood";
    [SerializeField] private float bloodDelay = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private GameManager gameManager;
    private string targetFloor;
    private GameObject appliedPlayer;
    private PlayerMovement movement;
    private string appliedFloor;
    private Coroutine bloodRoutine;

    private bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused;
    private int CurrentStageNumber => gameManager != null ? gameManager.CurrentStageNumber : 0;

    private void Awake()
    {
        Instance = this;
        targetFloor = defaultFloor;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StageEntered += HandleStageEntered;
        gameManager.StageReset += HandleStageReset;
        ApplyStage(gameManager.CurrentStageNumber);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.StageEntered -= HandleStageEntered;
            gameManager.StageReset -= HandleStageReset;
        }

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        GameObject player = gameManager != null ? gameManager.CurrentPlayer : null;
        if (player == null) return;

        if (player != appliedPlayer)
        {
            appliedPlayer = player;
            movement = player.GetComponentInChildren<PlayerMovement>();
            appliedFloor = null;
        }

        if (movement == null || appliedFloor == targetFloor) return;

        movement.SetFloor(targetFloor);
        appliedFloor = targetFloor;

        if (debugLogs)
            Debug.Log($"StageFloorSounds: footsteps now on '{targetFloor}' in stage {CurrentStageNumber}.", this);
    }

    public void StartBloodOverflow()
    {
        StopBlood();
        bloodRoutine = StartCoroutine(BloodAfterDelay(CurrentStageNumber));
    }

    private void HandleStageEntered(Stage stage, int stageNumber)
    {
        ApplyStage(stageNumber);
    }

    private void HandleStageReset(Stage stage, int stageNumber)
    {
        ApplyStage(stageNumber);
    }

    private void ApplyStage(int stageNumber)
    {
        StopBlood();
        targetFloor = FloorFor(stageNumber);
    }

    private string FloorFor(int stageNumber)
    {
        StageFloor match = stageFloors.Find(entry => entry.stage == stageNumber);
        return match != null && !string.IsNullOrEmpty(match.floor) ? match.floor : defaultFloor;
    }

    private IEnumerator BloodAfterDelay(int stageNumber)
    {
        float elapsed = 0f;

        while (elapsed < bloodDelay)
        {
            if (!IsPaused)
                elapsed += Time.deltaTime;

            yield return null;
        }

        bloodRoutine = null;

        if (CurrentStageNumber == stageNumber)
            targetFloor = bloodFloor;
    }

    private void StopBlood()
    {
        if (bloodRoutine == null) return;

        StopCoroutine(bloodRoutine);
        bloodRoutine = null;
    }
}
