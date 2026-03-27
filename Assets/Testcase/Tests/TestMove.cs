using System.Collections;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

public class TestMove
{
    private GameObject playerObj;
    private Component playerMovement;
    private GameObject joystickObj;
    private Component joystick;
    private GameObject canvasObj;
    private GameObject cameraObj;
    private GameObject floorObj;
    private GameObject lightObj;

    private Type playerMovementType;
    private Type mobileJoystickType;
    private Type mobileControlsManagerType;
    private Type gameUIType;
    private Type difficultyMenuType;
    private Type exitTriggerType;

    private static Type FindType(string typeName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = asm.GetType(typeName);
            if (t != null)
            {
                return t;
            }
        }

        return null;
    }

    private static Type FindTypeOrAssert(string typeName)
    {
        Type t = FindType(typeName);
        Assert.IsNotNull(t, "Missing type: " + typeName + ". Ensure scripts compiled in Assembly-CSharp.");
        return t;
    }

    private static void DestroySingletonInstance(Type singletonType)
    {
        if (singletonType == null) return;

        PropertyInfo instanceProp = singletonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProp == null) return;

        UnityEngine.Object instance = instanceProp.GetValue(null) as UnityEngine.Object;
        if (instance != null)
        {
            UObject.DestroyImmediate(instance);
        }
    }

    private static void SetDifficultyGameStarted(bool value)
    {
        Type difficultyMenuTypeLocal = FindType("DifficultyMenu");
        if (difficultyMenuTypeLocal == null) return;

        FieldInfo field = difficultyMenuTypeLocal.GetField("<GameStarted>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, value);
        }
    }

    private static void SetMovementJoystick(Component value)
    {
        Type mobileJoystickTypeLocal = FindType("MobileJoystick");
        if (mobileJoystickTypeLocal == null) return;

        FieldInfo field = mobileJoystickTypeLocal.GetField("<MovementJoystick>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, value);
        }
    }

    [SetUp]
    public void Setup()
    {
        Time.timeScale = 1.0f;

        playerMovementType = FindTypeOrAssert("PlayerMovement");
        mobileJoystickType = FindTypeOrAssert("MobileJoystick");
        mobileControlsManagerType = FindTypeOrAssert("MobileControlsManager");
        gameUIType = FindTypeOrAssert("GameUI");
        difficultyMenuType = FindTypeOrAssert("DifficultyMenu");
        exitTriggerType = FindTypeOrAssert("ExitTrigger");

        // Clean up any existing instances from previous runs
        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas) UObject.DestroyImmediate(existingCanvas);
        
        // Destroy existing Singletons to avoid interference
        DestroySingletonInstance(mobileControlsManagerType);
        DestroySingletonInstance(gameUIType);
        
        // Setup scene environment
        cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();

        lightObj = new GameObject("Light");
        lightObj.AddComponent<Light>().type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Floor for physics
        floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorObj.name = "Floor";
        floorObj.transform.position = Vector3.zero;
        floorObj.transform.localScale = new Vector3(100, 1, 100);

        // Canvas for Joystick
        canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Joystick Setup
        joystickObj = new GameObject("Joystick");
        joystickObj.transform.SetParent(canvasObj.transform);
        joystick = joystickObj.AddComponent(mobileJoystickType);
        
        // Initialize Joystick Requirements
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(joystickObj.transform);
        bg.AddComponent<Image>();
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(bg.transform);
        handle.AddComponent<Image>();
        RectTransform handleRect = handle.AddComponent<RectTransform>();

        FieldInfo bgField = mobileJoystickType.GetField("background", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo handleField = mobileJoystickType.GetField("handle", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo joystickTypeField = mobileJoystickType.GetField("joystickType", BindingFlags.Public | BindingFlags.Instance);
        Type joystickEnumType = mobileJoystickType.GetNestedType("JoystickType", BindingFlags.Public);

        if (bgField != null) bgField.SetValue(joystick, bgRect);
        if (handleField != null) handleField.SetValue(joystick, handleRect);
        if (joystickTypeField != null && joystickEnumType != null)
        {
            object movementEnum = Enum.Parse(joystickEnumType, "Movement");
            joystickTypeField.SetValue(joystick, movementEnum);
        }
        
        // Force set static instance immediately (mocking Start execution)
        SetMovementJoystick(joystick);

        // Create Dummy MobileControlsManager to prevent PlayerMovement from creating a conflicting one
        GameObject mcmObj = new GameObject("MobileControlsManager");
        Component mcm = mcmObj.AddComponent(mobileControlsManagerType);
        FieldInfo forceShowInEditorField = mobileControlsManagerType.GetField("forceShowInEditor", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo showOnMobileField = mobileControlsManagerType.GetField("showOnMobile", BindingFlags.Public | BindingFlags.Instance);
        if (forceShowInEditorField != null) forceShowInEditorField.SetValue(mcm, false);
        if (showOnMobileField != null) showOnMobileField.SetValue(mcm, false);

        // Create Dummy GameUI
        GameObject guiObj = new GameObject("GameUIManager");
        guiObj.AddComponent(gameUIType);

        // Player Setup
        playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(0, 1, 0); // Start above floor
        playerObj.AddComponent<CharacterController>();
        playerMovement = playerObj.AddComponent(playerMovementType);

        FieldInfo cameraTransformField = playerMovementType.GetField("cameraTransform", BindingFlags.Public | BindingFlags.Instance);
        if (cameraTransformField != null)
        {
            cameraTransformField.SetValue(playerMovement, cameraObj.transform);
        }
        
        // Game Logic Setup: Force GameStarted = true via reflection to bypass menu
        SetDifficultyGameStarted(true);
    }

    [TearDown]
    public void Teardown()
    {
        Time.timeScale = 1.0f; // Reset time scale
        
        if (playerObj) UObject.DestroyImmediate(playerObj);
        if (joystickObj) UObject.DestroyImmediate(joystickObj);
        if (canvasObj) UObject.DestroyImmediate(canvasObj);
        if (cameraObj) UObject.DestroyImmediate(cameraObj);
        if (floorObj) UObject.DestroyImmediate(floorObj);
        if (lightObj) UObject.DestroyImmediate(lightObj);
        
        DestroySingletonInstance(mobileControlsManagerType);
        DestroySingletonInstance(gameUIType);
        
        // Reset GameStarted State
        SetDifficultyGameStarted(false);
            
        // Reset Joystick Static Reference
        SetMovementJoystick(null);
    }

    // Helper to simulate joystick input by accessing private backing field
    private void SetJoystickInput(Vector2 input)
    {
        // Try setting private field 'inputVector'
        FieldInfo field = mobileJoystickType.GetField("inputVector", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(joystick, input);
        }
        
        // Try setting backing field of property 'InputVector'
        FieldInfo propField = mobileJoystickType.GetField("<InputVector>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (propField != null)
        {
             propField.SetValue(joystick, input);
        }
    }

    // TestCase 1: Move and hit wall
    [UnityTest]
    public IEnumerator Test01_DiChuyenVaChamTuong()
    {
        // Create Wall in front
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.position = new Vector3(0, 1, 2); 
        wall.transform.localScale = new Vector3(5, 5, 1);
        
        yield return new WaitForSecondsRealtime(0.1f); // Physics init

        SetJoystickInput(new Vector2(0, 1)); // Press W (Forward)
        
        // Move for 1.5 seconds
        yield return new WaitForSecondsRealtime(1.5f);
        
        // Check position. Should be blocked.
        // Wall surface at z=1.5. Player radius 0.5. Center at 1.0. Allow small penetration/margin.
        Assert.Less(playerObj.transform.position.z, 1.9f, "Player should be blocked by wall");
        Assert.Greater(playerObj.transform.position.z, 0.5f, "Player should have moved some distance");
        
        UObject.Destroy(wall);
    }

    // TestCase 2: Diagonal Movement
    [UnityTest]
    public IEnumerator Test02_DiChuyenCheo()
    {
        yield return null;
        Vector3 startPos = playerObj.transform.position;
        SetJoystickInput(new Vector2(0.7f, 0.7f).normalized); // W + D
        
        yield return new WaitForSecondsRealtime(1.0f);
        
        Vector3 endPos = playerObj.transform.position;
        Assert.Greater(endPos.x, startPos.x + 0.5f, "Should move in X");
        Assert.Greater(endPos.z, startPos.z + 0.5f, "Should move in Z");
    }

    // TestCase 3: Stop Immediately
    [UnityTest]
    public IEnumerator Test03_DungLai()
    {
        yield return null;
        SetJoystickInput(new Vector2(0, 1)); // Move
        yield return new WaitForSecondsRealtime(0.5f);
        
        Vector3 movingPos = playerObj.transform.position;
        SetJoystickInput(Vector2.zero); // Stop
        yield return new WaitForSecondsRealtime(0.5f); // Inertia wait
        
        Vector3 stopPos = playerObj.transform.position;
        // Allow slight movement for inertia/damping
        Assert.Less(Vector3.Distance(movingPos, stopPos), 0.5f, "Player should stop moving");
    }

    // TestCase 4: Victory / Reach Goal
    [UnityTest]
    public IEnumerator Test04_VeDich()
    {
        // Place Exit Trigger just ahead
        GameObject exitObj = new GameObject("Exit");
        exitObj.transform.position = new Vector3(0, 1, 3);
        BoxCollider collider = exitObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(2, 2, 2);
        
        // Add Exit script
        Component exitTrigger = exitObj.AddComponent(exitTriggerType);
        FieldInfo pauseUiField = exitTriggerType.GetField("pauseAndShowDefaultUI", BindingFlags.Public | BindingFlags.Instance);
        if (pauseUiField != null)
        {
            pauseUiField.SetValue(exitTrigger, false);
        }
        // Note: ExitTrigger logic depends on DifficultyMenu.GameStarted (handled in Setup)
        
        SetJoystickInput(new Vector2(0, 1)); // Move forward
        
        yield return new WaitForSecondsRealtime(2.0f);

        // Check if we are inside trigger area physically
        Bounds bounds = collider.bounds;
        bool inside = bounds.Contains(playerObj.transform.position);
        Assert.IsTrue(inside || playerObj.transform.position.z > 2.0f, "Player should reach exit area");
        
        UObject.Destroy(exitObj);
    }

    // TestCase 5: Move Back (S)
    [UnityTest]
    public IEnumerator Test05_DiLui()
    {
        yield return null;
        Vector3 startPos = playerObj.transform.position;
        SetJoystickInput(new Vector2(0, -1)); // S
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.Less(playerObj.transform.position.z, startPos.z - 0.5f, "Should move backwards");
    }

    // TestCase 6: Move Left (A)
    [UnityTest]
    public IEnumerator Test06_SangTrai()
    {
        yield return null;
        Vector3 startPos = playerObj.transform.position;
        SetJoystickInput(new Vector2(-1, 0)); // A
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.Less(playerObj.transform.position.x, startPos.x - 0.5f, "Should move left");
    }

    // TestCase 7: Move Right (D)
    [UnityTest]
    public IEnumerator Test07_SangPhai()
    {
        yield return null;
        Vector3 startPos = playerObj.transform.position;
        SetJoystickInput(new Vector2(1, 0)); // D
        yield return new WaitForSecondsRealtime(1.0f);
        Assert.Greater(playerObj.transform.position.x, startPos.x + 0.5f, "Should move right");
    }

    // TestCase 8: Change Direction
    [UnityTest]
    public IEnumerator Test08_DoiHuongLienTuc()
    {
        yield return null;
        Vector3 pos0 = playerObj.transform.position;
        
        // W
        SetJoystickInput(new Vector2(0, 1));
        yield return new WaitForSecondsRealtime(0.5f);
        Vector3 pos1 = playerObj.transform.position;
        Assert.Greater(pos1.z, pos0.z);
        
        // S
        SetJoystickInput(new Vector2(0, -1));
        yield return new WaitForSecondsRealtime(0.5f);
        Vector3 pos2 = playerObj.transform.position;
        Assert.Less(pos2.z, pos1.z);
        
        // A
        SetJoystickInput(new Vector2(-1, 0));
        yield return new WaitForSecondsRealtime(0.5f);
        Vector3 pos3 = playerObj.transform.position;
        Assert.Less(pos3.x, pos2.x);
        
        // D
        SetJoystickInput(new Vector2(1, 0));
        yield return new WaitForSecondsRealtime(0.5f);
        Vector3 pos4 = playerObj.transform.position;
        Assert.Greater(pos4.x, pos3.x);
    }

    // TestCase 9: Slide Along Wall
    [UnityTest]
    public IEnumerator Test09_TruotTheoTuong()
    {
        // Wall on the right (x=2), runs along Z
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "SideWall";
        wall.transform.position = new Vector3(2f, 1, 0); 
        wall.transform.localScale = new Vector3(1, 5, 20); 
        
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Move diagonally Forward-Right (into wall)
        SetJoystickInput(new Vector2(0.7f, 0.7f).normalized); 
        
        Vector3 startPos = playerObj.transform.position;
        yield return new WaitForSecondsRealtime(1.5f);
        Vector3 endPos = playerObj.transform.position;
        
        // Should move significant distance Forward (Z)
        Assert.Greater(endPos.z, startPos.z + 1.0f, "Should slide forward along wall");
        
        // Should be blocked in X (approx wall surface: x=1.5)
        Assert.Less(endPos.x, 2.0f, "Should stay on player side of wall");
        
        UObject.Destroy(wall);
    }

    // TestCase 10: Map Boundary
    [UnityTest]
    public IEnumerator Test10_BienBanDo()
    {
        // Simulate boundary at z=10
        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.name = "MapBoundary";
        boundary.transform.position = new Vector3(0, 1, 10);
        boundary.transform.localScale = new Vector3(20, 10, 1);
        
        // Teleport near boundary
        playerObj.transform.position = new Vector3(0, 1, 9);
        yield return null; 
        
        // Move towards boundary
        SetJoystickInput(new Vector2(0, 1));
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        // Check z position blocked
        Assert.Less(playerObj.transform.position.z, 10.5f, "Should be blocked by map boundary");
        
        UObject.Destroy(boundary);
    }
}
