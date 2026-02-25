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

    // Thêm biến cho animation
    private GameObject contentPanel;
    private TextMeshProUGUI titleText;
    private Image glowImage;

    /// <summary>
    /// Tự động tạo UI Pause nếu chưa có
    /// </summary>
    private void CreatePauseUI()
    {
        // Tạo Canvas MỚI riêng cho Pause UI
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // === MAIN PAUSE PANEL (Full screen overlay) ===
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvas.transform, false);
        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        SetFullStretch(panelImage.rectTransform);

        // === VIGNETTE EFFECT (4 góc tối) ===
        CreateVignetteCorners(pausePanel.transform);

        // === DECORATIVE LINES (trên và dưới) ===
        CreateDecorativeLine(pausePanel.transform, true); // Top
        CreateDecorativeLine(pausePanel.transform, false); // Bottom

        // === CONTENT CONTAINER ===
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(pausePanel.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.2f, 0.15f);
        contentRect.anchorMax = new Vector2(0.8f, 0.85f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // === GLOW BACKGROUND ===
        GameObject glowObj = new GameObject("GlowBg");
        glowObj.transform.SetParent(contentPanel.transform, false);
        glowImage = glowObj.AddComponent<Image>();
        glowImage.color = new Color(0.4f, 0.2f, 0.8f, 0.15f); // Tím nhạt
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(-0.1f, -0.1f);
        glowRect.anchorMax = new Vector2(1.1f, 1.1f);
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;

        // === MAIN BOX ===
        GameObject boxObj = new GameObject("MainBox");
        boxObj.transform.SetParent(contentPanel.transform, false);
        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.1f, 0.1f);
        boxRect.anchorMax = new Vector2(0.9f, 0.9f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;

        // === GRADIENT BORDER ===
        CreateGradientBorder(boxObj.transform);

        // === ICON PAUSE (2 thanh dọc) ===
        CreatePauseIcon(boxObj.transform);

        // === TITLE "TẠM DỪNG" ===
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(boxObj.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "TẠM DỪNG";
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.outlineWidth = 0.25f;
        titleText.outlineColor = new Color(0.5f, 0.3f, 1f, 0.8f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.55f);
        titleRect.anchorMax = new Vector2(0.95f, 0.75f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // === COUNTDOWN TEXT ===
        GameObject textObj = new GameObject("PauseText");
        textObj.transform.SetParent(boxObj.transform, false);
        pauseText = textObj.AddComponent<TextMeshProUGUI>();
        pauseText.text = "Nhấn <color=#FFD700>R</color> để tiếp tục";
        pauseText.fontSize = 36;
        pauseText.fontStyle = FontStyles.Normal;
        pauseText.alignment = TextAlignmentOptions.Center;
        pauseText.color = new Color(0.85f, 0.85f, 0.9f);
        pauseText.richText = true;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.3f);
        textRect.anchorMax = new Vector2(0.9f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // === HINT KEYS ===
        CreateKeyHints(boxObj.transform);

        // === DECORATIVE DOTS ===
        CreateDecorativeDots(boxObj.transform);

        Debug.Log("Đã tạo UI Pause đẹp!");
    }

    private void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void CreateVignetteCorners(Transform parent)
    {
        // Tạo 4 góc tối để tạo hiệu ứng vignette
        float cornerSize = 0.35f;
        Color cornerColor = new Color(0, 0, 0, 0.6f);

        // Top-Left
        CreateCorner(parent, "CornerTL", new Vector2(0, 1-cornerSize), new Vector2(cornerSize, 1), cornerColor);
        // Top-Right  
        CreateCorner(parent, "CornerTR", new Vector2(1-cornerSize, 1-cornerSize), new Vector2(1, 1), cornerColor);
        // Bottom-Left
        CreateCorner(parent, "CornerBL", new Vector2(0, 0), new Vector2(cornerSize, cornerSize), cornerColor);
        // Bottom-Right
        CreateCorner(parent, "CornerBR", new Vector2(1-cornerSize, 0), new Vector2(1, cornerSize), cornerColor);
    }

    private void CreateCorner(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject corner = new GameObject(name);
        corner.transform.SetParent(parent, false);
        Image img = corner.AddComponent<Image>();
        img.color = color;
        RectTransform rt = corner.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void CreateDecorativeLine(Transform parent, bool isTop)
    {
        GameObject line = new GameObject(isTop ? "TopLine" : "BottomLine");
        line.transform.SetParent(parent, false);
        Image img = line.AddComponent<Image>();
        
        // Gradient màu tím-xanh
        img.color = new Color(0.6f, 0.4f, 1f, 0.8f);
        
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, isTop ? 0.88f : 0.12f);
        rt.anchorMax = new Vector2(0.85f, isTop ? 0.885f : 0.125f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void CreateGradientBorder(Transform parent)
    {
        // Border trên
        CreateBorderSide(parent, "BorderTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), new Color(0.7f, 0.5f, 1f));
        // Border dưới
        CreateBorderSide(parent, "BorderBottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Color(0.3f, 0.7f, 1f));
        // Border trái
        CreateBorderSide(parent, "BorderLeft", new Vector2(0, 0), new Vector2(0, 1), new Vector2(4, 0), new Color(0.5f, 0.3f, 1f));
        // Border phải
        CreateBorderSide(parent, "BorderRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-4, 0), new Color(0.5f, 0.8f, 1f));
    }

    private void CreateBorderSide(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Color color)
    {
        GameObject border = new GameObject(name);
        border.transform.SetParent(parent, false);
        border.transform.SetAsFirstSibling();
        Image img = border.AddComponent<Image>();
        img.color = color;
        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
    }

    private void CreatePauseIcon(Transform parent)
    {
        GameObject iconContainer = new GameObject("PauseIcon");
        iconContainer.transform.SetParent(parent, false);
        RectTransform containerRT = iconContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.4f, 0.76f);
        containerRT.anchorMax = new Vector2(0.6f, 0.95f);
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        // Thanh trái
        GameObject bar1 = new GameObject("Bar1");
        bar1.transform.SetParent(iconContainer.transform, false);
        Image bar1Img = bar1.AddComponent<Image>();
        bar1Img.color = new Color(1f, 0.85f, 0.3f); // Vàng
        RectTransform bar1RT = bar1.GetComponent<RectTransform>();
        bar1RT.anchorMin = new Vector2(0.25f, 0.1f);
        bar1RT.anchorMax = new Vector2(0.4f, 0.9f);
        bar1RT.offsetMin = Vector2.zero;
        bar1RT.offsetMax = Vector2.zero;

        // Thanh phải
        GameObject bar2 = new GameObject("Bar2");
        bar2.transform.SetParent(iconContainer.transform, false);
        Image bar2Img = bar2.AddComponent<Image>();
        bar2Img.color = new Color(1f, 0.85f, 0.3f); // Vàng
        RectTransform bar2RT = bar2.GetComponent<RectTransform>();
        bar2RT.anchorMin = new Vector2(0.6f, 0.1f);
        bar2RT.anchorMax = new Vector2(0.75f, 0.9f);
        bar2RT.offsetMin = Vector2.zero;
        bar2RT.offsetMax = Vector2.zero;
    }

    private void CreateKeyHints(Transform parent)
    {
        GameObject hintsContainer = new GameObject("KeyHints");
        hintsContainer.transform.SetParent(parent, false);
        RectTransform hintsRT = hintsContainer.AddComponent<RectTransform>();
        hintsRT.anchorMin = new Vector2(0.1f, 0.08f);
        hintsRT.anchorMax = new Vector2(0.9f, 0.22f);
        hintsRT.offsetMin = Vector2.zero;
        hintsRT.offsetMax = Vector2.zero;

        // Key P
        CreateKeyBox(hintsContainer.transform, "KeyP", "P", "Tạm dừng", 0.15f);
        // Key R
        CreateKeyBox(hintsContainer.transform, "KeyR", "R", "Tiếp tục", 0.55f);
    }

    private void CreateKeyBox(Transform parent, string name, string key, string action, float xPos)
    {
        GameObject keyBox = new GameObject(name);
        keyBox.transform.SetParent(parent, false);
        RectTransform keyRT = keyBox.AddComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(xPos, 0.1f);
        keyRT.anchorMax = new Vector2(xPos + 0.3f, 0.9f);
        keyRT.offsetMin = Vector2.zero;
        keyRT.offsetMax = Vector2.zero;

        // Key background
        GameObject keyBg = new GameObject("KeyBg");
        keyBg.transform.SetParent(keyBox.transform, false);
        Image bgImg = keyBg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        RectTransform bgRT = keyBg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.2f);
        bgRT.anchorMax = new Vector2(0.35f, 0.95f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Key letter
        GameObject keyText = new GameObject("KeyText");
        keyText.transform.SetParent(keyBg.transform, false);
        TextMeshProUGUI keyTMP = keyText.AddComponent<TextMeshProUGUI>();
        keyTMP.text = key;
        keyTMP.fontSize = 28;
        keyTMP.fontStyle = FontStyles.Bold;
        keyTMP.alignment = TextAlignmentOptions.Center;
        keyTMP.color = new Color(1f, 0.85f, 0.3f);
        SetFullStretch(keyText.GetComponent<RectTransform>());

        // Action text
        GameObject actionText = new GameObject("ActionText");
        actionText.transform.SetParent(keyBox.transform, false);
        TextMeshProUGUI actionTMP = actionText.AddComponent<TextMeshProUGUI>();
        actionTMP.text = action;
        actionTMP.fontSize = 22;
        actionTMP.alignment = TextAlignmentOptions.Left;
        actionTMP.color = new Color(0.7f, 0.7f, 0.75f);
        RectTransform actionRT = actionText.GetComponent<RectTransform>();
        actionRT.anchorMin = new Vector2(0.4f, 0.2f);
        actionRT.anchorMax = new Vector2(1f, 0.9f);
        actionRT.offsetMin = Vector2.zero;
        actionRT.offsetMax = Vector2.zero;
    }

    private void CreateDecorativeDots(Transform parent)
    {
        // Tạo các chấm trang trí ở góc
        CreateDot(parent, new Vector2(0.05f, 0.05f), new Color(0.5f, 0.3f, 1f, 0.6f));
        CreateDot(parent, new Vector2(0.95f, 0.05f), new Color(0.3f, 0.7f, 1f, 0.6f));
        CreateDot(parent, new Vector2(0.05f, 0.95f), new Color(0.3f, 0.7f, 1f, 0.6f));
        CreateDot(parent, new Vector2(0.95f, 0.95f), new Color(0.5f, 0.3f, 1f, 0.6f));
    }

    private void CreateDot(Transform parent, Vector2 position, Color color)
    {
        GameObject dot = new GameObject("Dot");
        dot.transform.SetParent(parent, false);
        Image img = dot.AddComponent<Image>();
        img.color = color;
        RectTransform rt = dot.GetComponent<RectTransform>();
        rt.anchorMin = position;
        rt.anchorMax = position;
        rt.sizeDelta = new Vector2(12, 12);
        rt.anchoredPosition = Vector2.zero;
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
        Time.timeScale = 0f;
        AudioListener.pause = true;
        
        // Unlock cursor khi pause
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Hiển thị UI Pause với animation
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            StartCoroutine(AnimatePauseIn());
        }
        if (pauseText != null)
        {
            pauseText.text = "Nhấn <color=#FFD700>R</color> để tiếp tục";
            pauseText.fontSize = 36;
            pauseText.color = new Color(0.85f, 0.85f, 0.9f);
        }
        if (titleText != null)
        {
            titleText.text = "TẠM DỪNG";
        }
        
        Debug.Log("Game đã được PAUSE! Nhấn R để resume");
    }

    private IEnumerator AnimatePauseIn()
    {
        if (contentPanel == null) yield break;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        contentPanel.transform.localScale = Vector3.one * 0.8f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Ease out back
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            contentPanel.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.8f, 1.05f);
            yield return null;
        }
        
        contentPanel.transform.localScale = Vector3.one;
        
        // Bắt đầu glow animation
        StartCoroutine(AnimateGlow());
    }

    private IEnumerator AnimateGlow()
    {
        if (glowImage == null) yield break;
        
        while (isPaused)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * 2f) + 1f) / 2f;
            glowImage.color = new Color(0.4f + pulse * 0.2f, 0.2f + pulse * 0.1f, 0.8f + pulse * 0.2f, 0.1f + pulse * 0.1f);
            yield return null;
        }
    }

    /// <summary>
    /// Bắt đầu đếm ngược để resume game
    /// </summary>
    public void StartCountdownToResume()
    {
        if (!isPaused || isCountingDown) return;
        
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
        
        // Đổi title
        if (titleText != null)
        {
            titleText.text = "CHUẨN BỊ...";
            titleText.color = new Color(0.5f, 1f, 0.5f);
        }
        
        Debug.Log("Bắt đầu đếm ngược...");
        
        while (remainingTime > 0)
        {
            int secondsLeft = Mathf.CeilToInt(remainingTime);
            
            if (pauseText != null)
            {
                pauseText.text = secondsLeft.ToString();
                pauseText.fontSize = 150;
                
                // Màu gradient theo thời gian
                if (secondsLeft <= 1)
                {
                    pauseText.color = new Color(1f, 0.3f, 0.3f);
                    pauseText.outlineColor = new Color(0.8f, 0f, 0f, 0.8f);
                }
                else if (secondsLeft <= 2)
                {
                    pauseText.color = new Color(1f, 0.6f, 0.2f);
                    pauseText.outlineColor = new Color(0.8f, 0.4f, 0f, 0.8f);
                }
                else if (secondsLeft <= 3)
                {
                    pauseText.color = new Color(1f, 0.9f, 0.3f);
                    pauseText.outlineColor = new Color(0.8f, 0.7f, 0f, 0.8f);
                }
                else
                {
                    pauseText.color = new Color(0.4f, 1f, 0.4f);
                    pauseText.outlineColor = new Color(0f, 0.6f, 0f, 0.8f);
                }
                pauseText.outlineWidth = 0.3f;
                
                // Animation scale cho số
                StartCoroutine(AnimateCountdownNumber());
            }
            
            Debug.Log("Resume trong: " + secondsLeft + " giây...");
            yield return new WaitForSecondsRealtime(1f);
            remainingTime -= 1f;
        }
        
        // Hiệu ứng "GO!"
        if (pauseText != null)
        {
            pauseText.text = "GO!";
            pauseText.fontSize = 120;
            pauseText.color = new Color(0.3f, 1f, 0.3f);
        }
        
        yield return new WaitForSecondsRealtime(0.3f);
        
        // Resume game
        isPaused = false;
        isCountingDown = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        // Lock cursor lại cho gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Animation out
        yield return StartCoroutine(AnimatePauseOut());
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        Debug.Log("Game đã được RESUME!");
        countdownCoroutine = null;
    }

    private IEnumerator AnimateCountdownNumber()
    {
        if (pauseText == null) yield break;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        Vector3 originalScale = pauseText.transform.localScale;
        pauseText.transform.localScale = Vector3.one * 1.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            pauseText.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t);
            yield return null;
        }
        
        pauseText.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimatePauseOut()
    {
        if (contentPanel == null) yield break;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            contentPanel.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);
            yield return null;
        }
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
