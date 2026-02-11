using UnityEngine;

/// <summary>
/// Toplanabilir envanter eşyası
/// Oyun dünyasında spawn olur, dokunulduğunda envantere eklenir
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType itemType = ItemType.HealthPotion;
    public int amount = 1;

    [Header("Visual")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;
    public float rotateSpeed = 90f;
    public bool enableBob = true;
    public bool enableRotate = false;

    [Header("Pickup")]
    public float pickupRadius = 0.8f;
    public bool autoPickup = true;

    private Vector3 startPos;
    private SpriteRenderer spriteRenderer;
    private float timeOffset;

    void Start()
    {
        startPos = transform.position;
        timeOffset = Random.value * Mathf.PI * 2f;

        // Sprite oluştur
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Collider ekle
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = pickupRadius;
            col.isTrigger = true;
        }

        // Görsel ayarla
        SetupVisual();
    }

    void SetupVisual()
    {
        InventoryItem itemInfo = InventoryItem.Create(itemType);
        spriteRenderer.color = itemInfo.itemColor;

        // Basit kare sprite (yoksa oluştur)
        if (spriteRenderer.sprite == null)
        {
            Texture2D tex = new Texture2D(16, 16);
            Color[] colors = new Color[16 * 16];

            // Eşya tipine göre basit şekil çiz
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool filled = false;

                    switch (itemType)
                    {
                        case ItemType.HealthPotion:
                            // Şişe şekli
                            filled = (x >= 5 && x <= 10 && y >= 2 && y <= 12) ||
                                     (x >= 6 && x <= 9 && y >= 12 && y <= 14);
                            break;

                        case ItemType.Shield:
                            // Kalkan şekli
                            int cx = 8, cy = 8;
                            float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                            filled = dist < 7 && dist > 4;
                            break;

                        case ItemType.SpeedBoost:
                            // Yıldırım şekli
                            filled = (x >= 6 && x <= 10 && y == 14) ||
                                     (x >= 5 && x <= 8 && y >= 8 && y <= 13) ||
                                     (x >= 6 && x <= 11 && y == 7) ||
                                     (x >= 8 && x <= 11 && y >= 2 && y <= 7);
                            break;

                        case ItemType.Bomb:
                            // Bomba şekli
                            int bcx = 8, bcy = 6;
                            float bdist = Mathf.Sqrt((x - bcx) * (x - bcx) + (y - bcy) * (y - bcy));
                            filled = bdist < 6 || (x >= 7 && x <= 9 && y >= 11 && y <= 14);
                            break;

                        default:
                            // Varsayılan kare
                            filled = x >= 3 && x <= 12 && y >= 3 && y <= 12;
                            break;
                    }

                    colors[y * 16 + x] = filled ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(colors);
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        // Sorting
        spriteRenderer.sortingOrder = 5;
    }

    void Update()
    {
        // Yukarı aşağı hareket
        if (enableBob)
        {
            float newY = startPos.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Dönme
        if (enableRotate)
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoPickup) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            TryPickup();
        }
    }

    public void TryPickup()
    {
        if (InventoryManager.Instance != null)
        {
            bool added = InventoryManager.Instance.AddItem(itemType, amount);

            if (added)
            {
                // Efekt
                if (ParticleManager.Instance != null)
                {
                    ParticleManager.Instance.PlayCoinCollect(transform.position);
                }

                // Ses
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPowerUp();
                }

                // Bildirim goster
                if (NotificationManager.Instance != null)
                {
                    InventoryItem itemInfo = InventoryItem.Create(itemType);
                    NotificationManager.Instance.ShowItemPickup(itemInfo.name);
                }

                // Floating text
                if (FloatingTextManager.Instance != null)
                {
                    FloatingTextManager.Instance.ShowText(transform.position + Vector3.up * 0.5f, "+1", spriteRenderer.color);
                }

                // Degerli esya icin neon patlama efekti (ekipman, set parcasi, nadir malzemeler)
                InventoryItem pickupItemInfo = InventoryItem.Create(itemType);
                if (pickupItemInfo.category == ItemCategory.Equipment ||
                    pickupItemInfo.category == ItemCategory.SetPiece ||
                    itemType == ItemType.PlasmaCore || itemType == ItemType.EternalShard ||
                    itemType == ItemType.VoidEssence)
                {
                    PlayRarityPickupEffect(transform.position);
                }

                Destroy(gameObject);
            }
            else
            {
                // Envanter dolu - eşya alınamadı
                // Belki bir ses veya görsel feedback
            }
        }
    }

    void PlayRarityPickupEffect(Vector3 position)
    {
        // Neon patlama efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayExplosion(position);
        }

        // Ekran sarsmasi (hafif)
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Shake(0.1f, 0.08f);
        }
    }

    /// <summary>
    /// Runtime'da eşya oluştur
    /// </summary>
    public static GameObject Spawn(ItemType type, Vector3 position, int amount = 1)
    {
        GameObject obj = new GameObject("Collectible_" + type.ToString());
        obj.transform.position = position;

        CollectibleItem item = obj.AddComponent<CollectibleItem>();
        item.itemType = type;
        item.amount = amount;

        return obj;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
