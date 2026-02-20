using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// İndirilen silah sprite'larını yükler ve yönetir
/// Sprite'lar Assets/Resources/Weapons klasöründen yüklenir
/// </summary>
public static class WeaponSpriteLoader
{
    // Cache
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, Sprite[]> spritesheetCache = new Dictionary<string, Sprite[]>();
    private static bool isInitialized = false;

    // Sprite yolları (Resources klasörü içinde)
    // Dosya adları: AssaultRifle_Idle.png, Pistol_Idle.png, Shotgun_Idle.png, SniperRifle_Idle.png
    private static readonly Dictionary<WeaponType, string> weaponSpritePaths = new Dictionary<WeaponType, string>
    {
        { WeaponType.Pistol, "Weapons/Pistol" },
        { WeaponType.Rifle, "Weapons/AssaultRifle" },
        { WeaponType.Shotgun, "Weapons/Shotgun" },
        { WeaponType.SMG, "Weapons/AssaultRifle" }, // SMG için assault rifle kullan
        { WeaponType.Sniper, "Weapons/SniperRifle" },
        { WeaponType.RocketLauncher, "Weapons/RocketLauncher" }, // Procedural fallback
        { WeaponType.Flamethrower, "Weapons/Flamethrower" },     // Procedural fallback
        { WeaponType.GrenadeLauncher, "Weapons/GrenadeLauncher" } // Procedural fallback
    };

    /// <summary>
    /// Silah sprite'ı yükle (statik görünüm)
    /// </summary>
    public static Sprite GetWeaponSprite(WeaponType type)
    {
        string path = GetSpritePath(type, "Idle");

        if (spriteCache.TryGetValue(path, out Sprite cached))
            return cached;

        // Resources'dan yüklemeyi dene
        Sprite sprite = LoadSpriteFromResources(path);

        if (sprite == null)
        {
            // Alternatif yolları dene
            sprite = TryAlternativePaths(type);
        }

        if (sprite == null)
        {
            // Fallback: Procedural sprite
            sprite = WeaponVisuals.CreateWeaponSprite(type);
            Debug.Log($"[WeaponSpriteLoader] {type} için Resources sprite bulunamadı, procedural kullanılıyor");
        }
        else
        {
            Debug.Log($"[WeaponSpriteLoader] {type} sprite yüklendi: {path}");
        }

        spriteCache[path] = sprite;
        return sprite;
    }

    /// <summary>
    /// Silah icon'u yükle (UI için) - Figma ikonlari oncelikli
    /// </summary>
    public static Sprite GetWeaponIcon(WeaponType type)
    {
        string path = GetSpritePath(type, "Icon");

        if (spriteCache.TryGetValue(path, out Sprite cached))
            return cached;

        // Figma ikonunu dene
        Sprite sprite = UIAssetLoader.GetWeaponIcon(type);

        if (sprite == null)
        {
            sprite = LoadSpriteFromResources(path);
        }

        if (sprite == null)
        {
            // Idle sprite'ı icon olarak kullan
            sprite = GetWeaponSprite(type);
        }

        spriteCache[path] = sprite;
        return sprite;
    }

    /// <summary>
    /// Shooting animation sprite sheet yükle
    /// </summary>
    public static Sprite[] GetShootingSprites(WeaponType type)
    {
        string path = GetSpritePath(type, "Shooting");

        if (spritesheetCache.TryGetValue(path, out Sprite[] cached))
            return cached;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);

        if (sprites == null || sprites.Length == 0)
        {
            // Tek sprite döndür
            sprites = new Sprite[] { GetWeaponSprite(type) };
        }

        spritesheetCache[path] = sprites;
        return sprites;
    }

    /// <summary>
    /// Muzzle flash sprite yükle
    /// </summary>
    public static Sprite GetMuzzleFlashSprite(WeaponType type)
    {
        string path = GetSpritePath(type, "MuzzleFlash");

        if (spriteCache.TryGetValue(path, out Sprite cached))
            return cached;

        Sprite sprite = LoadSpriteFromResources(path);
        spriteCache[path] = sprite;
        return sprite;
    }

    /// <summary>
    /// Reload animation sprites yükle
    /// </summary>
    public static Sprite[] GetReloadSprites(WeaponType type)
    {
        string path = GetSpritePath(type, "Reload");

        if (spritesheetCache.TryGetValue(path, out Sprite[] cached))
            return cached;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        spritesheetCache[path] = sprites;
        return sprites;
    }

    private static string GetSpritePath(WeaponType type, string suffix)
    {
        if (weaponSpritePaths.TryGetValue(type, out string basePath))
        {
            return $"{basePath}_{suffix}";
        }
        return $"Weapons/{type}_{suffix}";
    }

    private static Sprite LoadSpriteFromResources(string path)
    {
        // Önce direkt yükle
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        // Texture olarak yükleyip sprite oluştur
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
        }

        return null;
    }

    private static Sprite TryAlternativePaths(WeaponType type)
    {
        // Farklı isimlendirme formatlarını dene
        string[] alternativePaths = GetAlternativePaths(type);

        foreach (string path in alternativePaths)
        {
            Sprite sprite = LoadSpriteFromResources(path);
            if (sprite != null)
            {
                Debug.Log($"[WeaponSpriteLoader] Alternatif yol bulundu: {path}");
                return sprite;
            }
        }

        return null;
    }

    private static string[] GetAlternativePaths(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol:
                return new string[] {
                    "Weapons/Pistol",
                    "Weapons/pistol",
                    "Weapons/Gun01"
                };
            case WeaponType.Rifle:
                return new string[] {
                    "Weapons/AssaultRifle",
                    "Weapons/Assault_rifle",
                    "Weapons/Rifle",
                    "Weapons/AK47"
                };
            case WeaponType.Shotgun:
                return new string[] {
                    "Weapons/Shotgun",
                    "Weapons/shotgun"
                };
            case WeaponType.Sniper:
                return new string[] {
                    "Weapons/SniperRifle",
                    "Weapons/Sniper_rifle",
                    "Weapons/Sniper",
                    "Weapons/KAR98"
                };
            default:
                return new string[] { $"Weapons/{type}" };
        }
    }

    /// <summary>
    /// Sprite cache'i temizle
    /// </summary>
    public static void ClearCache()
    {
        spriteCache.Clear();
        spritesheetCache.Clear();
        isInitialized = false;
    }

    /// <summary>
    /// Belirli bir silah tipi için sprite mevcut mu?
    /// </summary>
    public static bool HasSprite(WeaponType type)
    {
        string path = GetSpritePath(type, "Idle");

        if (spriteCache.ContainsKey(path))
            return spriteCache[path] != null;

        Sprite sprite = LoadSpriteFromResources(path);
        return sprite != null || TryAlternativePaths(type) != null;
    }
}
