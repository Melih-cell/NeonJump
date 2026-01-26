using UnityEngine;

/// <summary>
/// Silah drop sistemi - Dusmanlar olunce silah dusurur
/// Pickup edildiginde oyuncuya silah verir
/// </summary>
public class WeaponDrop : MonoBehaviour
{
    [Header("Weapon Info")]
    public WeaponType weaponType;
    public WeaponRarity rarity;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer glowRenderer;

    [Header("Animation")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.15f;
    public float rotateSpeed = 30f;
    public float glowPulseSpeed = 3f;

    private Vector3 startPosition;
    private float bobOffset;
    private bool isPickedUp = false;

    // Drop spawn ayarlari
    public static float dropChance = 0.15f;          // %15 drop sansi
    public static float rareDropChanceBonus = 0.05f; // Boss'lar icin +%5

    void Start()
    {
        startPosition = transform.position;
        bobOffset = Random.value * Mathf.PI * 2f; // Rastgele baslangic fazı

        // Sprite yoksa olustur
        if (spriteRenderer == null)
        {
            CreateVisuals();
        }

        // 30 saniye sonra yok ol (toplanmazsa)
        Destroy(gameObject, 30f);
    }

    void CreateVisuals()
    {
        // Ana sprite
        GameObject spriteObj = new GameObject("WeaponSprite");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;

        spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = WeaponSpriteLoader.GetWeaponIcon(weaponType);
        spriteRenderer.sortingOrder = 15;

        // Rarity'ye gore renk tonu
        Color rarityColor = WeaponRarityHelper.GetRarityColor(rarity);
        spriteRenderer.color = Color.Lerp(Color.white, rarityColor, 0.3f);

        // Glow efekti (Rare ve ustu icin)
        if (rarity >= WeaponRarity.Rare)
        {
            CreateGlowEffect(rarityColor);
        }

        // Collider
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;
    }

    void CreateGlowEffect(Color color)
    {
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * 2f;

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sortingOrder = 14;

        // Glow texture olustur
        Texture2D glowTex = CreateGlowTexture(32, color);
        glowRenderer.sprite = Sprite.Create(glowTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        glowRenderer.color = new Color(color.r, color.g, color.b, 0.5f);
    }

    Texture2D CreateGlowTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha * alpha; // Daha yumusak
                colors[y * size + x] = new Color(color.r, color.g, color.b, alpha);
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (isPickedUp) return;

        // Bobbing animasyonu
        float bob = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
        transform.position = startPosition + Vector3.up * bob;

        // Rotasyon (sadece sprite)
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        // Glow pulse
        if (glowRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) * 0.3f) + 0.5f;
            Color c = glowRenderer.color;
            glowRenderer.color = new Color(c.r, c.g, c.b, pulse);

            // Glow da donsun (ters yone)
            glowRenderer.transform.Rotate(0, 0, -rotateSpeed * 0.5f * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;

        // Player mi?
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            PickUp();
        }
    }

    void PickUp()
    {
        isPickedUp = true;

        // WeaponManager'a silahi ekle
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.AddWeapon(weaponType, rarity);
        }

        // Efekt
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayPowerUpCollect(transform.position);
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPowerUp();
        }

        // Yok ol
        Destroy(gameObject);
    }

    // === STATIC SPAWN METODLARI ===

    /// <summary>
    /// Rastgele silah drop'u olustur
    /// </summary>
    public static WeaponDrop SpawnRandomDrop(Vector3 position)
    {
        // Drop sansi kontrolu
        if (Random.value > dropChance) return null;

        // Rastgele silah tipi (Pistol haric)
        WeaponType[] dropableWeapons = {
            WeaponType.Rifle,
            WeaponType.Shotgun,
            WeaponType.SMG,
            WeaponType.Sniper,
            WeaponType.RocketLauncher,
            WeaponType.Flamethrower,
            WeaponType.GrenadeLauncher
        };

        WeaponType randomType = dropableWeapons[Random.Range(0, dropableWeapons.Length)];
        WeaponRarity randomRarity = WeaponRarityHelper.GetRandomRarity();

        return SpawnDrop(position, randomType, randomRarity);
    }

    /// <summary>
    /// Belirli silah drop'u olustur
    /// </summary>
    public static WeaponDrop SpawnDrop(Vector3 position, WeaponType type, WeaponRarity rarity)
    {
        GameObject dropObj = new GameObject($"WeaponDrop_{type}_{rarity}");
        dropObj.transform.position = position + Vector3.up * 0.5f; // Biraz yukari

        WeaponDrop drop = dropObj.AddComponent<WeaponDrop>();
        drop.weaponType = type;
        drop.rarity = rarity;

        // Hafif yukari firlat
        Rigidbody2D rb = dropObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(Random.Range(-2f, 2f), Random.Range(3f, 5f));

        // 1 saniye sonra fizigi kapat (yerde kalsin)
        drop.StartCoroutine(drop.DisablePhysicsAfterDelay(1f, rb));

        return drop;
    }

    /// <summary>
    /// Boss drop'u - garanti ve daha iyi rarity
    /// </summary>
    public static WeaponDrop SpawnBossDrop(Vector3 position)
    {
        // Boss'lar her zaman silah dusurur
        WeaponType[] bossWeapons = {
            WeaponType.Sniper,
            WeaponType.RocketLauncher,
            WeaponType.Flamethrower,
            WeaponType.GrenadeLauncher
        };

        WeaponType type = bossWeapons[Random.Range(0, bossWeapons.Length)];

        // Boss'lardan daha iyi rarity
        WeaponRarity rarity = GetBossRarity();

        return SpawnDrop(position, type, rarity);
    }

    static WeaponRarity GetBossRarity()
    {
        float roll = Random.value * 100f;

        // Rare: 40%, Epic: 40%, Legendary: 20%
        if (roll < 40f) return WeaponRarity.Rare;
        if (roll < 80f) return WeaponRarity.Epic;
        return WeaponRarity.Legendary;
    }

    System.Collections.IEnumerator DisablePhysicsAfterDelay(float delay, Rigidbody2D rb)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
            startPosition = transform.position;
        }
    }
}
