using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Xử lý nút xóa marker trên mobile
/// </summary>
public class MobileRemoveMarkerButton : MonoBehaviour, IPointerDownHandler
{
    public static bool RemoveMarkerPressed { get; private set; }
    
    private static MobileRemoveMarkerButton instance;
    
    void Awake()
    {
        instance = this;
    }
    
    void LateUpdate()
    {
        // Reset trạng thái mỗi frame (giống GetButtonDown)
        RemoveMarkerPressed = false;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        RemoveMarkerPressed = true;
        
        // Gọi trực tiếp WallMarker để xóa marker cuối cùng
        WallMarker wallMarker = FindFirstObjectByType<WallMarker>();
        if (wallMarker != null)
        {
            wallMarker.RemoveLastMarker();
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
