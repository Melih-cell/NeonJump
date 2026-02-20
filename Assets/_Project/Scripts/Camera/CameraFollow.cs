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
    public float minY = -100f;
    public float maxY = 100f;

    [Header("Screen Shake")]
    public float maxShakeIntensity = 0.5f;
    private float shakeIntensity = 0f;
    private float shakeDuration = 0f;
    private float shakeTimer = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    // Advanced shake parameters
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
    public float lookAheadSmooth = 8f;
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

    [Header("Dynamic Zoom")]
    [Tooltip("Enable speed-based dynamic zoom")]
    public bool enableDynamicZoom = true;
    [Tooltip("Extra zoom out amount during dash")]
    public float dashZoomOutAmount = 0.5f;
    [Tooltip("Speed threshold to start zooming out")]
    public float zoomSpeedThreshold = 12f;
    [Tooltip("Max zoom out based on speed")]
    public float speedZoomOutMax = 0.3f;
    [Tooltip("How fast zoom transitions happen")]
    public float zoomSmoothSpeed = 3f;

    [Header("Camera Breathing")]
    [Tooltip("Enable subtle idle breathing motion")]
    public bool enableBreathing = false;
    [Tooltip("Breathing amplitude in units")]
    public float breathingAmplitude = 0.012f;
    [Tooltip("Breathing cycles per second")]
    public float breathingFrequency = 0.4f;

    [Header("Vertical Look-Ahead")]
    [Tooltip("Vertical look-ahead distance when falling")]
    public float verticalLookAheadDown = 0f;
    [Tooltip("Vertical look-ahead distance when jumping")]
    public float verticalLookAheadUp = 0f;
    [Tooltip("Vertical look-ahead smooth speed")]
    public float verticalLookAheadSmooth = 5f;

    [Header("Landing Impact")]
    [Tooltip("Enable camera dip on landing from height")]
    public bool enableLandingImpact = false;
    [Tooltip("Max camera dip distance on landing")]
    public float landingImpactMax = 0.25f;
    [Tooltip("Fall speed threshold to trigger landing impact")]
    public float landingImpactThreshold = 12f;
    [Tooltip("How fast the landing impact recovers")]
    public float landingImpactRecoverySpeed = 6f;

    private float currentLookAhead = 0f;
    private float currentVerticalLookAhead = 0f;
    private bool isMobilePlatform = false;
    private float baseOrthographicSize;
    private Vector3 lastTargetPosition;
    private Camera cam;
    private Rigidbody2D targetRb;
    private PlayerController targetPC;

    // Dynamic zoom state
    private float currentZoomOffset = 0f;
    private float targetZoomOffset = 0f;

    // Landing impact state
    private float landingImpactOffset = 0f;
    private bool wasGroundedLastFrame = false;
    private float lastFallSpeed = 0f;

    // Freeze state
    private bool isFrozen = false;
    private Vector3 frozenPosition;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (target != null)
        {
            lastTargetPosition = target.position;
            CacheTargetComponents();
        }

        isMobilePlatform = Application.isMobilePlatform;

        #if UNITY_EDITOR
        if (MobileControls.Instance != null && MobileControls.Instance.IsEnabled)
            isMobilePlatform = true;
        #endif

        cam = GetComponent<Camera>();
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
        if (isFrozen)
        {
            transform.position = frozenPosition;
            return;
        }

        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        // Cache components if missing
        if (targetRb == null)
            CacheTargetComponents();

        Vector3 desiredPosition = GetTargetPosition();

        // Dead zone
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

        // Asymmetric Y follow
        float ySmooth = smoothSpeed;
        if (target != null)
        {
            float yVelocity = target.position.y - lastTargetPosition.y;
            if (yVelocity > 0.01f)
            {
                ySmooth = smoothSpeed * upSmoothMultiplier;
            }
            else if (yVelocity < -0.01f)
            {
                ySmooth = smoothSpeed * downSmoothMultiplier;
            }
            lastTargetPosition = target.position;
        }

        Vector3 smoothedPosition = new Vector3(
            Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime),
            Mathf.Lerp(transform.position.y, targetY, ySmooth * Time.deltaTime),
            desiredPosition.z
        );

        // Landing impact offset
        UpdateLandingImpact();
        smoothedPosition.y += landingImpactOffset;

        // Camera breathing
        if (enableBreathing)
        {
            smoothedPosition += GetBreathingOffset();
        }

        // Screen shake
        UpdateShake();
        smoothedPosition += shakeOffset;

        // Dynamic zoom
        UpdateDynamicZoom();

        transform.position = smoothedPosition;
    }

    void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            float progress = 1f - (shakeTimer / shakeDuration);
            float falloff = 1f - (progress * progress);
            float currentIntensity = shakeIntensity * falloff;
            currentIntensity = Mathf.Min(currentIntensity, maxShakeIntensity);

            if (useDirectionalShake && shakeDirection != Vector2.zero)
            {
                float noise = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
                float perpNoise = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;

                Vector2 perpDir = new Vector2(-shakeDirection.y, shakeDirection.x);
                Vector2 shake = shakeDirection * noise * currentIntensity +
                               perpDir * perpNoise * currentIntensity * 0.5f;

                shakeOffset = new Vector3(shake.x, shake.y, 0);
            }
            else
            {
                float noiseX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
                float noiseY = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;

                shakeOffset = new Vector3(
                    noiseX * currentIntensity,
                    noiseY * currentIntensity,
                    0
                );
            }

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

    // === DYNAMIC ZOOM ===

    void UpdateDynamicZoom()
    {
        if (!enableDynamicZoom || cam == null) return;

        targetZoomOffset = 0f;

        if (targetRb != null)
        {
            // Dash zoom out
            if (targetPC != null && targetPC.IsDashing())
            {
                targetZoomOffset = dashZoomOutAmount;
            }
            else
            {
                // Speed-based zoom out
                float speed = targetRb.linearVelocity.magnitude;
                if (speed > zoomSpeedThreshold)
                {
                    float t = Mathf.Clamp01((speed - zoomSpeedThreshold) / (zoomSpeedThreshold * 1.5f));
                    targetZoomOffset = t * speedZoomOutMax;
                }
            }
        }

        currentZoomOffset = Mathf.Lerp(currentZoomOffset, targetZoomOffset, zoomSmoothSpeed * Time.deltaTime);

        float mobileBase = isMobilePlatform ? baseOrthographicSize - mobileZoomOutOffset : baseOrthographicSize;
        cam.orthographicSize = mobileBase + currentZoomOffset;
    }

    // === CAMERA BREATHING ===

    Vector3 GetBreathingOffset()
    {
        // Only breathe when player is mostly still
        float speedSqr = 0f;
        if (targetRb != null)
            speedSqr = targetRb.linearVelocity.sqrMagnitude;

        // Fade out breathing when moving fast
        float breathFade = Mathf.Clamp01(1f - speedSqr / 25f);
        if (breathFade < 0.01f) return Vector3.zero;

        float t = Time.time * breathingFrequency * Mathf.PI * 2f;
        float yBreath = Mathf.Sin(t) * breathingAmplitude * breathFade;
        float xBreath = Mathf.Sin(t * 0.7f) * breathingAmplitude * 0.3f * breathFade;

        return new Vector3(xBreath, yBreath, 0f);
    }

    // === LANDING IMPACT ===

    void UpdateLandingImpact()
    {
        if (!enableLandingImpact)
        {
            landingImpactOffset = 0f;
            return;
        }

        bool isGrounded = false;
        if (targetPC != null)
            isGrounded = targetPC.IsGrounded;

        // Track fall speed while airborne
        if (!isGrounded && targetRb != null && targetRb.linearVelocity.y < 0)
        {
            lastFallSpeed = Mathf.Abs(targetRb.linearVelocity.y);
        }

        // Detect landing moment
        if (isGrounded && !wasGroundedLastFrame)
        {
            if (lastFallSpeed > landingImpactThreshold)
            {
                float normalized = Mathf.Clamp01((lastFallSpeed - landingImpactThreshold) / 15f);
                landingImpactOffset = -normalized * landingImpactMax;
            }
            lastFallSpeed = 0f;
        }

        // Recover from impact
        if (landingImpactOffset < 0f)
        {
            landingImpactOffset = Mathf.Lerp(landingImpactOffset, 0f, landingImpactRecoverySpeed * Time.deltaTime);
            if (landingImpactOffset > -0.005f)
                landingImpactOffset = 0f;
        }

        wasGroundedLastFrame = isGrounded;
    }

    // === SHAKE METHODS ===

    public void Shake(float intensity, float duration)
    {
        if (isMobilePlatform)
        {
            intensity *= mobileShakeMultiplier;
        }

        if (intensity > shakeIntensity || shakeTimer <= 0)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
    }

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

    // === ADVANCED SHAKE METHODS ===

    public void DirectionalShake(float intensity, float duration, Vector2 direction)
    {
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

    public void ShakeFromExplosion(Vector3 explosionPos, float intensity, float duration)
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)explosionPos).normalized;
        DirectionalShake(intensity, duration, direction);
    }

    public void ShakeOnLand(float fallSpeed)
    {
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(fallSpeed) / 20f);
        float intensity = normalizedSpeed * 0.3f;

        if (intensity > 0.05f)
        {
            DirectionalShake(intensity, 0.15f, Vector2.down);
        }
    }

    public void ShakeOnBossAttack()
    {
        Shake(0.4f, 0.35f);
    }

    public void ShakeOnBossDeath()
    {
        Shake(0.6f, 0.5f);
    }

    public void ShakeOnCriticalHit()
    {
        Shake(0.25f, 0.12f);
    }

    public void ShakeOnPlayerHit(Vector2 hitDirection)
    {
        DirectionalShake(0.35f, 0.2f, hitDirection);
    }

    public void ShakeOnFire(float recoilStrength = 0.05f)
    {
        Shake(recoilStrength, 0.05f);
    }

    public void ShakeOnCombo(int comboLevel)
    {
        float intensity = Mathf.Min(0.1f + comboLevel * 0.02f, 0.3f);
        Shake(intensity, 0.1f);
    }

    /// <summary>
    /// Hit freeze effect - briefly freezes the game (hitstop)
    /// Used for enemy kills and boss damage
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

    public void HitFreezeOnKill()
    {
        HitFreeze(0.04f);
    }

    public void HitFreezeOnBossDamage()
    {
        HitFreeze(0.06f);
    }

    // === TARGET POSITION WITH ENHANCED LOOK-AHEAD ===

    Vector3 GetTargetPosition()
    {
        if (target == null) return transform.position;

        // Horizontal look-ahead based on velocity direction and magnitude
        float targetLookAhead = 0f;
        float targetVertLookAhead = 0f;

        if (targetRb != null)
        {
            float velX = targetRb.linearVelocity.x;
            float velY = targetRb.linearVelocity.y;

            // Horizontal: scale look-ahead by speed for smoother feel
            if (Mathf.Abs(velX) > 0.5f)
            {
                float speedFactor = Mathf.Clamp01(Mathf.Abs(velX) / 15f);
                targetLookAhead = Mathf.Sign(velX) * lookAheadDistance * (0.5f + 0.5f * speedFactor);
            }

            // Vertical look-ahead: look down when falling, slightly up when jumping
            if (velY < -2f)
            {
                float fallFactor = Mathf.Clamp01(Mathf.Abs(velY) / 20f);
                targetVertLookAhead = -verticalLookAheadDown * fallFactor;
            }
            else if (velY > 2f)
            {
                float riseFactor = Mathf.Clamp01(velY / 15f);
                targetVertLookAhead = verticalLookAheadUp * riseFactor;
            }
        }

        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSmooth * Time.deltaTime);
        currentVerticalLookAhead = Mathf.Lerp(currentVerticalLookAhead, targetVertLookAhead, verticalLookAheadSmooth * Time.deltaTime);

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x + currentLookAhead,
            target.position.y + offset.y + currentVerticalLookAhead,
            offset.z
        );

        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

        return desiredPosition;
    }

    // === UTILITY ===

    void CacheTargetComponents()
    {
        if (target == null) return;
        targetRb = target.GetComponent<Rigidbody2D>();
        targetPC = target.GetComponent<PlayerController>();
    }

    public void SnapToTarget()
    {
        if (target == null) FindTarget();
        if (target == null) return;
        transform.position = GetTargetPosition();
    }

    /// <summary>
    /// Freeze camera at current position (for death)
    /// </summary>
    public void Freeze()
    {
        isFrozen = true;
        frozenPosition = transform.position;
    }

    /// <summary>
    /// Unfreeze camera (for respawn)
    /// </summary>
    public void Unfreeze()
    {
        isFrozen = false;
    }

    void FindTarget()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            target = pc.transform;
            CacheTargetComponents();
            return;
        }

        GameObject player = GameObject.Find("Player_Asker");

        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player == null)
            player = GameObject.Find("Player");

        if (player != null)
        {
            target = player.transform;
            CacheTargetComponents();
        }
    }
}
