using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Xử lý nút đặt marker trên mobile
/// </summary>
public class MobilePlaceMarkerButton : MonoBehaviour, IPointerDownHandler
{
    public static bool PlaceMarkerPressed { get; private set; }
    
    private static MobilePlaceMarkerButton instance;
    
    void Awake()
    {
        instance = this;
    }
    
    void LateUpdate()
    {
        // Reset trạng thái mỗi frame (giống GetButtonDown)
        PlaceMarkerPressed = false;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        PlaceMarkerPressed = true;
        
        // Gọi trực tiếp WallMarker để đặt marker
        WallMarker wallMarker = FindFirstObjectByType<WallMarker>();
        if (wallMarker != null)
        {
            wallMarker.TryPlaceMarker();
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
