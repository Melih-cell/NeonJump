using UnityEngine;

/// <summary>
/// Silah türleri
/// </summary>
public enum WeaponType
{
    Pistol,         // Tabanca - başlangıç silahı
    Rifle,          // Tüfek - dengeli
    Shotgun,        // Pompalı - yakın mesafe, yayılım
    SMG,            // Hafif makineli - hızlı ateş
    Sniper,         // Keskin nişancı - yüksek hasar
    RocketLauncher, // Roketatar - patlama hasarı
    Flamethrower,   // Alev silahı - sürekli hasar
    GrenadeLauncher // Bombaatar - alan hasarı
}

/// <summary>
/// Silah nadirlik seviyesi
/// </summary>
public enum WeaponRarity
{
    Common,     // Beyaz - x1.0 stat
    Uncommon,   // Yeşil - x1.15 stat
    Rare,       // Mavi - x1.3 stat
    Epic,       // Mor - x1.5 stat
    Legendary   // Turuncu - x1.8 stat
}

/// <summary>
/// Rarity yardımcı sınıfı
/// </summary>
public static class WeaponRarityHelper
{
    /// <summary>
    /// Rarity'ye göre stat çarpanı
    /// </summary>
    public static float GetStatMultiplier(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Common: return 1.0f;
            case WeaponRarity.Uncommon: return 1.15f;
            case WeaponRarity.Rare: return 1.3f;
            case WeaponRarity.Epic: return 1.5f;
            case WeaponRarity.Legendary: return 1.8f;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// Rarity'ye göre renk
    /// </summary>
    public static Color GetRarityColor(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Common: return new Color(0.8f, 0.8f, 0.8f);      // Beyaz/Gri
            case WeaponRarity.Uncommon: return new Color(0.3f, 0.9f, 0.3f);    // Yeşil
            case WeaponRarity.Rare: return new Color(0.3f, 0.5f, 1f);          // Mavi
            case WeaponRarity.Epic: return new Color(0.7f, 0.3f, 0.9f);        // Mor
            case WeaponRarity.Legendary: return new Color(1f, 0.6f, 0.1f);     // Turuncu
            default: return Color.white;
        }
    }

    /// <summary>
    /// Rarity Türkçe ismi
    /// </summary>
    public static string GetRarityName(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Common: return "Siradan";
            case WeaponRarity.Uncommon: return "Yaygin";
            case WeaponRarity.Rare: return "Nadir";
            case WeaponRarity.Epic: return "Epik";
            case WeaponRarity.Legendary: return "Efsanevi";
            default: return "Bilinmiyor";
        }
    }

    /// <summary>
    /// Rastgele rarity döndür (ağırlıklı)
    /// </summary>
    public static WeaponRarity GetRandomRarity()
    {
        float roll = Random.value * 100f;

        // Common: 50%, Uncommon: 25%, Rare: 15%, Epic: 8%, Legendary: 2%
        if (roll < 50f) return WeaponRarity.Common;
        if (roll < 75f) return WeaponRarity.Uncommon;
        if (roll < 90f) return WeaponRarity.Rare;
        if (roll < 98f) return WeaponRarity.Epic;
        return WeaponRarity.Legendary;
    }

    /// <summary>
    /// Upgrade maliyeti (coin)
    /// </summary>
    public static int GetUpgradeCost(int currentLevel, WeaponRarity rarity)
    {
        int baseCost = 50 + (currentLevel * 75);
        float rarityMultiplier = 1f + ((int)rarity * 0.5f);
        return Mathf.RoundToInt(baseCost * rarityMultiplier);
    }

    /// <summary>
    /// Level başına stat artışı
    /// </summary>
    public static float GetLevelMultiplier(int level)
    {
        // Level 1 = 1.0, Level 2 = 1.1, Level 3 = 1.2, Level 4 = 1.35, Level 5 = 1.5
        switch (level)
        {
            case 1: return 1.0f;
            case 2: return 1.1f;
            case 3: return 1.2f;
            case 4: return 1.35f;
            case 5: return 1.5f;
            default: return 1.0f;
        }
    }
}

