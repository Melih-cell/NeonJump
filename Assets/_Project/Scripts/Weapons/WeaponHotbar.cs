using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Basit ve kullanisli silah hotbar sistemi
/// Tum silahlara 1-8 tuslari ile hizli erisim
/// </summary>
public class WeaponHotbar : MonoBehaviour
{
    public static WeaponHotbar Instance;

    [Header("Settings")]
    public int maxSlots = 3;  // 3 slot: Pistol, Primary, Special
    public float slotSize = 80f;
    public float slotSpacing = 12f;
    public float bottomMargin = 25f;

    [Header("Colors - Cyberpunk Theme")]
    public Color activeSlotColor = new Color(0f, 1f, 1f, 1f);
    public Color inactiveSlotColor = new Color(0.12f, 0.08f, 0.2f, 0.9f);
    public Color emptySlotColor = new Color(0.06f, 0.03f, 0.12f, 0.7f);
    public Color lowAmmoColor = new Color(1f, 0.3f, 0.3f, 1f);
    public Color neonCyanColor = new Color(0f, 1f, 1f, 0.6f);

    // UI References
    private Canvas canvas;
    private GameObject hotbarPanel;
    private List<HotbarSlot> slots = new List<HotbarSlot>();

    // Weapon data - tum silahlar tek listede
    private List<WeaponInstance> allWeapons = new List<WeaponInstance>();
    private int currentSlotIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Eski UI sistemlerini devre disi birak
        DisableOldUISystems();

        CreateUI();

