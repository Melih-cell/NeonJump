using UnityEngine;

/// <summary>
/// Toplanabilir silah - dünyada spawn olur, dokunulduğunda envantere eklenir
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Settings")]
    public WeaponType weaponType = WeaponType.Rifle;

    [Header("Visual")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;
    public float rotateSpeed = 0f;
    public bool enableBob = true;
    public bool enableGlow = true;

    [Header("Pickup")]
    public float pickupRadius = 1f;
    public bool autoPickup = true;

    private Vector3 startPos;
    private SpriteRenderer spriteRenderer;
    private float timeOffset;
    private Color weaponColor;

    void Start()
    {
        startPos = transform.position;
        timeOffset = Random.value * Mathf.PI * 2f;

        // Sprite oluştur
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Collider ekle
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = pickupRadius;
            col.isTrigger = true;
        }

        // Görsel ayarla
        SetupVisual();
    }

    void SetupVisual()
    {
        // Silah tipine göre renk
        weaponColor = GetWeaponColor();

        // Önce indirilen sprite'ları dene, yoksa procedural kullan
        spriteRenderer.sprite = WeaponSpriteLoader.GetWeaponSprite(weaponType);
        spriteRenderer.color = Color.white; // Sprite kendi renklerini içeriyor

        spriteRenderer.sortingOrder = 5;

        // Sprite ölçeğini ayarla (indirilen sprite'lar farklı boyutlarda olabilir)
        AdjustSpriteScale();
    }

    void AdjustSpriteScale()
    {
        if (spriteRenderer.sprite == null) return;

        // Hedef boyut (dünya biriminde)
        float targetWidth = 1.5f;

        // Mevcut sprite boyutu
        float currentWidth = spriteRenderer.sprite.bounds.size.x;

        // Ölçek hesapla
        if (currentWidth > 0)
        {
            float scale = targetWidth / currentWidth;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    Color GetWeaponColor()
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                return new Color(0.7f, 0.7f, 0.7f); // Gri
            case WeaponType.Rifle:
                return new Color(0.4f, 0.6f, 0.2f); // Yeşil
            case WeaponType.Shotgun:
                return new Color(0.6f, 0.4f, 0.2f); // Kahverengi
            case WeaponType.SMG:
                return new Color(0.3f, 0.3f, 0.3f); // Koyu gri
            case WeaponType.Sniper:
                return new Color(0.2f, 0.4f, 0.6f); // Mavi
            case WeaponType.RocketLauncher:
                return new Color(0.8f, 0.3f, 0.1f); // Turuncu
            case WeaponType.Flamethrower:
                return new Color(1f, 0.5f, 0f); // Ateş rengi
            case WeaponType.GrenadeLauncher:
                return new Color(0.3f, 0.5f, 0.3f); // Yeşilimsi
            default:
                return Color.white;
        }
    }

    void CreateWeaponSprite()
    {
        int width = 24;
        int height = 12;
        Texture2D tex = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        // Şeffaf arka plan
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.clear;

        // Silah tipine göre şekil
        switch (weaponType)
        {
            case WeaponType.Pistol:
                DrawPistolShape(colors, width, height);
                break;
            case WeaponType.Rifle:
            case WeaponType.SMG:
                DrawRifleShape(colors, width, height);
                break;
            case WeaponType.Shotgun:
                DrawShotgunShape(colors, width, height);
                break;
            case WeaponType.Sniper:
                DrawSniperShape(colors, width, height);
                break;
            case WeaponType.RocketLauncher:
            case WeaponType.GrenadeLauncher:
                DrawLauncherShape(colors, width, height);
                break;
            case WeaponType.Flamethrower:
                DrawFlamethrowerShape(colors, width, height);
                break;
            default:
                DrawRifleShape(colors, width, height);
                break;
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16);
    }

    void DrawPistolShape(Color[] colors, int width, int height)
    {
        // Küçük tabanca şekli
        for (int x = 6; x < 18; x++)
        {
            for (int y = 5; y < 9; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Kabza
        for (int x = 8; x < 12; x++)
        {
            for (int y = 1; y < 5; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
    }

    void DrawRifleShape(Color[] colors, int width, int height)
    {
        // Uzun namlu
        for (int x = 2; x < 22; x++)
        {
            for (int y = 5; y < 8; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Kabza
        for (int x = 14; x < 18; x++)
        {
            for (int y = 1; y < 5; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
    }

    void DrawShotgunShape(Color[] colors, int width, int height)
    {
        // Kalın namlu
        for (int x = 2; x < 20; x++)
        {
            for (int y = 4; y < 9; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Kabza
        for (int x = 12; x < 16; x++)
        {
            for (int y = 1; y < 4; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
    }

    void DrawSniperShape(Color[] colors, int width, int height)
    {
        // Çok uzun ince namlu
        for (int x = 0; x < 24; x++)
        {
            for (int y = 5; y < 7; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Dürbün
        for (int x = 8; x < 12; x++)
        {
            for (int y = 7; y < 10; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Kabza
        for (int x = 16; x < 20; x++)
        {
            for (int y = 2; y < 5; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
    }

    void DrawLauncherShape(Color[] colors, int width, int height)
    {
        // Kalın tüp
        for (int x = 2; x < 20; x++)
        {
            for (int y = 3; y < 10; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Kabza
        for (int x = 12; x < 16; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
    }

    void DrawFlamethrowerShape(Color[] colors, int width, int height)
    {
        // Tank
        for (int x = 14; x < 22; x++)
        {
            for (int y = 2; y < 10; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
        // Namlu
        for (int x = 2; x < 14; x++)
        {
            for (int y = 4; y < 8; y++)
            {
                colors[y * width + x] = Color.white;
            }
        }
    }

    void Update()
    {
        // Yukarı aşağı hareket
        if (enableBob)
        {
            float newY = startPos.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Dönme
        if (rotateSpeed > 0)
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        // Glow efekti
        if (enableGlow && spriteRenderer != null)
        {
            float glow = (Mathf.Sin(Time.time * 3f) + 1f) * 0.25f + 0.5f;
            spriteRenderer.color = new Color(
                weaponColor.r * glow + 0.3f,
                weaponColor.g * glow + 0.3f,
                weaponColor.b * glow + 0.3f
            );
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoPickup) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            TryPickup();
        }
    }

    public void TryPickup()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.AddWeapon(weaponType);

            // Efekt
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayPowerUpCollect(transform.position);
            }

            // Ses
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPowerUp();
            }

            // Silah adını al
            WeaponData data = WeaponData.Create(weaponType);

            // Bildirim goster
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowWeaponPickup(data.weaponName);
            }

            // Floating text
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.ShowText(transform.position + Vector3.up * 0.5f, data.weaponName, weaponColor, 1.2f);
            }

            Debug.Log($"Silah alındı: {data.weaponName}");

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Runtime'da silah pickup oluştur
    /// </summary>
    public static GameObject Spawn(WeaponType type, Vector3 position)
    {
        GameObject obj = new GameObject("WeaponPickup_" + type.ToString());
        obj.transform.position = position;

        WeaponPickup pickup = obj.AddComponent<WeaponPickup>();
        pickup.weaponType = type;

        return obj;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
