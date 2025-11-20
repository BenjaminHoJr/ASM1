using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Độ nhạy chuột")]
    public float mouseSensitivity = 100f;

    [Header("Gán vào")]
    public Transform playerBody;     // Kéo "PlayerPrefab" (chính nó) vào đây
    public Transform playerCamera; // Kéo "Camera" con vào đây

    private float xRotation = 0f; // Biến lưu độ xoay lên/xuống

    void Start()
    {
        // Khóa con trỏ chuột vào giữa màn hình
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. Lấy input từ chuột
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. Xoay Lên/Xuống (cho Camera)
        // Chúng ta trừ đi mouseY vì kéo chuột lên là nhìn lên (trục X âm)
        xRotation -= mouseY;

        // Kẹp góc nhìn, không cho lộn ngược (chỉ nhìn từ -90 đến 90 độ)
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Áp dụng xoay Lên/Xuống cho CHỈ Camera
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 3. Xoay Trái/Phải (cho cả Player Body)
        // Áp dụng xoay Trái/Phải cho CẢ NGƯỜI NHÂN VẬT
        playerBody.Rotate(Vector3.up * mouseX);
    }
}