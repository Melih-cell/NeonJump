using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Gelistirilmis Game Over ekrani kontrolcusu.
/// Detayli istatistikler, yeni rekor animasyonu ve secenekler sunar.
/// </summary>
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }

    [Header("Panel Referanslari")]
    public GameObject gameOverPanel;
    public CanvasGroup canvasGroup;

    [Header("Baslik")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;

    [Header("Skor Gosterimi")]
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;
    public GameObject newRecordBadge;
    public TMP_Text newRecordText;

    [Header("Istatistikler")]
    public TMP_Text playTimeText;
    public TMP_Text enemiesKilledText;
    public TMP_Text coinsCollectedText;
    public TMP_Text maxComboText;
    public TMP_Text accuracyText;
    public TMP_Text damageText;

    [Header("Butonlar")]
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Animasyon")]
    public float fadeInDuration = 0.5f;
    public float scoreCountDuration = 1.5f;
    public float statRevealDelay = 0.2f;

    [Header("Neon Efektleri")]
    public Image backgroundGlow;
    public float pulseSpeed = 2f;

    // Neon renkleri
    private readonly Color neonCyan = new Color(0f, 1f, 1f);
    private readonly Color neonPink = new Color(1f, 0f, 0.6f);
    private readonly Color neonYellow = new Color(1f, 1f, 0f);
    private readonly Color neonRed = new Color(1f, 0.2f, 0.2f);

    private bool isNewRecord = false;
    private int finalScore = 0;
    private bool isMobile = false;

    void Awake()
    {
        Instance = this;

        isMobile = Application.isMobilePlatform ||
                   UnityEngine.InputSystem.Touchscreen.current != null;
    }

    void Start()
    {
        SetupListeners();
        if (isMobile) ApplyMobileButtonSizes();

        // Baslangicta gizle
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Mobil cihazlarda butonlarin minimum 48dp dokunmatik hedef boyutunu saglar
    /// </summary>
    void ApplyMobileButtonSizes()
    {
        float dpiScale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        float minHeight = Mathf.Max(80f, 48f * dpiScale);

        Button[] buttons = { retryButton, mainMenuButton };
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 size = rt.sizeDelta;
                if (size.y < minHeight)
                {
                    rt.sizeDelta = new Vector2(Mathf.Max(size.x, 300f), minHeight);
                }
            }
        }
    }

    void Update()
    {
        // New record pulse efekti
        if (isNewRecord && newRecordBadge != null && newRecordBadge.activeSelf)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float scale = 1f + pulse * 0.1f;
            newRecordBadge.transform.localScale = Vector3.one * scale;
        }
    }

    void SetupListeners()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    /// <summary>
    /// Game Over ekranini goster
    /// </summary>
    public void Show(int score)
    {
        finalScore = score;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Oyunu durdur
        Time.timeScale = 0f;

        // Lokalize baslik
        if (titleText != null)
        {
            titleText.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get("game_over")
                : "OYUN BITTI";
            titleText.color = neonRed;
        }

        // Yeni rekor kontrolu
        int currentHighScore = 0;
        if (SaveManager.Instance != null)
        {
            currentHighScore = SaveManager.Instance.Data.highScore;
        }
        else
        {
            currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        }

        isNewRecord = score > currentHighScore;

        // Animasyonlu gosterim
        StartCoroutine(ShowAnimated());
    }

    IEnumerator ShowAnimated()
    {
        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // Skor sayaci animasyonu
        yield return StartCoroutine(AnimateScoreCount());

        // High score
        if (highScoreText != null)
        {
            int highScore = isNewRecord ? finalScore : (SaveManager.Instance?.Data.highScore ?? 0);
            highScoreText.text = $"{GetLocalizedText("stats_high_score")}: {highScore:N0}";
        }

        // Yeni rekor badge
        if (newRecordBadge != null)
        {
            newRecordBadge.SetActive(isNewRecord);
            if (isNewRecord && newRecordText != null)
            {
                newRecordText.text = LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.Get("game_new_record")
                    : "YENI REKOR!";
            }
        }

        // Istatistikleri goster
        yield return StartCoroutine(RevealStatistics());

        // Kaydet
        SaveStatistics();
    }

    IEnumerator AnimateScoreCount()
    {
        if (finalScoreText == null) yield break;

        int displayScore = 0;
        float elapsed = 0f;

        while (elapsed < scoreCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / scoreCountDuration;
            t = t * t * (3f - 2f * t); // Smoothstep

            displayScore = Mathf.RoundToInt(Mathf.Lerp(0, finalScore, t));
            finalScoreText.text = displayScore.ToString("N0");

            // Renk pulse
            float pulse = Mathf.Sin(t * Mathf.PI * 4f);
            finalScoreText.color = Color.Lerp(neonCyan, neonYellow, (pulse + 1f) * 0.5f);

            yield return null;
        }

        finalScoreText.text = finalScore.ToString("N0");
        finalScoreText.color = neonYellow;

        // Eger yeni rekorsa ekstra efekt
        if (isNewRecord)
        {
            finalScoreText.transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSecondsRealtime(0.1f);
            finalScoreText.transform.localScale = Vector3.one;
        }
    }

    IEnumerator RevealStatistics()
    {
        var stats = InGameStatistics.Instance;

        // Oyun suresi
        if (playTimeText != null)
        {
            yield return new WaitForSecondsRealtime(statRevealDelay);
            string time = stats != null ? stats.GetFormattedPlayTime() : "00:00";
            playTimeText.text = $"{GetLocalizedText("stats_play_time")}: {time}";
            AnimateStatText(playTimeText);
        }

        // Oldurulen dusman
        if (enemiesKilledText != null)
        {
            yield return new WaitForSecondsRealtime(statRevealDelay);
            int enemies = stats?.enemiesKilled ?? 0;
            enemiesKilledText.text = $"{GetLocalizedText("stats_enemies_killed")}: {enemies}";
            AnimateStatText(enemiesKilledText);
        }

        // Toplanan coin
        if (coinsCollectedText != null)
        {
            yield return new WaitForSecondsRealtime(statRevealDelay);
            int coins = stats?.coinsCollected ?? 0;
            coinsCollectedText.text = $"{GetLocalizedText("stats_total_coins")}: {coins}";
            AnimateStatText(coinsCollectedText);
        }

        // Max combo
        if (maxComboText != null)
        {
            yield return new WaitForSecondsRealtime(statRevealDelay);
            int combo = stats?.maxCombo ?? 0;
            maxComboText.text = $"{GetLocalizedText("stats_max_combo")}: {combo}x";
            AnimateStatText(maxComboText);
        }

        // Accuracy
        if (accuracyText != null)
        {
            yield return new WaitForSecondsRealtime(statRevealDelay);
            float accuracy = stats?.GetAccuracy() ?? 0f;
            accuracyText.text = $"Isabetlilik: {accuracy:F1}%";
            AnimateStatText(accuracyText);
        }

        // Damage
        if (damageText != null)
        {
            yield return new WaitForSecondsRealtime(statRevealDelay);
            int dealt = stats?.damageDealt ?? 0;
            int taken = stats?.damageTaken ?? 0;
            damageText.text = $"Hasar: {dealt} / {taken}";
            AnimateStatText(damageText);
        }
    }

    void AnimateStatText(TMP_Text text)
    {
        if (text == null) return;

        // Basit scale animasyonu
        StartCoroutine(ScaleAnimation(text.transform));
    }

    IEnumerator ScaleAnimation(Transform target)
    {
        target.localScale = Vector3.one * 0.8f;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = 1f - (1f - t) * (1f - t); // Ease out

            target.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    void SaveStatistics()
    {
        // SaveManager'a kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateHighScore(finalScore);
            SaveManager.Instance.AddDeath();
        }

        // InGameStatistics'i global'e kaydet
        if (InGameStatistics.Instance != null)
        {
            InGameStatistics.Instance.SaveToGlobalStats();
        }
    }

    string GetLocalizedText(string key)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.Get(key);

        // Fallback
        return key switch
        {
            "stats_play_time" => "Sure",
            "stats_enemies_killed" => "Dusman",
            "stats_total_coins" => "Para",
            "stats_max_combo" => "Max Kombo",
            "stats_high_score" => "En Yuksek",
            _ => key
        };
    }

    // === BUTON AKSIYONLARI ===

    void Retry()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Paneli gizle
    /// </summary>
    public void Hide()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }
}
