using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Level tamamlama / Victory ekrani kontrolcusu.
/// Yildiz sistemi, bonus hesaplama ve sonraki level secenegi sunar.
/// </summary>
public class VictoryController : MonoBehaviour
{
    public static VictoryController Instance { get; private set; }

    [Header("Panel Referanslari")]
    public GameObject victoryPanel;
    public CanvasGroup canvasGroup;

    [Header("Baslik")]
    public TMP_Text titleText;
    public TMP_Text levelNameText;

    [Header("Yildiz Gosterimi")]
    public Image[] starImages;
    public Color starFilledColor = new Color(1f, 0.84f, 0f);
    public Color starEmptyColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Skor Gosterimi")]
    public TMP_Text baseScoreText;
    public TMP_Text timeBonusText;
    public TMP_Text comboBonusText;
    public TMP_Text noDamageBonusText;
    public TMP_Text totalScoreText;

    [Header("Istatistikler")]
    public TMP_Text completionTimeText;
    public TMP_Text enemiesText;
    public TMP_Text coinsText;
    public TMP_Text secretsText;

    [Header("Butonlar")]
    public Button nextLevelButton;
    public Button replayButton;
    public Button mainMenuButton;

    [Header("Animasyon")]
    public float fadeInDuration = 0.5f;
    public float starRevealDelay = 0.3f;
    public float scoreCountDuration = 1f;

    // Neon renkleri
    private readonly Color neonCyan = new Color(0f, 1f, 1f);
    private readonly Color neonYellow = new Color(1f, 1f, 0f);
    private readonly Color neonGreen = new Color(0f, 1f, 0.5f);

    private int currentLevel = 1;
    private int earnedStars = 0;
    private int baseScore = 0;
    private int timeBonus = 0;
    private int comboBonus = 0;
    private int noDamageBonus = 0;
    private int totalScore = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupListeners();

        // Baslangicta gizle
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    void SetupListeners()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(NextLevel);

