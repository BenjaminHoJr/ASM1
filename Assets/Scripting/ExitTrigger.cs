using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

        // Create lightweight UI overlay
        var canvasGO = new GameObject("WinCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);
        var panelRT = panelImage.rectTransform;
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var textGO = new GameObject("WinText");
        textGO.transform.SetParent(panelGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = "YOU WIN";
        txt.alignment = TextAnchor.MiddleCenter;

        // Safe font selection: prefer LegacyRuntime.ttf, fall back to any project font,
        // then try creating an OS font. This avoids the ArgumentException seen in newer Unity versions.
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
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
    }
}