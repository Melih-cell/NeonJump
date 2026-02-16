using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// Gelismis envanter slot UI elementi.
/// Drag & drop, hover efektleri ve rarity gosterimi destekler.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Referanslari")]
    public Image backgroundImage;
    public Image itemIcon;
    public Image rarityGlow;
    public Image selectionHighlight;
    public TMP_Text stackText;
    public TMP_Text rarityIndicator;

    [Header("Ekipman Gostergesi")]
    public GameObject equippedBadge;

    [Header("Slot Ayarlari")]
    public int slotIndex = -1;
    public bool isEquipmentSlot = false;
    public EquipmentSlot equipmentSlotType = EquipmentSlot.None;

    [Header("Animasyon")]
    public float hoverScaleMultiplier = 1.1f;
    public float hoverAnimationSpeed = 10f;
    public float rarityPulseSpeed = 2f;

    [Header("Renkler")]
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color selectedColor = new Color(1f, 1f, 0f, 0.5f);
    public Color hoverColor = new Color(0f, 1f, 1f, 0.3f);

    // Item verisi
    private InventoryItemInstance currentItem;
    private bool isHovered = false;
    private bool isSelected = false;
    private bool isDragging = false;
    private Vector3 originalScale;
    private Vector3 targetScale;

    // Drag icin
    private static InventorySlotUI dragSourceSlot;
    private static GameObject dragIcon;
    private Canvas parentCanvas;

    // Mobil platform flag
    private bool _isMobile = false;

    // Events
    public static event Action<InventorySlotUI> OnSlotClicked;
    public static event Action<InventorySlotUI> OnSlotRightClicked;
    public static event Action<InventorySlotUI> OnSlotHoverEnter;
    public static event Action<InventorySlotUI> OnSlotHoverExit;
    public static event Action<InventorySlotUI, InventorySlotUI> OnSlotSwapped;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        parentCanvas = GetComponentInParent<Canvas>();
        _isMobile = Application.isMobilePlatform ||
            (MobileControls.Instance != null && MobileControls.Instance.IsEnabled);
    }

    void Start()
    {
        InitializeSlot();
    }

    void Update()
    {
        // Scale animasyonu
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * hoverAnimationSpeed);

        // Rarity pulse efekti - hover sirasinda 3x hiz, yuksek alfa
        if (currentItem != null && currentItem.rarity >= ItemRarity.Rare && rarityGlow != null)
        {
            float speed = isHovered ? rarityPulseSpeed * 3f : rarityPulseSpeed;
            float minAlpha = isHovered ? 0.5f : 0.3f;
            float maxAlpha = isHovered ? 0.9f : 0.7f;
            float pulse = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            var color = rarityGlow.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            rarityGlow.color = color;
        }
    }

    void InitializeSlot()
    {
        // Bos slot gorunumu
        SetEmpty();

        // Highlight'lari gizle
        if (selectionHighlight != null)
            selectionHighlight.enabled = false;
    }

    /// <summary>
    /// Slot'a item ata
    /// </summary>
    public void SetItem(InventoryItemInstance item)
    {
        currentItem = item;

        if (item == null)
        {
            SetEmpty();
            return;
        }

        // Icon
        if (itemIcon != null)
        {
            itemIcon.enabled = true;
            itemIcon.sprite = GetItemSprite(item.itemType);
            itemIcon.color = Color.white;
        }

        // Background
        if (backgroundImage != null)
        {
            backgroundImage.color = filledSlotColor;
        }

        // Stack sayisi
        if (stackText != null)
        {
            if (item.stackCount > 1)
            {
                stackText.enabled = true;
                stackText.text = item.stackCount.ToString();
            }
            else
            {
                stackText.enabled = false;
            }
        }

        // Rarity glow
        if (rarityGlow != null)
        {
            rarityGlow.enabled = item.rarity >= ItemRarity.Uncommon;
            rarityGlow.color = ItemRarityHelper.GetGlowColor(item.rarity);
        }

        // Rarity indicator (kose)
        if (rarityIndicator != null)
        {
            rarityIndicator.enabled = item.rarity >= ItemRarity.Uncommon;
            rarityIndicator.text = GetRaritySymbol(item.rarity);
            rarityIndicator.color = ItemRarityHelper.GetRarityColor(item.rarity);
        }

        // Equipped badge
        if (equippedBadge != null)
        {
            equippedBadge.SetActive(item.isEquipped);
        }
    }

    /// <summary>
    /// Slot'u bosalt
    /// </summary>
    public void SetEmpty()
    {
        currentItem = null;

        if (itemIcon != null)
        {
            itemIcon.enabled = false;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = emptySlotColor;
        }

        if (stackText != null)
        {
            stackText.enabled = false;
        }

        if (rarityGlow != null)
        {
            rarityGlow.enabled = false;
        }

        if (rarityIndicator != null)
        {
            rarityIndicator.enabled = false;
        }

        if (equippedBadge != null)
        {
            equippedBadge.SetActive(false);
        }
    }

    /// <summary>
    /// Slot secili mi ayarla
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = selected;
            selectionHighlight.color = selectedColor;
        }
    }

    /// <summary>
    /// Mevcut item'i al
    /// </summary>
    public InventoryItemInstance GetItem()
    {
        return currentItem;
    }

    /// <summary>
    /// Slot dolu mu?
    /// </summary>
    public bool HasItem()
    {
        return currentItem != null;
    }

    Sprite GetItemSprite(ItemType type)
    {
        if (InventorySprites.Instance != null)
        {
            return InventorySprites.Instance.GetSprite(type);
        }
        return null;
    }

    string GetRaritySymbol(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => "★",
            ItemRarity.Rare => "★★",
            ItemRarity.Epic => "★★★",
            ItemRarity.Legendary => "★★★★",
            _ => ""
        };
    }

    // === POINTER EVENTS ===

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScaleMultiplier;

        if (backgroundImage != null && currentItem != null)
        {
            // Rarity renginde hover glow (jenerik cyan yerine)
            Color rarityHover = ItemRarityHelper.GetRarityColor(currentItem.rarity);
            rarityHover.a = 0.4f;
            backgroundImage.color = Color.Lerp(filledSlotColor, rarityHover, 0.6f);
        }

        OnSlotHoverEnter?.Invoke(this);

        // Drag sirasinda hedef vurgulama
        if (dragSourceSlot != null && dragSourceSlot != this)
        {
            var sourceItem = dragSourceSlot.GetItem();
            if (sourceItem != null)
            {
                if (currentItem != null && sourceItem.CanStack(currentItem))
                {
                    // Yesil - stack birlesme
                    backgroundImage.color = new Color(0f, 1f, 0f, 0.4f);
                }
                else if (currentItem != null)
                {
                    // Sari - swap
                    backgroundImage.color = new Color(1f, 1f, 0f, 0.3f);
                }
                else if (isEquipmentSlot && (sourceItem == null || !sourceItem.IsEquippable()))
                {
                    // Kirmizi - gecersiz
                    backgroundImage.color = new Color(1f, 0f, 0f, 0.3f);
                }
                else
                {
                    // Yesil - tasima
                    backgroundImage.color = new Color(0f, 1f, 0f, 0.3f);
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = originalScale;

        if (backgroundImage != null)
        {
            backgroundImage.color = currentItem != null ? filledSlotColor : emptySlotColor;
        }

        OnSlotHoverExit?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSlotClicked?.Invoke(this);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButton();

            // Mobilde secili slotu highlight yap
            if (_isMobile)
            {
                SetSelected(true);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnSlotRightClicked?.Invoke(this);
        }
    }

    // === DRAG & DROP ===

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isMobile) return; // Mobilde drag&drop devre disi
        if (currentItem == null) return;

        isDragging = true;
        dragSourceSlot = this;

        // Drag icon olustur
        CreateDragIcon();

        // Orijinal icon'u saydam yap
        if (itemIcon != null)
        {
            var color = itemIcon.color;
            color.a = 0.5f;
            itemIcon.color = color;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isMobile) return;
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isMobile) return;
        isDragging = false;

        // Drag icon'u yok et
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        // Orijinal icon'u normale dondur
        if (itemIcon != null)
        {
            itemIcon.color = Color.white;
        }

        dragSourceSlot = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_isMobile) return;
        if (dragSourceSlot == null || dragSourceSlot == this) return;

        // Gecerlilik kontrolu ve gorsel geri bildirim
        var sourceItem = dragSourceSlot.GetItem();
        if (sourceItem != null && currentItem != null && sourceItem.CanStack(currentItem))
        {
            // Yesil flash - stack birlesme
            StartCoroutine(SlotFlash(new Color(0f, 1f, 0f, 0.5f)));
        }
        else if (sourceItem != null && currentItem != null)
        {
            // Sari flash - swap
            StartCoroutine(SlotFlash(new Color(1f, 1f, 0f, 0.5f)));
        }
        else
        {
            // Yesil flash - tasima
            StartCoroutine(SlotFlash(new Color(0f, 1f, 0f, 0.3f)));
        }

        // Slot swap
        OnSlotSwapped?.Invoke(dragSourceSlot, this);
    }

    private System.Collections.IEnumerator SlotFlash(Color flashColor)
    {
        if (backgroundImage == null) yield break;
        Color original = backgroundImage.color;
        backgroundImage.color = flashColor;
        yield return new WaitForSecondsRealtime(0.15f);
        backgroundImage.color = original;
    }

    void CreateDragIcon()
    {
        if (currentItem == null || parentCanvas == null) return;

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(parentCanvas.transform, false);

        var rectTransform = dragIcon.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(50, 50);

        var image = dragIcon.AddComponent<Image>();
        image.sprite = GetItemSprite(currentItem.itemType);
        image.raycastTarget = false;

        // Rarity rengi ile kenar (Neon glow efekti)
        Color rarityColor = ItemRarityHelper.GetRarityColor(currentItem.rarity);
        var outline = dragIcon.AddComponent<Outline>();
        outline.effectColor = rarityColor;
        outline.effectDistance = new Vector2(3, 3);

        // Ikinci outline katmani - daha buyuk glow
        var outerGlow = dragIcon.AddComponent<Outline>();
        outerGlow.effectColor = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
        outerGlow.effectDistance = new Vector2(5, 5);

        // Canvas'in en ustunde
        var canvas = dragIcon.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
    }

    /// <summary>
    /// Slot index'ini ayarla
    /// </summary>
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// Ekipman slot'u olarak ayarla
    /// </summary>
    public void SetAsEquipmentSlot(EquipmentSlot slot)
    {
        isEquipmentSlot = true;
        equipmentSlotType = slot;
    }
}
