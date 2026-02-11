using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Item detay paneli.
/// Secili item'in detayli bilgilerini gosterir.
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;

    [Header("Item Bilgisi")]
    public Image itemIcon;
    public Image rarityBackground;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text rarityText;
    public TMP_Text categoryText;

    [Header("Stat Listesi")]
    public Transform statListContainer;
    public GameObject statEntryPrefab;

    [Header("Set Bilgisi")]
    public GameObject setInfoSection;
    public TMP_Text setNameText;
    public Transform setBonusListContainer;
    public GameObject setBonusPrefab;

    [Header("Aksiyon Butonlari")]
    public Button useButton;
    public TMP_Text useButtonText;
    public Button equipButton;
    public TMP_Text equipButtonText;
    public Button dropButton;
    public Button sellButton;
    public Button dismantleButton;
    public TMP_Text dismantleText;
    public TMP_Text sellValueText;

    [Header("Animasyon")]
    public float fadeSpeed = 5f;

    // Current item
    private InventoryItemInstance currentItem;
    private List<GameObject> spawnedStatEntries = new List<GameObject>();
    private List<GameObject> spawnedBonusEntries = new List<GameObject>();
    private bool isVisible = false;

    void Start()
    {
        SetupListeners();
        Hide();
    }

    void Update()
    {
        // Fade animasyonu
        if (canvasGroup != null)
        {
            float targetAlpha = isVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);

            if (!isVisible && canvasGroup.alpha < 0.01f && panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }

    void SetupListeners()
    {
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseClicked);
            AddButtonAnimation(useButton);
        }

        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipClicked);
            AddButtonAnimation(equipButton);
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(OnDropClicked);
            AddButtonAnimation(dropButton);
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellClicked);
            AddButtonAnimation(sellButton);
        }

        if (dismantleButton != null)
        {
            dismantleButton.onClick.AddListener(OnDismantleClicked);
            AddButtonAnimation(dismantleButton);
        }
    }

    /// <summary>
    /// Butona hover/press animasyonlari ekler
    /// </summary>
    void AddButtonAnimation(Button button)
    {
        if (button == null) return;

        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        RectTransform btnRect = button.GetComponent<RectTransform>();
        if (btnRect == null) return;

        // Hover: 1.08x
        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            if (UIAnimator.Instance != null)
                UIAnimator.Instance.ScaleTo(btnRect, Vector3.one * 1.08f, 0.1f);
        });
        trigger.triggers.Add(enterEntry);

        // Exit: 1.0x
        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            if (UIAnimator.Instance != null)
                UIAnimator.Instance.ScaleTo(btnRect, Vector3.one, 0.1f);
        });
        trigger.triggers.Add(exitEntry);

        // Press: 0.92x
        var downEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        downEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => {
            if (UIAnimator.Instance != null)
                UIAnimator.Instance.ScaleTo(btnRect, Vector3.one * 0.92f, 0.05f);
        });
        trigger.triggers.Add(downEntry);

        // Release: 1.0x
        var upEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        upEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) => {
            if (UIAnimator.Instance != null)
                UIAnimator.Instance.ScaleTo(btnRect, Vector3.one, 0.1f);
        });
        trigger.triggers.Add(upEntry);
    }

    /// <summary>
    /// Item detaylarini goster
    /// </summary>
    public void ShowItem(InventoryItemInstance item)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        currentItem = item;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        isVisible = true;

        // Item bilgileri
        UpdateItemInfo(item);

        // Stat listesi
        UpdateStatList(item);

        // Set bilgisi
        UpdateSetInfo(item);

        // Butonlar
        UpdateButtons(item);

        // Pop-in animasyonu
        if (panelRoot != null && UIAnimator.Instance != null)
        {
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                UIAnimator.Instance.ScalePunch(panelRect, 1.05f, 0.2f);
            }
        }
    }

    /// <summary>
    /// Paneli gizle
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        currentItem = null;
    }

    void UpdateItemInfo(InventoryItemInstance item)
    {
        var data = item.GetData();

        // Icon
        if (itemIcon != null)
        {
            itemIcon.sprite = InventorySprites.Instance?.GetSprite(item.itemType);
        }

        // Rarity background
        if (rarityBackground != null)
        {
            rarityBackground.color = ItemRarityHelper.GetGlowColor(item.rarity);
        }

        // Isim
        if (itemNameText != null)
        {
            itemNameText.text = item.GetDisplayName();
            itemNameText.color = ItemRarityHelper.GetRarityColor(item.rarity);
        }

        // Aciklama
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = data.description;
        }

        // Rarity
        if (rarityText != null)
        {
            rarityText.text = ItemRarityHelper.GetRarityName(item.rarity);
            rarityText.color = ItemRarityHelper.GetRarityColor(item.rarity);
        }

        // Kategori
        if (categoryText != null)
        {
            categoryText.text = GetCategoryText(data.category);
        }
    }

    void UpdateStatList(InventoryItemInstance item)
    {
        // Eski entryleri temizle
        foreach (var entry in spawnedStatEntries)
        {
            Destroy(entry);
        }
        spawnedStatEntries.Clear();

        if (statListContainer == null || statEntryPrefab == null) return;

        var data = item.GetData();

        // Damage bonus
        if (data.damageBonus > 0)
        {
            AddStatEntry("Hasar", $"+{item.GetEffectiveDamageBonus() * 100:F0}%", new Color(1f, 0.5f, 0.5f));
        }

        // Speed bonus
        if (data.speedBonus > 0)
        {
            AddStatEntry("Hız", $"+{item.GetEffectiveSpeedBonus() * 100:F0}%", new Color(0.5f, 1f, 0.5f));
        }

        // Defense bonus
        if (data.defenseBonus > 0)
        {
            AddStatEntry("Savunma", $"+{item.GetEffectiveDefenseBonus() * 100:F0}%", new Color(0.5f, 0.5f, 1f));
        }

        // Crit bonus
        if (data.critBonus > 0)
        {
            AddStatEntry("Kritik", $"+{item.GetEffectiveCritBonus() * 100:F0}%", new Color(1f, 1f, 0.5f));
        }

        // Drop rate bonus
        if (data.dropRateBonus > 0)
        {
            AddStatEntry("Şans", $"+{item.GetEffectiveDropRateBonus() * 100:F0}%", new Color(0.5f, 1f, 1f));
        }

        // Stack count
        if (item.stackCount > 1)
        {
            AddStatEntry("Miktar", $"{item.stackCount}/{data.maxStack}", Color.white);
        }
    }

    void AddStatEntry(string statName, string statValue, Color valueColor)
    {
        if (statListContainer == null || statEntryPrefab == null) return;

        var entry = Instantiate(statEntryPrefab, statListContainer);
        spawnedStatEntries.Add(entry);

        // Text componentlerini bul
        var texts = entry.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 2)
        {
            texts[0].text = statName;
            texts[1].text = statValue;
            texts[1].color = valueColor;
        }
        else if (texts.Length == 1)
        {
            texts[0].text = $"{statName}: {statValue}";
            texts[0].color = valueColor;
        }
    }

    void UpdateSetInfo(InventoryItemInstance item)
    {
        if (setInfoSection == null) return;

        if (!item.IsSetPiece())
        {
            setInfoSection.SetActive(false);
            return;
        }

        setInfoSection.SetActive(true);

        string setId = item.GetSetId();
        var setDef = SetBonusManager.GetSetDefinition(setId);

        if (setDef == null)
        {
            setInfoSection.SetActive(false);
            return;
        }

        // Set ismi
        if (setNameText != null)
        {
            setNameText.text = setDef.setName;
            setNameText.color = setDef.setColor;
        }

        // Eski bonus entrylerini temizle
        foreach (var entry in spawnedBonusEntries)
        {
            Destroy(entry);
        }
        spawnedBonusEntries.Clear();

        // Set bonus listesi
        if (setBonusListContainer != null && setBonusPrefab != null)
        {
            int equippedCount = EquipmentManager.Instance?.GetEquippedSetPieceCount(setId) ?? 0;

            foreach (var tier in setDef.tiers)
            {
                var bonusEntry = Instantiate(setBonusPrefab, setBonusListContainer);
                spawnedBonusEntries.Add(bonusEntry);

                var text = bonusEntry.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    bool isActive = equippedCount >= tier.requiredPieces;
                    text.text = $"({tier.requiredPieces}) {tier.bonusDescription}";
                    text.color = isActive ? setDef.setColor : Color.gray;
                }
            }
        }
    }

    void UpdateButtons(InventoryItemInstance item)
    {
        var data = item.GetData();

        // Use button (consumable icin)
        if (useButton != null)
        {
            bool canUse = item.IsConsumable();
            useButton.gameObject.SetActive(canUse);

            if (useButtonText != null)
            {
                useButtonText.text = GetLocalizedText("inventory_use");
            }
        }

        // Equip button
        if (equipButton != null)
        {
            bool canEquip = item.IsEquippable();
            equipButton.gameObject.SetActive(canEquip);

            if (equipButtonText != null)
            {
                equipButtonText.text = item.isEquipped
                    ? GetLocalizedText("inventory_unequip")
                    : GetLocalizedText("inventory_equip");
            }
        }

        // Dismantle button (ekipman ve set parcalari icin)
        if (dismantleButton != null)
        {
            bool canDismantle = data.category == ItemCategory.Equipment || data.category == ItemCategory.SetPiece;
            dismantleButton.gameObject.SetActive(canDismantle);

            if (dismantleText != null && canDismantle)
            {
                ItemType mat = GetDismantleMaterial(item.rarity);
                int count = GetDismantleCount(item.rarity);
                string matName = InventoryItem.Create(mat).name;
                dismantleText.text = $"Sok ({count}x {matName})";
            }
        }

        // Sell value
        if (sellValueText != null)
        {
            sellValueText.text = item.GetSellValue().ToString();
        }
    }

    string GetCategoryText(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Consumable => "Sarf Malzemesi",
            ItemCategory.Equipment => "Ekipman",
            ItemCategory.Material => "Malzeme",
            ItemCategory.SetPiece => "Set Parçası",
            ItemCategory.Special => "Özel",
            _ => "Bilinmiyor"
        };
    }

    string GetLocalizedText(string key)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.Get(key);

        return key switch
        {
            "inventory_use" => "Kullan",
            "inventory_equip" => "Ekiple",
            "inventory_unequip" => "Çıkar",
            "inventory_drop" => "At",
            "inventory_sell" => "Sat",
            _ => key
        };
    }

    // === BUTON AKSIYONLARI ===

    void OnUseClicked()
    {
        if (currentItem == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        // Item kullan
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.UseItem(currentItem);
        }

        // Panel'i guncelle veya gizle
        if (currentItem.stackCount <= 0)
        {
            Hide();
        }
        else
        {
            ShowItem(currentItem);
        }
    }

    void OnEquipClicked()
    {
        if (currentItem == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        if (currentItem.isEquipped)
        {
            // Cikar
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.Unequip(currentItem.equippedSlot);
            }
        }
        else
        {
            // Ekiple
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.TryEquip(currentItem);
            }
        }

        // Panel'i guncelle
        ShowItem(currentItem);
    }

    void OnDropClicked()
    {
        if (currentItem == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        // Item'i at (yere drop et)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.DropItem(currentItem);
        }

        Hide();
    }

    void OnSellClicked()
    {
        if (currentItem == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCoin();

        int sellValue = currentItem.GetSellValue();

        // Para ekle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin(sellValue);
        }

        // Bildirim goster
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification(
                "SATILDI!",
                $"{currentItem.GetDisplayName()} - {sellValue} coin",
                NotificationType.Info
            );
        }

        // Item'i sil
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(currentItem);
        }

        // Envanter yenile
        if (InventoryUIController.Instance != null)
        {
            InventoryUIController.Instance.RefreshInventory();
        }

        Hide();
    }

    void OnDismantleClicked()
    {
        if (currentItem == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        // Rarity'ye gore malzeme ver
        ItemType materialType = GetDismantleMaterial(currentItem.rarity);
        int materialCount = GetDismantleCount(currentItem.rarity);

        // Malzeme ekle
        if (InventoryManager.Instance != null && materialCount > 0)
        {
            InventoryManager.Instance.AddItem(materialType, materialCount);
        }

        // Bildirim goster
        string materialName = InventoryItem.Create(materialType).name;
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification(
                "SOKULDU!",
                $"{materialCount}x {materialName} elde edildi",
                NotificationType.Info
            );
        }

        // Item'i sil
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(currentItem);
        }

        // Envanter yenile
        if (InventoryUIController.Instance != null)
        {
            InventoryUIController.Instance.RefreshInventory();
        }

        Hide();
    }

    ItemType GetDismantleMaterial(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => ItemType.ScrapMetal,
            ItemRarity.Uncommon => ItemType.ScrapMetal,
            ItemRarity.Rare => ItemType.NeonCrystal,
            ItemRarity.Epic => ItemType.VoidEssence,
            ItemRarity.Legendary => ItemType.PlasmaCore,
            _ => ItemType.ScrapMetal
        };
    }

    int GetDismantleCount(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => 1,
            ItemRarity.Uncommon => 2,
            ItemRarity.Rare => 3,
            ItemRarity.Epic => 2,
            ItemRarity.Legendary => 1,
            _ => 1
        };
    }
}