/// <summary>
/// Silah kategorisi
/// </summary>
public enum WeaponCategory
{
    Primary,    // Ana silah (Rifle, Shotgun, SMG, Sniper)
    Secondary,  // İkincil silah (Pistol)
    Special     // Özel silah (Rocket, Flamethrower, Grenade)
}

/// <summary>
/// Silah verileri
/// </summary>
[System.Serializable]
public class WeaponData
{
    public WeaponType type;
    public string weaponName;
    public WeaponCategory category;

    [Header("Combat Stats")]
    public int damage = 10;
    public float fireRate = 0.3f;          // Atışlar arası süre
    public float bulletSpeed = 20f;
    public float range = 15f;

    [Header("Ammo")]
    public int maxAmmo = 30;               // Şarjör kapasitesi
    public int maxReserveAmmo = 90;        // Yedek mermi
    public float reloadTime = 1.5f;

    [Header("Spread & Bullets")]
    public float spread = 0f;              // Yayılım açısı
    public int bulletsPerShot = 1;         // Tek atışta mermi sayısı
    public bool isAutomatic = false;       // Otomatik mi?

    [Header("Special")]
    public bool hasExplosion = false;
    public float explosionRadius = 0f;
    public bool isPiercing = false;        // Mermiler düşmanları deler mi

    [Header("Visual")]
    public Color bulletColor = Color.yellow;
    public Color muzzleFlashColor = Color.white;

    /// <summary>
    /// Varsayılan silah verisi oluştur
    /// </summary>
    public static WeaponData Create(WeaponType type)
    {
        WeaponData data = new WeaponData();
        data.type = type;

        switch (type)
        {
            case WeaponType.Pistol:
                data.weaponName = "Tabanca";
                data.category = WeaponCategory.Secondary;
                data.damage = 15;
                data.fireRate = 0.4f;
                data.bulletSpeed = 18f;
                data.range = 12f;
                data.maxAmmo = 12;
                data.maxReserveAmmo = 48;
                data.reloadTime = 1.2f;
                data.spread = 2f;
                data.bulletsPerShot = 1;
                data.isAutomatic = false;
                data.bulletColor = new Color(1f, 0.9f, 0.3f);
                break;

            case WeaponType.Rifle:
                data.weaponName = "Tufek";
                data.category = WeaponCategory.Primary;
                data.damage = 25;
                data.fireRate = 0.15f;
                data.bulletSpeed = 25f;
                data.range = 20f;
                data.maxAmmo = 30;
                data.maxReserveAmmo = 120;
                data.reloadTime = 2f;
                data.spread = 3f;
                data.bulletsPerShot = 1;
                data.isAutomatic = true;
                data.bulletColor = new Color(1f, 0.8f, 0.2f);
                break;

            case WeaponType.Shotgun:
                data.weaponName = "Pompali";
                data.category = WeaponCategory.Primary;
                data.damage = 12; // Her mermi için
                data.fireRate = 0.8f;
                data.bulletSpeed = 15f;
                data.range = 8f;
                data.maxAmmo = 8;
                data.maxReserveAmmo = 32;
                data.reloadTime = 2.5f;
                data.spread = 15f;
                data.bulletsPerShot = 6;
                data.isAutomatic = false;
                data.bulletColor = new Color(1f, 0.5f, 0.2f);
                break;

            case WeaponType.SMG:
                data.weaponName = "Makineli";
                data.category = WeaponCategory.Primary;
                data.damage = 12;
                data.fireRate = 0.08f;
                data.bulletSpeed = 22f;
                data.range = 15f;
                data.maxAmmo = 40;
                data.maxReserveAmmo = 160;
                data.reloadTime = 1.8f;
                data.spread = 5f;
                data.bulletsPerShot = 1;
                data.isAutomatic = true;
                data.bulletColor = new Color(1f, 1f, 0.4f);
                break;

            case WeaponType.Sniper:
                data.weaponName = "Keskin Nisanci";
                data.category = WeaponCategory.Primary;
                data.damage = 100;
                data.fireRate = 1.2f;
                data.bulletSpeed = 50f;
                data.range = 40f;
                data.maxAmmo = 5;
                data.maxReserveAmmo = 20;
                data.reloadTime = 3f;
                data.spread = 0f;
                data.bulletsPerShot = 1;
                data.isAutomatic = false;
                data.isPiercing = true;
                data.bulletColor = new Color(0.3f, 0.8f, 1f);
                break;

            case WeaponType.RocketLauncher:
                data.weaponName = "Roketatar";
                data.category = WeaponCategory.Special;
                data.damage = 80;
                data.fireRate = 1.5f;
                data.bulletSpeed = 12f;
                data.range = 25f;
                data.maxAmmo = 1;
                data.maxReserveAmmo = 5;
                data.reloadTime = 2.5f;
                data.spread = 0f;
                data.bulletsPerShot = 1;
                data.isAutomatic = false;
                data.hasExplosion = true;
                data.explosionRadius = 3f;
                data.bulletColor = new Color(1f, 0.3f, 0.1f);
                break;

            case WeaponType.Flamethrower:
                data.weaponName = "Alev Makinesi";
                data.category = WeaponCategory.Special;
                data.damage = 5;
                data.fireRate = 0.05f;
                data.bulletSpeed = 8f;
                data.range = 6f;
                data.maxAmmo = 100;
                data.maxReserveAmmo = 200;
                data.reloadTime = 3f;
                data.spread = 10f;
                data.bulletsPerShot = 1;
                data.isAutomatic = true;
                data.bulletColor = new Color(1f, 0.5f, 0f);
                break;

            case WeaponType.GrenadeLauncher:
                data.weaponName = "Bombaatar";
                data.category = WeaponCategory.Special;
                data.damage = 60;
                data.fireRate = 1f;
                data.bulletSpeed = 10f;
                data.range = 15f;
                data.maxAmmo = 6;
                data.maxReserveAmmo = 18;
                data.reloadTime = 2.8f;
                data.spread = 0f;
                data.bulletsPerShot = 1;
                data.isAutomatic = false;
                data.hasExplosion = true;
                data.explosionRadius = 2.5f;
                data.bulletColor = new Color(0.2f, 0.8f, 0.2f);
                break;
        }

        return data;
    }
}

