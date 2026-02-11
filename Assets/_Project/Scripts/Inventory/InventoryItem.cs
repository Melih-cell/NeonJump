using UnityEngine;

/// <summary>
/// Envanter eşya türleri
/// </summary>
public enum ItemType
{
    HealthPotion,       // Can iksiri - 1 can verir
    Shield,             // Kalkan - 5 saniye hasar almaz
    SpeedBoost,         // Hiz - 8 saniye hizli kosar
    DoubleDamage,       // Cift hasar - 10 saniye
    Magnet,             // Miknatis - 10 saniye coin ceker
    Bomb,               // Bomba - Yakindaki dusmanlari oldurur
    ExtraLife,          // Ekstra can - Max cani 1 artirir

    // Malzemeler (Materials)
    ScrapMetal,         // Hurda metal - temel crafting malzemesi
    NeonCrystal,        // Neon kristali - orta seviye malzeme
    VoidEssence,        // Bosluk ozu - ileri seviye malzeme
    PlasmaCore,         // Plazma cekirdegi - nadir malzeme
    EternalShard,       // Ebedi parcasi - epic malzeme

    // Tuketilebilir (Consumables)
    ManaPotion,         // Enerji iksiri

    // Ekipman (Equipment)
    DamageBooster,      // Hasar artirici
    SpeedRing,          // Hiz yuzugu
    FireRateModule,     // Ates hizi modulu
    MagnetCore,         // Miknatis cekirdegi
    LuckyCharm,         // Sans tilsimi
    VampireAmulet,      // Vampir muskasi
    ShieldGenerator,    // Kalkan jeneratoru

    // Set Parcalari (Set Pieces)
    NeonWarriorHelm,    // Neon Savasci Migrferi
    ShadowHunterMask,   // Golge Avcisi Maskesi
    VoidWalkerCrown,    // Bosluk Gezgini Taci

    // Yeni Ekipman (New Equipment)
    StaminaPotion,      // Stamina iksiri
    GrenadePack,        // Bomba paketi
    TeleportOrb,        // Isinlanma kuyresi
    ReviveToken,        // Dirilis jetonu
    CriticalLens,       // Kritik merceği
    VampireFangs,       // Vampir disleri
    ReflectShield,      // Yansitma kalkani
    AmmoGenerator,      // Mermi ureticisi
}

/// <summary>
/// Envanter eşyası tanımı
/// </summary>
[System.Serializable]
public class InventoryItem
{
    public ItemType type;
    public string name;
    public string description;
    public Color itemColor;
    public int maxStack;
    public float duration; // Süreli etkiler için

    // Ekipman stat bonuslari
    public float damageBonus;
    public float speedBonus;
    public float defenseBonus;
    public float critBonus;
    public float dropRateBonus;

    // Satis ve kategori
    public int baseSellValue = 10;
    public ItemCategory category = ItemCategory.Consumable;
    public EquipmentSlot equipSlot = EquipmentSlot.None;

    // Set sistemi
    public string setId = "";

    /// <summary>
    /// Ekipman olarak takilabilir mi?
    /// </summary>
    public bool IsEquippable()
    {
        return category == ItemCategory.Equipment || category == ItemCategory.SetPiece;
    }

    /// <summary>
    /// Tuketilebilir mi?
    /// </summary>
    public bool IsConsumable()
    {
        return category == ItemCategory.Consumable;
    }

    /// <summary>
    /// Set parcasi mi?
    /// </summary>
    public bool IsSetPiece()
    {
        return !string.IsNullOrEmpty(setId);
    }

