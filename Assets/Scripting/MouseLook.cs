using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Độ nhạy chuột")]
    public float mouseSensitivity = 100f;
    
    [Header("Độ nhạy Joystick Mobile")]
    public float joystickSensitivity = 150f;

    [Header("Gán vào")]
    public Transform playerBody;     // Kéo "PlayerPrefab" (chính nó) vào đây
    public Transform playerCamera; // Kéo "Camera" con vào đây

    private float xRotation = 0f; // Biến lưu độ xoay lên/xuống
    private bool useMobileControls = false;

    void Start()
    {
        // Kiểm tra xem có dùng mobile controls không
        #if UNITY_ANDROID || UNITY_IOS
        useMobileControls = true;
        #endif
        
        #if UNITY_EDITOR
        // Trong Editor, kiểm tra MobileControlsManager
        var mobileManager = FindObjectOfType<MobileControlsManager>();
        if (mobileManager != null && mobileManager.forceShowInEditor)
        {
            useMobileControls = true;
        }
        #endif
        
        // Chỉ khóa chuột khi KHÔNG dùng mobile controls
        if (!useMobileControls)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        float lookX = 0f;
        float lookY = 0f;
        
        // 1. Lấy input từ chuột (PC)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        lookX = mouseX;
        lookY = mouseY;
        
        // 2. Lấy input từ joystick mobile (nếu có)
        if (MobileJoystick.LookJoystick != null)
        {
            Vector2 joystickInput = MobileJoystick.LookJoystick.InputVector;
            if (joystickInput.magnitude > 0.1f)
            {
                lookX = joystickInput.x * joystickSensitivity * Time.deltaTime;
                lookY = joystickInput.y * joystickSensitivity * Time.deltaTime;
            }
        }

        // 3. Xoay Lên/Xuống (cho Camera)
        // Chúng ta trừ đi lookY vì kéo lên là nhìn lên (trục X âm)
        xRotation -= lookY;

        // Kẹp góc nhìn, không cho lộn ngược (chỉ nhìn từ -90 đến 90 độ)
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Áp dụng xoay Lên/Xuống cho CHỈ Camera
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 4. Xoay Trái/Phải (cho cả Player Body)
        // Áp dụng xoay Trái/Phải cho CẢ NGƯỜI NHÂN VẬT
        playerBody.Rotate(Vector3.up * lookX);
    }
}