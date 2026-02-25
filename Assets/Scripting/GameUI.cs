using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script quản lý UI hiển thị số lần nhảy và đánh dấu trên màn hình
/// Gắn script này vào Player hoặc GameObject bất kỳ trong scene
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("UI References (Tự động tạo nếu để trống)")]
    public TextMeshProUGUI jumpText;
    public TextMeshProUGUI markerText;
    
    [Header("UI Settings")]
    public int fontSize = 32;
    public Color textColor = Color.white;
    
    private Canvas uiCanvas;
    private bool isUICreated = false;
    
    // Singleton để các script khác truy cập
    public static GameUI Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern - nếu đã có instance thì destroy cái mới
        if (Instance != null && Instance != this)
        {
            // Destroy cả canvas cũ để tránh trùng UI
            if (Instance.uiCanvas != null)
            {
                Destroy(Instance.uiCanvas.gameObject);
            }
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }
    
    void Start()
    {
        // Tạo UI ngay từ đầu nhưng ẩn
        CreateUI();
    }
    
    void Update()
    {
        // Hiện UI khi game bắt đầu VÀ review xong
        if (!isUICreated) return;
        
        if (uiCanvas != null && !uiCanvas.gameObject.activeSelf)
        {
            // Chờ cả game bắt đầu VÀ timeline review hoàn thành
            if (DifficultyMenu.GameStarted && !TimeLine.IsReviewing)
            {
                uiCanvas.gameObject.SetActive(true);
                Debug.Log("GameUI: Đã hiện UI!");
            }
        }
    }
    
    /// <summary>
    /// Tạo Canvas và các Text UI
    /// </summary>
    private void CreateUI()
    {
        // Tạo Canvas
        GameObject canvasObj = new GameObject("GameUICanvas");
        // Không dùng DontDestroyOnLoad để tránh trùng UI khi reload scene
        
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 999; // Hiện trên tất cả
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Tạo Panel background cho text (bán trong suốt)
        GameObject panelObj = new GameObject("UIPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f); // Đen bán trong suốt
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.85f);
        panelRect.anchorMax = new Vector2(0.25f, 1f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);
        
        // Tạo Text hiển thị số lần nhảy
        jumpText = CreateText(panelObj.transform, "JumpText", "Nhảy: 5/5", new Vector2(0, 0.5f), new Vector2(1, 1));
        
        // Tạo Text hiển thị số dấu hiệu
        markerText = CreateText(panelObj.transform, "MarkerText", "Dấu: 10/10", new Vector2(0, 0), new Vector2(1, 0.5f));
        
        // Ẩn canvas cho đến khi game bắt đầu
        canvasObj.SetActive(false);
        
        isUICreated = true;
        Debug.Log("GameUI: Đã tạo UI thành công!");
    }
    
    /// <summary>
    /// Helper tạo TextMeshProUGUI
    /// </summary>
    private TextMeshProUGUI CreateText(Transform parent, string name, string content, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = textColor;
        tmp.enableWordWrapping = false;
        
        // Thử load font
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            tmp.font = font;
        }
        
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(15, 5);
        rect.offsetMax = new Vector2(-10, -5);
        rect.pivot = new Vector2(0, 0.5f);
        
        return tmp;
    }
    
    /// <summary>
    /// Cập nhật text số lần nhảy
    /// </summary>
    public void UpdateJumpCount(int remaining, int max)
    {
        if (jumpText != null)
        {
            jumpText.text = "Nhảy: " + remaining + "/" + max;
            
            // Đổi màu khi sắp hết
            if (remaining <= 1)
                jumpText.color = Color.red;
            else if (remaining <= 2)
                jumpText.color = Color.yellow;
            else
                jumpText.color = Color.white;
        }
    }
    
    /// <summary>
    /// Cập nhật text số dấu hiệu
    /// </summary>
    public void UpdateMarkerCount(int remaining, int max)
    {
        if (markerText != null)
        {
            markerText.text = "Dấu: " + remaining + "/" + max;
            
            // Đổi màu khi sắp hết
            if (remaining <= 2)
                markerText.color = Color.red;
            else if (remaining <= 4)
                markerText.color = Color.yellow;
            else
                markerText.color = Color.white;
        }
    }

    void OnDestroy()
    {
        // Reset singleton khi bị destroy
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
