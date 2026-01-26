using UnityEngine;

/// <summary>
/// Script để xử lý vấn đề Audio Listener trùng lặp
/// Gắn vào bất kỳ object nào trong scene
/// </summary>
public class AudioListenerManager : MonoBehaviour
{
    void Awake()
    {
        FixDuplicateAudioListeners();
    }
    
    void Start()
    {
        // Chạy lại sau Start để đảm bảo xử lý các object được spawn
        Invoke("FixDuplicateAudioListeners", 0.5f);
    }
    
    /// <summary>
    /// Tìm và xóa các Audio Listener thừa, chỉ giữ lại 1 cái trên Main Camera
    /// </summary>
    public void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length <= 1)
        {
            return; // Không có vấn đề
        }
        
        Debug.Log("Tìm thấy " + listeners.Length + " Audio Listeners. Đang xóa thừa...");
        
        // Ưu tiên giữ AudioListener trên Camera có tag MainCamera hoặc Camera chính
        AudioListener keepListener = null;
        
        // Tìm listener trên Main Camera trước
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            AudioListener mainCamListener = mainCam.GetComponent<AudioListener>();
            if (mainCamListener != null)
            {
                keepListener = mainCamListener;
            }
        }
        
        // Nếu không có trên main camera, giữ cái đầu tiên tìm được
        if (keepListener == null && listeners.Length > 0)
        {
            keepListener = listeners[0];
        }
        
        // Xóa các listener khác
        foreach (AudioListener listener in listeners)
        {
            if (listener != keepListener)
            {
                Debug.Log("Xóa Audio Listener thừa trên: " + listener.gameObject.name);
                Destroy(listener);
            }
        }
        
        if (keepListener != null)
        {
            Debug.Log("Giữ lại Audio Listener trên: " + keepListener.gameObject.name);
        }
    }
}
