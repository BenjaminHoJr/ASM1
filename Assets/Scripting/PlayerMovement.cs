using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Gán vào (Assign)")]
    public Transform cameraTransform;
    
    [Header("UI")]
    [Tooltip("Text hiển thị số lần nhảy còn lại. Nếu để trống sẽ tự động tạo")]
    public TextMeshProUGUI jumpCountText;

    [Header("Tốc độ Di chuyển")]
    public float runSpeed = 8f;
    public float walkSpeed = 4f;
    public float crouchSpeed = 2f;

    [Header("Nhảy & Trọng lực")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Giới hạn Nhảy")]
    [Tooltip("Số lần nhảy tối đa. Nếu resetJumpsOnGround = true thì giới hạn cho mỗi lần trên không; nếu false thì là tổng cho cả game.")]
    public int maxJumps = 5;
    private int jumpCount = 0;

    [Tooltip("Nếu true -> reset jumpCount khi chạm đất. Nếu false -> tổng số lần nhảy không reset.")]
    public bool resetJumpsOnGround = false; // đặt false nếu bạn muốn chỉ được nhảy 5 lần tổng cộng

    [Header("Cúi (Crouch)")]
    public float crouchHeight = 1f;
    private float standingHeight;
    private bool isCrouching = false;

    private CharacterController controller;
    private NetworkObject networkObject;
    private PlayerInput playerInput;
    private MobileJoystick movementJoystickCache;
    private Vector3 verticalVelocity;
    private float currentSpeed;
    private bool localSetupDone;
    
    private bool gameStarted = false; // Game chỉ bắt đầu khi chọn độ khó
    private GameObject jumpCanvasObj; // Canvas chứa UI jump count

    /// <summary>
    /// Tự động tạo UI hiển thị số lần nhảy còn lại
    /// </summary>
    private void CreateJumpCountUI()
    {
        // Tạo Canvas mới riêng cho jump count
        GameObject canvasObj = new GameObject("JumpCountCanvas");
        jumpCanvasObj = canvasObj;
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Đảm bảo hiện trên cùng
        
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Ẩn canvas ban đầu - chỉ hiện khi game bắt đầu
        canvasObj.SetActive(false);
        
        // Tạo Text cho JumpCount
        GameObject textObj = new GameObject("JumpCountText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        jumpCountText = textObj.AddComponent<TextMeshProUGUI>();
        
        // Load font TMP mặc định
        TMP_FontAsset tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (tmpFont == null)
        {
            tmpFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Length > 0 
                ? Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0] 
                : null;
        }
        
        if (tmpFont != null)
        {
            jumpCountText.font = tmpFont;
        }
        
        jumpCountText.text = "Nhảy: " + maxJumps + "/" + maxJumps;
        jumpCountText.fontSize = 36;
        jumpCountText.fontStyle = FontStyles.Bold;
        jumpCountText.alignment = TextAlignmentOptions.TopLeft;
        jumpCountText.color = Color.white;
        
        // Đặt vị trí góc trên bên trái
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.9f);
        textRect.anchorMax = new Vector2(0.3f, 1f);
        textRect.pivot = new Vector2(0, 1);
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(0, -10);

        Debug.Log("Đã tự động tạo UI JumpCountText!");
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        networkObject = GetComponent<NetworkObject>();
        playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null)
        {
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
            {
                cameraTransform = childCam.transform;
            }
        }

        standingHeight = controller.height;
        currentSpeed = runSpeed;
    }

    private void EnsureLocalSetupIfNeeded()
    {
        if (localSetupDone) return;
        if (!HasControlAuthority())
        {
            if (playerInput != null && playerInput.enabled)
            {
                playerInput.enabled = false;
            }

            if (cameraTransform != null)
            {
                cameraTransform.gameObject.SetActive(false);
            }
            return;
        }

        if (playerInput != null && !playerInput.enabled)
        {
            playerInput.enabled = true;
        }

        if (cameraTransform != null && !cameraTransform.gameObject.activeSelf)
        {
            cameraTransform.gameObject.SetActive(true);
        }
        
        // Tự động tạo GameUI nếu chưa có
        if (GameUI.Instance == null)
        {
            GameObject uiManager = new GameObject("GameUIManager");
            uiManager.AddComponent<GameUI>();
            Debug.Log("PlayerMovement: Đã tạo GameUI!");
        }
        
        // Tự động tạo Mobile Controls nếu chưa có
        if (MobileControlsManager.Instance == null)
        {
            GameObject mobileManager = new GameObject("MobileControlsManager");
            mobileManager.AddComponent<MobileControlsManager>();
            Debug.Log("PlayerMovement: Đã tạo MobileControlsManager!");
        }
        
        // Không tạo UI riêng nữa - sử dụng GameUI
        // Không gọi UpdateJumpUI ở đây - chờ game bắt đầu
        localSetupDone = true;
    }

    void UpdateJumpUI()
    {
        if (!HasControlAuthority()) return;

        int remainingJumps = maxJumps - jumpCount;
        
        // Cập nhật qua GameUI singleton
        if (GameUI.Instance != null)
        {
            GameUI.Instance.UpdateJumpCount(remainingJumps, maxJumps);
        }
    }

    void Update()
    {
        EnsureLocalSetupIfNeeded();

        if (!HasControlAuthority())
        {
            return;
        }

        // Neu dang pause chu dong thi khoa gameplay.
        if (IsPausedByController())
        {
            return;
        }

        // Kiểm tra nếu game đã bắt đầu (sử dụng biến static từ DifficultyMenu)
        if (!gameStarted)
        {
            // Kiểm tra xem DifficultyMenu đã đánh dấu game bắt đầu 
            // VÀ timeline review đã hoàn thành chưa
            if (CanStartGameplayNow())
            {
                // Game đã bắt đầu và review xong
                gameStarted = true;
                UpdateJumpUI();
                Debug.Log("PlayerMovement: Game bắt đầu!");
            }
            return; // Không xử lý input khi chưa bắt đầu game hoặc đang review
        }

        // Failsafe: tranh bi ket cung khi timeScale = 0 do state lech.
        if (Time.timeScale <= 0.0001f && !TimeLine.IsReviewing)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        float dt = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
        
        // --- 1. KIỂM TRA MẶT ĐẤT ---
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
            // reset bộ đếm nhảy khi chạm đất chỉ nếu tùy chọn bật
            if (resetJumpsOnGround)
            {
                jumpCount = 0;
                UpdateJumpUI();
            }
        }

        // --- 2. LẤY INPUT DI CHUYỂN (A/D, W/S hoặc Joystick Mobile) ---
        Vector2 moveInput = GetMovementInput();
        float moveX = moveInput.x;
        float moveZ = moveInput.y;
        
        // Thêm input từ mobile joystick
        MobileJoystick movementJoystick = GetMovementJoystick();
        if (movementJoystick != null)
        {
            Vector2 joystickInput = movementJoystick.InputVector;
            if (joystickInput.magnitude > 0.02f)
            {
                moveX = joystickInput.x;
                moveZ = joystickInput.y;
            }
        }
        
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = Vector3.ClampMagnitude(move, 1f);

        // --- 3. XỬ LÝ TRẠNG THÁI (Cúi, Đi bộ) ---
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

        // --- 5. XỬ LÝ NHẢY (do Player hoặc nút mobile) ---
        bool jumpInput = Input.GetButtonDown("Jump") || MobileJumpButton.JumpPressed;
        if (jumpInput)
        {
            if (resetJumpsOnGround)
            {
                // Cho phép nhảy nếu đang grounded hoặc chưa vượt quá maxJumps (trong một lần không)
                if (isGrounded || jumpCount < maxJumps)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    jumpCount++;
                    UpdateJumpUI();
                }
            }
            else
            {
                // Giới hạn tổng số lần nhảy (không reset khi grounded)
                if (jumpCount < maxJumps)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    jumpCount++;
                    UpdateJumpUI();
                }
                else
                {
                    // Không còn lượt nhảy
                }
            }
        }

        // --- 6. ÁP DỤNG TRỌNG LỰC + DI CHUYỂN TỔNG HỢP ---
        verticalVelocity.y += gravity * dt;

        Vector3 totalVelocity = move * currentSpeed;
        totalVelocity.y = verticalVelocity.y;
        controller.Move(totalVelocity * dt);
    }

    private bool CanStartGameplayNow()
    {
        // Luong binh thuong.
        if (DifficultyMenu.GameStarted && !TimeLine.IsReviewing)
        {
            return true;
        }

        // Failsafe cho Shared mode: tranh bi ket vi static GameStarted local chua cap nhat.
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.IsRunning && HasControlAuthority() && !TimeLine.IsReviewing)
        {
            // Neu nguoi choi da co input, cho phep vao game ngay.
            if (HasAnyGameplayInput())
            {
                return true;
            }

            // Hoac sau mot khoang tre ngan de tranh deadlock khi vao room.
            if (Time.timeSinceLevelLoad > 1.0f)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyGameplayInput()
    {
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f)
        {
            return true;
        }

        if (MobileJoystick.MovementJoystick != null && MobileJoystick.MovementJoystick.InputVector.magnitude > 0.1f)
        {
            return true;
        }

        return Input.GetButtonDown("Jump") || MobileJumpButton.JumpPressed;
    }

    private MobileJoystick GetMovementJoystick()
    {
        if (MobileJoystick.MovementJoystick != null)
        {
            movementJoystickCache = MobileJoystick.MovementJoystick;
            return movementJoystickCache;
        }

        if (movementJoystickCache != null)
        {
            return movementJoystickCache;
        }

        MobileJoystick[] allJoysticks = FindObjectsByType<MobileJoystick>(FindObjectsSortMode.None);
        for (int i = 0; i < allJoysticks.Length; i++)
        {
            if (allJoysticks[i] != null && allJoysticks[i].joystickType == MobileJoystick.JoystickType.Movement)
            {
                movementJoystickCache = allJoysticks[i];
                return movementJoystickCache;
            }
        }

        return null;
    }

    private Vector2 GetMovementInput()
    {
        // Legacy Input Manager path.
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // New Input System keyboard fallback (when GetAxis returns zero).
        if (Mathf.Abs(moveX) < 0.01f && Mathf.Abs(moveZ) < 0.01f && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ += 1f;
        }

        return new Vector2(Mathf.Clamp(moveX, -1f, 1f), Mathf.Clamp(moveZ, -1f, 1f));
    }

    private bool IsPausedByController()
    {
        PasueController pauseController = FindFirstObjectByType<PasueController>();
        return pauseController != null && pauseController.IsPaused();
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

        // Shared mode thuong dieu khien theo StateAuthority, nhung fallback theo PlayerInput local.
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