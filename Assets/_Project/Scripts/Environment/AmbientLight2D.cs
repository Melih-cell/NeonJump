using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sahneye Global Light2D ekleyerek hafif ortam aydinlatmasi saglar
/// Bos bir GameObject'e ekleyin veya sahneye attiginizda otomatik calisir
/// </summary>
public class AmbientLight2D : MonoBehaviour
{
    [Header("Ortam Isigi")]
    [Range(0f, 1f)]
    public float ambientIntensity = 0.15f;
    public Color ambientColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Koyu mavi gece tonu

    private Light2D globalLight;

    void Start()
    {
        SetupGlobalLight();
    }

    void SetupGlobalLight()
    {
        // Zaten bir Light2D varsa onu kullan
        globalLight = GetComponent<Light2D>();

        if (globalLight == null)
        {
            globalLight = gameObject.AddComponent<Light2D>();
        }

        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = ambientColor;
        globalLight.intensity = ambientIntensity;
    }

    /// <summary>
    /// Runtime'da yogunlugu degistirmek icin
    /// </summary>
    public void SetIntensity(float value)
    {
        ambientIntensity = Mathf.Clamp01(value);
        if (globalLight != null)
            globalLight.intensity = ambientIntensity;
    }

    /// <summary>
    /// Runtime'da rengi degistirmek icin
    /// </summary>
    public void SetColor(Color color)
    {
        ambientColor = color;
        if (globalLight != null)
            globalLight.color = ambientColor;
    }
}
