using UnityEngine;

/// <summary>
/// Controller để áp dụng và điều khiển Shader Graph materials trong runtime.
/// Hướng dẫn sử dụng:
/// 1. Tạo Shader Graph: Right-click > Create > Shader Graph > URP > Lit Shader Graph
/// 2. Tạo Material sử dụng shader đó
/// 3. Gán material vào field 'customMaterial' trong Inspector
/// 4. Attach script này vào GameObject cần effect
/// </summary>
public class ShaderGraphController : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("Material sử dụng Shader Graph. Tạo từ: Create > Material, rồi chọn Shader Graphs/TenShader")]
    [SerializeField] private Material customMaterial;
    
    [Header("Shader Properties")]
    [Tooltip("Tên property màu trong Shader Graph (thường là _BaseColor hoặc _Color)")]
    [SerializeField] private string colorPropertyName = "_BaseColor";
    [SerializeField] private Color baseColor = Color.white;
    
    [Tooltip("Tên property emission trong Shader Graph")]
    [SerializeField] private string emissionPropertyName = "_EmissionColor";
    [SerializeField] private Color emissionColor = Color.cyan;
    [SerializeField] private float emissionIntensity = 2f;
    
    [Header("Animation")]
    [SerializeField] private bool animateEmission = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 3f;

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        
        // Áp dụng custom material nếu có
        if (customMaterial != null && targetRenderer != null)
        {
            targetRenderer.material = customMaterial;
        }
        
        ApplyShaderProperties();
    }

    void Update()
    {
        if (animateEmission && targetRenderer != null)
        {
            // Tạo hiệu ứng pulse cho emission
            float pulse = Mathf.Lerp(minIntensity, maxIntensity, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            
            Color animatedEmission = emissionColor * pulse;
            
            // Sử dụng MaterialPropertyBlock để hiệu quả hơn (không tạo material instance mới)
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(emissionPropertyName, animatedEmission);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>
    /// Áp dụng các property vào shader
    /// </summary>
    public void ApplyShaderProperties()
    {
        if (targetRenderer == null) return;
        
        Material mat = targetRenderer.material;
        
        // Set base color
        if (mat.HasProperty(colorPropertyName))
        {
            mat.SetColor(colorPropertyName, baseColor);
        }
        
        // Set emission
        if (mat.HasProperty(emissionPropertyName))
        {
            mat.SetColor(emissionPropertyName, emissionColor * emissionIntensity);
            
            // Bật emission cho URP
            mat.EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// Thay đổi màu runtime
    /// </summary>
    public void SetColor(Color newColor)
    {
        baseColor = newColor;
        if (targetRenderer != null && targetRenderer.material.HasProperty(colorPropertyName))
        {
            targetRenderer.material.SetColor(colorPropertyName, newColor);
        }
    }

    /// <summary>
    /// Thay đổi emission runtime
    /// </summary>
    public void SetEmission(Color newEmission, float intensity)
    {
        emissionColor = newEmission;
        emissionIntensity = intensity;
        
        if (targetRenderer != null && targetRenderer.material.HasProperty(emissionPropertyName))
        {
            targetRenderer.material.SetColor(emissionPropertyName, newEmission * intensity);
        }
    }

    /// <summary>
    /// Tạo material từ shader name (dùng khi không gán trong Inspector)
    /// </summary>
    public void CreateMaterialFromShader(string shaderName)
    {
        // Ví dụ: "Shader Graphs/MyGlowShader"
        UnityEngine.Shader shader = UnityEngine.Shader.Find(shaderName);
        
        if (shader != null)
        {
            customMaterial = new Material(shader);
            if (targetRenderer != null)
            {
                targetRenderer.material = customMaterial;
            }
            ApplyShaderProperties();
        }
        else
        {
            Debug.LogWarning($"Shader '{shaderName}' not found!");
        }
    }
}
