using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gelistirilmis pause menu kontrolcusu.
/// Oyun durumu, hizli ayarlar ve mevcut istatistikleri gosterir.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("Panel Referanslari")]
    public GameObject pausePanel;
    public CanvasGroup canvasGroup;

    [Header("Baslik")]
    public TMP_Text titleText;

    [Header("Butonlar")]
    public Button continueButton;
    public Button settingsButton;
    public Button controlsButton;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Mevcut Istatistikler")]
    public TMP_Text playTimeText;
    public TMP_Text scoreText;
    public TMP_Text enemiesText;
    public TMP_Text comboText;

    [Header("Hizli Ayarlar")]
    public Slider masterVolumeSlider;
    public TMP_Text volumeText;

    [Header("Kontroller Paneli")]
    public GameObject controlsPanel;
    public TMP_Text controlsInfoText;
    public Button controlsBackButton;

    [Header("Onay Paneli")]
    public GameObject confirmPanel;
    public TMP_Text confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Animasyon")]
    public float fadeSpeed = 5f;

    [Header("Mobil Kontrol Ayarlari")]
    public GameObject mobileSettingsPanel;
    public Toggle mobileToggle;
    public Slider mobileSizeSlider;
    public Slider mobileOpacitySlider;
    public TMP_Text mobileSizeText;
    public TMP_Text mobileOpacityText;

    private bool isPaused = false;
    private System.Action pendingAction;

    // Neon renkleri
    private readonly Color neonCyan = new Color(0f, 1f, 1f);
    private readonly Color neonPink = new Color(1f, 0f, 0.6f);

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupListeners();
        SetupControlsText();
        SetupMobileSettings();

        // Baslangicta gizle
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    void Update()
    {
        // ESC veya P tusu ile pause
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                // Kontroller veya onay paneli aciksa onlari kapat
                if (controlsPanel != null && controlsPanel.activeSelf)
                {
                    HideControls();
                }
                else if (confirmPanel != null && confirmPanel.activeSelf)
                {
                    HideConfirm();
                }
                else
                {
                    Resume();
                }
            }
            else
            {
                Pause();
            }
        }
    }

    void SetupListeners()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(Resume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(ShowControls);

        if (restartButton != null)
            restartButton.onClick.AddListener(() => ShowConfirm("Oyunu yeniden baslatmak istediginizden emin misiniz?", Restart));

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => ShowConfirm("Ana menuye donmek istediginizden emin misiniz?", GoToMainMenu));

        if (controlsBackButton != null)
            controlsBackButton.onClick.AddListener(HideControls);

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmAction);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(HideConfirm);

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void SetupControlsText()
    {
        if (controlsInfoText == null) return;

        string controls;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == "en")
        {
            controls = @"CONTROLS

Movement: WASD / Arrow Keys
Jump: Space
Double Jump: Space (in air)
Dash: Ctrl / Shift
Roll: K
Wall Slide: Hold against wall
Wall Jump: Jump while wall sliding

Shoot: J / Left Mouse
Aim: Mouse / Right Stick
Reload: R
Switch Weapon: 1, 2, 3 / Scroll

Pause: ESC / P
Minimap Toggle: M
HUD Edit: F10";
        }
        else
        {
            controls = @"KONTROLLER

Hareket: WASD / Ok Tuslari
Ziplama: Space
Cift Ziplama: Space (havadayken)
Dash: Ctrl / Shift
Takla: K
Duvar Kaymasi: Duvara dogru tut
Duvar Ziplama: Kayarken ziplama

Ates: J / Sol Mouse
Nishan: Mouse / Sag Stick
Sarj: R
Silah Degistir: 1, 2, 3 / Scroll

Duraklat: ESC / P
Minimap: M
HUD Duzenle: F10";
        }

        controlsInfoText.text = controls;
    }

    // === ANA FONKSIYONLAR ===

    /// <summary>
    /// Oyunu duraklat
    /// </summary>
    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        UpdateStatistics();
        LoadVolumeSettings();

        // Lokalize baslik
        if (titleText != null)
        {
            titleText.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get("game_paused")
                : "OYUN DURDURULDU";
        }

        // Event fire
        GameEvents.RaiseGamePaused();

        Debug.Log("[PauseMenu] Oyun duraklatildi");
    }

    /// <summary>
    /// Oyuna devam et
    /// </summary>
    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        // Event fire
        GameEvents.RaiseGameResumed();

        PlayButtonSound();
        Debug.Log("[PauseMenu] Oyuna devam edildi");
    }

    /// <summary>
    /// Istatistikleri guncelle
    /// </summary>
    void UpdateStatistics()
    {
        if (InGameStatistics.Instance != null)
        {
            var stats = InGameStatistics.Instance;

            if (playTimeText != null)
                playTimeText.text = $"Sure: {stats.GetFormattedPlayTime()}";

            if (scoreText != null)
                scoreText.text = $"Skor: {stats.scoreGained:N0}";

            if (enemiesText != null)
                enemiesText.text = $"Dusman: {stats.enemiesKilled}";

            if (comboText != null)
                comboText.text = $"Max Kombo: {stats.maxCombo}x";
        }
        else
        {
            // GameManager'dan al (fallback)
            if (GameManager.Instance != null)
            {
                if (scoreText != null)
                    scoreText.text = $"Skor: {GameManager.Instance.GetScore():N0}";
            }
        }
    }

    void LoadVolumeSettings()
    {
        if (masterVolumeSlider != null && SaveManager.Instance != null)
        {
            masterVolumeSlider.value = SaveManager.Instance.Data.masterVolume;
            UpdateVolumeText(masterVolumeSlider.value);
        }
    }

    void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetMasterVolume(value);

        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        if (volumeText != null)
            volumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    // === PANEL KONTROLLERI ===

    void OpenSettings()
    {
        PlayButtonSound();

        if (SettingsUI.Instance != null)
        {
            SettingsUI.Instance.Show();
        }
        else if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ShowSettings();
        }
    }

    void ShowControls()
    {
        PlayButtonSound();

        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    void HideControls()
    {
        PlayButtonSound();

        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    void ShowConfirm(string message, System.Action action)
    {
        PlayButtonSound();

        pendingAction = action;

        if (confirmText != null)
            confirmText.text = message;

        if (confirmPanel != null)
            confirmPanel.SetActive(true);
    }

    void HideConfirm()
    {
        PlayButtonSound();

        pendingAction = null;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    void ConfirmAction()
    {
        PlayButtonSound();

        pendingAction?.Invoke();
        pendingAction = null;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    // === OYUN AKSIYONLARI ===

    void Restart()
    {
        // Istatistikleri kaydet
        if (InGameStatistics.Instance != null)
        {
            InGameStatistics.Instance.SaveToGlobalStats();
        }

        Time.timeScale = 1f;
        isPaused = false;

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    void GoToMainMenu()
    {
        // Istatistikleri kaydet
        if (InGameStatistics.Instance != null)
        {
            InGameStatistics.Instance.SaveToGlobalStats();
        }

        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene("MainMenu");
    }

    // === MOBIL KONTROL AYARLARI ===

    void SetupMobileSettings()
    {
        // Sadece mobil platformlarda veya editor'de goster
        bool showMobile = Application.isMobilePlatform ||
                          UnityEngine.InputSystem.Touchscreen.current != null;

        #if UNITY_EDITOR
        showMobile = true; // Editor'de her zaman goster (test icin)
        #endif

        if (mobileSettingsPanel != null)
            mobileSettingsPanel.SetActive(showMobile);

        if (!showMobile) return;

        // Toggle
        if (mobileToggle != null)
        {
            mobileToggle.isOn = MobileControls.Instance != null && MobileControls.Instance.IsEnabled;
            mobileToggle.onValueChanged.AddListener(OnMobileToggleChanged);
        }

        // Size slider (0.5 - 1.5)
        if (mobileSizeSlider != null)
        {
            mobileSizeSlider.minValue = 0.5f;
            mobileSizeSlider.maxValue = 1.5f;
            mobileSizeSlider.value = MobileControls.Instance != null ? MobileControls.Instance.ButtonSizeScale : 1.0f;
            mobileSizeSlider.onValueChanged.AddListener(OnMobileSizeChanged);
            UpdateMobileSizeText(mobileSizeSlider.value);
        }

        // Opacity slider (0.3 - 1.0)
        if (mobileOpacitySlider != null)
        {
            mobileOpacitySlider.minValue = 0.3f;
            mobileOpacitySlider.maxValue = 1.0f;
            mobileOpacitySlider.value = MobileControls.Instance != null ? MobileControls.Instance.Opacity : 0.8f;
            mobileOpacitySlider.onValueChanged.AddListener(OnMobileOpacityChanged);
            UpdateMobileOpacityText(mobileOpacitySlider.value);
        }
    }

    void OnMobileToggleChanged(bool value)
    {
        if (MobileControls.Instance != null)
            MobileControls.Instance.SetEnabled(value);

        PlayButtonSound();
    }

    void OnMobileSizeChanged(float value)
    {
        if (MobileControls.Instance != null)
            MobileControls.Instance.SetButtonSize(value);

        UpdateMobileSizeText(value);
    }

    void OnMobileOpacityChanged(float value)
    {
        if (MobileControls.Instance != null)
            MobileControls.Instance.SetOpacity(value);

        UpdateMobileOpacityText(value);
    }

    void UpdateMobileSizeText(float value)
    {
        if (mobileSizeText != null)
            mobileSizeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    void UpdateMobileOpacityText(float value)
    {
        if (mobileOpacityText != null)
            mobileOpacityText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    void PlayButtonSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    /// <summary>
    /// Duraklatildi mi?
    /// </summary>
    public bool IsPaused => isPaused;
}
