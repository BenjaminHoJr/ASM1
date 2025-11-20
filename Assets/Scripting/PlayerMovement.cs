using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Gán vào (Assign)")]
    public Transform cameraTransform;

    [Header("Tốc độ Di chuyển")]
    public float runSpeed = 8f;
    public float walkSpeed = 4f;
    public float crouchSpeed = 2f;

    [Header("Nhảy & Trọng lực")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Cúi (Crouch)")]
    public float crouchHeight = 1f;
    private float standingHeight;
    private bool isCrouching = false;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float currentSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        currentSpeed = runSpeed;
    }

    void Update()
    {
        // --- 1. KIỂM TRA MẶT ĐẤT ---
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // --- 2. LẤY INPUT DI CHUYỂN (A/D, W/S) ---
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = Vector3.ClampMagnitude(move, 1f);

        // --- 3. XỬ LÝ TRẠNG THÁI (Cúi, Đi bộ) ---
        // (Logic Cúi và Đi bộ giữ nguyên... không thay đổi)
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            controller.height = crouchHeight;
            currentSpeed = crouchSpeed;
            cameraTransform.localPosition = new Vector3(0, crouchHeight * 0.45f, 0);
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            controller.height = standingHeight;
            currentSpeed = runSpeed;
            cameraTransform.localPosition = new Vector3(0, 0.7f, 0);
        }
        if (!isCrouching)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                currentSpeed = walkSpeed;
            else
                currentSpeed = runSpeed;
        }

        // --- 4. ÁP DỤNG DI CHUYỂN NGANG (do Player) ---
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- 5. XỬ LÝ NHẢY (do Player) ---
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        // --- 6. ÁP DỤNG TRỌNG LỰC ---
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}