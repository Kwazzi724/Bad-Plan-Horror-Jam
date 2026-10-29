using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class Mannequin : MonoBehaviour, IEnemy
{
    [Header("Parts")]
    [SerializeField] private Transform visual;
    [SerializeField] private MouthItem mouth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float catchDistance = 1.2f;
    [SerializeField] private float movingGrace = 0.2f;
    [SerializeField] private float minTravelToCatch = 0.3f;
    [SerializeField] private float gravity = -20f;

    [Header("Pathing")]
    [SerializeField] private LayerMask obstacles = ~0;
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField] private float searchPadding = 6f;
    [SerializeField] private int maxSearchNodes = 4000;
    [SerializeField] private float repathInterval = 0.4f;
    [SerializeField] private float walkableCacheTime = 2f;

    [Header("Being Seen")]
    [SerializeField] private float screenMargin = 0.05f;
    [SerializeField] private LayerMask sightBlockers = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    [Header("Sounds")]
    [SerializeField] private EventReference moveSound;
    [SerializeField] private float moveSoundDistance = 1.2f;

    private readonly List<Vector3> path = new List<Vector3>();
    private readonly Dictionary<Vector2Int, float> walkableCheckedAt = new Dictionary<Vector2Int, float>();
    private readonly Dictionary<Vector2Int, bool> walkableCache = new Dictionary<Vector2Int, bool>();
    private readonly Collider[] overlapBuffer = new Collider[16];

    private Transform player;
    private Camera viewer;
    private Stage stage;
    private Renderer[] renderers;
    private CharacterController controller;
    private bool isActive;
    private bool isInCatchRange;
    private bool isDisarmed;
    private float distanceSinceSound;
    private float verticalVelocity;
    private Vector3 currentTarget;
    private float nextRepathTime;
    private float lastMovedTime = float.NegativeInfinity;
    private float travelSinceFrozen;

    public bool IsSeen { get; private set; }

    private bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused;

    private void Awake()
    {
        stage = GetComponentInParent<Stage>();
        controller = GetComponent<CharacterController>();
        renderers = (visual != null ? visual : transform).GetComponentsInChildren<Renderer>(true);

        if (mouth == null)
            mouth = GetComponentInChildren<MouthItem>(true);

        if (mouth != null)
            mouth.Pickup.PickedUp += HandleMouthTaken;
    }

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            isActive = stage == null;
            return;
        }

        gameManager.StageEntered += HandleStageEntered;
        isActive = stage == null || gameManager.CurrentStage == stage;
    }

    private void OnDestroy()
    {
        if (mouth != null)
            mouth.Pickup.PickedUp -= HandleMouthTaken;

        if (GameManager.Instance != null)
            GameManager.Instance.StageEntered -= HandleStageEntered;
    }

    public void Begin(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
    }

    private void HandleStageEntered(Stage enteredStage, int stageNumber)
    {
        isActive = stage == null || enteredStage == stage;

        if (isActive)
        {
            path.Clear();
            walkableCache.Clear();
            walkableCheckedAt.Clear();
        }

        if (debugLogs)
            Debug.Log($"Mannequin '{name}': stage {stageNumber} entered, active = {isActive}.", this);
    }

    private void Update()
    {
        if (!isActive || isDisarmed || IsPaused) return;

        if (player == null)
        {
            PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement == null) return;
            player = playerMovement.transform;
        }

        if (viewer == null)
        {
            viewer = player.GetComponentInChildren<Camera>();

            if (viewer == null)
                viewer = Camera.main;

            if (viewer == null)
            {
                if (debugLogs)
                    Debug.LogWarning($"Mannequin '{name}': no camera found on the player.", this);
                return;
            }
        }

        bool wasSeen = IsSeen;
        IsSeen = CheckSeen();

        if (debugLogs && wasSeen != IsSeen)
            Debug.Log($"Mannequin '{name}': {(IsSeen ? "seen, freezing" : "not seen, moving")}.", this);

        if (IsSeen)
        {
            distanceSinceSound = 0f;
            travelSinceFrozen = 0f;
            return;
        }

        Move();

        bool isMoving = Time.time - lastMovedTime <= movingGrace && travelSinceFrozen >= minTravelToCatch;
        bool inRange = isMoving && FlatDistance(transform.position, player.position) <= catchDistance;

        if (inRange && !isInCatchRange)
            Caught();

        isInCatchRange = inRange;
    }

    private void Move()
    {
        currentTarget = ChooseTarget();

        Vector3 direction = currentTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);

        Vector3 step = direction.normalized * Mathf.Min(moveSpeed * Time.deltaTime, direction.magnitude);
        Vector3 before = transform.position;

        if (controller != null && controller.enabled)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(step + Vector3.up * verticalVelocity * Time.deltaTime);
        }
        else
        {
            transform.position += step;
        }

        float moved = FlatDistance(before, transform.position);

        if (moved > 0.001f)
        {
            lastMovedTime = Time.time;
            travelSinceFrozen += moved;
        }

        distanceSinceSound += moved;

        if (distanceSinceSound >= moveSoundDistance)
        {
            distanceSinceSound = 0f;
            SoundPlayer.PlayOneShot(moveSound, transform.position);
        }
    }

    private Vector3 ChooseTarget()
    {
        if (HasClearPath(player.position))
        {
            path.Clear();
            return player.position;
        }

        if (Time.time >= nextRepathTime || path.Count == 0)
        {
            nextRepathTime = Time.time + repathInterval;
            FindPath();
        }

        while (path.Count > 0 && FlatDistance(transform.position, path[0]) <= cellSize * 0.5f)
            path.RemoveAt(0);

        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (HasClearPath(path[i]))
                return path[i];
        }

        return path.Count > 0 ? path[0] : player.position;
    }

    private Vector2Int ToCell(Vector3 position)
    {
        return new Vector2Int(Mathf.RoundToInt(position.x / cellSize), Mathf.RoundToInt(position.z / cellSize));
    }

    private Vector3 ToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, transform.position.y, cell.y * cellSize);
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (walkableCache.TryGetValue(cell, out bool walkable)
            && walkableCheckedAt.TryGetValue(cell, out float checkedAt)
            && Time.time - checkedAt < walkableCacheTime)
            return walkable;

        GetCapsuleAt(ToWorld(cell), out Vector3 bottom, out Vector3 top, out float radius);
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius * 0.9f, overlapBuffer, obstacles, QueryTriggerInteraction.Ignore);

        walkable = true;
        for (int i = 0; i < count; i++)
        {
            Transform hit = overlapBuffer[i].transform;
            if (hit.IsChildOf(transform) || hit.IsChildOf(player)) continue;

            walkable = false;
            break;
        }

        walkableCache[cell] = walkable;
        walkableCheckedAt[cell] = Time.time;
        return walkable;
    }

    private void FindPath()
    {
        path.Clear();

        Vector2Int start = ToCell(transform.position);
        Vector2Int goal = ToCell(player.position);

        int padding = Mathf.CeilToInt(searchPadding / cellSize);
        int minX = Mathf.Min(start.x, goal.x) - padding;
        int maxX = Mathf.Max(start.x, goal.x) + padding;
        int minY = Mathf.Min(start.y, goal.y) - padding;
        int maxY = Mathf.Max(start.y, goal.y) + padding;

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> costSoFar = new Dictionary<Vector2Int, float> { [start] = 0f };
        CellHeap open = new CellHeap();
        open.Push(start, Heuristic(start, goal));

        int explored = 0;
        bool found = false;

        while (open.Count > 0 && explored < maxSearchNodes)
        {
            Vector2Int current = open.Pop();
            explored++;

            if (current == goal)
            {
                found = true;
                break;
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vector2Int next = new Vector2Int(current.x + dx, current.y + dy);
                    if (next.x < minX || next.x > maxX || next.y < minY || next.y > maxY) continue;

                    bool isGoal = next == goal;
                    if (!isGoal && !IsWalkable(next)) continue;

                    bool diagonal = dx != 0 && dy != 0;
                    if (diagonal && (!IsWalkable(new Vector2Int(current.x + dx, current.y)) || !IsWalkable(new Vector2Int(current.x, current.y + dy))))
                        continue;

                    float newCost = costSoFar[current] + (diagonal ? 1.4142f : 1f);

                    if (costSoFar.TryGetValue(next, out float existing) && existing <= newCost) continue;

                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    open.Push(next, newCost + Heuristic(next, goal));
                }
            }
        }

        if (!found)
        {
            if (debugLogs)
                Debug.Log($"Mannequin '{name}': no route found, heading straight for the player.", this);
            return;
        }

        Vector2Int step = goal;
        while (step != start)
        {
            path.Insert(0, ToWorld(step));
            step = cameFrom[step];
        }

        if (path.Count > 0)
            path[path.Count - 1] = new Vector3(player.position.x, transform.position.y, player.position.z);
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.4142f * Mathf.Min(dx, dy);
    }

    private bool HasClearPath(Vector3 target)
    {
        GetCapsule(out Vector3 bottom, out Vector3 top, out float radius);

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance < 0.01f) return true;

        if (!Physics.CapsuleCast(bottom, top, radius * 0.9f, direction / distance, out RaycastHit hit, distance, obstacles, QueryTriggerInteraction.Ignore))
            return true;

        return hit.transform == player || hit.transform.IsChildOf(player) || hit.transform.IsChildOf(transform);
    }

    private void GetCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        GetCapsuleAt(transform.position, out bottom, out top, out radius);
    }

    private void GetCapsuleAt(Vector3 position, out Vector3 bottom, out Vector3 top, out float radius)
    {
        if (controller != null)
        {
            radius = controller.radius;
            Vector3 center = position + transform.rotation * controller.center;
            float half = Mathf.Max(0f, controller.height * 0.5f - radius);
            bottom = center - Vector3.up * half + Vector3.up * controller.stepOffset;
            top = center + Vector3.up * half;
            return;
        }

        radius = 0.3f;
        bottom = position + Vector3.up * 0.5f;
        top = position + Vector3.up * 1.5f;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool CheckSeen()
    {
        if (!TryGetBounds(out Bounds bounds))
            return IsPointVisible(transform.position);

        Vector3 up = Vector3.up * bounds.extents.y * 0.9f;

        return IsPointVisible(bounds.center)
               || IsPointVisible(bounds.center + up)
               || IsPointVisible(bounds.center - up);
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer meshRenderer in renderers)
        {
            if (meshRenderer == null || !meshRenderer.enabled || !meshRenderer.transform.IsChildOf(transform)) continue;

            if (!found)
            {
                bounds = meshRenderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(meshRenderer.bounds);
            }
        }

        return found;
    }

    private bool IsPointVisible(Vector3 point)
    {
        Vector3 viewport = viewer.WorldToViewportPoint(point);

        if (viewport.z <= 0f) return false;

        bool onScreen = viewport.x > -screenMargin && viewport.x < 1f + screenMargin
                        && viewport.y > -screenMargin && viewport.y < 1f + screenMargin;

        if (!onScreen) return false;

        if (Physics.Linecast(viewer.transform.position, point, out RaycastHit hit, sightBlockers, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(transform);

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        for (int i = 1; i < path.Count; i++)
            Gizmos.DrawLine(path[i - 1], path[i]);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, currentTarget);
        Gizmos.DrawSphere(currentTarget, 0.15f);
    }

    private void HandleMouthTaken(Pickup item)
    {
        item.PickedUp -= HandleMouthTaken;
        mouth = null;
        isDisarmed = true;

        if (debugLogs)
            Debug.Log($"Mannequin '{name}': mouth taken, stopped for good.", this);
    }

    public void Caught()
    {
        if (debugLogs)
            Debug.Log($"Mannequin '{name}': caught the player.", this);

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
}

public class CellHeap
{
    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private readonly List<float> priorities = new List<float>();

    public int Count => cells.Count;

    public void Push(Vector2Int cell, float priority)
    {
        cells.Add(cell);
        priorities.Add(priority);

        int child = cells.Count - 1;
        while (child > 0)
        {
            int parent = (child - 1) / 2;
            if (priorities[parent] <= priorities[child]) break;
            Swap(parent, child);
            child = parent;
        }
    }

    public Vector2Int Pop()
    {
        Vector2Int top = cells[0];
        int last = cells.Count - 1;

        cells[0] = cells[last];
        priorities[0] = priorities[last];
        cells.RemoveAt(last);
        priorities.RemoveAt(last);

        int parent = 0;
        while (true)
        {
            int left = parent * 2 + 1;
            int right = left + 1;
            int smallest = parent;

            if (left < cells.Count && priorities[left] < priorities[smallest]) smallest = left;
            if (right < cells.Count && priorities[right] < priorities[smallest]) smallest = right;
            if (smallest == parent) break;

            Swap(parent, smallest);
            parent = smallest;
        }

        return top;
    }

    private void Swap(int a, int b)
    {
        (cells[a], cells[b]) = (cells[b], cells[a]);
        (priorities[a], priorities[b]) = (priorities[b], priorities[a]);
    }
}