using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Xử lý nút nhảy trên mobile
/// </summary>
public class MobileJumpButton : MonoBehaviour, IPointerDownHandler
{
    public static bool JumpPressed { get; private set; }
    
    private static MobileJumpButton instance;
    
    void Awake()
    {
        instance = this;
    }
    
    void LateUpdate()
    {
        // Reset trạng thái nhảy mỗi frame (giống GetButtonDown)
        JumpPressed = false;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        JumpPressed = true;
    }
    
    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
