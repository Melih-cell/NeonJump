using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Gelişmiş Envanter UI Sistemi
/// - Hızlı erişim slotları (5,6,7,8 tuşları)
/// - Tam envanter paneli (Tab/I tuşu)
/// - Tooltip sistemi
/// - Aktif efekt göstergesi
/// - Eşya toplama bildirimi
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("UI References")]
    private Canvas mainCanvas;
    private GameObject quickSlotsPanel;
    private GameObject fullInventoryPanel;
    private GameObject tooltipPanel;
    private GameObject activeEffectsPanel;
    private GameObject notificationPanel;

    private List<QuickSlotUI> quickSlots = new List<QuickSlotUI>();
    private List<InventorySlotSimpleUI> inventorySlots = new List<InventorySlotSimpleUI>();
    private List<ActiveEffectUI> activeEffects = new List<ActiveEffectUI>();

    [Header("Settings")]
    public float slotSize = 60f;
    public float slotSpacing = 8f;
    public float margin = 15f;

    // Tooltip
    private TextMeshProUGUI tooltipTitle;
    private TextMeshProUGUI tooltipDesc;
    private Image tooltipIcon;
    private bool isTooltipVisible = false;

    // Full inventory state
    private bool isInventoryOpen = false;

    // Notification queue
    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isShowingNotification = false;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        CreateMainCanvas();
        CreateQuickSlots();
        CreateFullInventoryPanel();
        CreateTooltip();
        CreateActiveEffectsPanel();
        CreateNotificationPanel();

        // Event'lere abone ol
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemChanged += OnItemChanged;
            InventoryManager.Instance.OnItemUsed += OnItemUsed;
        }

        RefreshAllSlots();
    }

    void Update()
    {
        HandleInput();
        UpdateActiveEffects();
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Envanter aç/kapa (Tab veya I)
        if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
        {
            ToggleFullInventory();
        }

        // Hızlı slot kullanımı (5, 6, 7, 8 tuşları)
        if (keyboard.digit5Key.wasPressedThisFrame) UseQuickSlot(0);
        if (keyboard.digit6Key.wasPressedThisFrame) UseQuickSlot(1);
        if (keyboard.digit7Key.wasPressedThisFrame) UseQuickSlot(2);
        if (keyboard.digit8Key.wasPressedThisFrame) UseQuickSlot(3);

        // ESC ile envanter kapat
        if (keyboard.escapeKey.wasPressedThisFrame && isInventoryOpen)
        {
            CloseFullInventory();
        }
    }

    void UseQuickSlot(int index)
    {
        if (InventoryManager.Instance != null)
        {
            bool used = InventoryManager.Instance.UseQuickSlot(index);
            if (index < quickSlots.Count)
            {
                if (used)
                    quickSlots[index].PlayUseAnimation();
                else
                    quickSlots[index].PlayEmptyAnimation();
            }
        }
    }

    // === CANVAS ===

    void CreateMainCanvas()
    {
        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("InventoryCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 95;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }
    }

    // === HIZLI ERİŞİM SLOTLARI ===

    void CreateQuickSlots()
    {
        quickSlotsPanel = new GameObject("QuickSlotsPanel");
        quickSlotsPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRt = quickSlotsPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1, 0);
        panelRt.anchorMax = new Vector2(1, 0);
        panelRt.pivot = new Vector2(1, 0);
        panelRt.anchoredPosition = new Vector2(-margin, margin + 80); // Silah UI'ın üstünde

        HorizontalLayoutGroup hlg = quickSlotsPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = slotSpacing;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.reverseArrangement = true;

        ContentSizeFitter csf = quickSlotsPanel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4 slot oluştur
        int slotCount = InventoryManager.Instance != null ? InventoryManager.Instance.quickSlotCount : 4;
        for (int i = 0; i < slotCount; i++)
        {
            CreateQuickSlot(i);
        }
    }

    void CreateQuickSlot(int index)
    {
        GameObject slotObj = new GameObject("QuickSlot_" + index);
        slotObj.transform.SetParent(quickSlotsPanel.transform, false);

        RectTransform rt = slotObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        // Arka plan (gradient efektli)
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);

        // Çerçeve
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slotObj.transform, false);
        RectTransform borderRt = borderObj.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = Vector2.zero;
        Image border = borderObj.AddComponent<Image>();
        border.color = new Color(0.3f, 0.35f, 0.45f, 0.8f);
        border.type = Image.Type.Sliced;
        border.raycastTarget = false;

        // Button
        Button btn = slotObj.AddComponent<Button>();
        int slotIndex = index;
        btn.onClick.AddListener(() => UseQuickSlot(slotIndex));

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.1f, 0.12f, 0.18f);
        colors.highlightedColor = new Color(0.18f, 0.22f, 0.32f);
        colors.pressedColor = new Color(0.25f, 0.3f, 0.4f);
        btn.colors = colors;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.1f, 0.2f);
        iconRt.anchorMax = new Vector2(0.9f, 0.95f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image icon = iconObj.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        // Count (sağ alt)
        GameObject countObj = new GameObject("Count");
        countObj.transform.SetParent(slotObj.transform, false);
        RectTransform countRt = countObj.AddComponent<RectTransform>();
        countRt.anchorMin = new Vector2(0.5f, 0);
        countRt.anchorMax = new Vector2(1, 0.3f);
        countRt.offsetMin = Vector2.zero;
        countRt.offsetMax = Vector2.zero;

        TextMeshProUGUI countText = countObj.AddComponent<TextMeshProUGUI>();
        countText.text = "0";
        countText.fontSize = 16;
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.color = Color.white;
        countText.fontStyle = FontStyles.Bold;
        countText.raycastTarget = false;

        // Kısayol tuşu göstergesi (sol üst)
        GameObject keyObj = new GameObject("KeyHint");
        keyObj.transform.SetParent(slotObj.transform, false);
        RectTransform keyRt = keyObj.AddComponent<RectTransform>();
        keyRt.anchorMin = new Vector2(0, 0.7f);
        keyRt.anchorMax = new Vector2(0.35f, 1);
        keyRt.offsetMin = Vector2.zero;
        keyRt.offsetMax = Vector2.zero;

        // Key background
        Image keyBg = keyObj.AddComponent<Image>();
        keyBg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
        keyBg.raycastTarget = false;

        GameObject keyTextObj = new GameObject("KeyText");
        keyTextObj.transform.SetParent(keyObj.transform, false);
        RectTransform keyTextRt = keyTextObj.AddComponent<RectTransform>();
        keyTextRt.anchorMin = Vector2.zero;
        keyTextRt.anchorMax = Vector2.one;
        keyTextRt.offsetMin = Vector2.zero;
        keyTextRt.offsetMax = Vector2.zero;

        TextMeshProUGUI keyText = keyTextObj.AddComponent<TextMeshProUGUI>();
        keyText.text = (index + 5).ToString(); // 5, 6, 7, 8
        keyText.fontSize = 12;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = new Color(0.7f, 0.75f, 0.85f);
        keyText.raycastTarget = false;

        // Cooldown overlay
        GameObject cooldownObj = new GameObject("Cooldown");
        cooldownObj.transform.SetParent(slotObj.transform, false);
        RectTransform cooldownRt = cooldownObj.AddComponent<RectTransform>();
        cooldownRt.anchorMin = Vector2.zero;
        cooldownRt.anchorMax = Vector2.one;
        cooldownRt.offsetMin = Vector2.zero;
        cooldownRt.offsetMax = Vector2.zero;
        Image cooldown = cooldownObj.AddComponent<Image>();
        cooldown.color = new Color(0, 0, 0, 0.6f);
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = 0;
        cooldown.fillAmount = 0;
        cooldown.raycastTarget = false;

        // QuickSlotUI component
        QuickSlotUI slotUI = slotObj.AddComponent<QuickSlotUI>();
        slotUI.icon = icon;
        slotUI.countText = countText;
        slotUI.background = bg;
        slotUI.cooldownOverlay = cooldown;
        slotUI.slotIndex = index;

        // Hover events for tooltip
        EventTrigger trigger = slotObj.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => ShowTooltipForSlot(slotIndex));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => HideTooltip());
        trigger.triggers.Add(exitEntry);

        quickSlots.Add(slotUI);
    }

    // === TAM ENVANTER PANELİ ===

    void CreateFullInventoryPanel()
    {
        fullInventoryPanel = new GameObject("FullInventoryPanel");
        fullInventoryPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRt = fullInventoryPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(450, 400);

        // Arka plan
        Image bg = fullInventoryPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.15f, 0.95f);

        // Başlık
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(fullInventoryPanel.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(0, 50);

        Image titleBg = titleObj.AddComponent<Image>();
        titleBg.color = new Color(0.15f, 0.18f, 0.25f);

        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(titleObj.transform, false);
        RectTransform titleTextRt = titleTextObj.AddComponent<RectTransform>();
        titleTextRt.anchorMin = Vector2.zero;
        titleTextRt.anchorMax = Vector2.one;
        titleTextRt.offsetMin = new Vector2(15, 0);
        titleTextRt.offsetMax = new Vector2(-15, 0);

        TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ENVANTER";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;

        // Kapatma tuşu bilgisi
        GameObject closeHintObj = new GameObject("CloseHint");
        closeHintObj.transform.SetParent(titleObj.transform, false);
        RectTransform closeHintRt = closeHintObj.AddComponent<RectTransform>();
        closeHintRt.anchorMin = new Vector2(1, 0);
        closeHintRt.anchorMax = new Vector2(1, 1);
        closeHintRt.pivot = new Vector2(1, 0.5f);
        closeHintRt.anchoredPosition = new Vector2(-15, 0);
        closeHintRt.sizeDelta = new Vector2(100, 0);

        TextMeshProUGUI closeHint = closeHintObj.AddComponent<TextMeshProUGUI>();
        closeHint.text = "[TAB/ESC]";
        closeHint.fontSize = 14;
        closeHint.alignment = TextAlignmentOptions.MidlineRight;
        closeHint.color = new Color(0.6f, 0.65f, 0.75f);

        // Eşya grid container
        GameObject gridObj = new GameObject("ItemGrid");
        gridObj.transform.SetParent(fullInventoryPanel.transform, false);
        RectTransform gridRt = gridObj.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 0);
        gridRt.anchorMax = new Vector2(1, 1);
        gridRt.offsetMin = new Vector2(20, 20);
        gridRt.offsetMax = new Vector2(-20, -60);

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 95);
        grid.spacing = new Vector2(10, 10);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        // Her eşya tipi için slot oluştur
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            CreateInventorySlot(gridObj.transform, type);
        }

        fullInventoryPanel.SetActive(false);
    }

    void CreateInventorySlot(Transform parent, ItemType itemType)
    {
        InventoryItem itemInfo = InventoryItem.Create(itemType);

        GameObject slotObj = new GameObject("Slot_" + itemType.ToString());
        slotObj.transform.SetParent(parent, false);

        // Arka plan
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.15f, 0.22f, 0.9f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.1f, 0.3f);
        iconRt.anchorMax = new Vector2(0.9f, 0.95f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = InventorySprites.CreateItemSprite(itemType);
        icon.preserveAspect = true;

        // İsim
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slotObj.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0);
        nameRt.anchorMax = new Vector2(1, 0.28f);
        nameRt.offsetMin = new Vector2(3, 0);
        nameRt.offsetMax = new Vector2(-3, 0);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = itemInfo.name;
        nameText.fontSize = 11;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.8f, 0.85f, 0.95f);
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        // Miktar badge
        GameObject countObj = new GameObject("Count");
        countObj.transform.SetParent(slotObj.transform, false);
        RectTransform countRt = countObj.AddComponent<RectTransform>();
        countRt.anchorMin = new Vector2(0.65f, 0.7f);
        countRt.anchorMax = new Vector2(1, 0.95f);
        countRt.offsetMin = Vector2.zero;
        countRt.offsetMax = Vector2.zero;

        Image countBg = countObj.AddComponent<Image>();
        countBg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);

        GameObject countTextObj = new GameObject("CountText");
        countTextObj.transform.SetParent(countObj.transform, false);
        RectTransform countTextRt = countTextObj.AddComponent<RectTransform>();
        countTextRt.anchorMin = Vector2.zero;
        countTextRt.anchorMax = Vector2.one;
        countTextRt.offsetMin = Vector2.zero;
        countTextRt.offsetMax = Vector2.zero;

        TextMeshProUGUI countText = countTextObj.AddComponent<TextMeshProUGUI>();
        countText.text = "0";
        countText.fontSize = 14;
        countText.alignment = TextAlignmentOptions.Center;
        countText.color = Color.white;
        countText.fontStyle = FontStyles.Bold;

        // Button
        Button btn = slotObj.AddComponent<Button>();
        ItemType capturedType = itemType;
        btn.onClick.AddListener(() => OnInventorySlotClicked(capturedType));

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.15f, 0.22f);
        colors.highlightedColor = new Color(0.2f, 0.25f, 0.35f);
        colors.pressedColor = new Color(0.25f, 0.3f, 0.4f);
        btn.colors = colors;

        // Slot UI component
        InventorySlotSimpleUI slotUI = slotObj.AddComponent<InventorySlotSimpleUI>();
        slotUI.itemType = itemType;
        slotUI.icon = icon;
        slotUI.countText = countText;
        slotUI.background = bg;

        // Hover tooltip
        EventTrigger trigger = slotObj.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => ShowTooltipForItem(itemType));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => HideTooltip());
        trigger.triggers.Add(exitEntry);

        inventorySlots.Add(slotUI);
    }

    void OnInventorySlotClicked(ItemType type)
    {
        if (InventoryManager.Instance != null)
        {
            bool used = InventoryManager.Instance.UseItem(type);
            if (used)
            {
                ShowNotification(type, -1); // -1 = kullanıldı
            }
        }
    }

    // === TOOLTIP ===

    void CreateTooltip()
    {
        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = tooltipPanel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);
        rt.pivot = new Vector2(0, 1);

        Image bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);
        bg.raycastTarget = false;

        // Çerçeve
        Outline outline = tooltipPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.35f, 0.45f);
        outline.effectDistance = new Vector2(2, 2);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.3f);
        iconRt.anchorMax = new Vector2(0, 1);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, -5);
        iconRt.sizeDelta = new Vector2(40, 40);
        tooltipIcon = iconObj.AddComponent<Image>();
        tooltipIcon.preserveAspect = true;

        // Başlık
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0.55f);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.offsetMin = new Vector2(55, 0);
        titleRt.offsetMax = new Vector2(-10, -8);
        tooltipTitle = titleObj.AddComponent<TextMeshProUGUI>();
        tooltipTitle.fontSize = 16;
        tooltipTitle.fontStyle = FontStyles.Bold;
        tooltipTitle.color = Color.white;

        // Açıklama
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0, 0);
        descRt.anchorMax = new Vector2(1, 0.55f);
        descRt.offsetMin = new Vector2(55, 8);
        descRt.offsetMax = new Vector2(-10, 0);
        tooltipDesc = descObj.AddComponent<TextMeshProUGUI>();
        tooltipDesc.fontSize = 12;
        tooltipDesc.color = new Color(0.7f, 0.75f, 0.85f);

        tooltipPanel.SetActive(false);
    }

    void ShowTooltipForSlot(int slotIndex)
    {
        if (InventoryManager.Instance == null) return;
        ItemType type = InventoryManager.Instance.GetQuickSlotItem(slotIndex);
        ShowTooltipForItem(type);
    }

    void ShowTooltipForItem(ItemType type)
    {
        InventoryItem item = InventoryItem.Create(type);
        int count = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(type) : 0;

        tooltipIcon.sprite = InventorySprites.CreateItemSprite(type);
        tooltipTitle.text = item.name;
        tooltipDesc.text = item.description + "\nAdet: " + count;

        tooltipPanel.SetActive(true);
        isTooltipVisible = true;

        // Mouse pozisyonuna taşı
        StartCoroutine(UpdateTooltipPosition());
    }

    System.Collections.IEnumerator UpdateTooltipPosition()
    {
        while (isTooltipVisible && tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            rt.position = new Vector3(mousePos.x + 15, mousePos.y - 10, 0);
            yield return null;
        }
    }

    void HideTooltip()
    {
        isTooltipVisible = false;
        tooltipPanel.SetActive(false);
    }

    // === AKTİF EFEKTLER ===

    void CreateActiveEffectsPanel()
    {
        activeEffectsPanel = new GameObject("ActiveEffectsPanel");
        activeEffectsPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = activeEffectsPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(margin, -margin - 50);

        VerticalLayoutGroup vlg = activeEffectsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = activeEffectsPanel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void UpdateActiveEffects()
    {
        if (PowerUpManager.Instance == null) return;

        // Aktif power-up'ları kontrol et
        ItemType[] timedItems = { ItemType.Shield, ItemType.SpeedBoost, ItemType.DoubleDamage, ItemType.Magnet };
        PowerUpType[] powerUpTypes = { PowerUpType.Shield, PowerUpType.SpeedBoost, PowerUpType.SpeedBoost, PowerUpType.Magnet };

        for (int i = 0; i < timedItems.Length; i++)
        {
            // PowerUpManager'dan kalan süreyi al
            float remaining = 0f;

            // Shield için özel kontrol
            if (timedItems[i] == ItemType.Shield)
            {
                remaining = PowerUpManager.Instance.GetRemainingTime(PowerUpType.Shield);
            }
            else if (timedItems[i] == ItemType.SpeedBoost)
            {
                remaining = PowerUpManager.Instance.GetRemainingTime(PowerUpType.SpeedBoost);
            }
            else if (timedItems[i] == ItemType.Magnet)
            {
                remaining = PowerUpManager.Instance.GetRemainingTime(PowerUpType.Magnet);
            }

            if (remaining > 0)
            {
                UpdateOrCreateActiveEffect(timedItems[i], remaining);
            }
            else
            {
                RemoveActiveEffect(timedItems[i]);
            }
        }
    }

    void UpdateOrCreateActiveEffect(ItemType type, float remaining)
    {
        // Mevcut efekt var mı?
        ActiveEffectUI existing = activeEffects.Find(e => e.itemType == type);
        if (existing != null)
        {
            existing.UpdateTime(remaining);
        }
        else
        {
            // Yeni efekt oluştur
            CreateActiveEffectUI(type, remaining);
        }
    }

    void CreateActiveEffectUI(ItemType type, float duration)
    {
        InventoryItem item = InventoryItem.Create(type);

        GameObject effectObj = new GameObject("Effect_" + type.ToString());
        effectObj.transform.SetParent(activeEffectsPanel.transform, false);

        RectTransform rt = effectObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150, 30);

        Image bg = effectObj.AddComponent<Image>();
        bg.color = new Color(item.itemColor.r * 0.3f, item.itemColor.g * 0.3f, item.itemColor.b * 0.3f, 0.8f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(effectObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0);
        iconRt.anchorMax = new Vector2(0, 1);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(5, 0);
        iconRt.sizeDelta = new Vector2(25, 25);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = InventorySprites.CreateItemSprite(type);
        icon.preserveAspect = true;

        // Timer text
        GameObject timerObj = new GameObject("Timer");
        timerObj.transform.SetParent(effectObj.transform, false);
        RectTransform timerRt = timerObj.AddComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0, 0);
        timerRt.anchorMax = new Vector2(1, 1);
        timerRt.offsetMin = new Vector2(35, 0);
        timerRt.offsetMax = new Vector2(-5, 0);
        TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = duration.ToString("F1") + "s";
        timerText.fontSize = 14;
        timerText.alignment = TextAlignmentOptions.MidlineLeft;
        timerText.color = Color.white;

        // Progress bar
        GameObject barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(effectObj.transform, false);
        RectTransform barBgRt = barBgObj.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0, 0);
        barBgRt.anchorMax = new Vector2(1, 0);
        barBgRt.pivot = new Vector2(0, 0);
        barBgRt.anchoredPosition = Vector2.zero;
        barBgRt.sizeDelta = new Vector2(0, 4);
        Image barBg = barBgObj.AddComponent<Image>();
        barBg.color = new Color(0, 0, 0, 0.5f);

        GameObject barObj = new GameObject("Bar");
        barObj.transform.SetParent(barBgObj.transform, false);
        RectTransform barRt = barObj.AddComponent<RectTransform>();
        barRt.anchorMin = Vector2.zero;
        barRt.anchorMax = new Vector2(1, 1);
        barRt.offsetMin = Vector2.zero;
        barRt.offsetMax = Vector2.zero;
        Image bar = barObj.AddComponent<Image>();
        bar.color = item.itemColor;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;

        ActiveEffectUI effectUI = effectObj.AddComponent<ActiveEffectUI>();
        effectUI.itemType = type;
        effectUI.timerText = timerText;
        effectUI.progressBar = bar;
        effectUI.maxDuration = duration;

        activeEffects.Add(effectUI);
    }

    void RemoveActiveEffect(ItemType type)
    {
        ActiveEffectUI existing = activeEffects.Find(e => e.itemType == type);
        if (existing != null)
        {
            activeEffects.Remove(existing);
            Destroy(existing.gameObject);
        }
    }

    // === BİLDİRİMLER ===

    void CreateNotificationPanel()
    {
        notificationPanel = new GameObject("NotificationPanel");
        notificationPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = notificationPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.8f);
        rt.anchorMax = new Vector2(0.5f, 0.8f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 50);

        Image bg = notificationPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.15f, 0.2f, 0.9f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(notificationPanel.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0);
        iconRt.anchorMax = new Vector2(0, 1);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, 0);
        iconRt.sizeDelta = new Vector2(35, 35);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(notificationPanel.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(55, 0);
        textRt.offsetMax = new Vector2(-10, 0);

        notificationPanel.SetActive(false);
    }

    public void ShowNotification(ItemType type, int amount)
    {
        notificationQueue.Enqueue(new NotificationData(type, amount));
        if (!isShowingNotification)
        {
            StartCoroutine(ShowNextNotification());
        }
    }

    System.Collections.IEnumerator ShowNextNotification()
    {
        while (notificationQueue.Count > 0)
        {
            isShowingNotification = true;
            NotificationData data = notificationQueue.Dequeue();

            InventoryItem item = InventoryItem.Create(data.type);

            // Update notification UI
            Image icon = notificationPanel.transform.Find("Icon").GetComponent<Image>();
            if (icon == null)
            {
                icon = notificationPanel.transform.Find("Icon").gameObject.AddComponent<Image>();
            }
            icon.sprite = InventorySprites.CreateItemSprite(data.type);
            icon.preserveAspect = true;

            TextMeshProUGUI text = notificationPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = notificationPanel.transform.Find("Text").gameObject.AddComponent<TextMeshProUGUI>();
                text.fontSize = 18;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.color = Color.white;
            }

            if (data.amount > 0)
                text.text = "+" + data.amount + " " + item.name;
            else if (data.amount == -1)
                text.text = item.name + " kullanildi!";
            else
                text.text = item.name;

            // Animate in
            notificationPanel.SetActive(true);
            CanvasGroup cg = notificationPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = notificationPanel.AddComponent<CanvasGroup>();

            float t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = t / 0.3f;
                yield return null;
            }

            // Wait
            yield return new WaitForSecondsRealtime(1.5f);

            // Animate out
            t = 0;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - (t / 0.3f);
                yield return null;
            }

            notificationPanel.SetActive(false);
        }

        isShowingNotification = false;
    }

    // === ENVANTER AÇ/KAPA ===

    public void ToggleFullInventory()
    {
        if (isInventoryOpen)
            CloseFullInventory();
        else
            OpenFullInventory();
    }

    public void OpenFullInventory()
    {
        isInventoryOpen = true;
        fullInventoryPanel.SetActive(true);
        RefreshInventorySlots();
        Time.timeScale = 0.1f; // Oyunu yavaşlat
    }

    public void CloseFullInventory()
    {
        isInventoryOpen = false;
        fullInventoryPanel.SetActive(false);
        HideTooltip();
        Time.timeScale = 1f;
    }

    // === REFRESH ===

    void OnItemChanged(ItemType type, int newCount)
    {
        RefreshSlotForItem(type);
        RefreshInventorySlots();

        // Bildirim göster (toplama)
        if (newCount > 0)
        {
            // ShowNotification(type, 1); // Her eklemede bildirim - opsiyonel
        }
    }

    void OnItemUsed(ItemType type)
    {
        RefreshSlotForItem(type);
        RefreshInventorySlots();
    }

    void RefreshSlotForItem(ItemType type)
    {
        if (InventoryManager.Instance == null) return;

        foreach (var slot in quickSlots)
        {
            ItemType slotItem = InventoryManager.Instance.GetQuickSlotItem(slot.slotIndex);
            if (slotItem == type)
            {
                slot.Refresh();
            }
        }
    }

    public void RefreshAllSlots()
    {
        foreach (var slot in quickSlots)
        {
            slot.Refresh();
        }
        RefreshInventorySlots();
    }

    void RefreshInventorySlots()
    {
        foreach (var slot in inventorySlots)
        {
            slot.Refresh();
        }
    }

    public void SetVisible(bool visible)
    {
        if (quickSlotsPanel != null)
            quickSlotsPanel.SetActive(visible);
    }
}

