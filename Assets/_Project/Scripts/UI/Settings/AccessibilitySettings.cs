using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>
/// Erisilebilirlik ayarlarini yoneten sinif.
/// Brightness, renk korlugu, UI boyutu gibi ayarlari kontrol eder.
/// </summary>
public class AccessibilitySettings : MonoBehaviour
{
    public static AccessibilitySettings Instance { get; private set; }

    [Header("Ayarlar")]
    [Range(0.5f, 1.5f)]
    public float brightness = 1f;

    public ColorBlindMode colorBlindMode = ColorBlindMode.None;

    [Range(0.8f, 1.5f)]
    public float uiScale = 1f;

    public bool screenShakeEnabled = true;

    [Header("Post Processing (Opsiyonel)")]
    [Tooltip("Brightness icin Volume profili (URP)")]
    public Volume brightnessVolume;

    [Header("Renk Korlugu Paleti")]
    public ColorBlindPalette normalPalette;
    public ColorBlindPalette deuteranopiaPalette;
    public ColorBlindPalette protanopiaPalette;
    public ColorBlindPalette tritanopiaPalette;

    // Aktif palet
    private ColorBlindPalette activePalette;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        // Varsayilan paletleri olustur
        InitializeDefaultPalettes();

        // Kaydedilmis ayarlari yukle
        LoadSettings();

