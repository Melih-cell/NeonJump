using UnityEngine;

/// <summary>
/// Envanter eşya türleri
/// </summary>
public enum ItemType
{
    HealthPotion,   // Can iksiri - 1 can verir
    Shield,         // Kalkan - 5 saniye hasar almaz
    SpeedBoost,     // Hız - 8 saniye hızlı koşar
    DoubleDamage,   // Çift hasar - 10 saniye
    Magnet,         // Mıknatıs - 10 saniye coin çeker
    Bomb,           // Bomba - Yakındaki düşmanları öldürür
    ExtraLife       // Ekstra can - Max canı 1 artırır
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
                break;

            case ItemType.Shield:
                item.name = "Kalkan";
                item.description = "5 sn hasar almaz";
                item.itemColor = new Color(0.3f, 0.5f, 1f); // Mavi
                item.maxStack = 3;
                item.duration = 5f;
                break;

            case ItemType.SpeedBoost:
                item.name = "Hiz Gucu";
                item.description = "8 sn hizli kosar";
                item.itemColor = new Color(1f, 1f, 0.3f); // Sarı
                item.maxStack = 3;
                item.duration = 8f;
                break;

            case ItemType.DoubleDamage:
                item.name = "Guc Artisi";
                item.description = "10 sn cift hasar";
                item.itemColor = new Color(1f, 0.5f, 0f); // Turuncu
                item.maxStack = 3;
                item.duration = 10f;
                break;

            case ItemType.Magnet:
                item.name = "Miknatis";
                item.description = "10 sn coin ceker";
                item.itemColor = new Color(0.8f, 0.2f, 0.8f); // Mor
                item.maxStack = 3;
                item.duration = 10f;
                break;

            case ItemType.Bomb:
                item.name = "Bomba";
                item.description = "Yakin dusmanlari oldurur";
                item.itemColor = new Color(0.2f, 0.2f, 0.2f); // Siyah
                item.maxStack = 3;
                item.duration = 0;
                break;

            case ItemType.ExtraLife:
                item.name = "Ekstra Can";
                item.description = "Max cani 1 artirir";
                item.itemColor = new Color(0.3f, 1f, 0.5f); // Yeşil
                item.maxStack = 2;
                item.duration = 0;
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
