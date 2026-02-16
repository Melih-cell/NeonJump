using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI titleText;

    [Header("Settings Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Animation")]
    public float titleBounceSpeed = 2f;
    public float titleBounceAmount = 10f;
    public float neonPulseSpeed = 3f;

    private Vector3 titleStartPos;
    private float neonTime = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Sadece MainMenu sahnesinde UI olustur
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "MainMenu")
        {
            // Bu sahne MainMenu degil, kendini yok et
            Destroy(gameObject);
            return;
        }

        // UI yoksa runtime'da olustur
        if (mainMenuPanel == null)
        {
            CreateNeonUI();
        }

        // High score goster
        UpdateHighScoreDisplay();

        // Title animasyonu icin baslangic pozisyonu
        if (titleText != null)
        {
            titleStartPos = titleText.rectTransform.anchoredPosition;
        }

        // Settings panelini gizle
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Volume degerlerini yukle
        LoadVolumeSettings();

        // Menu muzigini baslat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }
    }

    void Update()
    {
        neonTime += Time.deltaTime;

        // Title bounce ve neon pulse animasyonu
        if (titleText != null)
        {
            float newY = titleStartPos.y + Mathf.Sin(Time.time * titleBounceSpeed) * titleBounceAmount;
            titleText.rectTransform.anchoredPosition = new Vector2(titleStartPos.x, newY);

            // Neon glow pulse
            float pulse = (Mathf.Sin(neonTime * neonPulseSpeed) + 1f) * 0.5f;
            Color neonColor = Color.Lerp(new Color(0f, 0.8f, 1f), new Color(1f, 0f, 1f), pulse);
            titleText.color = neonColor;
        }
    }

    void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            highScoreText.text = "EN YUKSEK SKOR: " + highScore.ToString("N0");
        }
    }

    void LoadVolumeSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.7f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = master;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = music;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfx;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // AudioManager'a uygula
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(master);
            AudioManager.Instance.SetMusicVolume(music);
            AudioManager.Instance.SetSFXVolume(sfx);
        }
    }

    // === BUTTON FONKSIYONLARI ===

    public void PlayGame()
    {
        PlayButtonSound();
        SceneManager.LoadScene("1");
    }

    public void OpenSettings()
    {
        PlayButtonSound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenUpgrades()
    {
        PlayButtonSound();
        if (UpgradeManager.Instance != null)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            UpgradeManager.Instance.ShowUpgradePanel();
        }
        else
        {
            Debug.LogWarning("UpgradeManager bulunamadi!");
        }
    }

    public void CloseUpgrades()
    {
        PlayButtonSound();
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.HideUpgradePanel();
        }
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayButtonSound();
        SaveVolumeSettings();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        PlayButtonSound();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ResetHighScore()
    {
        PlayButtonSound();
        PlayerPrefs.SetInt("HighScore", 0);
        PlayerPrefs.Save();
        UpdateHighScoreDisplay();
    }

    void PlayButtonSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    // === VOLUME KONTROL ===

    public void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    void SaveVolumeSettings()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        PlayerPrefs.Save();
    }

    // === RUNTIME UI OLUSTURMA ===

    void CreateNeonUI()
    {
        // Canvas bul veya olustur
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Arka plan
        CreateBackground(canvas.transform);

        // Ana Menu Paneli
        mainMenuPanel = CreatePanel(canvas.transform, "MainMenuPanel");

        // Title
        titleText = CreateNeonText(mainMenuPanel.transform, "NEON JUMP", 72, new Vector2(0, 150));

        // High Score
        highScoreText = CreateNeonText(mainMenuPanel.transform, "EN YUKSEK SKOR: 0", 24, new Vector2(0, 80));
        highScoreText.color = new Color(1f, 0.8f, 0f);

        // Butonlar
        CreateNeonButton(mainMenuPanel.transform, "OYNA", new Vector2(0, 0), PlayGame);
        CreateNeonButton(mainMenuPanel.transform, "YUKSELTMELER", new Vector2(0, -70), OpenUpgrades);
        CreateNeonButton(mainMenuPanel.transform, "AYARLAR", new Vector2(0, -140), OpenSettings);
        CreateNeonButton(mainMenuPanel.transform, "CIKIS", new Vector2(0, -210), QuitGame);

        // Settings Paneli
        settingsPanel = CreatePanel(canvas.transform, "SettingsPanel");
        settingsPanel.SetActive(false);

        CreateNeonText(settingsPanel.transform, "AYARLAR", 48, new Vector2(0, 150));

        // Volume Sliders
        masterVolumeSlider = CreateNeonSlider(settingsPanel.transform, "ANA SES", new Vector2(0, 50));
        musicVolumeSlider = CreateNeonSlider(settingsPanel.transform, "MUZIK", new Vector2(0, -20));
        sfxVolumeSlider = CreateNeonSlider(settingsPanel.transform, "EFEKT", new Vector2(0, -90));

        // Geri butonu
        CreateNeonButton(settingsPanel.transform, "GERI", new Vector2(0, -180), CloseSettings);

        // Baslangic pozisyonunu kaydet
        titleStartPos = titleText.rectTransform.anchoredPosition;

        // Volume listenerlarini ekle
        LoadVolumeSettings();
    }

    void CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        RectTransform rt = bg.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.08f);
    }

    GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    TextMeshProUGUI CreateNeonText(Transform parent, string text, int fontSize, Vector2 position)
    {
        GameObject textObj = new GameObject("Text_" + text);
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(600, fontSize + 20);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0f, 1f, 1f);
        tmp.fontStyle = FontStyles.Bold;

        return tmp;
    }

    void CreateNeonButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Button_" + text);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;

        // Mobilde minimum 48dp buton yuksekligi
        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;
        float minHeight = isMobile ? Mathf.Max(60f, 48f * (Screen.dpi > 0 ? Screen.dpi / 160f : 1f)) : 50f;
        float btnWidth = isMobile ? 300f : 250f;
        rt.sizeDelta = new Vector2(btnWidth, minHeight);

        // Arka plan
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);

        // Border efekti
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        // Button
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.1f, 0.1f, 0.2f);
        colors.highlightedColor = new Color(0f, 0.3f, 0.4f);
        colors.pressedColor = new Color(0f, 0.5f, 0.6f);
        colors.selectedColor = new Color(0f, 0.3f, 0.4f);
        btn.colors = colors;

        btn.onClick.AddListener(action);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0f, 1f, 1f);
        tmp.fontStyle = FontStyles.Bold;
    }

    Slider CreateNeonSlider(Transform parent, string label, Vector2 position)
    {
        // Container
        GameObject container = new GameObject("Slider_" + label);
        container.transform.SetParent(parent, false);

        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.pivot = new Vector2(0.5f, 0.5f);
        containerRt.anchoredPosition = position;
        containerRt.sizeDelta = new Vector2(350, 50);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);

        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0.5f);
        labelRt.anchorMax = new Vector2(0, 0.5f);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.anchoredPosition = new Vector2(0, 0);
        labelRt.sizeDelta = new Vector2(100, 30);

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 20;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = new Color(0.8f, 0.8f, 0.8f);

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(container.transform, false);

        RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(1, 0.5f);
        sliderRt.anchorMax = new Vector2(1, 0.5f);
        sliderRt.pivot = new Vector2(1, 0.5f);
        sliderRt.anchoredPosition = new Vector2(0, 0);
        sliderRt.sizeDelta = new Vector2(200, 20);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.25f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5, 0);
        fillAreaRt.offsetMax = new Vector2(-5, 0);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0f, 1f, 1f);

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10, 0);
        handleAreaRt.offsetMax = new Vector2(-10, 0);

        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 30);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(1f, 0f, 1f);

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        return slider;
    }
}
