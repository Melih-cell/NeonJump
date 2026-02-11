using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Loot tablosu girisi
/// </summary>
[Serializable]
public class LootTableEntry
{
    [Tooltip("Drop edilecek item turu")]
    public ItemType itemType;

    [Tooltip("Drop sansi (0-100)")]
    [Range(0f, 100f)]
    public float dropChance = 10f;

    [Tooltip("Minimum miktar")]
    [Min(1)]
    public int minAmount = 1;

    [Tooltip("Maksimum miktar")]
    [Min(1)]
    public int maxAmount = 1;

    [Tooltip("Zorunlu rarity (None = rastgele)")]
    public ItemRarity forcedRarity = ItemRarity.Common;

    [Tooltip("Rastgele rarity kullan")]
    public bool useRandomRarity = true;

    [Tooltip("Bonus luck carpani (1.0 = normal)")]
    public float luckMultiplier = 1f;

    /// <summary>
    /// Rastgele miktar al
    /// </summary>
    public int GetRandomAmount()
    {
        return UnityEngine.Random.Range(minAmount, maxAmount + 1);
    }

    /// <summary>
    /// Rarity belirle
    /// </summary>
    public ItemRarity GetRarity(float bonusLuck = 0f)
    {
        if (!useRandomRarity)
            return forcedRarity;

        return ItemRarityHelper.RollRarity(bonusLuck * luckMultiplier);
    }
}

/// <summary>
/// Garantili drop girisi
/// </summary>
[Serializable]
public class GuaranteedDrop
{
    public ItemType itemType;
    public ItemRarity minRarity = ItemRarity.Common;
    public int amount = 1;
}

/// <summary>
/// Loot tablosu ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "NeonJump/Loot Table")]
public class LootTableSO : ScriptableObject
{
    [Header("Tablo Bilgisi")]
    public string tableId;
    public string tableName;

    [Header("Drop Ayarlari")]
    [Tooltip("Maksimum drop sayisi (0 = sinirsiz)")]
    public int maxDrops = 3;

    [Tooltip("Minimum drop sayisi")]
    public int minDrops = 0;

    [Tooltip("Coin drop sansi")]
    [Range(0f, 100f)]
    public float coinDropChance = 80f;

    [Tooltip("Coin miktari (min-max)")]
    public Vector2Int coinAmount = new Vector2Int(1, 5);

    [Header("Loot Girisler")]
    public List<LootTableEntry> entries = new List<LootTableEntry>();

    [Header("Garantili Droplar")]
    [Tooltip("Her zaman drop edilecek itemler")]
    public List<GuaranteedDrop> guaranteedDrops = new List<GuaranteedDrop>();

    [Header("Ozel Ayarlar")]
    [Tooltip("Boss loot tablosu mu?")]
    public bool isBossTable = false;

    [Tooltip("Boss tablosu ise minimum epic+ drop garantisi")]
    public bool guaranteeEpicOnBoss = true;

    /// <summary>
    /// Loot roll yap
    /// </summary>
    public List<LootResult> RollLoot(float bonusLuck = 0f)
    {
        var results = new List<LootResult>();

        // Garantili droplar
        foreach (var guaranteed in guaranteedDrops)
        {
            results.Add(new LootResult
            {
                itemType = guaranteed.itemType,
                rarity = GetBoostedRarity(guaranteed.minRarity, bonusLuck),
                amount = guaranteed.amount
            });
        }

        // Boss tablosu ise epic+ garanti
        if (isBossTable && guaranteeEpicOnBoss)
        {
            var epicEntry = GetRandomEntryWithMinRarity(ItemRarity.Epic);
            if (epicEntry != null)
            {
                results.Add(new LootResult
                {
                    itemType = epicEntry.itemType,
                    rarity = GetBoostedRarity(ItemRarity.Epic, bonusLuck),
                    amount = epicEntry.GetRandomAmount()
                });
            }
        }

        // Normal droplar
        int dropCount = 0;
        foreach (var entry in entries)
        {
            if (maxDrops > 0 && dropCount >= maxDrops)
                break;

            // Drop sansi kontrolu
            float adjustedChance = entry.dropChance * (1f + bonusLuck * 0.01f);
            if (UnityEngine.Random.value * 100f <= adjustedChance)
            {
                results.Add(new LootResult
                {
                    itemType = entry.itemType,
                    rarity = entry.GetRarity(bonusLuck),
                    amount = entry.GetRandomAmount()
                });
                dropCount++;
            }
        }

        // Minimum drop kontrolu
        while (results.Count < minDrops && entries.Count > 0)
        {
            var randomEntry = entries[UnityEngine.Random.Range(0, entries.Count)];
            results.Add(new LootResult
            {
                itemType = randomEntry.itemType,
                rarity = randomEntry.GetRarity(bonusLuck),
                amount = randomEntry.GetRandomAmount()
            });
        }

        // Coin drop
        if (UnityEngine.Random.value * 100f <= coinDropChance)
        {
            int coinCount = UnityEngine.Random.Range(coinAmount.x, coinAmount.y + 1);
            // Coin'ler ayri handle edilir (LootResult'a dahil degil)
        }

        return results;
    }

