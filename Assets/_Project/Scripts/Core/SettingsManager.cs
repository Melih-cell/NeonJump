using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Graphics")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;

    [Header("UI References")]
    public GameObject settingsPanel;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private bool isInitialized = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeSettings();
    }

    public void InitializeSettings()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Cozunurlukleri al
        SetupResolutions();

        // Kalite seviyelerini ayarla
        SetupQualityLevels();

        // Kayitli ayarlari yukle
        LoadSettings();

        // Listener'lari ekle
        AddListeners();
    }

    void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        // Tekrar eden cozunurlukleri filtrele (ayni boyut, farkli refresh rate)
        HashSet<string> addedResolutions = new HashSet<string>();
        List<string> options = new List<string>();

        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            string key = $"{resolutions[i].width}x{resolutions[i].height}";
            if (!addedResolutions.Contains(key))
            {
                addedResolutions.Add(key);
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
        List<string> options = new List<string>
        {
            "Cok Dusuk",
            "Dusuk",
            "Orta",
            "Yuksek",
            "Cok Yuksek",
            "Ultra"
        };
        qualityDropdown.AddOptions(options);
    }

    void AddListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    void LoadSettings()
    {
        // SaveManager varsa ondan yukle
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            var data = SaveManager.Instance.Data;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = data.masterVolume;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = data.musicVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = data.sfxVolume;

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = data.fullscreen;

            if (qualityDropdown != null)
                qualityDropdown.value = Mathf.Clamp(data.qualityLevel, 0, qualityDropdown.options.Count - 1);

            if (resolutionDropdown != null && data.resolutionIndex >= 0)
                resolutionDropdown.value = Mathf.Clamp(data.resolutionIndex, 0, resolutionDropdown.options.Count - 1);

            // AudioManager'a uygula
            ApplyAudioSettings();
        }
        else
        {
            // PlayerPrefs'ten yukle (fallback)
            LoadFromPlayerPrefs();
        }
    }

    void LoadFromPlayerPrefs()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.7f);

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        if (qualityDropdown != null)
            qualityDropdown.value = PlayerPrefs.GetInt("Quality", 2);

        ApplyAudioSettings();
    }

    void ApplyAudioSettings()
    {
        if (AudioManager.Instance != null)
        {
            if (masterVolumeSlider != null)
                AudioManager.Instance.SetMasterVolume(masterVolumeSlider.value);

            if (musicVolumeSlider != null)
                AudioManager.Instance.SetMusicVolume(musicVolumeSlider.value);

            if (sfxVolumeSlider != null)
                AudioManager.Instance.SetSFXVolume(sfxVolumeSlider.value);
        }
    }

    // === CALLBACK'LER ===

    void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetMasterVolume(value);
    }

    void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetMusicVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
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
            Resolution res = filteredResolutions[index];
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

    // === PANEL KONTROLU ===

    public void ShowSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        // Ayarlari yeniden yukle
        LoadSettings();
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Ayarlari kaydet
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveSettings();
    }

    public void ApplyAndClose()
    {
        // Tum ayarlari kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveSettings();
        }
        else
        {
            // PlayerPrefs'e kaydet (fallback)
            SaveToPlayerPrefs();
        }

        HideSettings();
    }

    void SaveToPlayerPrefs()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);

        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        if (qualityDropdown != null)
            PlayerPrefs.SetInt("Quality", qualityDropdown.value);

        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = 1f;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = 0.5f;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = 0.7f;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = true;

        if (qualityDropdown != null)
            qualityDropdown.value = 2; // Orta

        // Ilk cozunurluk (en yuksek)
        if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
            resolutionDropdown.value = 0;

        ApplyAudioSettings();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }
}
