using UnityEngine;

/// <summary>
/// Isinlanan Dusman - Belirli araliklarla isinlanarak oyuncuyu sasirtan dusman
/// </summary>
public class TeleportingEnemy : MonoBehaviour
{
    [Header("Teleport Settings")]
    public float teleportInterval = 3f;
    public float teleportRange = 5f;
    public float teleportCooldownAfterDamage = 1f;
    public float warningTime = 0.5f; // Isinlanmadan once uyari

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 8f;
    public bool followPlayer = true;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int damage = 1;

    [Header("Visuals")]
    public Color normalColor = new Color(0.8f, 0f, 1f); // Mor
    public Color warningColor = new Color(1f, 1f, 0f);  // Sari (uyari)
    public Color teleportColor = new Color(0f, 1f, 1f); // Cyan (isinlanma)

    [Header("Item Drop")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f;
    [Range(0f, 1f)]
    public float weaponDropChance = 0.15f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float teleportTimer;
    private float attackTimer;
    private bool isDead = false;
    private bool isTeleporting = false;
    private Vector3 teleportTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateDefaultSprite();
        }

        // Collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }

        spriteRenderer.color = normalColor;
        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Default");

        teleportTimer = teleportInterval;
        FindPlayer();
    }

    void CreateDefaultSprite()
    {
        // Basit mor/siyah sprite olustur
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];

        // Seffaf arka plan
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Merkez govde (daire benzeri)
        for (int y = 8; y < 24; y++)
        {
            for (int x = 8; x < 24; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                if (dist < 8)
                {
                    pixels[y * size + x] = normalColor;
                }
            }
        }

        // Gozler - beyaz noktalar
        pixels[18 * size + 12] = Color.white;
        pixels[18 * size + 13] = Color.white;
        pixels[18 * size + 19] = Color.white;
        pixels[18 * size + 20] = Color.white;

        // Portal/isinlanma efekti - cevredeki halkalar
        for (int i = 0; i < size; i++)
        {
            float angle = i * Mathf.PI * 2 / size;
            int x = Mathf.RoundToInt(16 + Mathf.Cos(angle) * 12);
            int y = Mathf.RoundToInt(16 + Mathf.Sin(angle) * 12);
            if (x >= 0 && x < size && y >= 0 && y < size)
            {
                pixels[y * size + x] = new Color(0.5f, 0f, 0.8f, 0.5f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    void Update()
    {
        if (isDead) return;

        FindPlayer();
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Isinlanma zamanlayicisi
        teleportTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        if (!isTeleporting)
        {
            // Isinlanma zamani geldi mi?
            if (teleportTimer <= warningTime && teleportTimer > 0)
            {
                // Uyari - renk degisimi
                spriteRenderer.color = Color.Lerp(normalColor, warningColor, (warningTime - teleportTimer) / warningTime);
            }
            else if (teleportTimer <= 0)
            {
                StartTeleport();
            }
            else
            {
                // Normal hareket
                if (followPlayer && distanceToPlayer <= chaseRange && distanceToPlayer > attackRange)
                {
                    MoveTowardsPlayer();
                }

                // Saldiri
                if (distanceToPlayer <= attackRange && attackTimer <= 0)
                {
                    Attack();
                }
            }
        }

        // Pulsing efekti
        float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
        transform.localScale = Vector3.one * pulse;
    }

    void MoveTowardsPlayer()
    {
        float direction = player.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // Sprite flip
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0;
    }

    void StartTeleport()
    {
        isTeleporting = true;

        // Isinlanma hedefi bul - oyuncunun etrafinda rastgele
        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(2f, teleportRange);
        teleportTarget = player.position + new Vector3(randomOffset.x, Mathf.Abs(randomOffset.y), 0);

        // Zemin kontrolu - cok asagi dusmesin
        RaycastHit2D groundCheck = Physics2D.Raycast(teleportTarget, Vector2.down, 10f);
        if (groundCheck.collider != null)
        {
            teleportTarget.y = groundCheck.point.y + 1f;
        }

        // Isinlanma efekti
        StartCoroutine(TeleportSequence());
    }

    System.Collections.IEnumerator TeleportSequence()
    {
        // Kaybolma animasyonu
        float fadeTime = 0.2f;
        float elapsed = 0;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            spriteRenderer.color = new Color(teleportColor.r, teleportColor.g, teleportColor.b, 1 - t);
            transform.localScale = Vector3.one * (1 - t * 0.5f);
            yield return null;
        }

        // Pozisyonu degistir
        transform.position = teleportTarget;

        // Particle efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayJumpDust(transform.position);
        }

        // Belirme animasyonu
        elapsed = 0;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            spriteRenderer.color = new Color(normalColor.r, normalColor.g, normalColor.b, t);
            transform.localScale = Vector3.one * (0.5f + t * 0.5f);
            yield return null;
        }

        spriteRenderer.color = normalColor;
        transform.localScale = Vector3.one;
        isTeleporting = false;
        teleportTimer = teleportInterval;
    }

    void Attack()
    {
        // Oyuncuya hasar ver
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage();
        }
        attackTimer = attackCooldown;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Esya dusur
        TryDropItem();

        // Skor
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyKilled(transform.position);
        }

        // Olum efekti
        StartCoroutine(DeathSequence());
    }

    void TryDropItem()
    {
        Vector3 dropPos = transform.position + Vector3.up * 0.5f;

        // Silah dusurme
        if (Random.value <= weaponDropChance)
        {
            WeaponType[] possibleWeapons = new WeaponType[]
            {
                WeaponType.SMG,
                WeaponType.Shotgun,
                WeaponType.RocketLauncher
            };

            WeaponType weaponType = possibleWeapons[Random.Range(0, possibleWeapons.Length)];
            WeaponPickup.Spawn(weaponType, dropPos);
            return;
        }

        // Normal esya dusurme
        if (Random.value <= dropChance)
        {
            // Mermi veya saglik
            if (Random.value < 0.5f)
            {
                AmmoPickup.Spawn(dropPos);
            }
            else
            {
                ItemType[] items = { ItemType.HealthPotion, ItemType.Shield, ItemType.SpeedBoost };
                CollectibleItem.Spawn(items[Random.Range(0, items.Length)], dropPos);
            }
        }
    }

    System.Collections.IEnumerator DeathSequence()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Patlama efekti - parcalara ayrilma
        float deathTime = 0.5f;
        float elapsed = 0;

        while (elapsed < deathTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathTime;

            // Buyuyerek kaybol
            transform.localScale = Vector3.one * (1 + t);
            spriteRenderer.color = new Color(teleportColor.r, teleportColor.g, teleportColor.b, 1 - t);

            yield return null;
        }

        Destroy(gameObject);
    }

    void FindPlayer()
    {
        if (player != null) return;

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            return;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Oyuncu ustunden atladiysa ol
        if (collision.gameObject.CompareTag("Player"))
        {
            float enemyTop = transform.position.y + 0.3f;
            float playerBottom = collision.transform.position.y - 0.5f;

            if (playerBottom > enemyTop)
            {
                Die();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, teleportRange);
    }
}
