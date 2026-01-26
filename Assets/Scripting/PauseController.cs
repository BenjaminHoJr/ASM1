using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class PasueController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float countdownTime = 5f; // Thời gian đếm ngược trước khi resume (giây)
    [SerializeField] private Key pauseKey = Key.P; // Phím để pause game
    [SerializeField] private Key resumeKey = Key.R; // Phím để resume game
    
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel; // Panel chứa UI pause
    [SerializeField] private TextMeshProUGUI pauseText; // Text hiển thị "Pause" hoặc đếm ngược
    
    private bool isPaused = false;
    private bool isCountingDown = false;
    private Coroutine countdownCoroutine;

    void Start()
    {
        // Tự động tạo UI nếu chưa được gán
        if (pausePanel == null || pauseText == null)
        {
            CreatePauseUI();
        }
        
        // Ẩn UI pause khi bắt đầu
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Tự động tạo UI Pause nếu chưa có
    /// </summary>
    private void CreatePauseUI()
    {
        // Tìm hoặc tạo Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Tạo Pause Panel
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvas.transform, false);
        
        // Thêm Image làm background
        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f); // Nền đen mờ
        
        // Đặt kích thước full màn hình
        RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Tạo Text
        GameObject textObj = new GameObject("PauseText");
        textObj.transform.SetParent(pausePanel.transform, false);
        
        pauseText = textObj.AddComponent<TextMeshProUGUI>();
        pauseText.text = "PAUSE";
        pauseText.fontSize = 100;
        pauseText.alignment = TextAlignmentOptions.Center;
        pauseText.color = Color.white;
        
        // Căn giữa text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 200);
        textRect.anchoredPosition = Vector2.zero;

        Debug.Log("Đã tự động tạo UI Pause!");
    }

    void Update()
    {
        // Nhấn phím P để pause game
        if (Keyboard.current[pauseKey].wasPressedThisFrame && !isPaused && !isCountingDown)
        {
            PauseGame();
        }
        
        // Nhấn phím R để bắt đầu đếm ngược resume
        if (Keyboard.current[resumeKey].wasPressedThisFrame && isPaused && !isCountingDown)
        {
            StartCountdownToResume();
        }
    }

    /// <summary>
    /// Pause game ngay lập tức
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        Time.timeScale = 0f; // Dừng thời gian game
        AudioListener.pause = true; // Dừng tất cả âm thanh
        
        // Hiển thị UI Pause
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        if (pauseText != null)
        {
            pauseText.text = "PAUSE";
        }
        
        Debug.Log("Game đã được PAUSE! Nhấn R để resume (đếm ngược 5 giây)");
    }

    /// <summary>
    /// Bắt đầu đếm ngược để resume game
    /// </summary>
    public void StartCountdownToResume()
    {
        if (!isPaused || isCountingDown) return;
        
        // Bắt đầu coroutine đếm ngược
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownAndResume());
    }

    /// <summary>
    /// Coroutine đếm ngược và resume game
    /// </summary>
    private IEnumerator CountdownAndResume()
    {
        isCountingDown = true;
        float remainingTime = countdownTime;
        
        Debug.Log("Bắt đầu đếm ngược...");
        
        while (remainingTime > 0)
        {
            // Cập nhật UI hiển thị thời gian đếm ngược
            if (pauseText != null)
            {
                pauseText.text = Mathf.CeilToInt(remainingTime).ToString();
            }
            
            Debug.Log("Resume trong: " + Mathf.CeilToInt(remainingTime) + " giây...");
            yield return new WaitForSecondsRealtime(1f);
            remainingTime -= 1f;
        }
        
        // Resume game
        isPaused = false;
        isCountingDown = false;
        Time.timeScale = 1f;
        AudioListener.pause = false; // Tiếp tục phát âm thanh
        
        // Ẩn UI pause
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        Debug.Log("Game đã được RESUME!");
        
        countdownCoroutine = null;
    }

    /// <summary>
    /// Hủy đếm ngược (nếu cần)
    /// </summary>
    public void CancelCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            isCountingDown = false;
            
            // Quay lại hiển thị "PAUSE"
            if (pauseText != null)
            {
                pauseText.text = "PAUSE";
            }
            
            Debug.Log("Đã hủy đếm ngược!");
        }
    }

    /// <summary>
    /// Kiểm tra game có đang pause không
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Kiểm tra có đang đếm ngược không
    /// </summary>
    public bool IsCountingDown()
    {
        return isCountingDown;
    }

    // Đảm bảo timeScale được reset khi script bị disable hoặc destroy
    private void OnDisable()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
