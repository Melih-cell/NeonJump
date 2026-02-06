using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sokak lambasi - Light2D ile isik yayar
/// Neon temali titreme ve renk efektleri icin ayarlar icerir
/// </summary>
public class StreetLamp : MonoBehaviour
{
    [Header("Isik Ayarlari")]
    public Color lightColor = new Color(1f, 0.95f, 0.7f, 1f); // Sicak sari
    public float intensity = 1.5f;
    public float innerRadius = 1f;
    public float outerRadius = 5f;
    public float falloffStrength = 0.5f;

    [Header("Spot Isik (Asagi Yonlu)")]
    public bool useSpotLight = true;
    public float spotAngle = 120f;
    public Vector2 spotOffset = new Vector2(0f, -0.5f); // Lambanin ucundan asagi

    [Header("Titreme Efekti")]
    public bool enableFlicker = true;
    public float flickerSpeed = 8f;
    public float flickerAmount = 0.15f; // Yogunluk degisim miktari

    [Header("Neon Glow")]
    public bool enableGlow = true;
    public Color glowColor = new Color(1f, 0.8f, 0.3f, 0.5f);
    public float glowRadius = 1.5f;
    public float glowIntensity = 0.8f;

    private Light2D mainLight;
    private Light2D glowLight;
    private float baseIntensity;
    private float timer;

    void Start()
    {
        SetupLights();
    }

    void SetupLights()
    {
        // Ana isik - spot veya point
        mainLight = GetComponentInChildren<Light2D>();
        if (mainLight == null)
        {
            GameObject lightObj = new GameObject("LampLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(spotOffset.x, spotOffset.y, 0f);
            mainLight = lightObj.AddComponent<Light2D>();
        }

        if (useSpotLight)
        {
            mainLight.lightType = Light2D.LightType.Point;
        }
        else
        {
            mainLight.lightType = Light2D.LightType.Point;
        }

        mainLight.color = lightColor;
        mainLight.intensity = intensity;
        mainLight.pointLightInnerRadius = innerRadius;
        mainLight.pointLightOuterRadius = outerRadius;
        mainLight.falloffIntensity = falloffStrength;

        baseIntensity = intensity;

        // Neon glow isigi
        if (enableGlow)
        {
            Transform existing = transform.Find("GlowLight");
            if (existing != null)
            {
                glowLight = existing.GetComponent<Light2D>();
            }

            if (glowLight == null)
            {
                GameObject glowObj = new GameObject("GlowLight");
                glowObj.transform.SetParent(transform);
                glowObj.transform.localPosition = Vector3.zero;
                glowLight = glowObj.AddComponent<Light2D>();
            }

            glowLight.lightType = Light2D.LightType.Point;
            glowLight.color = glowColor;
            glowLight.intensity = glowIntensity;
            glowLight.pointLightInnerRadius = 0f;
            glowLight.pointLightOuterRadius = glowRadius;
            glowLight.falloffIntensity = 1f;
        }
    }

    void Update()
    {
        if (mainLight == null) return;

        timer += Time.deltaTime;

        if (enableFlicker)
        {
            // Perlin noise tabanli dogal titreme
            float noise = Mathf.PerlinNoise(timer * flickerSpeed, 0f);
            float flicker = 1f + (noise - 0.5f) * 2f * flickerAmount;
            mainLight.intensity = baseIntensity * flicker;

            if (glowLight != null)
            {
                glowLight.intensity = glowIntensity * flicker;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Isik alanini goster
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.3f);
        Vector3 lightPos = transform.position + new Vector3(spotOffset.x, spotOffset.y, 0f);
        Gizmos.DrawWireSphere(lightPos, outerRadius);

        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.6f);
        Gizmos.DrawWireSphere(lightPos, innerRadius);

        // Glow alani
        if (enableGlow)
        {
            Gizmos.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.2f);
            Gizmos.DrawWireSphere(transform.position, glowRadius);
        }
    }
}
