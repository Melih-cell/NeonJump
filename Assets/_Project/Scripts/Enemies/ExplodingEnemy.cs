using UnityEngine;

public class ExplodingEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public float chaseSpeed = 5f;
    public bool startMovingRight = false;

    [Header("Explosion")]
    public float explosionRange = 2.5f;
    public float explosionTriggerDistance = 1.5f;
    public float fuseTime = 1.5f;
    public int explosionDamage = 2;
    public bool explodeOnDeath = true;

    [Header("Visual")]
    public Color normalColor = new Color(1f, 0.5f, 0f);   // Turuncu
    public Color warningColor = new Color(1f, 0f, 0f);     // Kirmizi
    public Color fuseColor = new Color(1f, 1f, 0f);        // Sari (yanip sonme)
    public float pulseSpeed = 8f;
    public float growAmount = 1.3f;  // Patlama oncesi buyume

    [Header("Audio")]
    public bool playTickSound = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool isDead = false;
    private bool isFusing = false;
    private float fuseTimer = 0f;
    private float effectTimer = 0f;
    private Vector3 baseScale;
    private Color originalColor;
    private bool isChasing = false;
    private int moveDirection = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateExplosiveSprite();
        }

        baseScale = transform.localScale;
        originalColor = spriteRenderer.color;

        if (originalColor == Color.white)
        {
            spriteRenderer.color = normalColor;
            originalColor = normalColor;
        }

        moveDirection = startMovingRight ? 1 : -1;
        gameObject.tag = "Enemy";

        // BoxCollider yoksa ekle
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }
    }

    void CreateExplosiveSprite()
    {
        // Basit patlayici dusman sprite'i olustur
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Yuvarlak govde
        int centerX = size / 2;
        int centerY = size / 2;
        int radius = 12;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist < radius)
                {
                    if (dist > radius - 2)
                        pixels[y * size + x] = new Color(0.8f, 0.3f, 0f); // Outline
                    else
                        pixels[y * size + x] = normalColor;
                }
            }
        }

        // Fitil (ust kisim)
        for (int y = centerY + radius - 2; y < size - 2; y++)
        {
            pixels[y * size + centerX] = new Color(0.4f, 0.3f, 0.2f);
            pixels[y * size + centerX + 1] = new Color(0.3f, 0.2f, 0.1f);
        }

        // Tehlike isareti (X)
        for (int i = -3; i <= 3; i++)
        {
            int px1 = centerX + i;
            int py1 = centerY + i;
            int py2 = centerY - i;

            if (px1 >= 0 && px1 < size && py1 >= 0 && py1 < size)
                pixels[py1 * size + px1] = Color.black;
            if (px1 >= 0 && px1 < size && py2 >= 0 && py2 < size)
                pixels[py2 * size + px1] = Color.black;
        }

        // Gozler (kizgin)
        pixels[(centerY + 3) * size + (centerX - 4)] = Color.white;
        pixels[(centerY + 3) * size + (centerX + 4)] = Color.white;
        pixels[(centerY + 2) * size + (centerX - 4)] = Color.black;
        pixels[(centerY + 2) * size + (centerX + 4)] = Color.black;

        tex.SetPixels(pixels);
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        spriteRenderer.sortingOrder = 5;
    }

    void Update()
    {
        if (isDead) return;

        effectTimer += Time.deltaTime;

        // Player'i bul
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Fitil yakilmissa
        if (isFusing)
        {
            UpdateFuse();
            return;
        }

        // Patlama mesafesine geldiyse fitili yak
        if (distanceToPlayer <= explosionTriggerDistance)
        {
            StartFuse();
            return;
        }

        // Oyuncuyu tespit ettiyse takip et
        if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;
            ChasePlayer();
        }
        else
        {
            isChasing = false;
            Patrol();
        }

        UpdateVisuals();
    }

    void FindPlayer()
    {
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

    void Patrol()
    {
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        // Sprite yonu
        if (spriteRenderer != null)
            spriteRenderer.flipX = moveDirection < 0;

        // Duvar veya ucurum kontrolu
        Vector2 wallCheck = (Vector2)transform.position + new Vector2(moveDirection * 0.5f, 0);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck, Vector2.right * moveDirection, 0.5f);

        Vector2 groundCheck = (Vector2)transform.position + new Vector2(moveDirection * 0.5f, -0.5f);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheck, Vector2.down, 1f);

        bool shouldTurn = false;

        if (wallHit.collider != null && !wallHit.collider.isTrigger && !wallHit.collider.CompareTag("Player"))
            shouldTurn = true;

        if (groundHit.collider == null)
            shouldTurn = true;

        if (shouldTurn)
            moveDirection *= -1;
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(directionToPlayer * chaseSpeed, rb.linearVelocity.y);

        if (spriteRenderer != null)
            spriteRenderer.flipX = directionToPlayer < 0;

        moveDirection = (int)directionToPlayer;
    }

    void StartFuse()
    {
        if (isFusing) return;

        isFusing = true;
        fuseTimer = fuseTime;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Alarm sesi
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossHit(); // Uyari sesi olarak kullan
        }
    }

    void UpdateFuse()
    {
        fuseTimer -= Time.deltaTime;

        // Gorusel efektler - gitgide hizlanan yanip sonme
        float fuseProgress = 1f - (fuseTimer / fuseTime);
        float currentPulseSpeed = Mathf.Lerp(pulseSpeed, pulseSpeed * 4f, fuseProgress);
        float pulse = (Mathf.Sin(effectTimer * currentPulseSpeed) + 1f) * 0.5f;

        // Renk degisimi
        Color currentColor = Color.Lerp(warningColor, fuseColor, pulse);
        if (spriteRenderer != null)
            spriteRenderer.color = currentColor;

        // Buyume
        float currentScale = Mathf.Lerp(1f, growAmount, fuseProgress);
        transform.localScale = baseScale * currentScale;

        // Titreme
        float shake = Random.Range(-0.05f, 0.05f) * fuseProgress;
        transform.position += new Vector3(shake, 0, 0);

        // Patlama zamani
        if (fuseTimer <= 0)
        {
            Explode();
        }
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (isChasing)
        {
            // Takip ederken kirmizimsi
            float pulse = (Mathf.Sin(effectTimer * pulseSpeed) + 1f) * 0.5f;
            spriteRenderer.color = Color.Lerp(normalColor, warningColor, pulse * 0.5f);
        }
        else
        {
            // Normal devriye - hafif nabiz
            float pulse = (Mathf.Sin(effectTimer * pulseSpeed * 0.5f) + 1f) * 0.5f;
            float scale = 1f + pulse * 0.05f;
            transform.localScale = baseScale * scale;
        }
    }

    void Explode()
    {
        if (isDead) return;
        isDead = true;

        // Patlama efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayExplosion(transform.position);
        }

        // Screen shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnExplosion();
        }

        // Patlama sesi
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDeath();
        }

        // Yakin oyunculara hasar ver
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // Mesafeye gore hasar
                    float distance = Vector2.Distance(transform.position, hit.transform.position);
                    float damageMultiplier = 1f - (distance / explosionRange);

                    if (damageMultiplier > 0.3f) // Minimum hasar esigi
                    {
                        pc.TakeDamage();
                    }
                }
            }
            // Diger dusmanlar da hasar alabilir
            else if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                    enemy.Die();

                ExplodingEnemy explEnemy = hit.GetComponent<ExplodingEnemy>();
                if (explEnemy != null && !explEnemy.isDead)
                    explEnemy.TriggerExplosion(); // Zincirleme patlama!
            }
        }

        // Skor
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyKilled(transform.position);
        }

        Destroy(gameObject);
    }

    public void TriggerExplosion()
    {
        // Disaridan patlamayi tetikle (zincirleme icin)
        if (!isDead && !isFusing)
        {
            fuseTime = 0.3f; // Cok kisa fitil
            StartFuse();
        }
        else if (isFusing)
        {
            fuseTimer = Mathf.Min(fuseTimer, 0.2f); // Hizlandir
        }
    }

    public void Die()
    {
        if (isDead) return;

        if (explodeOnDeath)
        {
            // Olunce de patla
            fuseTime = 0.5f;
            StartFuse();
        }
        else
        {
            isDead = true;
            if (GameManager.Instance != null)
                GameManager.Instance.EnemyKilled(transform.position);

            if (ParticleManager.Instance != null)
                ParticleManager.Instance.PlayEnemyDeath(transform.position);

            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Oyuncuyla temas edince hemen fitil yak
        if (collision.gameObject.CompareTag("Player") && !isFusing)
        {
            StartFuse();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Tespit menzili
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Patlama tetikleme mesafesi
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionTriggerDistance);

        // Patlama menzili
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
