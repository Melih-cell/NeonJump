using UnityEngine;
using System.Collections;
#if UNITY_2D_LIGHTING
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Dunyada drop edilen loot gorunumu.
/// Rarity'ye gore farkli gorsel efektler.
/// </summary>
public class LootDropVisual : MonoBehaviour
{
    [Header("Item Bilgisi")]
    public ItemType itemType;
    public ItemRarity rarity = ItemRarity.Common;
    public int amount = 1;

    [Header("Gorsel Ayarlari")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer glowRenderer;
    public ParticleSystem rarityParticles;
    // Light2D URP 2D Lighting paketi gerektirir - opsiyonel
    // public Light2D pointLight;

    [Header("Animasyon")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.1f;
    public float rotationSpeed = 30f;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.2f;

    [Header("Pickup")]
    public float pickupRadius = 1f;
    public float magnetRadius = 3f;
    public float magnetSpeed = 8f;
    public bool canBeMagneted = true;

    [Header("Spawn Animasyonu")]
    public float spawnJumpForce = 5f;
    public float spawnGravity = 20f;
    public float groundY = 0f;
    public bool hasLanded = false;

    [Header("Lifetime")]
    public float lifetime = 30f;
    public float blinkStartTime = 25f;
    public float blinkSpeed = 10f;

    // Cached
    private Transform playerTransform;
    private Vector3 startPosition;
    private Vector3 velocity;
    private float spawnTime;
    private bool isBeingMagneted = false;
    private Collider2D pickupCollider;

    // Rarity colors
    private static readonly Color CommonColor = new Color(0.9f, 0.9f, 0.9f);
    private static readonly Color UncommonColor = new Color(0.2f, 1f, 0.2f);
    private static readonly Color RareColor = new Color(0.2f, 0.5f, 1f);
    private static readonly Color EpicColor = new Color(0.7f, 0.2f, 1f);
    private static readonly Color LegendaryColor = new Color(1f, 0.6f, 0f);

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        spawnTime = Time.time;

        // Player referansi
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Sprite ayarla
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = GetItemSprite();
        }

        // Collider
        pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = pickupRadius;
            col.isTrigger = true;
            pickupCollider = col;
        }

        // Rarity efektlerini ayarla
        SetupRarityVisuals();

