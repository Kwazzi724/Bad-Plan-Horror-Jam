using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Serializable]
    public class EnemySpawn
    {
        public int stage = 3;
        public GameObject enemyPrefab;
        public float spawnDelay = 10f;
        public float distanceBehindPlayer = 4f;
        public bool despawnOnNextStage = true;
    }

    [SerializeField] private List<EnemySpawn> spawns = new List<EnemySpawn> { new EnemySpawn() };

    private readonly List<GameObject> stageEnemies = new List<GameObject>();
    private readonly List<Coroutine> pendingSpawns = new List<Coroutine>();
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.StageEntered += HandleStageEntered;
        gameManager.StageReset += HandleStageEntered;

        if (gameManager.CurrentStageNumber > 0)
            HandleStageEntered(gameManager.CurrentStage, gameManager.CurrentStageNumber);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.StageEntered -= HandleStageEntered;
            gameManager.StageReset -= HandleStageEntered;
        }
    }

    private void HandleStageEntered(Stage stage, int stageNumber)
    {
        foreach (Coroutine pending in pendingSpawns)
        {
            if (pending != null)
                StopCoroutine(pending);
        }
        pendingSpawns.Clear();

        foreach (GameObject enemy in stageEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        stageEnemies.Clear();

        foreach (EnemySpawn spawn in spawns)
        {
            if (spawn.stage == stageNumber && spawn.enemyPrefab != null)
                pendingSpawns.Add(StartCoroutine(SpawnAfterDelay(spawn)));
        }
    }

    private IEnumerator SpawnAfterDelay(EnemySpawn spawn)
    {
        yield return new WaitForSeconds(spawn.spawnDelay);

        PlayerMovement player = null;
        while (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>();
            if (player == null)
                yield return null;
        }

        Transform playerTransform = player.transform;
        Vector3 behind = -playerTransform.forward;
        behind.y = 0f;
        behind = behind.sqrMagnitude > 0.0001f ? behind.normalized : Vector3.back;

        Vector3 position = playerTransform.position + behind * spawn.distanceBehindPlayer;
        Quaternion rotation = Quaternion.LookRotation(-behind);

        GameObject enemy = Instantiate(spawn.enemyPrefab, position, rotation);

        if (spawn.despawnOnNextStage)
            stageEnemies.Add(enemy);

        foreach (IEnemy enemyBehaviour in enemy.GetComponentsInChildren<IEnemy>())
            enemyBehaviour.Begin(playerTransform);
    }
}