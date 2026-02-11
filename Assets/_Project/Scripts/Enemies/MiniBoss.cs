using UnityEngine;
using System.Collections;

/// <summary>
/// Mini Boss - Coklu saldiri paternleri ve fazlari olan guclu dusman
/// </summary>
public class MiniBoss : MonoBehaviour
{
    [Header("Stats")]
    public string bossName = "SHADOW GUARDIAN";
    public int maxHealth = 20;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpForce = 10f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("Attack Patterns")]
    public float attackCooldown = 2f;
    public float chargeSpeed = 12f;
    public float chargeDuration = 0.8f;
    public float slamRadius = 3f;
    public int slamDamage = 2;
    public float projectileSpeed = 8f;

    [Header("Phases")]
    public float phase2HealthPercent = 0.6f; // %60 can altinda faz 2
    public float phase3HealthPercent = 0.3f; // %30 can altinda faz 3
    public Color phase1Color = new Color(0.3f, 0.3f, 0.3f); // Koyu gri
    public Color phase2Color = new Color(0.5f, 0f, 0f);    // Koyu kirmizi
    public Color phase3Color = new Color(1f, 0f, 0f);      // Parlak kirmizi

    [Header("Rewards")]
    public int scoreReward = 1000;
    public bool dropSpecialWeapon = true;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;
    private bool isAttacking = false;
    private float attackTimer;
    private int currentPhase = 1;
    private bool isGrounded;
    private bool facingRight = false;

    // Shield system
    private bool isShielded = false;
    private float shieldTimer = 0f;
    public float shieldDuration = 2f;
    public Color shieldColor = new Color(0f, 0.8f, 1f, 0.4f);
    private GameObject shieldVisual;

    // Attack state
    private enum AttackType { Charge, Slam, Projectile, JumpSlam }
    private AttackType currentAttack;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.mass = 5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateBossSprite();
        }

        // Buyuk collider
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2.5f);
            col.offset = new Vector2(0, 0.25f);
        }

        spriteRenderer.color = phase1Color;
        gameObject.tag = "Enemy";

        attackTimer = attackCooldown;
        FindPlayer();

        // Boss saglik barini goster
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBossHealthBar(bossName, maxHealth);
        }
    }

    void CreateBossSprite()
    {
        int width = 64;
        int height = 80;
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color bodyColor = phase1Color;
        Color outlineColor = new Color(0.1f, 0.1f, 0.1f);
        Color eyeColor = new Color(1f, 0.3f, 0f); // Turuncu gozler

        // Ana govde - buyuk dikdortgen
        for (int y = 10; y < 70; y++)
        {
            for (int x = 12; x < 52; x++)
            {
                // Kenarlar
                if (x == 12 || x == 51 || y == 10 || y == 69)
                    pixels[y * width + x] = outlineColor;
                else
                    pixels[y * width + x] = bodyColor;
            }
        }

        // Omuzlar - genis
        for (int y = 55; y < 70; y++)
        {
            for (int x = 5; x < 12; x++)
                pixels[y * width + x] = bodyColor;
            for (int x = 52; x < 59; x++)
                pixels[y * width + x] = bodyColor;
        }

        // Kafa - daha karanlik
        for (int y = 60; y < 78; y++)
        {
            for (int x = 20; x < 44; x++)
            {
                pixels[y * width + x] = outlineColor;
            }
        }

        // Gozler - parlak
        for (int y = 66; y < 72; y++)
        {
            for (int x = 24; x < 30; x++)
                pixels[y * width + x] = eyeColor;
            for (int x = 34; x < 40; x++)
                pixels[y * width + x] = eyeColor;
        }

        // Bacaklar
        for (int y = 0; y < 10; y++)
        {
            for (int x = 16; x < 26; x++)
                pixels[y * width + x] = bodyColor;
            for (int x = 38; x < 48; x++)
                pixels[y * width + x] = bodyColor;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.25f), 32);
    }

    void Update()
    {
        if (isDead) return;

        FindPlayer();
        CheckGround();
        UpdatePhase();

        if (player == null) return;

        attackTimer -= Time.deltaTime;

        // Shield timer
        if (isShielded)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
            {
                isShielded = false;
                DestroyShieldVisual();
            }
        }

        if (!isAttacking)
        {
            // Oyuncuya don
            FacePlayer();

            // Saldiri secimi
            if (attackTimer <= 0)
            {
                ChooseAndExecuteAttack();
            }
            else
            {
                // Yavasca oyuncuya yaklas
                MoveTowardsPlayer();
            }
        }
    }

    void CheckGround()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        if (!isGrounded)
        {
            // Default layer'i de kontrol et
            isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance);
        }
    }

    void UpdatePhase()
    {
        float healthPercent = (float)currentHealth / maxHealth;

        int newPhase = 1;
        Color targetColor = phase1Color;

        if (healthPercent <= phase3HealthPercent)
        {
            newPhase = 3;
            targetColor = phase3Color;
        }
        else if (healthPercent <= phase2HealthPercent)
        {
            newPhase = 2;
            targetColor = phase2Color;
        }

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            StartCoroutine(PhaseTransition(targetColor));
        }
    }

    IEnumerator PhaseTransition(Color targetColor)
    {
        // Faz gecis efekti
        isAttacking = true;

        // Kalkan aktiflestir
        isShielded = true;
        shieldTimer = shieldDuration;
        CreateShieldVisual();

        // Screen shake
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(0.4f, 0.5f);

        // Renk degisimi
        Color startColor = spriteRenderer.color;
        float duration = 0.5f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, targetColor, elapsed / duration);

            // Titresim
            transform.position += new Vector3(Random.Range(-0.1f, 0.1f), 0, 0);

            yield return null;
        }

        spriteRenderer.color = targetColor;

        // Cooldown azalt (daha agresif)
        attackCooldown *= 0.8f;

        // Kalkan kapat
        isShielded = false;
        DestroyShieldVisual();

        isAttacking = false;
        attackTimer = 0.5f; // Hizli saldiri
    }

    void FacePlayer()
    {
        if (player == null) return;

        bool shouldFaceRight = player.position.x > transform.position.x;
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        }
    }

    void MoveTowardsPlayer()
    {
        if (player == null || !isGrounded) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > 4f)
        {
            float direction = player.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void ChooseAndExecuteAttack()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        // Faza gore saldiri secimi
        if (currentPhase == 1)
        {
            // Faz 1: Basit saldirilar
            if (distance < 4f)
                currentAttack = AttackType.Slam;
            else
                currentAttack = AttackType.Charge;
        }
        else if (currentPhase == 2)
        {
            // Faz 2: Daha cok saldiri cesidi
            float rand = Random.value;
            if (rand < 0.4f)
                currentAttack = AttackType.Charge;
            else if (rand < 0.7f)
                currentAttack = AttackType.JumpSlam;
            else
                currentAttack = AttackType.Projectile;
        }
        else
        {
            // Faz 3: Tam guc
            float rand = Random.value;
            if (rand < 0.3f)
                currentAttack = AttackType.Charge;
            else if (rand < 0.5f)
                currentAttack = AttackType.JumpSlam;
            else if (rand < 0.7f)
                currentAttack = AttackType.Projectile;
            else
                currentAttack = AttackType.Slam;
        }

        StartCoroutine(ExecuteAttack());
    }

    IEnumerator ExecuteAttack()
    {
        isAttacking = true;

        switch (currentAttack)
        {
            case AttackType.Charge:
                yield return StartCoroutine(ChargeAttack());
                break;
            case AttackType.Slam:
                yield return StartCoroutine(SlamAttack());
                break;
            case AttackType.JumpSlam:
                yield return StartCoroutine(JumpSlamAttack());
                break;
            case AttackType.Projectile:
                yield return StartCoroutine(ProjectileAttack());
                break;
        }

        isAttacking = false;
        attackTimer = attackCooldown / currentPhase; // Faz arttikca hizlaniyor
    }

    IEnumerator ChargeAttack()
    {
        // Uyari
        spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(0.3f);

        // Hucum
        float direction = player.position.x > transform.position.x ? 1 : -1;
        float elapsed = 0;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            rb.linearVelocity = new Vector2(direction * chargeSpeed, rb.linearVelocity.y);

            // Screen shake
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.Shake(0.1f, 0.05f);

            yield return null;
        }

        // Dur
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        spriteRenderer.color = GetPhaseColor();
    }

    IEnumerator SlamAttack()
    {
        // Yukari ziplama hazirligi
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.2f);

        // Yere vur
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeOnExplosion();

        // Alan hasari
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage();
            }
        }

        // Efekt
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.PlayLandDust(transform.position);

        spriteRenderer.color = GetPhaseColor();
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator JumpSlamAttack()
    {
        // Yukari zipla
        spriteRenderer.color = new Color(1f, 0.5f, 0f);
        rb.linearVelocity = new Vector2(0, jumpForce);

        yield return new WaitForSeconds(0.5f);

        // Oyuncuya dogru havalanma
        if (player != null)
        {
            float dirX = (player.position.x - transform.position.x) * 0.5f;
            rb.linearVelocity = new Vector2(Mathf.Clamp(dirX, -5f, 5f), rb.linearVelocity.y);
        }

        // Yere dusmeyi bekle
        yield return new WaitUntil(() => isGrounded || isDead);

        if (!isDead)
        {
            // Guclu slam
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.ShakeOnExplosion();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius * 1.5f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerController pc = hit.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage();
                }
            }

            if (ParticleManager.Instance != null)
                ParticleManager.Instance.PlayLandDust(transform.position);
        }

        spriteRenderer.color = GetPhaseColor();
    }

    IEnumerator ProjectileAttack()
    {
        // Atis hazirligi
        spriteRenderer.color = new Color(0.5f, 0f, 1f);
        yield return new WaitForSeconds(0.3f);

        // Mermi sayisi faza gore
        int projectileCount = currentPhase + 1;
        float spreadAngle = 15f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i - (projectileCount - 1) / 2f) * spreadAngle;
            Vector2 direction = player != null ?
                ((Vector2)(player.position - transform.position)).normalized :
                (facingRight ? Vector2.right : Vector2.left);

            direction = Quaternion.Euler(0, 0, angle) * direction;

            CreateProjectile(direction);

            yield return new WaitForSeconds(0.1f);
        }

        spriteRenderer.color = GetPhaseColor();
    }

    void CreateProjectile(Vector2 direction)
    {
        GameObject proj = new GameObject("BossProjectile");
        proj.transform.position = transform.position + Vector3.up;
        proj.layer = LayerMask.NameToLayer("Default");

        // Sprite
        SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0f, 0.5f);

        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[256];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
                colors[y * 16 + x] = dist < 6 ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        sr.sortingOrder = 5;

        // Physics
        Rigidbody2D projRb = proj.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0;
        projRb.linearVelocity = direction * projectileSpeed;

        CircleCollider2D col = proj.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Projectile script
        Projectile projScript = proj.AddComponent<Projectile>();
        projScript.damage = 1;
        projScript.isPlayerBullet = false;

        Destroy(proj, 5f);
    }

    Color GetPhaseColor()
    {
        switch (currentPhase)
        {
            case 1: return phase1Color;
            case 2: return phase2Color;
            case 3: return phase3Color;
            default: return phase1Color;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Kalkan aktifse hasari engelle
        if (isShielded)
        {
            // Kalkan efekti
            if (shieldVisual != null)
            {
                StartCoroutine(ShieldFlashEffect());
            }
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.Shake(0.1f, 0.05f);
            return;
        }

        currentHealth -= damage;

        // UI guncelle
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateBossHealth(currentHealth);

        // Hasar efekti
        StartCoroutine(DamageFlash());

        // Screen shake
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(0.15f, 0.1f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlash()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (!isDead)
            spriteRenderer.color = originalColor;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Skor
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreReward);
            GameManager.Instance.EnemyKilled(transform.position);
        }

        // Screen shake
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.ShakeOnExplosion();

        // Boss bar'i gizle
        if (UIManager.Instance != null)
            UIManager.Instance.HideBossHealthBar();

        // Ozel silah dusur
        if (dropSpecialWeapon)
        {
            WeaponType[] specialWeapons = { WeaponType.RocketLauncher, WeaponType.GrenadeLauncher };
            WeaponPickup.Spawn(specialWeapons[Random.Range(0, specialWeapons.Length)],
                transform.position + Vector3.up);
        }

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Epik olum - birden fazla patlama
        for (int i = 0; i < 5; i++)
        {
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.ShakeOnExplosion();

            if (ParticleManager.Instance != null)
            {
                Vector3 explosionPos = transform.position +
                    new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 1.5f), 0);
                ParticleManager.Instance.PlayEnemyDeath(explosionPos);
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEnemyDeath();

            yield return new WaitForSeconds(0.2f);
        }

        // Son patlama
        yield return new WaitForSeconds(0.3f);

        // Solma
        float fadeTime = 0.5f;
        float elapsed = 0;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1 - elapsed / fadeTime);
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

        // Charge sirasinda oyuncuya carpti
        if (collision.gameObject.CompareTag("Player") && isAttacking && currentAttack == AttackType.Charge)
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null)
                pc.TakeDamage();
        }
    }

    // Mermi hasari icin
    void OnTriggerEnter2D(Collider2D other)
    {
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null && proj.isPlayerBullet)
        {
            TakeDamage(proj.damage);
            Destroy(other.gameObject);
        }
    }

    void CreateShieldVisual()
    {
        if (shieldVisual != null) return;

        shieldVisual = new GameObject("BossShield");
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = shieldVisual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // Basit daire shield sprite
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f;
        float innerRadius = outerRadius - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist >= innerRadius && dist <= outerRadius)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16);
        sr.color = shieldColor;

        // Buyukluk
        shieldVisual.transform.localScale = Vector3.one * 3f;
    }

    void DestroyShieldVisual()
    {
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
            shieldVisual = null;
        }
    }

    IEnumerator ShieldFlashEffect()
    {
        if (shieldVisual == null) yield break;
        SpriteRenderer sr = shieldVisual.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color original = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (sr != null)
            sr.color = original;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
