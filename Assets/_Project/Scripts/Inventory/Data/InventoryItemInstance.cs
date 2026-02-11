using UnityEngine;
using System;

/// <summary>
/// Envanterdeki bir item ornegi.
/// Rarity, stack count ve benzersiz ID icerir.
/// </summary>
[Serializable]
public class InventoryItemInstance
{
    /// <summary>
    /// Benzersiz tanimlayici (GUID)
    /// </summary>
    public string uniqueId;

    /// <summary>
    /// Item turu
    /// </summary>
    public ItemType itemType;

    /// <summary>
    /// Item nadirlik seviyesi
    /// </summary>
    public ItemRarity rarity;

    /// <summary>
    /// Yigin sayisi
    /// </summary>
    public int stackCount;

    /// <summary>
    /// Envanter grid'indeki slot indeksi
    /// </summary>
    public int slotIndex;

    /// <summary>
    /// Ekipman takili mi?
    /// </summary>
    public bool isEquipped;

    /// <summary>
    /// Hangi slota takili
    /// </summary>
    public EquipmentSlot equippedSlot;

    /// <summary>
    /// Olusturulma zamani (siralamada kullanilir)
    /// </summary>
    public long createdTimestamp;

    // Cached data
    private InventoryItem _cachedData;

    /// <summary>
    /// Yeni bir item instance olustur
    /// </summary>
    public static InventoryItemInstance Create(ItemType type, ItemRarity rarity, int count = 1)
    {
        var instance = new InventoryItemInstance
        {
            uniqueId = Guid.NewGuid().ToString(),
            itemType = type,
            rarity = rarity,
            stackCount = count,
            slotIndex = -1,
            isEquipped = false,
            equippedSlot = EquipmentSlot.None,
            createdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return instance;
    }

    /// <summary>
    /// Varsayilan rarity ile olustur (Common)
    /// </summary>
    public static InventoryItemInstance Create(ItemType type, int count = 1)
    {
        return Create(type, ItemRarity.Common, count);
    }

    /// <summary>
    /// Rastgele rarity ile olustur
    /// </summary>
    public static InventoryItemInstance CreateWithRandomRarity(ItemType type, int count = 1, float bonusLuck = 0f)
    {
        ItemRarity rarity = ItemRarityHelper.RollRarity(bonusLuck);
        return Create(type, rarity, count);
    }

    /// <summary>
    /// Item verisini al (cached)
    /// </summary>
    public InventoryItem GetData()
    {
        if (_cachedData == null || _cachedData.type != itemType)
        {
            _cachedData = InventoryItem.Create(itemType);
        }
        return _cachedData;
    }

    /// <summary>
    /// Rarity ile carpilmis efektif hasar bonusu
    /// </summary>
    public float GetEffectiveDamageBonus()
    {
        var data = GetData();
        return data.damageBonus * ItemRarityHelper.GetStatMultiplier(rarity);
    }

    /// <summary>
    /// Rarity ile carpilmis efektif hiz bonusu
    /// </summary>
    public float GetEffectiveSpeedBonus()
    {
        var data = GetData();
        return data.speedBonus * ItemRarityHelper.GetStatMultiplier(rarity);
    }

    /// <summary>
    /// Rarity ile carpilmis efektif savunma bonusu
    /// </summary>
    public float GetEffectiveDefenseBonus()
    {
        var data = GetData();
        return data.defenseBonus * ItemRarityHelper.GetStatMultiplier(rarity);
    }

    /// <summary>
    /// Rarity ile carpilmis efektif crit bonusu
    /// </summary>
    public float GetEffectiveCritBonus()
    {
        var data = GetData();
        return data.critBonus * ItemRarityHelper.GetStatMultiplier(rarity);
    }

    /// <summary>
    /// Rarity ile carpilmis efektif drop rate bonusu
    /// </summary>
    public float GetEffectiveDropRateBonus()
    {
        var data = GetData();
        return data.dropRateBonus * ItemRarityHelper.GetStatMultiplier(rarity);
    }

    /// <summary>
    /// Satis degerini hesapla
    /// </summary>
    public int GetSellValue()
    {
        var data = GetData();
        return data.baseSellValue * ItemRarityHelper.GetSellValueMultiplier(rarity) * stackCount;
    }

    /// <summary>
    /// Item ismi (rarity dahil)
    /// </summary>
    public string GetDisplayName()
    {
        var data = GetData();
        if (rarity > ItemRarity.Common)
        {
            return $"{ItemRarityHelper.GetRarityName(rarity)} {data.name}";
        }
        return data.name;
    }

    /// <summary>
    /// Rarity rengi
    /// </summary>
    public Color GetRarityColor()
    {
        return ItemRarityHelper.GetRarityColor(rarity);
    }

    /// <summary>
    /// Glow rengi
    /// </summary>
    public Color GetGlowColor()
    {
        return ItemRarityHelper.GetGlowColor(rarity);
    }

    /// <summary>
    /// Stack eklenebilir mi?
    /// </summary>
    public bool CanStack(InventoryItemInstance other)
    {
        if (other == null) return false;
        if (itemType != other.itemType) return false;
        if (rarity != other.rarity) return false;

        var data = GetData();
        return stackCount < data.maxStack;
    }

    /// <summary>
    /// Stack ekle
    /// </summary>
    public int AddToStack(int amount)
    {
        var data = GetData();
        int space = data.maxStack - stackCount;
        int toAdd = Mathf.Min(amount, space);
        stackCount += toAdd;
        return amount - toAdd; // Kalan miktar
    }

    /// <summary>
    /// Stack'ten cikar
    /// </summary>
    public int RemoveFromStack(int amount)
    {
        int toRemove = Mathf.Min(amount, stackCount);
        stackCount -= toRemove;
        return toRemove;
    }

    /// <summary>
    /// Ekipman mi?
    /// </summary>
    public bool IsEquippable()
    {
        return GetData().IsEquippable();
    }

    /// <summary>
    /// Tuketilebilir mi?
    /// </summary>
    public bool IsConsumable()
    {
        return GetData().IsConsumable();
    }

    /// <summary>
    /// Set parcasi mi?
    /// </summary>
    public bool IsSetPiece()
    {
        return GetData().IsSetPiece();
    }

    /// <summary>
    /// Set ID'si
    /// </summary>
    public string GetSetId()
    {
        return GetData().setId;
    }

    /// <summary>
    /// Klonla
    /// </summary>
    public InventoryItemInstance Clone()
    {
        return new InventoryItemInstance
        {
            uniqueId = Guid.NewGuid().ToString(),
            itemType = this.itemType,
            rarity = this.rarity,
            stackCount = this.stackCount,
            slotIndex = -1,
            isEquipped = false,
            equippedSlot = EquipmentSlot.None,
            createdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    /// <summary>
    /// Tam kopya (ayni ID ile)
    /// </summary>
    public InventoryItemInstance DeepCopy()
    {
        return new InventoryItemInstance
        {
            uniqueId = this.uniqueId,
            itemType = this.itemType,
            rarity = this.rarity,
            stackCount = this.stackCount,
            slotIndex = this.slotIndex,
            isEquipped = this.isEquipped,
            equippedSlot = this.equippedSlot,
            createdTimestamp = this.createdTimestamp
        };
    }

    public override string ToString()
    {
        return $"{GetDisplayName()} x{stackCount} (Slot:{slotIndex}, Equipped:{isEquipped})";
    }
}

/// <summary>
/// Envanter kayit verisi
/// </summary>
[Serializable]
public class InventorySaveData
{
    public InventoryItemSaveData[] items;
    public EquippedItemSaveData[] equippedItems;
    public int[] quickSlotItemIds;

    public InventorySaveData()
    {
        items = new InventoryItemSaveData[0];
        equippedItems = new EquippedItemSaveData[0];
        quickSlotItemIds = new int[4] { -1, -1, -1, -1 };
    }
}

/// <summary>
/// Tek item kayit verisi
/// </summary>
[Serializable]
public class InventoryItemSaveData
{
    public string uniqueId;
    public int itemTypeIndex;
    public int rarityIndex;
    public int stackCount;
    public int slotIndex;
    public long createdTimestamp;

    public static InventoryItemSaveData FromInstance(InventoryItemInstance instance)
    {
        return new InventoryItemSaveData
        {
            uniqueId = instance.uniqueId,
            itemTypeIndex = (int)instance.itemType,
            rarityIndex = (int)instance.rarity,
            stackCount = instance.stackCount,
            slotIndex = instance.slotIndex,
            createdTimestamp = instance.createdTimestamp
        };
    }

    public InventoryItemInstance ToInstance()
    {
        return new InventoryItemInstance
        {
            uniqueId = uniqueId,
            itemType = (ItemType)itemTypeIndex,
            rarity = (ItemRarity)rarityIndex,
            stackCount = stackCount,
            slotIndex = slotIndex,
            isEquipped = false,
            equippedSlot = EquipmentSlot.None,
            createdTimestamp = createdTimestamp
        };
    }
}

/// <summary>
/// Ekipman kayit verisi
/// </summary>
[Serializable]
public class EquippedItemSaveData
{
    public string itemUniqueId;
    public int slotIndex;

    public EquippedItemSaveData(string id, EquipmentSlot slot)
    {
        itemUniqueId = id;
        slotIndex = (int)slot;
    }
}
