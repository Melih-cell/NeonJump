using UnityEngine;

/// <summary>
/// Tehlike bölgesi - oyuncuya hasar verir
/// Dikenler, lavlar, elektrik vb. için kullanılır
/// </summary>
public class Hazard : MonoBehaviour
{
    public enum HazardType
    {
        Spikes,      // Dikenler - anında hasar
        Lava,        // Lav - sürekli hasar
        Electric,    // Elektrik - aralıklı hasar
        Poison,      // Zehir - yavaşlatma + hasar
        Void         // Boşluk - anında ölüm
    }

    [Header("Hazard Settings")]
    public HazardType hazardType = HazardType.Spikes;
    public int damage = 1;
    public float damageInterval = 0.5f; // Sürekli hasar için
    public float slowAmount = 0.5f; // Zehir için yavaşlatma

    [Header("Visual Effects")]
    public Color hazardColor = Color.red;
    public bool animateColor = true;
    public float animSpeed = 2f;

    private float damageTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Collider trigger olmalı
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        // Renk animasyonu
        if (animateColor && spriteRenderer != null)
        {
            float t = (Mathf.Sin(Time.time * animSpeed) + 1f) / 2f;
            spriteRenderer.color = Color.Lerp(originalColor, hazardColor, t * 0.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (hazardType == HazardType.Void)
            {
                // Anında ölüm
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    // Tüm canları al
                    for (int i = 0; i < 10; i++)
                        player.TakeDamage();
                }
            }
            else if (hazardType == HazardType.Spikes)
            {
                ApplyDamage(other);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            damageTimer += Time.deltaTime;

            if (hazardType == HazardType.Lava || hazardType == HazardType.Electric || hazardType == HazardType.Poison)
            {
                if (damageTimer >= damageInterval)
                {
                    ApplyDamage(other);
                    damageTimer = 0f;
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            damageTimer = 0f;
        }
    }

    void ApplyDamage(Collider2D playerCollider)
    {
        PlayerController player = playerCollider.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage();

            // Efekt
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayDamageEffect(playerCollider.transform.position);
            }
        }
    }
}
