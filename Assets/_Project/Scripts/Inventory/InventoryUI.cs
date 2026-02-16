using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Gelismis Envanter UI Sistemi
/// - Hizli erisim slotlari (5,6,7,8 tuslari)
/// - Tam envanter paneli (Tab/I tusu + mobil buton)
/// - Tooltip sistemi (masaustu: fare takip, mobil: slot ustune sabit)
/// - Aktif efekt gostergesi
/// - Esya toplama bildirimi
/// - Mobil uyumlu: scroll, responsive boyut, kapatma butonu
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
    private RectTransform _lastTappedSlotRt; // Mobilde tooltip pozisyonu icin

    // Full inventory state
    private bool isInventoryOpen = false;

    // Mobil alt detay paneli
    private GameObject _mobileDetailPanel;
    private Image _mobileDetailIcon;
    private TextMeshProUGUI _mobileDetailName;
    private TextMeshProUGUI _mobileDetailDesc;
    private Button _mobileDetailUseBtn;
    private Button _mobileDetailEquipBtn;
    private Button _mobileDetailDropBtn;
    private ItemType _selectedItemType;
    private InventorySlotSimpleUI _selectedSlot;

    // Notification queue
    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isShowingNotification = false;

    // Mobil
    private bool _isMobile = false;
    private GameObject _mobileInventoryButton;
    private GameObject _closeButton;
    private ScrollRect _inventoryScrollRect;
    private GridLayoutGroup _itemGrid;

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
        _isMobile = IsMobilePlatform();

        CreateMainCanvas();
        CreateQuickSlots();
        CreateFullInventoryPanel();
        if (!_isMobile)
        {
            CreateTooltip();
        }
        CreateActiveEffectsPanel();
        CreateNotificationPanel();

        if (_isMobile)
        {
            CreateMobileInventoryButton();
        }

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

        // Masaustunde: mobil tooltip dismiss
        if (!_isMobile && isTooltipVisible)
        {
            // Masaustunde tooltip zaten hover ile kapaniyor
        }
    }

    /// <summary>
    /// Mobil platform tespiti: gercek mobil veya MobileControls aktifse true
    /// </summary>
    bool IsMobilePlatform()
    {
        if (Application.isMobilePlatform) return true;
        if (MobileControls.Instance != null && MobileControls.Instance.IsEnabled) return true;
        return false;
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Envanter ac/kapa (Tab veya I)
        if (keyboard.tabKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
        {
            ToggleFullInventory();
        }

        // Hizli slot kullanimi (5, 6, 7, 8 tuslari)
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

    /// <summary>
    /// Mobilde baska bir yere dokunulunca tooltip'i kapat
    /// </summary>
    void HandleMobileTooltipDismiss()
    {
        if (Touchscreen.current == null) return;
        var touch = Touchscreen.current.primaryTouch;
        if (touch.press.wasPressedThisFrame)
        {
            // Tooltip paneline dokunuldu mu kontrol et
            Vector2 touchPos = touch.position.ReadValue();
            RectTransform tooltipRt = tooltipPanel.GetComponent<RectTransform>();
            if (!RectTransformUtility.RectangleContainsScreenPoint(tooltipRt, touchPos, null))
            {
                HideTooltip();
            }
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

    // === HIZLI ERISIM SLOTLARI ===

    void CreateQuickSlots()
    {
        quickSlotsPanel = new GameObject("QuickSlotsPanel");
        quickSlotsPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRt = quickSlotsPanel.AddComponent<RectTransform>();

        if (_isMobile)
        {
            // Mobilde ust ortaya
            panelRt.anchorMin = new Vector2(0.5f, 1);
            panelRt.anchorMax = new Vector2(0.5f, 1);
            panelRt.pivot = new Vector2(0.5f, 1);
            panelRt.anchoredPosition = new Vector2(0, -15);
        }
        else
        {
            // Masaustunde sag alta
            panelRt.anchorMin = new Vector2(1, 0);
            panelRt.anchorMax = new Vector2(1, 0);
            panelRt.pivot = new Vector2(1, 0);
            panelRt.anchoredPosition = new Vector2(-margin, margin + 80);
        }

        HorizontalLayoutGroup hlg = quickSlotsPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = slotSpacing;
        hlg.childAlignment = _isMobile ? TextAnchor.MiddleCenter : TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.reverseArrangement = !_isMobile;

        ContentSizeFitter csf = quickSlotsPanel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4 slot olustur
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
        float quickSlotActualSize = _isMobile ? 45f : slotSize;
        rt.sizeDelta = new Vector2(quickSlotActualSize, quickSlotActualSize);

        // Arka plan (gradient efektli)
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);

        // Cerceve
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

        // Count (sag alt)
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

        // Kisayol tusu gostergesi (sol ust)
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

        // Mobilde kisa yol tusunu gizle
        if (_isMobile)
        {
            keyObj.SetActive(false);
        }

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

    // === TAM ENVANTER PANELI ===

    void CreateFullInventoryPanel()
    {
        fullInventoryPanel = new GameObject("FullInventoryPanel");
        fullInventoryPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRt = fullInventoryPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);

        // Mobilde neredeyse fullscreen, masaustunde sabit boyut
        float panelW, panelH;
        if (_isMobile)
        {
            panelW = Screen.width * 0.95f;
            panelH = Screen.height * 0.9f;
        }
        else
        {
            panelW = 450f;
            panelH = 400f;
        }
        panelRt.sizeDelta = new Vector2(panelW, panelH);

        // Arka plan
        Image bg = fullInventoryPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.15f, 0.95f);

        // Baslik
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

        // Kapatma tusu bilgisi (masaustunde) veya X butonu (mobilde)
        if (_isMobile)
        {
            CreateCloseButton(titleObj.transform);
        }
        else
        {
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
        }

        // ScrollRect sarmalayici - content panelden buyukse kaydirilabilir
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(fullInventoryPanel.transform, false);
        RectTransform scrollViewRt = scrollViewObj.AddComponent<RectTransform>();
        scrollViewRt.anchorMin = new Vector2(0, 0);
        scrollViewRt.anchorMax = new Vector2(1, 1);
        // Mobilde altta detay paneli icin 130px yer birak
        float bottomOffset = _isMobile ? 130f : 15f;
        scrollViewRt.offsetMin = new Vector2(10, bottomOffset);
        scrollViewRt.offsetMax = new Vector2(-10, -55);

        Image scrollViewBg = scrollViewObj.AddComponent<Image>();
        scrollViewBg.color = new Color(0, 0, 0, 0.01f); // Neredeyse seffaf - raycast icin
        scrollViewObj.AddComponent<Mask>().showMaskGraphic = false;

        _inventoryScrollRect = scrollViewObj.AddComponent<ScrollRect>();
        _inventoryScrollRect.horizontal = false;
        _inventoryScrollRect.vertical = true;
        _inventoryScrollRect.movementType = ScrollRect.MovementType.Elastic;
        _inventoryScrollRect.elasticity = 0.1f;
        _inventoryScrollRect.scrollSensitivity = 20f;

        // Content objesi - GridLayout buraya ekleniyor
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        // sizeDelta.x = 0 (stretch), sizeDelta.y ContentSizeFitter ayarlayacak
        contentRt.sizeDelta = new Vector2(0, 0);

        ContentSizeFitter contentCsf = contentObj.AddComponent<ContentSizeFitter>();
        contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _inventoryScrollRect.content = contentRt;

        // Grid layout
        _itemGrid = contentObj.AddComponent<GridLayoutGroup>();
        if (_isMobile)
        {
            _itemGrid.cellSize = new Vector2(75, 75);
        }
        else
        {
            _itemGrid.cellSize = new Vector2(80, 95);
        }
        _itemGrid.spacing = new Vector2(10, 10);
        _itemGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _itemGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        _itemGrid.childAlignment = TextAnchor.UpperLeft;
        _itemGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _itemGrid.constraintCount = 4;
        _itemGrid.padding = new RectOffset(5, 5, 5, 5);

        // Scrollbar (opsiyonel, ince)
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform scrollbarRt = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRt.anchorMin = new Vector2(1, 0);
        scrollbarRt.anchorMax = new Vector2(1, 1);
        scrollbarRt.pivot = new Vector2(1, 0.5f);
        scrollbarRt.anchoredPosition = Vector2.zero;
        scrollbarRt.sizeDelta = new Vector2(6, 0);

        Image scrollbarBg = scrollbarObj.AddComponent<Image>();
        scrollbarBg.color = new Color(0.15f, 0.18f, 0.25f, 0.5f);

        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(scrollbarObj.transform, false);
        RectTransform handleRt = handleObj.AddComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.4f, 0.45f, 0.55f, 0.7f);

        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handleImg;
        _inventoryScrollRect.verticalScrollbar = scrollbar;
        _inventoryScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        _inventoryScrollRect.verticalScrollbarSpacing = 2f;

        // Her esya tipi icin slot olustur
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            CreateInventorySlot(contentObj.transform, type);
        }

        // Mobilde alt detay paneli olustur
        if (_isMobile)
        {
            CreateMobileDetailPanel(fullInventoryPanel.transform);
        }

        fullInventoryPanel.SetActive(false);
    }

    /// <summary>
    /// Mobilde envanter panelinin altinda secili item detay alani olusturur
    /// </summary>
    void CreateMobileDetailPanel(Transform parent)
    {
        _mobileDetailPanel = new GameObject("MobileDetailPanel");
        _mobileDetailPanel.transform.SetParent(parent, false);

        RectTransform rt = _mobileDetailPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 5);
        rt.sizeDelta = new Vector2(-10, 120);

        Image bg = _mobileDetailPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.13f, 0.2f, 0.95f);

        // Sol taraf: Icon (50x50)
        GameObject iconObj = new GameObject("DetailIcon");
        iconObj.transform.SetParent(_mobileDetailPanel.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(8, 10);
        iconRt.sizeDelta = new Vector2(50, 50);
        _mobileDetailIcon = iconObj.AddComponent<Image>();
        _mobileDetailIcon.preserveAspect = true;
        _mobileDetailIcon.raycastTarget = false;

        // Item adi (bold)
        GameObject nameObj = new GameObject("DetailName");
        nameObj.transform.SetParent(_mobileDetailPanel.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0, 1);
        nameRt.anchoredPosition = new Vector2(65, -5);
        nameRt.sizeDelta = new Vector2(-75, 24);
        _mobileDetailName = nameObj.AddComponent<TextMeshProUGUI>();
        _mobileDetailName.fontSize = 16;
        _mobileDetailName.fontStyle = FontStyles.Bold;
        _mobileDetailName.color = Color.white;
        _mobileDetailName.alignment = TextAlignmentOptions.MidlineLeft;
        _mobileDetailName.raycastTarget = false;

        // Item aciklamasi
        GameObject descObj = new GameObject("DetailDesc");
        descObj.transform.SetParent(_mobileDetailPanel.transform, false);
        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0, 1);
        descRt.anchorMax = new Vector2(1, 1);
        descRt.pivot = new Vector2(0, 1);
        descRt.anchoredPosition = new Vector2(65, -28);
        descRt.sizeDelta = new Vector2(-75, 30);
        _mobileDetailDesc = descObj.AddComponent<TextMeshProUGUI>();
        _mobileDetailDesc.fontSize = 11;
        _mobileDetailDesc.color = new Color(0.7f, 0.75f, 0.85f);
        _mobileDetailDesc.alignment = TextAlignmentOptions.TopLeft;
        _mobileDetailDesc.enableWordWrapping = true;
        _mobileDetailDesc.overflowMode = TextOverflowModes.Ellipsis;
        _mobileDetailDesc.raycastTarget = false;

        // Alt kisim: Butonlar (yan yana)
        GameObject buttonsRow = new GameObject("ButtonsRow");
        buttonsRow.transform.SetParent(_mobileDetailPanel.transform, false);
        RectTransform buttonsRt = buttonsRow.AddComponent<RectTransform>();
        buttonsRt.anchorMin = new Vector2(0, 0);
        buttonsRt.anchorMax = new Vector2(1, 0);
        buttonsRt.pivot = new Vector2(0.5f, 0);
        buttonsRt.anchoredPosition = new Vector2(0, 5);
        buttonsRt.sizeDelta = new Vector2(-16, 40);

        HorizontalLayoutGroup hlg = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        // KULLAN butonu (yesil, genis)
        _mobileDetailUseBtn = CreateDetailButton(buttonsRow.transform, "KULLAN",
            new Color(0.15f, 0.6f, 0.15f), new Color(0.2f, 0.7f, 0.2f));
        LayoutElement useLayout = _mobileDetailUseBtn.gameObject.AddComponent<LayoutElement>();
        useLayout.flexibleWidth = 2f;
        _mobileDetailUseBtn.onClick.AddListener(OnMobileDetailUse);

        // KUSAN butonu (mavi)
        _mobileDetailEquipBtn = CreateDetailButton(buttonsRow.transform, "KUSAN",
            new Color(0.15f, 0.3f, 0.7f), new Color(0.2f, 0.4f, 0.8f));
        LayoutElement equipLayout = _mobileDetailEquipBtn.gameObject.AddComponent<LayoutElement>();
        equipLayout.flexibleWidth = 1.5f;
        _mobileDetailEquipBtn.onClick.AddListener(OnMobileDetailEquip);

        // AT butonu (kirmizi, kucuk)
        _mobileDetailDropBtn = CreateDetailButton(buttonsRow.transform, "AT",
            new Color(0.6f, 0.15f, 0.15f), new Color(0.7f, 0.2f, 0.2f));
        LayoutElement dropLayout = _mobileDetailDropBtn.gameObject.AddComponent<LayoutElement>();
        dropLayout.flexibleWidth = 1f;
        _mobileDetailDropBtn.onClick.AddListener(OnMobileDetailDrop);

        _mobileDetailPanel.SetActive(false);
    }

    Button CreateDetailButton(Transform parent, string text, Color normalColor, Color pressedColor)
    {
        GameObject btnObj = new GameObject("Btn_" + text);
        btnObj.transform.SetParent(parent, false);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.2f);
        colors.pressedColor = pressedColor;
        btn.colors = colors;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = txtObj.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 14;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.fontStyle = FontStyles.Bold;
        btnText.raycastTarget = false;

        return btn;
    }

    /// <summary>
    /// Mobil detay panelini secili item ile guncelle
    /// </summary>
    void ShowMobileDetailForItem(ItemType type, InventorySlotSimpleUI slot)
    {
        // Onceki secili slotu temizle
        if (_selectedSlot != null)
        {
            ClearSlotHighlight(_selectedSlot);
        }

        _selectedItemType = type;
        _selectedSlot = slot;

        // Slotu highlight yap
        if (slot != null)
        {
            HighlightSlot(slot);
        }

        InventoryItem item = InventoryItem.Create(type);
        int count = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(type) : 0;

        _mobileDetailIcon.sprite = InventorySprites.CreateItemSprite(type);
        _mobileDetailName.text = item.name;
        _mobileDetailDesc.text = item.description + " (Adet: " + count + ")";

        _mobileDetailPanel.SetActive(true);
    }

    void HighlightSlot(InventorySlotSimpleUI slot)
    {
        if (slot != null && slot.background != null)
        {
            slot.background.color = new Color(0.3f, 0.35f, 0.5f, 0.95f);
        }
    }

    void ClearSlotHighlight(InventorySlotSimpleUI slot)
    {
        if (slot != null)
        {
            slot.Refresh(); // Orijinal rengi geri yukle
        }
    }

    void OnMobileDetailUse()
    {
        if (InventoryManager.Instance != null)
        {
            bool used = InventoryManager.Instance.UseItem(_selectedItemType);
            if (used)
            {
                ShowNotification(_selectedItemType, -1);
                // Detay panelini guncelle
                int newCount = InventoryManager.Instance.GetItemCount(_selectedItemType);
                if (newCount <= 0)
                {
                    _mobileDetailPanel.SetActive(false);
                }
                else
                {
                    ShowMobileDetailForItem(_selectedItemType, _selectedSlot);
                }
            }
        }
    }

    void OnMobileDetailEquip()
    {
        if (InventoryManager.Instance == null) return;

        // InventoryItemInstance uzerinden equip denemesi
        var items = InventoryManager.Instance.GetAllItemInstances();
        InventoryItemInstance targetItem = null;
        foreach (var item in items)
        {
            if (item.itemType == _selectedItemType)
            {
                targetItem = item;
                break;
            }
        }

        if (targetItem != null && EquipmentManager.Instance != null)
        {
            if (targetItem.isEquipped)
            {
                EquipmentManager.Instance.Unequip(targetItem.equippedSlot);
            }
            else
            {
                EquipmentManager.Instance.TryEquip(targetItem);
            }
        }
    }

    void OnMobileDetailDrop()
    {
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetAllItemInstances();
        InventoryItemInstance targetItem = null;
        foreach (var item in items)
        {
            if (item.itemType == _selectedItemType)
            {
                targetItem = item;
                break;
            }
        }

        if (targetItem != null)
        {
            InventoryManager.Instance.DropItem(targetItem);
            _mobileDetailPanel.SetActive(false);
            if (_selectedSlot != null) ClearSlotHighlight(_selectedSlot);
            _selectedSlot = null;
            RefreshInventorySlots();
        }
    }

    /// <summary>
    /// Mobilde panel sag ust kosesine X kapatma butonu ekler
    /// </summary>
    void CreateCloseButton(Transform titleParent)
    {
        _closeButton = new GameObject("CloseButton");
        _closeButton.transform.SetParent(titleParent, false);

        RectTransform rt = _closeButton.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-8, 0);
        rt.sizeDelta = new Vector2(50, 50);

        Image bg = _closeButton.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

        Button btn = _closeButton.AddComponent<Button>();
        btn.onClick.AddListener(CloseFullInventory);

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f);
        colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f);
        colors.pressedColor = new Color(0.6f, 0.15f, 0.15f);
        btn.colors = colors;

        // X yazisi
        GameObject xTextObj = new GameObject("XText");
        xTextObj.transform.SetParent(_closeButton.transform, false);
        RectTransform xRt = xTextObj.AddComponent<RectTransform>();
        xRt.anchorMin = Vector2.zero;
        xRt.anchorMax = Vector2.one;
        xRt.offsetMin = Vector2.zero;
        xRt.offsetMax = Vector2.zero;

        TextMeshProUGUI xText = xTextObj.AddComponent<TextMeshProUGUI>();
        xText.text = "X";
        xText.fontSize = 20;
        xText.alignment = TextAlignmentOptions.Center;
        xText.color = Color.white;
        xText.fontStyle = FontStyles.Bold;
        xText.raycastTarget = false;
    }

    // === MOBIL ENVANTER BUTONU ===

    /// <summary>
    /// Sol ust koseye kucuk canta/envanter butonu olusturur (sadece mobilde)
    /// </summary>
    void CreateMobileInventoryButton()
    {
        _mobileInventoryButton = new GameObject("MobileInventoryButton");
        _mobileInventoryButton.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = _mobileInventoryButton.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(15, -60);
        rt.sizeDelta = new Vector2(55, 55);

        Image bg = _mobileInventoryButton.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.18f, 0.25f, 0.85f);

        Button btn = _mobileInventoryButton.AddComponent<Button>();
        btn.onClick.AddListener(ToggleFullInventory);

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.18f, 0.25f);
        colors.highlightedColor = new Color(0.22f, 0.26f, 0.35f);
        colors.pressedColor = new Color(0.3f, 0.35f, 0.45f);
        btn.colors = colors;

        // 4 kare grid ikonu (procedural canta ikonu)
        CreateGridIcon(_mobileInventoryButton.transform);
    }

    /// <summary>
    /// 2x2 grid seklinde basit canta/envanter ikonu olusturur
    /// </summary>
    void CreateGridIcon(Transform parent)
    {
        float iconPadding = 10f;
        float squareSize = 9f;
        float gap = 3f;

        // Her kare icin pozisyonlar (2x2 grid, merkezde)
        Vector2[] positions = new Vector2[]
        {
            new Vector2(-squareSize / 2f - gap / 2f, squareSize / 2f + gap / 2f),   // sol ust
            new Vector2(squareSize / 2f + gap / 2f, squareSize / 2f + gap / 2f),     // sag ust
            new Vector2(-squareSize / 2f - gap / 2f, -squareSize / 2f - gap / 2f),   // sol alt
            new Vector2(squareSize / 2f + gap / 2f, -squareSize / 2f - gap / 2f),    // sag alt
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject sq = new GameObject("GridSquare_" + i);
            sq.transform.SetParent(parent, false);
            RectTransform sqRt = sq.AddComponent<RectTransform>();
            sqRt.anchorMin = new Vector2(0.5f, 0.5f);
            sqRt.anchorMax = new Vector2(0.5f, 0.5f);
            sqRt.pivot = new Vector2(0.5f, 0.5f);
            sqRt.anchoredPosition = positions[i];
            sqRt.sizeDelta = new Vector2(squareSize, squareSize);

            Image sqImg = sq.AddComponent<Image>();
            sqImg.color = new Color(0.65f, 0.7f, 0.8f, 0.9f);
            sqImg.raycastTarget = false;
        }
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

        // Isim
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slotObj.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0);
        nameRt.anchorMax = new Vector2(1, 0.28f);
        nameRt.offsetMin = new Vector2(3, 0);
        nameRt.offsetMax = new Vector2(-3, 0);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = itemInfo.name;
        nameText.fontSize = _isMobile ? 10 : 11;
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
        countText.fontSize = _isMobile ? 12 : 14;
        countText.alignment = TextAlignmentOptions.Center;
        countText.color = Color.white;
        countText.fontStyle = FontStyles.Bold;

        // Button
        Button btn = slotObj.AddComponent<Button>();
        ItemType capturedType = itemType;
        btn.onClick.AddListener(() => OnInventorySlotClicked(capturedType));

        ColorBlock slotColors = btn.colors;
        slotColors.normalColor = new Color(0.12f, 0.15f, 0.22f);
        slotColors.highlightedColor = new Color(0.2f, 0.25f, 0.35f);
        slotColors.pressedColor = new Color(0.25f, 0.3f, 0.4f);
        btn.colors = slotColors;

        // Slot UI component
        InventorySlotSimpleUI slotUI = slotObj.AddComponent<InventorySlotSimpleUI>();
        slotUI.itemType = itemType;
        slotUI.icon = icon;
        slotUI.countText = countText;
        slotUI.background = bg;

        // Hover / tap tooltip (sadece masaustunde tooltip, mobilde detay paneli buton ile aciliyor)
        if (!_isMobile)
        {
            EventTrigger trigger = slotObj.AddComponent<EventTrigger>();

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => ShowTooltipForItem(capturedType));
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        inventorySlots.Add(slotUI);
    }

    void OnInventorySlotClicked(ItemType type)
    {
        if (_isMobile)
        {
            // Mobilde: tiklaninca detay paneline bilgi yansit
            InventorySlotSimpleUI clickedSlot = inventorySlots.Find(s => s.itemType == type);
            ShowMobileDetailForItem(type, clickedSlot);
        }
        else
        {
            // Masaustunde: dogrudan kullan
            if (InventoryManager.Instance != null)
            {
                bool used = InventoryManager.Instance.UseItem(type);
                if (used)
                {
                    ShowNotification(type, -1); // -1 = kullanildi
                }
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

        // Cerceve
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

        // Baslik
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

        // Aciklama
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
        // Mobilde tooltip kullanma, detay paneli var
        if (_isMobile) return;
        if (tooltipPanel == null) return;

        InventoryItem item = InventoryItem.Create(type);
        int count = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(type) : 0;

        tooltipIcon.sprite = InventorySprites.CreateItemSprite(type);
        tooltipTitle.text = item.name;
        tooltipDesc.text = item.description + "\nAdet: " + count;

        tooltipPanel.SetActive(true);
        isTooltipVisible = true;

        // Masaustunde: mouse pozisyonuna takip
        StartCoroutine(UpdateTooltipPosition());
    }

    /// <summary>
    /// Mobilde tooltip'i son dokunulan slot'un ustune konumlar
    /// </summary>
    void PositionTooltipAboveSlot()
    {
        if (_lastTappedSlotRt == null) return;

        RectTransform tooltipRt = tooltipPanel.GetComponent<RectTransform>();

        // Slot'un dunya pozisyonunu al, tooltip'i ustune koy
        Vector3 slotWorldPos = _lastTappedSlotRt.position;
        float slotHeight = _lastTappedSlotRt.rect.height * _lastTappedSlotRt.lossyScale.y;
        tooltipRt.position = new Vector3(slotWorldPos.x, slotWorldPos.y + slotHeight / 2f + 5f, 0);

        // Ekran disina tasmamasi icin clamp
        Vector3[] corners = new Vector3[4];
        tooltipRt.GetWorldCorners(corners);
        float screenW = Screen.width;
        float screenH = Screen.height;

        // Sag tarafa tasiyor mu?
        if (corners[2].x > screenW)
        {
            float overflow = corners[2].x - screenW;
            tooltipRt.position -= new Vector3(overflow + 10f, 0, 0);
        }
        // Sol tarafa tasiyor mu?
        if (corners[0].x < 0)
        {
            float overflow = -corners[0].x;
            tooltipRt.position += new Vector3(overflow + 10f, 0, 0);
        }
        // Ust tarafa tasiyor mu? Altta goster
        if (corners[1].y > screenH)
        {
            tooltipRt.position = new Vector3(tooltipRt.position.x,
                slotWorldPos.y - slotHeight / 2f - tooltipRt.rect.height * tooltipRt.lossyScale.y - 5f, 0);
        }
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
        _lastTappedSlotRt = null;
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    // === AKTIF EFEKTLER ===

    void CreateActiveEffectsPanel()
    {
        activeEffectsPanel = new GameObject("ActiveEffectsPanel");
        activeEffectsPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = activeEffectsPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        // Mobilde envanter butonunun altinda yer birak
        float yOffset = _isMobile ? -120f : -margin - 50f;
        rt.anchoredPosition = new Vector2(margin, yOffset);

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

        // Aktif power-up'lari kontrol et
        ItemType[] timedItems = { ItemType.Shield, ItemType.SpeedBoost, ItemType.DoubleDamage, ItemType.Magnet };
        PowerUpType[] powerUpTypes = { PowerUpType.Shield, PowerUpType.SpeedBoost, PowerUpType.SpeedBoost, PowerUpType.Magnet };

        for (int i = 0; i < timedItems.Length; i++)
        {
            // PowerUpManager'dan kalan sureyi al
            float remaining = 0f;

            // Shield icin ozel kontrol
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
        // Mevcut efekt var mi?
        ActiveEffectUI existing = activeEffects.Find(e => e.itemType == type);
        if (existing != null)
        {
            existing.UpdateTime(remaining);
        }
        else
        {
            // Yeni efekt olustur
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

    // === BILDIRIMLER ===

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

    // === ENVANTER AC/KAPA ===

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

        // Scroll'u en basa al
        if (_inventoryScrollRect != null)
        {
            _inventoryScrollRect.verticalNormalizedPosition = 1f;
        }

        Time.timeScale = 0f; // Oyunu tamamen durdur
    }

    public void CloseFullInventory()
    {
        isInventoryOpen = false;
        fullInventoryPanel.SetActive(false);
        HideTooltip();

        // Mobil detay panelini de kapat
        if (_isMobile && _mobileDetailPanel != null)
        {
            _mobileDetailPanel.SetActive(false);
            if (_selectedSlot != null) ClearSlotHighlight(_selectedSlot);
            _selectedSlot = null;
        }

        Time.timeScale = 1f;
    }

    // === REFRESH ===

    void OnItemChanged(ItemType type, int newCount)
    {
        RefreshSlotForItem(type);
        RefreshInventorySlots();

        // Bildirim goster (toplama)
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

        // Esya yoksa soluk goster
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
