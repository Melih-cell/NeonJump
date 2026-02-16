using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gelismis HUD sistemi - Health bar, ammo, skill cooldowns
/// </summary>
public class AdvancedHUD : MonoBehaviour
{
    public static AdvancedHUD Instance { get; private set; }

    [Header("References")]
    private Canvas canvas;
    private PlayerController player;

    [Header("Health Bar")]
    private RectTransform healthBarContainer;
    private Image healthBarFill;
    private Image healthBarBackground;
    private Image healthBarGlow;
    private TextMeshProUGUI healthText;
    private float displayedHealth;
    private float healthAnimSpeed = 5f;

    [Header("Ammo Display")]
    private RectTransform ammoContainer;
    private TextMeshProUGUI ammoText;
    private Image ammoIcon;
    private List<Image> ammoBullets = new List<Image>();

    [Header("Skill Cooldowns")]
    private RectTransform skillContainer;
    private SkillSlotUI dashSlot;
    private SkillSlotUI rollSlot;
    private SkillSlotUI jumpSlot;
    private SkillSlotUI grappleSlot;
    private SkillSlotUI groundPoundSlot;

    [Header("Combo Display")]
    private RectTransform comboContainer;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI comboMultiplierText;
    private Image comboFill;
    private CanvasGroup comboCanvasGroup;
    private float comboDisplayTimer;

    [Header("Score Display")]
    private TextMeshProUGUI scoreText;
    private int displayedScore;

    [Header("Coin Display")]
    private TextMeshProUGUI coinText;
    private Image coinIcon;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupCanvas();
        SetupHealthBar();
        SetupAmmoDisplay();
        SetupSkillCooldowns();
        SetupComboDisplay();
        SetupScoreDisplay();
        SetupCoinDisplay();

