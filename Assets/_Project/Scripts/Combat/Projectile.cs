using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;
    public float speed = 8f;
    public bool isPlayerBullet = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // isPlayerBullet zaten kod tarafindan ayarlaniyor, tag kontrolune gerek yok

        // 5 saniye sonra otomatik yok et (ekran disinda kaldiysa)
        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Player mermisi ise
        if (isPlayerBullet)
        {
            // Dusmana carpti
            if (other.CompareTag("Enemy"))
            {
                // Oncelikle EnemyHealth dene (yeni sistem)
                EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                }
                else
                {
                    // Eski sistem - Enemy component
                    Enemy enemy = other.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Die();
                    }
                }

                // Carpma efekti
                if (ParticleManager.Instance != null)
                {
                    ParticleManager.Instance.PlayDamageEffect(transform.position);
                }
                Destroy(gameObject);
            }
            // Zemine carpti
            else if (!other.isTrigger && !other.CompareTag("Player"))
            {
                if (ParticleManager.Instance != null)
                {
                    ParticleManager.Instance.PlayDamageEffect(transform.position);
                }
                Destroy(gameObject);
            }
        }
        // Dusman mermisi ise
        else
        {
            // Oyuncuya carpti
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage();
                }
                Destroy(gameObject);
            }
            // Zemine veya platforma carpti
            else if (!other.isTrigger && !other.CompareTag("Enemy") && !other.CompareTag("EnemyProjectile"))
            {
                if (ParticleManager.Instance != null)
                {
                    ParticleManager.Instance.PlayDamageEffect(transform.position);
                }
                Destroy(gameObject);
            }
        }
    }
}
