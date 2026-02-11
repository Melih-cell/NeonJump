using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Boss health bar - Ekranin ustunde buyuk ve gosterisli
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance { get; private set; }

    [Header("Settings")]
    public float showDuration = 0.5f;
    public float hideDuration = 0.3f;
    public float damageFlashDuration = 0.1f;

    private Canvas canvas;
    private RectTransform container;
    private CanvasGroup canvasGroup;

    private Image backgroundBar;
    private Image healthFill;
    private Image damageFill; // Hasar gecis efekti
    private Image glowEffect;
    private TextMeshProUGUI bossNameText;
    private TextMeshProUGUI healthPercentText;

    // Boss bilgileri
    private bool isShowing = false;
    private float currentHealth;
    private float maxHealth;
    private float displayedHealth;
    private float damageDisplayHealth;

    void Awake()
    {
        Instance = this;
        SetupUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (isShowing)
        {
            UpdateHealthDisplay();
        }
    }

    void SetupUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("BossHealthCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Ana container
        GameObject containerObj = new GameObject("BossBarContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);

        container = containerObj.AddComponent<RectTransform>();
        container.anchorMin = new Vector2(0.5f, 1);
        container.anchorMax = new Vector2(0.5f, 1);
        container.pivot = new Vector2(0.5f, 1);
        container.anchoredPosition = new Vector2(0, -20);
        container.sizeDelta = new Vector2(800, 80);

        canvasGroup = containerObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        // Dekoratif cerceve
        CreateFrame();

        // Background bar
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(container, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.3f);
        bgRt.anchorMax = new Vector2(1, 0.7f);
        bgRt.offsetMin = new Vector2(100, 0);
        bgRt.offsetMax = new Vector2(-100, 0);

        backgroundBar = bgObj.AddComponent<Image>();
        backgroundBar.color = new Color(0.03f, 0.01f, 0.06f, 0.95f);

        // Neon sinir cizgisi (bar etrafinda)
        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0f, 1f, 1f, 0.5f);
        bgOutline.effectDistance = new Vector2(1, 1);

        // Damage fill (turuncu-kirmizi gecis efekti)
        GameObject dmgObj = new GameObject("DamageFill");
        dmgObj.transform.SetParent(bgObj.transform, false);
        RectTransform dmgRt = dmgObj.AddComponent<RectTransform>();
        dmgRt.anchorMin = new Vector2(0, 0);
        dmgRt.anchorMax = new Vector2(1, 1);
        dmgRt.pivot = new Vector2(0, 0.5f);
        dmgRt.offsetMin = new Vector2(2, 2);
        dmgRt.offsetMax = new Vector2(-2, -2);

        damageFill = dmgObj.AddComponent<Image>();
        damageFill.color = new Color(1f, 0.4f, 0f, 0.7f);

        // Health fill - magenta/cyan neon gradient
        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);

        healthFill = fillObj.AddComponent<Image>();
        healthFill.color = new Color(0f, 0.9f, 0.9f, 1f); // Neon cyan

        // Gradient overlay (uste dogru aydinlik)
        GameObject gradientObj = new GameObject("GradientOverlay");
        gradientObj.transform.SetParent(fillObj.transform, false);
        RectTransform gradRt = gradientObj.AddComponent<RectTransform>();
        gradRt.anchorMin = new Vector2(0, 0.5f);
        gradRt.anchorMax = Vector2.one;
        gradRt.offsetMin = Vector2.zero;
        gradRt.offsetMax = Vector2.zero;

        Image gradient = gradientObj.AddComponent<Image>();
        gradient.color = new Color(1f, 1f, 1f, 0.15f);
        gradient.raycastTarget = false;

        // Glow efekti
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(fillObj.transform, false);
        RectTransform glowRt = glowObj.AddComponent<RectTransform>();
        glowRt.anchorMin = new Vector2(0, 0);
        glowRt.anchorMax = new Vector2(1, 1);
        glowRt.offsetMin = new Vector2(-4, -4);
        glowRt.offsetMax = new Vector2(4, 4);

        glowEffect = glowObj.AddComponent<Image>();
        glowEffect.color = new Color(0f, 1f, 1f, 0.2f);
        glowEffect.raycastTarget = false;

        // Boss name
        GameObject nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(container, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.7f);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.offsetMin = new Vector2(0, 0);
        nameRt.offsetMax = new Vector2(0, 0);

        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
        bossNameText.text = "BOSS";
        bossNameText.fontSize = 26;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.alignment = TextAlignmentOptions.Center;
        bossNameText.color = new Color(0f, 1f, 1f, 1f);

        // Name outline - koyu cyan glisten
        Outline nameOutline = nameObj.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0f, 0.3f, 0.4f, 0.8f);
        nameOutline.effectDistance = new Vector2(1, 1);

        // Health percent
        GameObject percentObj = new GameObject("HealthPercent");
        percentObj.transform.SetParent(container, false);
        RectTransform percentRt = percentObj.AddComponent<RectTransform>();
        percentRt.anchorMin = new Vector2(0, 0);
        percentRt.anchorMax = new Vector2(1, 0.3f);
        percentRt.offsetMin = new Vector2(0, 0);
        percentRt.offsetMax = new Vector2(0, 0);

        healthPercentText = percentObj.AddComponent<TextMeshProUGUI>();
        healthPercentText.text = "100%";
        healthPercentText.fontSize = 18;
        healthPercentText.alignment = TextAlignmentOptions.Center;
        healthPercentText.color = new Color(0.9f, 0.9f, 0.9f);
    }

    void CreateFrame()
    {
        // Sol dekoratif eleman
        CreateFrameElement("LeftFrame", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(80, 60), true);

        // Sag dekoratif eleman
        CreateFrameElement("RightFrame", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(80, 60), false);

        // Ust cizgi
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(container, false);
        RectTransform topRt = topLine.AddComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0.1f, 0.85f);
        topRt.anchorMax = new Vector2(0.9f, 0.87f);
        topRt.offsetMin = Vector2.zero;
        topRt.offsetMax = Vector2.zero;

        Image topImg = topLine.AddComponent<Image>();
        topImg.color = new Color(0f, 1f, 1f, 0.7f);

        // Alt cizgi
        GameObject botLine = new GameObject("BottomLine");
        botLine.transform.SetParent(container, false);
        RectTransform botRt = botLine.AddComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0.1f, 0.13f);
        botRt.anchorMax = new Vector2(0.9f, 0.15f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;

        Image botImg = botLine.AddComponent<Image>();
        botImg.color = new Color(0f, 1f, 1f, 0.7f);
    }

    void CreateFrameElement(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, bool isLeft)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(container, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = isLeft ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = isLeft ? new Vector2(10, 0) : new Vector2(-10, 0);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0f, 0.8f, 0.9f, 0.85f);

        // Ic bosluk
        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(obj.transform, false);
        RectTransform innerRt = inner.AddComponent<RectTransform>();
        innerRt.anchorMin = new Vector2(0.1f, 0.15f);
        innerRt.anchorMax = new Vector2(0.9f, 0.85f);
        innerRt.offsetMin = Vector2.zero;
        innerRt.offsetMax = Vector2.zero;

        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.02f, 0.01f, 0.05f, 1f);
    }

    void UpdateHealthDisplay()
    {
        // Yumusak gecis
        displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * 10f);
        damageDisplayHealth = Mathf.Lerp(damageDisplayHealth, currentHealth, Time.deltaTime * 3f);

        float healthPercent = displayedHealth / maxHealth;
        float damagePercent = damageDisplayHealth / maxHealth;

        // Fill'leri guncelle
        healthFill.rectTransform.anchorMax = new Vector2(healthPercent, 1);
        damageFill.rectTransform.anchorMax = new Vector2(damagePercent, 1);

        // Yuzde text
        healthPercentText.text = $"{Mathf.CeilToInt(healthPercent * 100)}%";

        // Renk degisimi - can durumuna gore cyan -> magenta -> kirmizi
        if (healthPercent < 0.25f)
        {
            // Kritik - kirmizi pulse
            float pulse = 0.7f + Mathf.Sin(Time.time * 6f) * 0.3f;
            healthFill.color = new Color(1f * pulse, 0.1f, 0.2f * pulse, 1f);
            glowEffect.color = new Color(1f, 0.1f, 0.2f, 0.4f * pulse);
        }
        else if (healthPercent < 0.5f)
        {
            // Orta - magenta/pembe
            healthFill.color = new Color(0.9f, 0.1f, 0.6f, 1f);
            glowEffect.color = new Color(0.9f, 0.1f, 0.6f, 0.25f);
        }
        else
        {
            // Normal - neon cyan
            healthFill.color = new Color(0f, 0.9f, 0.9f, 1f);
            glowEffect.color = new Color(0f, 1f, 1f, 0.2f);
        }

        // Boss name hafif pulse
        float nameGlow = 0.85f + Mathf.Sin(Time.time * 2f) * 0.15f;
        bossNameText.color = new Color(0f, nameGlow, nameGlow);
    }

    // === PUBLIC METODLAR ===

    public void ShowBoss(string bossName, float health, float max)
    {
        currentHealth = health;
        maxHealth = max;
        displayedHealth = health;
        damageDisplayHealth = health;

        bossNameText.text = bossName.ToUpper();

        StartCoroutine(ShowAnimation());

        // Bildirim goster
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowBossAppear(bossName);
        }
    }

    public void UpdateHealth(float health)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, health);

        // Hasar aldiysa flash
        if (currentHealth < previousHealth)
        {
            StartCoroutine(DamageFlash());
        }
    }

    public void HideBoss()
    {
        StartCoroutine(HideAnimation());
    }

    IEnumerator ShowAnimation()
    {
        isShowing = true;

        // Baslangic
        container.anchoredPosition = new Vector2(0, 100);
        canvasGroup.alpha = 0;

        float elapsed = 0f;
        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutBack(elapsed / showDuration);

            container.anchoredPosition = Vector2.Lerp(new Vector2(0, 100), new Vector2(0, -20), t);
            canvasGroup.alpha = t;

            yield return null;
        }

        container.anchoredPosition = new Vector2(0, -20);
        canvasGroup.alpha = 1;

        // Gosterisli giris efekti
        StartCoroutine(IntroFlash());
    }

    IEnumerator IntroFlash()
    {
        Color originalColor = backgroundBar.color;

        // Cyan flash
        backgroundBar.color = new Color(0f, 1f, 1f, 0.8f);
        yield return new WaitForSeconds(0.1f);

        // Magenta flash
        backgroundBar.color = new Color(0.8f, 0f, 0.6f, 0.8f);
        yield return new WaitForSeconds(0.1f);

        // Normale don
        backgroundBar.color = originalColor;
    }

    IEnumerator HideAnimation()
    {
        float elapsed = 0f;
        Vector2 startPos = container.anchoredPosition;

        while (elapsed < hideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hideDuration;

            container.anchoredPosition = Vector2.Lerp(startPos, new Vector2(0, 100), t);
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        canvasGroup.alpha = 0;
        isShowing = false;
    }

    IEnumerator DamageFlash()
    {
        Color originalColor = backgroundBar.color;

        backgroundBar.color = new Color(0.8f, 0f, 0.4f, 0.95f);
        yield return new WaitForSeconds(damageFlashDuration);

        backgroundBar.color = originalColor;
    }

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
