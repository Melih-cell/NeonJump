using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool moveRight = false;

    [Header("Patrol Settings")]
    public float patrolWaitTime = 0.5f;  // Dönüşlerde bekleme süresi
    public float directionChangeCooldown = 0.3f;  // Yön değişim cooldown'u
    public float stuckCheckTime = 1f;  // Sıkışma kontrol süresi
    public float minMoveDistance = 0.1f;  // Minimum hareket mesafesi

    [Header("Detection")]
    public float wallCheckDistance = 0.5f;
    public float groundCheckDistance = 1f;
    public LayerMask groundLayer;

    [Header("Animation Mode")]
    public AnimationMode animationMode = AnimationMode.CodeEffects;

    public enum AnimationMode
    {
        Animator,
        SpriteSheet,
        CodeEffects
    }

    [Header("Animator Settings")]
    public string walkAnimParam = "IsWalking";
    public string speedAnimParam = "Speed";
    public string dieAnimTrigger = "Die";

    [Header("Code Effects")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.15f;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.05f;
    public bool enableBobbing = true;
    public bool enablePulsing = true;
    public bool enableGlowEffect = false;
    public Color glowColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Item Drop")]
    [Range(0f, 1f)]
    public float dropChance = 0.2f; // %20 şans
    public bool canDropItems = true;

    [Header("Weapon Drop")]
    [Range(0f, 1f)]
    public float weaponDropChance = 0.1f; // %10 şans silah düşürme
    public bool canDropWeapons = true;

    [Header("Sprite Sheet Animation")]
    public Sprite[] walkSprites;
    public Sprite[] idleSprites;
    public Sprite[] deathSprites;
    public float spriteAnimFPS = 10f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isDead = false;
    private bool isInitialized = false;

    // Patrol state
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float directionCooldownTimer = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    private Vector3 startLocalPos;
    private Vector3 baseScale;
    private Color originalColor;
    private float effectTimer;

    private Sprite[] currentAnimation;
    private int currentFrame;
    private float animTimer;
    private float frameTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogError("Enemy: Rigidbody2D bulunamadi!");
            enabled = false;
            return;
        }

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        startLocalPos = transform.localPosition;
        baseScale = transform.localScale;
        originalColor = spriteRenderer.color;
        frameTime = 1f / spriteAnimFPS;

        if (animationMode == AnimationMode.Animator && animator == null)
        {
            Debug.LogWarning("Animator bulunamadi, CodeEffects kullaniliyor.");
            animationMode = AnimationMode.CodeEffects;
        }

        if (animationMode == AnimationMode.SpriteSheet)
        {
            if ((walkSprites == null || walkSprites.Length == 0) && (idleSprites == null || idleSprites.Length == 0))
            {
                animationMode = AnimationMode.CodeEffects;
            }
            else
            {
                currentAnimation = walkSprites != null && walkSprites.Length > 0 ? walkSprites : idleSprites;
            }
        }

        isInitialized = true;
        gameObject.tag = "Enemy";
        lastPosition = transform.position;
    }

    void Update()
    {
        if (isDead || !isInitialized || rb == null) return;
        HandleMovement();
        HandleAnimation();
    }

    void HandleMovement()
    {
        // Cooldown timer'ları güncelle
        if (directionCooldownTimer > 0)
            directionCooldownTimer -= Time.deltaTime;

        // Bekleme durumu
        if (isWaiting)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
            }
            return;
        }

        // Sıkışma kontrolü
        CheckIfStuck();

        float direction = moveRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        if (spriteRenderer != null)
            spriteRenderer.flipX = !moveRight;

        // Yön değiştirme kontrolü (cooldown'da değilse)
        if (directionCooldownTimer <= 0 && ShouldChangeDirection(direction))
        {
            ChangeDirection();
        }
    }

    bool ShouldChangeDirection(float direction)
    {
        // Duvar kontrolü
        Vector2 wallCheckPos = (Vector2)transform.position + new Vector2(direction * 0.5f, 0);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, Vector2.right * direction, wallCheckDistance, groundLayer);

        if (wallHit.collider == null)
        {
            wallHit = Physics2D.Raycast(wallCheckPos, Vector2.right * direction, wallCheckDistance);
            if (wallHit.collider != null && (wallHit.collider.gameObject == gameObject || wallHit.collider.CompareTag("Enemy") || wallHit.collider.isTrigger))
                wallHit = new RaycastHit2D();
        }

        if (wallHit.collider != null)
            return true;

        // Zemin kontrolü
        Vector2 groundCheckPos = (Vector2)transform.position + new Vector2(direction * 0.5f, -0.5f);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistance, groundLayer);

        if (groundHit.collider == null)
        {
            groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistance);
            if (groundHit.collider != null && groundHit.collider.isTrigger)
                groundHit = new RaycastHit2D();
        }

        if (groundHit.collider == null)
            return true;

        return false;
    }

    void ChangeDirection()
    {
        moveRight = !moveRight;
        directionCooldownTimer = directionChangeCooldown;

        // Dönüşte bekle
        if (patrolWaitTime > 0)
        {
            isWaiting = true;
            waitTimer = patrolWaitTime;
        }
    }

    void CheckIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance < minMoveDistance)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckCheckTime)
            {
                // Sıkıştık, yön değiştir
                ChangeDirection();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    void HandleAnimation()
    {
        effectTimer += Time.deltaTime;

        switch (animationMode)
        {
            case AnimationMode.Animator:
                UpdateAnimator();
                break;
            case AnimationMode.SpriteSheet:
                UpdateSpriteAnimation();
                ApplyCodeEffects();
                break;
            case AnimationMode.CodeEffects:
                ApplyCodeEffects();
                break;
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        bool isWalking = speed > 0.1f;

        if (HasParameter(walkAnimParam, AnimatorControllerParameterType.Bool))
            animator.SetBool(walkAnimParam, isWalking);

        if (HasParameter(speedAnimParam, AnimatorControllerParameterType.Float))
            animator.SetFloat(speedAnimParam, speed);

        if (enableBobbing || enablePulsing || enableGlowEffect)
            ApplyCodeEffects();
    }

    void UpdateSpriteAnimation()
    {
        if (currentAnimation == null || currentAnimation.Length == 0) return;

        bool isWalking = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        Sprite[] targetAnim = isWalking ?
            (walkSprites != null && walkSprites.Length > 0 ? walkSprites : currentAnimation) :
            (idleSprites != null && idleSprites.Length > 0 ? idleSprites : currentAnimation);

        if (targetAnim != currentAnimation)
        {
            currentAnimation = targetAnim;
            currentFrame = 0;
            animTimer = 0;
        }

        animTimer += Time.deltaTime;
        if (animTimer >= frameTime)
        {
            animTimer = 0;
            currentFrame = (currentFrame + 1) % currentAnimation.Length;
        }

        if (spriteRenderer != null && currentAnimation.Length > 0)
            spriteRenderer.sprite = currentAnimation[currentFrame];
    }

    void ApplyCodeEffects()
    {
        if (spriteRenderer == null) return;

        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin(effectTimer * bobSpeed) * bobAmount;
            transform.localPosition = new Vector3(transform.localPosition.x, startLocalPos.y + bobOffset, transform.localPosition.z);
        }

        if (enablePulsing)
        {
            float pulseScale = 1f + Mathf.Sin(effectTimer * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * pulseScale;
        }

        if (enableGlowEffect)
        {
            float glowIntensity = (Mathf.Sin(effectTimer * pulseSpeed * 2f) + 1f) * 0.5f;
            spriteRenderer.color = Color.Lerp(originalColor, glowColor, glowIntensity * 0.3f);
        }
    }

    bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Eşya düşür
        TryDropItem();

        if (animator != null && HasParameter(dieAnimTrigger, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(dieAnimTrigger);

        if (animationMode == AnimationMode.SpriteSheet && deathSprites != null && deathSprites.Length > 0)
        {
            StartCoroutine(PlayDeathAnimation());
            return;
        }

        PerformDefaultDeath();
    }

    void TryDropItem()
    {
        Vector3 dropPos = transform.position + Vector3.up * 0.5f;

        // Önce silah düşürme şansını kontrol et - YENİ RARITY SİSTEMİ İLE
        if (canDropWeapons && Random.value <= weaponDropChance)
        {
            // WeaponDrop sistemi ile rastgele rarity'li silah düşür
            WeaponType[] possibleWeapons = new WeaponType[]
            {
                WeaponType.Rifle,
                WeaponType.Shotgun,
                WeaponType.SMG,
                WeaponType.Sniper,
                WeaponType.RocketLauncher,
                WeaponType.GrenadeLauncher
            };

            WeaponType weaponType = possibleWeapons[Random.Range(0, possibleWeapons.Length)];
            WeaponRarity rarity = WeaponRarityHelper.GetRandomRarity();

            // Yeni drop sistemi kullan
            WeaponDrop.SpawnDrop(dropPos, weaponType, rarity);
            return;
        }

        // Normal eşya düşürme
        if (!canDropItems) return;
        if (Random.value > dropChance) return;

        // Rastgele bir eşya türü seç (mermi veya sağlık)
        // %50 mermi, %50 diğer eşyalar
        if (Random.value < 0.5f)
        {
            // Mermi düşür
            AmmoPickup.Spawn(dropPos);
        }
        else
        {
            // Diğer eşyalar
            ItemType[] possibleDrops = new ItemType[]
            {
                ItemType.HealthPotion,
                ItemType.Shield,
                ItemType.SpeedBoost,
                ItemType.Bomb
            };

            ItemType dropType = possibleDrops[Random.Range(0, possibleDrops.Length)];
            CollectibleItem.Spawn(dropType, dropPos);
        }
    }

    System.Collections.IEnumerator PlayDeathAnimation()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        for (int i = 0; i < deathSprites.Length; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = deathSprites[i];
            yield return new WaitForSeconds(frameTime);
        }

        yield return StartCoroutine(FadeOut(0.3f));
        Destroy(gameObject);
    }

    void PerformDefaultDeath()
    {
        transform.localScale = new Vector3(baseScale.x, baseScale.y * 0.3f, baseScale.z);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        StartCoroutine(FadeAndDestroy());
    }

    System.Collections.IEnumerator FadeOut(float duration)
    {
        float elapsed = 0;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }
    }

    System.Collections.IEnumerator FadeAndDestroy()
    {
        yield return StartCoroutine(FadeOut(0.5f));
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        float direction = moveRight ? 1f : -1f;

        // Duvar kontrolü (kırmızı)
        Gizmos.color = Color.red;
        Vector2 wallCheckPos = (Vector2)transform.position + new Vector2(direction * 0.5f, 0);
        Gizmos.DrawLine(wallCheckPos, wallCheckPos + Vector2.right * direction * wallCheckDistance);

        // Zemin kontrolü (yeşil)
        Gizmos.color = Color.green;
        Vector2 groundCheckPos = (Vector2)transform.position + new Vector2(direction * 0.5f, -0.5f);
        Gizmos.DrawLine(groundCheckPos, groundCheckPos + Vector2.down * groundCheckDistance);

        // Hareket yönü (mavi ok)
        Gizmos.color = Color.blue;
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = arrowStart + Vector3.right * direction * 1f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(-direction * 0.2f, 0.2f, 0));
        Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(-direction * 0.2f, -0.2f, 0));

        // Bekleme durumu (sarı daire)
        if (Application.isPlaying && isWaiting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