        // Event'lere abone ol
        GameEvents.OnSettingsChanged += OnSettingsChanged;
    }

    void OnDestroy()
    {
        GameEvents.OnSettingsChanged -= OnSettingsChanged;
    }

    void OnSettingsChanged()
    {
        LoadSettings();
    }

    void InitializeDefaultPalettes()
    {
        if (normalPalette == null)
        {
            normalPalette = new ColorBlindPalette
            {
                health = new Color(0f, 1f, 0.5f),
                healthLow = new Color(1f, 0.3f, 0.3f),
                enemy = new Color(1f, 0.2f, 0.2f),
                friendly = new Color(0.2f, 1f, 0.2f),
                warning = new Color(1f, 0.8f, 0f),
                info = new Color(0f, 0.8f, 1f),
                neonCyan = new Color(0f, 1f, 1f),
                neonPink = new Color(1f, 0f, 0.6f),
                neonYellow = new Color(1f, 1f, 0f)
            };
        }

        if (deuteranopiaPalette == null)
        {
            // Yesil-kirmizi korlugu icin optimize edilmis
            deuteranopiaPalette = new ColorBlindPalette
            {
                health = new Color(0.2f, 0.6f, 1f),      // Mavi
                healthLow = new Color(1f, 0.6f, 0f),    // Turuncu
                enemy = new Color(1f, 0.5f, 0f),        // Turuncu
                friendly = new Color(0f, 0.5f, 1f),     // Mavi
                warning = new Color(1f, 0.9f, 0.3f),    // Acik sari
                info = new Color(0.3f, 0.7f, 1f),       // Acik mavi
                neonCyan = new Color(0.3f, 0.7f, 1f),
                neonPink = new Color(1f, 0.6f, 0.3f),
                neonYellow = new Color(1f, 1f, 0.5f)
            };
        }

        if (protanopiaPalette == null)
        {
            // Kirmizi korlugu icin optimize edilmis
            protanopiaPalette = new ColorBlindPalette
            {
                health = new Color(0f, 0.7f, 1f),       // Cyan
                healthLow = new Color(1f, 0.7f, 0f),    // Sari-turuncu
                enemy = new Color(1f, 0.6f, 0.2f),      // Turuncu
                friendly = new Color(0f, 0.8f, 0.8f),   // Cyan
                warning = new Color(1f, 0.85f, 0.4f),   // Acik sari
                info = new Color(0.2f, 0.6f, 1f),       // Mavi
                neonCyan = new Color(0.2f, 0.8f, 1f),
                neonPink = new Color(1f, 0.7f, 0.4f),
                neonYellow = new Color(1f, 1f, 0.4f)
            };
        }

        if (tritanopiaPalette == null)
        {
            // Mavi-sari korlugu icin optimize edilmis
            tritanopiaPalette = new ColorBlindPalette
            {
                health = new Color(0f, 1f, 0.4f),       // Yesil
                healthLow = new Color(1f, 0.3f, 0.5f),  // Kirmizi-pembe
                enemy = new Color(1f, 0.3f, 0.3f),      // Kirmizi
                friendly = new Color(0.3f, 1f, 0.3f),   // Yesil
                warning = new Color(1f, 0.5f, 0.5f),    // Acik kirmizi
                info = new Color(0.5f, 1f, 0.5f),       // Acik yesil
                neonCyan = new Color(0.5f, 1f, 0.8f),
                neonPink = new Color(1f, 0.4f, 0.6f),
                neonYellow = new Color(1f, 0.8f, 0.6f)
            };
        }

        activePalette = normalPalette;
    }

    /// <summary>
    /// Ayarlari kayitli veriden yukle
    /// </summary>
    public void LoadSettings()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            var data = SaveManager.Instance.Data;

            brightness = data.brightness;
            colorBlindMode = (ColorBlindMode)data.colorBlindMode;
            uiScale = data.uiScale;
            screenShakeEnabled = data.screenShakeEnabled;

            ApplyBrightness();
            ApplyColorBlindMode();
        }
    }

    /// <summary>
    /// Brightness ayarini uygula
    /// </summary>
    public void SetBrightness(float value)
    {
        brightness = Mathf.Clamp(value, 0.5f, 1.5f);
        ApplyBrightness();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetBrightness(brightness);
        }
    }

    void ApplyBrightness()
    {
        // Basit brightness - RenderSettings.ambientLight ile
        // Not: Daha iyi sonuc icin Post Processing Volume kullanilabilir
        float intensity = Mathf.Lerp(0.5f, 1.5f, (brightness - 0.5f));

        // Ambient light'i ayarla
        RenderSettings.ambientLight = new Color(intensity, intensity, intensity);

        // Volume varsa onu da ayarla (URP icin)
        if (brightnessVolume != null)
        {
            // Color adjustments bilesenini bul ve exposure ayarla
            // Bu URP'de Volume profile'a bagli
        }
    }

    /// <summary>
    /// Renk korlugu modunu degistir
    /// </summary>
    public void SetColorBlindMode(ColorBlindMode mode)
    {
        colorBlindMode = mode;
        ApplyColorBlindMode();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetColorBlindMode((int)mode);
        }
    }

    void ApplyColorBlindMode()
    {
        switch (colorBlindMode)
        {
            case ColorBlindMode.Deuteranopia:
                activePalette = deuteranopiaPalette;
                break;
            case ColorBlindMode.Protanopia:
                activePalette = protanopiaPalette;
                break;
            case ColorBlindMode.Tritanopia:
                activePalette = tritanopiaPalette;
                break;
            default:
                activePalette = normalPalette;
                break;
        }

        // HUD'a bildir
        GameEvents.RaiseHUDSettingsChanged();
    }

    /// <summary>
    /// UI boyutunu degistir
    /// </summary>
    public void SetUIScale(float scale)
    {
        uiScale = Mathf.Clamp(scale, 0.8f, 1.5f);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetUIScale(uiScale);
        }

        // Tum canvas scaler'lari guncelle
        UpdateAllCanvasScalers();
    }

    void UpdateAllCanvasScalers()
    {
        // Sahnedeki tum CanvasScaler'lari bul ve referenceResolution'i ayarla
        var scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
        foreach (var scaler in scalers)
        {
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                // Varsayilan 1920x1080'i scale'e gore ayarla
                float baseWidth = 1920f / uiScale;
                float baseHeight = 1080f / uiScale;
                scaler.referenceResolution = new Vector2(baseWidth, baseHeight);
            }
        }
    }

    /// <summary>
    /// Ekran titremesini ayarla
    /// </summary>
    public void SetScreenShake(bool enabled)
    {
        screenShakeEnabled = enabled;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetScreenShake(enabled);
        }
    }

    /// <summary>
    /// Aktif renk paletini al
    /// </summary>
    public ColorBlindPalette GetActivePalette()
    {
        return activePalette ?? normalPalette;
    }

    /// <summary>
    /// Rengi aktif palete gore donustur
    /// </summary>
    public Color GetColor(AccessibilityColorType colorType)
    {
        var palette = GetActivePalette();

        return colorType switch
        {
            AccessibilityColorType.Health => palette.health,
            AccessibilityColorType.HealthLow => palette.healthLow,
            AccessibilityColorType.Enemy => palette.enemy,
            AccessibilityColorType.Friendly => palette.friendly,
            AccessibilityColorType.Warning => palette.warning,
            AccessibilityColorType.Info => palette.info,
            AccessibilityColorType.NeonCyan => palette.neonCyan,
            AccessibilityColorType.NeonPink => palette.neonPink,
            AccessibilityColorType.NeonYellow => palette.neonYellow,
            _ => Color.white
        };
    }

    /// <summary>
    /// Ekran titremesi yapilabilir mi?
    /// </summary>
    public bool CanShakeScreen()
    {
        return screenShakeEnabled;
    }
}

/// <summary>
/// Renk korlugu modu icin renk paleti
/// </summary>
[System.Serializable]
public class ColorBlindPalette
{
    public Color health;
    public Color healthLow;
    public Color enemy;
    public Color friendly;
    public Color warning;
    public Color info;
    public Color neonCyan;
    public Color neonPink;
    public Color neonYellow;
}

/// <summary>
/// Erisilebilirlik renk turleri
/// </summary>
public enum AccessibilityColorType
{
    Health,
    HealthLow,
    Enemy,
    Friendly,
    Warning,
    Info,
    NeonCyan,
    NeonPink,
    NeonYellow
}
