using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Ana envanter UI kontrolcusu.
/// Slot grid'i, tab'lar, filtreleme ve siralama islerini yonetir.
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    [Header("Panel")]
    public GameObject inventoryPanel;
    public CanvasGroup canvasGroup;

    [Header("Grid Ayarlari")]
    public Transform slotContainer;
    public GameObject slotPrefab;
    public int gridColumns = 6;
    public int gridRows = 5;

    [Header("Ekipman Slotlari")]
    public Transform equipmentSlotContainer;
    public InventorySlotUI weaponMod1Slot;
    public InventorySlotUI weaponMod2Slot;
    public InventorySlotUI accessory1Slot;
    public InventorySlotUI accessory2Slot;
    public InventorySlotUI armorSlot;

    [Header("Tab Butonlari")]
    public Button allTab;
    public Button consumablesTab;
    public Button equipmentTab;
    public Button materialsTab;
    public Button setsTab;

    [Header("Siralama ve Filtreleme")]
    public TMP_Dropdown sortDropdown;
    public TMP_InputField searchInput;

    [Header("Alt Bilgi")]
    public TMP_Text slotCountText;
    public TMP_Text totalValueText;
    public TMP_Text weightText;

    [Header("Detay Paneli")]
    public ItemDetailPanel detailPanel;

    [Header("Quick Slot Bar")]
    public Transform quickSlotContainer;
    public List<InventorySlotUI> quickSlots = new List<InventorySlotUI>();

    [Header("Tooltip")]
    private GameObject tooltipPanel;
    private TMP_Text tooltipNameText;
    private TMP_Text tooltipDescText;
    private TMP_Text tooltipStatsText;
    private TMP_Text tooltipValueText;
    private Image tooltipRarityBar;

    [Header("Animasyon")]
    public float openSpeed = 8f;

    // State
    private List<InventorySlotUI> inventorySlots = new List<InventorySlotUI>();
    private List<InventorySlotUI> equipmentSlots = new List<InventorySlotUI>();
    private ItemCategory currentFilter = ItemCategory.Consumable; // Default: All (-1 would be all)
    private bool showAll = true;
    private SortType currentSort = SortType.Recent;
    private string searchQuery = "";
    private bool isOpen = false;
    private InventorySlotUI selectedSlot;

    // Events
    public static event Action OnInventoryOpened;
    public static event Action OnInventoryClosed;

    public enum SortType
    {
        Recent,
        Name,
        Rarity,
        Type,
        Amount
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Initialize();
        SubscribeToEvents();

        // Baslangicta gizle
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        // Tab veya I tusu ile ac/kapat
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            Toggle();
        }

        // ESC ile kapat
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            Close();
        }
    }

    void Initialize()
    {
        // Slot grid'i olustur
        CreateSlotGrid();

        // Ekipman slotlarini ayarla
        SetupEquipmentSlots();

        // Tab butonlarini ayarla
        SetupTabs();

        // Siralama dropdown'ini ayarla
        SetupSortDropdown();

        // Arama input'unu ayarla
        SetupSearchInput();

        // Tooltip
        SetupTooltip();

        // Neon scanline overlay
        CreateScanlineOverlay();
    }

    void CreateSlotGrid()
    {
        if (slotContainer == null || slotPrefab == null) return;

        // Mevcut slotlari temizle
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        // Yeni slotlar olustur
        int totalSlots = gridColumns * gridRows;

        for (int i = 0; i < totalSlots; i++)
        {
            var slotGo = Instantiate(slotPrefab, slotContainer);
            var slot = slotGo.GetComponent<InventorySlotUI>();

            if (slot == null)
                slot = slotGo.AddComponent<InventorySlotUI>();

            slot.SetSlotIndex(i);
            inventorySlots.Add(slot);
        }

        Debug.Log($"[InventoryUI] {totalSlots} slot olusturuldu");
    }

    void SetupEquipmentSlots()
    {
        equipmentSlots.Clear();

        if (weaponMod1Slot != null)
        {
            weaponMod1Slot.SetAsEquipmentSlot(EquipmentSlot.WeaponMod1);
            equipmentSlots.Add(weaponMod1Slot);
        }

        if (weaponMod2Slot != null)
        {
            weaponMod2Slot.SetAsEquipmentSlot(EquipmentSlot.WeaponMod2);
            equipmentSlots.Add(weaponMod2Slot);
        }

        if (accessory1Slot != null)
        {
            accessory1Slot.SetAsEquipmentSlot(EquipmentSlot.Accessory1);
            equipmentSlots.Add(accessory1Slot);
        }

        if (accessory2Slot != null)
        {
            accessory2Slot.SetAsEquipmentSlot(EquipmentSlot.Accessory2);
            equipmentSlots.Add(accessory2Slot);
        }

        if (armorSlot != null)
        {
            armorSlot.SetAsEquipmentSlot(EquipmentSlot.Armor);
            equipmentSlots.Add(armorSlot);
        }
    }

    void SetupTabs()
    {
        if (allTab != null)
            allTab.onClick.AddListener(() => SetFilter(ItemCategory.Consumable, true));

        if (consumablesTab != null)
            consumablesTab.onClick.AddListener(() => SetFilter(ItemCategory.Consumable, false));

        if (equipmentTab != null)
            equipmentTab.onClick.AddListener(() => SetFilter(ItemCategory.Equipment, false));

        if (materialsTab != null)
            materialsTab.onClick.AddListener(() => SetFilter(ItemCategory.Material, false));

        if (setsTab != null)
            setsTab.onClick.AddListener(() => SetFilter(ItemCategory.SetPiece, false));
    }

    void SetupSortDropdown()
    {
        if (sortDropdown == null) return;

        sortDropdown.ClearOptions();
        sortDropdown.AddOptions(new List<string>
        {
            "En Yeni",
            "İsim",
            "Nadirlik",
            "Tür",
            "Miktar"
        });

        sortDropdown.onValueChanged.AddListener(index =>
        {
            currentSort = (SortType)index;
            RefreshInventory();
        });
    }

    void SetupSearchInput()
    {
        if (searchInput == null) return;

        searchInput.onValueChanged.AddListener(query =>
        {
            searchQuery = query.ToLower();
            RefreshInventory();
        });
    }

    void SetupTooltip()
    {
        // Tooltip panel olustur
        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(transform, false);

        RectTransform tooltipRt = tooltipPanel.AddComponent<RectTransform>();
        tooltipRt.sizeDelta = new Vector2(250, 180);
        tooltipRt.pivot = new Vector2(0, 1);

        // Background
        Image tooltipBg = tooltipPanel.AddComponent<Image>();
        tooltipBg.color = new Color(0.08f, 0.05f, 0.12f, 0.95f);
        tooltipBg.raycastTarget = false;

        Outline tooltipOutline = tooltipPanel.AddComponent<Outline>();
        tooltipOutline.effectColor = new Color(0f, 1f, 1f, 0.5f);
        tooltipOutline.effectDistance = new Vector2(2, 2);

        CanvasGroup tooltipCg = tooltipPanel.AddComponent<CanvasGroup>();
        tooltipCg.blocksRaycasts = false;

        // Rarity color bar (ust kenar)
        GameObject rarityBarObj = new GameObject("RarityBar");
        rarityBarObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform rarityRt = rarityBarObj.AddComponent<RectTransform>();
        rarityRt.anchorMin = new Vector2(0, 1);
        rarityRt.anchorMax = new Vector2(1, 1);
        rarityRt.pivot = new Vector2(0.5f, 1);
        rarityRt.anchoredPosition = Vector2.zero;
        rarityRt.sizeDelta = new Vector2(0, 4);
        tooltipRarityBar = rarityBarObj.AddComponent<Image>();
        tooltipRarityBar.color = Color.white;
        tooltipRarityBar.raycastTarget = false;

        // Item name
        GameObject nameObj = new GameObject("ItemName");
        nameObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0, 1);
        nameRt.anchoredPosition = new Vector2(10, -10);
        nameRt.sizeDelta = new Vector2(-20, 30);
        tooltipNameText = nameObj.AddComponent<TextMeshProUGUI>();
        tooltipNameText.fontSize = 18;
        tooltipNameText.fontStyle = FontStyles.Bold;
        tooltipNameText.color = Color.white;
        tooltipNameText.raycastTarget = false;

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0, 1);
        descRt.anchorMax = new Vector2(1, 1);
        descRt.pivot = new Vector2(0, 1);
        descRt.anchoredPosition = new Vector2(10, -40);
        descRt.sizeDelta = new Vector2(-20, 30);
        tooltipDescText = descObj.AddComponent<TextMeshProUGUI>();
        tooltipDescText.fontSize = 13;
        tooltipDescText.color = new Color(0.7f, 0.7f, 0.7f);
        tooltipDescText.raycastTarget = false;

        // Stats
        GameObject statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform statsRt = statsObj.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0, 1);
        statsRt.anchorMax = new Vector2(1, 1);
        statsRt.pivot = new Vector2(0, 1);
        statsRt.anchoredPosition = new Vector2(10, -72);
        statsRt.sizeDelta = new Vector2(-20, 70);
        tooltipStatsText = statsObj.AddComponent<TextMeshProUGUI>();
        tooltipStatsText.fontSize = 13;
        tooltipStatsText.color = new Color(0.5f, 1f, 0.5f);
        tooltipStatsText.raycastTarget = false;

        // Sell value
        GameObject valueObj = new GameObject("SellValue");
        valueObj.transform.SetParent(tooltipPanel.transform, false);
        RectTransform valueRt = valueObj.AddComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0, 0);
        valueRt.anchorMax = new Vector2(1, 0);
        valueRt.pivot = new Vector2(0, 0);
        valueRt.anchoredPosition = new Vector2(10, 5);
        valueRt.sizeDelta = new Vector2(-20, 25);
        tooltipValueText = valueObj.AddComponent<TextMeshProUGUI>();
        tooltipValueText.fontSize = 14;
        tooltipValueText.color = new Color(1f, 0.85f, 0f);
        tooltipValueText.raycastTarget = false;

        tooltipPanel.SetActive(false);
    }

    void SubscribeToEvents()
    {
        InventorySlotUI.OnSlotClicked += OnSlotClicked;
        InventorySlotUI.OnSlotRightClicked += OnSlotRightClicked;
        InventorySlotUI.OnSlotHoverEnter += OnSlotHoverEnter;
        InventorySlotUI.OnSlotHoverExit += OnSlotHoverExit;
        InventorySlotUI.OnSlotSwapped += OnSlotSwapped;

        if (InventoryManager.Instance != null)
        {
            // InventoryManager event'lerine baglan
        }

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnItemEquipped += OnItemEquipped;
            EquipmentManager.Instance.OnItemUnequipped += OnItemUnequipped;
        }
    }

    void UnsubscribeFromEvents()
    {
        InventorySlotUI.OnSlotClicked -= OnSlotClicked;
        InventorySlotUI.OnSlotRightClicked -= OnSlotRightClicked;
        InventorySlotUI.OnSlotHoverEnter -= OnSlotHoverEnter;
        InventorySlotUI.OnSlotHoverExit -= OnSlotHoverExit;
        InventorySlotUI.OnSlotSwapped -= OnSlotSwapped;

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnItemEquipped -= OnItemEquipped;
            EquipmentManager.Instance.OnItemUnequipped -= OnItemUnequipped;
        }
    }

    // === PUBLIC METODLAR ===

    /// <summary>
    /// Envanteri ac/kapat
    /// </summary>
    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    /// <summary>
    /// Envanteri ac
    /// </summary>
    public void Open()
    {
        isOpen = true;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        RefreshInventory();
        RefreshEquipmentSlots();
        UpdateBottomInfo();

        // Panel acilis animasyonu
        RectTransform panelRect = inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null;
        if (panelRect != null && UIAnimator.Instance != null)
        {
            panelRect.localScale = Vector3.one * 0.85f;
            UIAnimator.Instance.ScaleTo(panelRect, Vector3.one, 0.25f);
        }
        if (canvasGroup != null && UIAnimator.Instance != null)
        {
            canvasGroup.alpha = 0f;
            UIAnimator.Instance.FadeIn(canvasGroup, 0.2f);
        }

        // Kademeli slot gorunumu
        StartCoroutine(StaggeredSlotAppear());

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        OnInventoryOpened?.Invoke();

        Debug.Log("[InventoryUI] Envanter acildi");
    }

    /// <summary>
    /// Envanteri kapat
    /// </summary>
    public void Close()
    {
        isOpen = false;

        if (detailPanel != null)
            detailPanel.Hide();

        // Kapanis animasyonu
        RectTransform panelRect = inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null;
        if (panelRect != null && UIAnimator.Instance != null)
        {
            UIAnimator.Instance.ScaleTo(panelRect, Vector3.one * 0.85f, 0.2f);
        }
        if (canvasGroup != null && UIAnimator.Instance != null)
        {
            UIAnimator.Instance.FadeOut(canvasGroup, 0.2f);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        OnInventoryClosed?.Invoke();

        // Panel'i gizle (animasyon bittikten sonra)
        Invoke(nameof(HidePanel), 0.3f);

        Debug.Log("[InventoryUI] Envanter kapatildi");
    }

    void HidePanel()
    {
        if (!isOpen && inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    /// <summary>
    /// Envanteri yenile
    /// </summary>
    public void RefreshInventory()
    {
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetAllItemInstances();

        // Filtrele
        var filteredItems = FilterItems(items);

        // Sirala
        var sortedItems = SortItems(filteredItems);

        // Slotlara yerle stir
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < sortedItems.Count)
            {
                inventorySlots[i].SetItem(sortedItems[i]);
            }
            else
            {
                inventorySlots[i].SetEmpty();
            }
        }

        UpdateBottomInfo();
    }

    /// <summary>
    /// Ekipman slotlarini yenile
    /// </summary>
    public void RefreshEquipmentSlots()
    {
        if (EquipmentManager.Instance == null) return;

        foreach (var slot in equipmentSlots)
        {
            var equippedItem = EquipmentManager.Instance.GetEquippedItem(slot.equipmentSlotType);
            slot.SetItem(equippedItem);
        }
    }

    /// <summary>
    /// Filtre ayarla
    /// </summary>
    public void SetFilter(ItemCategory category, bool all = false)
    {
        showAll = all;
        currentFilter = category;
        RefreshInventory();

        // Tab butonlarini guncelle
        UpdateTabButtons();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    List<InventoryItemInstance> FilterItems(List<InventoryItemInstance> items)
    {
        var result = new List<InventoryItemInstance>();

        foreach (var item in items)
        {
            // Arama filtresi
            if (!string.IsNullOrEmpty(searchQuery))
            {
                string itemName = item.GetDisplayName().ToLower();
                if (!itemName.Contains(searchQuery))
                    continue;
            }

            // Kategori filtresi
            if (!showAll)
            {
                var data = item.GetData();
                if (data.category != currentFilter)
                    continue;
            }

            result.Add(item);
        }

        return result;
    }

    List<InventoryItemInstance> SortItems(List<InventoryItemInstance> items)
    {
        switch (currentSort)
        {
            case SortType.Recent:
                items.Sort((a, b) => b.createdTimestamp.CompareTo(a.createdTimestamp));
                break;

            case SortType.Name:
                items.Sort((a, b) => a.GetDisplayName().CompareTo(b.GetDisplayName()));
                break;

            case SortType.Rarity:
                items.Sort((a, b) => b.rarity.CompareTo(a.rarity));
                break;

            case SortType.Type:
                items.Sort((a, b) => a.itemType.CompareTo(b.itemType));
                break;

            case SortType.Amount:
                items.Sort((a, b) => b.stackCount.CompareTo(a.stackCount));
                break;
        }

        return items;
    }

    void UpdateTabButtons()
    {
        // Tab butonlarinin gorunumunu guncelle
        Color activeColor = new Color(0f, 1f, 1f);
        Color inactiveColor = Color.white;

        if (allTab != null)
        {
            var text = allTab.GetComponentInChildren<TMP_Text>();
            if (text != null) text.color = showAll ? activeColor : inactiveColor;
        }

        if (consumablesTab != null)
        {
            var text = consumablesTab.GetComponentInChildren<TMP_Text>();
            if (text != null) text.color = (!showAll && currentFilter == ItemCategory.Consumable) ? activeColor : inactiveColor;
        }

        if (equipmentTab != null)
        {
            var text = equipmentTab.GetComponentInChildren<TMP_Text>();
            if (text != null) text.color = (!showAll && currentFilter == ItemCategory.Equipment) ? activeColor : inactiveColor;
        }

        if (materialsTab != null)
        {
            var text = materialsTab.GetComponentInChildren<TMP_Text>();
            if (text != null) text.color = (!showAll && currentFilter == ItemCategory.Material) ? activeColor : inactiveColor;
        }

        if (setsTab != null)
        {
            var text = setsTab.GetComponentInChildren<TMP_Text>();
            if (text != null) text.color = (!showAll && currentFilter == ItemCategory.SetPiece) ? activeColor : inactiveColor;
        }
    }

    void UpdateBottomInfo()
    {
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetAllItemInstances();

        // Slot sayisi
        if (slotCountText != null)
        {
            int used = items.Count;
            int total = gridColumns * gridRows;
            slotCountText.text = $"{used}/{total}";
        }

        // Toplam deger
        if (totalValueText != null)
        {
            int totalValue = 0;
            foreach (var item in items)
            {
                totalValue += item.GetSellValue();
            }
            totalValueText.text = totalValue.ToString("N0");
        }
    }

    // === SLOT EVENTS ===

    void OnSlotClicked(InventorySlotUI slot)
    {
        // Onceki secimi kaldir
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;
        slot.SetSelected(true);

        // Detay panelini goster
        if (detailPanel != null && slot.HasItem())
        {
            detailPanel.ShowItem(slot.GetItem());
        }
        else if (detailPanel != null)
        {
            detailPanel.Hide();
        }
    }

    void OnSlotRightClicked(InventorySlotUI slot)
    {
        if (!slot.HasItem()) return;

        var item = slot.GetItem();

        // Hizli aksiyon: Consumable ise kullan, Equipment ise ekiple/cikar
        if (item.IsConsumable())
        {
            InventoryManager.Instance?.UseItem(item);
            RefreshInventory();
        }
        else if (item.IsEquippable())
        {
            if (item.isEquipped)
            {
                EquipmentManager.Instance?.Unequip(item.equippedSlot);
            }
            else
            {
                EquipmentManager.Instance?.TryEquip(item);
            }
            RefreshInventory();
            RefreshEquipmentSlots();
        }
    }

    void OnSlotHoverEnter(InventorySlotUI slot)
    {
        if (tooltipPanel == null || !slot.HasItem()) return;

        var item = slot.GetItem();
        var data = item.GetData();

        // Rarity rengi
        Color rarityColor = ItemRarityHelper.GetRarityColor(item.rarity);
        tooltipRarityBar.color = rarityColor;

        // Isim (rarity renginde)
        tooltipNameText.text = item.GetDisplayName();
        tooltipNameText.color = rarityColor;

        // Aciklama
        tooltipDescText.text = data.description;

        // Statlar
        string stats = "";
        if (data.damageBonus > 0) stats += $"Hasar: +{item.GetEffectiveDamageBonus() * 100:F0}%\n";
        if (data.speedBonus > 0) stats += $"Hiz: +{item.GetEffectiveSpeedBonus() * 100:F0}%\n";
        if (data.defenseBonus > 0) stats += $"Savunma: +{item.GetEffectiveDefenseBonus() * 100:F0}%\n";
        if (data.critBonus > 0) stats += $"Kritik: +{item.GetEffectiveCritBonus() * 100:F0}%\n";
        if (data.dropRateBonus > 0) stats += $"Sans: +{item.GetEffectiveDropRateBonus() * 100:F0}%\n";
        tooltipStatsText.text = stats;

        // Satis degeri
        tooltipValueText.text = $"Satis: {item.GetSellValue()} coin";

        // Pozisyon - mouse pozisyonuna gore
        tooltipPanel.SetActive(true);
        RectTransform tooltipRt = tooltipPanel.GetComponent<RectTransform>();
        tooltipRt.position = Input.mousePosition + new Vector3(20, -20, 0);
    }

    void OnSlotHoverExit(InventorySlotUI slot)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void OnSlotSwapped(InventorySlotUI source, InventorySlotUI target)
    {
        if (InventoryManager.Instance == null) return;

        var sourceItem = source.GetItem();
        var targetItem = target.GetItem();

        // Ekipman slotuna surukleme
        if (target.isEquipmentSlot && sourceItem != null && sourceItem.IsEquippable())
        {
            EquipmentManager.Instance?.TryEquip(sourceItem);
            RefreshInventory();
            RefreshEquipmentSlots();
            return;
        }

        // Normal slot swap
        if (sourceItem != null && targetItem != null)
        {
            // Stack birles tirme kontrolu
            if (sourceItem.CanStack(targetItem))
            {
                int remaining = targetItem.AddToStack(sourceItem.stackCount);
                sourceItem.stackCount = remaining;

                if (sourceItem.stackCount <= 0)
                {
                    InventoryManager.Instance.RemoveItem(sourceItem);
                }
            }
            else
            {
                // Slot indexlerini degistir
                int tempIndex = sourceItem.slotIndex;
                sourceItem.slotIndex = targetItem.slotIndex;
                targetItem.slotIndex = tempIndex;
            }
        }
        else if (sourceItem != null)
        {
            sourceItem.slotIndex = target.slotIndex;
        }

        RefreshInventory();
    }

    void OnItemEquipped(EquipmentSlot slot, InventoryItemInstance item)
    {
        RefreshInventory();
        RefreshEquipmentSlots();
    }

    void OnItemUnequipped(EquipmentSlot slot, InventoryItemInstance item)
    {
        RefreshInventory();
        RefreshEquipmentSlots();
    }

    /// <summary>
    /// Envanter acik mi?
    /// </summary>
    public bool IsOpen()
    {
        return isOpen;
    }

    /// <summary>
    /// Kademeli slot gorunum animasyonu - slotlar sirayla belirir
    /// </summary>
    private System.Collections.IEnumerator StaggeredSlotAppear()
    {
        // Tum slotlari scale=0 yap
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                slot.transform.localScale = Vector3.zero;
        }

        yield return null; // Bir frame bekle

        // Sirayla PopIn
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                int index = row * gridColumns + col;
                if (index < inventorySlots.Count && inventorySlots[index] != null)
                {
                    RectTransform slotRect = inventorySlots[index].GetComponent<RectTransform>();
                    if (slotRect != null && UIAnimator.Instance != null)
                    {
                        UIAnimator.Instance.PopIn(slotRect, 0.15f);
                    }
                    else
                    {
                        inventorySlots[index].transform.localScale = Vector3.one;
                    }
                }
                yield return new WaitForSecondsRealtime(0.015f);
            }
            yield return new WaitForSecondsRealtime(0.04f);
        }
    }

    /// <summary>
    /// Neon scanline overlay olusturur - cyberpunk/holografik his
    /// </summary>
    private void CreateScanlineOverlay()
    {
        if (inventoryPanel == null) return;

        GameObject scanlineObj = new GameObject("ScanlineOverlay");
        scanlineObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform rt = scanlineObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image scanlineImage = scanlineObj.AddComponent<Image>();
        scanlineImage.raycastTarget = false;

        // Procedural scanline texture (her 4. piksel cyan %6 alfa)
        int texWidth = 4;
        int texHeight = 64;
        Texture2D scanTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        Color scanColor = new Color(0f, 1f, 1f, 0.06f);
        Color clear = Color.clear;

        for (int y = 0; y < texHeight; y++)
        {
            Color lineColor = (y % 4 == 0) ? scanColor : clear;
            for (int x = 0; x < texWidth; x++)
            {
                scanTex.SetPixel(x, y, lineColor);
            }
        }

        scanTex.filterMode = FilterMode.Point;
        scanTex.wrapMode = TextureWrapMode.Repeat;
        scanTex.Apply();

        scanlineImage.sprite = Sprite.Create(scanTex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f));
        scanlineImage.type = Image.Type.Tiled;

        // En uste tasi
        scanlineObj.transform.SetAsLastSibling();
    }
}
