using UnityEngine;

public class NeonGuardianBoss : Boss
{
    [Header("Phase 1 - Projectile Attack")]
    public float projectileSpeed = 8f;
    public float shootInterval = 2f;
    private float shootTimer;

    [Header("Phase 2 - Dive Attack")]
    public float diveSpeed = 15f;
    public float diveInterval = 4f;
    private float diveTimer;
    private bool isDiving = false;
    private Vector3 diveTarget;
    private float originalY;

    [Header("Phase 3 - Homing Projectile")]
    public float homingSpeed = 6f;
    public float homingLifetime = 5f;

    [Header("Movement")]
    public float floatSpeed = 2f;
    public float floatHeight = 1f;
    private Vector3 startPos;
    private bool movingRight = true;

    // Runtime sprite
    private Sprite bossSprite;

    protected override void Start()
    {
        base.Start();

        bossName = "NEON GUARDIAN";
        maxHealth = 10;
        currentHealth = maxHealth;

        startPos = transform.position;
        originalY = transform.position.y;

        shootTimer = shootInterval;
        diveTimer = diveInterval;

        // Boss sprite olustur
        CreateBossSprite();

        // Rigidbody ayarla
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Collider ekle
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2f);
        }

        // Tag ayarla
        gameObject.tag = "Enemy";

        // UI guncelle
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBossHealthBar(bossName, maxHealth);
            UIManager.Instance.UpdateBossHealth(currentHealth);
        }
    }

    void CreateBossSprite()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 64x64 boss sprite
        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[4096];

        Color bodyColor = new Color(0.8f, 0.2f, 0.8f); // Mor
        Color glowColor = new Color(1f, 0.5f, 1f); // Parlak mor
        Color eyeColor = new Color(1f, 0f, 0f); // Kirmizi gozler

        // Arka plan temizle
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Ana govde - kare seklinde
        for (int y = 10; y < 54; y++)
        {
            for (int x = 10; x < 54; x++)
            {
                // Kenarlar
                if (x == 10 || x == 53 || y == 10 || y == 53)
                {
                    pixels[y * 64 + x] = glowColor;
                }
                else
                {
                    pixels[y * 64 + x] = bodyColor;
                }
            }
        }

        // Gozler - kizgin gorunum
        for (int y = 35; y < 45; y++)
        {
            for (int x = 18; x < 28; x++)
            {
                pixels[y * 64 + x] = eyeColor;
            }
            for (int x = 36; x < 46; x++)
            {
                pixels[y * 64 + x] = eyeColor;
            }
        }

        // Agiz - tehditkar
        for (int y = 18; y < 25; y++)
        {
            for (int x = 22; x < 42; x++)
            {
                // Disler
                if ((x - 22) % 4 < 2 && y < 22)
                {
                    pixels[y * 64 + x] = Color.white;
                }
                else
                {
                    pixels[y * 64 + x] = new Color(0.3f, 0f, 0f);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        bossSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32);
        spriteRenderer.sprite = bossSprite;
        spriteRenderer.sortingOrder = 10;
    }

    protected override void BossBehavior()
    {
        if (player == null) return;

        // Genel hareket - yuzuyor
        FloatMovement();

        // Faza gore saldiri
        switch (currentPhase)
        {
            case 1:
                Phase1Attack();
                break;
            case 2:
                Phase2Attack();
                break;
            case 3:
                Phase3Attack();
                break;
        }

        // Oyuncuya bak
        if (spriteRenderer != null && !isDiving)
        {
            spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    void FloatMovement()
    {
        if (isDiving) return;

        // Yukari asagi hareket
        float newY = originalY + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // Yatay hareket (Phase 2 ve 3'te daha agresif)
        float horizontalSpeed = moveSpeed * (currentPhase == 1 ? 1f : 1.5f);

        if (movingRight)
        {
            transform.position += Vector3.right * horizontalSpeed * Time.deltaTime;
            if (transform.position.x > arenaMaxX)
            {
                movingRight = false;
            }
        }
        else
        {
            transform.position += Vector3.left * horizontalSpeed * Time.deltaTime;
            if (transform.position.x < arenaMinX)
            {
                movingRight = true;
            }
        }

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // PHASE 1: Yatay projectile atisi
    void Phase1Attack()
    {
        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0)
        {
            ShootProjectile();
            shootTimer = shootInterval;
        }
    }

    void ShootProjectile()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;

        // Mermi olustur
        GameObject projectile = new GameObject("BossProjectile");
        projectile.transform.position = transform.position;
        projectile.tag = "EnemyProjectile";

        SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.3f, 1f);
        sr.sortingOrder = 5;

        // Sprite
        Texture2D tex = new Texture2D(12, 12);
        Color[] colors = new Color[144];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 12);

        // Collider
        CircleCollider2D col = projectile.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Rigidbody
        Rigidbody2D projRb = projectile.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0;
        projRb.linearVelocity = direction * projectileSpeed;

        // Projectile script
        Projectile proj = projectile.AddComponent<Projectile>();
        proj.damage = 1;

        Destroy(projectile, 5f);
    }

    // PHASE 2: Dalis saldirisi + shockwave
    void Phase2Attack()
    {
        // Projectile de at (daha yavas)
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            ShootProjectile();
            shootTimer = shootInterval * 1.5f;
        }

        // Dalis saldirisi
        diveTimer -= Time.deltaTime;

        if (!isDiving && diveTimer <= 0)
        {
            StartDive();
        }

        if (isDiving)
        {
            PerformDive();
        }
    }

    void StartDive()
    {
        if (player == null) return;

        isDiving = true;
        diveTarget = new Vector3(player.position.x, -1f, transform.position.z);

        // Renk degistir
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
    }

    void PerformDive()
    {
        transform.position = Vector3.MoveTowards(transform.position, diveTarget, diveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, diveTarget) < 0.5f)
        {
            // Yere carpti - shockwave
            CreateShockwave();

            // Yukari geri don
            isDiving = false;
            diveTimer = diveInterval;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            // Yukari zipla
            if (rb != null)
            {
                rb.linearVelocity = Vector2.up * 10f;
            }

            StartCoroutine(ReturnToOriginalHeight());
        }
    }

    System.Collections.IEnumerator ReturnToOriginalHeight()
    {
        yield return new WaitForSeconds(0.5f);

        while (transform.position.y < originalY - 0.5f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(transform.position.x, originalY, transform.position.z),
                moveSpeed * 2f * Time.deltaTime
            );
            yield return null;
        }
    }

    void CreateShockwave()
    {
        // Sol ve sag yonde iki projectile
        for (int dir = -1; dir <= 1; dir += 2)
        {
            GameObject wave = new GameObject("Shockwave");
            wave.transform.position = new Vector3(transform.position.x, 0.5f, 0);
            wave.tag = "EnemyProjectile";

            SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.5f, 0f, 0.8f);
            sr.sortingOrder = 3;

            Texture2D tex = new Texture2D(32, 8);
            Color[] colors = new Color[256];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 8), new Vector2(0.5f, 0.5f), 16);

            wave.transform.localScale = new Vector3(2f, 1f, 1f);

            BoxCollider2D col = wave.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 0.5f);
            col.isTrigger = true;

            Rigidbody2D waveRb = wave.AddComponent<Rigidbody2D>();
            waveRb.gravityScale = 0;
            waveRb.linearVelocity = Vector2.right * dir * 10f;

            wave.AddComponent<Projectile>();

            Destroy(wave, 3f);
        }
    }

    // PHASE 3: Homing projectile + hizli hareket
    void Phase3Attack()
    {
        // Daha hizli normal projectile
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            ShootHomingProjectile();
            shootTimer = shootInterval * 0.7f;
        }

        // Dalis saldirisi da devam (daha sik)
        diveTimer -= Time.deltaTime;
        if (!isDiving && diveTimer <= 0)
        {
            StartDive();
        }

        if (isDiving)
        {
            PerformDive();
        }
    }

    void ShootHomingProjectile()
    {
        if (player == null) return;

        GameObject projectile = new GameObject("HomingProjectile");
        projectile.transform.position = transform.position;
        projectile.tag = "EnemyProjectile";

        SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 1f, 0f);
        sr.sortingOrder = 5;

        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[256];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                colors[y * 16 + x] = dist < 7f ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);

        CircleCollider2D col = projectile.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        Rigidbody2D projRb = projectile.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0;

        // Homing script ekle
        HomingProjectile homing = projectile.AddComponent<HomingProjectile>();
        homing.speed = homingSpeed;
        homing.target = player;

        projectile.AddComponent<Projectile>();

        Destroy(projectile, homingLifetime);
    }

    // Arena sinirlari ayarla
    public void SetArenaBounds(float minX, float maxX)
    {
        arenaMinX = minX;
        arenaMaxX = maxX;
    }
}

// Homing projectile icin yardimci script
public class HomingProjectile : MonoBehaviour
{
    public float speed = 5f;
    public Transform target;
    public float turnSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
        }
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }
            return;
        }

        // Hedefe dogru don
        Vector2 targetDir = (target.position - transform.position).normalized;
        direction = Vector2.Lerp(direction, targetDir, turnSpeed * Time.fixedDeltaTime);
        direction.Normalize();

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Rotasyon
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
