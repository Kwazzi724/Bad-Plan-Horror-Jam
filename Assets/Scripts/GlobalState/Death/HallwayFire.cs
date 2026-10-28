using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class HallwayFire : MonoBehaviour
{
    [Header("Flame")]
    [SerializeField] private GameObject flamePrefab;
    [SerializeField] private string spawnRateProperty = "SpawnRate";
    [SerializeField] private string sizeProperty = "Size";
    [SerializeField] private Vector2 spawnRateRange = new Vector2(120f, 250f);
    [SerializeField] private Vector2 sizeRange = new Vector2(0.8f, 1.4f);
    [SerializeField] private float growDuration = 1.5f;
    [SerializeField] private float flickerSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.4f;
    [SerializeField] private float surfaceOffset = 0.1f;
    [SerializeField] private int maxFlames = 60;
    [SerializeField] private float flameSpacing = 1.2f;

    [Header("Spread")]
    [SerializeField] private float burnDuration = 10f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private int originCount = 3;
    [SerializeField] private float spreadExponent = 1f;
    [SerializeField, Range(0f, 0.5f)] private float timingJitter = 0.15f;
    [SerializeField] private float wallDelay = 0.6f;
    [SerializeField] private float ceilingDelay = 1.2f;
    [SerializeField] private float noiseScale = 0.5f;
    [SerializeField] private float noiseStrength = 1.5f;

    [Header("Surfaces")]
    [SerializeField] private LayerMask levelMask = ~0;
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField] private float probeHeight = 1.2f;
    [SerializeField] private float maxStepHeight = 0.4f;
    [SerializeField] private float ceilingCheckDistance = 6f;
    [SerializeField] private float fallbackWallHeight = 2.5f;
    [SerializeField] private int wallSamplesPerEdge = 2;
    [SerializeField] private int maxCells = 20000;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [Header("Sound")]
    [SerializeField] private EventReference fireLoop;
    [SerializeField, Range(0f, 1f)] private float startVolume = 0.3f;

    private static readonly Vector2Int[] Neighbours =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private class Cell
    {
        public Vector3 floor;
        public float ceiling;
        public float distance = float.PositiveInfinity;
    }

    private class Flame
    {
        public Vector3 position;
        public Vector3 normal;
        public float arrival;
        public VisualEffect effect;
        public float rate;
        public float size;
        public float seed;
    }

    private readonly List<Flame> flames = new List<Flame>();
    private EventInstance loopInstance;
    private int spawnRateId;
    private int sizeId;

    private void Awake()
    {
        spawnRateId = Shader.PropertyToID(spawnRateProperty);
        sizeId = Shader.PropertyToID(sizeProperty);
    }

    private void OnDestroy()
    {
        Stop();
    }

    public IEnumerator Burn(Stage stage)
    {
        Stop();

        Bounds bounds = GetStageBounds(stage);
        Vector3 start = FindStartPoint(stage, bounds);
        Dictionary<Vector2Int, Cell> cells = MapFloor(start, bounds);

        if (cells.Count == 0)
        {
            Debug.LogWarning($"HallwayFire: found no floor in '{stage.name}'. Check the Level Mask.", this);
            yield return new WaitForSeconds(burnDuration + holdDuration);
            yield break;
        }

        SpreadFire(cells);
        PlaceFlames(cells);

        if (debugLogs)
            Debug.Log($"HallwayFire: {cells.Count} floor cells in '{stage.name}', placing {flames.Count} flames.", this);

        if (flamePrefab == null)
            Debug.LogWarning("HallwayFire: no Flame Prefab assigned, nothing will show.", this);

        StartLoop();

        float elapsed = 0f;
        int ignited = 0;
        float finishTime = burnDuration + holdDuration;

        while (elapsed < finishTime)
        {
            elapsed += Time.deltaTime;

            while (ignited < flames.Count && flames[ignited].arrival <= elapsed)
            {
                Ignite(flames[ignited], stage.transform);
                ignited++;
            }

            UpdateFlames(elapsed, ignited);
            UpdateLoop(flames.Count > 0 ? (float)ignited / flames.Count : 1f);
            yield return null;
        }
    }

    public void Stop()
    {
        flames.Clear();
        StopLoop();
    }

    private void StopLoop()
    {
        if (!loopInstance.isValid()) return;

        loopInstance.stop(STOP_MODE.ALLOWFADEOUT);
        loopInstance.release();
        loopInstance.clearHandle();
    }

    private Bounds GetStageBounds(Stage stage)
    {
        Renderer[] renderers = stage.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(stage.transform.position, Vector3.one);
        bool hasBounds = false;

        foreach (Renderer stageRenderer in renderers)
        {
            if (stageRenderer is ParticleSystemRenderer) continue;

            if (!hasBounds)
            {
                bounds = stageRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(stageRenderer.bounds);
            }
        }

        bounds.Expand(0.5f);
        return bounds;
    }

    private Vector3 FindStartPoint(Stage stage, Bounds bounds)
    {
        GameManager gameManager = GameManager.Instance;
        GameObject player = gameManager != null ? gameManager.CurrentPlayer : null;

        if (player != null && bounds.Contains(player.transform.position))
            return player.transform.position;

        PlayerSpawnPoint spawn = stage.GetComponentInChildren<PlayerSpawnPoint>(true);
        if (spawn != null)
            return spawn.transform.position;

        return bounds.center;
    }

    private Dictionary<Vector2Int, Cell> MapFloor(Vector3 start, Bounds bounds)
    {
        Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
        Queue<Vector2Int> open = new Queue<Vector2Int>();

        float startHeight = start.y;
        if (Physics.Raycast(start + Vector3.up * 0.1f, Vector3.down, out RaycastHit groundHit, 5f, levelMask, QueryTriggerInteraction.Ignore))
            startHeight = groundHit.point.y;

        if (!FindStartCell(ToKey(start), startHeight, bounds, out Vector2Int startKey, out Cell startCell))
            return cells;

        cells[startKey] = startCell;
        open.Enqueue(startKey);

        while (open.Count > 0 && cells.Count < maxCells)
        {
            Vector2Int current = open.Dequeue();
            Cell currentCell = cells[current];

            foreach (Vector2Int step in Neighbours)
            {
                Vector2Int next = current + step;
                if (cells.ContainsKey(next)) continue;
                if (!SampleFloor(next, currentCell.floor.y, bounds, out Cell nextCell)) continue;
                if (Mathf.Abs(nextCell.floor.y - currentCell.floor.y) > maxStepHeight) continue;

                cells[next] = nextCell;
                open.Enqueue(next);
            }
        }

        return cells;
    }

    private bool FindStartCell(Vector2Int center, float height, Bounds bounds, out Vector2Int key, out Cell cell)
    {
        for (int radius = 0; radius <= 4; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != radius) continue;

                    key = center + new Vector2Int(x, z);
                    if (SampleFloor(key, height, bounds, out cell))
                        return true;
                }
            }
        }

        key = center;
        cell = null;
        return false;
    }

    private bool SampleFloor(Vector2Int key, float referenceHeight, Bounds bounds, out Cell cell)
    {
        cell = null;
        Vector3 center = FromKey(key, referenceHeight);

        if (center.x < bounds.min.x || center.x > bounds.max.x || center.z < bounds.min.z || center.z > bounds.max.z)
            return false;

        Vector3 origin = center + Vector3.up * probeHeight;
        float length = probeHeight + maxStepHeight + 0.5f;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, length, levelMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.normal.y < 0.7f)
            return false;

        Vector3 boxHalf = new Vector3(cellSize * 0.4f, 0.3f, cellSize * 0.4f);
        if (Physics.CheckBox(hit.point + Vector3.up * (0.05f + boxHalf.y), boxHalf, Quaternion.identity, levelMask, QueryTriggerInteraction.Ignore))
            return false;

        float ceiling = fallbackWallHeight;
        if (Physics.Raycast(hit.point + Vector3.up * 0.2f, Vector3.up, out RaycastHit ceilingHit, ceilingCheckDistance, levelMask, QueryTriggerInteraction.Ignore))
            ceiling = ceilingHit.point.y - hit.point.y;

        cell = new Cell { floor = hit.point, ceiling = ceiling };
        return true;
    }

    private void SpreadFire(Dictionary<Vector2Int, Cell> cells)
    {
        List<Vector2Int> keys = new List<Vector2Int>(cells.Keys);
        FireHeap open = new FireHeap();
        float seed = Random.Range(0f, 1000f);
        int origins = Mathf.Clamp(originCount, 1, keys.Count);

        for (int i = 0; i < origins; i++)
        {
            Vector2Int key = keys[Random.Range(0, keys.Count)];
            cells[key].distance = 0f;
            open.Push(key, 0f);
        }

        while (open.Count > 0)
        {
            open.Pop(out Vector2Int current, out float currentDistance);
            Cell currentCell = cells[current];
            if (currentDistance > currentCell.distance) continue;

            foreach (Vector2Int step in Neighbours)
            {
                Vector2Int next = current + step;
                if (!cells.TryGetValue(next, out Cell nextCell)) continue;

                Vector3 point = nextCell.floor;
                float noise = Mathf.PerlinNoise(seed + point.x * noiseScale, seed + point.z * noiseScale);
                float cost = cellSize * (1f + noise * noiseStrength);
                float newDistance = currentDistance + cost;

                if (newDistance < nextCell.distance)
                {
                    nextCell.distance = newDistance;
                    open.Push(next, newDistance);
                }
            }
        }
    }

    private void PlaceFlames(Dictionary<Vector2Int, Cell> cells)
    {
        float maxDistance = 0f;
        foreach (Cell cell in cells.Values)
        {
            if (!float.IsPositiveInfinity(cell.distance))
                maxDistance = Mathf.Max(maxDistance, cell.distance);
        }

        List<Flame> candidates = new List<Flame>();

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            Cell cell = pair.Value;
            if (float.IsPositiveInfinity(cell.distance)) continue;

            float normalised = maxDistance > 0f ? cell.distance / maxDistance : 0f;

            candidates.Add(new Flame { position = cell.floor, normal = Vector3.up, arrival = normalised });

            if (Physics.Raycast(cell.floor + Vector3.up * 0.2f, Vector3.up, out RaycastHit ceilingHit, ceilingCheckDistance, levelMask, QueryTriggerInteraction.Ignore)
                && ceilingHit.normal.y < -0.5f)
            {
                candidates.Add(new Flame { position = ceilingHit.point, normal = ceilingHit.normal, arrival = normalised + ceilingDelay / Mathf.Max(0.01f, burnDuration) });
            }

            foreach (Vector2Int step in Neighbours)
            {
                if (cells.ContainsKey(pair.Key + step)) continue;

                Vector3 direction = new Vector3(step.x, 0f, step.y);

                for (int s = 0; s < wallSamplesPerEdge; s++)
                {
                    float height = Random.Range(0.3f, Mathf.Max(0.4f, cell.ceiling - 0.3f));
                    Vector3 origin = cell.floor + Vector3.up * height;

                    if (!Physics.Raycast(origin, direction, out RaycastHit wallHit, cellSize * 1.5f, levelMask, QueryTriggerInteraction.Ignore))
                        continue;

                    if (Mathf.Abs(wallHit.normal.y) > 0.5f)
                        continue;

                    float heightDelay = wallDelay * (height / Mathf.Max(0.5f, cell.ceiling));
                    candidates.Add(new Flame { position = wallHit.point, normal = wallHit.normal, arrival = normalised + heightDelay / Mathf.Max(0.01f, burnDuration) });
                }
            }
        }

        Shuffle(candidates);

        Dictionary<Vector3Int, List<Vector3>> taken = new Dictionary<Vector3Int, List<Vector3>>();
        List<Flame> accepted = new List<Flame>();

        foreach (Flame candidate in candidates)
        {
            if (IsCrowded(candidate.position, taken)) continue;

            Vector3Int bucket = ToBucket(candidate.position);
            if (!taken.TryGetValue(bucket, out List<Vector3> list))
            {
                list = new List<Vector3>();
                taken[bucket] = list;
            }

            list.Add(candidate.position);
            accepted.Add(candidate);

            if (accepted.Count >= maxFlames) break;
        }

        float latest = 0f;
        foreach (Flame flame in accepted)
        {
            float jitter = 1f + Random.Range(-timingJitter, timingJitter);
            flame.arrival = Mathf.Pow(Mathf.Max(0f, flame.arrival * jitter), spreadExponent);
            latest = Mathf.Max(latest, flame.arrival);
        }

        foreach (Flame flame in accepted)
        {
            flame.arrival = latest > 0f ? flame.arrival / latest * burnDuration : 0f;
            flame.rate = Random.Range(spawnRateRange.x, spawnRateRange.y);
            flame.size = Random.Range(sizeRange.x, sizeRange.y);
            flame.seed = Random.Range(0f, 1000f);
        }

        accepted.Sort((a, b) => a.arrival.CompareTo(b.arrival));
        flames.AddRange(accepted);
    }

    private bool IsCrowded(Vector3 position, Dictionary<Vector3Int, List<Vector3>> taken)
    {
        Vector3Int bucket = ToBucket(position);
        float spacingSquared = flameSpacing * flameSpacing;

        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            if (!taken.TryGetValue(bucket + new Vector3Int(x, y, z), out List<Vector3> list)) continue;

            foreach (Vector3 other in list)
            {
                if ((other - position).sqrMagnitude < spacingSquared)
                    return true;
            }
        }

        return false;
    }

    private void Ignite(Flame flame, Transform parent)
    {
        if (flamePrefab == null) return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, flame.normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject instance = Instantiate(flamePrefab, flame.position + flame.normal * surfaceOffset, rotation, parent);

        flame.effect = instance.GetComponentInChildren<VisualEffect>();

        if (debugLogs && flame == flames[0])
        {
            if (flame.effect == null)
                Debug.LogWarning($"HallwayFire: '{flamePrefab.name}' has no Visual Effect component.", instance);
            else if (flame.effect.visualEffectAsset == null)
                Debug.LogWarning($"HallwayFire: '{flamePrefab.name}' has no Visual Effect asset assigned.", instance);
            else
                Debug.Log($"HallwayFire: first flame lit at {flame.position}. Has {spawnRateProperty}: {flame.effect.HasInt(spawnRateId)}, has {sizeProperty}: {flame.effect.HasFloat(sizeId)}.", instance);
        }

        SetFlame(flame, 0f, 0f);
    }

    private void UpdateFlames(float elapsed, int ignited)
    {
        for (int i = 0; i < ignited; i++)
        {
            Flame flame = flames[i];
            if (flame.effect == null) continue;

            float grow = growDuration > 0f ? Mathf.Clamp01((elapsed - flame.arrival) / growDuration) : 1f;
            grow = Mathf.SmoothStep(0f, 1f, grow);

            float flicker = 1f - flickerAmount + flickerAmount * 2f * Mathf.PerlinNoise(flame.seed, elapsed * flickerSpeed);
            SetFlame(flame, grow, flicker);
        }
    }

    private void SetFlame(Flame flame, float grow, float flicker)
    {
        VisualEffect effect = flame.effect;
        if (effect == null) return;

        if (effect.HasInt(spawnRateId))
            effect.SetInt(spawnRateId, Mathf.RoundToInt(flame.rate * grow * flicker));

        if (effect.HasFloat(sizeId))
            effect.SetFloat(sizeId, Mathf.Lerp(flame.size * 0.2f, flame.size, grow));
    }

    private void StartLoop()
    {
        StopLoop();

        if (fireLoop.IsNull) return;

        try
        {
            loopInstance = RuntimeManager.CreateInstance(fireLoop);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"HallwayFire: event {fireLoop} isn't in the loaded banks.", this);
            return;
        }

        UpdateLoop(0f);
        loopInstance.start();
    }

    private void UpdateLoop(float burning)
    {
        if (!loopInstance.isValid()) return;

        loopInstance.setVolume(Mathf.Lerp(startVolume, 1f, burning));

        Transform listener = Camera.main != null ? Camera.main.transform : transform;
        loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(listener));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);

        foreach (Flame flame in flames)
        {
            Gizmos.DrawSphere(flame.position, 0.08f);
            Gizmos.DrawLine(flame.position, flame.position + flame.normal * 0.4f);
        }
    }

    private Vector2Int ToKey(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
    }

    private Vector3 FromKey(Vector2Int key, float height)
    {
        return new Vector3((key.x + 0.5f) * cellSize, height, (key.y + 0.5f) * cellSize);
    }

    private Vector3Int ToBucket(Vector3 position)
    {
        float size = Mathf.Max(0.01f, flameSpacing);
        return new Vector3Int(Mathf.FloorToInt(position.x / size), Mathf.FloorToInt(position.y / size), Mathf.FloorToInt(position.z / size));
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private class FireHeap
    {
        private readonly List<Vector2Int> keys = new List<Vector2Int>();
        private readonly List<float> costs = new List<float>();

        public int Count => keys.Count;

        public void Push(Vector2Int key, float cost)
        {
            keys.Add(key);
            costs.Add(cost);

            int child = keys.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (costs[parent] <= costs[child]) break;

                Swap(parent, child);
                child = parent;
            }
        }

        public void Pop(out Vector2Int key, out float cost)
        {
            key = keys[0];
            cost = costs[0];

            int last = keys.Count - 1;
            keys[0] = keys[last];
            costs[0] = costs[last];
            keys.RemoveAt(last);
            costs.RemoveAt(last);

            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;

                if (left < keys.Count && costs[left] < costs[smallest]) smallest = left;
                if (right < keys.Count && costs[right] < costs[smallest]) smallest = right;
                if (smallest == parent) break;

                Swap(parent, smallest);
                parent = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (keys[a], keys[b]) = (keys[b], keys[a]);
            (costs[a], costs[b]) = (costs[b], costs[a]);
        }
    }
}