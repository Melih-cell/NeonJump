using UnityEngine;

/// <summary>
/// Item nadirlik seviyeleri
/// </summary>
public enum ItemRarity
{
    Common,     // Beyaz - x1.0
    Uncommon,   // Yesil - x1.15
    Rare,       // Mavi - x1.3
    Epic,       // Mor - x1.5
    Legendary   // Turuncu - x2.0
}

/// <summary>
/// Item kategorileri
/// </summary>
public enum ItemCategory
{
    Consumable,     // Tuketilebilir (iksir, bomba)
    Equipment,      // Ekipman (silah mod, aksesuar)
    Material,       // Malzeme (crafting icin)
    SetPiece,       // Set parcasi
    Special         // Ozel (quest itemleri, anahtarlar)
}

/// <summary>
/// Ekipman slot turleri
/// </summary>
public enum EquipmentSlot
{
    None,           // Ekipman degil
    WeaponMod1,     // Birincil silah modu
    WeaponMod2,     // Ikincil silah modu
    Accessory1,     // Aksesuar 1 (yuzuk, kolye)
    Accessory2,     // Aksesuar 2
    Armor           // Zirh
}

/// <summary>
/// ItemRarity yardimci metodlari
/// </summary>
public static class ItemRarityHelper
{
    /// <summary>
    /// Rarity rengini al
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.8f, 0.8f, 0.8f),      // Beyaz/Gri
            ItemRarity.Uncommon => new Color(0.3f, 1f, 0.3f),      // Yesil
            ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),          // Mavi
            ItemRarity.Epic => new Color(0.7f, 0.3f, 1f),          // Mor
            ItemRarity.Legendary => new Color(1f, 0.6f, 0f),       // Turuncu
            _ => Color.white
        };
    }

    /// <summary>
    /// Neon glow rengi (daha parlak)
    /// </summary>
    public static Color GetGlowColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(1f, 1f, 1f, 0.3f),
            ItemRarity.Uncommon => new Color(0f, 1f, 0.5f, 0.5f),
            ItemRarity.Rare => new Color(0f, 0.8f, 1f, 0.6f),
            ItemRarity.Epic => new Color(0.8f, 0f, 1f, 0.7f),
            ItemRarity.Legendary => new Color(1f, 0.8f, 0f, 0.8f),
            _ => Color.clear
        };
    }

    /// <summary>
    /// Stat carpani al
    /// </summary>
    public static float GetStatMultiplier(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => 1.0f,
            ItemRarity.Uncommon => 1.15f,
            ItemRarity.Rare => 1.3f,
            ItemRarity.Epic => 1.5f,
            ItemRarity.Legendary => 2.0f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Turkce rarity ismi
    /// </summary>
    public static string GetRarityName(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "Siradan",
            ItemRarity.Uncommon => "Yaygin Olmayan",
            ItemRarity.Rare => "Nadir",
            ItemRarity.Epic => "Destansi",
            ItemRarity.Legendary => "Efsanevi",
            _ => "Bilinmeyen"
        };
    }

    /// <summary>
    /// Ingilizce rarity ismi
    /// </summary>
    public static string GetRarityNameEN(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "Common",
            ItemRarity.Uncommon => "Uncommon",
            ItemRarity.Rare => "Rare",
            ItemRarity.Epic => "Epic",
            ItemRarity.Legendary => "Legendary",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Drop sans agirligi (dusuk = daha nadir)
    /// </summary>
    public static float GetDropWeight(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => 50f,
            ItemRarity.Uncommon => 30f,
            ItemRarity.Rare => 15f,
            ItemRarity.Epic => 4f,
            ItemRarity.Legendary => 1f,
            _ => 0f
        };
    }

    /// <summary>
    /// Satis degeri carpani
    /// </summary>
    public static int GetSellValueMultiplier(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => 1,
            ItemRarity.Uncommon => 3,
            ItemRarity.Rare => 10,
            ItemRarity.Epic => 25,
            ItemRarity.Legendary => 100,
            _ => 1
        };
    }

    /// <summary>
    /// Rastgele rarity sec (agirlikli)
    /// </summary>
    public static ItemRarity RollRarity(float bonusLuckPercent = 0f)
    {
        float totalWeight = 0f;
        float[] weights = new float[5];

        for (int i = 0; i < 5; i++)
        {
            ItemRarity r = (ItemRarity)i;
            float weight = GetDropWeight(r);

            // Luck bonusu nadir itemlerin sansini artirir
            if (r >= ItemRarity.Rare)
            {
                weight *= (1f + bonusLuckPercent / 100f);
            }

            weights[i] = weight;
            totalWeight += weight;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < 5; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                return (ItemRarity)i;
            }
        }

        return ItemRarity.Common;
    }

    /// <summary>
    /// Kategori Turkce ismi
    /// </summary>
    public static string GetCategoryName(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Consumable => "Tuketilebilir",
            ItemCategory.Equipment => "Ekipman",
            ItemCategory.Material => "Malzeme",
            ItemCategory.SetPiece => "Set Parcasi",
            ItemCategory.Special => "Ozel",
            _ => "Bilinmeyen"
        };
    }

    /// <summary>
    /// Ekipman slot ismi
    /// </summary>
    public static string GetSlotName(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.WeaponMod1 => "Silah Modu 1",
            EquipmentSlot.WeaponMod2 => "Silah Modu 2",
            EquipmentSlot.Accessory1 => "Aksesuar 1",
            EquipmentSlot.Accessory2 => "Aksesuar 2",
            EquipmentSlot.Armor => "Zirh",
            _ => "Yok"
        };
    }
}
