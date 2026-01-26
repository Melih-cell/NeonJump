using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinText;
    public Transform heartsContainer;
    public GameObject heartPrefab;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GameObject pausePanel;

    [Header("Game Over Panel")]
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverHighScoreText;

    [Header("Win Panel")]
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI winTimeText;

    [Header("Animation")]
    public float panelFadeTime = 0.5f;
    public float neonPulseSpeed = 2f;

    [Header("Boss Health Bar")]
    private GameObject bossHealthBarPanel;
    private TextMeshProUGUI bossNameText;
    private Image bossHealthFill;
    private int bossMaxHealth;

    [Header("Power-Up Indicators")]
    private GameObject powerUpContainer;
    private System.Collections.Generic.Dictionary<PowerUpType, GameObject> powerUpIndicators =
        new System.Collections.Generic.Dictionary<PowerUpType, GameObject>();

    // Neon HUD referanslari
    private GameObject hudPanel;
    private TextMeshProUGUI scoreLabelText;
    private Image coinIcon;
    private float neonTime = 0f;

    // Combo UI
    private GameObject comboContainer;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI comboMultiplierText;
    private Image comboTimerBar;
    private float comboDisplayTimer = 0f;

    private float gameStartTime;
    private bool isPaused = false;

    void Awake()
    {
        Instance = this;
        gameStartTime = Time.time;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        // HUD yoksa runtime'da olustur
        if (scoreText == null)
        {
            CreateNeonHUD();
        }

        // Panelleri gizle
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Baslangic UI
        UpdateScore(0);
        UpdateCoins(0);

        // Oyun muzigini baslat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }
    }

    void Update()
    {
        neonTime += Time.unscaledDeltaTime;

        // Neon pulse animasyonu
        if (scoreLabelText != null)
        {
            float pulse = (Mathf.Sin(neonTime * neonPulseSpeed) + 1f) * 0.5f;
            scoreLabelText.color = Color.Lerp(new Color(0f, 0.8f, 1f), new Color(0f, 1f, 1f), pulse);
        }

        // Pause kontrolu - Yeni Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0");
        }
    }

    public void UpdateCoins(int coins)
    {
        if (coinText != null)
        {
            coinText.text = "x " + coins.ToString();
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (heartsContainer == null) return;

        // Mevcut kalpleri temizle
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }

        // Yeni kalpleri olustur
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = CreateHeart();
            heart.transform.SetParent(heartsContainer, false);

            // Dolu veya bos kalp
            Image heartImage = heart.GetComponent<Image>();
            if (heartImage != null)
            {
                if (i < currentHealth)
                    heartImage.color = Color.red;
                else
                    heartImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }

    GameObject CreateHeart()
    {
        GameObject heart = new GameObject("Heart");
        Image img = heart.AddComponent<Image>();
        img.color = Color.red;

        // Neon glow efekti
        Outline outline = heart.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        RectTransform rt = heart.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 40);

        return heart;
    }

    // === NEON HUD OLUSTURMA ===

    void CreateNeonHUD()
    {
        // Canvas bul veya olustur
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // HUD Panel
        hudPanel = new GameObject("HUDPanel");
        hudPanel.transform.SetParent(canvas.transform, false);
        RectTransform hudRt = hudPanel.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero;
        hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero;
        hudRt.offsetMax = Vector2.zero;

        // === SOL UST - SKOR ===
        GameObject scoreContainer = CreateHUDContainer(hudPanel.transform, "ScoreContainer",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(200, 60));

        // Skor label
        scoreLabelText = CreateNeonText(scoreContainer.transform, "SKOR", 18,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -5));
        scoreLabelText.color = new Color(0f, 0.8f, 1f);

        // Skor degeri
        scoreText = CreateNeonText(scoreContainer.transform, "0", 32,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 10));
        scoreText.color = Color.white;
        scoreText.alignment = TextAlignmentOptions.BottomLeft;

        // === SAG UST - COIN ===
        GameObject coinContainer = CreateHUDContainer(hudPanel.transform, "CoinContainer",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(150, 50));

        // Coin icon
        GameObject coinIconObj = new GameObject("CoinIcon");
        coinIconObj.transform.SetParent(coinContainer.transform, false);
        RectTransform coinIconRt = coinIconObj.AddComponent<RectTransform>();
        coinIconRt.anchorMin = coinIconRt.anchorMax = new Vector2(0, 0.5f);
        coinIconRt.pivot = new Vector2(0, 0.5f);
        coinIconRt.anchoredPosition = new Vector2(10, 0);
        coinIconRt.sizeDelta = new Vector2(35, 35);
        coinIcon = coinIconObj.AddComponent<Image>();
        coinIcon.color = new Color(1f, 0.85f, 0f);
        Outline coinOutline = coinIconObj.AddComponent<Outline>();
        coinOutline.effectColor = new Color(1f, 0.5f, 0f, 0.8f);
        coinOutline.effectDistance = new Vector2(2, 2);

        // Coin text
        coinText = CreateNeonText(coinContainer.transform, "x 0", 28,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(55, 0));
        coinText.color = new Color(1f, 0.85f, 0f);
        coinText.alignment = TextAlignmentOptions.Left;

        // === SOL UST (skor altinda) - CANLAR ===
        GameObject heartsObj = new GameObject("HeartsContainer");
        heartsObj.transform.SetParent(hudPanel.transform, false);
        RectTransform heartsRt = heartsObj.AddComponent<RectTransform>();
        heartsRt.anchorMin = heartsRt.anchorMax = new Vector2(0, 1);
        heartsRt.pivot = new Vector2(0, 1);
        heartsRt.anchoredPosition = new Vector2(20, -90);
        heartsRt.sizeDelta = new Vector2(200, 50);

        HorizontalLayoutGroup hlg = heartsObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        heartsContainer = heartsObj.transform;

        // === EKRAN ORTASI UST - COMBO ===
        CreateComboUI(hudPanel.transform);

        // === PANELLER ===
        CreateGameOverPanel(canvas.transform);
        CreateWinPanel(canvas.transform);
        CreatePausePanel(canvas.transform);
    }

    void CreateComboUI(Transform parent)
    {
        // Combo container - ekranin ust ortasinda
        comboContainer = new GameObject("ComboContainer");
        comboContainer.transform.SetParent(parent, false);
        RectTransform comboRt = comboContainer.AddComponent<RectTransform>();
        comboRt.anchorMin = comboRt.anchorMax = new Vector2(0.5f, 1);
        comboRt.pivot = new Vector2(0.5f, 1);
        comboRt.anchoredPosition = new Vector2(0, -20);
        comboRt.sizeDelta = new Vector2(200, 80);

        // Combo text (ana yazi)
        comboText = CreateNeonText(comboContainer.transform, "", 36,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -5));
        comboText.alignment = TextAlignmentOptions.Top;
        comboText.color = new Color(1f, 0.5f, 0f); // Turuncu

        // Combo multiplier
        comboMultiplierText = CreateNeonText(comboContainer.transform, "", 24,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10));
        comboMultiplierText.alignment = TextAlignmentOptions.Bottom;
        comboMultiplierText.color = new Color(1f, 1f, 0f); // Sari

        // Combo timer bar
        GameObject timerBarBg = new GameObject("ComboTimerBg");
        timerBarBg.transform.SetParent(comboContainer.transform, false);
        RectTransform timerBgRt = timerBarBg.AddComponent<RectTransform>();
        timerBgRt.anchorMin = new Vector2(0.1f, 0);
        timerBgRt.anchorMax = new Vector2(0.9f, 0);
        timerBgRt.pivot = new Vector2(0.5f, 0);
        timerBgRt.anchoredPosition = new Vector2(0, 5);
        timerBgRt.sizeDelta = new Vector2(0, 6);
        Image timerBgImg = timerBarBg.AddComponent<Image>();
        timerBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        GameObject timerBar = new GameObject("ComboTimerBar");
        timerBar.transform.SetParent(timerBarBg.transform, false);
        RectTransform timerRt = timerBar.AddComponent<RectTransform>();
        timerRt.anchorMin = Vector2.zero;
        timerRt.anchorMax = Vector2.one;
        timerRt.offsetMin = Vector2.zero;
        timerRt.offsetMax = Vector2.zero;
        timerRt.pivot = new Vector2(0, 0.5f);
        comboTimerBar = timerBar.AddComponent<Image>();
        comboTimerBar.color = new Color(1f, 0.5f, 0f); // Turuncu

        // Baslangicta gizle
        comboContainer.SetActive(false);
    }

    GameObject CreateHUDContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMax;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        // Arka plan
        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.15f, 0.7f);

        // Neon border
        Outline outline = container.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0.5f);
        outline.effectDistance = new Vector2(1, 1);

        return container;
    }

    TextMeshProUGUI CreateNeonText(Transform parent, string text, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
    {
        GameObject textObj = new GameObject("Text_" + text);
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(180, fontSize + 10);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return tmp;
    }

    void CreateGameOverPanel(Transform parent)
    {
        GameObject outerPanel;
        GameObject innerPanel;
        CreateNeonPanel(parent, "GameOverPanel", out outerPanel, out innerPanel);
        gameOverPanel = outerPanel;

        // Title
        TextMeshProUGUI title = CreatePanelText(innerPanel.transform, "OYUN BITTI", 48, new Vector2(0, 80));
        title.color = new Color(1f, 0.3f, 0.3f);

        // Skor
        gameOverScoreText = CreatePanelText(innerPanel.transform, "Skor: 0", 32, new Vector2(0, 20));

        // High Score
        gameOverHighScoreText = CreatePanelText(innerPanel.transform, "En Yuksek: 0", 24, new Vector2(0, -20));
        gameOverHighScoreText.color = new Color(1f, 0.8f, 0f);

        // Butonlar
        CreateNeonButton(innerPanel.transform, "TEKRAR OYNA", new Vector2(0, -80), RestartGame);
        CreateNeonButton(innerPanel.transform, "ANA MENU", new Vector2(0, -140), GoToMainMenu);

        gameOverPanel.SetActive(false);
    }

    void CreateWinPanel(Transform parent)
    {
        GameObject outerPanel;
        GameObject innerPanel;
        CreateNeonPanel(parent, "WinPanel", out outerPanel, out innerPanel);
        winPanel = outerPanel;

        // Title
        TextMeshProUGUI title = CreatePanelText(innerPanel.transform, "TEBRIKLER!", 48, new Vector2(0, 80));
        title.color = new Color(0.3f, 1f, 0.3f);

        // Skor
        winScoreText = CreatePanelText(innerPanel.transform, "Skor: 0", 32, new Vector2(0, 20));

        // Sure
        winTimeText = CreatePanelText(innerPanel.transform, "Sure: 00:00", 24, new Vector2(0, -20));
        winTimeText.color = new Color(0f, 0.8f, 1f);

        // Butonlar
        CreateNeonButton(innerPanel.transform, "TEKRAR OYNA", new Vector2(0, -80), RestartGame);
        CreateNeonButton(innerPanel.transform, "ANA MENU", new Vector2(0, -140), GoToMainMenu);

        winPanel.SetActive(false);
    }

    void CreatePausePanel(Transform parent)
    {
        GameObject outerPanel;
        GameObject innerPanel;
        CreateNeonPanel(parent, "PausePanel", out outerPanel, out innerPanel);
        pausePanel = outerPanel;

        // Title
        TextMeshProUGUI title = CreatePanelText(innerPanel.transform, "DURAKLATILDI", 48, new Vector2(0, 80));
        title.color = new Color(1f, 1f, 0f);

        // Butonlar
        CreateNeonButton(innerPanel.transform, "DEVAM ET", new Vector2(0, 0), ResumeGame);
        CreateNeonButton(innerPanel.transform, "TEKRAR BASLAT", new Vector2(0, -60), RestartGame);
        CreateNeonButton(innerPanel.transform, "ANA MENU", new Vector2(0, -120), GoToMainMenu);

        pausePanel.SetActive(false);
    }

    void CreateNeonPanel(Transform parent, string name, out GameObject outerPanel, out GameObject innerPanel)
    {
        outerPanel = new GameObject(name);
        outerPanel.transform.SetParent(parent, false);

        RectTransform rt = outerPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Karartma arka plan
        Image bg = outerPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Ic panel
        innerPanel = new GameObject("InnerPanel");
        innerPanel.transform.SetParent(outerPanel.transform, false);

        RectTransform innerRt = innerPanel.AddComponent<RectTransform>();
        innerRt.anchorMin = innerRt.anchorMax = new Vector2(0.5f, 0.5f);
        innerRt.pivot = new Vector2(0.5f, 0.5f);
        innerRt.anchoredPosition = Vector2.zero;
        innerRt.sizeDelta = new Vector2(400, 350);

        Image innerBg = innerPanel.AddComponent<Image>();
        innerBg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

        Outline outline = innerPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0.8f);
        outline.effectDistance = new Vector2(3, 3);
    }

    TextMeshProUGUI CreatePanelText(Transform parent, string text, int fontSize, Vector2 position)
    {
        GameObject textObj = new GameObject("Text_" + text);
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(380, fontSize + 20);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
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
        rt.sizeDelta = new Vector2(220, 45);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.25f, 0.9f);

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0.7f);
        outline.effectDistance = new Vector2(2, 2);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.1f, 0.1f, 0.25f);
        colors.highlightedColor = new Color(0f, 0.3f, 0.4f);
        colors.pressedColor = new Color(0f, 0.5f, 0.6f);
        colors.selectedColor = new Color(0f, 0.3f, 0.4f);
        btn.colors = colors;

        btn.onClick.AddListener(action);
        btn.onClick.AddListener(() => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        });

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
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0f, 1f, 1f);
        tmp.fontStyle = FontStyles.Bold;
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverScoreText != null)
                gameOverScoreText.text = "Skor: " + finalScore.ToString("N0");

            // High score kontrolu
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (finalScore > highScore)
            {
                highScore = finalScore;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
            }

            if (gameOverHighScoreText != null)
                gameOverHighScoreText.text = "En Yuksek: " + highScore.ToString("N0");

            StartCoroutine(AnimatePanel(gameOverPanel));
        }

        Time.timeScale = 0f;
    }

    public void ShowWin(int finalScore)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);

            if (winScoreText != null)
                winScoreText.text = "Skor: " + finalScore.ToString("N0");

            float gameTime = Time.time - gameStartTime;
            if (winTimeText != null)
                winTimeText.text = "Sure: " + FormatTime(gameTime);

            // High score kontrolu
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (finalScore > highScore)
            {
                PlayerPrefs.SetInt("HighScore", finalScore);
                PlayerPrefs.Save();
            }

            StartCoroutine(AnimatePanel(winPanel));
        }

        Time.timeScale = 0f;
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    IEnumerator AnimatePanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        float elapsed = 0;

        while (elapsed < panelFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = elapsed / panelFadeTime;
            yield return null;
        }

        cg.alpha = 1;
    }

    public void TogglePause()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        isPaused = !isPaused;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    // Button fonksiyonlari
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // === BOSS HEALTH BAR ===

    public void ShowBossHealthBar(string bossName, int maxHealth)
    {
        bossMaxHealth = maxHealth;

        // Canvas bul
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Panel olustur
        bossHealthBarPanel = new GameObject("BossHealthBarPanel");
        bossHealthBarPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRT = bossHealthBarPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 1f);
        panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0, -100);
        panelRT.sizeDelta = new Vector2(400, 60);

        // Arka plan
        Image bg = bossHealthBarPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Boss adi
        GameObject nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(bossHealthBarPanel.transform, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.5f, 1f);
        nameRT.anchorMax = new Vector2(0.5f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0, -5);
        nameRT.sizeDelta = new Vector2(380, 25);

        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
        bossNameText.text = bossName;
        bossNameText.fontSize = 20;
        bossNameText.alignment = TextAlignmentOptions.Center;
        bossNameText.color = Color.red;
        bossNameText.fontStyle = FontStyles.Bold;

        // Health bar arka plan
        GameObject healthBg = new GameObject("HealthBarBg");
        healthBg.transform.SetParent(bossHealthBarPanel.transform, false);
        RectTransform healthBgRT = healthBg.AddComponent<RectTransform>();
        healthBgRT.anchorMin = new Vector2(0.5f, 0f);
        healthBgRT.anchorMax = new Vector2(0.5f, 0f);
        healthBgRT.pivot = new Vector2(0.5f, 0f);
        healthBgRT.anchoredPosition = new Vector2(0, 8);
        healthBgRT.sizeDelta = new Vector2(380, 20);
        Image healthBgImg = healthBg.AddComponent<Image>();
        healthBgImg.color = new Color(0.3f, 0f, 0f);

        // Health bar fill
        GameObject healthFill = new GameObject("HealthBarFill");
        healthFill.transform.SetParent(healthBg.transform, false);
        RectTransform healthFillRT = healthFill.AddComponent<RectTransform>();
        healthFillRT.anchorMin = new Vector2(0f, 0f);
        healthFillRT.anchorMax = new Vector2(1f, 1f);
        healthFillRT.pivot = new Vector2(0f, 0.5f);
        healthFillRT.offsetMin = new Vector2(2, 2);
        healthFillRT.offsetMax = new Vector2(-2, -2);

        bossHealthFill = healthFill.AddComponent<Image>();
        bossHealthFill.color = Color.red;
    }

    public void UpdateBossHealth(int currentHealth)
    {
        if (bossHealthFill == null) return;

        float healthPercent = (float)currentHealth / bossMaxHealth;
        bossHealthFill.rectTransform.anchorMax = new Vector2(healthPercent, 1f);

        // Renk degisimi
        if (healthPercent > 0.66f)
            bossHealthFill.color = Color.red;
        else if (healthPercent > 0.33f)
            bossHealthFill.color = new Color(1f, 0.5f, 0f); // Turuncu
        else
            bossHealthFill.color = new Color(1f, 0f, 0f); // Koyu kirmizi
    }

    public void HideBossHealthBar()
    {
        if (bossHealthBarPanel != null)
        {
            Destroy(bossHealthBarPanel);
        }
    }

    // === POWER-UP INDICATORS ===

    public void ShowPowerUpIndicator(PowerUpType type, float duration)
    {
        // Container yoksa olustur
        if (powerUpContainer == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            powerUpContainer = new GameObject("PowerUpContainer");
            powerUpContainer.transform.SetParent(canvas.transform, false);
            RectTransform containerRT = powerUpContainer.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0f, 0.5f);
            containerRT.anchorMax = new Vector2(0f, 0.5f);
            containerRT.pivot = new Vector2(0f, 0.5f);
            containerRT.anchoredPosition = new Vector2(20, 0);
            containerRT.sizeDelta = new Vector2(200, 300);

            VerticalLayoutGroup vlg = powerUpContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
        }

        // Zaten varsa guncelle
        if (powerUpIndicators.ContainsKey(type))
        {
            // Timer'i sifirla
            return;
        }

        // Yeni indicator olustur
        GameObject indicator = new GameObject("PowerUp_" + type.ToString());
        indicator.transform.SetParent(powerUpContainer.transform, false);

        RectTransform rt = indicator.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150, 35);

        Image bg = indicator.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        // Icon
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(indicator.transform, false);
        RectTransform iconRT = icon.AddComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(5, 0);
        iconRT.sizeDelta = new Vector2(25, 25);

        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = GetPowerUpColor(type);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(indicator.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = textRT.anchorMax = textRT.pivot = new Vector2(0f, 0.5f);
        textRT.anchoredPosition = new Vector2(35, 0);
        textRT.sizeDelta = new Vector2(110, 30);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = GetPowerUpName(type);
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = GetPowerUpColor(type);

        powerUpIndicators[type] = indicator;

        // Timer ile otomatik kaldir
        if (duration < 900f) // Shield icin cok uzun sure
        {
            StartCoroutine(RemovePowerUpIndicatorAfterDelay(type, duration));
        }
    }

    IEnumerator RemovePowerUpIndicatorAfterDelay(PowerUpType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePowerUpIndicator(type);
    }

    public void HidePowerUpIndicator(PowerUpType type)
    {
        if (powerUpIndicators.ContainsKey(type))
        {
            Destroy(powerUpIndicators[type]);
            powerUpIndicators.Remove(type);
        }
    }

    Color GetPowerUpColor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.SpeedBoost: return new Color(0f, 0.8f, 1f);
            case PowerUpType.DoubleJump: return new Color(0.5f, 1f, 0.5f);
            case PowerUpType.Shield: return new Color(0.3f, 0.3f, 1f);
            case PowerUpType.Magnet: return new Color(1f, 0.8f, 0f);
            case PowerUpType.Invincibility: return new Color(1f, 1f, 0f);
            default: return Color.white;
        }
    }

    string GetPowerUpName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.SpeedBoost: return "HIZ";
            case PowerUpType.DoubleJump: return "CIFT ZIPLAMA";
            case PowerUpType.Shield: return "KALKAN";
            case PowerUpType.Magnet: return "MIKNATIS";
            case PowerUpType.Invincibility: return "DOKUNULMAZLIK";
            default: return type.ToString();
        }
    }

    // === COMBO UI ===
    public void UpdateCombo(int combo, int multiplier, float timerPercent)
    {
        if (comboContainer == null) return;

        if (combo <= 0)
        {
            comboContainer.SetActive(false);
            return;
        }

        comboContainer.SetActive(true);

        // Combo sayisi
        if (comboText != null)
        {
            comboText.text = combo + " HIT!";

            // Combo buyudukce renk degisir
            if (combo >= 10)
                comboText.color = new Color(1f, 0f, 0.5f); // Pembe
            else if (combo >= 5)
                comboText.color = new Color(1f, 0.3f, 0f); // Kirmizi-turuncu
            else
                comboText.color = new Color(1f, 0.5f, 0f); // Turuncu
        }

        // Multiplier
        if (comboMultiplierText != null)
        {
            if (multiplier > 1)
            {
                comboMultiplierText.text = "x" + multiplier + " SKOR!";
                comboMultiplierText.gameObject.SetActive(true);
            }
            else
            {
                comboMultiplierText.gameObject.SetActive(false);
            }
        }

        // Timer bar
        if (comboTimerBar != null)
        {
            comboTimerBar.rectTransform.anchorMax = new Vector2(timerPercent, 1);
        }
    }

    public void ShowComboText(string text, Vector3 worldPos)
    {
        // Ekranda kayan combo yazisi
        StartCoroutine(ShowFloatingComboText(text, worldPos));
    }

    IEnumerator ShowFloatingComboText(string text, Vector3 worldPos)
    {
        // Canvas bul
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;

        // Floating text olustur
        GameObject floatText = new GameObject("FloatingCombo");
        floatText.transform.SetParent(canvas.transform, false);

        RectTransform rt = floatText.AddComponent<RectTransform>();

        // World pozisyonunu screen pozisyonuna cevir
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        rt.position = screenPos;

        TextMeshProUGUI tmp = floatText.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 0f); // Parlak sari

        // Outline ekle
        Outline outline = floatText.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.5f, 0f);
        outline.effectDistance = new Vector2(2, 2);

        // Animasyon - yukari kayarak solma
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startPos = rt.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Yukari kay
            rt.position = startPos + Vector3.up * (t * 100f);

            // Buyut sonra kucult
            float scale = t < 0.2f ? Mathf.Lerp(0.5f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.8f);
            rt.localScale = Vector3.one * scale;

            // Sol
            tmp.color = new Color(1f, 1f, 0f, 1f - t);

            yield return null;
        }

        Destroy(floatText);
    }
}