        // Spawn animasyonu baslat
        StartCoroutine(SpawnAnimation());
    }

    void SetupRarityVisuals()
    {
        Color rarityColor = GetRarityColor();

        // Glow
        if (glowRenderer != null)
        {
            glowRenderer.color = rarityColor;
            glowRenderer.gameObject.SetActive(rarity >= ItemRarity.Uncommon);
        }

        // Particles
        if (rarityParticles != null)
        {
            var main = rarityParticles.main;
            main.startColor = rarityColor;

            if (rarity >= ItemRarity.Epic)
            {
                rarityParticles.Play();
            }
            else
            {
                rarityParticles.Stop();
            }
        }

        // Legendary ozel efekt
        if (rarity == ItemRarity.Legendary)
        {
            StartCoroutine(LegendaryBeamEffect());
        }
    }

    Color GetRarityColor()
    {
        return rarity switch
        {
            ItemRarity.Common => CommonColor,
            ItemRarity.Uncommon => UncommonColor,
            ItemRarity.Rare => RareColor,
            ItemRarity.Epic => EpicColor,
            ItemRarity.Legendary => LegendaryColor,
            _ => CommonColor
        };
    }

    Sprite GetItemSprite()
    {
        // InventorySprites'dan sprite al
        if (InventorySprites.Instance != null)
        {
            return InventorySprites.Instance.GetSprite(itemType);
        }

        return null;
    }

    void Update()
    {
        if (!hasLanded) return;

        UpdateBobAnimation();
        UpdateMagnet();
        UpdateLifetime();
        UpdateRarityEffects();
    }

    void UpdateBobAnimation()
    {
        // Yukari asagi hareket
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = startPosition + Vector3.up * bob;

        // Rare+ icin rotation
        if (rarity >= ItemRarity.Rare)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    void UpdateMagnet()
    {
        if (!canBeMagneted || playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // Magnet alaninda mi?
        bool hasMagnet = EquipmentManager.Instance != null &&
                        EquipmentManager.Instance.HasItemEquipped(ItemType.MagnetCore);

        float effectiveRadius = hasMagnet ? magnetRadius * 2f : magnetRadius;

        if (dist < effectiveRadius)
        {
            isBeingMagneted = true;
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            float speed = magnetSpeed * (1f - dist / effectiveRadius);
            transform.position += dir * speed * Time.deltaTime;
            startPosition = transform.position;
        }

        // Pickup mesafesi
        if (dist < pickupRadius)
        {
            Pickup();
        }
    }

    void UpdateLifetime()
    {
        float elapsed = Time.time - spawnTime;

        // Blink efekti
        if (elapsed > blinkStartTime)
        {
            float blink = Mathf.Sin(elapsed * blinkSpeed);
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = blink > 0 ? 1f : 0.3f;
                spriteRenderer.color = color;
            }
        }

        // Yok ol
        if (elapsed > lifetime)
        {
            Destroy(gameObject);
        }
    }

    void UpdateRarityEffects()
    {
        // Pulse efekti (Epic+)
        if (rarity >= ItemRarity.Epic && glowRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float scale = 1f + pulse * pulseAmount;
            glowRenderer.transform.localScale = Vector3.one * scale;
        }
    }

    IEnumerator SpawnAnimation()
    {
        // Rastgele yon
        float angle = Random.Range(45f, 135f) * Mathf.Deg2Rad;
        velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnJumpForce;

        // Carpan ekleme (rastgele)
        velocity.x *= Random.Range(-1f, 1f);

        while (!hasLanded)
        {
            // Yercekimi
            velocity.y -= spawnGravity * Time.deltaTime;

            // Hareket
            transform.position += velocity * Time.deltaTime;

            // Yere degdi mi?
            if (transform.position.y <= groundY)
            {
                var pos = transform.position;
                pos.y = groundY;
                transform.position = pos;
                hasLanded = true;
                startPosition = transform.position;

                // Landing efekti
                SpawnLandingEffect();
            }

            yield return null;
        }
    }

    void SpawnLandingEffect()
    {
        // Toz partikulleri
        if (rarity >= ItemRarity.Rare)
        {
            // TODO: Landing particle effect
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoin();
        }
    }

    IEnumerator LegendaryBeamEffect()
    {
        // Legendary item icin ozel efekt
        while (this != null && gameObject != null)
        {
            // Glow pulse
            if (glowRenderer != null)
            {
                float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
                float scale = 1.5f + pulse * 0.3f;
                glowRenderer.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }
    }

    void Pickup()
    {
        // Envantere ekle
        if (InventoryManager.Instance != null)
        {
            // Yeni sistem ile
            var item = InventoryItemInstance.Create(itemType, rarity, amount);
            bool added = InventoryManager.Instance.TryAddItemInstance(item);

            if (!added)
            {
                // Envanter dolu - geri don
                Debug.Log("[LootDropVisual] Envanter dolu, item alinamadi");
                return;
            }
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            // Rarity'ye gore farkli ses
            if (rarity >= ItemRarity.Epic)
            {
                AudioManager.Instance.PlayPowerUp();
            }
            else
            {
                AudioManager.Instance.PlayCoin();
            }
        }

        // Pickup efekti
        SpawnPickupEffect();

        // Yok et
        Destroy(gameObject);
    }

    void SpawnPickupEffect()
    {
        // TODO: Pickup particle effect based on rarity
        Color color = GetRarityColor();

        // Basit scale animasyonu
        // Particle system spawn edebilir
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && hasLanded)
        {
            Pickup();
        }
    }

    /// <summary>
    /// Factory: Yeni loot drop olustur
    /// </summary>
    public static LootDropVisual Create(ItemType type, ItemRarity rarity, int amount, Vector3 position)
    {
        var go = new GameObject($"LootDrop_{type}_{rarity}");
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Collectibles");

        var drop = go.AddComponent<LootDropVisual>();
        drop.itemType = type;
        drop.rarity = rarity;
        drop.amount = amount;

        // Sprite renderer
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Items";
        sr.sortingOrder = 10;
        drop.spriteRenderer = sr;

        // Glow (child object)
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(go.transform);
        glowGo.transform.localPosition = Vector3.zero;
        glowGo.transform.localScale = Vector3.one * 1.5f;

        var glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sortingLayerName = "Items";
        glowSr.sortingOrder = 9;
        glowSr.color = new Color(1, 1, 1, 0.5f);
        drop.glowRenderer = glowSr;

        // Collider
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = drop.pickupRadius;
        col.isTrigger = true;

        return drop;
    }

    /// <summary>
    /// Factory: LootResult'dan olustur
    /// </summary>
    public static LootDropVisual Create(LootResult result, Vector3 position)
    {
        return Create(result.itemType, result.rarity, result.amount, position);
    }
}
