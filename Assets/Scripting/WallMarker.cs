using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Cho phép player đặt dấu hiệu lên tường maze để đánh dấu đường đi
/// Tối đa 10 dấu hiệu
/// </summary>
public class WallMarker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Key placeMarkerKey = Key.F; // Phím đặt dấu hiệu
    [SerializeField] private Key removeLastMarkerKey = Key.G; // Phím xóa dấu hiệu cuối cùng
    [SerializeField] private int maxMarkers = 10; // Số dấu hiệu tối đa
    [SerializeField] private float placeDistance = 20f; // Khoảng cách tối đa để đặt dấu hiệu
    [SerializeField] private float markerOffset = 0.01f; // Offset từ tường để tránh z-fighting
    
    [Header("Marker Appearance")]
    [SerializeField] private Color markerColor = Color.red; // Màu dấu hiệu
    [SerializeField] private float markerSize = 0.3f; // Kích thước dấu hiệu
    [SerializeField] private Sprite markerSprite; // Sprite tùy chỉnh (nếu có)
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI markerCountText; // Text hiển thị số dấu hiệu còn lại
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform; // Camera của player
    [SerializeField] private LayerMask wallLayer; // Layer của tường (nếu muốn chỉ định cụ thể)
    
    private List<GameObject> placedMarkers = new List<GameObject>();
    private int markersRemaining;
    private bool gameStarted = false; // Game chỉ bắt đầu khi chọn độ khó
    private GameObject markerCanvasObj; // Canvas chứa UI marker
    
    private UnityEngine.UI.Text markerCountTextLegacy; // Backup nếu TMP không hoạt động
    
    /// <summary>
    /// Tự động tạo UI hiển thị số dấu hiệu còn lại
    /// </summary>
    private void CreateMarkerCountUI()
    {
        // Tạo Canvas mới riêng cho marker
        GameObject canvasObj = new GameObject("MarkerCanvas");
        markerCanvasObj = canvasObj;
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Đảm bảo hiện trên cùng
        
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Ẩn canvas ban đầu - chỉ hiện khi game bắt đầu
        canvasObj.SetActive(false);
        
        Debug.Log("Đã tạo Canvas mới cho MarkerCountText (ẩn cho đến khi game bắt đầu)!");

        // Tạo Text cho MarkerCount
        GameObject textObj = new GameObject("MarkerCountText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        // Thử dùng TextMeshPro trước
        markerCountText = textObj.AddComponent<TextMeshProUGUI>();
        
        // Load font TMP mặc định
        TMP_FontAsset tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (tmpFont == null)
        {
            // Tìm font TMP có sẵn trong project
            tmpFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Length > 0 
                ? Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0] 
                : null;
        }
        
        if (tmpFont != null)
        {
            markerCountText.font = tmpFont;
        }
        
        markerCountText.text = "Markers: " + maxMarkers + "/" + maxMarkers;
        markerCountText.fontSize = 36;
        markerCountText.fontStyle = FontStyles.Bold;
        markerCountText.alignment = TextAlignmentOptions.TopRight;
        markerCountText.color = Color.white;
        
        // Đặt vị trí góc trên bên phải (stretch để text nằm đúng góc)
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.7f, 0.9f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(0, 0);
        textRect.offsetMax = new Vector2(-20, -10);

        Debug.Log("Đã tự động tạo UI MarkerCountText!");
    }
    
    void Start()
    {
        markersRemaining = maxMarkers;
        
        // Tự động tìm camera nếu chưa gán
        if (cameraTransform == null)
        {
            // Cách 1: Tìm Camera.main
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
            else
            {
                // Cách 2: Tìm camera là con của Player
                Camera childCam = GetComponentInChildren<Camera>();
                if (childCam != null)
                {
                    cameraTransform = childCam.transform;
                }
                else
                {
                    // Cách 3: Tìm bất kỳ camera nào trong scene
                    Camera anyCam = FindFirstObjectByType<Camera>();
                    if (anyCam != null)
                    {
                        cameraTransform = anyCam.transform;
                    }
                    else
                    {
                        Debug.LogWarning("WallMarker: Không tìm thấy Camera nào! Hãy gán cameraTransform.");
                    }
                }
            }
            
            if (cameraTransform != null)
            {
                Debug.Log("WallMarker: Đã tự động tìm thấy camera: " + cameraTransform.name);
            }
        }
        
        // Không tạo UI riêng nữa - sử dụng GameUI
        // UI sẽ được quản lý bởi GameUI singleton
        
        // Nếu không chỉ định wallLayer, sử dụng tất cả layer
        if (wallLayer == 0)
        {
            wallLayer = ~0; // Tất cả layers
        }
        
        // Không gọi UpdateUI ở đây - chờ game bắt đầu
    }
    
    void Update()
    {
        // Kiểm tra nếu game đã bắt đầu (sử dụng biến static từ DifficultyMenu)
        if (!gameStarted)
        {
            // Kiểm tra xem DifficultyMenu đã đánh dấu game bắt đầu
            // VÀ timeline review đã hoàn thành chưa
            if (DifficultyMenu.GameStarted && !TimeLine.IsReviewing)
            {
                // Game đã bắt đầu và review xong
                gameStarted = true;
                UpdateUI();
                Debug.Log("WallMarker: Game bắt đầu!");
            }
            return; // Không xử lý input khi chưa bắt đầu game
        }
        
        // Nhấn phím để đặt dấu hiệu
        if (Keyboard.current[placeMarkerKey].wasPressedThisFrame)
        {
            TryPlaceMarker();
        }
        
        // Nhấn phím để xóa dấu hiệu cuối cùng
        if (Keyboard.current[removeLastMarkerKey].wasPressedThisFrame)
        {
            RemoveLastMarker();
        }
    }
    
    /// <summary>
    /// Thử đặt dấu hiệu lên tường
    /// </summary>
    public void TryPlaceMarker()
    {
        if (markersRemaining <= 0)
        {
            Debug.Log("Đã hết dấu hiệu! Bạn đã sử dụng hết " + maxMarkers + " dấu hiệu.");
            return;
        }
        
        // Tìm camera từ PlayerMovement trước
        Transform camTransform = null;
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null && player.cameraTransform != null)
        {
            camTransform = player.cameraTransform;
        }
        
        // Nếu không có, tìm camera thường
        Camera cam = null;
        if (camTransform == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
            }
            if (cam != null)
            {
                camTransform = cam.transform;
            }
        }
        
        if (camTransform == null)
        {
            Debug.LogWarning("WallMarker: Không tìm thấy camera!");
            return;
        }
        
        // Raycast từ camera theo hướng camera đang nhìn
        Ray ray = new Ray(camTransform.position, camTransform.forward);
        RaycastHit hit;
        
        // Debug: vẽ ray trong Scene view
        Debug.DrawRay(ray.origin, ray.direction * placeDistance, Color.red, 2f);
        Debug.Log("Đang raycast từ: " + camTransform.name + ", hướng: " + camTransform.forward);
        
        // Thử raycast với tất cả layers
        bool hitSomething = Physics.Raycast(ray, out hit, placeDistance);
        
        if (hitSomething)
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name + " tại khoảng cách " + hit.distance + "m");
            
            // Kiểm tra xem đã có dấu hiệu ở vị trí này chưa
            foreach (GameObject marker in placedMarkers)
            {
                if (marker != null && Vector3.Distance(marker.transform.position, hit.point) < markerSize * 0.5f)
                {
                    Debug.Log("Đã có dấu hiệu ở vị trí này!");
                    return;
                }
            }
            
            // Tạo dấu hiệu
            CreateMarker(hit.point, hit.normal);
            markersRemaining--;
            UpdateUI();
            
            Debug.Log("Đã đặt dấu hiệu lên " + hit.collider.gameObject.name + "! Còn lại: " + markersRemaining + "/" + maxMarkers);
        }
        else
        {
            Debug.Log("Không tìm thấy vật thể nào trong phạm vi " + placeDistance + "m! Hãy nhìn vào tường và nhấn F.");
        }
    }
    
    /// <summary>
    /// Tạo dấu hiệu tại vị trí chỉ định
    /// </summary>
    private void CreateMarker(Vector3 position, Vector3 normal)
    {
        GameObject marker;
        
        if (markerSprite != null)
        {
            // Sử dụng sprite tùy chỉnh
            marker = new GameObject("WallMarker_" + (maxMarkers - markersRemaining + 1));
            SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
            sr.sprite = markerSprite;
            sr.color = markerColor;
            marker.transform.localScale = Vector3.one * markerSize;
        }
        else
        {
            // Tạo hình tròn đơn giản bằng Quad
            marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "WallMarker_" + (maxMarkers - markersRemaining + 1);
            
            // Xóa collider để không ảnh hưởng gameplay
            Collider col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            // Tạo material màu
            Renderer renderer = marker.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = markerColor;
            renderer.material = mat;
            
            marker.transform.localScale = Vector3.one * markerSize;
        }
        
        // Đặt vị trí (offset một chút từ tường)
        marker.transform.position = position + normal * markerOffset;
        
        // Xoay dấu hiệu để hướng ra ngoài tường
        marker.transform.rotation = Quaternion.LookRotation(-normal);
        
        // Thêm vào danh sách
        placedMarkers.Add(marker);
    }
    
    /// <summary>
    /// Xóa dấu hiệu cuối cùng được đặt
    /// </summary>
    public void RemoveLastMarker()
    {
        if (placedMarkers.Count > 0)
        {
            GameObject lastMarker = placedMarkers[placedMarkers.Count - 1];
            placedMarkers.RemoveAt(placedMarkers.Count - 1);
            
            if (lastMarker != null)
            {
                Destroy(lastMarker);
            }
            
            markersRemaining++;
            UpdateUI();
            
            Debug.Log("Đã xóa dấu hiệu! Còn lại: " + markersRemaining + "/" + maxMarkers);
        }
        else
        {
            Debug.Log("Không có dấu hiệu nào để xóa!");
        }
    }
    
    /// <summary>
    /// Xóa tất cả dấu hiệu
    /// </summary>
    public void RemoveAllMarkers()
    {
        foreach (GameObject marker in placedMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        placedMarkers.Clear();
        markersRemaining = maxMarkers;
        UpdateUI();
        
        Debug.Log("Đã xóa tất cả dấu hiệu!");
    }
    
    /// <summary>
    /// Cập nhật UI hiển thị số dấu hiệu còn lại
    /// </summary>
    private void UpdateUI()
    {
        // Cập nhật qua GameUI singleton
        if (GameUI.Instance != null)
        {
            GameUI.Instance.UpdateMarkerCount(markersRemaining, maxMarkers);
        }
    }
    
    /// <summary>
    /// Lấy số dấu hiệu còn lại
    /// </summary>
    public int GetMarkersRemaining()
    {
        return markersRemaining;
    }
    
    /// <summary>
    /// Lấy số dấu hiệu đã đặt
    /// </summary>
    public int GetMarkersPlaced()
    {
        return placedMarkers.Count;
    }
    
    /// <summary>
    /// Thêm dấu hiệu (dùng cho power-up hoặc bonus)
    /// </summary>
    public void AddMarkers(int amount)
    {
        markersRemaining += amount;
        UpdateUI();
        Debug.Log("Đã nhận thêm " + amount + " dấu hiệu! Tổng: " + markersRemaining);
    }
}
