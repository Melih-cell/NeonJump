using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Merkezi UI sprite yukleme sistemi.
/// Figma'dan olusturulan silah, upgrade ve achievement ikonlarini yukler.
/// Resources/UI/ klasorundan runtime'da yukleme yapar.
/// </summary>
public static class UIAssetLoader
{
    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    // === SILAH IKONLARI ===

    private static readonly Dictionary<WeaponType, string> weaponIconPaths = new Dictionary<WeaponType, string>
    {
        { WeaponType.Pistol, "UI/Weapons/WeaponIcon_Pistol" },
        { WeaponType.Rifle, "UI/Weapons/WeaponIcon_Rifle" },
        { WeaponType.Shotgun, "UI/Weapons/WeaponIcon_Shotgun" },
        { WeaponType.SMG, "UI/Weapons/WeaponIcon_SMG" },
        { WeaponType.Sniper, "UI/Weapons/WeaponIcon_Sniper" },
        { WeaponType.RocketLauncher, "UI/Weapons/WeaponIcon_Rocket_Launcher" },
        { WeaponType.Flamethrower, "UI/Weapons/WeaponIcon_Flamethrower" },
        { WeaponType.GrenadeLauncher, "UI/Weapons/WeaponIcon_Grenade_Launcher" }
    };

    /// <summary>
    /// Silah ikonu yukle - Figma tasarimi
    /// </summary>
    public static Sprite GetWeaponIcon(WeaponType type)
    {
        if (!weaponIconPaths.TryGetValue(type, out string path))
            return null;

        return LoadSprite(path);
    }

    // === UPGRADE IKONLARI ===

    private static readonly Dictionary<string, string> upgradeIconPaths = new Dictionary<string, string>
    {
        { "health", "UI/Upgrades/UpgradeIcon_Health" },
        { "speed", "UI/Upgrades/UpgradeIcon_Speed" },
        { "dash", "UI/Upgrades/UpgradeIcon_Dash" },
        { "jump", "UI/Upgrades/UpgradeIcon_Jump" },
        { "damage", "UI/Upgrades/UpgradeIcon_Damage" }
    };

    /// <summary>
    /// Upgrade ikonu yukle - Figma tasarimi
    /// </summary>
    public static Sprite GetUpgradeIcon(string upgradeId)
    {
        if (!upgradeIconPaths.TryGetValue(upgradeId, out string path))
            return null;

        return LoadSprite(path);
    }

    // === ACHIEVEMENT IKONLARI ===

    private static readonly Dictionary<string, string> achievementIconPaths = new Dictionary<string, string>
    {
        { "ilk_adim", "UI/Achievements/Achievement_Ilk_Adim" },
        { "dusman_avcisi", "UI/Achievements/Achievement_Dusman_Avcisi" },
        { "boss_katili", "UI/Achievements/Achievement_Boss_Katili" },
        { "zengin", "UI/Achievements/Achievement_Zengin" },
        { "combo_ustasi", "UI/Achievements/Achievement_Combo_Ustasi" },
        { "koleksiyoncu", "UI/Achievements/Achievement_Koleksiyoncu" },
        { "hayatta_kalan", "UI/Achievements/Achievement_Hayatta_Kalan" },
        { "efsane", "UI/Achievements/Achievement_Efsane" }
    };

    /// <summary>
    /// Achievement ikonu yukle - Figma tasarimi
    /// </summary>
    public static Sprite GetAchievementIcon(string achievementId)
    {
        if (!achievementIconPaths.TryGetValue(achievementId, out string path))
            return null;

        return LoadSprite(path);
    }

    /// <summary>
    /// Tum achievement ID'lerini dondur
    /// </summary>
    public static string[] GetAllAchievementIds()
    {
        string[] ids = new string[achievementIconPaths.Count];
        achievementIconPaths.Keys.CopyTo(ids, 0);
        return ids;
    }

    // === YARDIMCI ===

    private static Sprite LoadSprite(string resourcePath)
    {
        if (_cache.TryGetValue(resourcePath, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);

        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
        }

        _cache[resourcePath] = sprite;
        return sprite;
    }

    /// <summary>
    /// Cache temizle
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
}
