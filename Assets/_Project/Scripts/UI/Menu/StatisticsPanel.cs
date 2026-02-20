using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Istatistik gosterim paneli.
/// Oyuncu ilerlemesi ve istatistiklerini gosterir.
/// </summary>
public class StatisticsPanel : MonoBehaviour
{
    [Header("Panel Referanslari")]
    public GameObject panelRoot;

    [Header("Istatistik Metinleri")]
    public TMP_Text highScoreText;
    public TMP_Text totalCoinsText;
    public TMP_Text enemiesKilledText;
    public TMP_Text deathsText;
    public TMP_Text playTimeText;
    public TMP_Text maxComboText;
    public TMP_Text bossesDefeatedText;
    public TMP_Text kdRatioText;
    public TMP_Text gamesPlayedText;
    public TMP_Text totalStarsText;

    [Header("Upgrade Seviyeleri")]
    public TMP_Text healthUpgradeText;
    public TMP_Text speedUpgradeText;
    public TMP_Text dashUpgradeText;
    public TMP_Text jumpUpgradeText;
    public TMP_Text damageUpgradeText;

    [Header("Butonlar")]
    public Button backButton;
    public Button resetButton;

    [Header("Onay Paneli")]
    public GameObject confirmResetPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Doga Temasi Stili")]
    public Color statLabelColor = new Color(0.7f, 0.7f, 0.7f);
    public Color statValueColor = new Color(0.9f, 0.8f, 0.55f);
    public Color upgradeMaxColor = new Color(0.35f, 0.75f, 0.45f);

    void Start()
    {
        SetupListeners();
    }

