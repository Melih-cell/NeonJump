using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    public Transform target;
    public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(2f, 2.5f, -10f);

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

    [Header("Mobile Camera Settings")]
    [Tooltip("Mobilde kamera ek zoom out miktari (negatif = zoom out)")]
    public float mobileZoomOutOffset = -2.5f;
    [Tooltip("Hareket yonune look-ahead mesafesi")]
    public float lookAheadDistance = 3.5f;
    [Tooltip("Look-ahead yumusaklik hizi")]
    public float lookAheadSmooth = 5f;
    [Tooltip("Mobilde screen shake carpani (0.5 = %50 daha az)")]
    [Range(0f, 1f)]
    public float mobileShakeMultiplier = 0.5f;

    [Header("Dead Zone")]
    [Tooltip("Yatay dead zone mesafesi - bu araliktaki hareketlerde kamera sabit kalir")]
    public float deadZoneX = 0.5f;
    [Tooltip("Dikey dead zone mesafesi - bu araliktaki hareketlerde kamera sabit kalir")]
    public float deadZoneY = 0.3f;

    [Header("Asymmetric Y Follow")]
    [Tooltip("Yukari hareket (ziplama) icin smooth carpani")]
    public float upSmoothMultiplier = 0.5f;
    [Tooltip("Asagi hareket (dusus) icin smooth carpani")]
    public float downSmoothMultiplier = 1.3f;

    private float currentLookAhead = 0f;
    private bool isMobilePlatform = false;
    private float baseOrthographicSize;
    private Vector3 lastTargetPosition;

    // Ölüm durumunda kamerayı dondur
    private bool isFrozen = false;
    private Vector3 frozenPosition;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // lastTargetPosition'i baslat
        if (target != null)
            lastTargetPosition = target.position;

        // Mobil platform tespiti
        isMobilePlatform = Application.isMobilePlatform;

        #if UNITY_EDITOR
        // Editor'da MobileControls aktifse mobil gibi davran
        if (MobileControls.Instance != null && MobileControls.Instance.IsEnabled)
            isMobilePlatform = true;
        #endif

        // Ana kamera varsa ortographic size'i kaydet ve mobil zoom out uygula
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            baseOrthographicSize = cam.orthographicSize;
            if (isMobilePlatform)
            {
                cam.orthographicSize = baseOrthographicSize - mobileZoomOutOffset;
            }
        }
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

        // Dead zone kontrolu - kucuk hareketlerde kamera sabit kalsin
        float deltaX = desiredPosition.x - transform.position.x;
        float deltaY = desiredPosition.y - transform.position.y;

        float targetX = transform.position.x;
        float targetY = transform.position.y;

        if (Mathf.Abs(deltaX) > deadZoneX)
        {
            targetX = desiredPosition.x - Mathf.Sign(deltaX) * deadZoneX;
        }

        if (Mathf.Abs(deltaY) > deadZoneY)
        {
            targetY = desiredPosition.y - Mathf.Sign(deltaY) * deadZoneY;
        }

        // Asimetrik Y takip: yukari yavaş, asagi hizli
        float ySmooth = smoothSpeed;
        if (target != null)
        {
            float yVelocity = target.position.y - lastTargetPosition.y;
            if (yVelocity > 0.01f)
            {
                // Yukari hareket (ziplama) - yavas takip, stabil kamera
                ySmooth = smoothSpeed * upSmoothMultiplier;
            }
            else if (yVelocity < -0.01f)
            {
                // Asagi hareket (dusus) - hizli takip, oyuncuyu kaybetmesin
                ySmooth = smoothSpeed * downSmoothMultiplier;
            }
            lastTargetPosition = target.position;
        }

        Vector3 smoothedPosition = new Vector3(
            Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime),
            Mathf.Lerp(transform.position.y, targetY, ySmooth * Time.deltaTime),
            desiredPosition.z
        );

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
        // Mobilde screen shake azalt
        if (isMobilePlatform)
        {
            intensity *= mobileShakeMultiplier;
        }

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
        // Mobilde screen shake azalt
        if (isMobilePlatform)
        {
            intensity *= mobileShakeMultiplier;
        }

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

    /// <summary>
    /// Hit freeze efekti - kisa sure oyunu dondurur (hitstop)
    /// Dusman oldurme ve boss hasarinda kullanilir
    /// </summary>
    public void HitFreeze(float duration = 0.04f)
    {
        StartCoroutine(HitFreezeCoroutine(duration));
    }

    private System.Collections.IEnumerator HitFreezeCoroutine(float duration)
    {
        Time.timeScale = 0.02f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Dusman oldurme hitstop'u
    /// </summary>
    public void HitFreezeOnKill()
    {
        HitFreeze(0.04f);
    }

    /// <summary>
    /// Boss hasar hitstop'u (daha uzun)
    /// </summary>
    public void HitFreezeOnBossDamage()
    {
        HitFreeze(0.06f);
    }

    Vector3 GetTargetPosition()
    {
        if (target == null) return transform.position;

        // Look-ahead: oyuncunun hareket yonune dogru kamerayi kaydir
        float targetLookAhead = 0f;
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null && Mathf.Abs(targetRb.linearVelocity.x) > 0.5f)
        {
            targetLookAhead = Mathf.Sign(targetRb.linearVelocity.x) * lookAheadDistance;
        }
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSmooth * Time.deltaTime);

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x + currentLookAhead,
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
