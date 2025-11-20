using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Size")]
    [SerializeField] private int width = 15;
    [SerializeField] private int height = 15;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;   // thin cube
    [SerializeField] private GameObject floorPrefab;  // plane or flat cube
    [SerializeField] private GameObject exitPrefab;   // has ExitTrigger
    [SerializeField] private Transform playerPrefab;  // has CharacterController & PlayerController

    [Header("Dimensions")]
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private float wallThickness = 0.25f;
    [SerializeField] private float wallHeight = 2.5f;
    [SerializeField] private float floorThickness = 0.1f;

    [Header("Generation")]
    [SerializeField] private int seed = 0; // 0 = random
    [SerializeField] private bool regenerateOnPlay = true;

    private Cell[,] grid;

    private class Cell
    {
        public bool visited;
        // 0=N,1=E,2=S,3=W
        public bool[] walls = { true, true, true, true };
    }

    private void Start()
    {
        if (regenerateOnPlay) Generate();
    }

    [ContextMenu("Generate Maze")]
    public void Generate()
    {
        ClearChildren();

        width = Mathf.Max(2, width);
        height = Mathf.Max(2, height);

        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = new Cell();

        if (seed == 0) seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        CarveMaze(0, 0);
        BuildFloor();
        BuildWalls();
        CreateEntranceAndExitAndPlayer();
    }

    private void CarveMaze(int startX, int startY)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(startX, startY);
        grid[current.x, current.y].visited = true;

        while (true)
        {
            var neighbors = GetUnvisitedNeighbors(current);
            if (neighbors.Count > 0)
            {
                var choice = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int next = current + choice.dir;
                int w = choice.wallIndex;
                int opposite = (w + 2) % 4;

                grid[current.x, current.y].walls[w] = false;
                grid[next.x, next.y].walls[opposite] = false;

                stack.Push(current);
                current = next;
                grid[current.x, current.y].visited = true;
            }
            else if (stack.Count > 0)
            {
                current = stack.Pop();
            }
            else break;
        }
    }

    private List<(Vector2Int dir, int wallIndex)> GetUnvisitedNeighbors(Vector2Int c)
    {
        var res = new List<(Vector2Int, int)>();
        var dirs = new (Vector2Int delta, int wallIdx)[]
        {
            (new Vector2Int(0, 1), 0),  // N
            (new Vector2Int(1, 0), 1),  // E
            (new Vector2Int(0, -1), 2), // S
            (new Vector2Int(-1, 0), 3), // W
        };

        foreach (var d in dirs)
        {
            int nx = c.x + d.delta.x;
            int ny = c.y + d.delta.y;
            if (nx >= 0 && ny >= 0 && nx < width && ny < height && !grid[nx, ny].visited)
                res.Add((d.delta, d.wallIdx));
        }
        return res;
    }

    private void BuildFloor()
    {
        var floorGO = Instantiate(floorPrefab, transform);
        float mazeW = width * cellSize;
        float mazeH = height * cellSize;

        floorGO.name = "Floor";
        floorGO.transform.localScale = new Vector3(mazeW, floorThickness, mazeH);
        floorGO.transform.localPosition = new Vector3(
            mazeW / 2f - cellSize / 2f,
            -floorThickness / 2f,
            mazeH / 2f - cellSize / 2f
        );
    }

    private void BuildWalls()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 cellCenter = CellToWorld(x, y);

                if (grid[x, y].walls[0])
                    SpawnWall(cellCenter + new Vector3(0, wallHeight / 2f, +cellSize / 2f - wallThickness / 2f),
                        new Vector3(cellSize, wallHeight, wallThickness), 0);

                if (grid[x, y].walls[1])
                    SpawnWall(cellCenter + new Vector3(+cellSize / 2f - wallThickness / 2f, wallHeight / 2f, 0),
                        new Vector3(wallThickness, wallHeight, cellSize), 0);

                if (grid[x, y].walls[2])
                    SpawnWall(cellCenter + new Vector3(0, wallHeight / 2f, -cellSize / 2f + wallThickness / 2f),
                        new Vector3(cellSize, wallHeight, wallThickness), 0);

                if (grid[x, y].walls[3])
                    SpawnWall(cellCenter + new Vector3(-cellSize / 2f + wallThickness / 2f, wallHeight / 2f, 0),
                        new Vector3(wallThickness, wallHeight, cellSize), 0);
            }

        BuildPerimeter();
    }

    private void BuildPerimeter()
    {
        float mazeW = width * cellSize;
        float mazeH = height * cellSize;
        float y = wallHeight / 2f;

        SpawnWall(new Vector3(mazeW / 2f - cellSize / 2f, y, mazeH - cellSize / 2f),
            new Vector3(mazeW, wallHeight, wallThickness), 0); // North
        SpawnWall(new Vector3(mazeW / 2f - cellSize / 2f, y, -cellSize / 2f),
            new Vector3(mazeW, wallHeight, wallThickness), 0); // South
        SpawnWall(new Vector3(mazeW - cellSize / 2f, y, mazeH / 2f - cellSize / 2f),
            new Vector3(wallThickness, wallHeight, mazeH), 0); // East
        SpawnWall(new Vector3(-cellSize / 2f, y, mazeH / 2f - cellSize / 2f),
            new Vector3(wallThickness, wallHeight, mazeH), 0); // West
    }

    private void SpawnWall(Vector3 localPos, Vector3 scale, float yRot)
    {
        var w = Instantiate(wallPrefab, transform);
        w.name = "Wall";
        w.transform.localPosition = localPos;
        w.transform.localRotation = Quaternion.Euler(0, yRot, 0);
        w.transform.localScale = scale;
    }

    private Vector3 CellToWorld(int x, int y)
    {
        return new Vector3(x * cellSize, 0, y * cellSize);
    }

    private void CreateEntranceAndExitAndPlayer()
    {
        // Open entrance at (0,0) WEST
        Vector2Int startCell = new Vector2Int(0, 0);
        OpenEntrance(startCell, 3);

        // Open exit at (width-1,height-1) EAST
        Vector2Int exitCell = new Vector2Int(width - 1, height - 1);
        OpenEntrance(exitCell, 1);

        // Place Exit
        if (exitPrefab != null)
        {
            var exit = Instantiate(exitPrefab, transform);
            exit.name = "Exit";
            exit.transform.localPosition = CellToWorld(exitCell.x, exitCell.y) + new Vector3(0, 0.01f, 0);
        }

        // Spawn Player at start
        if (playerPrefab != null)
        {
            Vector3 spawnPos = CellToWorld(startCell.x, startCell.y) + new Vector3(0, 0.25f, 0);
            var player = Instantiate(playerPrefab, transform);
            player.name = "Player";
            player.transform.localPosition = spawnPos;

            // Optional: face toward +X (into maze)
            player.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private void OpenEntrance(Vector2Int cell, int side)
    {
        Vector3 center = CellToWorld(cell.x, cell.y);
        Vector3 dir = side switch
        {
            0 => Vector3.forward,
            1 => Vector3.right,
            2 => Vector3.back,
            3 => Vector3.left,
            _ => Vector3.right
        };

        Vector3 hitPos = center + dir * (cellSize / 2f);
        Collider[] hits = Physics.OverlapBox(
            transform.TransformPoint(hitPos + new Vector3(0, wallHeight / 2f, 0)),
            new Vector3(cellSize * 0.45f, wallHeight * 0.6f, Mathf.Max(wallThickness, 0.3f) * 0.75f),
            transform.rotation);

        foreach (var h in hits)
        {
            if (h && h.gameObject.name.Contains("Wall"))
                DestroyImmediate(h.gameObject);
        }
    }

    private void ClearChildren()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in transform)
            toDestroy.Add(child.gameObject);
#if UNITY_EDITOR
        foreach (var go in toDestroy) DestroyImmediate(go);
#else
        foreach (var go in toDestroy) Destroy(go);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float mazeW = width * cellSize;
        float mazeH = height * cellSize;
        Gizmos.DrawWireCube(
            transform.TransformPoint(new Vector3(mazeW / 2f - cellSize / 2f, 0, mazeH / 2f - cellSize / 2f)),
            new Vector3(mazeW, 0.01f, mazeH)
        );
    }
}
