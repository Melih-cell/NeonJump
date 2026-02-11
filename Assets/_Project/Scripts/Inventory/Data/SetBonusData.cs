using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Set bonus tier verisi
/// </summary>
[Serializable]
public class SetBonusTier
{
    public int requiredPieces;
    public string bonusDescription;
    public float damageBonus;
    public float speedBonus;
    public float defenseBonus;
    public float critBonus;
    public float dropRateBonus;
    public bool hasSpecialEffect;
    public string specialEffectId;
}

/// <summary>
/// Set bonus tanimlari
/// </summary>
[CreateAssetMenu(fileName = "SetBonusData", menuName = "NeonJump/Set Bonus Data")]
public class SetBonusData : ScriptableObject
{
    public string setId;
    public string setName;
    public string setDescription;
    public Color setColor = Color.cyan;
    public ItemType[] setPieces;
    public List<SetBonusTier> tierBonuses;

    /// <summary>
    /// Aktif bonus tier'i al
    /// </summary>
    public SetBonusTier GetActiveBonus(int equippedCount)
    {
        SetBonusTier activeTier = null;

        foreach (var tier in tierBonuses)
        {
            if (equippedCount >= tier.requiredPieces)
            {
                if (activeTier == null || tier.requiredPieces > activeTier.requiredPieces)
                {
                    activeTier = tier;
                }
            }
        }

        return activeTier;
    }

    /// <summary>
    /// Tum tier'lari al (UI icin)
    /// </summary>
    public List<SetBonusTierDisplay> GetAllTiers(int equippedCount)
    {
        var result = new List<SetBonusTierDisplay>();

        foreach (var tier in tierBonuses)
        {
            result.Add(new SetBonusTierDisplay
            {
                tier = tier,
                isActive = equippedCount >= tier.requiredPieces
            });
        }

        return result;
    }
}

/// <summary>
/// UI gosterimi icin tier bilgisi
/// </summary>
public class SetBonusTierDisplay
{
    public SetBonusTier tier;
    public bool isActive;
}

/// <summary>
/// Set bonus yonetimi (statik)
/// </summary>
public static class SetBonusManager
{
    // Varsayilan set bonuslari
    private static Dictionary<string, SetBonusDefinition> defaultSets;

    static SetBonusManager()
    {
        InitializeDefaultSets();
    }

    static void InitializeDefaultSets()
    {
        defaultSets = new Dictionary<string, SetBonusDefinition>();

        // Neon Warrior Set
        defaultSets["NeonWarrior"] = new SetBonusDefinition
        {
            setId = "NeonWarrior",
            setName = "Neon Savasci Seti",
            setColor = new Color(0f, 1f, 1f),
            tiers = new List<SetBonusTier>
            {
                new SetBonusTier
                {
                    requiredPieces = 2,
                    bonusDescription = "+10% Hasar",
                    damageBonus = 0.1f
                },
                new SetBonusTier
                {
                    requiredPieces = 3,
                    bonusDescription = "Oldurmelerde Neon Patlamasi",
                    damageBonus = 0.1f,
                    hasSpecialEffect = true,
                    specialEffectId = "NeonBurst"
                }
            }
        };

        // Shadow Hunter Set
        defaultSets["ShadowHunter"] = new SetBonusDefinition
        {
            setId = "ShadowHunter",
            setName = "Golge Avcisi Seti",
            setColor = new Color(0.3f, 0f, 0.5f),
            tiers = new List<SetBonusTier>
            {
                new SetBonusTier
                {
                    requiredPieces = 2,
                    bonusDescription = "+15% Hiz",
                    speedBonus = 0.15f
                },
                new SetBonusTier
                {
                    requiredPieces = 3,
                    bonusDescription = "Dash sirasinda gorunmezlik",
                    speedBonus = 0.15f,
                    hasSpecialEffect = true,
                    specialEffectId = "InvisibleDash"
                }
            }
        };

        // Void Walker Set
        defaultSets["VoidWalker"] = new SetBonusDefinition
        {
            setId = "VoidWalker",
            setName = "Bosluk Gezgini Seti",
            setColor = new Color(0.5f, 0f, 1f),
            tiers = new List<SetBonusTier>
            {
                new SetBonusTier
                {
                    requiredPieces = 2,
                    bonusDescription = "+20% Kritik Sans",
                    critBonus = 0.2f
                },
                new SetBonusTier
                {
                    requiredPieces = 3,
                    bonusDescription = "Olumden 1 kez geri don",
                    critBonus = 0.2f,
                    hasSpecialEffect = true,
                    specialEffectId = "DeathSave"
                }
            }
        };
    }

    /// <summary>
    /// Aktif tier'i al
    /// </summary>
    public static SetBonusTier GetActiveTier(string setId, int equippedCount)
    {
        if (!defaultSets.TryGetValue(setId, out var setDef))
            return null;

        SetBonusTier activeTier = null;

        foreach (var tier in setDef.tiers)
        {
            if (equippedCount >= tier.requiredPieces)
            {
                if (activeTier == null || tier.requiredPieces > activeTier.requiredPieces)
                {
                    activeTier = tier;
                }
            }
        }

        return activeTier;
    }

    /// <summary>
    /// Set bilgisini al
    /// </summary>
    public static SetBonusDefinition GetSetDefinition(string setId)
    {
        if (defaultSets.TryGetValue(setId, out var def))
            return def;
        return null;
    }

    /// <summary>
    /// Tum setleri al
    /// </summary>
    public static Dictionary<string, SetBonusDefinition> GetAllSets()
    {
        return new Dictionary<string, SetBonusDefinition>(defaultSets);
    }

    /// <summary>
    /// Set ismini al
    /// </summary>
    public static string GetSetName(string setId)
    {
        if (defaultSets.TryGetValue(setId, out var def))
            return def.setName;
        return setId;
    }

    /// <summary>
    /// Set rengini al
    /// </summary>
    public static Color GetSetColor(string setId)
    {
        if (defaultSets.TryGetValue(setId, out var def))
            return def.setColor;
        return Color.white;
    }

    /// <summary>
    /// Ozel efekt aktif mi kontrol et
    /// </summary>
    public static bool IsSpecialEffectActive(string setId, string effectId, int equippedCount)
    {
        var tier = GetActiveTier(setId, equippedCount);
        if (tier == null) return false;
        return tier.hasSpecialEffect && tier.specialEffectId == effectId;
    }
}

/// <summary>
/// Set bonus tanimi
/// </summary>
public class SetBonusDefinition
{
    public string setId;
    public string setName;
    public Color setColor;
    public List<SetBonusTier> tiers;
}