    /// <summary>
    /// Belirli rarity'den yukarisini garanti et
    /// </summary>
    ItemRarity GetBoostedRarity(ItemRarity minRarity, float bonusLuck)
    {
        ItemRarity rolled = ItemRarityHelper.RollRarity(bonusLuck);
        return rolled > minRarity ? rolled : minRarity;
    }

    /// <summary>
    /// Minimum rarity'li rastgele entry
    /// </summary>
    LootTableEntry GetRandomEntryWithMinRarity(ItemRarity minRarity)
    {
        var validEntries = entries.FindAll(e => !e.useRandomRarity && e.forcedRarity >= minRarity);

        if (validEntries.Count == 0)
        {
            // Herhangi birini sec
            if (entries.Count > 0)
                return entries[UnityEngine.Random.Range(0, entries.Count)];
            return null;
        }

        return validEntries[UnityEngine.Random.Range(0, validEntries.Count)];
    }

    /// <summary>
    /// Coin miktari roll
    /// </summary>
    public int RollCoinAmount()
    {
        if (UnityEngine.Random.value * 100f > coinDropChance)
            return 0;

        return UnityEngine.Random.Range(coinAmount.x, coinAmount.y + 1);
    }
}

/// <summary>
/// Loot roll sonucu
/// </summary>
public struct LootResult
{
    public ItemType itemType;
    public ItemRarity rarity;
    public int amount;

    public override string ToString()
    {
        return $"{ItemRarityHelper.GetRarityName(rarity)} {itemType} x{amount}";
    }
}

/// <summary>
/// Varsayilan loot tablolari (kod tabanli)
/// </summary>
public static class DefaultLootTables
{
    // Basit dusman tablosu
    public static List<LootResult> RollBasicEnemy(float bonusLuck = 0f)
    {
        var results = new List<LootResult>();

        // %30 sans ile item drop
        if (UnityEngine.Random.value <= 0.30f + bonusLuck * 0.01f)
        {
            // Rastgele item sec
            ItemType[] possibleDrops = {
                ItemType.ScrapMetal,
                ItemType.HealthPotion,
                ItemType.NeonCrystal
            };

            results.Add(new LootResult
            {
                itemType = possibleDrops[UnityEngine.Random.Range(0, possibleDrops.Length)],
                rarity = ItemRarityHelper.RollRarity(bonusLuck),
                amount = 1
            });
        }

        return results;
    }

    // Elit dusman tablosu
    public static List<LootResult> RollEliteEnemy(float bonusLuck = 0f)
    {
        var results = new List<LootResult>();

        // %60 sans ile item drop
        if (UnityEngine.Random.value <= 0.60f + bonusLuck * 0.01f)
        {
            ItemType[] possibleDrops = {
                ItemType.ScrapMetal,
                ItemType.NeonCrystal,
                ItemType.VoidEssence,
                ItemType.DamageBooster,
                ItemType.SpeedRing
            };

            // Minimum Uncommon
            ItemRarity rarity = ItemRarityHelper.RollRarity(bonusLuck + 20f);
            if (rarity < ItemRarity.Uncommon)
                rarity = ItemRarity.Uncommon;

            results.Add(new LootResult
            {
                itemType = possibleDrops[UnityEngine.Random.Range(0, possibleDrops.Length)],
                rarity = rarity,
                amount = 1
            });
        }

        // %20 sans ile ekstra drop
        if (UnityEngine.Random.value <= 0.20f)
        {
            results.Add(new LootResult
            {
                itemType = ItemType.PlasmaCore,
                rarity = ItemRarityHelper.RollRarity(bonusLuck),
                amount = 1
            });
        }

        return results;
    }

    // Boss tablosu
    public static List<LootResult> RollBoss(float bonusLuck = 0f)
    {
        var results = new List<LootResult>();

        // Garantili epic+ item
        ItemType[] epicDrops = {
            ItemType.VampireAmulet,
            ItemType.ShieldGenerator,
            ItemType.EternalShard,
            ItemType.NeonWarriorHelm,
            ItemType.ShadowHunterMask,
            ItemType.VoidWalkerCrown
        };

        ItemRarity bossRarity = ItemRarityHelper.RollRarity(bonusLuck + 50f);
        if (bossRarity < ItemRarity.Epic)
            bossRarity = ItemRarity.Epic;

        results.Add(new LootResult
        {
            itemType = epicDrops[UnityEngine.Random.Range(0, epicDrops.Length)],
            rarity = bossRarity,
            amount = 1
        });

        // Ekstra 2-3 drop
        int extraDrops = UnityEngine.Random.Range(2, 4);
        ItemType[] normalDrops = {
            ItemType.ScrapMetal,
            ItemType.NeonCrystal,
            ItemType.VoidEssence,
            ItemType.PlasmaCore
        };

        for (int i = 0; i < extraDrops; i++)
        {
            results.Add(new LootResult
            {
                itemType = normalDrops[UnityEngine.Random.Range(0, normalDrops.Length)],
                rarity = ItemRarityHelper.RollRarity(bonusLuck + 10f),
                amount = UnityEngine.Random.Range(1, 3)
            });
        }

        return results;
    }
}