        // Player referansini bul
        StartCoroutine(FindPlayerDelayed());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    IEnumerator FindPlayerDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        UpdateHealthBar();
        UpdateAmmoDisplay();
        UpdateSkillCooldowns();
        UpdateComboDisplay();
        UpdateScoreDisplay();
    }

    void SetupCanvas()
    {
        // Mevcut canvas'i bul veya olustur
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUDCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform, false);
        }

        // Mobil DPI ayari
        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;
        if (isMobile)
        {
            CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
            if (cs != null)
            {
                cs.referenceResolution = new Vector2(1280, 720);
                cs.matchWidthOrHeight = 0.5f;
            }
        }

        // Safe Area paneli ekle
        GameObject safeAreaObj = new GameObject("SafeAreaPanel");
        safeAreaObj.transform.SetParent(transform, false);
        RectTransform safeRt = safeAreaObj.AddComponent<RectTransform>();
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = Vector2.zero;
        safeRt.offsetMax = Vector2.zero;
        safeAreaObj.AddComponent<SafeAreaHandler>();
    }

    /// <summary>
    /// Screen DPI'a gore font boyutu olcekler.
    /// Mobilde okunabilirlik icin minimum boyut uygular.
    /// </summary>
    float GetScaledFontSize(float baseFontSize)
    {
        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;
        if (!isMobile) return baseFontSize;

        // Yuksek DPI ekranlarda font boyutunu arttir
        float dpiScale = Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f);
        float scaled = baseFontSize * dpiScale;
        // Mobilde minimum font boyutu 14
        return Mathf.Max(scaled, 14f);
    }

    // === HEALTH BAR ===

    void SetupHealthBar()
    {
        // Container
        GameObject containerObj = new GameObject("HealthBarContainer");
        containerObj.transform.SetParent(transform, false);

        healthBarContainer = containerObj.AddComponent<RectTransform>();
        healthBarContainer.anchorMin = new Vector2(0, 1);
        healthBarContainer.anchorMax = new Vector2(0, 1);
        healthBarContainer.pivot = new Vector2(0, 1);
        healthBarContainer.anchoredPosition = new Vector2(30, -30);
        healthBarContainer.sizeDelta = new Vector2(300, 40);

        // Background (koyu)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(healthBarContainer, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        healthBarBackground = bgObj.AddComponent<Image>();
        healthBarBackground.color = new Color(0.1f, 0.05f, 0.1f, 0.9f);

        // Neon border
        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0f, 1f, 1f, 0.5f);
        bgOutline.effectDistance = new Vector2(2, 2);

        // Fill (can barı)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarContainer, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = new Vector2(4, 4);
        fillRt.offsetMax = new Vector2(-4, -4);

        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = new Color(0f, 1f, 0.5f, 1f); // Neon yesil

        // Glow efekti
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(fillObj.transform, false);
        RectTransform glowRt = glowObj.AddComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.sizeDelta = new Vector2(10, 10);
        glowRt.anchoredPosition = Vector2.zero;

        healthBarGlow = glowObj.AddComponent<Image>();
        healthBarGlow.color = new Color(0f, 1f, 0.5f, 0.3f);

        // Health text
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(healthBarContainer, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        healthText = textObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "100 / 100";
        healthText.fontSize = GetScaledFontSize(18);
        healthText.fontStyle = FontStyles.Bold;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;

        // HP ikonu
        GameObject iconObj = new GameObject("HPIcon");
        iconObj.transform.SetParent(healthBarContainer, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(1, 0.5f);
        iconRt.anchoredPosition = new Vector2(-10, 0);
        iconRt.sizeDelta = new Vector2(30, 30);

        Image hpIcon = iconObj.AddComponent<Image>();
        hpIcon.color = new Color(1f, 0.3f, 0.3f);

        // Heart seklinde mask (basit kare simdilik)
        TextMeshProUGUI hpSymbol = iconObj.AddComponent<TextMeshProUGUI>();
        hpSymbol.text = "♥";
        hpSymbol.fontSize = 28;
        hpSymbol.alignment = TextAlignmentOptions.Center;
        hpSymbol.color = new Color(1f, 0.2f, 0.3f);
    }

    void UpdateHealthBar()
    {
        if (GameManager.Instance == null) return;

        float currentHealth = GameManager.Instance.currentHealth;
        float maxHealth = GameManager.Instance.maxHealth;

        // Yumusak gecis
        displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * healthAnimSpeed);

        float healthPercent = displayedHealth / maxHealth;
        healthBarFill.rectTransform.anchorMax = new Vector2(healthPercent, 1);

        // Renk degisimi (dusuk canda kirmizi)
        Color healthColor;
        if (healthPercent > 0.6f)
            healthColor = new Color(0f, 1f, 0.5f); // Yesil
        else if (healthPercent > 0.3f)
            healthColor = new Color(1f, 0.8f, 0f); // Sari
        else
            healthColor = new Color(1f, 0.2f, 0.2f); // Kirmizi

        healthBarFill.color = healthColor;
        healthBarGlow.color = new Color(healthColor.r, healthColor.g, healthColor.b, 0.3f);

        // Text
        healthText.text = $"{Mathf.CeilToInt(displayedHealth)} / {Mathf.CeilToInt(maxHealth)}";

        // Dusuk can pulse efekti
        if (healthPercent < 0.3f)
        {
            float pulse = 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
            healthBarFill.color = new Color(healthColor.r * pulse, healthColor.g * pulse, healthColor.b * pulse);
        }
    }

    // === AMMO DISPLAY ===

    void SetupAmmoDisplay()
    {
        // Container
        GameObject containerObj = new GameObject("AmmoContainer");
        containerObj.transform.SetParent(transform, false);

        ammoContainer = containerObj.AddComponent<RectTransform>();
        ammoContainer.anchorMin = new Vector2(1, 0);
        ammoContainer.anchorMax = new Vector2(1, 0);
        ammoContainer.pivot = new Vector2(1, 0);
        ammoContainer.anchoredPosition = new Vector2(-30, 30);
        ammoContainer.sizeDelta = new Vector2(200, 60);

        // Background
        Image bg = containerObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.05f, 0.15f, 0.8f);

        Outline outline = containerObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.5f, 0f, 0.5f);
        outline.effectDistance = new Vector2(2, 2);

        // Ammo text
        GameObject textObj = new GameObject("AmmoText");
        textObj.transform.SetParent(ammoContainer, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(50, 5);
        textRt.offsetMax = new Vector2(-10, -5);

        ammoText = textObj.AddComponent<TextMeshProUGUI>();
        ammoText.text = "∞";
        ammoText.fontSize = GetScaledFontSize(32);
        ammoText.fontStyle = FontStyles.Bold;
        ammoText.alignment = TextAlignmentOptions.Right;
        ammoText.color = new Color(1f, 0.8f, 0f);

        // Bullet icon container
        GameObject bulletContainer = new GameObject("BulletIcons");
        bulletContainer.transform.SetParent(ammoContainer, false);
        RectTransform bulletRt = bulletContainer.AddComponent<RectTransform>();
        bulletRt.anchorMin = new Vector2(0, 0.5f);
        bulletRt.anchorMax = new Vector2(0, 0.5f);
        bulletRt.pivot = new Vector2(0, 0.5f);
        bulletRt.anchoredPosition = new Vector2(10, 0);
        bulletRt.sizeDelta = new Vector2(40, 40);

        // Mermi ikonu
        ammoIcon = bulletContainer.AddComponent<Image>();
        ammoIcon.color = new Color(1f, 0.6f, 0f);
    }

    void UpdateAmmoDisplay()
    {
        // Weapon system ile entegre edilecek
        // Simdilik sonsuz goster
        ammoText.text = "∞";
    }

    // === SKILL COOLDOWNS ===

    void SetupSkillCooldowns()
    {
        // Container
        GameObject containerObj = new GameObject("SkillContainer");
        containerObj.transform.SetParent(transform, false);

        skillContainer = containerObj.AddComponent<RectTransform>();
        skillContainer.anchorMin = new Vector2(0, 0);
        skillContainer.anchorMax = new Vector2(0, 0);
        skillContainer.pivot = new Vector2(0, 0);
        skillContainer.anchoredPosition = new Vector2(30, 30);
        skillContainer.sizeDelta = new Vector2(200, 60);

        HorizontalLayoutGroup hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Skill slotlari olustur
        dashSlot = CreateSkillSlot("Dash", "D", new Color(0f, 1f, 1f));
        rollSlot = CreateSkillSlot("Roll", "R", new Color(1f, 0.5f, 0f));
        jumpSlot = CreateSkillSlot("Jump", "J", new Color(0.5f, 1f, 0.5f));
        grappleSlot = CreateSkillSlot("Grapple", "Q", new Color(0f, 0.8f, 1f));
        groundPoundSlot = CreateSkillSlot("GPound", "S+J", new Color(1f, 0.3f, 0f));
    }

    SkillSlotUI CreateSkillSlot(string skillName, string keyLabel, Color color)
    {
        GameObject slotObj = new GameObject($"Skill_{skillName}");
        slotObj.transform.SetParent(skillContainer, false);

        RectTransform slotRt = slotObj.AddComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(50, 50);

        LayoutElement le = slotObj.AddComponent<LayoutElement>();
        le.preferredWidth = 50;
        le.preferredHeight = 50;

        // Background
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        Outline outline = slotObj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, 2);

        // Cooldown fill (radial)
        GameObject fillObj = new GameObject("CooldownFill");
        fillObj.transform.SetParent(slotObj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3, 3);
        fillRt.offsetMax = new Vector2(-3, -3);

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(color.r, color.g, color.b, 0.5f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Radial360;
        fillImg.fillOrigin = (int)Image.Origin360.Top;
        fillImg.fillClockwise = false;
        fillImg.fillAmount = 0;

        // Key label
        GameObject labelObj = new GameObject("KeyLabel");
        labelObj.transform.SetParent(slotObj.transform, false);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = keyLabel;
        labelTmp.fontSize = 24;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = color;

        return new SkillSlotUI
        {
            container = slotRt,
            background = bg,
            cooldownFill = fillImg,
            keyLabel = labelTmp,
            baseColor = color
        };
    }

    void UpdateSkillCooldowns()
    {
        if (player == null) return;

        // Dash cooldown
        float dashCooldownPercent = player.DashCooldownTimer / player.DashCooldownMax;
        UpdateSkillSlot(dashSlot, dashCooldownPercent, player.CanDash);

        // Roll cooldown
        float rollCooldownPercent = player.RollCooldownTimer / player.RollCooldownMax;
        UpdateSkillSlot(rollSlot, rollCooldownPercent, player.CanRoll);

        // Double jump (remaining jumps)
        bool canJump = player.JumpCount < player.MaxJumpCount || player.IsGrounded;
        jumpSlot.cooldownFill.fillAmount = canJump ? 0 : 1;
        jumpSlot.keyLabel.color = canJump ? jumpSlot.baseColor : new Color(0.5f, 0.5f, 0.5f);

        // Grapple cooldown
        float grappleCooldownPercent = player.GrappleCooldownTimer / player.GrappleCooldownMax;
        UpdateSkillSlot(grappleSlot, grappleCooldownPercent, player.CanGrapple);

        // Ground Pound cooldown
        float gpCooldownPercent = player.GroundPoundCooldownTimer / player.GroundPoundCooldownMax;
        UpdateSkillSlot(groundPoundSlot, gpCooldownPercent, player.CanGroundPound);
    }

    void UpdateSkillSlot(SkillSlotUI slot, float cooldownPercent, bool isReady)
    {
        slot.cooldownFill.fillAmount = Mathf.Clamp01(cooldownPercent);

        if (isReady)
        {
            slot.keyLabel.color = slot.baseColor;
            // Ready pulse
            float pulse = 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f;
            slot.cooldownFill.color = new Color(slot.baseColor.r, slot.baseColor.g, slot.baseColor.b, 0.1f * pulse);
        }
        else
        {
            slot.keyLabel.color = new Color(0.5f, 0.5f, 0.5f);
            slot.cooldownFill.color = new Color(slot.baseColor.r * 0.5f, slot.baseColor.g * 0.5f, slot.baseColor.b * 0.5f, 0.5f);
        }
    }

    // === COMBO DISPLAY ===

    void SetupComboDisplay()
    {
        // Container
        GameObject containerObj = new GameObject("ComboContainer");
        containerObj.transform.SetParent(transform, false);

        comboContainer = containerObj.AddComponent<RectTransform>();
        comboContainer.anchorMin = new Vector2(1, 1);
        comboContainer.anchorMax = new Vector2(1, 1);
        comboContainer.pivot = new Vector2(1, 1);
        comboContainer.anchoredPosition = new Vector2(-30, -100);
        comboContainer.sizeDelta = new Vector2(200, 80);

        comboCanvasGroup = containerObj.AddComponent<CanvasGroup>();
        comboCanvasGroup.alpha = 0;

        // Background
        Image bg = containerObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.05f, 0.2f, 0.8f);

        Outline outline = containerObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.5f, 0f, 0.7f);
        outline.effectDistance = new Vector2(2, 2);

        // Combo text
        GameObject textObj = new GameObject("ComboText");
        textObj.transform.SetParent(comboContainer, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0.5f);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(10, 0);
        textRt.offsetMax = new Vector2(-10, -5);

        comboText = textObj.AddComponent<TextMeshProUGUI>();
        comboText.text = "0 HIT";
        comboText.fontSize = 28;
        comboText.fontStyle = FontStyles.Bold;
        comboText.alignment = TextAlignmentOptions.Right;
        comboText.color = new Color(1f, 0.8f, 0f);

        // Multiplier text
        GameObject multObj = new GameObject("MultiplierText");
        multObj.transform.SetParent(comboContainer, false);
        RectTransform multRt = multObj.AddComponent<RectTransform>();
        multRt.anchorMin = new Vector2(0, 0);
        multRt.anchorMax = new Vector2(1, 0.5f);
        multRt.offsetMin = new Vector2(10, 5);
        multRt.offsetMax = new Vector2(-10, 0);

        comboMultiplierText = multObj.AddComponent<TextMeshProUGUI>();
        comboMultiplierText.text = "x1";
        comboMultiplierText.fontSize = 20;
        comboMultiplierText.alignment = TextAlignmentOptions.Right;
        comboMultiplierText.color = new Color(1f, 0.6f, 0f);

        // Combo timer fill
        GameObject fillObj = new GameObject("ComboFill");
        fillObj.transform.SetParent(comboContainer, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 0);
        fillRt.pivot = new Vector2(0, 0);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = new Vector2(0, 4);

        comboFill = fillObj.AddComponent<Image>();
        comboFill.color = new Color(1f, 0.5f, 0f);
    }

    void UpdateComboDisplay()
    {
        if (ComboManager.Instance == null)
        {
            comboCanvasGroup.alpha = 0;
            return;
        }

        int combo = ComboManager.Instance.currentCombo;
        int multiplier = ComboManager.Instance.multiplier;
        float comboTimer = ComboManager.Instance.comboTimer;
        float maxComboTime = ComboManager.Instance.comboTimeout;

        if (combo > 0)
        {
            comboCanvasGroup.alpha = 1;
            comboText.text = $"{combo} HIT";
            comboMultiplierText.text = $"x{multiplier}";

            // Timer fill
            comboFill.rectTransform.anchorMax = new Vector2(comboTimer / maxComboTime, 1);

            // Renk gecisleri: beyaz -> sari -> turuncu -> kirmizi
            Color comboColor;
            if (combo >= 20)
                comboColor = new Color(1f, 0f, 0.3f); // Kirmizi
            else if (combo >= 10)
                comboColor = new Color(1f, 0.4f, 0f); // Turuncu
            else if (combo >= 5)
                comboColor = new Color(1f, 0.8f, 0f); // Sari
            else
                comboColor = Color.white;

            comboText.color = comboColor;
            comboFill.color = comboColor;

            // Combo sayisina gore buyuyen pulse animasyonu
            float pulseScale = 1f + Mathf.Min(combo * 0.02f, 0.4f);
            float pulse = 1f + Mathf.Sin(Time.time * (5f + combo * 0.5f)) * 0.05f * pulseScale;
            comboContainer.localScale = Vector3.one * pulse;

            // Font boyutu artisi
            comboText.fontSize = Mathf.Min(28 + combo, 48);

            comboDisplayTimer = 1f;
        }
        else if (comboDisplayTimer > 0)
        {
            comboDisplayTimer -= Time.deltaTime;
            comboCanvasGroup.alpha = comboDisplayTimer;
        }
    }

    Color GetComboColor(int combo)
    {
        if (combo >= 20) return new Color(1f, 0f, 1f);      // Mor
        if (combo >= 15) return new Color(1f, 0f, 0.5f);    // Pembe
        if (combo >= 10) return new Color(1f, 0.3f, 0f);    // Kirmizi
        if (combo >= 5) return new Color(1f, 0.6f, 0f);     // Turuncu
        return new Color(1f, 0.8f, 0f);                      // Sari
    }

    // === SCORE DISPLAY ===

    void SetupScoreDisplay()
    {
        // Score container
        GameObject scoreObj = new GameObject("ScoreDisplay");
        scoreObj.transform.SetParent(transform, false);

        RectTransform scoreRt = scoreObj.AddComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.5f, 1);
        scoreRt.anchorMax = new Vector2(0.5f, 1);
        scoreRt.pivot = new Vector2(0.5f, 1);
        scoreRt.anchoredPosition = new Vector2(0, -20);
        scoreRt.sizeDelta = new Vector2(300, 50);

        scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "0";
        scoreText.fontSize = GetScaledFontSize(42);
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = new Color(0f, 1f, 1f);

        // Glow efekti
        Outline outline = scoreObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0.5f);
        outline.effectDistance = new Vector2(3, 3);
    }

    void UpdateScoreDisplay()
    {
        if (GameManager.Instance == null) return;

        int targetScore = GameManager.Instance.GetScore();

        // Yumusak gecis
        if (displayedScore != targetScore)
        {
            int diff = targetScore - displayedScore;
            int step = Mathf.Max(1, Mathf.Abs(diff) / 10);

            if (diff > 0)
                displayedScore = Mathf.Min(displayedScore + step, targetScore);
            else
                displayedScore = Mathf.Max(displayedScore - step, targetScore);

            scoreText.text = displayedScore.ToString("N0");

            // Score artinca pulse
            if (diff > 0)
            {
                float pulse = 1f + (diff / 100f) * 0.1f;
                pulse = Mathf.Min(pulse, 1.3f);
                scoreText.transform.localScale = Vector3.one * pulse;
            }
        }
        else
        {
            // Normal boyuta don
            scoreText.transform.localScale = Vector3.Lerp(
                scoreText.transform.localScale,
                Vector3.one,
                Time.deltaTime * 5f
            );
        }
    }

    // === COIN DISPLAY ===

    void SetupCoinDisplay()
    {
        // Coin container
        GameObject coinObj = new GameObject("CoinDisplay");
        coinObj.transform.SetParent(transform, false);

        RectTransform coinRt = coinObj.AddComponent<RectTransform>();
        coinRt.anchorMin = new Vector2(0, 1);
        coinRt.anchorMax = new Vector2(0, 1);
        coinRt.pivot = new Vector2(0, 1);
        coinRt.anchoredPosition = new Vector2(30, -80);
        coinRt.sizeDelta = new Vector2(150, 35);

        // Background
        Image bg = coinObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.02f, 0.8f);

        Outline outline = coinObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0f, 0.5f);
        outline.effectDistance = new Vector2(1, 1);

        // Coin icon
        GameObject iconObj = new GameObject("CoinIcon");
        iconObj.transform.SetParent(coinObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(8, 0);
        iconRt.sizeDelta = new Vector2(25, 25);

        coinIcon = iconObj.AddComponent<Image>();
        coinIcon.color = new Color(1f, 0.85f, 0f);

        // Coin text
        GameObject textObj = new GameObject("CoinText");
        textObj.transform.SetParent(coinObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(40, 0);
        textRt.offsetMax = new Vector2(-10, 0);

        coinText = textObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = GetScaledFontSize(22);
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.Left;
        coinText.color = new Color(1f, 0.9f, 0.3f);
    }

    public void UpdateCoinDisplay(int coins)
    {
        if (coinText != null)
        {
            coinText.text = coins.ToString("N0");
        }
    }

    // === PUBLIC METODLAR ===

    public void OnHealthChanged(float newHealth, float maxHealth)
    {
        // Hasar aldiginda flash efekti
        if (newHealth < displayedHealth)
        {
            StartCoroutine(HealthFlash());
        }
    }

    IEnumerator HealthFlash()
    {
        Color originalColor = healthBarBackground.color;
        healthBarBackground.color = new Color(1f, 0f, 0f, 0.8f);

        yield return new WaitForSeconds(0.1f);

        healthBarBackground.color = originalColor;
    }

    public void OnComboChanged(int combo, int multiplier)
    {
        if (combo > 0)
        {
            // Combo artinca pulse
            StartCoroutine(ComboPulse());
        }
    }

    IEnumerator ComboPulse()
    {
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            comboContainer.localScale = Vector3.one * scale;

            yield return null;
        }

        comboContainer.localScale = Vector3.one;
    }

    public void ShowDamageIndicator(Vector3 damageDirection)
    {
        StartCoroutine(ShowDamageDirectionOverlay(damageDirection));
    }

    private IEnumerator ShowDamageDirectionOverlay(Vector3 damageDirection)
    {
        // Hangi kenar? Hasar yonune gore
        string side;
        Vector2 anchorMin, anchorMax;

        if (Mathf.Abs(damageDirection.x) > Mathf.Abs(damageDirection.y))
        {
            if (damageDirection.x > 0)
            {
                side = "Right";
                anchorMin = new Vector2(0.9f, 0f);
                anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                side = "Left";
                anchorMin = new Vector2(0f, 0f);
                anchorMax = new Vector2(0.1f, 1f);
            }
        }
        else
        {
            if (damageDirection.y > 0)
            {
                side = "Top";
                anchorMin = new Vector2(0f, 0.9f);
                anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                side = "Bottom";
                anchorMin = new Vector2(0f, 0f);
                anchorMax = new Vector2(1f, 0.1f);
            }
        }

        // Overlay olustur
        GameObject overlay = new GameObject($"DmgIndicator_{side}");
        overlay.transform.SetParent(transform, false);

        RectTransform rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = overlay.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0.4f);
        img.raycastTarget = false;

        // 0.5s flash sonra sol
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            img.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        Destroy(overlay);
    }
}

// Skill slot yapisi
public class SkillSlotUI
{
    public RectTransform container;
    public Image background;
    public Image cooldownFill;
    public TextMeshProUGUI keyLabel;
    public Color baseColor;
}