        StartCoroutine(DelayedInit());
    }

    System.Collections.IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.1f);

        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged += OnWeaponChanged;
            WeaponManager.Instance.OnAmmoChanged += OnAmmoChanged;
        }

        RefreshWeaponList();
        UpdateUI();
    }

    void DisableOldUISystems()
    {
        // QuickWeaponSwitch kapat
        if (QuickWeaponSwitch.Instance != null)
        {
            QuickWeaponSwitch.Instance.gameObject.SetActive(false);
        }

        // WeaponWheel kapat
        if (WeaponWheel.Instance != null)
        {
            WeaponWheel.Instance.gameObject.SetActive(false);
        }

        // WeaponUI kapat
        if (WeaponUI.Instance != null)
        {
            WeaponUI.Instance.gameObject.SetActive(false);
        }
    }

    void CreateUI()
    {
        // Canvas bul veya olustur
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HotbarCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ana panel - ekranin altinda ortalanmis
        hotbarPanel = new GameObject("WeaponHotbar");
        hotbarPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = hotbarPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, bottomMargin);

        float totalWidth = (slotSize * maxSlots) + (slotSpacing * (maxSlots - 1)) + 20;
        panelRect.sizeDelta = new Vector2(totalWidth, slotSize + 30);

        // Panel arka plan - koyu mor-siyah cyberpunk
        Image panelBg = hotbarPanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.02f, 0.1f, 0.85f);

        // Neon cyan sinir cercevesi
        GameObject frameObj = new GameObject("NeonFrame");
        frameObj.transform.SetParent(hotbarPanel.transform, false);
        RectTransform frameRect = frameObj.AddComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = new Vector2(-2, -2);
        frameRect.offsetMax = new Vector2(2, 2);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = neonCyanColor;
        frameImg.raycastTarget = false;
        // Frame'i arkaya gonder (panel BG'nin arkasinda olmamali)
        frameObj.transform.SetAsFirstSibling();

        // Panel ust kenarinda ince neon cizgi
        GameObject topLineObj = new GameObject("TopNeonLine");
        topLineObj.transform.SetParent(hotbarPanel.transform, false);
        RectTransform topLineRect = topLineObj.AddComponent<RectTransform>();
        topLineRect.anchorMin = new Vector2(0, 1);
        topLineRect.anchorMax = new Vector2(1, 1);
        topLineRect.pivot = new Vector2(0.5f, 1);
        topLineRect.anchoredPosition = Vector2.zero;
        topLineRect.sizeDelta = new Vector2(0, 1);
        Image topLineImg = topLineObj.AddComponent<Image>();
        topLineImg.color = new Color(0f, 1f, 1f, 0.8f);
        topLineImg.raycastTarget = false;

        // Slotlari olustur
        CreateSlots();
    }

    void CreateSlots()
    {
        float startX = -((slotSize * maxSlots) + (slotSpacing * (maxSlots - 1))) / 2f + slotSize / 2f;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i + 1}");
            slotObj.transform.SetParent(hotbarPanel.transform, false);

            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            slotRect.anchoredPosition = new Vector2(startX + i * (slotSize + slotSpacing), 5);

            HotbarSlot slot = CreateSlotVisuals(slotObj, i);
            slots.Add(slot);

            // Slotlar arasi dikey neon ayirici cizgi (son slot haric)
            if (i < maxSlots - 1)
            {
                GameObject sepObj = new GameObject($"Separator_{i}");
                sepObj.transform.SetParent(hotbarPanel.transform, false);
                RectTransform sepRect = sepObj.AddComponent<RectTransform>();
                sepRect.anchorMin = new Vector2(0.5f, 0.5f);
                sepRect.anchorMax = new Vector2(0.5f, 0.5f);
                float sepX = startX + i * (slotSize + slotSpacing) + slotSize / 2f + slotSpacing / 2f;
                sepRect.anchoredPosition = new Vector2(sepX, 5);
                sepRect.sizeDelta = new Vector2(1, slotSize * 0.7f);
                Image sepImg = sepObj.AddComponent<Image>();
                sepImg.color = new Color(0f, 1f, 1f, 0.4f);
                sepImg.raycastTarget = false;
            }
        }
    }

    HotbarSlot CreateSlotVisuals(GameObject slotObj, int index)
    {
        HotbarSlot slot = new HotbarSlot();
        slot.index = index;

        // Slot kategorileri
        string[] slotLabels = { "PISTOL", "PRIMARY", "SPECIAL" };
        string slotLabel = index < slotLabels.Length ? slotLabels[index] : "";

        // Border/Frame - ince neon cerceve
        Image border = slotObj.AddComponent<Image>();
        border.color = inactiveSlotColor;
        slot.border = border;

        // Background - koyu ic alan
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(slotObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        slot.background = bgObj.AddComponent<Image>();
        slot.background.color = emptySlotColor;

        // Rarity glow overlay (hafif rarity renkli arka plan - ikon arkasinda)
        GameObject glowObj = new GameObject("RarityGlow");
        glowObj.transform.SetParent(slotObj.transform, false);
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(2, 2);
        glowRect.offsetMax = new Vector2(-2, -2);
        slot.rarityGlow = glowObj.AddComponent<Image>();
        slot.rarityGlow.color = Color.clear;
        slot.rarityGlow.raycastTarget = false;

        // Weapon Icon - biraz daha buyuk alan
        GameObject iconObj = new GameObject("WeaponIcon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.2f);
        iconRect.anchorMax = new Vector2(0.9f, 0.82f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        slot.weaponIcon = iconObj.AddComponent<Image>();
        slot.weaponIcon.preserveAspect = true;
        slot.weaponIcon.enabled = false;

        // Slot Number (sol ust kose - kucuk badge)
        GameObject numObj = new GameObject("SlotNumber");
        numObj.transform.SetParent(slotObj.transform, false);
        RectTransform numRect = numObj.AddComponent<RectTransform>();
        numRect.anchorMin = new Vector2(0, 1);
        numRect.anchorMax = new Vector2(0, 1);
        numRect.pivot = new Vector2(0, 1);
        numRect.anchoredPosition = new Vector2(4, -2);
        numRect.sizeDelta = new Vector2(16, 16);
        slot.slotNumberText = numObj.AddComponent<Text>();
        slot.slotNumberText.text = (index + 1).ToString();
        slot.slotNumberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        slot.slotNumberText.fontSize = 12;
        slot.slotNumberText.fontStyle = FontStyle.Bold;
        slot.slotNumberText.color = new Color(0.5f, 0.4f, 0.7f, 0.7f);
        slot.slotNumberText.alignment = TextAnchor.UpperLeft;

        // Slot Label (alt orta - kategori bilgisi)
        GameObject labelObj = new GameObject("SlotLabel");
        labelObj.transform.SetParent(slotObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0);
        labelRect.pivot = new Vector2(0.5f, 0);
        labelRect.anchoredPosition = new Vector2(0, 3);
        labelRect.sizeDelta = new Vector2(slotSize, 12);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = slotLabel;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 8;
        labelText.color = new Color(0.4f, 0.35f, 0.6f, 0.6f);
        labelText.alignment = TextAnchor.MiddleCenter;

        // Ammo Text (sag alt kose)
        GameObject ammoObj = new GameObject("AmmoText");
        ammoObj.transform.SetParent(slotObj.transform, false);
        RectTransform ammoRect = ammoObj.AddComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(0.5f, 0);
        ammoRect.anchorMax = new Vector2(1, 0);
        ammoRect.pivot = new Vector2(1, 0);
        ammoRect.anchoredPosition = new Vector2(-4, 14);
        ammoRect.sizeDelta = new Vector2(40, 14);
        slot.ammoText = ammoObj.AddComponent<Text>();
        slot.ammoText.text = "";
        slot.ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        slot.ammoText.fontSize = 11;
        slot.ammoText.fontStyle = FontStyle.Bold;
        slot.ammoText.color = Color.white;
        slot.ammoText.alignment = TextAnchor.LowerRight;

        // Weapon Name (ust - aktif oldugunda gosterilir)
        GameObject nameObj = new GameObject("WeaponName");
        nameObj.transform.SetParent(slotObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1);
        nameRect.anchorMax = new Vector2(0.5f, 1);
        nameRect.pivot = new Vector2(0.5f, 0);
        nameRect.anchoredPosition = new Vector2(0, 4);
        nameRect.sizeDelta = new Vector2(110, 18);
        slot.weaponNameText = nameObj.AddComponent<Text>();
        slot.weaponNameText.text = "";
        slot.weaponNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        slot.weaponNameText.fontSize = 11;
        slot.weaponNameText.fontStyle = FontStyle.Bold;
        slot.weaponNameText.color = new Color(0f, 1f, 1f, 1f);
        slot.weaponNameText.alignment = TextAnchor.MiddleCenter;
        slot.weaponNameText.enabled = false;

        // Rarity indicator (ince cizgi ustte)
        GameObject rarityObj = new GameObject("RarityBar");
        rarityObj.transform.SetParent(slotObj.transform, false);
        RectTransform rarityRect = rarityObj.AddComponent<RectTransform>();
        rarityRect.anchorMin = new Vector2(0.05f, 0.95f);
        rarityRect.anchorMax = new Vector2(0.95f, 0.98f);
        rarityRect.offsetMin = Vector2.zero;
        rarityRect.offsetMax = Vector2.zero;
        slot.rarityBar = rarityObj.AddComponent<Image>();
        slot.rarityBar.color = Color.clear;

        // Mermi cubugu (sol kenarda dikey bar)
        GameObject ammoBarObj = new GameObject("AmmoBar");
        ammoBarObj.transform.SetParent(slotObj.transform, false);
        RectTransform ammoBarRect = ammoBarObj.AddComponent<RectTransform>();
        ammoBarRect.anchorMin = new Vector2(0, 0.05f);
        ammoBarRect.anchorMax = new Vector2(0, 0.9f);
        ammoBarRect.pivot = new Vector2(0, 0);
        ammoBarRect.anchoredPosition = new Vector2(3, 0);
        ammoBarRect.sizeDelta = new Vector2(3, 0);
        slot.ammoBar = ammoBarObj.AddComponent<Image>();
        slot.ammoBar.type = Image.Type.Filled;
        slot.ammoBar.fillMethod = Image.FillMethod.Vertical;
        slot.ammoBar.fillAmount = 1f;
        slot.ammoBar.color = Color.clear;

        return slot;
    }

    void Update()
    {
        HandleInput();
        UpdateActiveSlotAnimation();
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            // 1-8 tuslari ile slot secimi
            if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) SelectSlot(3);
            else if (keyboard.digit5Key.wasPressedThisFrame) SelectSlot(4);
            else if (keyboard.digit6Key.wasPressedThisFrame) SelectSlot(5);
            else if (keyboard.digit7Key.wasPressedThisFrame) SelectSlot(6);
            else if (keyboard.digit8Key.wasPressedThisFrame) SelectSlot(7);

            // Q/E ile onceki/sonraki silah
            if (keyboard.qKey.wasPressedThisFrame) SelectPreviousWeapon();
            else if (keyboard.eKey.wasPressedThisFrame) SelectNextWeapon();
        }

        // Mouse scroll
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0) SelectPreviousWeapon();
            else if (scroll < 0) SelectNextWeapon();
        }

        // Mobil silah degistirme butonu
        if (MobileControls.Instance != null && MobileControls.Instance.SwitchWeaponPressed)
        {
            SelectNextWeapon();
        }
    }

    void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= allWeapons.Count) return;

        WeaponInstance weapon = allWeapons[slotIndex];
        if (weapon == null || !weapon.isUnlocked) return;

        currentSlotIndex = slotIndex;

        // WeaponManager'a bildir
        if (WeaponManager.Instance != null)
        {
            int managerSlot = HotbarToManagerSlot(slotIndex);
            if (managerSlot >= 0)
            {
                WeaponManager.Instance.SwitchToSlot(managerSlot);
            }
        }

        UpdateUI();
        PlaySwitchSound();

        // Silah degistirme animasyonu
        if (slotIndex < slots.Count)
        {
            StartCoroutine(WeaponSwitchAnimation(slots[slotIndex], weapon));
        }
    }

    /// <summary>
    /// Silah degistirme animasyonu: Scale punch + border renk flash
    /// </summary>
    System.Collections.IEnumerator WeaponSwitchAnimation(HotbarSlot slot, WeaponInstance weapon)
    {
        if (slot.border == null) yield break;

        Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);

        // Sadece renk flash (beyaz -> rarity), boyut degisimi yok
        float duration = 0.2f;
        float elapsed = 0f;

        slot.border.color = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            slot.border.color = Color.Lerp(Color.white, rarityColor, t);
            yield return null;
        }

        slot.border.color = rarityColor;
    }

    /// <summary>
    /// Hotbar slot indexini WeaponManager slot indexine cevirir
    /// Hotbar: 0=Pistol, 1=Primary, 2=Special
    /// Manager: 0=Primary, 1=Secondary(Pistol), 2=Special
    /// </summary>
    int HotbarToManagerSlot(int hotbarSlot)
    {
        switch (hotbarSlot)
        {
            case 0: return 1;  // Pistol -> Secondary
            case 1: return 0;  // Primary -> Primary
            case 2: return 2;  // Special -> Special
            default: return -1;
        }
    }

    /// <summary>
    /// WeaponManager slot indexini Hotbar slot indexine cevirir
    /// </summary>
    int ManagerToHotbarSlot(int managerSlot)
    {
        switch (managerSlot)
        {
            case 0: return 1;  // Primary -> slot 2
            case 1: return 0;  // Secondary(Pistol) -> slot 1
            case 2: return 2;  // Special -> slot 3
            default: return -1;
        }
    }

    void SelectNextWeapon()
    {
        if (allWeapons.Count == 0) return;

        int startIndex = currentSlotIndex;
        do
        {
            currentSlotIndex = (currentSlotIndex + 1) % allWeapons.Count;
            if (allWeapons[currentSlotIndex] != null && allWeapons[currentSlotIndex].isUnlocked)
            {
                SelectSlot(currentSlotIndex);
                return;
            }
        } while (currentSlotIndex != startIndex);
    }

    void SelectPreviousWeapon()
    {
        if (allWeapons.Count == 0) return;

        int startIndex = currentSlotIndex;
        do
        {
            currentSlotIndex = (currentSlotIndex - 1 + allWeapons.Count) % allWeapons.Count;
            if (allWeapons[currentSlotIndex] != null && allWeapons[currentSlotIndex].isUnlocked)
            {
                SelectSlot(currentSlotIndex);
                return;
            }
        } while (currentSlotIndex != startIndex);
    }

    void PlaySwitchSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJump(); // Gecici ses
        }
    }

    void RefreshWeaponList()
    {
        allWeapons.Clear();

        if (WeaponManager.Instance == null) return;

        // Tum silahlari listeye ekle
        // Sira: 1=Pistol, 2=Primary, 3=Special (bos slotlar null)

        // Slot 1: Pistol (Secondary - her zaman var)
        allWeapons.Add(WeaponManager.Instance.secondaryWeapon);

        // Slot 2: Primary (Rifle, Shotgun, SMG, Sniper)
        allWeapons.Add(WeaponManager.Instance.primaryWeapon);

        // Slot 3: Special (Rocket, Flamethrower, Grenade)
        allWeapons.Add(WeaponManager.Instance.specialWeapon);

        // Aktif silahi bul ve currentSlotIndex'i ayarla
        WeaponInstance current = WeaponManager.Instance.GetCurrentWeapon();
        if (current != null)
        {
            for (int i = 0; i < allWeapons.Count; i++)
            {
                if (allWeapons[i] == current)
                {
                    currentSlotIndex = i;
                    break;
                }
            }
        }
        else
        {
            // Ilk kullanilabilir silahi sec
            for (int i = 0; i < allWeapons.Count; i++)
            {
                if (allWeapons[i] != null && allWeapons[i].isUnlocked)
                {
                    currentSlotIndex = i;
                    break;
                }
            }
        }
    }

    void OnWeaponChanged(WeaponInstance weapon)
    {
        RefreshWeaponList();
        UpdateUI();
    }

    void OnAmmoChanged(int current, int reserve)
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            HotbarSlot slot = slots[i];
            WeaponInstance weapon = (i < allWeapons.Count) ? allWeapons[i] : null;
            bool isActive = (i == currentSlotIndex);
            bool hasWeapon = weapon != null && weapon.isUnlocked;

            // Border ve arkaplan rengi
            if (isActive && hasWeapon)
            {
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                slot.border.color = rarityColor;
                slot.background.color = new Color(rarityColor.r * 0.12f, rarityColor.g * 0.12f, rarityColor.b * 0.12f, 0.95f);
            }
            else if (hasWeapon)
            {
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                slot.border.color = new Color(rarityColor.r * 0.35f, rarityColor.g * 0.35f, rarityColor.b * 0.35f, 0.6f);
                slot.background.color = new Color(0.05f, 0.03f, 0.1f, 0.9f);
            }
            else
            {
                slot.border.color = inactiveSlotColor;
                slot.background.color = emptySlotColor;
            }

            // Silah ikonu
            if (hasWeapon)
            {
                slot.weaponIcon.enabled = true;
                slot.weaponIcon.sprite = WeaponSpriteLoader.GetWeaponIcon(weapon.data.type);
                slot.weaponIcon.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);

                // Ammo text
                slot.ammoText.text = $"{weapon.currentAmmo}";
                float ammoPercent = (weapon.data.maxAmmo > 0) ? (float)weapon.currentAmmo / weapon.data.maxAmmo : 1f;
                if (ammoPercent <= 0.2f)
                    slot.ammoText.color = lowAmmoColor;
                else
                    slot.ammoText.color = Color.white;

                // Ammo bar (sol kenarda dikey cubuk)
                if (slot.ammoBar != null)
                {
                    slot.ammoBar.fillAmount = ammoPercent;
                    if (ammoPercent >= 0.6f)
                        slot.ammoBar.color = new Color(0f, 1f, 0.6f, 0.8f);
                    else if (ammoPercent >= 0.2f)
                        slot.ammoBar.color = new Color(1f, 0.7f, 0f, 0.8f);
                    else
                        slot.ammoBar.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                }

                // Rarity bar (ust cizgi)
                Color rarityColor2 = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                slot.rarityBar.color = isActive ? rarityColor2 : new Color(rarityColor2.r, rarityColor2.g, rarityColor2.b, 0.5f);

                // Rarity glow overlay - sadece aktifken belirgin
                if (slot.rarityGlow != null)
                {
                    float glowAlpha = isActive ? 0.12f : 0.05f;
                    slot.rarityGlow.color = new Color(rarityColor2.r, rarityColor2.g, rarityColor2.b, glowAlpha);
                }

                // Weapon name (sadece aktifse)
                slot.weaponNameText.enabled = isActive;
                if (isActive)
                {
                    slot.weaponNameText.text = weapon.data.weaponName;
                    slot.weaponNameText.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                }
            }
            else
            {
                slot.weaponIcon.enabled = false;
                slot.ammoText.text = "";
                slot.rarityBar.color = Color.clear;
                slot.weaponNameText.enabled = false;
                if (slot.ammoBar != null) slot.ammoBar.color = Color.clear;
                if (slot.rarityGlow != null) slot.rarityGlow.color = Color.clear;
            }

            // Slot numarasi rengi (aktifken neon cyan)
            slot.slotNumberText.color = isActive ? new Color(0f, 1f, 1f, 1f) : new Color(0.6f, 0.5f, 0.8f, 0.6f);
        }
    }

    void UpdateActiveSlotAnimation()
    {
        // Aktif slot icin sadece renk pulse (scale yok)
        if (currentSlotIndex >= 0 && currentSlotIndex < slots.Count)
        {
            HotbarSlot slot = slots[currentSlotIndex];

            // Border renk pulse (cyan bazli, boyut degisimi yok)
            WeaponInstance weapon = (currentSlotIndex < allWeapons.Count) ? allWeapons[currentSlotIndex] : null;
            if (weapon != null && weapon.isUnlocked)
            {
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                float colorPulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                slot.border.color = Color.Lerp(rarityColor, neonCyanColor, colorPulse * 0.4f);
            }
            else
            {
                float colorPulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                slot.border.color = Color.Lerp(activeSlotColor, new Color(0f, 1f, 1f, 0.8f), colorPulse * 0.3f);
            }
        }
    }

    void OnDestroy()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged -= OnWeaponChanged;
            WeaponManager.Instance.OnAmmoChanged -= OnAmmoChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Disaridan silah eklendiginde cagirilir
    /// </summary>
    public void OnWeaponAdded()
    {
        RefreshWeaponList();
        UpdateUI();
    }
}

/// <summary>
/// Hotbar slot verisi
/// </summary>
[System.Serializable]
public class HotbarSlot
{
    public int index;
    public Image border;
    public Image background;
    public Image weaponIcon;
    public Image rarityBar;
    public Image rarityGlow;
    public Image ammoBar;
    public Text slotNumberText;
    public Text ammoText;
    public Text weaponNameText;
}
