using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering;

public class BloodFlood : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private Transform source;
    [SerializeField] private Vector3 areaCenter = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 areaSize = new Vector3(6f, 2f, 20f);
    [SerializeField] private float cellSize = 0.25f;
    [SerializeField] private LayerMask levelMask = ~0;
    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField, Range(0f, 1f)] private float minFloorNormal = 0.7f;

    [Header("Start")]
    [SerializeField] private Pickup cloth;
    [SerializeField] private float startDelay = 10f;

    [Header("Overflow")]
    [SerializeField] private float overflowDuration = 60f;
    [SerializeField, Range(0.05f, 1f)] private float spreadPortion = 0.4f;
    [SerializeField] private float spreadExponent = 1.3f;
    [SerializeField] private float drownHeight = 2f;
    [SerializeField] private AnimationCurve riseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.4f, 0.03f),
        new Keyframe(1f, 1f));

    [Header("Look")]
    [SerializeField] private Material bloodMaterial;
    [SerializeField] private float filmThickness = 0.03f;
    [SerializeField] private float settleTime = 1.5f;
    [SerializeField] private float hideDepth = 0.05f;
    [SerializeField] private float noiseScale = 0.6f;
    [SerializeField] private float noiseStrength = 1.5f;
    [SerializeField] private float uphillCost = 4f;
    [SerializeField] private float updateInterval = 0.033f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem sourceEffect;
    [SerializeField] private EventReference floodLoop;

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private int width;
    private int depth;
    private Vector3 gridOrigin;
    private bool[] walkable;
    private float[] floorHeight;
    private float[] arrival;
    private float[] cellSurface;
    private Vector3[] vertices;
    private float lowestFloor;
    private float sourceFloor;
    private bool baked;

    private Mesh mesh;
    private MeshRenderer surfaceRenderer;
    private EventInstance loopInstance;
    private bool started;
    private bool overflowed;

    public bool IsFlooding { get; private set; }
    public float Elapsed { get; private set; }

    private void Start()
    {
        Bake();

        if (cloth == null)
            cloth = FindCloth();

        if (cloth != null)
            cloth.PickedUp += HandleClothPickedUp;
        else
            Debug.LogWarning($"BloodFlood '{name}' couldn't find a cloth to wait for.", this);
    }

    private void OnDestroy()
    {
        if (cloth != null)
            cloth.PickedUp -= HandleClothPickedUp;

        StopLoop();

        if (mesh != null)
            Destroy(mesh);
    }

    private Pickup FindCloth()
    {
        Stage stage = GetComponentInParent<Stage>();
        Cloth found = stage != null ? stage.GetComponentInChildren<Cloth>(true) : FindAnyObjectByType<Cloth>();
        return found != null ? found.GetComponent<Pickup>() : null;
    }

    private void HandleClothPickedUp(Pickup item)
    {
        item.PickedUp -= HandleClothPickedUp;
        StartFlood();
    }

    public void StartFlood()
    {
        if (started || !baked) return;

        started = true;
        StartCoroutine(FloodRoutine());
    }

    private IEnumerator FloodRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        IsFlooding = true;
        Elapsed = 0f;
        surfaceRenderer.enabled = true;

        if (sourceEffect != null)
            sourceEffect.Play();

        StartLoop();

        float sinceUpdate = updateInterval;

        while (Elapsed < overflowDuration)
        {
            Elapsed += Time.deltaTime;
            sinceUpdate += Time.deltaTime;

            if (sinceUpdate >= updateInterval)
            {
                sinceUpdate = 0f;
                UpdateSurface(Elapsed);
            }

            yield return null;
        }

        Elapsed = overflowDuration;
        UpdateSurface(Elapsed);
        IsFlooding = false;
        Overflow();
    }

    public void Overflow()
    {
        if (overflowed) return;

        overflowed = true;

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    private void Bake()
    {
        if (source == null)
        {
            Debug.LogWarning($"BloodFlood '{name}' has no source.", this);
            return;
        }

        Physics.SyncTransforms();

        width = Mathf.Max(1, Mathf.CeilToInt(areaSize.x / cellSize));
        depth = Mathf.Max(1, Mathf.CeilToInt(areaSize.z / cellSize));
        gridOrigin = areaCenter - new Vector3(areaSize.x, 0f, areaSize.z) * 0.5f;

        int count = width * depth;
        walkable = new bool[count];
        floorHeight = new float[count];
        arrival = new float[count];
        cellSurface = new float[count];

        SampleFloor();

        int sourceCell = FindNearestCell(transform.InverseTransformPoint(source.position));
        if (sourceCell < 0)
        {
            Debug.LogWarning($"BloodFlood '{name}' found no floor inside its area. Check the area box and Level Mask.", this);
            return;
        }

        float[] distance = SpreadFrom(sourceCell);
        float maxDistance = 0f;
        lowestFloor = float.MaxValue;
        sourceFloor = floorHeight[sourceCell];

        for (int i = 0; i < count; i++)
        {
            if (float.IsPositiveInfinity(distance[i])) continue;

            maxDistance = Mathf.Max(maxDistance, distance[i]);
            lowestFloor = Mathf.Min(lowestFloor, floorHeight[i]);
        }

        float spreadTime = overflowDuration * spreadPortion;

        for (int i = 0; i < count; i++)
        {
            if (float.IsPositiveInfinity(distance[i]))
            {
                arrival[i] = float.PositiveInfinity;
                continue;
            }

            float normalised = maxDistance > 0f ? distance[i] / maxDistance : 0f;
            arrival[i] = spreadTime * Mathf.Pow(normalised, spreadExponent);
        }

        BuildMesh();
        UpdateSurface(-1f);
        baked = true;
    }

    private void SampleFloor()
    {
        Vector3 up = transform.up;
        float top = areaCenter.y + areaSize.y * 0.5f;
        float rayLength = areaSize.y * transform.lossyScale.y;
        Vector3 boxHalf = new Vector3(cellSize * 0.4f, 0.25f, cellSize * 0.4f);

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = Index(x, z);
                Vector3 local = CellCenter(x, z);
                Vector3 origin = transform.TransformPoint(new Vector3(local.x, top, local.z));

                if (!Physics.Raycast(origin, -up, out RaycastHit hit, rayLength, levelMask, QueryTriggerInteraction.Ignore))
                    continue;

                if (Vector3.Dot(hit.normal, up) < minFloorNormal)
                    continue;

                Vector3 clearanceCenter = hit.point + up * (0.05f + boxHalf.y);
                if (Physics.CheckBox(clearanceCenter, boxHalf, transform.rotation, levelMask, QueryTriggerInteraction.Ignore))
                    continue;

                walkable[i] = true;
                floorHeight[i] = transform.InverseTransformPoint(hit.point).y;
            }
        }
    }

    private float[] SpreadFrom(int start)
    {
        int count = width * depth;
        float[] distance = new float[count];
        for (int i = 0; i < count; i++)
            distance[i] = float.PositiveInfinity;

        float seed = Random.Range(0f, 1000f);
        FloodHeap open = new FloodHeap(count);
        distance[start] = 0f;
        open.Push(start, 0f);

        while (open.Count > 0)
        {
            open.Pop(out int current, out float currentDistance);
            if (currentDistance > distance[current]) continue;

            int cx = current % width;
            int cz = current / width;

            for (int d = 0; d < Directions.Length; d++)
            {
                Vector2Int step = Directions[d];
                int nx = cx + step.x;
                int nz = cz + step.y;

                if (!IsWalkable(nx, nz)) continue;

                bool diagonal = step.x != 0 && step.y != 0;
                if (diagonal && (!IsWalkable(cx + step.x, cz) || !IsWalkable(cx, cz + step.y))) continue;

                int next = Index(nx, nz);
                float rise = floorHeight[next] - floorHeight[current];
                if (Mathf.Abs(rise) > maxStepHeight) continue;

                Vector3 nextCenter = CellCenter(nx, nz);
                float noise = Mathf.PerlinNoise(seed + nextCenter.x * noiseScale, seed + nextCenter.z * noiseScale);
                float length = (diagonal ? 1.41421356f : 1f) * cellSize;
                float cost = length * (1f + noise * noiseStrength) + Mathf.Max(0f, rise) * uphillCost;
                float newDistance = currentDistance + cost;

                if (newDistance < distance[next])
                {
                    distance[next] = newDistance;
                    open.Push(next, newDistance);
                }
            }
        }

        return distance;
    }

    private void BuildMesh()
    {
        int vertexWidth = width + 1;
        vertices = new Vector3[vertexWidth * (depth + 1)];

        for (int z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
                vertices[x + z * vertexWidth] = gridOrigin + new Vector3(x * cellSize, 0f, z * cellSize);
        }

        List<int> triangles = new List<int>();

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (float.IsPositiveInfinity(arrival[Index(x, z)])) continue;

                int a = x + z * vertexWidth;
                int b = a + 1;
                int c = a + vertexWidth;
                int d = c + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh = new Mesh { name = "Blood Surface" };
        mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0);

        GameObject surface = new GameObject("Blood Surface");
        surface.transform.SetParent(transform, false);
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;

        surfaceRenderer = surface.AddComponent<MeshRenderer>();
        surfaceRenderer.sharedMaterial = bloodMaterial;
        surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
        surfaceRenderer.enabled = false;
    }

    private void UpdateSurface(float time)
    {
        float progress = overflowDuration > 0f ? Mathf.Clamp01(time / overflowDuration) : 1f;
        float poolLevel = Mathf.Lerp(lowestFloor, sourceFloor + drownHeight, riseCurve.Evaluate(progress));

        for (int i = 0; i < cellSurface.Length; i++)
        {
            if (float.IsPositiveInfinity(arrival[i])) continue;

            float since = time - arrival[i];

            if (since < 0f)
            {
                cellSurface[i] = floorHeight[i] - hideDepth;
                continue;
            }

            float settle = settleTime > 0f ? Mathf.SmoothStep(0f, 1f, since / settleTime) : 1f;
            float film = floorHeight[i] - hideDepth + (filmThickness + hideDepth) * settle;
            cellSurface[i] = Mathf.Max(film, poolLevel);
        }

        int vertexWidth = width + 1;

        for (int z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float total = 0f;
                int used = 0;

                for (int oz = -1; oz <= 0; oz++)
                {
                    for (int ox = -1; ox <= 0; ox++)
                    {
                        int cx = x + ox;
                        int cz = z + oz;
                        if (cx < 0 || cz < 0 || cx >= width || cz >= depth) continue;

                        int cell = Index(cx, cz);
                        if (float.IsPositiveInfinity(arrival[cell])) continue;

                        total += cellSurface[cell];
                        used++;
                    }
                }

                if (used == 0) continue;

                int v = x + z * vertexWidth;
                vertices[v].y = total / used;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private int FindNearestCell(Vector3 localPoint)
    {
        int best = -1;
        float bestDistance = float.MaxValue;

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = Index(x, z);
                if (!walkable[i]) continue;

                Vector3 center = CellCenter(x, z);
                float distance = new Vector2(center.x - localPoint.x, center.z - localPoint.z).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
        }

        return best;
    }

    private bool IsWalkable(int x, int z)
    {
        return x >= 0 && z >= 0 && x < width && z < depth && walkable[Index(x, z)];
    }

    private int Index(int x, int z)
    {
        return x + z * width;
    }

    private Vector3 CellCenter(int x, int z)
    {
        return gridOrigin + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
    }

    private void StartLoop()
    {
        if (floodLoop.IsNull) return;

        try
        {
            loopInstance = RuntimeManager.CreateInstance(floodLoop);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning($"BloodFlood: event {floodLoop} isn't in the loaded banks.", this);
            return;
        }

        Vector3 position = source != null ? source.position : transform.position;
        loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        loopInstance.start();
    }

    private void StopLoop()
    {
        if (!loopInstance.isValid()) return;

        loopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        loopInstance.release();
        loopInstance.clearHandle();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.8f, 0f, 0f, 0.6f);
        Gizmos.DrawWireCube(areaCenter, areaSize);

        if (baked && walkable != null)
        {
            Gizmos.color = new Color(0.8f, 0f, 0f, 0.25f);
            Vector3 size = new Vector3(cellSize * 0.9f, 0.01f, cellSize * 0.9f);

            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = Index(x, z);
                    if (float.IsPositiveInfinity(arrival[i])) continue;

                    Vector3 center = CellCenter(x, z);
                    center.y = floorHeight[i] + 0.02f;
                    Gizmos.DrawCube(center, size);
                }
            }
        }

        Gizmos.matrix = Matrix4x4.identity;

        if (source != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(source.position, 0.15f);
        }
    }

    private class FloodHeap
    {
        private readonly List<int> cells;
        private readonly List<float> costs;

        public int Count => cells.Count;

        public FloodHeap(int capacity)
        {
            cells = new List<int>(capacity);
            costs = new List<float>(capacity);
        }

        public void Push(int cell, float cost)
        {
            cells.Add(cell);
            costs.Add(cost);

            int child = cells.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (costs[parent] <= costs[child]) break;

                Swap(parent, child);
                child = parent;
            }
        }

        public void Pop(out int cell, out float cost)
        {
            cell = cells[0];
            cost = costs[0];

            int last = cells.Count - 1;
            cells[0] = cells[last];
            costs[0] = costs[last];
            cells.RemoveAt(last);
            costs.RemoveAt(last);

            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;

                if (left < cells.Count && costs[left] < costs[smallest]) smallest = left;
                if (right < cells.Count && costs[right] < costs[smallest]) smallest = right;
                if (smallest == parent) break;

                Swap(parent, smallest);
                parent = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (cells[a], cells[b]) = (cells[b], cells[a]);
            (costs[a], costs[b]) = (costs[b], costs[a]);
        }
    }
}