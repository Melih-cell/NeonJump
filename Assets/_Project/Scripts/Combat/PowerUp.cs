using UnityEngine;

public enum PowerUpType
{
    SpeedBoost,
    DoubleJump,
    Shield,
    Magnet,
    Invincibility
}

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType = PowerUpType.SpeedBoost;
    public float duration = 5f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;

    private Vector3 startPos;
    private SpriteRenderer spriteRenderer;
    private float glowTimer = 0f;

    void Start()
    {
        startPos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Sprite yoksa olustur
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            CreatePowerUpSprite();
        }

        // Collider ekle
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }
    }

    void CreatePowerUpSprite()
    {
        // 16x16 basit sprite
        Texture2D tex = new Texture2D(16, 16);
        Color[] pixels = new Color[256];

        Color mainColor = GetPowerUpColor();
        Color glowColor = Color.white;

        // Daire seklinde sprite
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < 6f)
                {
                    // Ic kisim - gradient
                    float t = dist / 6f;
                    pixels[y * 16 + x] = Color.Lerp(glowColor, mainColor, t);
                }
                else if (dist < 7f)
                {
                    // Kenar
                    pixels[y * 16 + x] = mainColor;
                }
                else
                {
                    pixels[y * 16 + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 5;
    }

    Color GetPowerUpColor()
    {
        switch (powerUpType)
        {
            case PowerUpType.SpeedBoost:
                return new Color(0f, 0.8f, 1f); // Cyan - Hiz
            case PowerUpType.DoubleJump:
                return new Color(0.5f, 1f, 0.5f); // Acik yesil - Cift ziplama
            case PowerUpType.Shield:
                return new Color(0.3f, 0.3f, 1f); // Mavi - Kalkan
            case PowerUpType.Magnet:
                return new Color(1f, 0.8f, 0f); // Altin - Miknatıs
            case PowerUpType.Invincibility:
                return new Color(1f, 1f, 0f); // Sari - Dokunulmazlik
            default:
                return Color.white;
        }
    }

    void Update()
    {
        // Yukari asagi hareket
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Parildama efekti
        glowTimer += Time.deltaTime * 3f;
        float glow = (Mathf.Sin(glowTimer) + 1f) * 0.25f + 0.5f;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(glow, glow, glow, 1f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Power-up'i aktifle
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.ActivatePowerUp(powerUpType, duration);
            }

            // Ses cal
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPowerUp();
            }

            // Particle efekti
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayCoinCollect(transform.position);
            }

            // Yok et
            Destroy(gameObject);
        }
    }
}
