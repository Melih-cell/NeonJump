using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Radyal Silah Carki - Mobil ve PC icin kullanisli silah secimi
/// Basili tutunca acilir, surukleme ile secim yapilir
/// New Input System kullaniyor
/// </summary>
public class WeaponWheel : MonoBehaviour
{
    public static WeaponWheel Instance;

    [Header("Settings")]
    public float wheelRadius = 150f;
    public float centerDeadzone = 40f;
    public float openDelay = 0.15f;
    public float slowMotionScale = 0.3f;
    public bool enableSlowMotion = true;

    [Header("Visual")]
    public Color backgroundColor = new Color(0, 0, 0, 0.85f);
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("References")]
    public Canvas canvas;
    public GameObject wheelPanel;
    public Image backgroundImage;
    public Image centerIcon;
    public Image selectionIndicator;
    public WeaponWheelSlot[] slots;
    public Text weaponInfoText;
    public Text ammoInfoText;
    public Image rarityBorder;

    // State
    private bool isOpen = false;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private int selectedSlot = -1;
    private int previousSelectedSlot = -1;
    private Vector2 inputPosition;
    private float originalTimeScale;
    private bool isTouchInput = false;

    // Touch tracking
    private int activeTouchIndex = -1;
    private Vector2 touchStartPos;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        CreateUI();
        CloseWheel();
    }

    void Update()
    {
        HandleInput();

        if (isOpen)
        {
            UpdateSelection();
            UpdateVisuals();
        }
    }

    void HandleInput()
    {
        // === TOUCH INPUT (New Input System) ===
        if (Touch.activeTouches.Count > 0)
        {
            HandleTouchInput();
            return;
        }

        // === MOUSE/KEYBOARD INPUT ===
        HandleMouseKeyboardInput();
    }

    void HandleTouchInput()
    {
        isTouchInput = true;

        var touches = Touch.activeTouches;

        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];

            // Yeni dokunma
            if (touch.phase == TouchPhase.Began && activeTouchIndex == -1)
            {
                if (IsTouchOnWheelButton(touch.screenPosition))
                {
                    activeTouchIndex = i;
                    touchStartPos = touch.screenPosition;
                    isHolding = true;
                    holdTimer = 0f;
                }
            }

            // Aktif dokunma takibi
            if (i == activeTouchIndex || (activeTouchIndex == -1 && isHolding))
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (isHolding && !isOpen)
                    {
                        holdTimer += Time.unscaledDeltaTime;
                        if (holdTimer >= openDelay)
                        {
                            OpenWheel();
                        }
                    }

                    if (isOpen && wheelPanel != null)
                    {
                        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                        inputPosition = touch.screenPosition - screenCenter;
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (isOpen)
                    {
                        ConfirmSelection();
                    }
                    isHolding = false;
                    activeTouchIndex = -1;
                    CloseWheel();
                }
            }
        }

        // Dokunma bitti mi kontrol
        if (touches.Count == 0 && isHolding)
        {
            if (isOpen)
            {
                ConfirmSelection();
            }
            isHolding = false;
            activeTouchIndex = -1;
            CloseWheel();
        }
    }

    void HandleMouseKeyboardInput()
    {
        isTouchInput = false;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null && mouse == null) return;

        // Tab tusu veya sag tik ile ac
        bool shouldOpen = (keyboard != null && keyboard.tabKey.isPressed) ||
                         (mouse != null && mouse.rightButton.isPressed);

        if (shouldOpen && !isOpen && !isHolding)
        {
            isHolding = true;
            holdTimer = 0f;
        }

        if (isHolding && !isOpen)
        {
            holdTimer += Time.unscaledDeltaTime;
            if (holdTimer >= openDelay)
            {
                OpenWheel();
            }
        }

        if (!shouldOpen && isHolding)
        {
            if (isOpen)
            {
                ConfirmSelection();
            }
            isHolding = false;
            CloseWheel();
        }

        if (isOpen && mouse != null)
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            inputPosition = mouse.position.ReadValue() - screenCenter;
        }
    }

    bool IsTouchOnWheelButton(Vector2 touchPos)
    {
        float buttonSize = Screen.width * 0.15f;
        Rect buttonRect = new Rect(
            Screen.width - buttonSize - 20,
            20,
            buttonSize,
            buttonSize
        );
        return buttonRect.Contains(touchPos);
    }

    void OpenWheel()
    {
        if (isOpen) return;

        isOpen = true;
        if (wheelPanel != null)
            wheelPanel.SetActive(true);

        if (enableSlowMotion)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = slowMotionScale;
        }

        if (WeaponManager.Instance != null)
        {
            selectedSlot = WeaponManager.Instance.currentSlot;
            previousSelectedSlot = selectedSlot;
        }

        UpdateSlots();
        StartCoroutine(OpenAnimation());
    }

    void CloseWheel()
    {
        if (!isOpen && wheelPanel != null && !wheelPanel.activeSelf) return;

        isOpen = false;

        if (enableSlowMotion)
        {
            Time.timeScale = 1f;
        }

        if (wheelPanel != null)
        {
            wheelPanel.SetActive(false);
        }

        selectedSlot = -1;
    }

    void ConfirmSelection()
    {
        if (selectedSlot >= 0 && selectedSlot < 3)
        {
            WeaponInstance weapon = GetWeaponAtSlot(selectedSlot);
            if (weapon != null && weapon.isUnlocked)
            {
                if (WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.SwitchToSlot(selectedSlot);
                }
            }
        }
    }

    void UpdateSelection()
    {
        float distance = inputPosition.magnitude;

        if (distance < centerDeadzone)
        {
            selectedSlot = previousSelectedSlot;
            return;
        }

        float angle = Mathf.Atan2(inputPosition.x, inputPosition.y) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if (angle >= 300 || angle < 60)
            selectedSlot = 0;
        else if (angle >= 60 && angle < 180)
            selectedSlot = 1;
        else
            selectedSlot = 2;
    }

    WeaponInstance GetWeaponAtSlot(int slot)
    {
        if (WeaponManager.Instance == null) return null;

        switch (slot)
        {
            case 0: return WeaponManager.Instance.primaryWeapon;
            case 1: return WeaponManager.Instance.secondaryWeapon;
            case 2: return WeaponManager.Instance.specialWeapon;
            default: return null;
        }
    }

    void UpdateSlots()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length && i < 3; i++)
        {
            WeaponInstance weapon = GetWeaponAtSlot(i);
            if (slots[i] != null)
                slots[i].UpdateSlot(weapon, i == selectedSlot);
        }
    }

    void UpdateVisuals()
    {
        UpdateSlots();

        if (selectionIndicator != null && selectedSlot >= 0)
        {
            float angle = GetSlotAngle(selectedSlot);
            selectionIndicator.transform.rotation = Quaternion.Euler(0, 0, -angle);

            WeaponInstance weapon = GetWeaponAtSlot(selectedSlot);
            if (weapon != null && weapon.isUnlocked)
            {
                selectionIndicator.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
            }
            else
            {
                selectionIndicator.color = emptySlotColor;
            }
        }

        UpdateWeaponInfo();
    }

    float GetSlotAngle(int slot)
    {
        switch (slot)
        {
            case 0: return 0f;
            case 1: return 120f;
            case 2: return 240f;
            default: return 0f;
        }
    }

    void UpdateWeaponInfo()
    {
        if (selectedSlot < 0) return;

        WeaponInstance weapon = GetWeaponAtSlot(selectedSlot);

        if (weapon != null && weapon.isUnlocked)
        {
            if (weaponInfoText != null)
            {
                string rarityName = WeaponRarityHelper.GetRarityName(weapon.rarity);
                weaponInfoText.text = $"{weapon.data.weaponName}\n<size=14>{rarityName} +{weapon.level}</size>";
                weaponInfoText.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
            }

            if (ammoInfoText != null)
            {
                ammoInfoText.text = $"{weapon.currentAmmo}/{weapon.GetEffectiveMaxAmmo()}\nHasar: {weapon.GetEffectiveDamage()}";
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
            }
        }
        else
        {
            if (weaponInfoText != null)
            {
                string[] slotNames = { "ANA SILAH", "IKINCIL", "OZEL" };
                weaponInfoText.text = $"{slotNames[selectedSlot]}\n<size=14>Bos Slot</size>";
                weaponInfoText.color = emptySlotColor;
            }

            if (ammoInfoText != null)
            {
                ammoInfoText.text = "-";
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = emptySlotColor;
            }
        }
    }

    IEnumerator OpenAnimation()
    {
        if (wheelPanel == null) yield break;

        float duration = 0.15f;
        float elapsed = 0f;

        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            wheelPanel.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                backgroundImage.color = new Color(c.r, c.g, c.b, t * 0.85f);
            }

            yield return null;
        }

        wheelPanel.transform.localScale = endScale;
    }

    void CreateUI()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("WeaponWheelCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        wheelPanel = new GameObject("WheelPanel");
        wheelPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = wheelPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(wheelRadius * 3f, wheelRadius * 3f);

        CreateBackground();
        CreateSlots();
        CreateCenter();
        CreateSelectionIndicator();
        CreateInfoPanel();
        CreateMobileButton();
    }

    void CreateBackground()
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(wheelPanel.transform, false);

        RectTransform rect = bgObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;

        Texture2D circleTex = CreateCircleTexture(128);
        backgroundImage.sprite = Sprite.Create(circleTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    void CreateSlots()
    {
        slots = new WeaponWheelSlot[3];
        float[] angles = { 90f, -30f, 210f };

        for (int i = 0; i < 3; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(wheelPanel.transform, false);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 80);

            float rad = angles[i] * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * wheelRadius;
            float y = Mathf.Sin(rad) * wheelRadius;
            rect.anchoredPosition = new Vector2(x, y);

            slots[i] = slotObj.AddComponent<WeaponWheelSlot>();
            slots[i].CreateVisuals();
        }
    }

    void CreateCenter()
    {
        GameObject centerObj = new GameObject("Center");
        centerObj.transform.SetParent(wheelPanel.transform, false);

        RectTransform rect = centerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(centerDeadzone * 2, centerDeadzone * 2);

        centerIcon = centerObj.AddComponent<Image>();
        centerIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        Texture2D tex = CreateCircleTexture(64);
        centerIcon.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    void CreateSelectionIndicator()
    {
        GameObject indicatorObj = new GameObject("SelectionIndicator");
        indicatorObj.transform.SetParent(wheelPanel.transform, false);

        RectTransform rect = indicatorObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(30, 50);
        rect.anchoredPosition = new Vector2(0, wheelRadius + 30);

        selectionIndicator = indicatorObj.AddComponent<Image>();
        selectionIndicator.color = selectedColor;

        Texture2D tex = CreateArrowTexture(64);
        selectionIndicator.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    void CreateInfoPanel()
    {
        GameObject infoObj = new GameObject("InfoPanel");
        infoObj.transform.SetParent(wheelPanel.transform, false);

        RectTransform rect = infoObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, 80);
        rect.anchoredPosition = new Vector2(0, -wheelRadius - 60);

        Image bg = infoObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);
        rarityBorder = bg;

        // Weapon name
        GameObject nameObj = new GameObject("WeaponName");
        nameObj.transform.SetParent(infoObj.transform, false);

        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-10, -5);

        weaponInfoText = nameObj.AddComponent<Text>();
        weaponInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weaponInfoText.fontSize = 18;
        weaponInfoText.fontStyle = FontStyle.Bold;
        weaponInfoText.color = Color.white;
        weaponInfoText.alignment = TextAnchor.MiddleCenter;
        weaponInfoText.supportRichText = true;

        // Ammo info
        GameObject ammoObj = new GameObject("AmmoInfo");
        ammoObj.transform.SetParent(infoObj.transform, false);

        RectTransform ammoRect = ammoObj.AddComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(0, 0);
        ammoRect.anchorMax = new Vector2(1, 0.5f);
        ammoRect.offsetMin = new Vector2(10, 5);
        ammoRect.offsetMax = new Vector2(-10, 0);

        ammoInfoText = ammoObj.AddComponent<Text>();
        ammoInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ammoInfoText.fontSize = 14;
        ammoInfoText.color = new Color(0.8f, 0.8f, 0.8f);
        ammoInfoText.alignment = TextAnchor.MiddleCenter;
    }

    void CreateMobileButton()
    {
        GameObject btnObj = new GameObject("WeaponWheelButton");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 150);
        rect.sizeDelta = new Vector2(70, 70);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        Texture2D tex = CreateCircleTexture(64);
        btnImg.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.2f);
        iconRect.anchorMax = new Vector2(0.8f, 0.8f);
        iconRect.sizeDelta = Vector2.zero;

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = WeaponSpriteLoader.GetWeaponIcon(WeaponType.Pistol);
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
    }

    Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius)
                {
                    float edge = Mathf.Clamp01((radius - dist) / 2f);
                    colors[y * size + x] = new Color(1, 1, 1, edge);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    Texture2D CreateArrowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.clear;

        int centerX = size / 2;
        for (int y = 0; y < size; y++)
        {
            float progress = (float)y / size;
            int width = (int)(progress * size * 0.4f);

            for (int x = centerX - width; x <= centerX + width; x++)
            {
                if (x >= 0 && x < size)
                    colors[y * size + x] = Color.white;
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}

/// <summary>
/// Silah carki slot'u
/// </summary>
public class WeaponWheelSlot : MonoBehaviour
{
    public Image background;
    public Image border;
    public Image weaponIcon;
    public Text levelText;
    public Text ammoText;

    public void CreateVisuals()
    {
        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        background = bgObj.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        Texture2D circleTex = CreateCircle(64);
        background.sprite = Sprite.Create(circleTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(transform, false);

        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(6, 6);

        border = borderObj.AddComponent<Image>();
        border.color = new Color(0.5f, 0.5f, 0.5f);
        border.sprite = background.sprite;

        // Weapon icon
        GameObject iconObj = new GameObject("WeaponIcon");
        iconObj.transform.SetParent(transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.15f);
        iconRect.anchorMax = new Vector2(0.85f, 0.85f);
        iconRect.sizeDelta = Vector2.zero;

        weaponIcon = iconObj.AddComponent<Image>();
        weaponIcon.preserveAspect = true;
        weaponIcon.color = Color.white;

        // Level text
        GameObject levelObj = new GameObject("Level");
        levelObj.transform.SetParent(transform, false);

        RectTransform levelRect = levelObj.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0, 0);
        levelRect.anchorMax = new Vector2(1, 0.3f);
        levelRect.sizeDelta = Vector2.zero;

        levelText = levelObj.AddComponent<Text>();
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 12;
        levelText.fontStyle = FontStyle.Bold;
        levelText.color = Color.white;
        levelText.alignment = TextAnchor.MiddleCenter;

        // Ammo text
        GameObject ammoObj = new GameObject("Ammo");
        ammoObj.transform.SetParent(transform, false);

        RectTransform ammoRect = ammoObj.AddComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(0, 0.7f);
        ammoRect.anchorMax = new Vector2(1, 1);
        ammoRect.sizeDelta = Vector2.zero;

        ammoText = ammoObj.AddComponent<Text>();
        ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ammoText.fontSize = 11;
        ammoText.color = new Color(0.9f, 0.9f, 0.9f);
        ammoText.alignment = TextAnchor.MiddleCenter;
    }

    public void UpdateSlot(WeaponInstance weapon, bool selected)
    {
        if (weapon == null || !weapon.isUnlocked)
        {
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            border.color = selected ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.2f, 0.2f, 0.2f);
            weaponIcon.enabled = false;
            levelText.text = "";
            ammoText.text = "";
            transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one * 0.9f;
            return;
        }

        Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);

        background.color = selected ?
            new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f, 0.95f) :
            new Color(0.15f, 0.15f, 0.2f, 0.9f);

        border.color = selected ? rarityColor : new Color(rarityColor.r * 0.5f, rarityColor.g * 0.5f, rarityColor.b * 0.5f);

        weaponIcon.enabled = true;
        weaponIcon.sprite = WeaponSpriteLoader.GetWeaponIcon(weapon.data.type);
        weaponIcon.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f);

        levelText.text = $"+{weapon.level}";
        levelText.color = rarityColor;

        ammoText.text = $"{weapon.currentAmmo}";
        ammoText.color = weapon.currentAmmo <= 0 ? Color.red : Color.white;

        float targetScale = selected ? 1.2f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 10f);
    }

    Texture2D CreateCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius)
                {
                    float edge = Mathf.Clamp01((radius - dist) / 2f);
                    colors[y * size + x] = new Color(1, 1, 1, edge);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
