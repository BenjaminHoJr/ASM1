using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Joystick ảo cho điện thoại - dùng để di chuyển hoặc xoay camera
/// </summary>
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Type")]
    public JoystickType joystickType = JoystickType.Movement;
    
    [Header("References")]
    public RectTransform background;  // Vùng nền joystick
    public RectTransform handle;      // Núm điều khiển
    
    [Header("Settings")]
    [Range(0f, 1f)]
    public float handleRange = 0.4f;  // Phạm vi di chuyển của handle (tỷ lệ với background)
    
    // Giá trị input từ -1 đến 1
    public Vector2 InputVector { get; private set; }
    
    // Static instances để các script khác truy cập
    public static MobileJoystick MovementJoystick { get; private set; }
    public static MobileJoystick LookJoystick { get; private set; }
    
    private Vector2 inputVector;
    private Canvas canvas;
    private Camera canvasCamera;
    
    public enum JoystickType
    {
        Movement,   // Joystick di chuyển (trái)
        Look        // Joystick xoay camera (phải)
    }
    
    void Start()
    {
        // Đăng ký static instance trong Start() để đảm bảo joystickType đã được gán
        if (joystickType == JoystickType.Movement)
            MovementJoystick = this;
        else
            LookJoystick = this;
            
        canvas = GetComponentInParent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            canvasCamera = canvas.worldCamera;
            
        // Reset handle position
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;
        
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, 
            eventData.position, 
            canvasCamera, 
            out position
        );
        
        // Tính toán vị trí tương đối
        float backgroundRadius = background.sizeDelta.x / 2f;
        
        // Chuẩn hóa input (-1 đến 1)
        inputVector = position / backgroundRadius;
        
        // Giới hạn magnitude
        if (inputVector.magnitude > 1f)
            inputVector = inputVector.normalized;
        
        InputVector = inputVector;
        
        // Di chuyển handle
        handle.anchoredPosition = inputVector * backgroundRadius * handleRange;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        InputVector = Vector2.zero;
        
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
    
    void OnDestroy()
    {
        if (MovementJoystick == this)
            MovementJoystick = null;
        if (LookJoystick == this)
            LookJoystick = null;
    }
}
