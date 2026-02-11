using UnityEngine;

/// <summary>
/// Tum oyun ayarlarini tutan ScriptableObject.
/// Assets > Create > NeonJump > Game Settings ile olusturulabilir.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "NeonJump/Game Settings")]
public class GameSettingsConfig : ScriptableObject
{
    [Header("Goruntu Ayarlari")]
    [Range(0.5f, 1.5f)]
    [Tooltip("Ekran parlaklik seviyesi")]
    public float brightness = 1f;

    [Tooltip("Tam ekran modu")]
    public bool fullscreen = true;

    [Tooltip("Cozunurluk index (-1 = native)")]
    public int resolutionIndex = -1;

    [Range(0, 5)]
    [Tooltip("Grafik kalite seviyesi (0=En Dusuk, 5=Ultra)")]
    public int qualityLevel = 2;

    [Header("Ses Ayarlari")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Range(0f, 1f)]
    public float sfxVolume = 0.7f;

    [Header("Erisilebilirlik")]
    [Tooltip("Renk korlugu modu")]
    public ColorBlindMode colorBlindMode = ColorBlindMode.None;

    [Range(0.8f, 1.5f)]
    [Tooltip("UI boyut carpani")]
    public float uiScale = 1f;

    [Tooltip("Ekran titresimleri")]
    public bool screenShakeEnabled = true;

    [Header("Dil")]
    public GameLanguage language = GameLanguage.Turkish;

    [Header("HUD Ayarlari")]
    public bool minimapEnabled = true;

    [Range(100f, 250f)]
    public float minimapSize = 150f;

    public bool comboDisplayEnabled = true;
    public bool damageIndicatorsEnabled = true;
    public bool killFeedEnabled = true;

    [Header("Kontroller")]
    public bool vibrationEnabled = true;

    [Range(0.5f, 2f)]
    public float aimSensitivity = 1f;

    /// <summary>
    /// Varsayilan ayarlara sifirla
    /// </summary>
    public void ResetToDefaults()
    {
        brightness = 1f;
        fullscreen = true;
        resolutionIndex = -1;
        qualityLevel = 2;

        masterVolume = 1f;
        musicVolume = 0.5f;
        sfxVolume = 0.7f;

        colorBlindMode = ColorBlindMode.None;
        uiScale = 1f;
        screenShakeEnabled = true;

        language = GameLanguage.Turkish;

        minimapEnabled = true;
        minimapSize = 150f;
        comboDisplayEnabled = true;
        damageIndicatorsEnabled = true;
        killFeedEnabled = true;

        vibrationEnabled = true;
        aimSensitivity = 1f;
    }

    /// <summary>
    /// Renk korlugu moduna gore renk donusumu
    /// </summary>
    public Color ApplyColorBlindFilter(Color originalColor)
    {
        switch (colorBlindMode)
        {
            case ColorBlindMode.Deuteranopia:
                return ConvertDeuteranopia(originalColor);
            case ColorBlindMode.Protanopia:
                return ConvertProtanopia(originalColor);
            case ColorBlindMode.Tritanopia:
                return ConvertTritanopia(originalColor);
            default:
                return originalColor;
        }
    }

    private Color ConvertDeuteranopia(Color c)
    {
        // Yesil-kirmizi korlugu - yesili maviye kaydir
        float r = c.r * 0.625f + c.g * 0.375f;
        float g = c.r * 0.7f + c.g * 0.3f;
        float b = c.b;
        return new Color(r, g, b, c.a);
    }

    private Color ConvertProtanopia(Color c)
    {
        // Kirmizi korlugu - kirmiziyi sariya kaydir
        float r = c.r * 0.567f + c.g * 0.433f;
        float g = c.r * 0.558f + c.g * 0.442f;
        float b = c.b;
        return new Color(r, g, b, c.a);
    }

    private Color ConvertTritanopia(Color c)
    {
        // Mavi-sari korlugu
        float r = c.r * 0.95f + c.g * 0.05f;
        float g = c.g;
        float b = c.g * 0.433f + c.b * 0.567f;
        return new Color(r, g, b, c.a);
    }
}

/// <summary>
/// Renk korlugu modlari
/// </summary>
public enum ColorBlindMode
{
    None = 0,
    Deuteranopia = 1,  // Yesil-kirmizi (en yaygin)
    Protanopia = 2,    // Kirmizi
    Tritanopia = 3     // Mavi-sari
}

/// <summary>
/// Desteklenen diller
/// </summary>
public enum GameLanguage
{
    Turkish = 0,
    English = 1
}
