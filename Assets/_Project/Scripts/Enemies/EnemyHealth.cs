using UnityEngine;
using System;

/// <summary>
/// Dusman can sistemi - Health bar icin gerekli
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Settings")]
    public bool showHealthBar = true;
    public float healthBarYOffset = 1.2f;

    [Header("Damage Effects")]
    public bool flashOnDamage = true;
    public Color damageFlashColor = Color.white;
    public float flashDuration = 0.1f;

    [Header("Death Settings")]
    public bool destroyOnDeath = true;
    public float deathDelay = 0f;
    public GameObject deathEffect;

    // Events
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;
    public event Action<float> OnDamaged; // damage amount

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Start()
    {
        // Tag'i ayarla
        if (!gameObject.CompareTag("Enemy"))
            gameObject.tag = "Enemy";
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Floating damage text
        if (FloatingTextManager.Instance != null)
        {
            bool isCritical = damage >= maxHealth * 0.3f;
            FloatingTextManager.Instance.ShowDamage(transform.position + Vector3.up * 0.5f, Mathf.RoundToInt(damage), isCritical);
        }

        // Enemy indicator manager'a bildir
        if (EnemyIndicatorManager.Instance != null)
        {
            EnemyIndicatorManager.Instance.OnEnemyDamaged(this, damage);
        }

        // Flash efekti
        if (flashOnDamage && spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(DamageFlash());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Floating heal text
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowHeal(transform.position + Vector3.up * 0.5f, Mathf.RoundToInt(amount));
        }
    }

    public void SetMaxHealth(float newMax, bool healToMax = false)
    {
        maxHealth = newMax;
        if (healToMax)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetHealthPercent()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    public bool IsDead()
    {
        return isDead;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        OnDeath?.Invoke();

        // Enemy indicator manager'a bildir
        if (EnemyIndicatorManager.Instance != null)
        {
            EnemyIndicatorManager.Instance.OnEnemyDied(this);
        }

        // EnemyBase varsa onu kullan (yeni sistem)
        EnemyBase enemyBase = GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            // EnemyBase.Die() zaten event'e bagli, tekrar cagirmaya gerek yok
            // Sadece destroyOnDeath false ise burada birakiyoruz
            if (!destroyOnDeath) return;

            // EnemyBase kendi olum animasyonunu yapar
            return;
        }

        // Mevcut enemy Die metodunu cagir (eski sistem uyumlulugu)
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Die();
            return;
        }

        FlyingEnemy flyingEnemy = GetComponent<FlyingEnemy>();
        if (flyingEnemy != null)
        {
            flyingEnemy.Die();
            return;
        }

        ShootingEnemy shootingEnemy = GetComponent<ShootingEnemy>();
        if (shootingEnemy != null)
        {
            shootingEnemy.Die();
            return;
        }

        JumpingEnemy jumpingEnemy = GetComponent<JumpingEnemy>();
        if (jumpingEnemy != null)
        {
            jumpingEnemy.Die();
            return;
        }

        TeleportingEnemy teleportingEnemy = GetComponent<TeleportingEnemy>();
        if (teleportingEnemy != null)
        {
            teleportingEnemy.Die();
            return;
        }

        ExplodingEnemy explodingEnemy = GetComponent<ExplodingEnemy>();
        if (explodingEnemy != null)
        {
            explodingEnemy.Die();
            return;
        }

        // Death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Varsayilan olum
        if (destroyOnDeath)
        {
            if (deathDelay > 0)
                Destroy(gameObject, deathDelay);
            else
                Destroy(gameObject);
        }
    }

    System.Collections.IEnumerator DamageFlash()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    // Hasar aldiginda cagrilacak (collision vb.)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Player'in silahi veya mermisi
        if (other.CompareTag("PlayerBullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.damage);
            }
            else
            {
                TakeDamage(10f); // Default hasar
            }
        }
    }
}
