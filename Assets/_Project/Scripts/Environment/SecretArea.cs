using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Gizli bölge sistemi - oyuncu yaklaşınca görünmez olur
/// </summary>
public class SecretArea : MonoBehaviour
{
    [Header("Settings")]
    public float fadeSpeed = 3f;
    public float hiddenAlpha = 0.2f; // Oyuncu içindeyken opaklık
    public bool revealOnTouch = true;

    [Header("Reveal Effect")]
    public bool showParticles = true;
    public Color revealColor = new Color(1f, 0.8f, 0f, 0.5f);

    private SpriteRenderer spriteRenderer;
    private TilemapRenderer tilemapRenderer;
    private float targetAlpha = 1f;
    private float currentAlpha = 1f;
    private bool isRevealed = false;
    private bool hasBeenDiscovered = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    void Update()
    {
        // Yumuşak geçiş
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = currentAlpha;
            spriteRenderer.color = c;
        }

        if (tilemapRenderer != null)
        {
            Color c = tilemapRenderer.material.color;
            c.a = currentAlpha;
            tilemapRenderer.material.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && revealOnTouch)
        {
            targetAlpha = hiddenAlpha;
            isRevealed = true;

            // İlk keşif efekti
            if (!hasBeenDiscovered)
            {
                hasBeenDiscovered = true;
                OnFirstDiscovery();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetAlpha = 1f;
            isRevealed = false;
        }
    }

    void OnFirstDiscovery()
    {
        if (showParticles && ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayItemUse(transform.position, revealColor);
        }

        // Skor veya başarım eklenebilir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(50);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoin(); // Keşif sesi
        }
    }
}