        if (replayButton != null)
            replayButton.onClick.AddListener(Replay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    /// <summary>
    /// Victory ekranini goster
    /// </summary>
    public void Show(int levelNumber, int score, float completionTime, int maxCombo, bool noDamageTaken)
    {
        currentLevel = levelNumber;
        baseScore = score;

        // Bonus hesapla
        CalculateBonuses(completionTime, maxCombo, noDamageTaken);

        // Yildiz hesapla
        CalculateStars(completionTime, maxCombo, noDamageTaken);

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        // Oyunu durdur
        Time.timeScale = 0f;

        // Animasyonlu gosterim
        StartCoroutine(ShowAnimated());
    }

    void CalculateBonuses(float completionTime, int maxCombo, bool noDamageTaken)
    {
        // Zaman bonusu (hizli tamamlama)
        // 60 saniyeden az: 1000 puan, her 10 saniye icin -100
        if (completionTime < 60f)
            timeBonus = 1000;
        else if (completionTime < 120f)
            timeBonus = 500;
        else if (completionTime < 180f)
            timeBonus = 200;
        else
            timeBonus = 0;

        // Combo bonusu
        comboBonus = maxCombo * 50;

        // Hasarsiz tamamlama bonusu
        noDamageBonus = noDamageTaken ? 500 : 0;

        // Toplam
        totalScore = baseScore + timeBonus + comboBonus + noDamageBonus;
    }

    void CalculateStars(float completionTime, int maxCombo, bool noDamageTaken)
    {
        earnedStars = 1; // Minimum 1 yildiz (tamamladin)

        // 2. yildiz: Hizli tamamlama veya yuksek combo
        if (completionTime < 120f || maxCombo >= 10)
            earnedStars = 2;

        // 3. yildiz: Hasarsiz veya cok hizli
        if (noDamageTaken || completionTime < 60f)
            earnedStars = 3;
    }

    IEnumerator ShowAnimated()
    {
        // Tum elementleri baslangicta gizle
        HideAllElements();

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

        // Baslik
        if (titleText != null)
        {
            titleText.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get("game_level_complete")
                : "LEVEL TAMAMLANDI";
            titleText.color = neonGreen;
            titleText.gameObject.SetActive(true);
        }

        if (levelNameText != null)
        {
            levelNameText.text = $"Level {currentLevel}";
            levelNameText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // Yildizlari tek tek goster
        yield return StartCoroutine(RevealStars());

        yield return new WaitForSecondsRealtime(0.3f);

        // Skor detaylarini goster
        yield return StartCoroutine(RevealScores());

        // Istatistikleri goster
        yield return StartCoroutine(RevealStats());

        // Butonlari goster
        ShowButtons();

        // Kaydet
        SaveProgress();
    }

    void HideAllElements()
    {
        // Yildizlari gizle
        if (starImages != null)
        {
            foreach (var star in starImages)
            {
                if (star != null)
                {
                    star.color = starEmptyColor;
                    star.transform.localScale = Vector3.zero;
                }
            }
        }

        // Skor metinlerini gizle
        if (baseScoreText != null) baseScoreText.gameObject.SetActive(false);
        if (timeBonusText != null) timeBonusText.gameObject.SetActive(false);
        if (comboBonusText != null) comboBonusText.gameObject.SetActive(false);
        if (noDamageBonusText != null) noDamageBonusText.gameObject.SetActive(false);
        if (totalScoreText != null) totalScoreText.gameObject.SetActive(false);

        // Butonlari gizle
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
    }

    IEnumerator RevealStars()
    {
        if (starImages == null) yield break;

        for (int i = 0; i < starImages.Length && i < 3; i++)
        {
            var star = starImages[i];
            if (star == null) continue;

            // Animasyon
            star.transform.localScale = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = 1f - (1f - t) * (1f - t); // Ease out

                star.transform.localScale = Vector3.one * t * 1.2f;
                yield return null;
            }

            // Final boyut ve renk
            star.transform.localScale = Vector3.one;

            if (i < earnedStars)
            {
                star.color = starFilledColor;
                // Parlama efekti
                star.transform.localScale = Vector3.one * 1.3f;
                yield return new WaitForSecondsRealtime(0.1f);
                star.transform.localScale = Vector3.one;

                // Ses efekti
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayButton();
            }
            else
            {
                star.color = starEmptyColor;
            }

            yield return new WaitForSecondsRealtime(starRevealDelay);
        }
    }

    IEnumerator RevealScores()
    {
        // Base score
        if (baseScoreText != null)
        {
            baseScoreText.gameObject.SetActive(true);
            baseScoreText.text = $"Skor: {baseScore:N0}";
            yield return new WaitForSecondsRealtime(0.15f);
        }

        // Time bonus
        if (timeBonusText != null && timeBonus > 0)
        {
            timeBonusText.gameObject.SetActive(true);
            timeBonusText.text = $"Zaman Bonusu: +{timeBonus:N0}";
            timeBonusText.color = neonCyan;
            yield return new WaitForSecondsRealtime(0.15f);
        }

        // Combo bonus
        if (comboBonusText != null && comboBonus > 0)
        {
            comboBonusText.gameObject.SetActive(true);
            comboBonusText.text = $"Kombo Bonusu: +{comboBonus:N0}";
            comboBonusText.color = neonYellow;
            yield return new WaitForSecondsRealtime(0.15f);
        }

        // No damage bonus
        if (noDamageBonusText != null && noDamageBonus > 0)
        {
            noDamageBonusText.gameObject.SetActive(true);
            noDamageBonusText.text = $"Hasarsiz Bonus: +{noDamageBonus:N0}";
            noDamageBonusText.color = neonGreen;
            yield return new WaitForSecondsRealtime(0.15f);
        }

        // Total score (animasyonlu)
        if (totalScoreText != null)
        {
            totalScoreText.gameObject.SetActive(true);
            yield return StartCoroutine(AnimateTotalScore());
        }
    }

    IEnumerator AnimateTotalScore()
    {
        int displayScore = 0;
        float elapsed = 0f;

        while (elapsed < scoreCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / scoreCountDuration;
            t = t * t * (3f - 2f * t); // Smoothstep

            displayScore = Mathf.RoundToInt(Mathf.Lerp(0, totalScore, t));
            totalScoreText.text = $"TOPLAM: {displayScore:N0}";

            yield return null;
        }

        totalScoreText.text = $"TOPLAM: {totalScore:N0}";
        totalScoreText.color = neonYellow;
        totalScoreText.fontSize = totalScoreText.fontSize * 1.1f;
    }

    IEnumerator RevealStats()
    {
        var stats = InGameStatistics.Instance;

        if (completionTimeText != null)
        {
            string time = stats != null ? stats.GetFormattedPlayTime() : "00:00";
            completionTimeText.text = $"Tamamlama Suresi: {time}";
            completionTimeText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (enemiesText != null)
        {
            int enemies = stats?.enemiesKilled ?? 0;
            enemiesText.text = $"Oldurulen Dusman: {enemies}";
            enemiesText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (coinsText != null)
        {
            int coins = stats?.coinsCollected ?? 0;
            coinsText.text = $"Toplanan Para: {coins}";
            coinsText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (secretsText != null)
        {
            int secrets = stats?.secretsFound ?? 0;
            secretsText.text = $"Bulunan Gizli Alan: {secrets}";
            secretsText.gameObject.SetActive(true);
        }
    }

    void ShowButtons()
    {
        // Sonraki level butonu (son level degilse)
        bool hasNextLevel = currentLevel < 20; // 20 level varsayimi

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(hasNextLevel);
        }

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);
    }

    void SaveProgress()
    {
        if (SaveManager.Instance == null) return;

        // Skor kaydet
        SaveManager.Instance.UpdateHighScore(totalScore);

        // Yildiz kaydet (sadece daha yuksekse)
        int existingStars = SaveManager.Instance.GetLevelStars(currentLevel - 1);
        if (earnedStars > existingStars)
        {
            SaveManager.Instance.SetLevelStars(currentLevel - 1, earnedStars);
        }

        // Sonraki leveli ac
        SaveManager.Instance.UnlockLevel(currentLevel);

        // Istatistikleri kaydet
        if (InGameStatistics.Instance != null)
        {
            InGameStatistics.Instance.SaveToGlobalStats();
        }

        SaveManager.Instance.Save();

        // Event fire
        GameEvents.RaiseLevelCompleted(currentLevel, earnedStars);
    }

    // === BUTON AKSIYONLARI ===

    void NextLevel()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        // Sonraki level'i yukle
        // Simdilik ayni sahneyi yukle (gercek implementasyonda farkli sahneler olabilir)
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    void Replay()
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
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        Time.timeScale = 1f;
    }
}
