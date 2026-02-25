using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Controller cho VFX của Exit trong Maze.
/// Attach script này vào Exit prefab hoặc để MazeGenerator tự động thêm VFX.
/// </summary>
public class ExitVFXController : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private VisualEffectAsset vfxAsset;
    [SerializeField] private Color particleColor = new Color(0.5f, 1f, 0.5f, 1f); // Màu xanh lá sáng
    [SerializeField] private float particleRate = 50f;
    [SerializeField] private float particleLifetime = 2f;
    [SerializeField] private float emissionRadius = 0.5f;
    
    [Header("Animation")]
    [SerializeField] private bool rotateEffect = true;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool pulseEffect = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMin = 0.8f;
    [SerializeField] private float pulseMax = 1.2f;

    private VisualEffect vfx;
    private Light exitLight;
    private float initialIntensity;

    void Start()
    {
        SetupVFX();
        SetupLight();
    }

    void Update()
    {
        if (rotateEffect)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        if (pulseEffect && exitLight != null)
        {
            float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            exitLight.intensity = initialIntensity * pulse;
        }
    }

    private void SetupVFX()
    {
        // Tìm hoặc tạo VisualEffect component
        vfx = GetComponentInChildren<VisualEffect>();
        
        if (vfx == null && vfxAsset != null)
        {
            GameObject vfxObj = new GameObject("ExitVFX");
            vfxObj.transform.SetParent(transform);
            vfxObj.transform.localPosition = Vector3.up * 0.5f;
            vfx = vfxObj.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = vfxAsset;
        }

        // Cấu hình VFX nếu có
        if (vfx != null)
        {
            // Thử set các property phổ biến (tùy thuộc vào VFX Graph asset)
            TrySetVFXParameter("ParticleColor", particleColor);
            TrySetVFXParameter("Rate", particleRate);
            TrySetVFXParameter("Lifetime", particleLifetime);
            TrySetVFXParameter("Radius", emissionRadius);
        }
    }

    private void SetupLight()
    {
        // Tạo point light cho hiệu ứng sáng
        exitLight = GetComponentInChildren<Light>();
        
        if (exitLight == null)
        {
            GameObject lightObj = new GameObject("ExitLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 1f;
            
            exitLight = lightObj.AddComponent<Light>();
            exitLight.type = LightType.Point;
            exitLight.color = particleColor;
            exitLight.intensity = 2f;
            exitLight.range = 5f;
            exitLight.shadows = LightShadows.Soft;
        }

        initialIntensity = exitLight.intensity;
    }

    private void TrySetVFXParameter(string name, float value)
    {
        if (vfx != null && vfx.HasFloat(name))
        {
            vfx.SetFloat(name, value);
        }
    }

    private void TrySetVFXParameter(string name, Vector4 value)
    {
        if (vfx != null && vfx.HasVector4(name))
        {
            vfx.SetVector4(name, value);
        }
    }

    private void TrySetVFXParameter(string name, Color value)
    {
        // Color có thể được truyền như Vector4
        TrySetVFXParameter(name, (Vector4)value);
    }

    /// <summary>
    /// Kích hoạt hiệu ứng burst khi player đến gần
    /// </summary>
    public void TriggerBurst()
    {
        if (vfx != null)
        {
            vfx.SendEvent("OnBurst");
        }
    }

    /// <summary>
    /// Thay đổi màu VFX runtime
    /// </summary>
    public void SetColor(Color newColor)
    {
        particleColor = newColor;
        TrySetVFXParameter("ParticleColor", newColor);
        
        if (exitLight != null)
        {
            exitLight.color = newColor;
        }
    }
}