    public static InventoryItem Create(ItemType type)
    {
        InventoryItem item = new InventoryItem();
        item.type = type;

        switch (type)
        {
            case ItemType.HealthPotion:
                item.name = "Can Iksiri";
                item.description = "1 can yeniler";
                item.itemColor = new Color(1f, 0.3f, 0.3f); // Kırmızı
                item.maxStack = 5;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 10;
                break;

            case ItemType.Shield:
                item.name = "Kalkan";
                item.description = "5 sn hasar almaz";
                item.itemColor = new Color(0.3f, 0.5f, 1f); // Mavi
                item.maxStack = 3;
                item.duration = 5f;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 25;
                break;

            case ItemType.SpeedBoost:
                item.name = "Hiz Gucu";
                item.description = "8 sn hizli kosar";
                item.itemColor = new Color(1f, 1f, 0.3f); // Sarı
                item.maxStack = 3;
                item.duration = 8f;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 20;
                break;

            case ItemType.DoubleDamage:
                item.name = "Guc Artisi";
                item.description = "10 sn cift hasar";
                item.itemColor = new Color(1f, 0.5f, 0f); // Turuncu
                item.maxStack = 3;
                item.duration = 10f;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 30;
                break;

            case ItemType.Magnet:
                item.name = "Miknatis";
                item.description = "10 sn coin ceker";
                item.itemColor = new Color(0.8f, 0.2f, 0.8f); // Mor
                item.maxStack = 3;
                item.duration = 10f;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 15;
                break;

            case ItemType.Bomb:
                item.name = "Bomba";
                item.description = "Yakin dusmanlari oldurur";
                item.itemColor = new Color(0.2f, 0.2f, 0.2f); // Siyah
                item.maxStack = 3;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 50;
                break;

            case ItemType.ExtraLife:
                item.name = "Ekstra Can";
                item.description = "Max cani 1 artirir";
                item.itemColor = new Color(0.3f, 1f, 0.5f); // Yeşil
                item.maxStack = 2;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 100;
                break;

            // === MALZEMELER ===
            case ItemType.ScrapMetal:
                item.name = "Hurda Metal";
                item.description = "Temel crafting malzemesi";
                item.itemColor = new Color(0.6f, 0.6f, 0.6f);
                item.maxStack = 99;
                item.category = ItemCategory.Material;
                item.baseSellValue = 5;
                break;

            case ItemType.NeonCrystal:
                item.name = "Neon Kristali";
                item.description = "Orta seviye malzeme, neon isigi yayar";
                item.itemColor = new Color(0f, 1f, 1f);
                item.maxStack = 50;
                item.category = ItemCategory.Material;
                item.baseSellValue = 15;
                break;

            case ItemType.VoidEssence:
                item.name = "Bosluk Ozu";
                item.description = "Ileri seviye malzeme, karanliktan cikarilir";
                item.itemColor = new Color(0.3f, 0f, 0.5f);
                item.maxStack = 30;
                item.category = ItemCategory.Material;
                item.baseSellValue = 40;
                break;

            case ItemType.PlasmaCore:
                item.name = "Plazma Cekirdegi";
                item.description = "Nadir malzeme, yuksek enerji barindiriyor";
                item.itemColor = new Color(1f, 0.2f, 0.8f);
                item.maxStack = 20;
                item.category = ItemCategory.Material;
                item.baseSellValue = 80;
                break;

            case ItemType.EternalShard:
                item.name = "Ebedi Parcasi";
                item.description = "Efsanevi malzeme, zamani buker";
                item.itemColor = new Color(1f, 0.85f, 0f);
                item.maxStack = 10;
                item.category = ItemCategory.Material;
                item.baseSellValue = 200;
                break;

            // === TUKETILEBILIR ===
            case ItemType.ManaPotion:
                item.name = "Enerji Iksiri";
                item.description = "Skill cooldown'larini sifirlar";
                item.itemColor = new Color(0.2f, 0.4f, 1f);
                item.maxStack = 5;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 20;
                break;

            // === EKIPMAN ===
            case ItemType.DamageBooster:
                item.name = "Hasar Artirici";
                item.description = "Silah hasarini %15 artirir";
                item.itemColor = new Color(1f, 0.3f, 0.2f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.WeaponMod1;
                item.damageBonus = 0.15f;
                item.baseSellValue = 150;
                break;

            case ItemType.SpeedRing:
                item.name = "Hiz Yuzugu";
                item.description = "Hareket hizini %12 artirir";
                item.itemColor = new Color(0.3f, 1f, 0.5f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Accessory1;
                item.speedBonus = 0.12f;
                item.baseSellValue = 120;
                break;

            case ItemType.FireRateModule:
                item.name = "Ates Hizi Modulu";
                item.description = "Ates hizini %20 artirir";
                item.itemColor = new Color(1f, 0.6f, 0f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.WeaponMod2;
                item.damageBonus = 0.08f;
                item.critBonus = 0.05f;
                item.baseSellValue = 180;
                break;

            case ItemType.MagnetCore:
                item.name = "Miknatis Cekirdegi";
                item.description = "Item toplama menzilini artirir, %10 drop sansi";
                item.itemColor = new Color(0.8f, 0.2f, 0.8f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Accessory2;
                item.dropRateBonus = 0.10f;
                item.baseSellValue = 100;
                break;

            case ItemType.LuckyCharm:
                item.name = "Sans Tilsimi";
                item.description = "Kritik vurus sansini %8, drop oranini %15 artirir";
                item.itemColor = new Color(0.2f, 1f, 0.2f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Accessory1;
                item.critBonus = 0.08f;
                item.dropRateBonus = 0.15f;
                item.baseSellValue = 200;
                break;

            case ItemType.VampireAmulet:
                item.name = "Vampir Muskasi";
                item.description = "Dusman olumlerinde %5 can yenileme sansi";
                item.itemColor = new Color(0.6f, 0f, 0.1f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Accessory2;
                item.damageBonus = 0.05f;
                item.defenseBonus = 0.05f;
                item.baseSellValue = 250;
                break;

            case ItemType.ShieldGenerator:
                item.name = "Kalkan Jeneratoru";
                item.description = "Savunmayi %18 artirir, hasar suresini uzatir";
                item.itemColor = new Color(0.3f, 0.5f, 1f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Armor;
                item.defenseBonus = 0.18f;
                item.baseSellValue = 220;
                break;

            // === SET PARCALARI ===
            case ItemType.NeonWarriorHelm:
                item.name = "Neon Savasci Migrferi";
                item.description = "Neon Savasci setinin bas parcasi";
                item.itemColor = new Color(0f, 1f, 1f);
                item.maxStack = 1;
                item.category = ItemCategory.SetPiece;
                item.equipSlot = EquipmentSlot.Armor;
                item.setId = "neon_warrior";
                item.damageBonus = 0.10f;
                item.speedBonus = 0.05f;
                item.baseSellValue = 300;
                break;

            case ItemType.ShadowHunterMask:
                item.name = "Golge Avcisi Maskesi";
                item.description = "Golge Avcisi setinin bas parcasi";
                item.itemColor = new Color(0.4f, 0f, 0.6f);
                item.maxStack = 1;
                item.category = ItemCategory.SetPiece;
                item.equipSlot = EquipmentSlot.Accessory1;
                item.setId = "shadow_hunter";
                item.critBonus = 0.12f;
                item.speedBonus = 0.08f;
                item.baseSellValue = 350;
                break;

            case ItemType.VoidWalkerCrown:
                item.name = "Bosluk Gezgini Taci";
                item.description = "Bosluk Gezgini setinin bas parcasi";
                item.itemColor = new Color(0.2f, 0f, 0.4f);
                item.maxStack = 1;
                item.category = ItemCategory.SetPiece;
                item.equipSlot = EquipmentSlot.Accessory2;
                item.setId = "void_walker";
                item.defenseBonus = 0.15f;
                item.dropRateBonus = 0.10f;
                item.baseSellValue = 400;
                break;

            // === YENI EKIPMAN TIPLERI ===
            case ItemType.StaminaPotion:
                item.name = "Stamina Iksiri";
                item.description = "Tum skill cooldown'larini aninda sifirlar";
                item.itemColor = new Color(0f, 1f, 0.6f);
                item.maxStack = 3;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 35;
                break;

            case ItemType.GrenadePack:
                item.name = "Bomba Paketi";
                item.description = "3 adet guclu bomba icerir";
                item.itemColor = new Color(0.3f, 0.3f, 0.3f);
                item.maxStack = 5;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 60;
                break;

            case ItemType.TeleportOrb:
                item.name = "Isinlanma Kuyresi";
                item.description = "Kisa mesafe isinlanma, engelleri gecer";
                item.itemColor = new Color(0.5f, 0f, 1f);
                item.maxStack = 3;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 75;
                break;

            case ItemType.ReviveToken:
                item.name = "Dirilis Jetonu";
                item.description = "Olumde otomatik dirilis, tam can";
                item.itemColor = new Color(1f, 0.85f, 0f);
                item.maxStack = 1;
                item.duration = 0;
                item.category = ItemCategory.Consumable;
                item.baseSellValue = 500;
                break;

            case ItemType.CriticalLens:
                item.name = "Kritik Mercegi";
                item.description = "Kritik vurus sansini %15 artirir";
                item.itemColor = new Color(1f, 1f, 0.3f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.WeaponMod1;
                item.critBonus = 0.15f;
                item.baseSellValue = 200;
                break;

            case ItemType.VampireFangs:
                item.name = "Vampir Disleri";
                item.description = "Her oldurme %8 can yeniler, hasar %5 artar";
                item.itemColor = new Color(0.8f, 0f, 0.2f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Accessory1;
                item.damageBonus = 0.05f;
                item.defenseBonus = 0.03f;
                item.baseSellValue = 280;
                break;

            case ItemType.ReflectShield:
                item.name = "Yansitma Kalkani";
                item.description = "Savunma %20, dusmanlar hasarinin %10 geri alir";
                item.itemColor = new Color(0.5f, 0.8f, 1f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.Armor;
                item.defenseBonus = 0.20f;
                item.baseSellValue = 320;
                break;

            case ItemType.AmmoGenerator:
                item.name = "Mermi Ureticisi";
                item.description = "Her 10 saniyede otomatik mermi uretir";
                item.itemColor = new Color(1f, 0.5f, 0f);
                item.maxStack = 1;
                item.category = ItemCategory.Equipment;
                item.equipSlot = EquipmentSlot.WeaponMod2;
                item.damageBonus = 0.03f;
                item.baseSellValue = 180;
                break;
        }

        return item;
    }
}

/// <summary>
/// Envanter slotu - bir eşya türü ve miktarı
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemType itemType;
    public int count;

    public InventorySlot(ItemType type, int amount = 1)
    {
        itemType = type;
        count = amount;
    }
}
