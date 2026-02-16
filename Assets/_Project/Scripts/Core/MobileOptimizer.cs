using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mobil cihazlarda otomatik performans optimizasyonu.
/// Adaptive performance: FPS duserse otomatik olarak efektleri azaltir.
/// Particle limitleri, shadow kontrolu ve pil tasarrufu icerir.
/// </summary>
public class MobileOptimizer : MonoBehaviour
{
    public static MobileOptimizer Instance { get; private set; }

    // === ADAPTIVE PERFORMANCE ===
    private float _fpsCheckInterval = 2f;
    private float _fpsTimer;
    private int _frameCount;
    private float _currentFps;
    private int _performanceTier; // 0=Very Low, 1=Low, 2=Medium, 3=High

    // Adaptive thresholds
    private const float FPS_LOW_THRESHOLD = 25f;
    private const float FPS_RECOVER_THRESHOLD = 45f;
    private const int TARGET_FPS_GAMEPLAY = 60;
    private const int TARGET_FPS_MENU = 30;

    // === PARTICLE LIMITS ===
    /// <summary>
    /// Mobilde particle count carpani (0.0 - 1.0).
    /// ParticleManager bu degeri kullanarak particle sayisini sinirlar.
    /// </summary>
    public float ParticleCountMultiplier { get; private set; } = 1f;

    /// <summary>
    /// Mobilde maksimum ayni anda aktif particle system sayisi.
    /// </summary>
    public int MaxActiveParticleSystems { get; private set; } = 20;

    /// <summary>
    /// Efektlerin azaltilip azaltilmadigi (adaptive performance tarafindan)
    /// </summary>
    public bool IsReducedEffects { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureMobileControls()
    {
        // Ilk sahne yuklendiginde olustur
        CreateMobileControlsIfNeeded();

        // Her sahne yuklendiginde kontrol et (sahne gecislerinde kaybolmasin)
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateMobileControlsIfNeeded();
    }

    static void CreateMobileControlsIfNeeded()
    {
        if (Object.FindFirstObjectByType<MobileControls>() == null)
        {
            GameObject mobileCtrlObj = new GameObject("MobileControls");
            mobileCtrlObj.AddComponent<MobileControls>();
            Debug.Log("[MobileOptimizer] MobileControls olusturuldu (sahne: " +
                      SceneManager.GetActiveScene().name + ")");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (!Application.isMobilePlatform) return;

        // Hedef frame rate - 60 FPS
        Application.targetFrameRate = TARGET_FPS_GAMEPLAY;

        // VSync kapali (mobilde targetFrameRate kullanilir)
        QualitySettings.vSyncCount = 0;

        // GPU performansina gore kalite ayarla
        int systemMemory = SystemInfo.systemMemorySize;

        if (systemMemory >= 6144) // 6GB+ RAM
        {
            QualitySettings.SetQualityLevel(3, true); // High
        }
        else if (systemMemory >= 4096) // 4GB+ RAM
        {
            QualitySettings.SetQualityLevel(2, true); // Medium
        }
        else if (systemMemory >= 2048) // 2GB+ RAM
        {
            QualitySettings.SetQualityLevel(1, true); // Low
        }
        else
        {
            QualitySettings.SetQualityLevel(0, true); // Very Low
        }

        // Shadow ayarlari - mobilde golgeleri kapat veya dusur
        ApplyShadowSettings(systemMemory);

        // Ekran daima acik (pil yonetimi oyuncuya birakilir)
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Multi-touch etkinlestir
        Input.multiTouchEnabled = true;

        // Fizik optimizasyonu
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        Debug.Log($"[MobileOptimizer] Cihaz: {SystemInfo.deviceModel}, RAM: {systemMemory}MB, " +
                  $"Kalite: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }

    static void ApplyShadowSettings(int systemMemory)
    {
        if (systemMemory < 4096)
        {
            // Dusuk RAM: golgeleri tamamen kapat
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
        }
        else if (systemMemory < 6144)
        {
            // Orta RAM: sadece hard shadow, dusuk cozunurluk
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 20f;
        }
        // 6GB+ RAM: varsayilan kalite ayarlari
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Performans tier'ini belirle
        int systemMemory = SystemInfo.systemMemorySize;
        if (systemMemory >= 6144) _performanceTier = 3;
        else if (systemMemory >= 4096) _performanceTier = 2;
        else if (systemMemory >= 2048) _performanceTier = 1;
        else _performanceTier = 0;

        // Baslangic particle limitleri
        ApplyParticleLimits();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!Application.isMobilePlatform) return;

        // FPS olcumu
        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;

        if (_fpsTimer >= _fpsCheckInterval)
        {
            _currentFps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0f;

            AdaptPerformance();
        }
    }

    /// <summary>
    /// FPS'e gore otomatik performans ayari.
    /// FPS duserse efektleri azaltir, duzeldikce geri acar.
    /// </summary>
    void AdaptPerformance()
    {
        if (_currentFps < FPS_LOW_THRESHOLD && !IsReducedEffects)
        {
            // FPS cok dustu - efektleri azalt
            IsReducedEffects = true;
            _performanceTier = Mathf.Max(0, _performanceTier - 1);
            ApplyParticleLimits();

            // Shadow'lari kapat
            QualitySettings.shadows = ShadowQuality.Disable;

            Debug.Log($"[MobileOptimizer] Adaptive: FPS={_currentFps:F0}, efektler azaltildi (tier={_performanceTier})");
        }
        else if (_currentFps > FPS_RECOVER_THRESHOLD && IsReducedEffects)
        {
            // FPS toparlandiflk - efektleri geri yukle
            IsReducedEffects = false;

            // Shadow'lari geri ac (RAM'e gore)
            int systemMemory = SystemInfo.systemMemorySize;
            ApplyShadowSettings(systemMemory);

            Debug.Log($"[MobileOptimizer] Adaptive: FPS={_currentFps:F0}, efektler normale dondu (tier={_performanceTier})");
        }
    }

    void ApplyParticleLimits()
    {
        switch (_performanceTier)
        {
            case 0: // Very Low
                ParticleCountMultiplier = 0.3f;
                MaxActiveParticleSystems = 5;
                break;
            case 1: // Low
                ParticleCountMultiplier = 0.5f;
                MaxActiveParticleSystems = 10;
                break;
            case 2: // Medium
                ParticleCountMultiplier = 0.75f;
                MaxActiveParticleSystems = 15;
                break;
            default: // High
                ParticleCountMultiplier = 1f;
                MaxActiveParticleSystems = 20;
                break;
        }
    }

    // === PIL TASARRUFU: FRAME RATE YONETIMI ===

    /// <summary>
    /// Oyun durakladiginda veya menudeyken frame rate'i dusur.
    /// GameManager.PauseGame() ve ResumeGame() tarafindan cagirilir.
    /// </summary>
    public static void SetMenuFrameRate()
    {
        if (!Application.isMobilePlatform) return;
        Application.targetFrameRate = TARGET_FPS_MENU;
    }

    /// <summary>
    /// Oyun devam ettiginde frame rate'i yukselt.
    /// </summary>
    public static void SetGameplayFrameRate()
    {
        if (!Application.isMobilePlatform) return;
        Application.targetFrameRate = TARGET_FPS_GAMEPLAY;
    }

    /// <summary>
    /// Mevcut FPS degerini dondurur (performans izleme icin)
    /// </summary>
    public float GetCurrentFPS() => _currentFps;

    /// <summary>
    /// Mevcut performans tier'ini dondurur (0-3)
    /// </summary>
    public int GetPerformanceTier() => _performanceTier;
}
