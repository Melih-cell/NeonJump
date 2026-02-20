using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// NeonJump HUD - Mobil uyumlu, temiz ve performansli
/// Elemanlar: Health, Score/Combo, Coin, Weapon, PowerUp, Boss Health
/// </summary>
public class NeonHUD : MonoBehaviour
{
    public static NeonHUD Instance { get; private set; }

    // Canvas
    private Canvas mainCanvas;
    private CanvasScaler canvasScaler;
    private RectTransform safeAreaRoot;
    private bool isMobile;

    // Health (Sol ust)
    private RectTransform healthPanel;
    private Image healthBarFill;
    private Image healthBarGlow;
    private TextMeshProUGUI healthText;
    private Image[] heartIcons;
    private float displayedHealth;

    // Score & Combo (Ust orta)
    private RectTransform scorePanel;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;
    private Image comboTimerFill;
    private CanvasGroup comboGroup;
    private int displayedScore;

    // Coin (Sag ust)
    private RectTransform coinPanel;
    private TextMeshProUGUI coinText;

    // Weapon (Sol alt)
    private RectTransform weaponPanel;
    private TextMeshProUGUI weaponNameText;
    private TextMeshProUGUI ammoText;
    private Image reloadBarFill;
    private Image[] weaponSlots;
    private Image[] slotRarityBorders;

    // Power-Up (Sol kenar)
    private RectTransform powerUpPanel;
    private Dictionary<PowerUpType, PowerUpIndicatorUI> powerUpIndicators = new Dictionary<PowerUpType, PowerUpIndicatorUI>();

    // Boss Health
    private RectTransform bossHealthPanel;
    private TextMeshProUGUI bossNameText;
    private Image bossHealthFill;
    private int bossMaxHealth;

    // Neon efekt
    private float pulseSpeed = 3f;
    private float glowIntensity = 0.5f;

    // Doga Temasi Renkleri
    private Color neonCyan = new Color(0.9f, 0.8f, 0.55f);
    private Color neonPink = new Color(0.7f, 0.35f, 0.35f);
    private Color neonYellow = new Color(0.92f, 0.78f, 0.35f);
    private Color neonOrange = new Color(0.85f, 0.55f, 0.25f);
    private Color neonGreen = new Color(0.35f, 0.75f, 0.45f);
    private Color neonPurple = new Color(0.55f, 0.40f, 0.65f);

    private static readonly Color panelBgColor = new Color(0.12f, 0.14f, 0.10f, 0.85f);

    // Cached string builder - her frame yeni string olusturmayi onler
    private readonly StringBuilder _sb = new StringBuilder(32);

    // Onceki degerler - sadece degisince UI guncelle
    private int _lastHealthDisplay = -1;
    private int _lastMaxHealthDisplay = -1;
    private int _lastScoreDisplay = -1;
    private int _lastCombo = -1;
    private int _lastMultiplier = -1;
    private int _lastCoinDisplay = -1;
    private int _lastAmmo = -1;
    private int _lastReserve = -1;

    // Neon efekt throttle
    private float _neonUpdateInterval = 0.05f; // 20 FPS glow yeterli
    private float _neonUpdateTimer;

    void Awake()
    {
        Instance = this;
        isMobile = Application.isMobilePlatform ||
                   UnityEngine.InputSystem.Touchscreen.current != null;
    }

    void Start()
    {
        CreateMainCanvas();
        CreateSafeAreaRoot();
        CreateHealthPanel();
        CreateScorePanel();
        CreateCoinPanel();
        CreateWeaponPanel();
        CreatePowerUpPanel();
        StartCoroutine(DelayedInit());
    }

    IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.2f);
        SubscribeToEvents();
        UpdateAllUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        UnsubscribeFromEvents();
    }

    void Update()
    {
        UpdateHealthAnimation();
        UpdateScoreAnimation();
        UpdateComboTimer();

        // Neon glow efekti gorsel oncelikli degil, throttle et
        _neonUpdateTimer += Time.deltaTime;
        if (_neonUpdateTimer >= _neonUpdateInterval)
        {
            _neonUpdateTimer = 0f;
            UpdateNeonEffects();
        }
    }

    // ============================================================
    // CANVAS & SAFE AREA
    // ============================================================

    void CreateMainCanvas()
    {
        mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();

        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("NeonHUDCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 150;

            canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform, false);
        }
        else
        {
            canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        }

        if (isMobile && canvasScaler != null)
            canvasScaler.referenceResolution = new Vector2(1280, 720);
    }

    void CreateSafeAreaRoot()
    {
        GameObject safeObj = new GameObject("SafeAreaRoot");
        safeObj.transform.SetParent(transform, false);
        safeAreaRoot = safeObj.AddComponent<RectTransform>();
        safeAreaRoot.anchorMin = Vector2.zero;
        safeAreaRoot.anchorMax = Vector2.one;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
        safeObj.AddComponent<SafeAreaHandler>();
    }

    float GetMobileFontSize(float baseFontSize)
    {
        if (!isMobile) return baseFontSize;
        float dpiScale = Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f);
        return Mathf.Max(baseFontSize * dpiScale, 14f);
    }

    // ============================================================
    // HEALTH PANEL - Sol Ust
    // ============================================================

    void CreateHealthPanel()
    {
        healthPanel = CreatePanel("HealthPanel",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(320, 70));

        Image panelBg = healthPanel.gameObject.AddComponent<Image>();
        panelBg.color = panelBgColor;
        AddNeonBorder(healthPanel.gameObject, neonCyan);

        // Kalp ikonu
        GameObject hpIconObj = CreateUIElement("HPIcon", healthPanel);
        RectTransform hpIconRt = hpIconObj.GetComponent<RectTransform>();
        hpIconRt.anchorMin = hpIconRt.anchorMax = new Vector2(0, 0.5f);
        hpIconRt.pivot = new Vector2(0, 0.5f);
        hpIconRt.anchoredPosition = new Vector2(10, 0);
        hpIconRt.sizeDelta = new Vector2(50, 50);

        TextMeshProUGUI hpSymbol = hpIconObj.AddComponent<TextMeshProUGUI>();
        hpSymbol.text = "<color=#FF3366>\u2665</color>";
        hpSymbol.fontSize = 36;
        hpSymbol.alignment = TextAlignmentOptions.Center;

        // Health Bar arka plan
        GameObject healthBarBg = CreateUIElement("HealthBarBg", healthPanel);
        RectTransform barBgRt = healthBarBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0, 0.5f);
        barBgRt.anchorMax = new Vector2(1, 0.5f);
        barBgRt.pivot = new Vector2(0, 0.5f);
        barBgRt.anchoredPosition = new Vector2(65, 5);
        barBgRt.sizeDelta = new Vector2(-85, 25);

        Image barBgImg = healthBarBg.AddComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.02f, 0.05f, 0.9f);

        // Health Bar Fill
        GameObject healthBarFillObj = CreateUIElement("HealthBarFill", healthBarBg.transform);
        RectTransform fillRt = healthBarFillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);

        healthBarFill = healthBarFillObj.AddComponent<Image>();
        healthBarFill.color = neonGreen;

        // Health Bar Glow
        GameObject glowObj = CreateUIElement("HealthGlow", healthBarFillObj.transform);
        RectTransform glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.sizeDelta = new Vector2(8, 8);
        glowRt.anchoredPosition = Vector2.zero;

        healthBarGlow = glowObj.AddComponent<Image>();
        healthBarGlow.color = new Color(neonGreen.r, neonGreen.g, neonGreen.b, 0.3f);

        // Health Text
        GameObject healthTextObj = CreateUIElement("HealthText", healthBarBg.transform);
        RectTransform textRt = healthTextObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "100 / 100";
        healthText.fontSize = GetMobileFontSize(16);
        healthText.fontStyle = FontStyles.Bold;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;

        CreateHeartIcons();
    }

    void CreateHeartIcons()
    {
        GameObject heartsContainer = CreateUIElement("HeartsContainer", healthPanel);
        RectTransform heartsRt = heartsContainer.GetComponent<RectTransform>();
        heartsRt.anchorMin = new Vector2(0, 0);
        heartsRt.anchorMax = new Vector2(1, 0);
        heartsRt.pivot = new Vector2(0, 0);
        heartsRt.anchoredPosition = new Vector2(65, 5);
        heartsRt.sizeDelta = new Vector2(-85, 20);

        HorizontalLayoutGroup hlg = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        heartIcons = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject heart = CreateUIElement($"Heart_{i}", heartsContainer.transform);
            RectTransform heartRt = heart.GetComponent<RectTransform>();
            heartRt.sizeDelta = new Vector2(18, 18);

            LayoutElement le = heart.AddComponent<LayoutElement>();
            le.preferredWidth = 18;
            le.preferredHeight = 18;

            heartIcons[i] = heart.AddComponent<Image>();
            heartIcons[i].color = new Color(1f, 0.2f, 0.4f);
        }
    }

    // ============================================================
    // SCORE PANEL - Ust Orta
    // ============================================================

    void CreateScorePanel()
    {
        scorePanel = CreatePanel("ScorePanel",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(280, 90));

        Image panelBg = scorePanel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.14f, 0.10f, 0.8f);
        AddNeonBorder(scorePanel.gameObject, neonYellow);

        // Score Label
        GameObject scoreLabelObj = CreateUIElement("ScoreLabel", scorePanel);
        RectTransform labelRt = scoreLabelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 1);
        labelRt.anchorMax = new Vector2(0.5f, 1);
        labelRt.pivot = new Vector2(0.5f, 1);
        labelRt.anchoredPosition = new Vector2(0, -5);
        labelRt.sizeDelta = new Vector2(200, 20);

        TextMeshProUGUI labelText = scoreLabelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "SKOR";
        labelText.fontSize = 14;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = neonCyan;
        labelText.fontStyle = FontStyles.Bold;

        // Score Value
        GameObject scoreValueObj = CreateUIElement("ScoreValue", scorePanel);
        RectTransform valueRt = scoreValueObj.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0.5f, 0.5f);
        valueRt.anchorMax = new Vector2(0.5f, 0.5f);
        valueRt.pivot = new Vector2(0.5f, 0.5f);
        valueRt.anchoredPosition = new Vector2(0, 5);
        valueRt.sizeDelta = new Vector2(260, 40);

        scoreText = scoreValueObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "0";
        scoreText.fontSize = GetMobileFontSize(36);
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = Color.white;

        // Combo Container
        GameObject comboContainer = CreateUIElement("ComboContainer", scorePanel);
        RectTransform comboContainerRt = comboContainer.GetComponent<RectTransform>();
        comboContainerRt.anchorMin = new Vector2(0, 0);
        comboContainerRt.anchorMax = new Vector2(1, 0);
        comboContainerRt.pivot = new Vector2(0.5f, 0);
        comboContainerRt.anchoredPosition = new Vector2(0, 5);
        comboContainerRt.sizeDelta = new Vector2(-20, 25);

        comboGroup = comboContainer.AddComponent<CanvasGroup>();
        comboGroup.alpha = 0;

        // Combo Timer Bar arka plan
        GameObject comboBarBg = CreateUIElement("ComboBarBg", comboContainer.transform);
        RectTransform comboBgRt = comboBarBg.GetComponent<RectTransform>();
        comboBgRt.anchorMin = new Vector2(0, 0);
        comboBgRt.anchorMax = new Vector2(1, 0);
        comboBgRt.pivot = new Vector2(0.5f, 0);
        comboBgRt.anchoredPosition = Vector2.zero;
        comboBgRt.sizeDelta = new Vector2(0, 4);

        Image comboBgImg = comboBarBg.AddComponent<Image>();
        comboBgImg.color = new Color(0.2f, 0.1f, 0f, 0.8f);

        // Combo Timer Fill
        GameObject comboFillObj = CreateUIElement("ComboFill", comboBarBg.transform);
        RectTransform comboFillRt = comboFillObj.GetComponent<RectTransform>();
        comboFillRt.anchorMin = Vector2.zero;
        comboFillRt.anchorMax = Vector2.one;
        comboFillRt.offsetMin = Vector2.zero;
        comboFillRt.offsetMax = Vector2.zero;
        comboFillRt.pivot = new Vector2(0, 0.5f);

        comboTimerFill = comboFillObj.AddComponent<Image>();
        comboTimerFill.color = neonOrange;

        // Combo Text
        GameObject comboTextObj = CreateUIElement("ComboText", comboContainer.transform);
        RectTransform comboTextRt = comboTextObj.GetComponent<RectTransform>();
        comboTextRt.anchorMin = new Vector2(0, 0);
        comboTextRt.anchorMax = new Vector2(1, 1);
        comboTextRt.offsetMin = new Vector2(0, 5);
        comboTextRt.offsetMax = Vector2.zero;

        comboText = comboTextObj.AddComponent<TextMeshProUGUI>();
        comboText.text = "";
        comboText.fontSize = 18;
        comboText.fontStyle = FontStyles.Bold;
        comboText.alignment = TextAlignmentOptions.Center;
        comboText.color = neonOrange;
    }

    // ============================================================
    // COIN PANEL - Sag Ust
    // ============================================================

    void CreateCoinPanel()
    {
        coinPanel = CreatePanel("CoinPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(160, 50));

        Image panelBg = coinPanel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.14f, 0.14f, 0.10f, 0.85f);
        AddNeonBorder(coinPanel.gameObject, new Color(1f, 0.85f, 0f));

        // Coin Icon
        GameObject coinIconObj = CreateUIElement("CoinIcon", coinPanel);
        RectTransform iconRt = coinIconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, 0);
        iconRt.sizeDelta = new Vector2(30, 30);

        Image coinIcon = coinIconObj.AddComponent<Image>();
        coinIcon.color = new Color(1f, 0.85f, 0f);

        // Coin Text
        GameObject coinTextObj = CreateUIElement("CoinText", coinPanel);
        RectTransform textRt = coinTextObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(50, 0);
        textRt.offsetMax = new Vector2(-10, 0);

        coinText = coinTextObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = GetMobileFontSize(24);
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.Left;
        coinText.color = new Color(1f, 0.9f, 0.3f);
    }

    // ============================================================
    // WEAPON PANEL - Sol Alt
    // ============================================================

    void CreateWeaponPanel()
    {
        float yOffset = isMobile ? 150 : 20;

        weaponPanel = CreatePanel("WeaponPanel",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(20, yOffset), new Vector2(280, 100));

        Image panelBg = weaponPanel.gameObject.AddComponent<Image>();
        panelBg.color = panelBgColor;
        AddNeonBorder(weaponPanel.gameObject, neonPink);

        // Weapon Name
        GameObject nameObj = CreateUIElement("WeaponName", weaponPanel);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, -5);
        nameRt.sizeDelta = new Vector2(-20, 25);

        weaponNameText = nameObj.AddComponent<TextMeshProUGUI>();
        weaponNameText.text = "TABANCA";
        weaponNameText.fontSize = 18;
        weaponNameText.fontStyle = FontStyles.Bold;
        weaponNameText.alignment = TextAlignmentOptions.Center;
        weaponNameText.color = neonPink;

        // Ammo Display
        GameObject ammoObj = CreateUIElement("AmmoDisplay", weaponPanel);
        RectTransform ammoRt = ammoObj.GetComponent<RectTransform>();
        ammoRt.anchorMin = new Vector2(0.5f, 0.5f);
        ammoRt.anchorMax = new Vector2(0.5f, 0.5f);
        ammoRt.pivot = new Vector2(0.5f, 0.5f);
        ammoRt.anchoredPosition = Vector2.zero;
        ammoRt.sizeDelta = new Vector2(200, 30);

        ammoText = ammoObj.AddComponent<TextMeshProUGUI>();
        ammoText.text = "12 / 48";
        ammoText.fontSize = GetMobileFontSize(28);
        ammoText.fontStyle = FontStyles.Bold;
        ammoText.alignment = TextAlignmentOptions.Center;
        ammoText.color = Color.white;

        // Reload Bar
        GameObject reloadBarObj = CreateUIElement("ReloadBar", weaponPanel);
        RectTransform reloadRt = reloadBarObj.GetComponent<RectTransform>();
        reloadRt.anchorMin = new Vector2(0.1f, 0.35f);
        reloadRt.anchorMax = new Vector2(0.9f, 0.42f);
        reloadRt.anchoredPosition = Vector2.zero;
        reloadRt.sizeDelta = Vector2.zero;

        Image reloadBg = reloadBarObj.AddComponent<Image>();
        reloadBg.color = new Color(0.18f, 0.20f, 0.16f, 0.8f);

        GameObject reloadFillObj = CreateUIElement("ReloadFill", reloadBarObj.transform);
        RectTransform rFillRt = reloadFillObj.GetComponent<RectTransform>();
        rFillRt.anchorMin = Vector2.zero;
        rFillRt.anchorMax = new Vector2(0, 1);
        rFillRt.pivot = new Vector2(0, 0.5f);
        rFillRt.offsetMin = Vector2.zero;
        rFillRt.offsetMax = Vector2.zero;

        reloadBarFill = reloadFillObj.AddComponent<Image>();
        reloadBarFill.color = neonCyan;
        reloadBarObj.SetActive(false);

        CreateWeaponSlots();
    }

    void CreateWeaponSlots()
    {
        GameObject slotsContainer = CreateUIElement("WeaponSlots", weaponPanel);
        RectTransform slotsRt = slotsContainer.GetComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0, 0);
        slotsRt.anchorMax = new Vector2(1, 0);
        slotsRt.pivot = new Vector2(0.5f, 0);
        slotsRt.anchoredPosition = new Vector2(0, 8);
        slotsRt.sizeDelta = new Vector2(-20, 30);

        HorizontalLayoutGroup hlg = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        weaponSlots = new Image[3];
        slotRarityBorders = new Image[3];
        string[] slotLabels = { "1", "2", "3" };

        for (int i = 0; i < 3; i++)
        {
            GameObject slotObj = CreateUIElement($"Slot_{i}", slotsContainer.transform);
            RectTransform slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(75, 28);

            LayoutElement le = slotObj.AddComponent<LayoutElement>();
            le.preferredWidth = 75;
            le.preferredHeight = 28;

            slotRarityBorders[i] = slotObj.AddComponent<Image>();
            slotRarityBorders[i].color = new Color(0.3f, 0.3f, 0.3f);

            GameObject innerObj = CreateUIElement("Inner", slotObj.transform);
            RectTransform innerRt = innerObj.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);

            weaponSlots[i] = innerObj.AddComponent<Image>();
            weaponSlots[i].color = new Color(0.16f, 0.18f, 0.14f, 0.9f);

            GameObject labelObj = CreateUIElement("Label", slotObj.transform);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = slotLabels[i];
            labelText.fontSize = 16;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(1f, 1f, 1f, 0.6f);
        }
    }

    // ============================================================
    // POWER-UP PANEL - Sol Kenar
    // ============================================================

    void CreatePowerUpPanel()
    {
        powerUpPanel = CreatePanel("PowerUpPanel",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(20, 0), new Vector2(180, 300));

        VerticalLayoutGroup vlg = powerUpPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(5, 5, 5, 5);
    }

    // ============================================================
    // BOSS HEALTH BAR
    // ============================================================

    void CreateBossHealthBar()
    {
        bossHealthPanel = CreatePanel("BossHealthPanel",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -120), new Vector2(500, 50));

        Image bg = bossHealthPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.02f, 0.02f, 0.9f);
        AddNeonBorder(bossHealthPanel.gameObject, new Color(1f, 0.2f, 0.2f));

        // Boss name
        GameObject nameObj = CreateUIElement("BossName", bossHealthPanel);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 1);
        nameRt.anchorMax = new Vector2(0.5f, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, -3);
        nameRt.sizeDelta = new Vector2(480, 20);

        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
        bossNameText.fontSize = 16;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.alignment = TextAlignmentOptions.Center;
        bossNameText.color = new Color(1f, 0.3f, 0.3f);

        // Health bar
        GameObject barBg = CreateUIElement("HealthBarBg", bossHealthPanel);
        RectTransform barRt = barBg.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0);
        barRt.anchorMax = new Vector2(0.5f, 0);
        barRt.pivot = new Vector2(0.5f, 0);
        barRt.anchoredPosition = new Vector2(0, 5);
        barRt.sizeDelta = new Vector2(480, 20);

        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.05f, 0.05f, 0.9f);

        GameObject fillObj = CreateUIElement("Fill", barBg.transform);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);
        fillRt.pivot = new Vector2(0, 0.5f);

        bossHealthFill = fillObj.AddComponent<Image>();
        bossHealthFill.color = new Color(1f, 0.2f, 0.2f);

        bossHealthPanel.gameObject.SetActive(false);
    }

    // ============================================================
    // UI HELPER METHODS
    // ============================================================

    RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        Transform parentTransform = safeAreaRoot != null ? safeAreaRoot : transform;

        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parentTransform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        return rt;
    }

    GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    GameObject CreateUIElement(string name, RectTransform parent)
    {
        return CreateUIElement(name, (Transform)parent);
    }

    void AddNeonBorder(GameObject obj, Color color)
    {
        Outline outline = obj.GetComponent<Outline>();
        if (outline == null) outline = obj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, 2);
    }

    // ============================================================
    // EVENT SUBSCRIPTIONS
    // ============================================================

    void SubscribeToEvents()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged += OnWeaponChanged;
            WeaponManager.Instance.OnAmmoChanged += OnAmmoChanged;
            WeaponManager.Instance.OnReloadStarted += OnReloadStarted;
            WeaponManager.Instance.OnReloadFinished += OnReloadFinished;
        }
    }

    void UnsubscribeFromEvents()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged -= OnWeaponChanged;
            WeaponManager.Instance.OnAmmoChanged -= OnAmmoChanged;
            WeaponManager.Instance.OnReloadStarted -= OnReloadStarted;
            WeaponManager.Instance.OnReloadFinished -= OnReloadFinished;
        }
    }

    // ============================================================
    // UPDATE METHODS
    // ============================================================

    void UpdateAllUI()
    {
        UpdateHealthUI();
        UpdateScoreUI();
        UpdateCoinUI();
        UpdateWeaponUI();
    }

    void UpdateHealthUI()
    {
        if (GameManager.Instance == null) return;

        float current = GameManager.Instance.currentHealth;
        float max = GameManager.Instance.MaxHealth;

        displayedHealth = current;
        healthBarFill.rectTransform.anchorMax = new Vector2(current / max, 1);

        int currentInt = Mathf.CeilToInt(current);
        int maxInt = Mathf.CeilToInt(max);
        _lastHealthDisplay = currentInt;
        _lastMaxHealthDisplay = maxInt;
        _sb.Clear();
        _sb.Append(currentInt).Append(" / ").Append(maxInt);
        healthText.SetText(_sb);

        float percent = current / max;
        if (percent > 0.6f)
            healthBarFill.color = neonGreen;
        else if (percent > 0.3f)
            healthBarFill.color = neonYellow;
        else
            healthBarFill.color = new Color(1f, 0.2f, 0.3f);

        int hearts = Mathf.CeilToInt(max);
        int filledHearts = Mathf.CeilToInt(current);
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (i < hearts)
            {
                heartIcons[i].gameObject.SetActive(true);
                heartIcons[i].color = i < filledHearts ?
                    new Color(1f, 0.2f, 0.4f) :
                    new Color(0.3f, 0.1f, 0.15f, 0.5f);
            }
            else
            {
                heartIcons[i].gameObject.SetActive(false);
            }
        }
    }

    void UpdateHealthAnimation()
    {
        if (GameManager.Instance == null) return;

        float target = GameManager.Instance.currentHealth;
        if (Mathf.Abs(displayedHealth - target) > 0.01f)
        {
            displayedHealth = Mathf.Lerp(displayedHealth, target, Time.deltaTime * 8f);
            float max = GameManager.Instance.MaxHealth;
            healthBarFill.rectTransform.anchorMax = new Vector2(displayedHealth / max, 1);

            // Sadece goruntulenen tam sayi degistiginde string olustur
            int displayInt = Mathf.CeilToInt(displayedHealth);
            int maxInt = Mathf.CeilToInt(max);
            if (displayInt != _lastHealthDisplay || maxInt != _lastMaxHealthDisplay)
            {
                _lastHealthDisplay = displayInt;
                _lastMaxHealthDisplay = maxInt;
                _sb.Clear();
                _sb.Append(displayInt).Append(" / ").Append(maxInt);
                healthText.SetText(_sb);
            }
        }
    }

    void UpdateScoreUI()
    {
        if (GameManager.Instance == null) return;
        displayedScore = GameManager.Instance.GetScore();
        _lastScoreDisplay = displayedScore;
        SetScoreText(displayedScore);
    }

    void UpdateScoreAnimation()
    {
        if (GameManager.Instance == null) return;

        int target = GameManager.Instance.GetScore();
        if (displayedScore != target)
        {
            int diff = target - displayedScore;
            int step = Mathf.Max(1, Mathf.Abs(diff) / 20);
            displayedScore = diff > 0 ?
                Mathf.Min(displayedScore + step, target) :
                Mathf.Max(displayedScore - step, target);

            if (displayedScore != _lastScoreDisplay)
            {
                _lastScoreDisplay = displayedScore;
                SetScoreText(displayedScore);
            }

            if (diff > 0)
            {
                float scale = 1f + Mathf.Min(diff / 500f, 0.3f);
                scoreText.transform.localScale = Vector3.one * scale;
            }
        }
        else
        {
            scoreText.transform.localScale = Vector3.Lerp(
                scoreText.transform.localScale, Vector3.one, Time.deltaTime * 5f);
        }
    }

    void SetScoreText(int score)
    {
        _sb.Clear();
        FormatNumber(_sb, score);
        scoreText.SetText(_sb);
    }

    // GC-free sayi formatlama (N0 yerine)
    static void FormatNumber(StringBuilder sb, int number)
    {
        if (number < 0)
        {
            sb.Append('-');
            number = -number;
        }
        if (number == 0)
        {
            sb.Append('0');
            return;
        }

        // Rakamlari bul
        int digits = 0;
        int temp = number;
        while (temp > 0) { digits++; temp /= 10; }

        int startIndex = sb.Length;
        for (int i = 0; i < digits; i++) sb.Append('0');

        int pos = startIndex + digits - 1;
        int groupCount = 0;
        while (number > 0)
        {
            if (groupCount > 0 && groupCount % 3 == 0)
            {
                sb.Insert(pos + 1, ',');
            }
            sb[pos] = (char)('0' + number % 10);
            number /= 10;
            pos--;
            groupCount++;
        }
    }

    void UpdateComboTimer()
    {
        if (GameManager.Instance == null) return;

        int combo = GameManager.Instance.GetCombo();
        int multiplier = GameManager.Instance.GetComboMultiplier();

        if (combo > 0)
        {
            comboGroup.alpha = 1f;

            // Sadece combo veya multiplier degistiginde string olustur
            if (combo != _lastCombo || multiplier != _lastMultiplier)
            {
                _lastCombo = combo;
                _lastMultiplier = multiplier;
                _sb.Clear();
                _sb.Append(combo).Append(" HIT! x").Append(multiplier);
                comboText.SetText(_sb);

                if (combo >= 20) comboText.color = neonPurple;
                else if (combo >= 10) comboText.color = neonPink;
                else if (combo >= 5) comboText.color = neonOrange;
                else comboText.color = neonYellow;
            }

            if (ComboManager.Instance != null)
            {
                float timerPercent = ComboManager.Instance.comboTimer / ComboManager.Instance.comboTimeout;
                comboTimerFill.rectTransform.anchorMax = new Vector2(timerPercent, 1);
            }
        }
        else
        {
            if (_lastCombo != 0)
            {
                _lastCombo = 0;
                _lastMultiplier = -1;
            }
            comboGroup.alpha = Mathf.Lerp(comboGroup.alpha, 0f, Time.deltaTime * 3f);
        }
    }

    void UpdateCoinUI()
    {
        if (GameManager.Instance == null) return;
        int coins = GameManager.Instance.GetCoins();
        if (coins != _lastCoinDisplay)
        {
            _lastCoinDisplay = coins;
            _sb.Clear();
            FormatNumber(_sb, coins);
            coinText.SetText(_sb);
        }
    }

    void UpdateWeaponUI()
    {
        if (WeaponManager.Instance == null) return;

        WeaponInstance weapon = WeaponManager.Instance.GetCurrentWeapon();
        if (weapon != null && weapon.isUnlocked)
        {
            weaponNameText.text = weapon.data.weaponName.ToUpper();
            weaponNameText.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
            SetAmmoText(weapon.currentAmmo, weapon.reserveAmmo);

            float ammoPercent = (float)weapon.currentAmmo / weapon.GetEffectiveMaxAmmo();
            if (ammoPercent <= 0.2f)
            {
                float blink = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
                ammoText.color = new Color(1f, 0.3f * blink, 0.3f * blink);
            }
            else
            {
                ammoText.color = Color.white;
            }
        }

        UpdateWeaponSlots();
    }

    void SetAmmoText(int current, int reserve)
    {
        if (current != _lastAmmo || reserve != _lastReserve)
        {
            _lastAmmo = current;
            _lastReserve = reserve;
            _sb.Clear();
            _sb.Append(current).Append(" / ").Append(reserve);
            ammoText.SetText(_sb);
        }
    }

    void UpdateWeaponSlots()
    {
        if (WeaponManager.Instance == null) return;

        WeaponInstance[] weapons = {
            WeaponManager.Instance.primaryWeapon,
            WeaponManager.Instance.secondaryWeapon,
            WeaponManager.Instance.specialWeapon
        };
        int currentSlot = WeaponManager.Instance.currentSlot;

        for (int i = 0; i < 3; i++)
        {
            bool hasWeapon = weapons[i] != null && weapons[i].isUnlocked;
            bool isActive = i == currentSlot;

            if (hasWeapon)
            {
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapons[i].rarity);

                if (isActive)
                {
                    slotRarityBorders[i].color = rarityColor;
                    weaponSlots[i].color = new Color(rarityColor.r * 0.2f, rarityColor.g * 0.2f, rarityColor.b * 0.2f, 0.9f);
                }
                else
                {
                    slotRarityBorders[i].color = new Color(rarityColor.r * 0.5f, rarityColor.g * 0.5f, rarityColor.b * 0.5f);
                    weaponSlots[i].color = new Color(0.16f, 0.18f, 0.14f, 0.7f);
                }
            }
            else
            {
                slotRarityBorders[i].color = new Color(0.2f, 0.2f, 0.2f);
                weaponSlots[i].color = new Color(0.12f, 0.13f, 0.10f, 0.5f);
            }
        }
    }

    void UpdateNeonEffects()
    {
        if (healthBarGlow == null) return;
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * glowIntensity;
        Color glowColor = healthBarFill.color;
        healthBarGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.2f + pulse * 0.2f);
    }

    // ============================================================
    // EVENT HANDLERS
    // ============================================================

    void OnWeaponChanged(WeaponInstance weapon)
    {
        UpdateWeaponUI();
    }

    void OnAmmoChanged(int current, int reserve)
    {
        SetAmmoText(current, reserve);
    }

    void OnReloadStarted()
    {
        ammoText.gameObject.SetActive(false);
        reloadBarFill.transform.parent.gameObject.SetActive(true);
        StartCoroutine(ReloadAnimation());
    }

    void OnReloadFinished()
    {
        ammoText.gameObject.SetActive(true);
        reloadBarFill.transform.parent.gameObject.SetActive(false);
    }

    IEnumerator ReloadAnimation()
    {
        WeaponInstance weapon = WeaponManager.Instance?.GetCurrentWeapon();
        if (weapon == null) yield break;

        float duration = weapon.GetEffectiveReloadTime();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            reloadBarFill.rectTransform.anchorMax = new Vector2(elapsed / duration, 1);
            yield return null;
        }
    }

    // ============================================================
    // PUBLIC API - Refresh
    // ============================================================

    public void RefreshCoins()
    {
        UpdateCoinUI();
    }

    // ============================================================
    // PUBLIC API - Power-Up
    // ============================================================

    public void ShowPowerUp(PowerUpType type, float duration)
    {
        if (powerUpIndicators.ContainsKey(type))
            return;

        StartCoroutine(CreatePowerUpIndicator(type, duration));
    }

    IEnumerator CreatePowerUpIndicator(PowerUpType type, float duration)
    {
        GameObject indicator = CreateUIElement($"PowerUp_{type}", powerUpPanel);
        RectTransform rt = indicator.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170, 35);

        LayoutElement le = indicator.AddComponent<LayoutElement>();
        le.preferredHeight = 35;

        Image bg = indicator.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        Color powerUpColor = GetPowerUpColor(type);
        AddNeonBorder(indicator, powerUpColor);

        // Icon
        GameObject iconObj = CreateUIElement("Icon", indicator.transform);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(5, 0);
        iconRt.sizeDelta = new Vector2(25, 25);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = powerUpColor;

        // Text
        GameObject textObj = CreateUIElement("Text", indicator.transform);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(35, 0);
        textRt.offsetMax = new Vector2(-5, 0);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = GetPowerUpName(type);
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Left;
        text.color = powerUpColor;

        PowerUpIndicatorUI indicatorUI = new PowerUpIndicatorUI
        {
            container = indicator,
            icon = iconImg,
            text = text
        };
        powerUpIndicators[type] = indicatorUI;

        yield return new WaitForSeconds(duration);

        // Fade out
        CanvasGroup cg = indicator.AddComponent<CanvasGroup>();
        float fadeTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - (elapsed / fadeTime);
            yield return null;
        }

        powerUpIndicators.Remove(type);
        Destroy(indicator);
    }

    public void HidePowerUp(PowerUpType type)
    {
        if (powerUpIndicators.TryGetValue(type, out PowerUpIndicatorUI indicator))
        {
            Destroy(indicator.container);
            powerUpIndicators.Remove(type);
        }
    }

    Color GetPowerUpColor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.SpeedBoost: return neonCyan;
            case PowerUpType.DoubleJump: return neonGreen;
            case PowerUpType.Shield: return new Color(0.3f, 0.3f, 1f);
            case PowerUpType.Magnet: return neonYellow;
            case PowerUpType.Invincibility: return neonPurple;
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

    // ============================================================
    // PUBLIC API - Boss Health
    // ============================================================

    public void ShowBossHealth(string bossName, int maxHealth)
    {
        bossMaxHealth = maxHealth;

        if (bossHealthPanel == null)
            CreateBossHealthBar();

        bossHealthPanel.gameObject.SetActive(true);
        bossNameText.text = bossName.ToUpper();
        bossHealthFill.rectTransform.anchorMax = Vector2.one;
    }

    public void UpdateBossHealth(int currentHealth)
    {
        if (bossHealthFill == null) return;

        float percent = (float)currentHealth / bossMaxHealth;
        bossHealthFill.rectTransform.anchorMax = new Vector2(percent, 1);

        if (percent > 0.66f)
            bossHealthFill.color = new Color(1f, 0.2f, 0.2f);
        else if (percent > 0.33f)
            bossHealthFill.color = neonOrange;
        else
            bossHealthFill.color = new Color(0.8f, 0f, 0f);
    }

    public void HideBossHealth()
    {
        if (bossHealthPanel != null)
            bossHealthPanel.gameObject.SetActive(false);
    }
}

// Helper class
public class PowerUpIndicatorUI
{
    public GameObject container;
    public Image icon;
    public TextMeshProUGUI text;
}
