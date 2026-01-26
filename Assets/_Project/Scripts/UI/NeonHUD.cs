using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gelismis Neon HUD - Mobil uyumlu, tek sistem
/// Tum HUD elementlerini birlestirir
/// </summary>
public class NeonHUD : MonoBehaviour
{
    public static NeonHUD Instance { get; private set; }

    [Header("Main Canvas")]
    private Canvas mainCanvas;
    private CanvasScaler canvasScaler;

    [Header("Top Left - Health & Status")]
    private RectTransform healthPanel;
    private Image healthBarFill;
    private Image healthBarGlow;
    private TextMeshProUGUI healthText;
    private Image[] heartIcons;
    private float displayedHealth;

    [Header("Top Center - Score & Combo")]
    private RectTransform scorePanel;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;
    private Image comboTimerFill;
    private CanvasGroup comboGroup;
    private int displayedScore;

    [Header("Top Right - Coins & Currency")]
    private RectTransform coinPanel;
    private TextMeshProUGUI coinText;
    private Image coinIcon;

    [Header("Bottom Left - Weapon Info")]
    private RectTransform weaponPanel;
    private TextMeshProUGUI weaponNameText;
    private TextMeshProUGUI ammoText;
    private Image reloadBarFill;
    private Image[] weaponSlots;
    private Image[] slotRarityBorders;

    [Header("Bottom Right - Skills")]
    private RectTransform skillPanel;
    private SkillIconUI[] skillIcons;

    [Header("Center - Crosshair & Hit Markers")]
    private RectTransform crosshairPanel;
    private Image crosshairImage;
    private Image hitMarker;
    private Image critMarker;

    [Header("Left Edge - Power-Up Indicators")]
    private RectTransform powerUpPanel;
    private Dictionary<PowerUpType, PowerUpIndicatorUI> powerUpIndicators = new Dictionary<PowerUpType, PowerUpIndicatorUI>();

    [Header("Right Edge - Kill Feed")]
    private RectTransform killFeedPanel;
    private List<KillFeedEntry> killFeedEntries = new List<KillFeedEntry>();

    [Header("Screen Edges - Damage Direction")]
    private Image[] damageDirectionIndicators;

    [Header("Mini Map")]
    private RectTransform miniMapPanel;
    private RawImage miniMapImage;
    private Image playerBlip;
    private List<Image> enemyBlips = new List<Image>();

    [Header("Boss Health Bar")]
    private RectTransform bossHealthPanel;
    private TextMeshProUGUI bossNameText;
    private Image bossHealthFill;
    private int bossMaxHealth;

    [Header("Mobile Controls")]
    private bool isMobile;
    private RectTransform mobileControlsPanel;

    [Header("Animation Settings")]
    private float pulseSpeed = 3f;
    private float glowIntensity = 0.5f;

    // Neon Colors
    private Color neonCyan = new Color(0f, 1f, 1f);
    private Color neonPink = new Color(1f, 0f, 0.6f);
    private Color neonYellow = new Color(1f, 1f, 0f);
    private Color neonOrange = new Color(1f, 0.5f, 0f);
    private Color neonGreen = new Color(0f, 1f, 0.5f);
    private Color neonPurple = new Color(0.7f, 0f, 1f);

    void Awake()
    {
        Instance = this;
        isMobile = Application.isMobilePlatform ||
                   UnityEngine.InputSystem.Touchscreen.current != null;
    }

    void Start()
    {
        CreateMainCanvas();
        CreateHealthPanel();
        CreateScorePanel();
        CreateCoinPanel();
        CreateWeaponPanel();
        CreateSkillPanel();
        CreateCrosshair();
        CreatePowerUpPanel();
        CreateKillFeedPanel();
        CreateDamageIndicators();
        CreateMiniMap();

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
        UpdateNeonEffects();
        UpdateMiniMap();
    }

    // === CANVAS SETUP ===

