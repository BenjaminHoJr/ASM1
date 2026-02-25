using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý tạo và hiển thị joystick cho mobile
/// </summary>
public class MobileControlsManager : MonoBehaviour
{
    public static MobileControlsManager Instance { get; private set; }
    
    [Header("Settings")]
    public bool showOnMobile = true;
    public bool forceShowInEditor = true; // Bật để test trong Editor
    
    [Header("Joystick Size")]
    public float joystickSize = 200f;
    public float handleSize = 80f;
    public float margin = 50f;
    
    [Header("Colors")]
    public Color backgroundColor = new Color(1f, 1f, 1f, 0.3f);
    public Color handleColor = new Color(1f, 1f, 1f, 0.8f);
    
    private Canvas mobileCanvas;
    private GameObject movementJoystickObj;
    private GameObject lookJoystickObj;
    private GameObject jumpButtonObj;
    private GameObject placeMarkerButtonObj;
    private GameObject removeMarkerButtonObj;
    
    private bool isInitialized = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Chỉ tạo controls trên mobile hoặc khi force trong editor
        bool shouldCreate = false;
        
        #if UNITY_ANDROID || UNITY_IOS
        shouldCreate = showOnMobile;
        #endif
        
        #if UNITY_EDITOR
        if (forceShowInEditor)
            shouldCreate = true;
        #endif
        
