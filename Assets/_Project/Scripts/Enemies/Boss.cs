using UnityEngine;

public abstract class Boss : MonoBehaviour
{
    [Header("Boss Settings")]
    public string bossName = "Boss";
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Phases")]
    public int currentPhase = 1;

    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected bool isDead = false;
    protected bool isInvincible = false;
    protected float invincibleTimer = 0f;
    protected float invincibleDuration = 1f;

    // Arena bounds
    protected float arenaMinX = 0f;
    protected float arenaMaxX = 20f;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Boss health bar'i goster
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBossHealthBar(bossName, maxHealth);
        }

        // Boss muzigini baslat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossMusic();
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Invincibility timer
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = Mathf.Sin(Time.time * 20f) > 0;
            }

            if (invincibleTimer <= 0)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                }
            }
        }

        // Faz kontrolu
        UpdatePhase();

        // Boss davranisi
        BossBehavior();
    }

    protected virtual void UpdatePhase()
    {
        float healthPercent = (float)currentHealth / maxHealth;

        if (healthPercent > 0.66f)
            currentPhase = 1;
        else if (healthPercent > 0.33f)
            currentPhase = 2;
        else
            currentPhase = 3;
    }

    protected abstract void BossBehavior();

    public virtual void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damage;

        // UI guncelle
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(currentHealth);
        }

        // Ses cal
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossHit();
        }

        // Invincible ol
        isInvincible = true;
        invincibleTimer = invincibleDuration;

        // Oldu mu?
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Boss health bar'i gizle
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBossHealthBar();
        }

        // Muzigi durdur
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWin();
            AudioManager.Instance.StopMusic();
        }

        // Particle efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEnemyDeath(transform.position);
        }

        // === BOSS LOOT DROP - GARANTİ EPİK/LEGENDARY SİLAH ===
        DropBossLoot();

        // Skor ekle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1000);
            GameManager.Instance.Win();
        }

        // Collider kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Yok et
        Destroy(gameObject, 1f);
    }

    /// <summary>
    /// Boss öldüğünde garanti iyi silah düşürür
    /// </summary>
    protected virtual void DropBossLoot()
    {
        // Boss'lar her zaman iyi silah düşürür
        WeaponDrop.SpawnBossDrop(transform.position);

        // Ekstra coin drop
        for (int i = 0; i < 5; i++)
        {
            Vector3 coinPos = transform.position + new Vector3(
                Random.Range(-1.5f, 1.5f),
                Random.Range(0.5f, 2f),
                0
            );

            // Coin oluştur (eğer Coin spawn metodu varsa)
            SpawnBossCoin(coinPos);
        }
    }

    /// <summary>
    /// Boss coin'i oluştur
    /// </summary>
    void SpawnBossCoin(Vector3 position)
    {
        GameObject coinObj = new GameObject("BossCoin");
        coinObj.transform.position = position;

        // Sprite
        SpriteRenderer sr = coinObj.AddComponent<SpriteRenderer>();
        sr.color = Color.yellow;
        sr.sortingOrder = 10;

        // Basit coin texture
        Texture2D tex = new Texture2D(8, 8);
        Color[] colors = new Color[64];
        for (int i = 0; i < 64; i++) colors[i] = Color.yellow;
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16);

        // Coin component
        Coin coin = coinObj.AddComponent<Coin>();
        coin.value = 10; // Boss coin'leri daha değerli

        // Collider
        CircleCollider2D col = coinObj.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Fizik - yukarı fırlat
        Rigidbody2D rb = coinObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 8f));

        Destroy(coinObj, 15f);
    }

    // Oyuncu boss'a zarar verebilir mi?
    protected bool CanBeHitByPlayer()
    {
        return !isDead && !isInvincible;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerCtrl = collision.gameObject.GetComponent<PlayerController>();
            if (playerCtrl == null) return;

            // Oyuncu ustten mi carpti?
            float playerBottom = collision.transform.position.y - 0.5f;
            float bossTop = transform.position.y + 0.5f;

            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb == null) return;

            if (playerBottom > bossTop && playerRb.linearVelocity.y < 0)
            {
                // Boss'a hasar ver
                TakeDamage(1);
                // Oyuncuyu ziptat
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 12f);
            }
            else
            {
                // Oyuncuya hasar ver
                playerCtrl.TakeDamage();
            }
        }
    }
}