/// <summary>
/// Oyuncunun taşıdığı silah instance'ı
/// </summary>
[System.Serializable]
public class WeaponInstance
{
    public WeaponData data;
    public int currentAmmo;
    public int reserveAmmo;
    public bool isUnlocked;

    // Rarity ve Upgrade sistemi
    public WeaponRarity rarity;
    public int level = 1;
    public const int MaxLevel = 5;

    public WeaponInstance(WeaponType type)
    {
        data = WeaponData.Create(type);
        currentAmmo = data.maxAmmo;
        reserveAmmo = data.maxReserveAmmo;
        isUnlocked = false;
        rarity = WeaponRarity.Common;
        level = 1;
    }

    public WeaponInstance(WeaponType type, WeaponRarity weaponRarity)
    {
        data = WeaponData.Create(type);
        rarity = weaponRarity;
        level = 1;
        currentAmmo = GetEffectiveMaxAmmo();
        reserveAmmo = GetEffectiveMaxReserve();
        isUnlocked = false;
    }

    // === EFFECTIVE STATS (Rarity + Level bonusları) ===

    /// <summary>
    /// Toplam stat çarpanı (Rarity x Level)
    /// </summary>
    public float GetTotalMultiplier()
    {
        return WeaponRarityHelper.GetStatMultiplier(rarity) * WeaponRarityHelper.GetLevelMultiplier(level);
    }

    /// <summary>
    /// Efektif hasar (Rarity + Level bonuslu)
    /// </summary>
    public int GetEffectiveDamage()
    {
        return Mathf.RoundToInt(data.damage * GetTotalMultiplier());
    }

