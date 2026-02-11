using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Ekipman yonetim sistemi.
/// Takilan itemleri ve stat bonuslarini yonetir.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // Slot basina ekipman
    private Dictionary<EquipmentSlot, InventoryItemInstance> equippedItems;

    // Aktif set bonuslari
    private Dictionary<string, int> setItemCounts;

    // Events
    public event Action<EquipmentSlot, InventoryItemInstance> OnItemEquipped;
    public event Action<EquipmentSlot, InventoryItemInstance> OnItemUnequipped;
    public event Action<string, int, SetBonusTier> OnSetBonusActivated;
    public event Action<string> OnSetBonusDeactivated;
    public event Action OnStatsChanged;

    // Hesaplanan toplam statlar
    public float TotalDamageBonus { get; private set; }
    public float TotalSpeedBonus { get; private set; }
    public float TotalDefenseBonus { get; private set; }
    public float TotalCritBonus { get; private set; }
    public float TotalDropRateBonus { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        equippedItems = new Dictionary<EquipmentSlot, InventoryItemInstance>();
        setItemCounts = new Dictionary<string, int>();

        // Tum slotlari bosalt
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot != EquipmentSlot.None)
            {
                equippedItems[slot] = null;
            }
        }
    }

    /// <summary>
    /// Ekipman tak
    /// </summary>
    public bool TryEquip(InventoryItemInstance item)
    {
        if (item == null) return false;

        var data = item.GetData();
        if (!data.IsEquippable()) return false;

        EquipmentSlot targetSlot = data.equipSlot;
        if (targetSlot == EquipmentSlot.None) return false;

        // Mevcut ekipmani cikar
        if (equippedItems.ContainsKey(targetSlot) && equippedItems[targetSlot] != null)
        {
            Unequip(targetSlot);
        }

        // Yeni ekipmani tak
        equippedItems[targetSlot] = item;
        item.isEquipped = true;
        item.equippedSlot = targetSlot;

        // Set bonusu guncelle
        if (data.IsSetPiece())
        {
            UpdateSetBonus(data.setId, 1);
        }

        RecalculateStats();
        OnItemEquipped?.Invoke(targetSlot, item);

        Debug.Log($"[EquipmentManager] {item.GetDisplayName()} {targetSlot} slotuna takildi");
        return true;
    }

    /// <summary>
    /// Ekipmani cikar
    /// </summary>
    public InventoryItemInstance Unequip(EquipmentSlot slot)
    {
        if (!equippedItems.ContainsKey(slot) || equippedItems[slot] == null)
            return null;

        var item = equippedItems[slot];
        var data = item.GetData();

        // Set bonusu guncelle
        if (data.IsSetPiece())
        {
            UpdateSetBonus(data.setId, -1);
        }

        item.isEquipped = false;
        item.equippedSlot = EquipmentSlot.None;
        equippedItems[slot] = null;

        RecalculateStats();
        OnItemUnequipped?.Invoke(slot, item);

        Debug.Log($"[EquipmentManager] {item.GetDisplayName()} {slot} slotundan cikarildi");
        return item;
    }

    /// <summary>
    /// Slottaki ekipmani al
    /// </summary>
    public InventoryItemInstance GetEquippedItem(EquipmentSlot slot)
    {
        if (equippedItems.TryGetValue(slot, out var item))
            return item;
        return null;
    }

    /// <summary>
    /// Tum ekipmanlari al
    /// </summary>
    public Dictionary<EquipmentSlot, InventoryItemInstance> GetAllEquipped()
    {
        return new Dictionary<EquipmentSlot, InventoryItemInstance>(equippedItems);
    }

    /// <summary>
    /// Slot dolu mu?
    /// </summary>
    public bool IsSlotOccupied(EquipmentSlot slot)
    {
        return equippedItems.ContainsKey(slot) && equippedItems[slot] != null;
    }

    /// <summary>
    /// Set bonus sayisini guncelle
    /// </summary>
    void UpdateSetBonus(string setId, int change)
    {
        if (string.IsNullOrEmpty(setId)) return;

        if (!setItemCounts.ContainsKey(setId))
            setItemCounts[setId] = 0;

        int oldCount = setItemCounts[setId];
        setItemCounts[setId] = Mathf.Max(0, setItemCounts[setId] + change);
        int newCount = setItemCounts[setId];

        // Bonus tier degisimini kontrol et
        var oldTier = SetBonusManager.GetActiveTier(setId, oldCount);
        var newTier = SetBonusManager.GetActiveTier(setId, newCount);

        if (newTier != null && (oldTier == null || newTier.requiredPieces > oldTier.requiredPieces))
        {
            OnSetBonusActivated?.Invoke(setId, newCount, newTier);
        }
        else if (newTier == null && oldTier != null)
        {
            OnSetBonusDeactivated?.Invoke(setId);
        }

        // Set sayisi 0 olursa kaldir
        if (setItemCounts[setId] <= 0)
            setItemCounts.Remove(setId);
    }

    /// <summary>
    /// Set parca sayisini al
    /// </summary>
    public int GetSetPieceCount(string setId)
    {
        if (setItemCounts.TryGetValue(setId, out int count))
            return count;
        return 0;
    }

    /// <summary>
    /// Belirli bir item tipi ekipman olarak takilmis mi?
    /// </summary>
    public bool HasItemEquipped(ItemType itemType)
    {
        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null && kvp.Value.itemType == itemType)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Takili set parcasi sayisini al (UI icin)
    /// </summary>
    public int GetEquippedSetPieceCount(string setId)
    {
        return GetSetPieceCount(setId);
    }

    /// <summary>
    /// Aktif set bonuslarini al
    /// </summary>
    public List<ActiveSetBonus> GetActiveSetBonuses()
    {
        var result = new List<ActiveSetBonus>();

        foreach (var kvp in setItemCounts)
        {
            string setId = kvp.Key;
            int count = kvp.Value;

            var tier = SetBonusManager.GetActiveTier(setId, count);
            if (tier != null)
            {
                result.Add(new ActiveSetBonus
                {
                    setId = setId,
                    equippedCount = count,
                    activeTier = tier
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Tum statlari yeniden hesapla
    /// </summary>
    public void RecalculateStats()
    {
        TotalDamageBonus = 0f;
        TotalSpeedBonus = 0f;
        TotalDefenseBonus = 0f;
        TotalCritBonus = 0f;
        TotalDropRateBonus = 0f;

        // Ekipman bonuslari
        foreach (var kvp in equippedItems)
        {
            var item = kvp.Value;
            if (item == null) continue;

            TotalDamageBonus += item.GetEffectiveDamageBonus();
            TotalSpeedBonus += item.GetEffectiveSpeedBonus();
            TotalDefenseBonus += item.GetEffectiveDefenseBonus();
            TotalCritBonus += item.GetEffectiveCritBonus();
            TotalDropRateBonus += item.GetEffectiveDropRateBonus();
        }

        // Set bonuslari
        foreach (var kvp in setItemCounts)
        {
            string setId = kvp.Key;
            int count = kvp.Value;

            var tier = SetBonusManager.GetActiveTier(setId, count);
            if (tier != null)
            {
                TotalDamageBonus += tier.damageBonus;
                TotalSpeedBonus += tier.speedBonus;
                TotalDefenseBonus += tier.defenseBonus;
                TotalCritBonus += tier.critBonus;
                TotalDropRateBonus += tier.dropRateBonus;
            }
        }

        OnStatsChanged?.Invoke();

        Debug.Log($"[EquipmentManager] Stats guncellendi - DMG:{TotalDamageBonus:P0} SPD:{TotalSpeedBonus:P0} DEF:{TotalDefenseBonus:P0} CRIT:{TotalCritBonus:P0} DROP:{TotalDropRateBonus:P0}");
    }

    /// <summary>
    /// Kaydet
    /// </summary>
    public EquippedItemSaveData[] Save()
    {
        var saveData = new List<EquippedItemSaveData>();

        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                saveData.Add(new EquippedItemSaveData(kvp.Value.uniqueId, kvp.Key));
            }
        }

        return saveData.ToArray();
    }

    /// <summary>
    /// Yukle
    /// </summary>
    public void Load(EquippedItemSaveData[] data, List<InventoryItemInstance> allItems)
    {
        if (data == null) return;

        // Once tum ekipmanlari cikar
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot != EquipmentSlot.None)
            {
                equippedItems[slot] = null;
            }
        }
        setItemCounts.Clear();

        // Kaydedilen ekipmanlari yukle
        foreach (var saved in data)
        {
            var item = allItems.Find(i => i.uniqueId == saved.itemUniqueId);
            if (item != null)
            {
                var slot = (EquipmentSlot)saved.slotIndex;
                equippedItems[slot] = item;
                item.isEquipped = true;
                item.equippedSlot = slot;

                // Set bonusu
                var itemData = item.GetData();
                if (itemData.IsSetPiece())
                {
                    if (!setItemCounts.ContainsKey(itemData.setId))
                        setItemCounts[itemData.setId] = 0;
                    setItemCounts[itemData.setId]++;
                }
            }
        }

        RecalculateStats();
    }

    /// <summary>
    /// Sifirla
    /// </summary>
    public void Reset()
    {
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot != EquipmentSlot.None && equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
            {
                var item = equippedItems[slot];
                item.isEquipped = false;
                item.equippedSlot = EquipmentSlot.None;
                equippedItems[slot] = null;
            }
        }

        setItemCounts.Clear();
        RecalculateStats();
    }
}

/// <summary>
/// Aktif set bonus bilgisi
/// </summary>
public class ActiveSetBonus
{
    public string setId;
    public int equippedCount;
    public SetBonusTier activeTier;
}
