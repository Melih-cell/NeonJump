using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Crafting malzemesi
/// </summary>
[Serializable]
public class CraftingIngredient
{
    public ItemType itemType;
    public int amount = 1;
    public ItemRarity minRarity = ItemRarity.Common;

    public override string ToString()
    {
        if (minRarity > ItemRarity.Common)
            return $"{amount}x {ItemRarityHelper.GetRarityName(minRarity)} {itemType}";
        return $"{amount}x {itemType}";
    }
}

/// <summary>
/// Crafting tarifi ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "NeonJump/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Tarif Bilgisi")]
    public string recipeId;
    public string recipeName;
    [TextArea(2, 4)]
    public string description;
    public Sprite recipeIcon;

    [Header("Malzemeler")]
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

    [Header("Sonuc")]
    public ItemType resultItem;
    public int resultAmount = 1;

    [Header("Rarity Ayarlari")]
    [Tooltip("Cikacak item'in rarity'si")]
    public ItemRarity resultRarity = ItemRarity.Common;

    [Tooltip("Rastgele rarity kullan")]
    public bool useRandomRarity = false;

    [Tooltip("Malzeme rarity'lerine gore bonus sans")]
    public bool inheritIngredientRarity = false;

    [Header("Gereksinimler")]
    [Tooltip("Crafting yapilabilmesi icin gerekli oyuncu seviyesi")]
    public int requiredLevel = 1;

    [Tooltip("Crafting maliyeti (coin)")]
    public int craftingCost = 0;

    [Tooltip("Crafting suresi (saniye, 0 = aninda)")]
    public float craftingTime = 0f;

    [Header("Unlock")]
    [Tooltip("Tarif varsayilan olarak acik mi?")]
    public bool unlockedByDefault = true;

    [Tooltip("Acmak icin gerekli item (opsiyonel)")]
    public ItemType unlockItem = ItemType.HealthPotion;

    [Tooltip("Unlock item gerekli mi?")]
    public bool requiresUnlockItem = false;

    /// <summary>
    /// Malzemeler yeterli mi kontrol et
    /// </summary>
    public bool CanCraft(Dictionary<ItemType, int> inventory)
    {
        foreach (var ingredient in ingredients)
        {
            if (!inventory.TryGetValue(ingredient.itemType, out int count))
                return false;

            if (count < ingredient.amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// InventoryItemInstance listesi ile kontrol
    /// </summary>
    public bool CanCraft(List<InventoryItemInstance> items)
    {
        foreach (var ingredient in ingredients)
        {
            int totalCount = 0;

            foreach (var item in items)
            {
                if (item.itemType == ingredient.itemType &&
                    item.rarity >= ingredient.minRarity)
                {
                    totalCount += item.stackCount;
                }
            }

            if (totalCount < ingredient.amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Sonuc rarity'sini hesapla
    /// </summary>
    public ItemRarity GetResultRarity(List<InventoryItemInstance> usedIngredients = null)
    {
        if (!useRandomRarity && !inheritIngredientRarity)
            return resultRarity;

        if (inheritIngredientRarity && usedIngredients != null)
        {
            // Kullanilan malzemelerin ortalama rarity'si
            int totalRarity = 0;
            int count = 0;

            foreach (var item in usedIngredients)
            {
                totalRarity += (int)item.rarity;
                count++;
            }

            if (count > 0)
            {
                int avgRarity = totalRarity / count;
                // Bonus sans
                if (UnityEngine.Random.value < 0.2f)
                    avgRarity++;

                return (ItemRarity)Mathf.Clamp(avgRarity, 0, 4);
            }
        }

        if (useRandomRarity)
        {
            return ItemRarityHelper.RollRarity(10f); // +10% luck bonus for crafting
        }

        return resultRarity;
    }

    /// <summary>
    /// Eksik malzemeleri listele
    /// </summary>
    public List<CraftingIngredient> GetMissingIngredients(List<InventoryItemInstance> items)
    {
        var missing = new List<CraftingIngredient>();

        foreach (var ingredient in ingredients)
        {
            int totalCount = 0;

            foreach (var item in items)
            {
                if (item.itemType == ingredient.itemType &&
                    item.rarity >= ingredient.minRarity)
                {
                    totalCount += item.stackCount;
                }
            }

            if (totalCount < ingredient.amount)
            {
                missing.Add(new CraftingIngredient
                {
                    itemType = ingredient.itemType,
                    amount = ingredient.amount - totalCount,
                    minRarity = ingredient.minRarity
                });
            }
        }

        return missing;
    }
}

/// <summary>
/// Varsayilan crafting tarifleri (kod tabanli)
/// </summary>
public static class DefaultCraftingRecipes
{
    private static Dictionary<string, CraftingRecipeData> recipes;

    static DefaultCraftingRecipes()
    {
        InitializeRecipes();
    }

    static void InitializeRecipes()
    {
        recipes = new Dictionary<string, CraftingRecipeData>();

        // === TEMEL TARIFLER ===

        // Health Potion
        recipes["HealthPotion"] = new CraftingRecipeData
        {
            recipeId = "HealthPotion",
            recipeName = "Saglik Iksiri",
            resultItem = ItemType.HealthPotion,
            resultAmount = 1,
            resultRarity = ItemRarity.Common,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.NeonCrystal, amount = 2 }
            }
        };

        // Mana Potion
        recipes["ManaPotion"] = new CraftingRecipeData
        {
            recipeId = "ManaPotion",
            recipeName = "Enerji Iksiri",
            resultItem = ItemType.ManaPotion,
            resultAmount = 1,
            resultRarity = ItemRarity.Common,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.VoidEssence, amount = 2 }
            }
        };

        // === SILAH MODLARI ===

        // Damage Booster
        recipes["DamageBooster"] = new CraftingRecipeData
        {
            recipeId = "DamageBooster",
            recipeName = "Hasar Artirici",
            resultItem = ItemType.DamageBooster,
            resultAmount = 1,
            resultRarity = ItemRarity.Uncommon,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.ScrapMetal, amount = 5 },
                new CraftingIngredient { itemType = ItemType.PlasmaCore, amount = 1 }
            },
            craftingCost = 50
        };

        // Fire Rate Module
        recipes["FireRateModule"] = new CraftingRecipeData
        {
            recipeId = "FireRateModule",
            recipeName = "Ates Hizi Modulu",
            resultItem = ItemType.FireRateModule,
            resultAmount = 1,
            resultRarity = ItemRarity.Uncommon,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.ScrapMetal, amount = 3 },
                new CraftingIngredient { itemType = ItemType.NeonCrystal, amount = 3 }
            },
            craftingCost = 50
        };

        // === AKSESUARLAR ===

        // Speed Ring
        recipes["SpeedRing"] = new CraftingRecipeData
        {
            recipeId = "SpeedRing",
            recipeName = "Hiz Yuzimu",
            resultItem = ItemType.SpeedRing,
            resultAmount = 1,
            resultRarity = ItemRarity.Rare,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.NeonCrystal, amount = 5 },
                new CraftingIngredient { itemType = ItemType.VoidEssence, amount = 2 }
            },
            craftingCost = 100
        };

        // Vampire Amulet
        recipes["VampireAmulet"] = new CraftingRecipeData
        {
            recipeId = "VampireAmulet",
            recipeName = "Vampir Muskasi",
            resultItem = ItemType.VampireAmulet,
            resultAmount = 1,
            resultRarity = ItemRarity.Epic,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.VoidEssence, amount = 5 },
                new CraftingIngredient { itemType = ItemType.PlasmaCore, amount = 3 },
                new CraftingIngredient { itemType = ItemType.EternalShard, amount = 1 }
            },
            craftingCost = 300
        };

        // Shield Generator
        recipes["ShieldGenerator"] = new CraftingRecipeData
        {
            recipeId = "ShieldGenerator",
            recipeName = "Kalkan Jeneratoru",
            resultItem = ItemType.ShieldGenerator,
            resultAmount = 1,
            resultRarity = ItemRarity.Epic,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.ScrapMetal, amount = 10 },
                new CraftingIngredient { itemType = ItemType.PlasmaCore, amount = 5 },
                new CraftingIngredient { itemType = ItemType.EternalShard, amount = 1 }
            },
            craftingCost = 350
        };

        // Lucky Charm
        recipes["LuckyCharm"] = new CraftingRecipeData
        {
            recipeId = "LuckyCharm",
            recipeName = "Sans Tılsımı",
            resultItem = ItemType.LuckyCharm,
            resultAmount = 1,
            resultRarity = ItemRarity.Rare,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.NeonCrystal, amount = 3 },
                new CraftingIngredient { itemType = ItemType.EternalShard, amount = 1 }
            },
            craftingCost = 150
        };

        // === MALZEME DONUSUMLERI ===

        // Neon Crystal (3 Scrap -> 1 Crystal)
        recipes["NeonCrystalConvert"] = new CraftingRecipeData
        {
            recipeId = "NeonCrystalConvert",
            recipeName = "Neon Kristali Donusumu",
            resultItem = ItemType.NeonCrystal,
            resultAmount = 1,
            resultRarity = ItemRarity.Common,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.ScrapMetal, amount = 3 }
            }
        };

        // Void Essence (2 Crystal -> 1 Void)
        recipes["VoidEssenceConvert"] = new CraftingRecipeData
        {
            recipeId = "VoidEssenceConvert",
            recipeName = "Bosluk Ozu Donusumu",
            resultItem = ItemType.VoidEssence,
            resultAmount = 1,
            resultRarity = ItemRarity.Uncommon,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.NeonCrystal, amount = 2 }
            },
            craftingCost = 25
        };

        // Plasma Core (3 Void -> 1 Plasma)
        recipes["PlasmaCoreConvert"] = new CraftingRecipeData
        {
            recipeId = "PlasmaCoreConvert",
            recipeName = "Plazma Cekirdeği Donusumu",
            resultItem = ItemType.PlasmaCore,
            resultAmount = 1,
            resultRarity = ItemRarity.Rare,
            ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { itemType = ItemType.VoidEssence, amount = 3 }
            },
            craftingCost = 75
        };
    }

    /// <summary>
    /// Tarif al
    /// </summary>
    public static CraftingRecipeData GetRecipe(string recipeId)
    {
        if (recipes.TryGetValue(recipeId, out var recipe))
            return recipe;
        return null;
    }

    /// <summary>
    /// Tum tarifleri al
    /// </summary>
    public static Dictionary<string, CraftingRecipeData> GetAllRecipes()
    {
        return new Dictionary<string, CraftingRecipeData>(recipes);
    }

    /// <summary>
    /// Item icin mevcut tarifleri bul
    /// </summary>
    public static List<CraftingRecipeData> GetRecipesForItem(ItemType itemType)
    {
        var result = new List<CraftingRecipeData>();

        foreach (var recipe in recipes.Values)
        {
            if (recipe.resultItem == itemType)
                result.Add(recipe);
        }

        return result;
    }
}

/// <summary>
/// Kod tabanli tarif verisi
/// </summary>
public class CraftingRecipeData
{
    public string recipeId;
    public string recipeName;
    public ItemType resultItem;
    public int resultAmount = 1;
    public ItemRarity resultRarity = ItemRarity.Common;
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
    public int craftingCost = 0;
    public bool useRandomRarity = false;

    /// <summary>
    /// Malzemeler yeterli mi?
    /// </summary>
    public bool CanCraft(List<InventoryItemInstance> items)
    {
        foreach (var ingredient in ingredients)
        {
            int totalCount = 0;

            foreach (var item in items)
            {
                if (item.itemType == ingredient.itemType &&
                    item.rarity >= ingredient.minRarity)
                {
                    totalCount += item.stackCount;
                }
            }

            if (totalCount < ingredient.amount)
                return false;
        }

        return true;
    }
}
