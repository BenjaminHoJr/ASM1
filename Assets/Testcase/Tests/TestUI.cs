using System.Collections;
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UObject = UnityEngine.Object;

public class TestUI
{
    private GameObject gameUiObj;
    private Component gameUi;
    private Type gameUiType;

    private GameObject pauseObj;
    private Component pauseController;
    private Type pauseControllerType;

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

    private static void SetDifficultyGameStarted(bool value)
    {
        Type difficultyMenuType = FindType("DifficultyMenu");
        if (difficultyMenuType == null) return;

        FieldInfo field = difficultyMenuType.GetField("<GameStarted>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, value);
        }
    }

    private static void DestroyByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null)
        {
            UObject.DestroyImmediate(go);
        }
    }

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetDifficultyGameStarted(false);

        gameUiType = FindTypeOrAssert("GameUI");
        pauseControllerType = FindTypeOrAssert("PasueController");

        // Remove leftovers from prior test runs.
        DestroyByName("GameUICanvas");
        DestroyByName("PauseCanvas");

        gameUiObj = new GameObject("GameUI_Test");
        gameUi = gameUiObj.AddComponent(gameUiType);

        pauseObj = new GameObject("PauseController_Test");
        pauseController = pauseObj.AddComponent(pauseControllerType);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetDifficultyGameStarted(false);

        if (gameUiObj != null) UObject.DestroyImmediate(gameUiObj);
        if (pauseObj != null) UObject.DestroyImmediate(pauseObj);

        DestroyByName("GameUICanvas");
        DestroyByName("PauseCanvas");
    }

    [UnityTest]
    public IEnumerator Test01_GameUI_DuocGanSingleton()
    {
        yield return null;
        PropertyInfo instanceProp = gameUiType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(instanceProp, "GameUI.Instance property should exist");
        UnityEngine.Object instance = instanceProp.GetValue(null) as UnityEngine.Object;
        Assert.IsNotNull(instance, "GameUI.Instance should be assigned after Start");
    }

    [UnityTest]
    public IEnumerator Test02_GameUI_TaoCanvasAnLucDau()
    {
        yield return null;
        GameObject canvas = GameObject.Find("GameUICanvas");
        Assert.IsNotNull(canvas, "GameUI should create GameUICanvas in Start");
        Assert.IsFalse(canvas.activeSelf, "GameUICanvas should be hidden before game starts");
    }

    [UnityTest]
    public IEnumerator Test03_GameUI_AnKhiGameChuaBatDau()
    {
        SetDifficultyGameStarted(false);
        yield return null;
        yield return null;

        GameObject canvas = GameObject.Find("GameUICanvas");
        Assert.IsNotNull(canvas, "GameUICanvas should exist");
        Assert.IsFalse(canvas.activeSelf, "UI should remain hidden while DifficultyMenu.GameStarted is false");
    }

    [UnityTest]
    public IEnumerator Test04_GameUI_HienKhiGameDaBatDau()
    {
        SetDifficultyGameStarted(true);
        yield return null;
        yield return null;

        GameObject canvas = GameObject.Find("GameUICanvas");
        Assert.IsNotNull(canvas, "GameUICanvas should exist");
        Assert.IsTrue(canvas.activeSelf, "UI should become visible when GameStarted=true and not reviewing");
    }

    [UnityTest]
    public IEnumerator Test05_GameUI_CapNhatSoLanNhay_TextVaMau()
    {
        yield return null;
        MethodInfo updateJumpCount = gameUiType.GetMethod("UpdateJumpCount", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(updateJumpCount, "UpdateJumpCount should exist");

        FieldInfo jumpTextField = gameUiType.GetField("jumpText", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(jumpTextField, "jumpText field should exist");

        updateJumpCount.Invoke(gameUi, new object[] { 1, 5 });
        Component jumpText = jumpTextField.GetValue(gameUi) as Component;

        Assert.IsNotNull(jumpText, "jumpText should be created");

        PropertyInfo textProp = jumpText.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo colorProp = jumpText.GetType().GetProperty("color", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(textProp, "jumpText should have text property");
        Assert.IsNotNull(colorProp, "jumpText should have color property");

        string textValue = textProp.GetValue(jumpText, null) as string;
        Color colorValue = (Color)colorProp.GetValue(jumpText, null);

        Assert.AreEqual("Nhảy: 1/5", textValue);
        Assert.AreEqual(Color.red, colorValue);
    }

    [UnityTest]
    public IEnumerator Test06_GameUI_CapNhatSoDanhDau_TextVaMau()
    {
        yield return null;
        MethodInfo updateMarkerCount = gameUiType.GetMethod("UpdateMarkerCount", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(updateMarkerCount, "UpdateMarkerCount should exist");

        FieldInfo markerTextField = gameUiType.GetField("markerText", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(markerTextField, "markerText field should exist");

        updateMarkerCount.Invoke(gameUi, new object[] { 2, 10 });
        Component markerText = markerTextField.GetValue(gameUi) as Component;

        Assert.IsNotNull(markerText, "markerText should be created");

        PropertyInfo textProp = markerText.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo colorProp = markerText.GetType().GetProperty("color", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(textProp, "markerText should have text property");
        Assert.IsNotNull(colorProp, "markerText should have color property");

        string textValue = textProp.GetValue(markerText, null) as string;
        Color colorValue = (Color)colorProp.GetValue(markerText, null);

        Assert.AreEqual("Dấu: 2/10", textValue);
        Assert.AreEqual(Color.red, colorValue);
    }

    [UnityTest]
    public IEnumerator Test07_PauseController_TamDungGame_DatTrangThaiPause()
    {
        yield return null;
        MethodInfo pauseGame = pauseControllerType.GetMethod("PauseGame", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo isPaused = pauseControllerType.GetMethod("IsPaused", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(pauseGame, "PauseGame should exist");
        Assert.IsNotNull(isPaused, "IsPaused should exist");

        pauseGame.Invoke(pauseController, null);
        bool paused = (bool)isPaused.Invoke(pauseController, null);

        Assert.IsTrue(paused, "PauseGame should set paused state to true");
        Assert.AreEqual(0f, Time.timeScale, "PauseGame should set Time.timeScale to 0");

        GameObject pauseCanvas = GameObject.Find("PauseCanvas");
        Assert.IsNotNull(pauseCanvas, "PauseCanvas should be created");
        Assert.IsTrue(pauseCanvas.activeSelf, "PauseCanvas should be visible when paused");
    }

    [UnityTest]
    public IEnumerator Test08_PauseController_DemNguoc_ResumeGame()
    {
        yield return null;
        FieldInfo countdownField = pauseControllerType.GetField("countdownTime", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo pauseGame = pauseControllerType.GetMethod("PauseGame", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo startCountdown = pauseControllerType.GetMethod("StartCountdownToResume", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo isPaused = pauseControllerType.GetMethod("IsPaused", BindingFlags.Public | BindingFlags.Instance);

        Assert.IsNotNull(countdownField, "countdownTime field should exist");
        countdownField.SetValue(pauseController, 1f);

        pauseGame.Invoke(pauseController, null);
        startCountdown.Invoke(pauseController, null);

        yield return new WaitForSecondsRealtime(1.8f);

        bool paused = (bool)isPaused.Invoke(pauseController, null);
        Assert.IsFalse(paused, "Game should resume after countdown");
        Assert.AreEqual(1f, Time.timeScale, "Time.timeScale should return to 1 after resume");
    }

    [UnityTest]
    public IEnumerator Test09_PauseController_HuyDemNguoc_DungDemNguoc()
    {
        yield return null;
        FieldInfo countdownField = pauseControllerType.GetField("countdownTime", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo pauseGame = pauseControllerType.GetMethod("PauseGame", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo startCountdown = pauseControllerType.GetMethod("StartCountdownToResume", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo cancelCountdown = pauseControllerType.GetMethod("CancelCountdown", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo isCountingDown = pauseControllerType.GetMethod("IsCountingDown", BindingFlags.Public | BindingFlags.Instance);

        countdownField.SetValue(pauseController, 3f);
        pauseGame.Invoke(pauseController, null);
        startCountdown.Invoke(pauseController, null);
        yield return new WaitForSecondsRealtime(0.2f);

        cancelCountdown.Invoke(pauseController, null);
        yield return null;

        bool countingDown = (bool)isCountingDown.Invoke(pauseController, null);
        Assert.IsFalse(countingDown, "CancelCountdown should stop countdown state");
        Assert.AreEqual(0f, Time.timeScale, "Game should still be paused after canceling countdown");
    }

    [UnityTest]
    public IEnumerator Test10_PauseController_GoiPauseLanHai_VanAnToan()
    {
        yield return null;
        MethodInfo pauseGame = pauseControllerType.GetMethod("PauseGame", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo isPaused = pauseControllerType.GetMethod("IsPaused", BindingFlags.Public | BindingFlags.Instance);

        pauseGame.Invoke(pauseController, null);
        pauseGame.Invoke(pauseController, null);
        yield return null;

        bool paused = (bool)isPaused.Invoke(pauseController, null);
        Assert.IsTrue(paused, "Calling PauseGame twice should remain paused without errors");
        Assert.AreEqual(0f, Time.timeScale, "Time.timeScale should remain 0 while paused");
    }
}