        if (shouldCreate)
        {
            CreateMobileControls();
        }
    }
    
    void CreateMobileControls()
    {
        if (isInitialized) return;
        
        // Tạo Canvas riêng cho mobile controls
        GameObject canvasObj = new GameObject("MobileControlsCanvas");
        canvasObj.transform.SetParent(transform);
        
        mobileCanvas = canvasObj.AddComponent<Canvas>();
        mobileCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mobileCanvas.sortingOrder = 50; // Dưới UI chính nhưng trên game
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Tạo Movement Joystick (bên trái)
        movementJoystickObj = CreateJoystick("MovementJoystick", MobileJoystick.JoystickType.Movement, true);
        
        // Tạo Look Joystick (bên phải)
        lookJoystickObj = CreateJoystick("LookJoystick", MobileJoystick.JoystickType.Look, false);
        
        // Tạo nút nhảy
        jumpButtonObj = CreateJumpButton();
        
        // Tạo nút đặt marker
        placeMarkerButtonObj = CreateMarkerButton("PlaceMarker", "F", new Color(0.8f, 0.2f, 0.2f, 0.7f), true);
        
        // Tạo nút xóa marker
        removeMarkerButtonObj = CreateMarkerButton("RemoveMarker", "G", new Color(0.5f, 0.5f, 0.5f, 0.7f), false);
        
        isInitialized = true;
        Debug.Log("MobileControlsManager: Đã tạo joystick controls!");
    }
    
    GameObject CreateJoystick(string name, MobileJoystick.JoystickType type, bool isLeft)
    {
        // Container
        GameObject joystickObj = new GameObject(name);
        joystickObj.transform.SetParent(mobileCanvas.transform, false);
        
        RectTransform joystickRect = joystickObj.AddComponent<RectTransform>();
        
        // Đặt vị trí
        if (isLeft)
        {
            joystickRect.anchorMin = new Vector2(0, 0);
            joystickRect.anchorMax = new Vector2(0, 0);
            joystickRect.pivot = new Vector2(0, 0);
            joystickRect.anchoredPosition = new Vector2(margin, margin);
        }
        else
        {
            joystickRect.anchorMin = new Vector2(1, 0);
            joystickRect.anchorMax = new Vector2(1, 0);
            joystickRect.pivot = new Vector2(1, 0);
            joystickRect.anchoredPosition = new Vector2(-margin - 100, margin); // Thêm khoảng cách cho nút nhảy
        }
        
        joystickRect.sizeDelta = new Vector2(joystickSize, joystickSize);
        
        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(joystickObj.transform, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(joystickSize, joystickSize);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;
        bgImage.raycastTarget = true;
        
        // Tạo sprite hình tròn cho background
        bgImage.sprite = CreateCircleSprite();
        
        // Handle
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(bgObj.transform, false);
        
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(handleSize, handleSize);
        
        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = handleColor;
        handleImage.raycastTarget = false;
        handleImage.sprite = CreateCircleSprite();
        
        // Thêm MobileJoystick component
        MobileJoystick joystick = bgObj.AddComponent<MobileJoystick>();
        joystick.joystickType = type;
        joystick.background = bgRect;
        joystick.handle = handleRect;
        joystick.handleRange = 0.6f;
        
        return joystickObj;
    }
    
    GameObject CreateJumpButton()
    {
        GameObject buttonObj = new GameObject("JumpButton");
        buttonObj.transform.SetParent(mobileCanvas.transform, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 0);
        buttonRect.anchorMax = new Vector2(1, 0);
        buttonRect.pivot = new Vector2(1, 0);
        buttonRect.anchoredPosition = new Vector2(-margin, margin + joystickSize / 2);
        buttonRect.sizeDelta = new Vector2(90, 90);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.7f);
        buttonImage.sprite = CreateCircleSprite();
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Thêm component xử lý nhảy
        MobileJumpButton jumpButton = buttonObj.AddComponent<MobileJumpButton>();
        
        // Thêm text "JUMP"
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Sử dụng Unity UI Text thay vì TMP để đơn giản
        Text text = textObj.AddComponent<Text>();
        text.text = "JUMP";
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 14;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        
        return buttonObj;
    }
    
    /// <summary>
    /// Tạo nút marker (đặt hoặc xóa)
    /// </summary>
    GameObject CreateMarkerButton(string name, string label, Color color, bool isPlaceMarker)
    {
        GameObject buttonObj = new GameObject(name + "Button");
        buttonObj.transform.SetParent(mobileCanvas.transform, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 0);
        buttonRect.anchorMax = new Vector2(1, 0);
        buttonRect.pivot = new Vector2(1, 0);
        
        // Đặt vị trí: nút đặt marker phía trên nút nhảy, nút xóa marker bên trái nút đặt
        if (isPlaceMarker)
        {
            // Nút đặt marker (F) - phía trên nút nhảy
            buttonRect.anchoredPosition = new Vector2(-margin, margin + joystickSize / 2 + 100);
        }
        else
        {
            // Nút xóa marker (G) - bên trái nút đặt marker
            buttonRect.anchoredPosition = new Vector2(-margin - 100, margin + joystickSize / 2 + 100);
        }
        buttonRect.sizeDelta = new Vector2(80, 80);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        buttonImage.sprite = CreateCircleSprite();
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Thêm component xử lý marker
        if (isPlaceMarker)
        {
            buttonObj.AddComponent<MobilePlaceMarkerButton>();
        }
        else
        {
            buttonObj.AddComponent<MobileRemoveMarkerButton>();
        }
        
        // Thêm text label
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        
        return buttonObj;
    }
    
    /// <summary>
    /// Tạo sprite hình tròn bằng code
    /// </summary>
    Sprite CreateCircleSprite()
    {
        int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution);
        
        float centerX = resolution / 2f;
        float centerY = resolution / 2f;
        float radius = resolution / 2f - 1;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance <= radius)
                {
                    // Làm mờ viền
                    float alpha = 1f;
                    if (distance > radius - 2)
                        alpha = (radius - distance) / 2f;
                    
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Hiện/Ẩn controls
    /// </summary>
    public void SetControlsActive(bool active)
    {
        if (mobileCanvas != null)
            mobileCanvas.gameObject.SetActive(active);
    }
    
    /// <summary>
    /// Kiểm tra xem có đang chạy trên mobile không
    /// </summary>
    public static bool IsMobile()
    {
        #if UNITY_ANDROID || UNITY_IOS
        return true;
        #else
        return false;
        #endif
    }
}
