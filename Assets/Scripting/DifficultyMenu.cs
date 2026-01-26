using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu chọn độ khó trước khi bắt đầu game
/// </summary>
public class DifficultyMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MazeGenerator mazeGenerator;
    
    [Header("UI Settings")]
    [SerializeField] private string titleText = "CHỌN ĐỘ KHÓ";
    [SerializeField] private Color easyColor = new Color(0.2f, 0.8f, 0.2f); // Xanh lá
    [SerializeField] private Color normalColor = new Color(1f, 0.8f, 0.2f); // Vàng
    [SerializeField] private Color hardColor = new Color(0.9f, 0.2f, 0.2f); // Đỏ
    
    private GameObject menuCanvas;
    private bool menuShown = false;
    
    void Awake()
    {
        // Xóa Audio Listener thừa
        FixAudioListeners();
    }
    
    void Start()
    {
        // Tìm MazeGenerator nếu chưa gán
        if (mazeGenerator == null)
        {
            mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        }
        
        // Xóa Camera riêng trong scene (nếu có) - chỉ giữ camera trong Player
        CleanupExtraCameras();
        
        // Hiện menu chọn độ khó
        ShowDifficultyMenu();
    }
    
    /// <summary>
    /// Xóa các Camera không cần thiết trong scene
    /// </summary>
    private void CleanupExtraCameras()
    {
        // Tìm camera tên "Camera" riêng lẻ trong scene (không phải con của Player)
        GameObject standaloneCamera = GameObject.Find("Camera");
        if (standaloneCamera != null)
        {
            Transform parent = standaloneCamera.transform.parent;
            // Nếu camera không có parent hoặc parent không phải Player thì disable
            if (parent == null || !parent.name.Contains("Player"))
            {
                Camera cam = standaloneCamera.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.enabled = false;
                    Debug.Log("Đã disable Camera riêng lẻ: " + standaloneCamera.name);
                }
            }
        }
    }
    
    /// <summary>
    /// Xóa Audio Listener thừa
    /// </summary>
    private void FixAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length <= 1) return;
        
        Debug.Log("Tìm thấy " + listeners.Length + " Audio Listeners. Đang xóa thừa...");
        
        // Giữ lại listener đầu tiên, xóa các cái khác
        for (int i = 1; i < listeners.Length; i++)
        {
            Debug.Log("Xóa Audio Listener thừa trên: " + listeners[i].gameObject.name);
            Destroy(listeners[i]);
        }
    }
    
    void Update()
    {
        // Cho phép chọn bằng phím số khi menu đang hiện
        if (menuShown && menuCanvas != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                SelectDifficulty(MazeGenerator.Difficulty.Easy);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                SelectDifficulty(MazeGenerator.Difficulty.Normal);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                SelectDifficulty(MazeGenerator.Difficulty.Hard);
            }
        }
    }
    
    /// <summary>
    /// Hiển thị menu chọn độ khó
    /// </summary>
    public void ShowDifficultyMenu()
    {
        if (menuShown) return;
        menuShown = true;
        
        // KHÔNG dừng time để button có thể click được
        // Time.timeScale = 0f;
        
        // Tạo Canvas
        menuCanvas = new GameObject("DifficultyMenuCanvas");
        Canvas canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Hiện trên tất cả
        
        CanvasScaler scaler = menuCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        menuCanvas.AddComponent<GraphicRaycaster>();
        
        // Tạo Panel background
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(menuCanvas.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Tạo Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = titleText;
        titleTMP.fontSize = 72;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = Vector2.zero;
        
        // Tạo các nút chọn độ khó
        CreateDifficultyButton(panel.transform, "DỄ\n(Maze 8x8)", MazeGenerator.Difficulty.Easy, easyColor, 150);
        CreateDifficultyButton(panel.transform, "TRUNG BÌNH\n(Maze 15x15)", MazeGenerator.Difficulty.Normal, normalColor, 0);
        CreateDifficultyButton(panel.transform, "KHÓ\n(Maze 25x25)", MazeGenerator.Difficulty.Hard, hardColor, -150);
        
        // Tạo hướng dẫn
        GameObject instructionObj = new GameObject("Instruction");
        instructionObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI instructionTMP = instructionObj.AddComponent<TextMeshProUGUI>();
        instructionTMP.text = "Sử dụng WASD để di chuyển. Tìm lối ra!\nGiới hạn nhảy: 5 lần (Space). Đánh dấu tường: F | Xóa dấu: G";
        instructionTMP.fontSize = 24;
        instructionTMP.alignment = TextAlignmentOptions.Center;
        instructionTMP.color = new Color(0.8f, 0.8f, 0.8f);
        instructionTMP.enableWordWrapping = true;
        
        RectTransform instrRect = instructionObj.GetComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.1f, 0.08f);
        instrRect.anchorMax = new Vector2(0.9f, 0.22f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;
        
        // Thêm hướng dẫn phím tắt
        GameObject keyHintObj = new GameObject("KeyHint");
        keyHintObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI keyHintTMP = keyHintObj.AddComponent<TextMeshProUGUI>();
        keyHintTMP.text = "Nhấn phím 1, 2, hoặc 3 để chọn - hoặc click chuột";
        keyHintTMP.fontSize = 22;
        keyHintTMP.alignment = TextAlignmentOptions.Center;
        keyHintTMP.color = Color.yellow;
        
        RectTransform keyHintRect = keyHintObj.GetComponent<RectTransform>();
        keyHintRect.anchorMin = new Vector2(0.1f, 0.02f);
        keyHintRect.anchorMax = new Vector2(0.9f, 0.08f);
        keyHintRect.offsetMin = Vector2.zero;
        keyHintRect.offsetMax = Vector2.zero;
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Menu chọn độ khó đã hiện! Nhấn 1, 2, hoặc 3 để chọn.");
    }
    
    /// <summary>
    /// Tạo nút chọn độ khó
    /// </summary>
    private void CreateDifficultyButton(Transform parent, string text, MazeGenerator.Difficulty difficulty, Color color, float yOffset)
    {
        GameObject buttonObj = new GameObject("Button_" + difficulty.ToString());
        buttonObj.transform.SetParent(parent, false);
        
        // Button Image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        
        // Button Component
        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;
        
        // Button click event
        button.onClick.AddListener(() => SelectDifficulty(difficulty));
        
        // Button size and position
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(350, 100);
        buttonRect.anchoredPosition = new Vector2(0, yOffset);
        
        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 36;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    /// <summary>
    /// Chọn độ khó và bắt đầu game
    /// </summary>
    public void SelectDifficulty(MazeGenerator.Difficulty difficulty)
    {
        Debug.Log("Đã chọn độ khó: " + difficulty.ToString());
        
        // Ẩn menu trước
        if (menuCanvas != null)
        {
            Destroy(menuCanvas);
            menuCanvas = null;
        }
        
        // Resume game TRƯỚC khi generate
        Time.timeScale = 1f;
        menuShown = false;
        
        // Lock cursor cho gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Tìm lại MazeGenerator nếu cần
        if (mazeGenerator == null)
        {
            mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        }
        
        // Set difficulty và Generate maze
        if (mazeGenerator != null)
        {
            mazeGenerator.SetDifficulty(difficulty);
            mazeGenerator.Generate();
            Debug.Log("Bắt đầu game với độ khó: " + difficulty.ToString());
        }
        else
        {
            Debug.LogError("Không tìm thấy MazeGenerator trong scene!");
        }
    }
}
