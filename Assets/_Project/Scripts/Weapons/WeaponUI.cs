using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gelişmiş Silah UI - mermi, slot göstergeleri, reload bar
/// </summary>
public class WeaponUI : MonoBehaviour
{
    public static WeaponUI Instance;

    [Header("UI Elements")]
    public Text weaponNameText;
    public Text ammoText;
    public Text levelText;           // Level göstergesi
    public Text statsText;           // Stat bilgisi
    public GameObject reloadBar;
    public Image reloadFill;
    public Image[] slotImages;
    public Image[] slotBorders;
    public Image[] slotIcons;
    public Image[] slotLevelBars;    // Her slot için level bar
    public Image rarityGlow;         // Rarity parıltı efekti

    [Header("Colors")]
    public Color activeSlotColor = new Color(1f, 0.8f, 0.2f);
    public Color inactiveSlotColor = new Color(0.4f, 0.4f, 0.4f);
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f);
    public Color lowAmmoColor = new Color(1f, 0.3f, 0.3f);
    public Color reloadColor = new Color(0.3f, 0.8f, 1f);

    private Canvas canvas;
    private GameObject weaponPanel;
    private Coroutine reloadCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateUI();
        StartCoroutine(DelayedSubscribe());
    }

    IEnumerator DelayedSubscribe()
    {
        // WeaponManager'ın başlamasını bekle
        yield return new WaitForSeconds(0.2f);
        SubscribeToEvents();
    }

    void CreateUI()
    {
        // Canvas bul veya oluştur
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("WeaponCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ana panel - sağ alt köşe
        weaponPanel = new GameObject("WeaponPanel");
        weaponPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = weaponPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20);
        panelRect.sizeDelta = new Vector2(280, 140);

        // Panel arka planı
        Image panelBg = weaponPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);

        // Silah adı
        CreateWeaponNameText();

        // Level ve rarity göstergesi
        CreateLevelDisplay();

        // Mermi göstergesi
        CreateAmmoText();

        // Reload bar
        CreateReloadBar();

        // Slot göstergeleri
        CreateSlotIndicators();

        // Rarity glow efekti
        CreateRarityGlow();
    }

    void CreateLevelDisplay()
    {
        // Level text - silah adının altında
        GameObject levelObj = new GameObject("LevelText");
        levelObj.transform.SetParent(weaponPanel.transform, false);

        RectTransform rect = levelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.72f);
        rect.anchorMax = new Vector2(1, 0.85f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-20, 0);

        levelText = levelObj.AddComponent<Text>();
        levelText.text = "+1 | Siradan";
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 14;
        levelText.fontStyle = FontStyle.Italic;
        levelText.color = new Color(0.8f, 0.8f, 0.8f);
        levelText.alignment = TextAnchor.MiddleCenter;
    }

    void CreateRarityGlow()
    {
        // Panel arkasında parıltı efekti
        GameObject glowObj = new GameObject("RarityGlow");
        glowObj.transform.SetParent(weaponPanel.transform, false);
        glowObj.transform.SetAsFirstSibling(); // En arkaya

        RectTransform rect = glowObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-5, -5);
        rect.offsetMax = new Vector2(5, 5);

        rarityGlow = glowObj.AddComponent<Image>();
        rarityGlow.color = new Color(1, 1, 1, 0); // Başta görünmez

        // Basit bir glow texture oluştur
        Texture2D glowTex = CreateGlowTexture(64);
        rarityGlow.sprite = Sprite.Create(glowTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        rarityGlow.type = Image.Type.Sliced;
    }

    Texture2D CreateGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // Yumuşak geçiş
                colors[y * size + x] = new Color(1, 1, 1, alpha * 0.3f);
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    void CreateWeaponNameText()
    {
        GameObject nameObj = new GameObject("WeaponName");
        nameObj.transform.SetParent(weaponPanel.transform, false);

        RectTransform rect = nameObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -8);
        rect.sizeDelta = new Vector2(-20, 28);

        weaponNameText = nameObj.AddComponent<Text>();
        weaponNameText.text = "TABANCA";
        weaponNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weaponNameText.fontSize = 20;
        weaponNameText.fontStyle = FontStyle.Bold;
        weaponNameText.color = activeSlotColor;
        weaponNameText.alignment = TextAnchor.MiddleCenter;

        // Gölge efekti
        Shadow shadow = nameObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(2, -2);
    }

    void CreateAmmoText()
    {
        GameObject ammoObj = new GameObject("AmmoText");
        ammoObj.transform.SetParent(weaponPanel.transform, false);

        RectTransform rect = ammoObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.4f);
        rect.anchorMax = new Vector2(1, 0.75f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-20, 0);

        ammoText = ammoObj.AddComponent<Text>();
        ammoText.text = "12 / 48";
        ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ammoText.fontSize = 32;
        ammoText.fontStyle = FontStyle.Bold;
        ammoText.color = Color.white;
        ammoText.alignment = TextAnchor.MiddleCenter;

        // Outline efekti
        Outline outline = ammoObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1, -1);
    }

    void CreateReloadBar()
    {
        // Reload bar container
        reloadBar = new GameObject("ReloadBar");
        reloadBar.transform.SetParent(weaponPanel.transform, false);

        RectTransform barRect = reloadBar.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.1f, 0.4f);
        barRect.anchorMax = new Vector2(0.9f, 0.5f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = Vector2.zero;

        // Arka plan
        Image barBg = reloadBar.AddComponent<Image>();
        barBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(reloadBar.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        reloadFill = fillObj.AddComponent<Image>();
        reloadFill.color = reloadColor;

        // "RELOADING" text
        GameObject reloadTextObj = new GameObject("ReloadText");
        reloadTextObj.transform.SetParent(reloadBar.transform, false);

        RectTransform textRect = reloadTextObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        Text reloadText = reloadTextObj.AddComponent<Text>();
        reloadText.text = "RELOADING...";
        reloadText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        reloadText.fontSize = 14;
        reloadText.fontStyle = FontStyle.Bold;
        reloadText.color = Color.white;
        reloadText.alignment = TextAnchor.MiddleCenter;

        reloadBar.SetActive(false);
    }

    void CreateSlotIndicators()
    {
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(weaponPanel.transform, false);

        RectTransform containerRect = slotsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 0.35f);
        containerRect.pivot = new Vector2(0.5f, 0);
        containerRect.anchoredPosition = new Vector2(0, 8);
        containerRect.sizeDelta = new Vector2(-20, 0);

        HorizontalLayoutGroup layout = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        slotImages = new Image[3];
        slotBorders = new Image[3];
        slotIcons = new Image[3];
        slotLevelBars = new Image[3];
        string[] slotLabels = { "1", "2", "3" };
        string[] slotNames = { "Primary", "Secondary", "Special" };

        for (int i = 0; i < 3; i++)
        {
            // Slot container
            GameObject slotObj = new GameObject("Slot_" + slotNames[i]);
            slotObj.transform.SetParent(slotsContainer.transform, false);

            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(75, 40);

            // Border
            slotBorders[i] = slotObj.AddComponent<Image>();
            slotBorders[i].color = inactiveSlotColor;

            // İç kısım
            GameObject innerObj = new GameObject("Inner");
            innerObj.transform.SetParent(slotObj.transform, false);

            RectTransform innerRect = innerObj.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(2, 2);
            innerRect.offsetMax = new Vector2(-2, -2);

            slotImages[i] = innerObj.AddComponent<Image>();
            slotImages[i].color = emptySlotColor;

            // Silah ikonu
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.15f);
            iconRect.anchorMax = new Vector2(0.6f, 0.85f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;

            slotIcons[i] = iconObj.AddComponent<Image>();
            slotIcons[i].color = Color.white;
            slotIcons[i].preserveAspect = true;
            slotIcons[i].enabled = false;

            // Numara etiketi
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(slotObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.65f, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = slotLabels[i];
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = new Color(1, 1, 1, 0.7f);
            labelText.alignment = TextAnchor.MiddleCenter;

            // Level bar (slot altında)
            GameObject levelBarObj = new GameObject("LevelBar");
            levelBarObj.transform.SetParent(slotObj.transform, false);

            RectTransform levelBarRect = levelBarObj.AddComponent<RectTransform>();
            levelBarRect.anchorMin = new Vector2(0.05f, 0);
            levelBarRect.anchorMax = new Vector2(0.95f, 0.1f);
            levelBarRect.anchoredPosition = Vector2.zero;
            levelBarRect.sizeDelta = Vector2.zero;

            // Level bar arka plan
            Image levelBarBg = levelBarObj.AddComponent<Image>();
            levelBarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Level bar fill
            GameObject levelFillObj = new GameObject("LevelFill");
            levelFillObj.transform.SetParent(levelBarObj.transform, false);

            RectTransform levelFillRect = levelFillObj.AddComponent<RectTransform>();
            levelFillRect.anchorMin = Vector2.zero;
            levelFillRect.anchorMax = Vector2.one;
            levelFillRect.offsetMin = Vector2.one;
            levelFillRect.offsetMax = -Vector2.one;

            slotLevelBars[i] = levelFillObj.AddComponent<Image>();
            slotLevelBars[i].color = Color.white;
            slotLevelBars[i].type = Image.Type.Filled;
            slotLevelBars[i].fillMethod = Image.FillMethod.Horizontal;
            slotLevelBars[i].fillAmount = 0;
        }
    }

    void SubscribeToEvents()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged += OnWeaponChanged;
            WeaponManager.Instance.OnAmmoChanged += OnAmmoChanged;
            WeaponManager.Instance.OnReloadStarted += OnReloadStarted;
            WeaponManager.Instance.OnReloadFinished += OnReloadFinished;

            // İlk güncelleme
            UpdateUI();
        }
        else
        {
            StartCoroutine(DelayedSubscribe());
        }
    }

    void OnWeaponChanged(WeaponInstance weapon)
    {
        UpdateUI();
    }

    void OnAmmoChanged(int current, int reserve)
    {
        UpdateAmmoDisplay(current, reserve);
    }

    void OnReloadStarted()
    {
        if (ammoText != null) ammoText.gameObject.SetActive(false);
        if (reloadBar != null)
        {
            reloadBar.SetActive(true);
            StartReloadAnimation();
        }
    }

    void OnReloadFinished()
    {
        if (ammoText != null) ammoText.gameObject.SetActive(true);
        if (reloadBar != null) reloadBar.SetActive(false);

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
    }

    void StartReloadAnimation()
    {
        if (reloadCoroutine != null)
            StopCoroutine(reloadCoroutine);

        WeaponInstance weapon = WeaponManager.Instance?.GetCurrentWeapon();
        if (weapon != null)
        {
            reloadCoroutine = StartCoroutine(ReloadAnimation(weapon.GetEffectiveReloadTime()));
        }
    }

    IEnumerator ReloadAnimation(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (reloadFill != null)
            {
                // Fill genişliğini ayarla
                RectTransform fillRect = reloadFill.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(progress, 1);
            }

            yield return null;
        }
    }

    void UpdateUI()
    {
        if (WeaponManager.Instance == null) return;

        WeaponInstance current = WeaponManager.Instance.GetCurrentWeapon();
        int currentSlot = WeaponManager.Instance.currentSlot;

        // Silah adı - Rarity renginde
        if (weaponNameText != null && current != null)
        {
            weaponNameText.text = current.data.weaponName.ToUpper();

            // Rarity'ye göre renk
            Color rarityColor = WeaponRarityHelper.GetRarityColor(current.rarity);
            weaponNameText.color = rarityColor;
        }

        // Level ve rarity bilgisi
        if (levelText != null && current != null)
        {
            string rarityName = WeaponRarityHelper.GetRarityName(current.rarity);
            levelText.text = $"+{current.level} | {rarityName}";
            levelText.color = WeaponRarityHelper.GetRarityColor(current.rarity);
        }

        // Rarity glow efekti
        if (rarityGlow != null && current != null)
        {
            if (current.rarity >= WeaponRarity.Rare)
            {
                Color glowColor = WeaponRarityHelper.GetRarityColor(current.rarity);
                float pulse = (Mathf.Sin(Time.time * 2f) * 0.15f) + 0.25f;
                rarityGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, pulse);
            }
            else
            {
                rarityGlow.color = new Color(1, 1, 1, 0);
            }
        }

        // Mermi
        if (current != null)
        {
            UpdateAmmoDisplay(current.currentAmmo, current.reserveAmmo);
        }

        // Slot göstergeleri
        UpdateSlotIndicators(currentSlot);
    }

    Color GetWeaponColor(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol: return new Color(0.8f, 0.8f, 0.8f);
            case WeaponType.Rifle: return new Color(0.4f, 0.8f, 0.4f);
            case WeaponType.Shotgun: return new Color(0.8f, 0.6f, 0.3f);
            case WeaponType.SMG: return new Color(0.9f, 0.9f, 0.4f);
            case WeaponType.Sniper: return new Color(0.4f, 0.7f, 1f);
            case WeaponType.RocketLauncher: return new Color(1f, 0.4f, 0.2f);
            case WeaponType.Flamethrower: return new Color(1f, 0.6f, 0f);
            case WeaponType.GrenadeLauncher: return new Color(0.4f, 0.8f, 0.4f);
            default: return activeSlotColor;
        }
    }

    void UpdateAmmoDisplay(int current, int reserve)
    {
        if (ammoText == null) return;

        ammoText.text = $"{current} / {reserve}";

        // Düşük mermi uyarısı
        WeaponInstance weapon = WeaponManager.Instance?.GetCurrentWeapon();
        if (weapon != null)
        {
            float ammoRatio = (float)current / weapon.data.maxAmmo;

            if (ammoRatio <= 0.2f)
            {
                ammoText.color = lowAmmoColor;
                // Yanıp sönme efekti
                float blink = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
                ammoText.color = new Color(lowAmmoColor.r, lowAmmoColor.g * blink, lowAmmoColor.b * blink);
            }
            else
            {
                ammoText.color = Color.white;
            }
        }
    }

    void UpdateSlotIndicators(int activeSlot)
    {
        if (WeaponManager.Instance == null) return;

        WeaponInstance[] weapons = {
            WeaponManager.Instance.primaryWeapon,
            WeaponManager.Instance.secondaryWeapon,
            WeaponManager.Instance.specialWeapon
        };

        for (int i = 0; i < 3; i++)
        {
            if (slotBorders[i] == null || slotImages[i] == null) continue;

            bool hasWeapon = weapons[i] != null && weapons[i].isUnlocked;

            if (i == activeSlot && hasWeapon)
            {
                // Aktif slot - RARITY rengini kullan
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapons[i].rarity);
                slotBorders[i].color = rarityColor;
                slotImages[i].color = new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f, 0.8f);

                // İkon göster
                if (slotIcons[i] != null)
                {
                    slotIcons[i].enabled = true;
                    slotIcons[i].sprite = CreateWeaponIcon(weapons[i].data.type);
                    slotIcons[i].color = rarityColor;
                }

                // Level bar güncelle
                UpdateSlotLevelBar(i, weapons[i]);
            }
            else if (hasWeapon)
            {
                // Silah var ama aktif değil - soluk rarity rengi
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapons[i].rarity);
                slotBorders[i].color = new Color(rarityColor.r * 0.5f, rarityColor.g * 0.5f, rarityColor.b * 0.5f);
                slotImages[i].color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                if (slotIcons[i] != null)
                {
                    slotIcons[i].enabled = true;
                    slotIcons[i].sprite = CreateWeaponIcon(weapons[i].data.type);
                    slotIcons[i].color = new Color(rarityColor.r * 0.6f, rarityColor.g * 0.6f, rarityColor.b * 0.6f);
                }

                // Level bar güncelle
                UpdateSlotLevelBar(i, weapons[i]);
            }
            else
            {
                // Boş slot
                slotBorders[i].color = emptySlotColor;
                slotImages[i].color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                if (slotIcons[i] != null)
                {
                    slotIcons[i].enabled = false;
                }

                // Level bar gizle
                UpdateSlotLevelBar(i, null);
            }
        }
    }

    /// <summary>
    /// Slot altındaki level bar'ı güncelle
    /// </summary>
    void UpdateSlotLevelBar(int slotIndex, WeaponInstance weapon)
    {
        if (slotLevelBars == null || slotIndex >= slotLevelBars.Length || slotLevelBars[slotIndex] == null)
            return;

        if (weapon == null)
        {
            slotLevelBars[slotIndex].fillAmount = 0;
            return;
        }

        // Level'e göre fill (1-5 arası, max 5)
        float fillAmount = (float)weapon.level / WeaponInstance.MaxLevel;
        slotLevelBars[slotIndex].fillAmount = fillAmount;
        slotLevelBars[slotIndex].color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
    }

    Sprite CreateWeaponIcon(WeaponType type)
    {
        // Önce indirilen sprite'ları dene, yoksa procedural kullan
        return WeaponSpriteLoader.GetWeaponIcon(type);
    }

    void Update()
    {
        // Düşük mermi yanıp sönme efekti için sürekli güncelleme
        if (WeaponManager.Instance != null && ammoText != null && ammoText.gameObject.activeSelf)
        {
            WeaponInstance weapon = WeaponManager.Instance.GetCurrentWeapon();
            if (weapon != null)
            {
                float ammoRatio = (float)weapon.currentAmmo / weapon.data.maxAmmo;
                if (ammoRatio <= 0.2f)
                {
                    float blink = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
                    ammoText.color = new Color(lowAmmoColor.r, lowAmmoColor.g * blink, lowAmmoColor.b * blink);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged -= OnWeaponChanged;
            WeaponManager.Instance.OnAmmoChanged -= OnAmmoChanged;
            WeaponManager.Instance.OnReloadStarted -= OnReloadStarted;
            WeaponManager.Instance.OnReloadFinished -= OnReloadFinished;
        }
    }
}
