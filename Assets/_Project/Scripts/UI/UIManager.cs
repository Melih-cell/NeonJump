using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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
        // Panelleri olustur (null ise)
        if (gameOverPanel == null || winPanel == null || pausePanel == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (gameOverPanel == null) CreateGameOverPanel(canvas.transform);
            if (winPanel == null) CreateWinPanel(canvas.transform);
            if (pausePanel == null) CreatePausePanel(canvas.transform);
        }

        // Panelleri gizle
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Oyun muzigini baslat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }
    }

    void Update()
    {
        // Pause kontrolu - Yeni Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // === HUD FORWARDING (NeonHUD handles all HUD rendering) ===

    public void UpdateScore(int score)
    {
        // NeonHUD reads score directly from GameManager
    }

    public void UpdateCoins(int coins)
    {
        // NeonHUD'a coin degisikligini bildir
        if (NeonHUD.Instance != null)
            NeonHUD.Instance.RefreshCoins();
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        // NeonHUD reads health directly from GameManager
    }

    public void UpdateCombo(int combo, int multiplier, float timerPercent)
    {
        // NeonHUD reads combo directly from ComboManager
    }

    public void ShowComboText(string text, Vector3 worldPos)
    {
        // NeonHUD handles combo display
    }

    public void ShowBossHealthBar(string bossName, int maxHealth)
    {
        if (NeonHUD.Instance != null)
            NeonHUD.Instance.ShowBossHealth(bossName, maxHealth);
    }

    public void UpdateBossHealth(int currentHealth)
    {
        if (NeonHUD.Instance != null)
            NeonHUD.Instance.UpdateBossHealth(currentHealth);
    }

    public void HideBossHealthBar()
    {
        if (NeonHUD.Instance != null)
            NeonHUD.Instance.HideBossHealth();
    }

    public void ShowPowerUpIndicator(PowerUpType type, float duration)
    {
        if (NeonHUD.Instance != null)
            NeonHUD.Instance.ShowPowerUp(type, duration);
    }

    public void HidePowerUpIndicator(PowerUpType type)
    {
        if (NeonHUD.Instance != null)
            NeonHUD.Instance.HidePowerUp(type);
    }

    // === PANEL CREATION ===

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
        gameOverHighScoreText.color = new Color(0.92f, 0.78f, 0.35f);

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
        winTimeText.color = new Color(0.45f, 0.72f, 0.88f);

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
        title.color = new Color(0.9f, 0.8f, 0.55f);

        // Butonlar
        CreateNeonButton(innerPanel.transform, "DEVAM ET", new Vector2(0, 0), ResumeGame);
        CreateNeonButton(innerPanel.transform, "TEKRAR BASLAT", new Vector2(0, -60), RestartGame);
        CreateNeonButton(innerPanel.transform, "ANA MENU", new Vector2(0, -120), GoToMainMenu);

        pausePanel.SetActive(false);
    }

    // === PANEL HELPERS ===

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
        innerBg.color = new Color(0.12f, 0.14f, 0.10f, 0.95f);

        Outline outline = innerPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.8f, 0.55f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);
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
        img.color = new Color(0.18f, 0.22f, 0.15f, 0.9f);

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.8f, 0.55f, 0.5f);
        outline.effectDistance = new Vector2(1, 1);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.18f, 0.22f, 0.15f);
        colors.highlightedColor = new Color(0.25f, 0.35f, 0.20f);
        colors.pressedColor = new Color(0.35f, 0.50f, 0.30f);
        colors.selectedColor = new Color(0.25f, 0.35f, 0.20f);
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
        tmp.color = new Color(0.9f, 0.8f, 0.55f);
        tmp.fontStyle = FontStyles.Bold;
    }

    // === PANEL DISPLAY ===

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

    // === PAUSE ===

    public void TogglePause()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        isPaused = !isPaused;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    // === BUTTON FUNCTIONS ===

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
}
