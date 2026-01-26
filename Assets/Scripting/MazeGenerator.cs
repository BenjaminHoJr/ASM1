using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MazeGenerator : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard }

    [Header("Maze Size")]
    [SerializeField] private int width = 15;
    [SerializeField] private int height = 15;
    [SerializeField] private Difficulty difficulty = Difficulty.Normal;

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
    [SerializeField] private bool regenerateOnPlay = false; // false = chờ menu chọn độ khó

    [Header("Gameplay / UI")]
    [SerializeField] private bool showInstructionsBeforePlay = false; // false vì đã có DifficultyMenu
    [SerializeField, TextArea(3, 6)] private string instructionsText = "Find the exit. Use WASD or arrow keys to move. The limit for the number of jumps is 5.\nPress Start to begin.";
    [SerializeField] private Vector2 instructionsSize = new Vector2(600, 260);
    [SerializeField] private Font instructionsFont; // assign a font in Inspector; fallback to LegacyRuntime.ttf if null

    private Cell[,] grid;
    private GameObject instructionsCanvas;
    private Vector2Int startCell = new Vector2Int(0, 0);

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

        // Adjust size for difficulty
        ApplyDifficultySize();

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

        // Place entrances/exits now. Player spawn may wait until user presses Start.
        CreateEntranceAndExit();

        if (showInstructionsBeforePlay)
            ShowInstructions();
        else
            SpawnPlayerAtStart();
    }

    /// <summary>
    /// Set độ khó và kích thước maze từ bên ngoài
    /// </summary>
    public void SetDifficulty(Difficulty diff)
    {
        difficulty = diff;
        switch (diff)
        {
            case Difficulty.Easy:
                width = 8;
                height = 8;
                break;
            case Difficulty.Normal:
                width = 15;
                height = 15;
                break;
            case Difficulty.Hard:
                width = 25;
                height = 25;
                break;
        }
        Debug.Log("MazeGenerator: Đã set độ khó = " + diff + ", size = " + width + "x" + height);
    }

    private void ApplyDifficultySize()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                width = Mathf.Max(8, width);
                height = Mathf.Max(8, height);
                break;
            case Difficulty.Normal:
                // keep user provided sizes
                break;
            case Difficulty.Hard:
                // ensure a larger grid by default
                width = Mathf.Max(25, width);
                height = Mathf.Max(25, height);
                break;
        }
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

    private void CreateEntranceAndExit()
    {
        // Open entrance at startCell WEST
        OpenEntrance(startCell, 3);

        // For harder difficulty place exit at farthest reachable cell from start,
        // otherwise use opposite corner.
        Vector2Int exitCell;
        if (difficulty == Difficulty.Hard)
            exitCell = FindFarthestCell(startCell);
        else
            exitCell = new Vector2Int(width - 1, height - 1);

        OpenEntrance(exitCell, 1);

        // Place Exit (parented to maze is OK)
        if (exitPrefab != null)
        {
            Vector3 exitLocal = CellToWorld(exitCell.x, exitCell.y) + new Vector3(0, 0.01f, 0);
            Vector3 exitWorld = transform.TransformPoint(exitLocal);
            var exit = Instantiate(exitPrefab, exitWorld, Quaternion.identity, transform);
            exit.name = "Exit";
        }
    }

    private Vector2Int FindFarthestCell(Vector2Int from)
    {
        int[,] dist = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = -1;

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(from);
        dist[from.x, from.y] = 0;

        Vector2Int best = from;
        int bestDist = 0;

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int d = dist[c.x, c.y];
            if (d > bestDist)
            {
                bestDist = d;
                best = c;
            }

            // check neighbors through open walls
            var neighbors = new (Vector2Int delta, int wallIdx)[]
            {
                (new Vector2Int(0, 1), 0),  // N
                (new Vector2Int(1, 0), 1),  // E
                (new Vector2Int(0, -1), 2), // S
                (new Vector2Int(-1, 0), 3), // W
            };

            foreach (var n in neighbors)
            {
                int nx = c.x + n.delta.x;
                int ny = c.y + n.delta.y;
                if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                {
                    // move only if there's no wall between them
                    if (!grid[c.x, c.y].walls[n.wallIdx] && dist[nx, ny] == -1)
                    {
                        dist[nx, ny] = d + 1;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        return best;
    }

    private void SpawnPlayerAtStart()
    {
        if (playerPrefab == null) return;

        // Xóa Player cũ nếu có
        GameObject existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
        {
            Debug.Log("MazeGenerator: Xóa Player cũ trước khi spawn mới");
            DestroyImmediate(existingPlayer);
        }
        
        // Tìm và xóa Player trong DontDestroyOnLoad nếu có
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            Debug.Log("MazeGenerator: Xóa Player cũ: " + p.gameObject.name);
            DestroyImmediate(p.gameObject);
        }

        Vector3 spawnLocal = CellToWorld(startCell.x, startCell.y) + new Vector3(0, 0.25f, 0);
        Vector3 spawnWorld = transform.TransformPoint(spawnLocal);

        Debug.Log($"MazeGenerator: spawnLocal={spawnLocal} spawnWorld={spawnWorld}");

        var player = Instantiate(playerPrefab, spawnWorld, Quaternion.Euler(0, 0, 0));
        player.name = "Player";

        // If there is a CharacterController, disable it while we set position to avoid collisions moving it.
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = spawnWorld;
            cc.enabled = true;
        }
        else
        {
            player.position = spawnWorld;
        }

        // Optional: face toward +X (into maze)
        player.rotation = Quaternion.Euler(0, 0, 0);
    }

    // Opens (removes) any wall objects overlapping the entrance area at the cell's side
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

    private void ShowInstructions()
    {
        // If a canvas already exists, don't duplicate
        if (instructionsCanvas != null) return;

        // Ensure EventSystem exists
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Create Canvas
        instructionsCanvas = new GameObject("MazeInstructionsCanvas");
        var canvas = instructionsCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        instructionsCanvas.AddComponent<CanvasScaler>();
        instructionsCanvas.AddComponent<GraphicRaycaster>();

        // Panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(instructionsCanvas.transform, false);
        var image = panelGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.7f);
        var rt = panelGO.GetComponent<RectTransform>();
        rt.sizeDelta = instructionsSize;
        rt.anchoredPosition = Vector2.zero;

        // Choose font: inspector assigned font has priority, otherwise fallback to LegacyRuntime.ttf
        Font fontToUse = instructionsFont != null ? instructionsFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(panelGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = instructionsText;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = fontToUse;
        txt.fontSize = 20;
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.05f, 0.25f);
        trt.anchorMax = new Vector2(0.95f, 0.85f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        // Start Button
        var buttonGO = new GameObject("StartButton");
        buttonGO.transform.SetParent(panelGO.transform, false);
        var btn = buttonGO.AddComponent<Button>();
        var btnImage = buttonGO.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        var brt = buttonGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.35f, 0.05f);
        brt.anchorMax = new Vector2(0.65f, 0.2f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        var btnTextGO = new GameObject("ButtonText");
        btnTextGO.transform.SetParent(buttonGO.transform, false);
        var btnText = btnTextGO.AddComponent<Text>();
        btnText.text = "Start";
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.font = fontToUse;
        btnText.fontSize = 22;
        var btrt = btnTextGO.GetComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero;
        btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero;
        btrt.offsetMax = Vector2.zero;

        btn.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        // Destroy UI
        if (instructionsCanvas != null)
            Destroy(instructionsCanvas);

        SpawnPlayerAtStart();
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