// === YARDIMCI SINIFLAR ===

public class QuickSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI countText;
    public Image background;
    public Image cooldownOverlay;
    public int slotIndex;

    private Color originalBgColor;

    void Start()
    {
        originalBgColor = background.color;
        Refresh();
    }

    public void Refresh()
    {
        if (InventoryManager.Instance == null) return;

        ItemType itemType = InventoryManager.Instance.GetQuickSlotItem(slotIndex);
        int count = InventoryManager.Instance.GetItemCount(itemType);

        // Icon sprite
        icon.sprite = InventorySprites.CreateItemSprite(itemType);

        // Miktar
        countText.text = count.ToString();

        // Eşya yoksa soluk göster
        float alpha = count > 0 ? 1f : 0.3f;
        icon.color = new Color(1, 1, 1, alpha);
        countText.color = count > 0 ? Color.white : new Color(0.4f, 0.4f, 0.4f);
    }

    public void PlayUseAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(UseAnimationCoroutine());
    }

    public void PlayEmptyAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(EmptyAnimationCoroutine());
    }

    System.Collections.IEnumerator UseAnimationCoroutine()
    {
        background.color = new Color(0.15f, 0.5f, 0.2f, 0.95f);
        transform.localScale = Vector3.one * 1.15f;

        float t = 0;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t / 0.25f);
            background.color = Color.Lerp(new Color(0.15f, 0.5f, 0.2f, 0.95f), originalBgColor, t / 0.25f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        background.color = originalBgColor;
        Refresh();
    }

    System.Collections.IEnumerator EmptyAnimationCoroutine()
    {
        background.color = new Color(0.5f, 0.15f, 0.15f, 0.95f);

        float t = 0;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float shake = Mathf.Sin(t * 50f) * 3f * (1f - t / 0.3f);
            transform.localPosition = new Vector3(shake, transform.localPosition.y, 0);
            background.color = Color.Lerp(new Color(0.5f, 0.15f, 0.15f, 0.95f), originalBgColor, t / 0.3f);
            yield return null;
        }

        transform.localPosition = Vector3.zero;
        background.color = originalBgColor;
    }
}

public class InventorySlotSimpleUI : MonoBehaviour
{
    public ItemType itemType;
    public Image icon;
    public TextMeshProUGUI countText;
    public Image background;

    public void Refresh()
    {
        if (InventoryManager.Instance == null) return;

        int count = InventoryManager.Instance.GetItemCount(itemType);
        countText.text = count.ToString();

        float alpha = count > 0 ? 1f : 0.4f;
        icon.color = new Color(1, 1, 1, alpha);
        background.color = count > 0 ?
            new Color(0.12f, 0.15f, 0.22f, 0.9f) :
            new Color(0.08f, 0.1f, 0.15f, 0.7f);
    }
}

public class ActiveEffectUI : MonoBehaviour
{
    public ItemType itemType;
    public TextMeshProUGUI timerText;
    public Image progressBar;
    public float maxDuration;

    public void UpdateTime(float remaining)
    {
        timerText.text = remaining.ToString("F1") + "s";
        progressBar.fillAmount = remaining / maxDuration;
    }
}

public struct NotificationData
{
    public ItemType type;
    public int amount;

    public NotificationData(ItemType t, int a)
    {
        type = t;
        amount = a;
    }
}
