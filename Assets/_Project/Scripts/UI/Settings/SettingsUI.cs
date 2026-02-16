using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Gelistirilmis ayarlar paneli UI.
/// Tab sistemi ile ses, grafik, kontroller, erisilebilirlik ve dil ayarlarini yonetir.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("Ana Panel")]
    public GameObject settingsPanel;
    public CanvasGroup canvasGroup;

    [Header("Tab Butonlari")]
    public Button audioTabButton;
    public Button graphicsTabButton;
    public Button controlsTabButton;
    public Button accessibilityTabButton;
    public Button languageTabButton;

    [Header("Tab Panelleri")]
    public GameObject audioPanel;
    public GameObject graphicsPanel;
    public GameObject controlsPanel;
    public GameObject accessibilityPanel;
    public GameObject languagePanel;

    [Header("Ses Ayarlari")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public TMP_Text masterVolumeText;
    public TMP_Text musicVolumeText;
    public TMP_Text sfxVolumeText;

    [Header("Grafik Ayarlari")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Slider brightnessSlider;
    public TMP_Text brightnessText;

    [Header("Kontrol Ayarlari")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityText;
    public Toggle vibrationToggle;

    [Header("Mobil Kontrol Ayarlari")]
    public GameObject mobileControlsPanel;
    public Slider buttonSizeSlider;
    public TMP_Text buttonSizeText;
    public Slider controlsOpacitySlider;
    public TMP_Text controlsOpacityText;
    public Slider joystickSensitivitySlider;
    public TMP_Text joystickSensitivityText;
    public Toggle hapticFeedbackToggle;
    public TMP_Dropdown performanceModeDropdown;
    public Toggle aimAssistToggle;
    public Toggle mobileEasyModeToggle;

    [Header("Erisilebilirlik Ayarlari")]
    public TMP_Dropdown colorBlindDropdown;
    public Slider uiScaleSlider;
    public TMP_Text uiScaleText;
    public Toggle screenShakeToggle;

    [Header("Dil Ayarlari")]
    public TMP_Dropdown languageDropdown;

    [Header("Genel Butonlar")]
    public Button applyButton;
    public Button resetButton;
    public Button closeButton;

    [Header("Neon Stili")]
    public Color activeTabColor = new Color(0f, 1f, 1f);
    public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.3f);

    private List<Resolution> filteredResolutions;
    private SettingsTab currentTab = SettingsTab.Audio;
    private bool isInitialized = false;

    // Neon renkleri
    private readonly Color neonCyan = new Color(0f, 1f, 1f);
    private readonly Color neonPink = new Color(1f, 0f, 0.6f);

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        SetupResolutions();
        SetupQualityLevels();
        SetupColorBlindModes();
        SetupLanguages();
        SetupMobileControls();
        SetupPerformanceModes();
        LoadSettings();
        AddListeners();
        ShowTab(SettingsTab.Audio);
    }

    void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        var resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        HashSet<string> added = new HashSet<string>();
        List<string> options = new List<string>();

        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            string key = $"{resolutions[i].width}x{resolutions[i].height}";
            if (!added.Contains(key))
            {
                added.Add(key);
                filteredResolutions.Add(resolutions[i]);
                options.Add(key);
            }
        }

        resolutionDropdown.AddOptions(options);
    }

    void SetupQualityLevels()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        var options = new List<string>();

        if (LocalizationManager.Instance != null)
        {
            options.Add(LocalizationManager.Instance.Get("quality_very_low"));
            options.Add(LocalizationManager.Instance.Get("quality_low"));
            options.Add(LocalizationManager.Instance.Get("quality_medium"));
            options.Add(LocalizationManager.Instance.Get("quality_high"));
            options.Add(LocalizationManager.Instance.Get("quality_very_high"));
            options.Add(LocalizationManager.Instance.Get("quality_ultra"));
        }
        else
        {
            options.AddRange(new[] { "Cok Dusuk", "Dusuk", "Orta", "Yuksek", "Cok Yuksek", "Ultra" });
        }

        qualityDropdown.AddOptions(options);
    }

    void SetupColorBlindModes()
    {
        if (colorBlindDropdown == null) return;

        colorBlindDropdown.ClearOptions();
        var options = new List<string>();

        if (LocalizationManager.Instance != null)
        {
            options.Add(LocalizationManager.Instance.Get("colorblind_none"));
            options.Add(LocalizationManager.Instance.Get("colorblind_deuteranopia"));
            options.Add(LocalizationManager.Instance.Get("colorblind_protanopia"));
            options.Add(LocalizationManager.Instance.Get("colorblind_tritanopia"));
        }
        else
        {
            options.AddRange(new[] { "Normal", "Deuteranopia", "Protanopia", "Tritanopia" });
        }

        colorBlindDropdown.AddOptions(options);
    }

    void SetupLanguages()
    {
        if (languageDropdown == null) return;

        languageDropdown.ClearOptions();
        var options = new List<string> { "Turkce", "English" };
        languageDropdown.AddOptions(options);
    }

    void SetupMobileControls()
    {
        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;

        #if UNITY_EDITOR
        isMobile = true; // Editor'de test icin goster
        #endif

        if (mobileControlsPanel != null)
            mobileControlsPanel.SetActive(isMobile);

        // Slider ayarlari
        if (buttonSizeSlider != null)
        {
            buttonSizeSlider.minValue = 0.5f;
            buttonSizeSlider.maxValue = 1.5f;
        }

        if (controlsOpacitySlider != null)
        {
            controlsOpacitySlider.minValue = 0.3f;
            controlsOpacitySlider.maxValue = 1.0f;
        }

        if (joystickSensitivitySlider != null)
        {
            joystickSensitivitySlider.minValue = 0.5f;
            joystickSensitivitySlider.maxValue = 2.0f;
        }
    }

    void SetupPerformanceModes()
    {
        if (performanceModeDropdown == null) return;

        performanceModeDropdown.ClearOptions();
        var options = new List<string>();

        if (LocalizationManager.Instance != null)
        {
            options.Add(LocalizationManager.Instance.Get("perf_balanced"));
            options.Add(LocalizationManager.Instance.Get("perf_performance"));
            options.Add(LocalizationManager.Instance.Get("perf_quality"));
        }
        else
        {
            options.AddRange(new[] { "Dengeli", "Performans", "Kalite" });
        }

        performanceModeDropdown.AddOptions(options);
    }

    void LoadSettings()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
            return;

        var data = SaveManager.Instance.Data;

        // Ses
        if (masterVolumeSlider != null) masterVolumeSlider.value = data.masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = data.musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = data.sfxVolume;

        // Grafik
        if (fullscreenToggle != null) fullscreenToggle.isOn = data.fullscreen;
        if (qualityDropdown != null) qualityDropdown.value = Mathf.Clamp(data.qualityLevel, 0, 5);
        if (brightnessSlider != null) brightnessSlider.value = data.brightness;
        if (resolutionDropdown != null && data.resolutionIndex >= 0)
            resolutionDropdown.value = Mathf.Clamp(data.resolutionIndex, 0, filteredResolutions.Count - 1);

        // Kontroller
        if (sensitivitySlider != null) sensitivitySlider.value = data.aimSensitivity;
        if (vibrationToggle != null) vibrationToggle.isOn = data.vibrationEnabled;
        if (aimAssistToggle != null) aimAssistToggle.isOn = data.aimAssistEnabled;
        if (mobileEasyModeToggle != null) mobileEasyModeToggle.isOn = data.mobileEasyMode;

        // Mobil Kontrol Ayarlari
        if (buttonSizeSlider != null)
            buttonSizeSlider.value = PlayerPrefs.GetFloat("MobileButtonSize", 1f);
        if (controlsOpacitySlider != null)
            controlsOpacitySlider.value = PlayerPrefs.GetFloat("MobileControlsOpacity", 0.8f);
        if (joystickSensitivitySlider != null)
            joystickSensitivitySlider.value = PlayerPrefs.GetFloat("JoystickSensitivity", 1f);
        if (hapticFeedbackToggle != null)
            hapticFeedbackToggle.isOn = PlayerPrefs.GetInt("HapticFeedback", 1) == 1;
        if (performanceModeDropdown != null)
            performanceModeDropdown.value = PlayerPrefs.GetInt("PerformanceMode", 0);

        // Erisilebilirlik
        if (colorBlindDropdown != null) colorBlindDropdown.value = data.colorBlindMode;
        if (uiScaleSlider != null) uiScaleSlider.value = data.uiScale;
        if (screenShakeToggle != null) screenShakeToggle.isOn = data.screenShakeEnabled;

        // Dil
        if (languageDropdown != null)
            languageDropdown.value = data.language == "tr" ? 0 : 1;

        UpdateAllTexts();
    }

    void AddListeners()
    {
        // Tab butonlari
        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Audio));
        if (graphicsTabButton != null)
            graphicsTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Graphics));
        if (controlsTabButton != null)
            controlsTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Controls));
        if (accessibilityTabButton != null)
            accessibilityTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Accessibility));
        if (languageTabButton != null)
            languageTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Language));

        // Ses slider'lari
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Grafik
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);

        // Kontroller
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        if (aimAssistToggle != null)
            aimAssistToggle.onValueChanged.AddListener(OnAimAssistChanged);
        if (mobileEasyModeToggle != null)
            mobileEasyModeToggle.onValueChanged.AddListener(OnMobileEasyModeChanged);

        // Erisilebilirlik
        if (colorBlindDropdown != null)
            colorBlindDropdown.onValueChanged.AddListener(OnColorBlindChanged);
        if (uiScaleSlider != null)
            uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);
        if (screenShakeToggle != null)
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);

        // Dil
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        // Mobil Kontrol Ayarlari
        if (buttonSizeSlider != null)
            buttonSizeSlider.onValueChanged.AddListener(OnButtonSizeChanged);
        if (controlsOpacitySlider != null)
            controlsOpacitySlider.onValueChanged.AddListener(OnControlsOpacityChanged);
        if (joystickSensitivitySlider != null)
            joystickSensitivitySlider.onValueChanged.AddListener(OnJoystickSensitivityChanged);
        if (hapticFeedbackToggle != null)
            hapticFeedbackToggle.onValueChanged.AddListener(OnHapticFeedbackChanged);
        if (performanceModeDropdown != null)
            performanceModeDropdown.onValueChanged.AddListener(OnPerformanceModeChanged);

        // Genel butonlar
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyAndClose);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefaults);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    // === TAB YONETIMI ===

    public void ShowTab(SettingsTab tab)
    {
        currentTab = tab;

        // Tum panelleri gizle
        if (audioPanel != null) audioPanel.SetActive(tab == SettingsTab.Audio);
        if (graphicsPanel != null) graphicsPanel.SetActive(tab == SettingsTab.Graphics);
        if (controlsPanel != null) controlsPanel.SetActive(tab == SettingsTab.Controls);
        if (accessibilityPanel != null) accessibilityPanel.SetActive(tab == SettingsTab.Accessibility);
        if (languagePanel != null) languagePanel.SetActive(tab == SettingsTab.Language);

        // Tab buton renklerini guncelle
        UpdateTabColors();

        // Buton sesi
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    void UpdateTabColors()
    {
        SetTabColor(audioTabButton, currentTab == SettingsTab.Audio);
        SetTabColor(graphicsTabButton, currentTab == SettingsTab.Graphics);
        SetTabColor(controlsTabButton, currentTab == SettingsTab.Controls);
        SetTabColor(accessibilityTabButton, currentTab == SettingsTab.Accessibility);
        SetTabColor(languageTabButton, currentTab == SettingsTab.Language);
    }

    void SetTabColor(Button button, bool active)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = active ? activeTabColor : inactiveTabColor;
        colors.highlightedColor = active ? activeTabColor : inactiveTabColor * 1.2f;
        button.colors = colors;

        // Text rengini de ayarla
        var text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.color = active ? Color.black : Color.white;
    }

    // === CALLBACK'LER ===

    void OnMasterVolumeChanged(float value)
    {
        if (masterVolumeText != null)
            masterVolumeText.text = Mathf.RoundToInt(value * 100) + "%";

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetMasterVolume(value);
    }

    void OnMusicVolumeChanged(float value)
    {
        if (musicVolumeText != null)
            musicVolumeText.text = Mathf.RoundToInt(value * 100) + "%";

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetMusicVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
        if (sfxVolumeText != null)
            sfxVolumeText.text = Mathf.RoundToInt(value * 100) + "%";

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetSFXVolume(value);
    }

    void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetFullscreen(value);
    }

    void OnResolutionChanged(int index)
    {
        if (index >= 0 && index < filteredResolutions.Count)
        {
            var res = filteredResolutions[index];
            bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
            Screen.SetResolution(res.width, res.height, isFullscreen);

            if (SaveManager.Instance != null)
                SaveManager.Instance.SetResolution(index);
        }
    }

    void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetQualityLevel(index);
    }

    void OnBrightnessChanged(float value)
    {
        if (brightnessText != null)
            brightnessText.text = Mathf.RoundToInt(value * 100) + "%";

        if (AccessibilitySettings.Instance != null)
            AccessibilitySettings.Instance.SetBrightness(value);
    }

    void OnSensitivityChanged(float value)
    {
        if (sensitivityText != null)
            sensitivityText.text = value.ToString("F1") + "x";

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetAimSensitivity(value);
    }

    void OnVibrationChanged(bool value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SetVibration(value);
    }

    void OnAimAssistChanged(bool value)
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.aimAssistEnabled = value;
            SaveManager.Instance.SaveSettings();
        }

        // PlayerController'a da aninda yansit
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.aimAssistEnabled = value;
        }
    }

    void OnMobileEasyModeChanged(bool value)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetMobileEasyMode(value);
        }
        else if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.mobileEasyMode = value;
            SaveManager.Instance.SaveSettings();
        }
    }

    void OnColorBlindChanged(int index)
    {
        if (AccessibilitySettings.Instance != null)
            AccessibilitySettings.Instance.SetColorBlindMode((ColorBlindMode)index);
    }

    void OnUIScaleChanged(float value)
    {
        if (uiScaleText != null)
            uiScaleText.text = Mathf.RoundToInt(value * 100) + "%";

        if (AccessibilitySettings.Instance != null)
            AccessibilitySettings.Instance.SetUIScale(value);
    }

    void OnScreenShakeChanged(bool value)
    {
        if (AccessibilitySettings.Instance != null)
            AccessibilitySettings.Instance.SetScreenShake(value);
    }

    void OnLanguageChanged(int index)
    {
        string langCode = index == 0 ? "tr" : "en";

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.SetLanguage(langCode);

        // Dropdown metinlerini guncelle
        SetupQualityLevels();
        SetupColorBlindModes();
        SetupPerformanceModes();
    }

    void OnButtonSizeChanged(float value)
    {
        if (buttonSizeText != null)
            buttonSizeText.text = Mathf.RoundToInt(value * 100) + "%";

        if (MobileControls.Instance != null)
            MobileControls.Instance.SetButtonSize(value);

        PlayerPrefs.SetFloat("MobileButtonSize", value);
    }

    void OnControlsOpacityChanged(float value)
    {
        if (controlsOpacityText != null)
            controlsOpacityText.text = Mathf.RoundToInt(value * 100) + "%";

        if (MobileControls.Instance != null)
            MobileControls.Instance.SetOpacity(value);

        PlayerPrefs.SetFloat("MobileControlsOpacity", value);
    }

    void OnJoystickSensitivityChanged(float value)
    {
        if (joystickSensitivityText != null)
            joystickSensitivityText.text = value.ToString("F1") + "x";

        PlayerPrefs.SetFloat("JoystickSensitivity", value);
    }

    void OnHapticFeedbackChanged(bool value)
    {
        PlayerPrefs.SetInt("HapticFeedback", value ? 1 : 0);

        #if UNITY_ANDROID
        if (value) Handheld.Vibrate();
        #endif
    }

    void OnPerformanceModeChanged(int index)
    {
        PlayerPrefs.SetInt("PerformanceMode", index);

        switch (index)
        {
            case 0: // Dengeli
                Application.targetFrameRate = 60;
                QualitySettings.SetQualityLevel(2);
                break;
            case 1: // Performans
                Application.targetFrameRate = 30;
                QualitySettings.SetQualityLevel(0);
                break;
            case 2: // Kalite
                Application.targetFrameRate = 60;
                QualitySettings.SetQualityLevel(4);
                break;
        }
    }

    void UpdateAllTexts()
    {
        if (masterVolumeSlider != null && masterVolumeText != null)
            masterVolumeText.text = Mathf.RoundToInt(masterVolumeSlider.value * 100) + "%";
        if (musicVolumeSlider != null && musicVolumeText != null)
            musicVolumeText.text = Mathf.RoundToInt(musicVolumeSlider.value * 100) + "%";
        if (sfxVolumeSlider != null && sfxVolumeText != null)
            sfxVolumeText.text = Mathf.RoundToInt(sfxVolumeSlider.value * 100) + "%";
        if (brightnessSlider != null && brightnessText != null)
            brightnessText.text = Mathf.RoundToInt(brightnessSlider.value * 100) + "%";
        if (sensitivitySlider != null && sensitivityText != null)
            sensitivityText.text = sensitivitySlider.value.ToString("F1") + "x";
        if (uiScaleSlider != null && uiScaleText != null)
            uiScaleText.text = Mathf.RoundToInt(uiScaleSlider.value * 100) + "%";

        // Mobil Kontrol Metinleri
        if (buttonSizeSlider != null && buttonSizeText != null)
            buttonSizeText.text = Mathf.RoundToInt(buttonSizeSlider.value * 100) + "%";
        if (controlsOpacitySlider != null && controlsOpacityText != null)
            controlsOpacityText.text = Mathf.RoundToInt(controlsOpacitySlider.value * 100) + "%";
        if (joystickSensitivitySlider != null && joystickSensitivityText != null)
            joystickSensitivityText.text = joystickSensitivitySlider.value.ToString("F1") + "x";
    }

    // === PANEL KONTROLU ===

    public void Show()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        LoadSettings();
        ShowTab(SettingsTab.Audio);
    }

    public void Close()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveSettings();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    public void ApplyAndClose()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveSettings();

        Close();
    }

    public void ResetToDefaults()
    {
        // Ses
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 0.5f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.7f;

        // Grafik
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (qualityDropdown != null) qualityDropdown.value = 2;
        if (brightnessSlider != null) brightnessSlider.value = 1f;
        if (resolutionDropdown != null && filteredResolutions.Count > 0)
            resolutionDropdown.value = 0;

        // Kontroller
        if (sensitivitySlider != null) sensitivitySlider.value = 1f;
        if (vibrationToggle != null) vibrationToggle.isOn = true;
        if (aimAssistToggle != null) aimAssistToggle.isOn = true;
        if (mobileEasyModeToggle != null) mobileEasyModeToggle.isOn = false;

        // Mobil Kontrol Ayarlari
        if (buttonSizeSlider != null) buttonSizeSlider.value = 1f;
        if (controlsOpacitySlider != null) controlsOpacitySlider.value = 0.8f;
        if (joystickSensitivitySlider != null) joystickSensitivitySlider.value = 1f;
        if (hapticFeedbackToggle != null) hapticFeedbackToggle.isOn = true;
        if (performanceModeDropdown != null) performanceModeDropdown.value = 0;

        // Erisilebilirlik
        if (colorBlindDropdown != null) colorBlindDropdown.value = 0;
        if (uiScaleSlider != null) uiScaleSlider.value = 1f;
        if (screenShakeToggle != null) screenShakeToggle.isOn = true;

        // Dil
        if (languageDropdown != null) languageDropdown.value = 0;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }
}

/// <summary>
/// Ayarlar sekme turleri
/// </summary>
public enum SettingsTab
{
    Audio,
    Graphics,
    Controls,
    Accessibility,
    Language
}
