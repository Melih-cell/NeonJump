using UnityEngine;
using System.Collections;

/// <summary>
/// Gelişmiş mermi sistemi - her silah tipine özel davranış
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Base Settings")]
    public int damage = 10;
    public float maxRange = 20f;
    public Vector3 startPosition;
    public WeaponType weaponType = WeaponType.Pistol;

    [Header("Special Properties")]
    public bool hasExplosion = false;
    public float explosionRadius = 0f;
    public bool isPiercing = false;
    public int maxPierceCount = 3;

    [Header("Visual")]
    public Color bulletColor = Color.yellow;
    public TrailRenderer trail;

    // Internal
    private bool hasHit = false;
    private int pierceCount = 0;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float lifeTime = 0f;

    // Flamethrower için
    private float flameDamageTimer = 0f;
    private float flameDamageInterval = 0.1f;

    // Grenade için
    private int bounceCount = 0;
    private int maxBounces = 2;
    private float fuseTime = 1.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        // Silah tipine göre özel ayarlar
        SetupByWeaponType();
    }

    void SetupByWeaponType()
    {
        switch (weaponType)
        {
            case WeaponType.Flamethrower:
                // Alev partikülü - kısa ömürlü
                StartCoroutine(FlameLifetime());
                break;

            case WeaponType.GrenadeLauncher:
                // Bomba - zamanlı patlama
                StartCoroutine(GrenadeTimer());
                break;

            case WeaponType.RocketLauncher:
                // Roket - duman izi
                CreateRocketTrail();
                break;

            case WeaponType.Sniper:
                // Keskin nişancı - çizgi efekti
                CreateSniperTrail();
                break;
        }
    }

    void Update()
    {
        lifeTime += Time.deltaTime;

        // Menzil kontrolü (Grenade hariç - o zaman patlar)
        if (weaponType != WeaponType.GrenadeLauncher)
        {
            float distance = Vector3.Distance(startPosition, transform.position);
            if (distance > maxRange)
            {
                if (hasExplosion)
                    Explode();
                else
                    DestroyBullet();
            }
        }

        // Flamethrower için sürekli hasar
        if (weaponType == WeaponType.Flamethrower)
        {
            flameDamageTimer += Time.deltaTime;
        }

        // Roket rotasyonu - hareket yönüne bak
        if (weaponType == WeaponType.RocketLauncher && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other, true);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Grenade için sekme
        if (weaponType == WeaponType.GrenadeLauncher)
        {
            bounceCount++;
            if (bounceCount >= maxBounces)
            {
                Explode();
            }
            return;
        }

        HandleCollision(collision.collider, false);
    }

    void HandleCollision(Collider2D other, bool isTrigger)
    {
        // Kendi mermileriyle çarpışma
        if (other.GetComponent<Bullet>() != null) return;

        // Player ile çarpışma (kendi mermimiz)
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null) return;

        // Diğer trigger objelerini atla (collectible vb.) - düşman değilse
        if (isTrigger && other.isTrigger && other.GetComponent<EnemyHealth>() == null && other.GetComponent<Enemy>() == null) return;

        // Düşmana çarptı - önce EnemyHealth dene (yeni sistem)
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            HitEnemyHealth(enemyHealth);
            return;
        }

        // Eski sistem - Enemy component
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            HitEnemy(enemy);
            return;
        }

        // Zemine/duvara çarptı
        if (!isTrigger || !other.isTrigger)
        {
            HitWall(other.transform.position);
        }
    }

    void HitEnemyHealth(EnemyHealth enemyHealth)
    {
        // Flamethrower için interval kontrolü
        if (weaponType == WeaponType.Flamethrower)
        {
            if (flameDamageTimer < flameDamageInterval) return;
            flameDamageTimer = 0f;
        }

        // Hasar ver
        enemyHealth.TakeDamage(damage);

        // Silah tipine göre efekt
        PlayHitEffect(enemyHealth.transform.position);

        // Patlama
        if (hasExplosion)
        {
            Explode();
            return;
        }

        // Piercing değilse yok et
        if (!isPiercing)
        {
            hasHit = true;
            Destroy(gameObject);
        }
        else
        {
            pierceCount++;
            if (pierceCount >= maxPierceCount)
            {
                Destroy(gameObject);
            }
        }
    }

    void HitEnemy(Enemy enemy)
    {
        // Flamethrower için interval kontrolü
        if (weaponType == WeaponType.Flamethrower)
        {
            if (flameDamageTimer < flameDamageInterval) return;
            flameDamageTimer = 0f;
        }

        // Hasar ver
        enemy.Die();

        // Skor ekle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(100);
        }

        // Silah tipine göre efekt
        PlayHitEffect(enemy.transform.position);

        // Patlama
        if (hasExplosion)
        {
            Explode();
            return;
        }

        // Delici mermi mi?
        if (isPiercing)
        {
            pierceCount++;
            if (pierceCount >= maxPierceCount)
            {
                DestroyBullet();
            }
            // Delici mermi devam eder
        }
        else if (weaponType != WeaponType.Flamethrower)
        {
            DestroyBullet();
        }
    }

    void HitWall(Vector3 hitPoint)
    {
        if (hasHit) return;

        // Flamethrower duvardan etkilenmez
        if (weaponType == WeaponType.Flamethrower) return;

        hasHit = true;

        // Patlama
        if (hasExplosion)
        {
            Explode();
        }
        else
        {
            PlayHitEffect(hitPoint);
            DestroyBullet();
        }
    }

    void PlayHitEffect(Vector3 position)
    {
        if (ParticleManager.Instance == null) return;

        switch (weaponType)
        {
            case WeaponType.Shotgun:
                // Küçük kıvılcımlar
                ParticleManager.Instance.PlayDamageEffect(position);
                break;

            case WeaponType.Sniper:
                // Kan efekti
                ParticleManager.Instance.PlayEnemyDeath(position);
                break;

            case WeaponType.Flamethrower:
                // Alev parlaması
                ParticleManager.Instance.PlayItemUse(position, new Color(1f, 0.5f, 0f));
                break;

            default:
                ParticleManager.Instance.PlayDamageEffect(position);
                break;
        }
    }

    void Explode()
    {
        if (hasHit && weaponType != WeaponType.GrenadeLauncher) return;
        hasHit = true;

        Vector3 pos = transform.position;

        // Büyük patlama efekti
        CreateExplosionEffect(pos);

        // Alan hasarı
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Mesafeye göre hasar (merkeze yakın = daha fazla hasar)
                float distance = Vector3.Distance(pos, enemy.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius) * 0.5f;

                enemy.Die();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(100);
                }
            }
        }

        // Ekran sarsıntısı
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.2f, 0.3f);
        }

        DestroyBullet();
    }

    void CreateExplosionEffect(Vector3 position)
    {
        // Patlama rengi
        Color explosionColor = weaponType == WeaponType.RocketLauncher ?
            new Color(1f, 0.4f, 0.1f) : new Color(0.3f, 0.8f, 0.3f);

        // Çoklu partikül
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEnemyDeath(position);

            // Ekstra parçacıklar
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = Random.insideUnitCircle * explosionRadius * 0.5f;
                ParticleManager.Instance.PlayItemUse(position + offset, explosionColor);
            }
        }

        // Patlama sprite'ı oluştur
        StartCoroutine(ShowExplosionSprite(position, explosionColor));
    }

    IEnumerator ShowExplosionSprite(Vector3 position, Color color)
    {
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = position;

        SpriteRenderer sr = explosion.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = 15;

        // Daire sprite
        Texture2D tex = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        Vector2 center = new Vector2(16, 16);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 14)
                    colors[y * 32 + x] = Color.white;
                else if (dist < 16)
                    colors[y * 32 + x] = new Color(1, 1, 1, 0.5f);
                else
                    colors[y * 32 + x] = Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);

        // Büyüme ve solma animasyonu
        float duration = 0.3f;
        float elapsed = 0f;
        float maxScale = explosionRadius * 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.Lerp(0.5f, maxScale, t);
            explosion.transform.localScale = new Vector3(scale, scale, 1);

            float alpha = Mathf.Lerp(1f, 0f, t);
            sr.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        Destroy(explosion);
    }

    // === ÖZEL SİLAH DAVRANIŞLARI ===

    IEnumerator FlameLifetime()
    {
        // Alev parçacığı kısa ömürlü
        yield return new WaitForSeconds(0.3f);

        // Solarak yok ol
        float fadeTime = 0.2f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = new Color(bulletColor.r, bulletColor.g, bulletColor.b, alpha);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator GrenadeTimer()
    {
        // Bomba fitili
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    void CreateRocketTrail()
    {
        // Trail renderer ekle
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));

        // Gradient - turuncu -> gri
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.0f),
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;
        trail.sortingOrder = 9;
    }

    void CreateSniperTrail()
    {
        // Keskin nişancı izi
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.1f;
        trail.startWidth = 0.08f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));

        // Mavi çizgi
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.0f),
                new GradientColorKey(new Color(0.1f, 0.3f, 0.5f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;
        trail.sortingOrder = 9;
    }

    void DestroyBullet()
    {
        // Trail varsa bekle
        if (trail != null)
        {
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, trail.time);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (hasExplosion && explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}

/// <summary>
/// Kamera sarsıntı efekti
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalPosition;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            transform.localPosition = originalPosition + Random.insideUnitSphere * shakeIntensity;
            shakeDuration -= Time.deltaTime;

            if (shakeDuration <= 0)
            {
                transform.localPosition = originalPosition;
            }
        }
    }

    public void Shake(float duration, float intensity)
    {
        shakeDuration = duration;
        shakeIntensity = intensity;
        originalPosition = transform.localPosition;
    }
}
