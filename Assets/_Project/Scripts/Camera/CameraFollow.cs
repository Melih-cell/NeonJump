using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(2f, 1f, -10f);

    [Header("Camera Bounds")]
    public float minX = -100f;
    public float maxX = 250f;
    public float minY = -100f;  // Asagi dogru level tasarimi icin
    public float maxY = 100f;

    [Header("Screen Shake")]
    public float maxShakeIntensity = 0.5f;
    private float shakeIntensity = 0f;
    private float shakeDuration = 0f;
    private float shakeTimer = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    // Gelismis shake parametreleri
    private Vector2 shakeDirection = Vector2.zero;
    private bool useDirectionalShake = false;
    private float shakeFrequency = 25f;
    private AnimationCurve shakeFalloff;

    [Header("Chromatic Aberration (Optional)")]
    public bool enableChromaticShake = false;
    private float chromaticAmount = 0f;

    // Ölüm durumunda kamerayı dondur
    private bool isFrozen = false;
    private Vector3 frozenPosition;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        // Kamera dondurulmuşsa hareket etme
        if (isFrozen)
        {
            transform.position = frozenPosition;
            return;
        }

        // Target yoksa veya yok edilmişse bul
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        // Hedef pozisyon
        Vector3 desiredPosition = GetTargetPosition();

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Screen Shake uygula
        UpdateShake();
        smoothedPosition += shakeOffset;

        transform.position = smoothedPosition;
    }

    void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            // Zamanla azalan sarsinti (exponential falloff)
            float progress = 1f - (shakeTimer / shakeDuration);
            float falloff = 1f - (progress * progress); // Quadratic falloff - basta guclu, sonda yavas
            float currentIntensity = shakeIntensity * falloff;
            currentIntensity = Mathf.Min(currentIntensity, maxShakeIntensity);

            if (useDirectionalShake && shakeDirection != Vector2.zero)
            {
                // Yonlu sarsinti (darbe yonunde daha guclu)
                float noise = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
                float perpNoise = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;

                Vector2 perpDir = new Vector2(-shakeDirection.y, shakeDirection.x);
                Vector2 shake = shakeDirection * noise * currentIntensity +
                               perpDir * perpNoise * currentIntensity * 0.5f;

                shakeOffset = new Vector3(shake.x, shake.y, 0);
            }
            else
            {
                // Rastgele sarsinti (Perlin noise ile daha dogal)
                float noiseX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
                float noiseY = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;

                shakeOffset = new Vector3(
                    noiseX * currentIntensity,
                    noiseY * currentIntensity,
                    0
                );
            }

            // Chromatic aberration (opsiyonel)
            if (enableChromaticShake)
            {
                chromaticAmount = currentIntensity * 0.5f;
            }
        }
        else
        {
            shakeOffset = Vector3.zero;
            useDirectionalShake = false;
            chromaticAmount = 0f;
        }
    }

    // Ekrani salla
    public void Shake(float intensity, float duration)
    {
        // Daha guclu sarsinti varsa degistirme
        if (intensity > shakeIntensity || shakeTimer <= 0)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
    }

    // Farkli durumlar icin hazir shake metodlari
    public void ShakeOnDamage()
    {
        Shake(0.3f, 0.2f);
    }

    public void ShakeOnEnemyKill()
    {
        Shake(0.15f, 0.1f);
    }

    public void ShakeOnExplosion()
    {
        Shake(0.5f, 0.3f);
    }

    public void ShakeOnDash()
    {
        Shake(0.1f, 0.08f);
    }

    // === GELISMIS SHAKE METODLARI ===

    // Yonlu sarsinti (darbe veya patlama icin)
    public void DirectionalShake(float intensity, float duration, Vector2 direction)
    {
        if (intensity > shakeIntensity || shakeTimer <= 0)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
            shakeDirection = direction.normalized;
            useDirectionalShake = true;
        }
    }

    // Patlama sarsintisi (merkezden disari)
    public void ShakeFromExplosion(Vector3 explosionPos, float intensity, float duration)
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)explosionPos).normalized;
        DirectionalShake(intensity, duration, direction);
    }

    // Inis sarsintisi (asagi dogru)
    public void ShakeOnLand(float fallSpeed)
    {
        // Dusus hizina gore sarsinti siddeti
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(fallSpeed) / 20f);
        float intensity = normalizedSpeed * 0.3f;

        if (intensity > 0.05f)
        {
            DirectionalShake(intensity, 0.15f, Vector2.down);
        }
    }

    // Boss sarsintisi (guclu ve uzun)
    public void ShakeOnBossAttack()
    {
        Shake(0.4f, 0.35f);
    }

    // Boss olum sarsintisi (cok guclu)
    public void ShakeOnBossDeath()
    {
        Shake(0.6f, 0.5f);
    }

    // Kritik vuruş sarsintisi
    public void ShakeOnCriticalHit()
    {
        Shake(0.25f, 0.12f);
    }

    // Oyuncu hasar aldığında yonlu shake
    public void ShakeOnPlayerHit(Vector2 hitDirection)
    {
        DirectionalShake(0.35f, 0.2f, hitDirection);
    }

    // Silah atesi sarsintisi (hafif)
    public void ShakeOnFire(float recoilStrength = 0.05f)
    {
        Shake(recoilStrength, 0.05f);
    }

    // Combo sarsintisi (combo seviyesine gore)
    public void ShakeOnCombo(int comboLevel)
    {
        float intensity = Mathf.Min(0.1f + comboLevel * 0.02f, 0.3f);
        Shake(intensity, 0.1f);
    }

    Vector3 GetTargetPosition()
    {
        if (target == null) return transform.position;

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            offset.z
        );

        // Sinirlari uygula
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

        return desiredPosition;
    }

    // Checkpoint icin ani gecis
    public void SnapToTarget()
    {
        if (target == null) FindTarget();
        if (target == null) return;
        transform.position = GetTargetPosition();
    }

    /// <summary>
    /// Kamerayı mevcut pozisyonda dondur (ölüm için)
    /// </summary>
    public void Freeze()
    {
        isFrozen = true;
        frozenPosition = transform.position;
    }

    /// <summary>
    /// Kamerayı çöz (respawn için)
    /// </summary>
    public void Unfreeze()
    {
        isFrozen = false;
    }

    void FindTarget()
    {
        // PlayerController ile ara (en guvenilir)
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            target = pc.transform;
            return;
        }

        // Player_Asker'i ara
        GameObject player = GameObject.Find("Player_Asker");

        // Yoksa tag ile ara
        if (player == null)
            player = GameObject.FindWithTag("Player");

        // Yoksa "Player" ismini ara
        if (player == null)
            player = GameObject.Find("Player");

        if (player != null)
        {
            target = player.transform;
        }
    }
}