    void SetupListeners()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Hide);

        if (resetButton != null)
            resetButton.onClick.AddListener(ShowResetConfirmation);

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmReset);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(HideResetConfirmation);
    }

    /// <summary>
    /// Paneli goster ve istatistikleri yukle
    /// </summary>
    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        LoadStatistics();

        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(false);
    }

    /// <summary>
    /// Paneli gizle
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    /// <summary>
    /// Istatistikleri yukle ve goster
    /// </summary>
    void LoadStatistics()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
        {
            Debug.LogWarning("[StatisticsPanel] SaveManager bulunamadi!");
            return;
        }

        var data = SaveManager.Instance.Data;

        // Ana istatistikler
        SetStatText(highScoreText, "stats_high_score", FormatNumber(data.highScore));
        SetStatText(totalCoinsText, "stats_total_coins", FormatNumber(data.totalCoins));
        SetStatText(enemiesKilledText, "stats_enemies_killed", FormatNumber(data.totalEnemiesKilled));
        SetStatText(deathsText, "stats_deaths", FormatNumber(data.totalDeaths));
        SetStatText(playTimeText, "stats_play_time", SaveManager.Instance.GetFormattedPlayTime());
        SetStatText(maxComboText, "stats_max_combo", $"{data.maxComboReached}x");
        SetStatText(bossesDefeatedText, "stats_bosses_defeated", data.bossesDefeated.ToString());
        SetStatText(kdRatioText, "stats_kd_ratio", SaveManager.Instance.GetKillDeathRatio().ToString("F2"));
        SetStatText(gamesPlayedText, "stats_games_played", data.gamesPlayed.ToString());

        // Toplam yildiz
        if (totalStarsText != null)
        {
            int totalStars = SaveManager.Instance.GetTotalStars();
            totalStarsText.text = $"\u2605 {totalStars}";
        }

        // Upgrade seviyeleri
        SetUpgradeText(healthUpgradeText, "health", data.maxHealthUpgrade, 3);
        SetUpgradeText(speedUpgradeText, "speed", data.speedUpgrade, 3);
        SetUpgradeText(dashUpgradeText, "dash", data.dashUpgrade, 2);
        SetUpgradeText(jumpUpgradeText, "jump", data.jumpUpgrade, 2);
        SetUpgradeText(damageUpgradeText, "damage", data.damageUpgrade, 3);
    }

    void SetStatText(TMP_Text textComponent, string locKey, string value)
    {
        if (textComponent == null) return;

        string label;
        if (LocalizationManager.Instance != null)
        {
            label = LocalizationManager.Instance.Get(locKey);
        }
        else
        {
            // Fallback Turkce
            label = locKey switch
            {
                "stats_high_score" => "En Yuksek Skor",
                "stats_total_coins" => "Toplam Para",
                "stats_enemies_killed" => "Oldurulen Dusman",
                "stats_deaths" => "Olum Sayisi",
                "stats_play_time" => "Oyun Suresi",
                "stats_max_combo" => "En Yuksek Kombo",
                "stats_bosses_defeated" => "Yenilen Boss",
                "stats_kd_ratio" => "K/D Orani",
                "stats_games_played" => "Oynanan Oyun",
                _ => locKey
            };
        }

        textComponent.text = $"<color=#{ColorUtility.ToHtmlStringRGB(statLabelColor)}>{label}:</color> <color=#{ColorUtility.ToHtmlStringRGB(statValueColor)}>{value}</color>";
    }

    void SetUpgradeText(TMP_Text textComponent, string upgradeType, int currentLevel, int maxLevel)
    {
        if (textComponent == null) return;

        string upgradeName = upgradeType switch
        {
            "health" => "Can",
            "speed" => "Hiz",
            "dash" => "Dash",
            "jump" => "Ziplama",
            "damage" => "Hasar",
            _ => upgradeType
        };

        Color valueColor = (currentLevel >= maxLevel) ? upgradeMaxColor : statValueColor;

        string levelText = "";
        for (int i = 0; i < maxLevel; i++)
        {
            if (i < currentLevel)
                levelText += "\u2588"; // Dolu blok
            else
                levelText += "\u2591"; // Bos blok
        }

        textComponent.text = $"<color=#{ColorUtility.ToHtmlStringRGB(statLabelColor)}>{upgradeName}:</color> <color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{levelText}</color>";
    }

    string FormatNumber(int number)
    {
        if (number >= 1000000)
            return (number / 1000000f).ToString("F1") + "M";
        if (number >= 1000)
            return (number / 1000f).ToString("F1") + "K";
        return number.ToString();
    }

    // === RESET ISLEMLERI ===

    void ShowResetConfirmation()
    {
        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    void HideResetConfirmation()
    {
        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    void ConfirmReset()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
        }

        HideResetConfirmation();
        LoadStatistics();

        Debug.Log("[StatisticsPanel] Ilerleme sifirlandi!");
    }

    /// <summary>
    /// Runtime'da panel olustur (prefab yoksa)
    /// </summary>
    public void CreateRuntimePanel(Transform parent)
    {
        // Ana panel
        GameObject panel = new GameObject("StatisticsPanel");
        panel.transform.SetParent(parent, false);
        panelRoot = panel;

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Arka plan
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        // Icerik container
        GameObject content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.1f);
        contentRect.anchorMax = new Vector2(0.9f, 0.9f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.12f, 0.14f, 0.10f);

        // Border
        var outline = content.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.8f, 0.55f, 0.5f);
        outline.effectDistance = new Vector2(1, 1);

        // Baslik
        GameObject titleObj = CreateText(content.transform, "ISTATISTIKLER", 36, TextAlignmentOptions.Center);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, -10);

        // Istatistik listesi
        float startY = 0.85f;
        float stepY = 0.08f;

        highScoreText = CreateStatLine(content.transform, startY).GetComponent<TMP_Text>();
        totalCoinsText = CreateStatLine(content.transform, startY - stepY).GetComponent<TMP_Text>();
        enemiesKilledText = CreateStatLine(content.transform, startY - stepY * 2).GetComponent<TMP_Text>();
        deathsText = CreateStatLine(content.transform, startY - stepY * 3).GetComponent<TMP_Text>();
        playTimeText = CreateStatLine(content.transform, startY - stepY * 4).GetComponent<TMP_Text>();
        maxComboText = CreateStatLine(content.transform, startY - stepY * 5).GetComponent<TMP_Text>();
        bossesDefeatedText = CreateStatLine(content.transform, startY - stepY * 6).GetComponent<TMP_Text>();
        kdRatioText = CreateStatLine(content.transform, startY - stepY * 7).GetComponent<TMP_Text>();
        gamesPlayedText = CreateStatLine(content.transform, startY - stepY * 8).GetComponent<TMP_Text>();

        // Geri butonu
        backButton = CreateButton(content.transform, "GERI", new Vector2(0.5f, 0.05f), OnBackClick);

        panelRoot.SetActive(false);
    }

    GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = statValueColor;

        return obj;
    }

    GameObject CreateStatLine(Transform parent, float yAnchor)
    {
        GameObject obj = new GameObject("StatLine");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, yAnchor - 0.03f);
        rect.anchorMax = new Vector2(0.9f, yAnchor + 0.03f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Left;

        return obj;
    }

    Button CreateButton(Transform parent, string text, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Button");
        btnObj.transform.SetParent(parent, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchor.x - 0.15f, anchor.y - 0.03f);
        rect.anchorMax = new Vector2(anchor.x + 0.15f, anchor.y + 0.03f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = btnObj.AddComponent<Image>();
        image.color = new Color(0.35f, 0.55f, 0.35f);

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        return button;
    }

    void OnBackClick()
    {
        Hide();
    }
}
