using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Timeline review mê cung - Camera bay qua mê cung trước khi bắt đầu chơi
/// Gắn vào một GameObject trong scene (ví dụ: MazeGenerator hoặc tạo GameObject riêng)
/// </summary>
public class TimeLine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float reviewDuration = 5f; // Thời gian review (giây)
    [SerializeField] private float cameraHeight = 30f; // Độ cao camera khi review
    [SerializeField] private float rotationSpeed = 20f; // Tốc độ xoay camera (độ/giây)
    [SerializeField] private bool enableZoom = true; // Có zoom vào không
    [SerializeField] private float zoomStartHeight = 50f; // Độ cao ban đầu
    [SerializeField] private float zoomEndHeight = 25f; // Độ cao khi kết thúc
    
    [Header("UI")]
    [SerializeField] private string reviewText = "QUAN SÁT MÊ CUNG...";
    [SerializeField] private string skipText = "Nhấn SPACE để bỏ qua";
    [SerializeField] private string startingText = "BẮT ĐẦU!";
    
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera playerCamera;
    
    // Internal
    private bool isReviewing = false;
    private bool reviewCompleted = false;
    private GameObject reviewCameraObj;
    private Camera reviewCamera;
    private GameObject uiCanvas;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI skipHintText;
    private TextMeshProUGUI countdownText;
    private Vector3 mazeCenter;
    private float mazeSize;
    private float currentAngle = 0f;
    private float elapsedTime = 0f;
    
    // Singleton
    public static TimeLine Instance { get; private set; }
    public static bool IsReviewing => Instance != null && Instance.isReviewing;
    public static bool ReviewCompleted => Instance != null && Instance.reviewCompleted;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Tìm player và camera nếu chưa gán
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) playerTransform = player.transform;
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
        }
    }
    
    /// <summary>
    /// Bắt đầu timeline review mê cung
    /// Gọi sau khi maze được generate
    /// </summary>
    public void StartMazeReview(Vector3 center, float size)
    {
        if (isReviewing) return;
        
        mazeCenter = center;
        mazeSize = size;
        
        StartCoroutine(MazeReviewCoroutine());
    }
    
    /// <summary>
    /// Overload - tự động tính center và size từ MazeGenerator
    /// </summary>
    public void StartMazeReview()
    {
        MazeGenerator maze = FindFirstObjectByType<MazeGenerator>();
        if (maze != null)
        {
            // Tính toán center và size từ maze (dùng Properties viết hoa)
            Vector3 center = maze.transform.position;
            float size = Mathf.Max(maze.Width, maze.Height) * maze.CellSize;
            
            // Điều chỉnh center về giữa maze
            center.x += (maze.Width * maze.CellSize) / 2f;
            center.z += (maze.Height * maze.CellSize) / 2f;
            center.y = 0;
            
            StartMazeReview(center, size);
        }
        else
        {
            Debug.LogWarning("TimeLine: Không tìm thấy MazeGenerator!");
            // Bắt đầu game luôn nếu không có maze
            reviewCompleted = true;
        }
    }
    
    private IEnumerator MazeReviewCoroutine()
    {
        isReviewing = true;
        reviewCompleted = false;
        elapsedTime = 0f;
        currentAngle = 0f;
        float startedAt = Time.realtimeSinceStartup;
        
        Debug.Log("TimeLine: Bắt đầu review mê cung!");
        
        // 1. Ẩn player camera
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
        }
        
        // 2. Tạo review camera
        CreateReviewCamera();
        
        // 3. Tạo UI
        CreateReviewUI();
        
        // 4. Chạy review loop
        while (elapsedTime < reviewDuration)
        {
            // Failsafe: khong de review bi treo vo han khi timeScale/state bi lech.
            if (Time.realtimeSinceStartup - startedAt > reviewDuration + 5f)
            {
                Debug.LogWarning("TimeLine: Review timeout, force end.");
                break;
            }

            // Kiểm tra skip
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Debug.Log("TimeLine: Đã skip review!");
                break;
            }
            
            // Update camera position
            UpdateReviewCamera();
            
            // Update countdown
            float remaining = reviewDuration - elapsedTime;
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(remaining).ToString();
            }
            
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // 5. Hiệu ứng kết thúc
        yield return StartCoroutine(EndReviewEffect());
        
        // 6. Cleanup
        EndReview();
    }
    
    private void CreateReviewCamera()
    {
        // Tạo camera mới cho review
        reviewCameraObj = new GameObject("ReviewCamera");
        reviewCamera = reviewCameraObj.AddComponent<Camera>();
        reviewCamera.clearFlags = CameraClearFlags.Skybox;
        reviewCamera.depth = 100; // Hiện trên tất cả
        reviewCamera.fieldOfView = 60f;
        
        // Khong them AudioListener cho review camera de tranh duplicate listener warnings.
        
        // Đặt vị trí ban đầu
        float startHeight = enableZoom ? zoomStartHeight : cameraHeight;
        reviewCameraObj.transform.position = mazeCenter + Vector3.up * startHeight;
        reviewCameraObj.transform.LookAt(mazeCenter);
    }
    
    private void CreateReviewUI()
    {
        // Tạo Canvas
        uiCanvas = new GameObject("ReviewUICanvas");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        CanvasScaler scaler = uiCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        uiCanvas.AddComponent<GraphicRaycaster>();
        
        // Title text (trên cùng)
        titleText = CreateUIText(uiCanvas.transform, "TitleText", reviewText, 
            new Vector2(0.5f, 0.9f), 48, Color.white, FontStyles.Bold);
        
        // Skip hint (dưới cùng)
        skipHintText = CreateUIText(uiCanvas.transform, "SkipText", skipText,
            new Vector2(0.5f, 0.1f), 24, Color.yellow, FontStyles.Normal);
        
        // Countdown (giữa màn hình, to)
        countdownText = CreateUIText(uiCanvas.transform, "CountdownText", reviewDuration.ToString(),
            new Vector2(0.5f, 0.5f), 72, new Color(1, 1, 1, 0.5f), FontStyles.Bold);
        
        // Vignette effect (optional - dark edges)
        CreateVignette(uiCanvas.transform);
    }
    
    private TextMeshProUGUI CreateUIText(Transform parent, string name, string content, 
        Vector2 anchorPos, float fontSize, Color color, FontStyles style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        
        // Add outline for better visibility
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorPos.x - 0.3f, anchorPos.y - 0.05f);
        rect.anchorMax = new Vector2(anchorPos.x + 0.3f, anchorPos.y + 0.05f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        return tmp;
    }
    
    private void CreateVignette(Transform parent)
    {
        // Tạo 4 gradient panels ở 4 cạnh để tạo hiệu ứng vignette
        // Top
        CreateGradientPanel(parent, "VignetteTop", 
            new Vector2(0, 0.85f), new Vector2(1, 1), 
            new Color(0, 0, 0, 0.7f), new Color(0, 0, 0, 0));
        // Bottom
        CreateGradientPanel(parent, "VignetteBottom", 
            new Vector2(0, 0), new Vector2(1, 0.15f), 
            new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.7f));
    }
    
    private void CreateGradientPanel(Transform parent, string name, 
        Vector2 anchorMin, Vector2 anchorMax, Color topColor, Color bottomColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.3f);
        
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    
    private void UpdateReviewCamera()
    {
        if (reviewCamera == null) return;

        float dt = Time.unscaledDeltaTime;
        
        // Xoay camera quanh maze center
        currentAngle += rotationSpeed * dt;
        
        // Tính vị trí mới trên quỹ đạo tròn
        float radius = mazeSize * 0.6f; // Bán kính quỹ đạo
        float x = mazeCenter.x + Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
        float z = mazeCenter.z + Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius;
        
        // Tính độ cao (zoom effect)
        float currentHeight;
        if (enableZoom)
        {
            float t = elapsedTime / reviewDuration;
            currentHeight = Mathf.Lerp(zoomStartHeight, zoomEndHeight, t);
        }
        else
        {
            currentHeight = cameraHeight;
        }
        
        // Cập nhật vị trí camera
        Vector3 newPos = new Vector3(x, currentHeight, z);
        reviewCamera.transform.position = Vector3.Lerp(
            reviewCamera.transform.position, newPos, dt * 3f);
        
        // Luôn nhìn về phía center của maze
        Vector3 lookTarget = mazeCenter + Vector3.up * 2f;
        reviewCamera.transform.LookAt(lookTarget);
    }
    
    private IEnumerator EndReviewEffect()
    {
        // Hiệu ứng flash trắng và hiện chữ "BẮT ĐẦU!"
        if (titleText != null)
        {
            titleText.text = startingText;
            titleText.fontSize = 72;
            titleText.color = Color.green;
        }
        
        if (skipHintText != null)
        {
            skipHintText.gameObject.SetActive(false);
        }
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        // Tạo flash effect
        GameObject flashObj = new GameObject("Flash");
        flashObj.transform.SetParent(uiCanvas.transform, false);
        Image flash = flashObj.AddComponent<Image>();
        flash.color = new Color(1, 1, 1, 0);
        
        RectTransform flashRect = flashObj.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        
        // Flash in
        float flashDuration = 0.3f;
        float t = 0;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            flash.color = new Color(1, 1, 1, t / flashDuration * 0.8f);
            yield return null;
        }
        
        // Chờ một chút
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Flash out
        t = flashDuration;
        while (t > 0)
        {
            t -= Time.unscaledDeltaTime;
            flash.color = new Color(1, 1, 1, t / flashDuration * 0.8f);
            yield return null;
        }
    }
    
    private void EndReview()
    {
        isReviewing = false;
        reviewCompleted = true;

        // Dam bao game tiep tuc chay neu co script nao de timeScale=0.
        if (Time.timeScale <= 0.0001f)
        {
            Time.timeScale = 1f;
        }
        
        // Bat lai camera gameplay truoc khi xoa review camera.
        Camera activeGameplayCam = EnsureGameplayCamera();

        // Xóa review camera neu da co camera gameplay thay the.
        if (reviewCameraObj != null && activeGameplayCam != reviewCamera)
        {
            Destroy(reviewCameraObj);
        }

        // Xóa UI
        if (uiCanvas != null)
        {
            Destroy(uiCanvas);
        }
        
        Debug.Log("TimeLine: Kết thúc review - Bắt đầu game!");
    }

    private Camera EnsureGameplayCamera()
    {
        // 1) Camera da duoc tham chieu tren player.
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            return playerCamera;
        }

        // 2) Tim camera trong PlayerMovement.
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null && pm.cameraTransform != null)
        {
            Camera cam = pm.cameraTransform.GetComponent<Camera>();
            if (cam != null)
            {
                playerCamera = cam;
                playerCamera.enabled = true;
                return playerCamera;
            }
        }

        // 3) Tim camera scene bat ky (ke ca dang disable).
        Camera[] allCams = Resources.FindObjectsOfTypeAll<Camera>();
        for (int i = 0; i < allCams.Length; i++)
        {
            Camera cam = allCams[i];
            if (cam == null) continue;
            if (cam == reviewCamera) continue;
            if (!cam.gameObject.scene.IsValid()) continue;

            cam.enabled = true;
            playerCamera = cam;
            return playerCamera;
        }

        // 4) Emergency camera de tranh "No cameras rendering".
        GameObject emergency = new GameObject("EmergencyGameplayCamera");
        Camera emergencyCam = emergency.AddComponent<Camera>();
        emergencyCam.clearFlags = CameraClearFlags.Skybox;
        emergencyCam.depth = 10;

        if (pm != null)
        {
            emergency.transform.position = pm.transform.position + new Vector3(0f, 1.5f, -3f);
            emergency.transform.LookAt(pm.transform.position + Vector3.up * 1.2f);
        }

        playerCamera = emergencyCam;
        return playerCamera;
    }
    
    /// <summary>
    /// Bỏ qua review ngay lập tức
    /// </summary>
    public void SkipReview()
    {
        if (isReviewing)
        {
            StopAllCoroutines();
            EndReview();
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
