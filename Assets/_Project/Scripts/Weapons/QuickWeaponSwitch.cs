using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Mobil icin hizli silah degistirme sistemi
/// - Tek dokunma: Sonraki silaha gec
/// - Cift dokunma: Son kullanilan silaha don
/// - Slot'lara dokunma: Direkt o silaha gec
/// New Input System kullaniyor
/// </summary>
public class QuickWeaponSwitch : MonoBehaviour
{
    public static QuickWeaponSwitch Instance;

    [Header("Settings")]
    public float doubleTapTime = 0.3f;
    public float holdTimeForWheel = 0.4f;

    [Header("Quick Switch Button")]
    public Button quickSwitchButton;
    public Image buttonIcon;
    public Image buttonBackground;
    public Text buttonHint;

    [Header("Slot Buttons")]
    public Button[] slotButtons;
    public Image[] slotHighlights;

    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
    public Color pressedColor = new Color(0.4f, 0.4f, 0.6f, 0.9f);
    public Color activeSlotColor = new Color(1f, 0.8f, 0.2f, 0.9f);

    // State
    private int lastUsedSlot = 1;
    private float lastTapTime = 0f;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Canvas canvas;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateUI();

        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged += OnWeaponChanged;
        }
    }

    void OnDestroy()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnWeaponChanged -= OnWeaponChanged;
        }
    }

    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdTimeForWheel)
            {
                isHolding = false;
            }
        }

        UpdateSlotHighlights();
    }

    void CreateUI()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        CreateQuickSwitchButton();
        CreateSlotButtons();
    }

    void CreateQuickSwitchButton()
    {
        GameObject btnObj = new GameObject("QuickSwitchButton");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-100, 150);
        rect.sizeDelta = new Vector2(65, 65);

        buttonBackground = btnObj.AddComponent<Image>();
        buttonBackground.color = normalColor;

        Texture2D circleTex = CreateCircleTexture(64);
        buttonBackground.sprite = Sprite.Create(circleTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        quickSwitchButton = btnObj.AddComponent<Button>();
        quickSwitchButton.targetGraphic = buttonBackground;

        EventTrigger trigger = btnObj.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => OnButtonDown());
        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => OnButtonUp());
        trigger.triggers.Add(pointerUp);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.2f);
        iconRect.anchorMax = new Vector2(0.8f, 0.8f);
        iconRect.sizeDelta = Vector2.zero;

        buttonIcon = iconObj.AddComponent<Image>();
        buttonIcon.color = Color.white;

        Texture2D arrowTex = CreateSwapArrowTexture(32);
        buttonIcon.sprite = Sprite.Create(arrowTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // Hint
        GameObject hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(btnObj.transform, false);

        RectTransform hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0);
        hintRect.anchorMax = new Vector2(0.5f, 0);
        hintRect.pivot = new Vector2(0.5f, 1);
        hintRect.anchoredPosition = new Vector2(0, -5);
        hintRect.sizeDelta = new Vector2(80, 20);

        buttonHint = hintObj.AddComponent<Text>();
        buttonHint.text = "SWAP";
        buttonHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonHint.fontSize = 11;
        buttonHint.color = new Color(0.7f, 0.7f, 0.7f);
        buttonHint.alignment = TextAnchor.MiddleCenter;
    }

    void CreateSlotButtons()
    {
        slotButtons = new Button[3];
        slotHighlights = new Image[3];

        float startX = -280f;
        float y = 20f;
        float spacing = 75f;

        string[] slotNames = { "1", "2", "3" };
        string[] slotLabels = { "ANA", "YAN", "OZEL" };

        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i;

            GameObject slotObj = new GameObject($"SlotButton_{i}");
            slotObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(startX + (i * spacing), y);
            rect.sizeDelta = new Vector2(70, 55);

            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            slotButtons[i] = slotObj.AddComponent<Button>();
            slotButtons[i].targetGraphic = bg;
            slotButtons[i].onClick.AddListener(() => OnSlotButtonClick(slotIndex));

            // Highlight
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(slotObj.transform, false);

            RectTransform hlRect = highlightObj.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.offsetMin = new Vector2(-3, -3);
            hlRect.offsetMax = new Vector2(3, 3);

            slotHighlights[i] = highlightObj.AddComponent<Image>();
            slotHighlights[i].color = Color.clear;

            Texture2D outlineTex = CreateOutlineTexture(32, 3);
            slotHighlights[i].sprite = Sprite.Create(outlineTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
            slotHighlights[i].type = Image.Type.Sliced;

            highlightObj.transform.SetAsFirstSibling();

            // Number
            GameObject numObj = new GameObject("Number");
            numObj.transform.SetParent(slotObj.transform, false);

            RectTransform numRect = numObj.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0, 0.5f);
            numRect.anchorMax = new Vector2(0.35f, 1);
            numRect.sizeDelta = Vector2.zero;

            Text numText = numObj.AddComponent<Text>();
            numText.text = slotNames[i];
            numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            numText.fontSize = 16;
            numText.fontStyle = FontStyle.Bold;
            numText.color = new Color(0.6f, 0.6f, 0.6f);
            numText.alignment = TextAnchor.MiddleCenter;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(slotObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.45f);
            labelRect.sizeDelta = Vector2.zero;

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = slotLabels[i];
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 10;
            labelText.color = new Color(0.5f, 0.5f, 0.5f);
            labelText.alignment = TextAnchor.MiddleCenter;

            // Weapon icon
            GameObject iconObj = new GameObject($"WeaponIcon_{i}");
            iconObj.transform.SetParent(slotObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.35f, 0.35f);
            iconRect.anchorMax = new Vector2(0.95f, 0.95f);
            iconRect.sizeDelta = Vector2.zero;

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.color = new Color(0.8f, 0.8f, 0.8f);
        }

        // Initial update
        UpdateSlotIcons();
    }

    void OnButtonDown()
    {
        isHolding = true;
        holdTimer = 0f;
        if (buttonBackground != null)
            buttonBackground.color = pressedColor;
    }

    void OnButtonUp()
    {
        if (buttonBackground != null)
            buttonBackground.color = normalColor;

        if (isHolding && holdTimer < holdTimeForWheel)
        {
            float timeSinceLastTap = Time.unscaledTime - lastTapTime;

            if (timeSinceLastTap < doubleTapTime)
            {
                SwitchToLastWeapon();
            }
            else
            {
                SwitchToNextWeapon();
            }

            lastTapTime = Time.unscaledTime;
        }

        isHolding = false;
    }

    void OnSlotButtonClick(int slot)
    {
        if (WeaponManager.Instance == null) return;

        int currentSlot = WeaponManager.Instance.currentSlot;
        if (currentSlot != slot)
        {
            lastUsedSlot = currentSlot;
        }

        WeaponManager.Instance.SwitchToSlot(slot);
        StartCoroutine(SlotPressAnimation(slot));
    }

    void SwitchToNextWeapon()
    {
        if (WeaponManager.Instance == null) return;

        int currentSlot = WeaponManager.Instance.currentSlot;
        lastUsedSlot = currentSlot;

        for (int i = 1; i <= 3; i++)
        {
            int nextSlot = (currentSlot + i) % 3;
            WeaponInstance weapon = GetWeaponAtSlot(nextSlot);

            if (weapon != null && weapon.isUnlocked)
            {
                WeaponManager.Instance.SwitchToSlot(nextSlot);
                ShowSwitchFeedback(nextSlot);
                return;
            }
        }
    }

    void SwitchToLastWeapon()
    {
        if (WeaponManager.Instance == null) return;

        WeaponInstance lastWeapon = GetWeaponAtSlot(lastUsedSlot);

        if (lastWeapon != null && lastWeapon.isUnlocked)
        {
            int currentSlot = WeaponManager.Instance.currentSlot;
            int targetSlot = lastUsedSlot;
            lastUsedSlot = currentSlot;

            WeaponManager.Instance.SwitchToSlot(targetSlot);
            ShowSwitchFeedback(targetSlot, true);
        }
        else
        {
            SwitchToNextWeapon();
        }
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

    void OnWeaponChanged(WeaponInstance weapon)
    {
        UpdateSlotHighlights();
        UpdateSlotIcons();
    }

    void UpdateSlotHighlights()
    {
        if (WeaponManager.Instance == null || slotHighlights == null) return;

        int currentSlot = WeaponManager.Instance.currentSlot;

        for (int i = 0; i < 3; i++)
        {
            if (slotHighlights[i] == null) continue;

            WeaponInstance weapon = GetWeaponAtSlot(i);

            if (i == currentSlot && weapon != null && weapon.isUnlocked)
            {
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                slotHighlights[i].color = rarityColor;
            }
            else if (weapon != null && weapon.isUnlocked)
            {
                Color rarityColor = WeaponRarityHelper.GetRarityColor(weapon.rarity);
                slotHighlights[i].color = new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f, 0.5f);
            }
            else
            {
                slotHighlights[i].color = Color.clear;
            }
        }
    }

    void UpdateSlotIcons()
    {
        if (canvas == null) return;

        for (int i = 0; i < 3; i++)
        {
            Transform slotBtn = canvas.transform.Find($"SlotButton_{i}");
            if (slotBtn == null) continue;

            Transform iconTrans = slotBtn.Find($"WeaponIcon_{i}");
            if (iconTrans == null) continue;

            Image iconImg = iconTrans.GetComponent<Image>();
            if (iconImg == null) continue;

            WeaponInstance weapon = GetWeaponAtSlot(i);

            if (weapon != null && weapon.isUnlocked)
            {
                iconImg.enabled = true;
                iconImg.sprite = WeaponSpriteLoader.GetWeaponIcon(weapon.data.type);
                iconImg.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);
            }
            else
            {
                iconImg.enabled = false;
            }
        }
    }

    void ShowSwitchFeedback(int slot, bool isQuickSwitch = false)
    {
        StartCoroutine(SlotPressAnimation(slot));

        if (isQuickSwitch && buttonIcon != null)
        {
            StartCoroutine(QuickSwitchAnimation());
        }
    }

    System.Collections.IEnumerator SlotPressAnimation(int slot)
    {
        if (slot < 0 || slot >= slotButtons.Length || slotButtons[slot] == null) yield break;

        Transform slotTrans = slotButtons[slot].transform;
        Vector3 originalScale = slotTrans.localScale;

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            slotTrans.localScale = originalScale * (1f + 0.2f * (1f - t));
            yield return null;
        }

        slotTrans.localScale = originalScale;
    }

    System.Collections.IEnumerator QuickSwitchAnimation()
    {
        if (buttonIcon == null) yield break;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            buttonIcon.transform.rotation = Quaternion.Euler(0, 0, t * 360f);
            yield return null;
        }

        buttonIcon.transform.rotation = Quaternion.identity;
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

    Texture2D CreateSwapArrowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.clear;

        int centerY = size / 2;

        // Upper arrow (right)
        for (int x = size / 4; x < size * 3 / 4; x++)
        {
            int y = centerY + 4;
            if (y >= 0 && y < size)
                colors[y * size + x] = Color.white;
        }
        for (int j = 0; j < 4; j++)
        {
            int x = size * 3 / 4 - j;
            int y1 = centerY + 4 + j;
            int y2 = centerY + 4 - j;
            if (x >= 0 && x < size && y1 >= 0 && y1 < size)
                colors[y1 * size + x] = Color.white;
            if (x >= 0 && x < size && y2 >= 0 && y2 < size)
                colors[y2 * size + x] = Color.white;
        }

        // Lower arrow (left)
        for (int x = size / 4; x < size * 3 / 4; x++)
        {
            int y = centerY - 4;
            if (y >= 0 && y < size)
                colors[y * size + x] = Color.white;
        }
        for (int j = 0; j < 4; j++)
        {
            int x = size / 4 + j;
            int y1 = centerY - 4 + j;
            int y2 = centerY - 4 - j;
            if (x >= 0 && x < size && y1 >= 0 && y1 < size)
                colors[y1 * size + x] = Color.white;
            if (x >= 0 && x < size && y2 >= 0 && y2 < size)
                colors[y2 * size + x] = Color.white;
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }

    Texture2D CreateOutlineTexture(int size, int thickness)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isEdge = x < thickness || x >= size - thickness ||
                             y < thickness || y >= size - thickness;

                colors[y * size + x] = isEdge ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }
}
