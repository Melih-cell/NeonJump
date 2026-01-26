using UnityEngine;
using System;

/// <summary>
/// Tum dusmanlarin temel sinifi
/// EnemyHealth ile entegre, olum ve hasar islemlerini yonetir
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Temel Ayarlar")]
    public bool autoSetupComponents = true;
    public string enemyTag = "Enemy";

    [Header("Hasar Ayarlari")]
    public bool dealsDamage = true;
    public int contactDamage = 1;
    public float contactKnockback = 5f;
    public bool canBeKilledByJump = true;
    public float bounceForce = 10f;
    public float jumpKillThreshold = 0.3f; // Ustten vurma esigi

    [Header("Olum Ayarlari")]
    public bool useSquashDeath = true;
    public float deathFadeDuration = 0.3f;
    public GameObject deathEffect;

    [Header("Ses Efektleri")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip attackSound;

    // Events
    public event Action OnEnemyHurt;
    public event Action OnEnemyDeath;
    public event Action<PlayerController> OnPlayerContact;

    // Components
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D col;
    protected Animator animator;
    protected EnemyHealth enemyHealth;
    protected EnemyAI enemyAI;
    protected AudioSource audioSource;

    // State
    protected bool isDead = false;
    protected Color originalColor;

    protected virtual void Awake()
    {
        // Component referanslarini al
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        enemyHealth = GetComponent<EnemyHealth>();
        enemyAI = GetComponent<EnemyAI>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hurtSound != null || deathSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    protected virtual void Start()
    {
        // Tag ayarla
        if (!string.IsNullOrEmpty(enemyTag))
            gameObject.tag = enemyTag;

        // Otomatik component kurulumu
        if (autoSetupComponents)
        {
            SetupComponents();
        }

        // EnemyHealth event'lerine baglan
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged += HandleDamaged;
            enemyHealth.OnDeath += HandleDeath;
        }

        // Diger dusmanlarla carpismayı devre disi birak
        IgnoreEnemyCollisions();
    }

    /// <summary>
    /// Diger dusmanlarla fiziksel carpismayı devre disi birakir
    /// </summary>
    protected virtual void IgnoreEnemyCollisions()
    {
        if (col == null) return;

        // Sahnedeki tum dusmanlari bul
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase otherEnemy in allEnemies)
        {
            if (otherEnemy == this) continue;
            if (otherEnemy.col == null) continue;

            // Carpismayı devre disi birak
            Physics2D.IgnoreCollision(col, otherEnemy.col, true);
        }

        // Ayrica Enemy tag'li diger objeleri de kontrol et
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemyObjects)
        {
            if (enemyObj == gameObject) continue;

            Collider2D otherCol = enemyObj.GetComponent<Collider2D>();
            if (otherCol != null && col != null)
            {
                Physics2D.IgnoreCollision(col, otherCol, true);
            }
        }
    }

    protected virtual void SetupComponents()
    {
        // Collider kontrolu
        if (col == null)
        {
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(1f, 1f);
            col = boxCol;
        }

        // Rigidbody kontrolu
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    #region Collision Handling

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerCollision(collision);
        }
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        // Surekli temas halinde hasar (opsiyonel)
        // Bu genellikle kapatik tutulur, sadece Enter'da hasar verilir
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player") && dealsDamage)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                DealDamageToPlayer(player, other.attachedRigidbody);
            }
        }

        // Mermi kontrolu
        if (other.CompareTag("PlayerBullet"))
        {
            HandleBulletHit(other);
        }
    }

    protected virtual void HandlePlayerCollision(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (player == null) return;

        OnPlayerContact?.Invoke(player);

        // Ustten mi vuruldu?
        bool jumpedOnTop = CheckJumpedOnTop(collision, playerRb);

        if (jumpedOnTop && canBeKilledByJump)
        {
            // Dusmanı oldur
            KillByJump(playerRb);
        }
        else if (dealsDamage)
        {
            // Oyuncuya hasar ver
            DealDamageToPlayer(player, playerRb);
        }
    }

    protected virtual bool CheckJumpedOnTop(Collision2D collision, Rigidbody2D playerRb)
    {
        if (playerRb == null) return false;

        // Oyuncu asagi dogru hareket etmeli
        if (playerRb.linearVelocity.y > 0) return false;

        // Collision noktalarini kontrol et
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Normal yukari bakiyorsa oyuncu ustten vurmus
            if (contact.normal.y < -0.5f)
            {
                // Ek kontrol: oyuncunun alt kismi dusmandan yukarda mi
                float playerBottom = collision.transform.position.y - jumpKillThreshold;
                float enemyTop = transform.position.y + (col != null ? col.bounds.extents.y * 0.5f : 0.3f);

                if (playerBottom > enemyTop)
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected virtual void KillByJump(Rigidbody2D playerRb)
    {
        // Oyuncuyu ziprat
        if (playerRb != null && bounceForce > 0)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, bounceForce);
        }

        // Skor ekle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyKilled(transform.position);
        }

        // Oldur
        Die();
    }

    protected virtual void DealDamageToPlayer(PlayerController player, Rigidbody2D playerRb)
    {
        player.TakeDamage();

        // Knockback
        if (playerRb != null && contactKnockback > 0)
        {
            Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
            knockbackDir.y = 0.3f;
            playerRb.AddForce(knockbackDir.normalized * contactKnockback, ForceMode2D.Impulse);
        }

        // Ses
        PlaySound(attackSound);
    }

    protected virtual void HandleBulletHit(Collider2D bullet)
    {
        // EnemyHealth varsa o halledecek
        if (enemyHealth != null) return;

        // Yoksa direkt oldur
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            // Hasar al ve ol
            Die();
        }
    }

    #endregion

    #region Damage & Death

    protected virtual void HandleDamaged(float damage)
    {
        if (isDead) return;

        OnEnemyHurt?.Invoke();
        PlaySound(hurtSound);

        // Flash efekti (EnemyHealth'de de var ama ekstra kontrol)
        if (spriteRenderer != null && enemyHealth != null && !enemyHealth.flashOnDamage)
        {
            StartCoroutine(DamageFlash());
        }
    }

    protected virtual void HandleDeath()
    {
        Die();
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        OnEnemyDeath?.Invoke();
        PlaySound(deathSound);

        // Hareket durdur
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Collider kapat
        if (col != null)
            col.enabled = false;

        // EnemyAI'i bilgilendir
        if (enemyAI != null)
        {
            // EnemyAI kendi OnDeath handler'inda halleder
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDeath();
        }

        // Particle efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEnemyDeath(transform.position);
        }

        // Death effect spawn
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Olum animasyonu
        if (useSquashDeath)
        {
            StartCoroutine(SquashDeathAnimation());
        }
        else
        {
            StartCoroutine(FadeDeathAnimation());
        }
    }

    System.Collections.IEnumerator SquashDeathAnimation()
    {
        // Ezilme efekti
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.3f, originalScale.z);

        // Fade out
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / deathFadeDuration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    System.Collections.IEnumerator FadeDeathAnimation()
    {
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / deathFadeDuration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    #endregion

    #region Utility

    protected void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (AudioManager.Instance != null)
        {
            // AudioManager varsa onu kullan
            // AudioManager.Instance.PlaySFX(clip);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void SetDealsDamage(bool value)
    {
        dealsDamage = value;
    }

    #endregion

    protected virtual void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged -= HandleDamaged;
            enemyHealth.OnDeath -= HandleDeath;
        }
    }
}
