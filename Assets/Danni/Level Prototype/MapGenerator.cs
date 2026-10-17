using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private enum CellType { Empty, Floor, Wall, Door }

    [Header("Source")]
    [SerializeField] private SpriteRenderer mapSpriteRenderer;
    [SerializeField] private Sprite mapSprite;
    [SerializeField] private bool hideSourceSpriteAfterGenerate = true;

    [Header("Colours")]
    [SerializeField] private Color floorColor = Color.black;
    [SerializeField] private Color wallColor = new Color(0.56f, 0.86f, 0.37f);
    [SerializeField] private Color doorColor = Color.green;
    [SerializeField, Range(0f, 1f)] private float colorTolerance = 0.25f;

    [Header("Dimensions")]
    [SerializeField] private float pixelSize = 0.1f;
    [SerializeField] private float floorThickness = 0.2f;
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private bool centerOnGenerator = true;

    [Header("Doors")]
    [SerializeField] private Door doorPrefab;
    [SerializeField] private bool fitDoorsToStrip = true;
    [SerializeField] private float doorHeight = 2.5f;
    [SerializeField] private float minDoorThickness = 0.1f;

    [Header("Materials")]
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;

    private Transform generatedRoot;
    private int floorCount;
    private int wallCount;
    private int doorCount;

    public bool Generate()
    {
        Sprite sprite = mapSprite != null ? mapSprite : mapSpriteRenderer != null ? mapSpriteRenderer.sprite : null;
        if (sprite == null) return false;
        Texture2D texture = sprite.texture;
        if (!texture.isReadable) return false;

        Rect rect = sprite.textureRect;
        int startX = Mathf.FloorToInt(rect.x);
        int startY = Mathf.FloorToInt(rect.y);
        int width = Mathf.FloorToInt(rect.width);
        int height = Mathf.FloorToInt(rect.height);

        Color[] pixels = texture.GetPixels(startX, startY, width, height);
        CellType[,] cells = new CellType[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells[x, y] = Classify(pixels[y * width + x]);
            }
        }
        Clear();
        floorCount = 0;
        wallCount = 0;
        doorCount = 0;
        generatedRoot = new GameObject("Generated Map").transform;
        generatedRoot.position = transform.position;

        Vector3 origin = centerOnGenerator
            ? new Vector3(-width * pixelSize * 0.5f, 0f, -height * pixelSize * 0.5f)
            : Vector3.zero;

        BuildRectangles(BuildMask(cells, CellType.Floor, CellType.Door), false, origin);
        BuildRectangles(BuildMask(cells, CellType.Wall), true, origin);
        BuildDoors(cells, origin);
        Debug.Log("map: spawned {floorCount} floor blocks, {wallCount} wall blocks and {doorCount} doors", this);
        if (hideSourceSpriteAfterGenerate && mapSpriteRenderer != null)
            mapSpriteRenderer.enabled = false;
        return true;
    }

    public void Clear()
    {
        if (generatedRoot != null)
        {
            Destroy(generatedRoot.gameObject);
            generatedRoot = null;
        }
    }

    private CellType Classify(Color pixel)
    {
        if (pixel.a < 0.5f) return CellType.Empty;

        CellType best = CellType.Empty;
        float bestDistance = colorTolerance;

        TryMatch(pixel, floorColor, CellType.Floor, ref best, ref bestDistance);
        TryMatch(pixel, wallColor, CellType.Wall, ref best, ref bestDistance);
        TryMatch(pixel, doorColor, CellType.Door, ref best, ref bestDistance);

        return best;
    }

    private static void TryMatch(Color pixel, Color target, CellType type, ref CellType best, ref float bestDistance)
    {
        float distance = ColorDistance(pixel, target);

        if (distance <= bestDistance)
        {
            bestDistance = distance;
            best = type;
        }
    }

    private static float ColorDistance(Color a, Color b)
    {
        float r = a.r - b.r;
        float g = a.g - b.g;
        float bl = a.b - b.b;
        return Mathf.Sqrt(r * r + g * g + bl * bl);
    }

    private static bool[,] BuildMask(CellType[,] cells, params CellType[] types)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        bool[,] mask = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                mask[x, y] = System.Array.IndexOf(types, cells[x, y]) >= 0;
            }
        }

        return mask;
    }

    private void BuildRectangles(bool[,] mask, bool isWall, Vector3 origin)
    {
        int width = mask.GetLength(0);
        int height = mask.GetLength(1);
        bool[,] used = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (used[x, y] || !mask[x, y]) continue;

                int rectWidth = 1;
                while (x + rectWidth < width && !used[x + rectWidth, y] && mask[x + rectWidth, y])
                {
                    rectWidth++;
                }

                int rectHeight = 1;
                while (y + rectHeight < height && RowMatches(mask, used, x, y + rectHeight, rectWidth))
                {
                    rectHeight++;
                }

                for (int dy = 0; dy < rectHeight; dy++)
                {
                    for (int dx = 0; dx < rectWidth; dx++)
                    {
                        used[x + dx, y + dy] = true;
                    }
                }

                SpawnBlock(isWall, x, y, rectWidth, rectHeight, origin);
            }
        }
    }

    private static bool RowMatches(bool[,] mask, bool[,] used, int startX, int y, int rectWidth)
    {
        for (int dx = 0; dx < rectWidth; dx++)
        {
            if (used[startX + dx, y] || !mask[startX + dx, y]) return false;
        }
        return true;
    }

    private void SpawnBlock(bool isWall, int x, int y, int rectWidth, int rectHeight, Vector3 origin)
    {
        float sizeX = rectWidth * pixelSize;
        float sizeZ = rectHeight * pixelSize;
        float sizeY = isWall ? wallHeight + floorThickness : floorThickness;

        float centerX = origin.x + (x + rectWidth * 0.5f) * pixelSize;
        float centerZ = origin.z + (y + rectHeight * 0.5f) * pixelSize;
        float centerY = isWall ? (wallHeight - floorThickness) * 0.5f : -floorThickness * 0.5f;

        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = isWall ? "Wall" : "Floor";
        if (isWall) wallCount++; else floorCount++;
        block.transform.SetParent(generatedRoot, false);
        block.transform.localPosition = new Vector3(centerX, centerY, centerZ);
        block.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

        Material material = isWall ? wallMaterial : floorMaterial;
        if (material != null)
        {
            block.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private void BuildDoors(CellType[,] cells, Vector3 origin)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[x, y] || cells[x, y] != CellType.Door) continue;

                int minX = x, maxX = x, minY = y, maxY = y;
                visited[x, y] = true;
                queue.Enqueue(new Vector2Int(x, y));

                while (queue.Count > 0)
                {
                    Vector2Int cell = queue.Dequeue();
                    minX = Mathf.Min(minX, cell.x);
                    maxX = Mathf.Max(maxX, cell.x);
                    minY = Mathf.Min(minY, cell.y);
                    maxY = Mathf.Max(maxY, cell.y);

                    TryVisit(cells, visited, queue, cell.x + 1, cell.y);
                    TryVisit(cells, visited, queue, cell.x - 1, cell.y);
                    TryVisit(cells, visited, queue, cell.x, cell.y + 1);
                    TryVisit(cells, visited, queue, cell.x, cell.y - 1);
                }

                SpawnDoor(minX, minY, maxX - minX + 1, maxY - minY + 1, origin);
            }
        }
    }

    private static void TryVisit(CellType[,] cells, bool[,] visited, Queue<Vector2Int> queue, int x, int y)
    {
        if (x < 0 || y < 0 || x >= cells.GetLength(0) || y >= cells.GetLength(1)) return;
        if (visited[x, y] || cells[x, y] != CellType.Door) return;

        visited[x, y] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    private void SpawnDoor(int x, int y, int stripWidth, int stripHeight, Vector3 origin)
    {
        if (doorPrefab == null)
            return;
        bool runsAlongX = stripWidth >= stripHeight;
        float length = (runsAlongX ? stripWidth : stripHeight) * pixelSize;
        float thickness = Mathf.Max((runsAlongX ? stripHeight : stripWidth) * pixelSize, minDoorThickness);
        float centerX = origin.x + (x + stripWidth * 0.5f) * pixelSize;
        float centerZ = origin.z + (y + stripHeight * 0.5f) * pixelSize;
        Door door = Instantiate(doorPrefab, generatedRoot);
        door.name = "Door";
        door.transform.localPosition = new Vector3(centerX, 0f, centerZ);
        door.transform.localRotation = Quaternion.Euler(0f, runsAlongX ? 0f : 90f, 0f);

        if (fitDoorsToStrip)
        {
            door.Configure(length, doorHeight, thickness);
        }

        doorCount++;
    }
}