using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

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
    private NetworkObject networkObject;
    private PlayerInput playerInput;
    private bool localLookSetupDone;

    void Start()
    {
        networkObject = GetComponent<NetworkObject>();
        playerInput = GetComponent<PlayerInput>();

        if (playerCamera == null)
        {
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
            {
                playerCamera = childCam.transform;
            }
        }

        if (playerBody == null)
        {
            playerBody = transform;
        }
    }

    void Update()
    {
        EnsureLocalLookSetupIfNeeded();

        if (!HasControlAuthority()) return;

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

    private void EnsureLocalLookSetupIfNeeded()
    {
        if (localLookSetupDone) return;

        if (!HasControlAuthority())
        {
            if (playerInput != null && playerInput.enabled)
            {
                playerInput.enabled = false;
            }

            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }
            return;
        }

        if (playerInput != null && !playerInput.enabled)
        {
            playerInput.enabled = true;
        }

        if (playerCamera != null && !playerCamera.gameObject.activeSelf)
        {
            playerCamera.gameObject.SetActive(true);
        }

        // Kiểm tra xem có dùng mobile controls không
        #if UNITY_ANDROID || UNITY_IOS
        useMobileControls = true;
        #endif
        
        #if UNITY_EDITOR
        // Trong Editor, kiểm tra MobileControlsManager
        var mobileManager = FindFirstObjectByType<MobileControlsManager>();
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

        localLookSetupDone = true;
    }

    private bool HasControlAuthority()
    {
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning)
        {
            return true;
        }

        if (networkObject == null)
        {
            return IsLocalInputUser();
        }

        if (networkObject.HasInputAuthority || networkObject.HasStateAuthority)
        {
            return true;
        }

        return IsLocalInputUser();
    }

    private bool IsLocalInputUser()
    {
        if (playerInput == null)
        {
            return Keyboard.current != null;
        }

        return playerInput.user.valid && playerInput.user.pairedDevices.Count > 0;
    }
}