    /// <summary>
    /// Efektif ateş hızı (daha düşük = daha hızlı)
    /// </summary>
    public float GetEffectiveFireRate()
    {
        // Fire rate düşerse daha hızlı ateş eder
        float reduction = (GetTotalMultiplier() - 1f) * 0.3f; // %30 etki
        return Mathf.Max(data.fireRate * (1f - reduction), data.fireRate * 0.5f);
    }

    /// <summary>
    /// Efektif şarjör kapasitesi
    /// </summary>
    public int GetEffectiveMaxAmmo()
    {
        float bonus = 1f + ((level - 1) * 0.15f); // Her level %15 bonus
        return Mathf.RoundToInt(data.maxAmmo * bonus);
    }

    /// <summary>
    /// Efektif yedek mermi kapasitesi
    /// </summary>
    public int GetEffectiveMaxReserve()
    {
        float bonus = 1f + ((level - 1) * 0.2f); // Her level %20 bonus
        return Mathf.RoundToInt(data.maxReserveAmmo * bonus);
    }

    /// <summary>
    /// Efektif reload süresi (daha düşük = daha hızlı)
    /// </summary>
    public float GetEffectiveReloadTime()
    {
        float reduction = (level - 1) * 0.1f; // Her level %10 hızlanma
        return Mathf.Max(data.reloadTime * (1f - reduction), data.reloadTime * 0.5f);
    }

    /// <summary>
    /// Efektif menzil
    /// </summary>
    public float GetEffectiveRange()
    {
        return data.range * (1f + ((int)rarity * 0.1f));
    }

    // === UPGRADE SİSTEMİ ===

    /// <summary>
    /// Upgrade yapılabilir mi?
    /// </summary>
    public bool CanUpgrade()
    {
        return level < MaxLevel;
    }

    /// <summary>
    /// Upgrade maliyeti
    /// </summary>
    public int GetUpgradeCost()
    {
        return WeaponRarityHelper.GetUpgradeCost(level, rarity);
    }

    /// <summary>
    /// Upgrade yap
    /// </summary>
    public bool TryUpgrade(ref int playerCoins)
    {
        if (!CanUpgrade()) return false;

        int cost = GetUpgradeCost();
        if (playerCoins < cost) return false;

        playerCoins -= cost;
        level++;

        // Yeni şarjör kapasitesine göre mermi güncelle
        int newMaxAmmo = GetEffectiveMaxAmmo();
        int newMaxReserve = GetEffectiveMaxReserve();

        // Mevcut mermiyi koru, yeni maksimuma göre sınırla
        currentAmmo = Mathf.Min(currentAmmo, newMaxAmmo);
        reserveAmmo = Mathf.Min(reserveAmmo, newMaxReserve);

        return true;
    }

    // === TEMEL FONKSİYONLAR ===

    public bool CanFire()
    {
        return currentAmmo > 0;
    }

    public bool CanReload()
    {
        return currentAmmo < GetEffectiveMaxAmmo() && reserveAmmo > 0;
    }

    public void Fire()
    {
        if (currentAmmo > 0)
            currentAmmo--;
    }

    public void Reload()
    {
        int maxAmmo = GetEffectiveMaxAmmo();
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToAdd = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToAdd;
        reserveAmmo -= ammoToAdd;
    }

    public void AddAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, GetEffectiveMaxReserve());
    }

    /// <summary>
    /// Silah bilgi metni
    /// </summary>
    public string GetDisplayName()
    {
        string rarityName = WeaponRarityHelper.GetRarityName(rarity);
        return $"{rarityName} {data.weaponName} +{level}";
    }

    /// <summary>
    /// Detaylı stat bilgisi
    /// </summary>
    public string GetStatsInfo()
    {
        return $"Hasar: {GetEffectiveDamage()} | Hiz: {GetEffectiveFireRate():F2}s | Sarjor: {GetEffectiveMaxAmmo()}";
    }
}
