using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    [Tooltip("Player tag to check. Leave as default if using CharacterController or PlayerController component.")]
    public string playerTag = "Player";

    [Tooltip("Optional event to hook up in inspector (e.g. show UI, load scene).")]
    public UnityEvent onPlayerExit;

    [Tooltip("If true, timeScale is set to 0 and a basic win UI is created.")]
    public bool pauseAndShowDefaultUI = true;

    [Header("Win UI Settings")]
    public string winTitle = "🎉 CHIẾN THẮNG! 🎉";
    public string winSubtitle = "Bạn đã tìm được lối ra!";
    public Color titleColor = new Color(1f, 0.84f, 0f); // Vàng gold
    public Color buttonPlayColor = new Color(0.2f, 0.8f, 0.2f); // Xanh lá
    public Color buttonQuitColor = new Color(0.8f, 0.2f, 0.2f); // Đỏ

    bool triggered = false;
    private GameObject winCanvas;
    private float gameTime = 0f;
    private bool isGameRunning = false;

    void Start()
    {
        // Bắt đầu đếm thời gian khi game chạy
        StartCoroutine(TrackGameTime());
    }

    IEnumerator TrackGameTime()
    {
        // Chờ game bắt đầu
        yield return new WaitUntil(() => DifficultyMenu.GameStarted && !TimeLine.IsReviewing);
        isGameRunning = true;
        
        while (isGameRunning && !triggered)
        {
            gameTime += Time.deltaTime;
            yield return null;
        }
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!IsPlayerCollider(other)) return;

        triggered = true;
        onPlayerExit?.Invoke();

        if (pauseAndShowDefaultUI)
            ShowDefaultWin();
    }

    bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;

        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            return true;

        // CharacterController check (common for first-person controllers)
        if (other.GetComponent<CharacterController>() != null ||
            other.GetComponentInParent<CharacterController>() != null)
            return true;

        // Custom player controller script check (if your project has one)
        if (other.GetComponentInParent<MonoBehaviour>() is MonoBehaviour mb &&
            mb.GetType().Name.ToLower().Contains("playercontroller"))
            return true;

        return false;
    }

    void ShowDefaultWin()
    {
        Debug.Log("Player reached exit – YOU WIN!");
        isGameRunning = false;

        // Pause game
        Time.timeScale = 0f;
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ẩn GameUI
        if (GameUI.Instance != null)
        {
            GameUI.Instance.gameObject.SetActive(false);
        }

        // Ensure an EventSystem exists so UI can receive input
        if (FindExisting<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        // Create beautiful UI overlay
        CreateBeautifulWinUI();
    }

    void CreateBeautifulWinUI()
    {
        // Main Canvas
        winCanvas = new GameObject("WinCanvas");
        // Không dùng DontDestroyOnLoad để canvas tự động bị xóa khi reload scene
        var canvas = winCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        var scaler = winCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        winCanvas.AddComponent<GraphicRaycaster>();

        // Background với gradient effect
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(winCanvas.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.85f);
        SetFullStretch(bgImage.rectTransform);

        // Container chính
        var containerGO = new GameObject("Container");
        containerGO.transform.SetParent(winCanvas.transform, false);
        var containerRT = containerGO.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.15f, 0.1f);
        containerRT.anchorMax = new Vector2(0.85f, 0.9f);
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        // Panel chính với viền
        var panelGO = new GameObject("MainPanel");
        panelGO.transform.SetParent(containerGO.transform, false);
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        SetFullStretch(panelImage.rectTransform);

        // Viền panel
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(panelGO.transform, false);
        var borderImage = borderGO.AddComponent<Image>();
        borderImage.color = titleColor;
        var borderRT = borderImage.rectTransform;
        SetFullStretch(borderRT);
        // Tạo outline bằng cách scale nhỏ hơn
        var innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(borderGO.transform, false);
        var innerImage = innerGO.AddComponent<Image>();
        innerImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        var innerRT = innerImage.rectTransform;
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(4, 4);
        innerRT.offsetMax = new Vector2(-4, -4);

        // === TITLE ===
        var titleGO = CreateTMPText(panelGO.transform, "Title", winTitle, 72, titleColor, FontStyles.Bold);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.75f);
        titleRT.anchorMax = new Vector2(0.95f, 0.95f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // === SUBTITLE ===
        var subtitleGO = CreateTMPText(panelGO.transform, "Subtitle", winSubtitle, 36, Color.white, FontStyles.Normal);
        var subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0.1f, 0.62f);
        subtitleRT.anchorMax = new Vector2(0.9f, 0.75f);
        subtitleRT.offsetMin = Vector2.zero;
        subtitleRT.offsetMax = Vector2.zero;

        // === STATS PANEL ===
        var statsGO = new GameObject("StatsPanel");
        statsGO.transform.SetParent(panelGO.transform, false);
        var statsRT = statsGO.AddComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.15f, 0.35f);
        statsRT.anchorMax = new Vector2(0.85f, 0.6f);
        statsRT.offsetMin = Vector2.zero;
        statsRT.offsetMax = Vector2.zero;

        // Thời gian hoàn thành
        string timeStr = FormatTime(gameTime);
        var timeGO = CreateTMPText(statsGO.transform, "Time", "⏱️ Thời gian: " + timeStr, 32, Color.cyan, FontStyles.Normal);
        var timeRT = timeGO.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0, 0.5f);
        timeRT.anchorMax = new Vector2(1, 1f);
        timeRT.offsetMin = Vector2.zero;
        timeRT.offsetMax = Vector2.zero;

        // Số lần nhảy đã dùng (nếu có PlayerMovement)
        string jumpInfo = GetJumpInfo();
        var jumpGO = CreateTMPText(statsGO.transform, "Jumps", jumpInfo, 28, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);
        var jumpRT = jumpGO.GetComponent<RectTransform>();
        jumpRT.anchorMin = new Vector2(0, 0f);
        jumpRT.anchorMax = new Vector2(1, 0.5f);
        jumpRT.offsetMin = Vector2.zero;
        jumpRT.offsetMax = Vector2.zero;

        // === BUTTONS ===
        var buttonsGO = new GameObject("Buttons");
        buttonsGO.transform.SetParent(panelGO.transform, false);
        var buttonsRT = buttonsGO.AddComponent<RectTransform>();
        buttonsRT.anchorMin = new Vector2(0.1f, 0.08f);
        buttonsRT.anchorMax = new Vector2(0.9f, 0.28f);
        buttonsRT.offsetMin = Vector2.zero;
        buttonsRT.offsetMax = Vector2.zero;

        // Horizontal Layout
        var hlg = buttonsGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(20, 20, 10, 10);

        // Nút CHƠI LẠI
        CreateBeautifulButton(buttonsGO.transform, "PlayAgain", "🔄 CHƠI LẠI", buttonPlayColor, () =>
        {
            Time.timeScale = 1f;
            // Destroy WinCanvas trước khi reload
            if (winCanvas != null)
            {
                Destroy(winCanvas);
            }
            // Reset static variables
            ResetGameState();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        // Nút THOÁT
        CreateBeautifulButton(buttonsGO.transform, "Quit", "🚪 THOÁT", buttonQuitColor, () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        // Chọn nút đầu tiên
        var esComp = FindExisting<EventSystem>();
        if (esComp != null)
        {
            var firstButton = buttonsGO.GetComponentInChildren<Button>();
            if (firstButton != null)
                esComp.SetSelectedGameObject(firstButton.gameObject);
        }

        // Bắt đầu animation
        StartCoroutine(AnimateWinUI(panelGO.transform));
    }

    GameObject CreateTMPText(Transform parent, string name, string content, float fontSize, Color color, FontStyles style)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        
        // Outline
        tmp.outlineWidth = 0.15f;
        tmp.outlineColor = new Color(0, 0, 0, 0.5f);
        
        return textGO;
    }

    void CreateBeautifulButton(Transform parent, string name, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        // Background
        var img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        // Button component
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        
        // Button colors
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        colors.selectedColor = bgColor * 1.1f;
        btn.colors = colors;

        // Layout element
        var le = btnGO.AddComponent<LayoutElement>();
        le.minHeight = 70;
        le.preferredHeight = 80;

        // Label với TMP
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color(0, 0, 0, 0.7f);

        SetFullStretch(tmp.rectTransform);
    }

    void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    string GetJumpInfo()
    {
        // Tìm PlayerMovement để lấy thông tin nhảy
        var player = FindExisting<PlayerMovement>();
        if (player != null)
        {
            // Sử dụng reflection hoặc public property nếu có
            return "🦘 Hoàn thành mê cung!";
        }
        return "🏆 Xuất sắc!";
    }

    void ResetGameState()
    {
        // Reset DifficultyMenu state
        // Điều này sẽ được reset khi scene load lại
    }

    IEnumerator AnimateWinUI(Transform panel)
    {
        // Scale animation (từ nhỏ đến lớn)
        float duration = 0.5f;
        float elapsed = 0f;
        
        panel.localScale = Vector3.zero;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Ease out back
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            panel.localScale = Vector3.one * Mathf.Clamp01(scale);
            yield return null;
        }
        
        panel.localScale = Vector3.one;
    }

    // Compatibility helper to avoid uses of the deprecated FindObjectOfType<T>() API.
    static T FindExisting<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_2_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }
}