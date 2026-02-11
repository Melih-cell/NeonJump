using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dusman loot drop komponenti.
/// Enemy'e eklendiginde olum aninda loot drop eder.
/// </summary>
public class EnemyLootComponent : MonoBehaviour
{
    [Header("Loot Ayarlari")]
    [Tooltip("ScriptableObject loot tablosu (opsiyonel)")]
    public LootTableSO lootTable;

    [Tooltip("Dusman tipi (varsayilan tablo icin)")]
    public EnemyLootType enemyType = EnemyLootType.Basic;

    [Tooltip("Ekstra drop sans bonusu")]
    [Range(0f, 100f)]
    public float bonusDropChance = 0f;

    [Header("Coin Ayarlari")]
    public bool dropCoins = true;
    public int minCoins = 1;
    public int maxCoins = 5;

    [Header("Drop Pozisyonu")]
    public Vector3 dropOffset = Vector3.zero;
    public float dropSpreadRadius = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Cached
    private bool hasDropped = false;

    /// <summary>
    /// Loot drop et (disaridan cagrilir, ornegin Enemy.Die())
    /// </summary>
    public void DropLoot()
    {
        if (hasDropped) return;
        hasDropped = true;

        // Player luck bonusu
        float playerLuck = GetPlayerLuckBonus();

        // Loot roll
        List<LootResult> lootResults;

        if (lootTable != null)
        {
            // ScriptableObject tablosu kullan
            lootResults = lootTable.RollLoot(playerLuck + bonusDropChance);
        }
        else
        {
            // Varsayilan tablo kullan
            lootResults = RollDefaultLoot(playerLuck + bonusDropChance);
        }

        // Loot'lari spawn et
        Vector3 basePosition = transform.position + dropOffset;

        foreach (var loot in lootResults)
        {
            SpawnLootDrop(loot, basePosition);
        }

        // Coin drop
        if (dropCoins)
        {
            int coinAmount = Random.Range(minCoins, maxCoins + 1);
            SpawnCoins(coinAmount, basePosition);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemyLootComponent] {gameObject.name} dropped {lootResults.Count} items");
        }
    }

    List<LootResult> RollDefaultLoot(float bonusLuck)
    {
        return enemyType switch
        {
            EnemyLootType.Basic => DefaultLootTables.RollBasicEnemy(bonusLuck),
            EnemyLootType.Elite => DefaultLootTables.RollEliteEnemy(bonusLuck),
            EnemyLootType.MiniBoss => DefaultLootTables.RollEliteEnemy(bonusLuck + 20f),
            EnemyLootType.Boss => DefaultLootTables.RollBoss(bonusLuck),
            _ => new List<LootResult>()
        };
    }

    void SpawnLootDrop(LootResult loot, Vector3 basePosition)
    {
        // Rastgele offset
        Vector3 offset = Random.insideUnitCircle * dropSpreadRadius;
        Vector3 spawnPos = basePosition + offset;

        // Ground Y bul
        float groundY = FindGroundY(spawnPos);

        // LootDropVisual olustur
        var drop = LootDropVisual.Create(loot.itemType, loot.rarity, loot.amount, spawnPos);
        drop.groundY = groundY;

        if (showDebugInfo)
        {
            Debug.Log($"[EnemyLootComponent] Spawned: {loot}");
        }
    }

    void SpawnCoins(int amount, Vector3 basePosition)
    {
        // Coin'leri tek tek degil topluca spawn et
        if (amount <= 0) return;

        // Coin pickup olustur (CollectibleItem kullan)
        for (int i = 0; i < Mathf.Min(amount, 5); i++) // Max 5 coin objesi
        {
            Vector3 offset = Random.insideUnitCircle * dropSpreadRadius;
            Vector3 spawnPos = basePosition + offset;

            int coinValue = amount / Mathf.Min(amount, 5);
            if (i == 0)
                coinValue += amount % Mathf.Min(amount, 5);

            SpawnCoinPickup(spawnPos, coinValue);
        }
    }

    void SpawnCoinPickup(Vector3 position, int value)
    {
        // Basit coin pickup
        var coinGo = new GameObject("CoinDrop");
        coinGo.transform.position = position;
        coinGo.layer = LayerMask.NameToLayer("Collectibles");

        // Sprite
        var sr = coinGo.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Items";
        sr.sortingOrder = 10;
        sr.color = new Color(1f, 0.85f, 0f); // Gold

        // InventorySprites'dan coin sprite
        if (InventorySprites.Instance != null)
        {
            sr.sprite = InventorySprites.Instance.GetSprite(ItemType.ScrapMetal); // Placeholder
        }

        // Collider
        var col = coinGo.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Coin pickup script
        var coinPickup = coinGo.AddComponent<CoinDropPickup>();
        coinPickup.coinValue = value;

        // Basit fizik
        var rb = coinGo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.AddForce(new Vector2(Random.Range(-2f, 2f), Random.Range(3f, 5f)), ForceMode2D.Impulse);

        // Auto destroy
        Destroy(coinGo, 30f);
    }

    float FindGroundY(Vector3 position)
    {
        // Raycast ile zemin bul
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 10f, LayerMask.GetMask("Ground", "Platform"));

        if (hit.collider != null)
        {
            return hit.point.y;
        }

        return position.y - 1f; // Fallback
    }

    float GetPlayerLuckBonus()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.TotalDropRateBonus * 100f;
        }
        return 0f;
    }

    /// <summary>
    /// Olum eventi icin (Enemy.cs'den cagirilir)
    /// </summary>
    public void OnEnemyDeath()
    {
        DropLoot();
    }

    void OnDestroy()
    {
        // Eger drop edilmediyse ve dusman yok ediliyorsa
        if (!hasDropped)
        {
            // Zorunlu drop? (opsiyonel)
            // DropLoot();
        }
    }
}

/// <summary>
/// Dusman loot tipi
/// </summary>
public enum EnemyLootType
{
    Basic,      // Normal dusman
    Elite,      // Guclu dusman
    MiniBoss,   // Mini boss
    Boss        // Ana boss
}

/// <summary>
/// Basit coin pickup
/// </summary>
public class CoinDropPickup : MonoBehaviour
{
    public int coinValue = 1;
    public float pickupRadius = 0.5f;
    public float magnetRadius = 2f;
    public float magnetSpeed = 10f;

    private Transform playerTransform;
    private bool hasLanded = false;
    private Rigidbody2D rb;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Landing kontrolu
        if (rb != null && rb.linearVelocity.magnitude < 0.1f)
        {
            hasLanded = true;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }

        if (!hasLanded) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // Magnet efekti
        bool hasMagnet = EquipmentManager.Instance != null &&
                        EquipmentManager.Instance.HasItemEquipped(ItemType.MagnetCore);

        float effectiveRadius = hasMagnet ? magnetRadius * 2f : magnetRadius;

        if (dist < effectiveRadius)
        {
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            float speed = magnetSpeed * (1f - dist / effectiveRadius);
            transform.position += dir * speed * Time.deltaTime;
        }

        // Pickup
        if (dist < pickupRadius)
        {
            Pickup();
        }
    }

    void Pickup()
    {
        // Para ekle - GameManager kullan
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin(coinValue);
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoin();
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }
}
