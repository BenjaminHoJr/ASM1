using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    [Tooltip("Player tag to check. Leave as default if using CharacterController or PlayerController component.")]
    public string playerTag = "Player";

    [Tooltip("Optional event to hook up in inspector (e.g. show UI, load scene).")]
    public UnityEvent onPlayerExit;

    [Tooltip("If true, timeScale is set to 0 and a basic win UI is created.")]
    public bool pauseAndShowDefaultUI = true;

    bool triggered = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!IsPlayerCollider(other)) return;

        triggered = true;
        onPlayerExit?.Invoke();

        if (pauseAndShowDefaultUI)
            ShowDefaultWin();
    }

    bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;

        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            return true;

        // CharacterController check (common for first-person controllers)
        if (other.GetComponent<CharacterController>() != null ||
            other.GetComponentInParent<CharacterController>() != null)
            return true;

        // Custom player controller script check (if your project has one)
        if (other.GetComponentInParent<MonoBehaviour>() is MonoBehaviour mb &&
            mb.GetType().Name.ToLower().Contains("playercontroller"))
            return true;

        return false;
    }

    void ShowDefaultWin()
    {
        Debug.Log("Player reached exit — YOU WIN");

        // Pause game
        Time.timeScale = 0f;

        // Ensure an EventSystem exists so UI can receive input
        if (FindExisting<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        // Create lightweight UI overlay
        var canvasGO = new GameObject("WinCanvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Background panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);
        var panelRT = panelImage.rectTransform;
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Win text
        var textGO = new GameObject("WinText");
        textGO.transform.SetParent(panelGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = "YOU WIN";
        txt.alignment = TextAnchor.MiddleCenter;

        // Safe font selection
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch { font = null; }

        if (font == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts != null && allFonts.Length > 0)
                font = allFonts[0];
        }

        if (font == null)
        {
            try
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            catch { font = null; }
        }

        if (font != null)
            txt.font = font;
        else
            Debug.LogWarning("ExitTrigger: No font available for win UI. Install a font asset or assign one manually if needed.");

        txt.fontSize = 72;
        txt.color = Color.white;
        var txtRT = txt.rectTransform;
        txtRT.anchorMin = new Vector2(0.1f, 0.6f);
        txtRT.anchorMax = new Vector2(0.9f, 0.9f);
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        // Buttons container
        var buttonsGO = new GameObject("Buttons");
        buttonsGO.transform.SetParent(panelGO.transform, false);
        var buttonsRT = buttonsGO.AddComponent<RectTransform>();
        buttonsRT.anchorMin = new Vector2(0.3f, 0.1f);
        buttonsRT.anchorMax = new Vector2(0.7f, 0.4f);
        buttonsRT.offsetMin = Vector2.zero;
        buttonsRT.offsetMax = Vector2.zero;

        // Restart button
        CreateButton(buttonsGO.transform, "Restart", "Restart", () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        // Quit button
        CreateButton(buttonsGO.transform, "Quit", "Quit", () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        // Make first button selected
        var esComp = FindExisting<EventSystem>();
        if (esComp != null)
        {
            var firstButton = buttonsGO.GetComponentInChildren<Button>();
            if (firstButton != null)
                esComp.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    // Helper to create a simple button under a parent transform
    void CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 60);

        // Label
        var labelGO = new GameObject("Text");
        labelGO.transform.SetParent(btnGO.transform, false);
        var txt = labelGO.AddComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;
        txt.fontSize = 28;

        // reuse same font selection approach as above
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts != null && allFonts.Length > 0)
                font = allFonts[0];
        }
        if (font == null)
        {
            try { font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { font = null; }
        }
        if (font != null) txt.font = font;

        var txtRT = txt.rectTransform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
    }

    // Compatibility helper to avoid uses of the deprecated FindObjectOfType<T>() API.
    static T FindExisting<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_2_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }
}