    void CreateMainCanvas()
    {
        mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            mainCanvas = FindFirstObjectByType<Canvas>();
        }
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

        // Mobilde UI scale ayarla
        if (isMobile && canvasScaler != null)
        {
            canvasScaler.referenceResolution = new Vector2(1280, 720);
        }
    }

    // === HEALTH PANEL - Sol Ust ===

    void CreateHealthPanel()
    {
        healthPanel = CreatePanel("HealthPanel",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(320, 70));

        // Arka plan - Neon border ile
        Image panelBg = healthPanel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);
        AddNeonBorder(healthPanel.gameObject, neonCyan, 2);

        // HP Icon - Sol taraf
        GameObject hpIconObj = CreateUIElement("HPIcon", healthPanel);
        RectTransform hpIconRt = hpIconObj.GetComponent<RectTransform>();
        hpIconRt.anchorMin = hpIconRt.anchorMax = new Vector2(0, 0.5f);
        hpIconRt.pivot = new Vector2(0, 0.5f);
        hpIconRt.anchoredPosition = new Vector2(10, 0);
        hpIconRt.sizeDelta = new Vector2(50, 50);

        TextMeshProUGUI hpSymbol = hpIconObj.AddComponent<TextMeshProUGUI>();
        hpSymbol.text = "<color=#FF3366>♥</color>";
        hpSymbol.fontSize = 36;
        hpSymbol.alignment = TextAlignmentOptions.Center;
        hpSymbol.enableVertexGradient = true;

        // Health Bar Container
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

        // Health Bar Glow (arka plan efekti)
        GameObject glowObj = CreateUIElement("HealthGlow", healthBarFillObj.transform);
        RectTransform glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.sizeDelta = new Vector2(8, 8);
        glowRt.anchoredPosition = Vector2.zero;

        healthBarGlow = glowObj.AddComponent<Image>();
        healthBarGlow.color = new Color(neonGreen.r, neonGreen.g, neonGreen.b, 0.3f);

        // Health Text (bar icinde)
        GameObject healthTextObj = CreateUIElement("HealthText", healthBarBg.transform);
        RectTransform textRt = healthTextObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "100 / 100";
        healthText.fontSize = 16;
        healthText.fontStyle = FontStyles.Bold;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;

        // Kalp ikonlari (bar altinda)
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

    // === SCORE PANEL - Ust Orta ===

    void CreateScorePanel()
    {
        scorePanel = CreatePanel("ScorePanel",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -15), new Vector2(280, 90));

        // Arka plan
        Image panelBg = scorePanel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.08f, 0.8f);
        AddNeonBorder(scorePanel.gameObject, neonYellow, 2);

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
        scoreText.fontSize = 36;
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

        // Combo Timer Bar (arka plan)
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

    // === COIN PANEL - Sag Ust ===

    void CreateCoinPanel()
    {
        coinPanel = CreatePanel("CoinPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(160, 50));

        // Arka plan
        Image panelBg = coinPanel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.04f, 0.01f, 0.85f);
        AddNeonBorder(coinPanel.gameObject, new Color(1f, 0.85f, 0f), 2);

        // Coin Icon
        GameObject coinIconObj = CreateUIElement("CoinIcon", coinPanel);
        RectTransform iconRt = coinIconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, 0);
        iconRt.sizeDelta = new Vector2(30, 30);

        coinIcon = coinIconObj.AddComponent<Image>();
        coinIcon.color = new Color(1f, 0.85f, 0f);

        // Neon glow for coin
        Outline coinGlow = coinIconObj.AddComponent<Outline>();
        coinGlow.effectColor = new Color(1f, 0.6f, 0f, 0.6f);
        coinGlow.effectDistance = new Vector2(2, 2);

        // Coin Text
        GameObject coinTextObj = CreateUIElement("CoinText", coinPanel);
        RectTransform textRt = coinTextObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(50, 0);
        textRt.offsetMax = new Vector2(-10, 0);

        coinText = coinTextObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 24;
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.Left;
        coinText.color = new Color(1f, 0.9f, 0.3f);
    }

    // === WEAPON PANEL - Sol Alt ===

    void CreateWeaponPanel()
    {
        float yOffset = isMobile ? 150 : 20;

        weaponPanel = CreatePanel("WeaponPanel",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(20, yOffset), new Vector2(280, 100));

        // Arka plan
        Image panelBg = weaponPanel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);
        AddNeonBorder(weaponPanel.gameObject, neonPink, 2);

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
        ammoRt.anchoredPosition = new Vector2(0, 0);
        ammoRt.sizeDelta = new Vector2(200, 30);

        ammoText = ammoObj.AddComponent<TextMeshProUGUI>();
        ammoText.text = "12 / 48";
        ammoText.fontSize = 28;
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
        reloadBg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        GameObject reloadFillObj = CreateUIElement("ReloadFill", reloadBarObj.transform);
        RectTransform fillRt = reloadFillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        reloadBarFill = reloadFillObj.AddComponent<Image>();
        reloadBarFill.color = neonCyan;
        reloadBarObj.SetActive(false);

        // Weapon Slots (3 slot)
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

            // Rarity border (dis cerceve)
            slotRarityBorders[i] = slotObj.AddComponent<Image>();
            slotRarityBorders[i].color = new Color(0.3f, 0.3f, 0.3f);

            // Inner slot
            GameObject innerObj = CreateUIElement("Inner", slotObj.transform);
            RectTransform innerRt = innerObj.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);

            weaponSlots[i] = innerObj.AddComponent<Image>();
            weaponSlots[i].color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // Slot number
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

    // === SKILL PANEL - Sag Alt ===

    void CreateSkillPanel()
    {
        float yOffset = isMobile ? 150 : 20;

        skillPanel = CreatePanel("SkillPanel",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-20, yOffset), new Vector2(180, 60));

        HorizontalLayoutGroup hlg = skillPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(10, 10, 5, 5);

        skillIcons = new SkillIconUI[3];
        string[] skillNames = { "Dash", "Roll", "Jump" };
        string[] skillKeys = { "D", "R", "J" };
        Color[] skillColors = { neonCyan, neonOrange, neonGreen };

        for (int i = 0; i < 3; i++)
        {
            skillIcons[i] = CreateSkillIcon(skillNames[i], skillKeys[i], skillColors[i]);
        }
    }

    SkillIconUI CreateSkillIcon(string name, string key, Color color)
    {
        GameObject iconObj = CreateUIElement($"Skill_{name}", skillPanel);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(50, 50);

        LayoutElement le = iconObj.AddComponent<LayoutElement>();
        le.preferredWidth = 50;
        le.preferredHeight = 50;

        // Background
        Image bg = iconObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

        // Neon border
        Outline outline = iconObj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, 2);

        // Cooldown fill (radial)
        GameObject fillObj = CreateUIElement("CooldownFill", iconObj.transform);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3, 3);
        fillRt.offsetMax = new Vector2(-3, -3);

        Image cooldownFill = fillObj.AddComponent<Image>();
        cooldownFill.color = new Color(color.r, color.g, color.b, 0.4f);
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Radial360;
        cooldownFill.fillOrigin = (int)Image.Origin360.Top;
        cooldownFill.fillClockwise = false;
        cooldownFill.fillAmount = 0;

        // Key label
        GameObject labelObj = CreateUIElement("KeyLabel", iconObj.transform);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = key;
        label.fontSize = 22;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;

        return new SkillIconUI
        {
            container = iconRt,
            background = bg,
            cooldownFill = cooldownFill,
            keyLabel = label,
            baseColor = color
        };
    }

    // === CROSSHAIR ===

    void CreateCrosshair()
    {
        crosshairPanel = CreatePanel("CrosshairPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(64, 64));

        // Crosshair (basit dot)
        crosshairImage = crosshairPanel.gameObject.AddComponent<Image>();
        crosshairImage.color = new Color(1f, 1f, 1f, 0.7f);

        // Hit marker (vurusta gosterilecek)
        GameObject hitObj = CreateUIElement("HitMarker", crosshairPanel);
        RectTransform hitRt = hitObj.GetComponent<RectTransform>();
        hitRt.anchorMin = hitRt.anchorMax = hitRt.pivot = new Vector2(0.5f, 0.5f);
        hitRt.sizeDelta = new Vector2(32, 32);

        hitMarker = hitObj.AddComponent<Image>();
        hitMarker.color = new Color(1f, 1f, 1f, 0f); // Baslangicta gorunmez

        // Crit marker
        GameObject critObj = CreateUIElement("CritMarker", crosshairPanel);
        RectTransform critRt = critObj.GetComponent<RectTransform>();
        critRt.anchorMin = critRt.anchorMax = critRt.pivot = new Vector2(0.5f, 0.5f);
        critRt.sizeDelta = new Vector2(48, 48);

        critMarker = critObj.AddComponent<Image>();
        critMarker.color = new Color(1f, 0.3f, 0f, 0f);
    }

    // === POWER-UP PANEL ===

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

    // === KILL FEED PANEL ===

    void CreateKillFeedPanel()
    {
        killFeedPanel = CreatePanel("KillFeedPanel",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-20, 50), new Vector2(250, 200));

        VerticalLayoutGroup vlg = killFeedPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.childAlignment = TextAnchor.UpperRight;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(5, 5, 5, 5);
    }

    // === DAMAGE INDICATORS ===

    void CreateDamageIndicators()
    {
        damageDirectionIndicators = new Image[4]; // Top, Right, Bottom, Left

        Vector2[] positions = {
            new Vector2(0.5f, 1f),   // Top
            new Vector2(1f, 0.5f),   // Right
            new Vector2(0.5f, 0f),   // Bottom
            new Vector2(0f, 0.5f)    // Left
        };

        Vector2[] sizes = {
            new Vector2(400, 50),   // Top
            new Vector2(50, 400),   // Right
            new Vector2(400, 50),   // Bottom
            new Vector2(50, 400)    // Left
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = CreateUIElement($"DamageIndicator_{i}", transform);
            RectTransform rt = indicator.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = positions[i];
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizes[i];

            damageDirectionIndicators[i] = indicator.AddComponent<Image>();
            damageDirectionIndicators[i].color = new Color(1f, 0f, 0f, 0f);

            // Gradient texture olustur
            Texture2D gradientTex = CreateDamageGradient(i);
            damageDirectionIndicators[i].sprite = Sprite.Create(gradientTex,
                new Rect(0, 0, gradientTex.width, gradientTex.height),
                new Vector2(0.5f, 0.5f));
        }
    }

    Texture2D CreateDamageGradient(int direction)
    {
        int w = direction % 2 == 0 ? 128 : 32;
        int h = direction % 2 == 0 ? 32 : 128;
        Texture2D tex = new Texture2D(w, h);
        Color[] colors = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float alpha = 0;
                switch (direction)
                {
                    case 0: alpha = 1f - (y / (float)h); break; // Top
                    case 1: alpha = x / (float)w; break;        // Right
                    case 2: alpha = y / (float)h; break;        // Bottom
                    case 3: alpha = 1f - (x / (float)w); break; // Left
                }
                colors[y * w + x] = new Color(1, 0, 0, alpha * 0.6f);
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    // === MINI MAP ===

    void CreateMiniMap()
    {
        miniMapPanel = CreatePanel("MiniMapPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -80), new Vector2(150, 150));

        // Arka plan
        Image bg = miniMapPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.08f, 0.8f);
        AddNeonBorder(miniMapPanel.gameObject, neonCyan, 2);

        // Mini map image (RenderTexture kullanilabilir)
        GameObject mapObj = CreateUIElement("MapImage", miniMapPanel);
        RectTransform mapRt = mapObj.GetComponent<RectTransform>();
        mapRt.anchorMin = Vector2.zero;
        mapRt.anchorMax = Vector2.one;
        mapRt.offsetMin = new Vector2(5, 5);
        mapRt.offsetMax = new Vector2(-5, -5);

        miniMapImage = mapObj.AddComponent<RawImage>();
        miniMapImage.color = new Color(0.1f, 0.15f, 0.2f, 0.9f);

        // Player blip (ortada)
        GameObject playerBlipObj = CreateUIElement("PlayerBlip", mapObj.transform);
        RectTransform blipRt = playerBlipObj.GetComponent<RectTransform>();
        blipRt.anchorMin = blipRt.anchorMax = blipRt.pivot = new Vector2(0.5f, 0.5f);
        blipRt.sizeDelta = new Vector2(10, 10);

        playerBlip = playerBlipObj.AddComponent<Image>();
        playerBlip.color = neonCyan;
    }

    // === HELPER METHODS ===

    RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(transform, false);

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

    void AddNeonBorder(GameObject obj, Color color, float width)
    {
        Outline outline = obj.GetComponent<Outline>();
        if (outline == null) outline = obj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, width);

        // Ekstra glow icin ikinci outline
        Outline glow = obj.AddComponent<Outline>();
        glow.effectColor = new Color(color.r, color.g, color.b, 0.3f);
        glow.effectDistance = new Vector2(width + 2, width + 2);
    }

    // === EVENT SUBSCRIPTIONS ===

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

    // === UPDATE METHODS ===

    void UpdateAllUI()
    {
        UpdateHealthUI();
        UpdateScoreUI();
        UpdateCoinUI();
        UpdateWeaponUI();
        UpdateSkillUI();
    }

    void UpdateHealthUI()
    {
        if (GameManager.Instance == null) return;

        float current = GameManager.Instance.currentHealth;
        float max = GameManager.Instance.MaxHealth;

        displayedHealth = current;
        healthBarFill.rectTransform.anchorMax = new Vector2(current / max, 1);
        healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // Can durumuna gore renk
        float percent = current / max;
        if (percent > 0.6f)
        {
            healthBarFill.color = neonGreen;
        }
        else if (percent > 0.3f)
        {
            healthBarFill.color = neonYellow;
        }
        else
        {
            healthBarFill.color = new Color(1f, 0.2f, 0.3f);
        }

        // Kalp ikonlarini guncelle
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
            healthText.text = $"{Mathf.CeilToInt(displayedHealth)} / {Mathf.CeilToInt(max)}";
        }
    }

    void UpdateScoreUI()
    {
        if (GameManager.Instance == null) return;
        displayedScore = GameManager.Instance.GetScore();
        scoreText.text = displayedScore.ToString("N0");
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
            scoreText.text = displayedScore.ToString("N0");

            // Score artinca pulse
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

    void UpdateComboTimer()
    {
        if (GameManager.Instance == null) return;

        int combo = GameManager.Instance.GetCombo();
        int multiplier = GameManager.Instance.GetComboMultiplier();

        if (combo > 0)
        {
            comboGroup.alpha = 1f;
            comboText.text = $"{combo} HIT! x{multiplier}";

            // Combo rengini ayarla
            if (combo >= 20) comboText.color = neonPurple;
            else if (combo >= 10) comboText.color = neonPink;
            else if (combo >= 5) comboText.color = neonOrange;
            else comboText.color = neonYellow;

            // Timer bar icin ComboManager kullanilabilir
            if (ComboManager.Instance != null)
            {
                float timerPercent = ComboManager.Instance.comboTimer / ComboManager.Instance.comboTimeout;
                comboTimerFill.rectTransform.anchorMax = new Vector2(timerPercent, 1);
            }
        }
        else
        {
            comboGroup.alpha = Mathf.Lerp(comboGroup.alpha, 0f, Time.deltaTime * 3f);
        }
    }

    void UpdateCoinUI()
    {
        if (GameManager.Instance == null) return;
        coinText.text = GameManager.Instance.GetCoins().ToString("N0");
    }

    void UpdateWeaponUI()
    {
        if (WeaponManager.Instance == null) return;

        WeaponInstance weapon = WeaponManager.Instance.GetCurrentWeapon();
        if (weapon != null && weapon.isUnlocked)
        {
            weaponNameText.text = weapon.data.weaponName.ToUpper();
            weaponNameText.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
            ammoText.text = $"{weapon.currentAmmo} / {weapon.reserveAmmo}";

            // Dusuk mermi uyarisi
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

        // Slot guncelle
        UpdateWeaponSlots();
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
                    weaponSlots[i].color = new Color(0.1f, 0.1f, 0.15f, 0.7f);
                }
            }
            else
            {
                slotRarityBorders[i].color = new Color(0.2f, 0.2f, 0.2f);
                weaponSlots[i].color = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            }
        }
    }

    void UpdateSkillUI()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        // Dash
        float dashPercent = player.DashCooldownTimer / player.DashCooldownMax;
        UpdateSkillIcon(skillIcons[0], dashPercent, player.CanDash);

        // Roll
        float rollPercent = player.RollCooldownTimer / player.RollCooldownMax;
        UpdateSkillIcon(skillIcons[1], rollPercent, player.CanRoll);

        // Jump
        bool canJump = player.JumpCount < player.MaxJumpCount || player.IsGrounded;
        skillIcons[2].cooldownFill.fillAmount = canJump ? 0 : 1;
        skillIcons[2].keyLabel.color = canJump ? skillIcons[2].baseColor : new Color(0.4f, 0.4f, 0.4f);
    }

    void UpdateSkillIcon(SkillIconUI icon, float cooldownPercent, bool isReady)
    {
        icon.cooldownFill.fillAmount = Mathf.Clamp01(cooldownPercent);
        icon.keyLabel.color = isReady ? icon.baseColor : new Color(0.4f, 0.4f, 0.4f);

        if (isReady)
        {
            float pulse = 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f;
            icon.cooldownFill.color = new Color(icon.baseColor.r, icon.baseColor.g, icon.baseColor.b, 0.1f * pulse);
        }
    }

    void UpdateNeonEffects()
    {
        // Neon pulse animasyonu
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * glowIntensity;

        // Health bar glow
        if (healthBarGlow != null)
        {
            Color glowColor = healthBarFill.color;
            healthBarGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.2f + pulse * 0.2f);
        }
    }

    void UpdateMiniMap()
    {
        // Mini map icin dusman blipleri guncellenebilir
        // Su an basit bir gorsel
    }

    // === EVENT HANDLERS ===

    void OnWeaponChanged(WeaponInstance weapon)
    {
        UpdateWeaponUI();
    }

    void OnAmmoChanged(int current, int reserve)
    {
        ammoText.text = $"{current} / {reserve}";
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

    // === PUBLIC METHODS ===

    public void ShowHitMarker(bool isCrit = false)
    {
        StartCoroutine(HitMarkerAnimation(isCrit));
    }

    IEnumerator HitMarkerAnimation(bool isCrit)
    {
        Image marker = isCrit ? critMarker : hitMarker;
        Color color = isCrit ? neonOrange : Color.white;

        marker.color = new Color(color.r, color.g, color.b, 1f);
        marker.transform.localScale = Vector3.one * 1.5f;

        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            marker.color = new Color(color.r, color.g, color.b, 1f - t);
            marker.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, t);
            yield return null;
        }

        marker.color = new Color(color.r, color.g, color.b, 0f);
    }

    public void ShowDamageDirection(Vector3 damageSource)
    {
        if (Camera.main == null) return;

        // Hasar yonunu hesapla
        Vector3 playerPos = Camera.main.transform.position;
        Vector3 direction = (damageSource - playerPos).normalized;

        // Hangi indicator'i goster
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        int index = GetDirectionIndex(angle);

        StartCoroutine(DamageIndicatorAnimation(index));
    }

    int GetDirectionIndex(float angle)
    {
        if (angle >= -45 && angle < 45) return 0;      // Top
        if (angle >= 45 && angle < 135) return 1;      // Right
        if (angle >= -135 && angle < -45) return 3;    // Left
        return 2;                                       // Bottom
    }

    IEnumerator DamageIndicatorAnimation(int index)
    {
        if (index < 0 || index >= damageDirectionIndicators.Length) yield break;

        Image indicator = damageDirectionIndicators[index];
        indicator.color = new Color(1f, 0f, 0f, 0.8f);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            indicator.color = new Color(1f, 0f, 0f, 0.8f * (1f - t));
            yield return null;
        }

        indicator.color = new Color(1f, 0f, 0f, 0f);
    }

    public void AddKillFeedEntry(string killerName, string victimName, string weaponName)
    {
        StartCoroutine(ShowKillFeedEntry(killerName, victimName, weaponName));
    }

    IEnumerator ShowKillFeedEntry(string killer, string victim, string weapon)
    {
        GameObject entry = CreateUIElement("KillEntry", killFeedPanel);
        RectTransform entryRt = entry.GetComponent<RectTransform>();
        entryRt.sizeDelta = new Vector2(240, 25);

        LayoutElement le = entry.AddComponent<LayoutElement>();
        le.preferredHeight = 25;

        Image bg = entry.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        TextMeshProUGUI text = entry.AddComponent<TextMeshProUGUI>();
        text.text = $"<color=#00FFFF>{killer}</color> <color=#FF66AA>[{weapon}]</color> <color=#FF3333>{victim}</color>";
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Right;

        // 5 saniye sonra kaybol
        yield return new WaitForSeconds(5f);

        float fadeDuration = 0.5f;
        float elapsed = 0f;
        CanvasGroup cg = entry.AddComponent<CanvasGroup>();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        Destroy(entry);
    }

    public void ShowPowerUp(PowerUpType type, float duration)
    {
        if (powerUpIndicators.ContainsKey(type))
        {
            // Zaten var, sureyi guncelle
            return;
        }

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
        AddNeonBorder(indicator, powerUpColor, 1);

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

        // Sure bekle
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

    public void ShowBossHealth(string bossName, int maxHealth)
    {
        bossMaxHealth = maxHealth;

        if (bossHealthPanel == null)
        {
            CreateBossHealthBar();
        }

        bossHealthPanel.gameObject.SetActive(true);
        bossNameText.text = bossName.ToUpper();
        bossHealthFill.rectTransform.anchorMax = Vector2.one;
    }

    void CreateBossHealthBar()
    {
        bossHealthPanel = CreatePanel("BossHealthPanel",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -120), new Vector2(500, 50));

        Image bg = bossHealthPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.02f, 0.02f, 0.9f);
        AddNeonBorder(bossHealthPanel.gameObject, new Color(1f, 0.2f, 0.2f), 2);

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

    public void UpdateBossHealth(int currentHealth)
    {
        if (bossHealthFill == null) return;

        float percent = (float)currentHealth / bossMaxHealth;
        bossHealthFill.rectTransform.anchorMax = new Vector2(percent, 1);

        // Renk degisimi
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
        {
            bossHealthPanel.gameObject.SetActive(false);
        }
    }
}

// Helper Classes
public class SkillIconUI
{
    public RectTransform container;
    public Image background;
    public Image cooldownFill;
    public TextMeshProUGUI keyLabel;
    public Color baseColor;
}

public class PowerUpIndicatorUI
{
    public GameObject container;
    public Image icon;
    public TextMeshProUGUI text;
}

public class KillFeedEntry
{
    public GameObject gameObject;
    public float creationTime;
}
