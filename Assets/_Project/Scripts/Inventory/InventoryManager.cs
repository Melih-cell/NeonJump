using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Envanter yönetim sistemi
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Quick Slots")]
    public int quickSlotCount = 4;

    // Envanter verileri
    private Dictionary<ItemType, int> inventory = new Dictionary<ItemType, int>();
    private ItemType[] quickSlots; // Hızlı erişim slotları

    // Events
    public System.Action<ItemType, int> OnItemChanged;
    public System.Action<int, ItemType> OnQuickSlotChanged;
    public System.Action<ItemType> OnItemUsed;

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

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Sahne yeniden yüklendiğinde event'leri temizle
        // (Eski UI nesnelerine referanslar geçersiz olabilir)
        OnItemChanged = null;
        OnQuickSlotChanged = null;
        OnItemUsed = null;
    }

    void Initialize()
    {
        // Tüm eşya türleri için envanter başlat
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            inventory[type] = 0;
        }

        // Hızlı slotları başlat
        quickSlots = new ItemType[quickSlotCount];

        // Varsayılan slot atamaları
        if (quickSlotCount >= 4)
        {
            quickSlots[0] = ItemType.HealthPotion;
            quickSlots[1] = ItemType.Shield;
            quickSlots[2] = ItemType.SpeedBoost;
            quickSlots[3] = ItemType.Bomb;
        }

        // Kayıtlı envanteri yükle
        LoadInventory();
    }

    /// <summary>
    /// Eşya ekle
    /// </summary>
    public bool AddItem(ItemType type, int amount = 1)
    {
        InventoryItem itemInfo = InventoryItem.Create(type);
        int currentAmount = inventory[type];
        int newAmount = Mathf.Min(currentAmount + amount, itemInfo.maxStack);

        if (newAmount > currentAmount)
        {
            inventory[type] = newAmount;
            OnItemChanged?.Invoke(type, newAmount);

            // Ses efekti
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayPowerUp();

            SaveInventory();
            return true;
        }

        return false; // Envanter dolu
    }

    /// <summary>
    /// Eşya kullan
    /// </summary>
    public bool UseItem(ItemType type)
    {
        if (inventory[type] <= 0) return false;

        // Eşyayı kullan
        bool used = ApplyItemEffect(type);

        if (used)
        {
            inventory[type]--;
            OnItemChanged?.Invoke(type, inventory[type]);
            OnItemUsed?.Invoke(type);
            SaveInventory();
        }

        return used;
    }

    /// <summary>
    /// Hızlı slot kullan (0-3)
    /// </summary>
    public bool UseQuickSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotCount) return false;

        ItemType itemType = quickSlots[slotIndex];
        return UseItem(itemType);
    }

    /// <summary>
    /// Eşya efektini uygula
    /// </summary>
    bool ApplyItemEffect(ItemType type)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        InventoryItem itemInfo = InventoryItem.Create(type);

        switch (type)
        {
            case ItemType.HealthPotion:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Heal(1);
                    SpawnEffect(player?.transform.position ?? Vector3.zero, itemInfo.itemColor);
                    return true;
                }
                break;

            case ItemType.Shield:
                if (player != null)
                {
                    player.SetInvincible(true);
                    // Süre sonunda kapat
                    StartCoroutine(RemoveEffectAfterDelay(() => {
                        if (player != null) player.SetInvincible(false);
                    }, itemInfo.duration));
                    SpawnEffect(player.transform.position, itemInfo.itemColor);
                    return true;
                }
                break;

            case ItemType.SpeedBoost:
                if (PowerUpManager.Instance != null)
                {
                    PowerUpManager.Instance.ActivatePowerUp(PowerUpType.SpeedBoost, itemInfo.duration);
                    SpawnEffect(player?.transform.position ?? Vector3.zero, itemInfo.itemColor);
                    return true;
                }
                break;

            case ItemType.DoubleDamage:
                // Double damage için PowerUpManager'a yeni bir tip eklenebilir
                // Şimdilik basit bir efekt
                if (player != null)
                {
                    SpawnEffect(player.transform.position, itemInfo.itemColor);
                    return true;
                }
                break;

            case ItemType.Magnet:
                if (PowerUpManager.Instance != null)
                {
                    PowerUpManager.Instance.ActivatePowerUp(PowerUpType.Magnet, itemInfo.duration);
                    SpawnEffect(player?.transform.position ?? Vector3.zero, itemInfo.itemColor);
                    return true;
                }
                break;

            case ItemType.Bomb:
                if (player != null)
                {
                    ExplodeBomb(player.transform.position);
                    return true;
                }
                break;

            case ItemType.ExtraLife:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.maxHealth++;
                    GameManager.Instance.Heal(1);
                    SpawnEffect(player?.transform.position ?? Vector3.zero, itemInfo.itemColor);
                    return true;
                }
                break;
        }

        return false;
    }

    void ExplodeBomb(Vector3 position)
    {
        float radius = 5f;

        // Efekt
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEnemyDeath(position);
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossHit();
        }

        // Yakındaki düşmanları bul ve öldür
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);
        foreach (Collider2D col in colliders)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnemyKilled(enemy.transform.position);
                }
            }
        }
    }

    void SpawnEffect(Vector3 position, Color color)
    {
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayPowerUpCollect(position);
        }
    }

    System.Collections.IEnumerator RemoveEffectAfterDelay(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    /// <summary>
    /// Eşya miktarını al
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }

    /// <summary>
    /// Tum item instance'larini al (yeni envanter UI icin)
    /// </summary>
    public List<InventoryItemInstance> GetAllItemInstances()
    {
        var result = new List<InventoryItemInstance>();
        int slotIdx = 0;
        foreach (var kvp in inventory)
        {
            if (kvp.Value > 0)
            {
                var instance = InventoryItemInstance.Create(kvp.Key, kvp.Value);
                instance.slotIndex = slotIdx;
                result.Add(instance);
                slotIdx++;
            }
        }
        return result;
    }

    /// <summary>
    /// Item instance'ini envanterden kaldir
    /// </summary>
    public bool RemoveItem(InventoryItemInstance item)
    {
        if (item == null) return false;

        if (inventory.ContainsKey(item.itemType) && inventory[item.itemType] >= item.stackCount)
        {
            inventory[item.itemType] -= item.stackCount;
            OnItemChanged?.Invoke(item.itemType, inventory[item.itemType]);
            SaveInventory();
            return true;
        }
        return false;
    }

    /// <summary>
    /// InventoryItemInstance uzerinden esya kullan
    /// </summary>
    public bool UseItem(InventoryItemInstance item)
    {
        if (item == null) return false;
        return UseItem(item.itemType);
    }

    /// <summary>
    /// InventoryItemInstance olarak esya ekle.
    /// Rarity ve stack bilgisini korur.
    /// </summary>
    public bool TryAddItemInstance(InventoryItemInstance item)
    {
        if (item == null) return false;

        return AddItem(item.itemType, item.stackCount);
    }

    /// <summary>
    /// Belirtilen turden belirtilen miktarda esya kaldir.
    /// </summary>
    public bool RemoveItemByType(ItemType type, int amount)
    {
        if (!inventory.ContainsKey(type)) return false;
        if (inventory[type] < amount) return false;

        inventory[type] -= amount;
        OnItemChanged?.Invoke(type, inventory[type]);
        SaveInventory();
        return true;
    }

    /// <summary>
    /// Esyayi envanterden cikar ve dunyaya drop et.
    /// </summary>
    public void DropItem(InventoryItemInstance item)
    {
        if (item == null) return;

        // Envanterden cikar
        RemoveItem(item);

        // Dunyaya drop et (player pozisyonunda)
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Vector3 dropPos = player.transform.position + Vector3.right * 1.5f;
            LootDropVisual.Create(item.itemType, item.rarity, item.stackCount, dropPos);
        }

        Debug.Log($"[InventoryManager] Item drop edildi: {item.GetDisplayName()}");
    }

    /// <summary>
    /// Hızlı slottaki eşya türünü al
    /// </summary>
    public ItemType GetQuickSlotItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < quickSlotCount)
            return quickSlots[slotIndex];
        return ItemType.HealthPotion;
    }

    /// <summary>
    /// Hızlı slota eşya ata
    /// </summary>
    public void SetQuickSlotItem(int slotIndex, ItemType type)
    {
        if (slotIndex >= 0 && slotIndex < quickSlotCount)
        {
            quickSlots[slotIndex] = type;
            OnQuickSlotChanged?.Invoke(slotIndex, type);
            SaveInventory();
        }
    }

    // === KAYIT SİSTEMİ ===

    void SaveInventory()
    {
        foreach (var item in inventory)
        {
            PlayerPrefs.SetInt("Inv_" + item.Key.ToString(), item.Value);
        }

        for (int i = 0; i < quickSlotCount; i++)
        {
            PlayerPrefs.SetInt("QuickSlot_" + i, (int)quickSlots[i]);
        }

        PlayerPrefs.Save();
    }

    void LoadInventory()
    {
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            inventory[type] = PlayerPrefs.GetInt("Inv_" + type.ToString(), 0);
        }

        for (int i = 0; i < quickSlotCount; i++)
        {
            int savedType = PlayerPrefs.GetInt("QuickSlot_" + i, (int)quickSlots[i]);
            quickSlots[i] = (ItemType)savedType;
        }
    }

    /// <summary>
    /// Envanteri sıfırla (test için)
    /// </summary>
    public void ResetInventory()
    {
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            inventory[type] = 0;
        }
        SaveInventory();
    }

    /// <summary>
    /// Test için eşya ekle
    /// </summary>
    [ContextMenu("Add Test Items")]
    public void AddTestItems()
    {
        AddItem(ItemType.HealthPotion, 3);
        AddItem(ItemType.Shield, 2);
        AddItem(ItemType.SpeedBoost, 2);
        AddItem(ItemType.Bomb, 1);
    }
}
