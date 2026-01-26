using UnityEngine;
using System.Collections;

/// <summary>
/// Kırılan/çöken platform - üstüne basınca titrer ve düşer
/// </summary>
public class CrumblingPlatform : MonoBehaviour
{
    [Header("Settings")]
    public float shakeTime = 0.5f;      // Titrme süresi
    public float fallDelay = 0.3f;      // Düşmeye başlama gecikmesi
    public float respawnTime = 3f;      // Yeniden oluşma süresi (0 = yeniden oluşmaz)
    public float shakeIntensity = 0.05f;

    [Header("Visual")]
    public Color warningColor = new Color(1f, 0.5f, 0.5f);

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isTriggered = false;
    private bool isFalling = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Rigidbody2D rb;
    private Color originalColor;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Rigidbody ayarları
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isTriggered)
        {
            // Üstten basıldıysa
            if (collision.contacts[0].normal.y < -0.5f)
            {
                StartCoroutine(CrumbleSequence());
            }
        }
    }

    IEnumerator CrumbleSequence()
    {
        isTriggered = true;

        // Renk uyarısı
        if (spriteRenderer != null)
        {
            spriteRenderer.color = warningColor;
        }

        // Titreme
        float elapsed = 0f;
        while (elapsed < shakeTime)
        {
            float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
            float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
            transform.position = originalPosition + new Vector3(shakeX, shakeY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Düşmeye başla
        yield return new WaitForSeconds(fallDelay);

        isFalling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;

        // Hafif rastgele dönüş
        rb.angularVelocity = Random.Range(-100f, 100f);

        // Bir süre sonra yok et
        yield return new WaitForSeconds(2f);

        if (respawnTime > 0)
        {
            // Gizle
            gameObject.SetActive(false);

            // Respawn bekle
            yield return new WaitForSeconds(respawnTime);

            // Yeniden oluştur
            Respawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Respawn()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isTriggered = false;
        isFalling = false;

        gameObject.SetActive(true);
    }
}
