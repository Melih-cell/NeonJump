using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 7f;
    public float bounceForce = 6f;
    public float fallMultiplier = 2.5f;  // Dusus hizlandirici
    public float lowJumpMultiplier = 2f; // Kisa ziplama

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f; // Daha hassas zemin kontrolu
    public LayerMask groundLayer;

    [Header("Wall Jump Settings")]
    public float wallCheckDistance = 0.5f; // Duvar algılama mesafesi
    public float wallSlideSpeed = 2f; // Kayma hizi
    public float wallJumpForceX = 2f;
    public float wallJumpForceY = 7f;
    public float wallJumpDuration = 0.15f;
    public LayerMask wallLayer; // Wall layer - Inspector'dan ayarla
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private float wallJumpTimer = 0f;
    private int wallJumpDirection = 0;

    [Header("Invincibility")]
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    public float invincibleDuration = 2f;
    private bool isPowerUpInvincible = false; // Power-up'tan gelen dokunulmazlik

    [Header("Death")]
    private bool isDead = false;
    public float deathAnimDuration = 2.5f; // Ölüm animasyonu süresi (Inspector'dan ayarlanabilir)

    [Header("Jump Settings")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Animation")]
    public Sprite idleSprite;
    public Sprite[] runSprites;
    public Sprite jumpSprite;
    public Sprite fallSprite;
    public float animationSpeed = 0.1f;

    [Header("Fire Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.3f;
    public float bulletSpeed = 15f;
    public float bulletDamage = 1f;
    private float nextFireTime = 0f;
    private bool isFiring = false;
    private bool useWeaponManager = true; // Yeni silah sistemi kullan

    [Header("Roll/Takla Settings")]
    public float rollSpeed = 12f;
    public float rollDuration = 0.5f;
    public float rollCooldown = 1f;
    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private int rollDirection = 1;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public bool canAirDash = true;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private int dashDirection = 1;
    private bool hasAirDashed = false; // Havada sadece 1 kez dash

    // Ziplama takla kontrolu
    private bool isJumpRolling = false;
    private float jumpRollMinTime = 0.35f; // Minimum takla suresi (artirildi - animasyonun tamamlanmasi icin)
    private float jumpRollTimer = 0f;
    private bool wasInAir = false; // Havada miydi kontrolu
    private bool jumpRollAnimStarted = false; // Animasyon baslatildi mi?

    // Yere iniş animasyonu (sadece çok yüksek düşüşlerde)
    private bool isLanding = false;
    private float landingTimer = 0f;
    private float hardFallSpeed = -16f; // Bu hızdan hızlı düşerse sendeleme olur
    private float lastFallSpeed = 0f; // Son düşme hızı
    private float currentLandingSlowdown = 1f; // Sendeleme yavaşlaması

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded;
    private float horizontalInput;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Animator animator;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isTouchingWall;

    // Animation
    private float animTimer;
    private int currentFrame;
    private bool spritesCreated = false;

    // Cached bullet sprite (runtime texture'i tekrar tekrar olusturmamak icin)
    private static Sprite cachedBulletSprite = null;

    // Jump tracking for HUD
    private int currentJumpCount = 0;

    // === PUBLIC PROPERTIES FOR HUD ===
    public float DashCooldownTimer => dashCooldownTimer;
    public float DashCooldownMax => dashCooldown;
    public float RollCooldownTimer => rollCooldownTimer;
    public float RollCooldownMax => rollCooldown;
    public bool CanDash => !isDashing && !isRolling && dashCooldownTimer <= 0;
    public bool CanRoll => !isRolling && !isDashing && rollCooldownTimer <= 0 && isGrounded;
    public int JumpCount => currentJumpCount;
    public int MaxJumpCount => 2; // Double jump destegi
    public bool IsGrounded => isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        // Player tag'ini ayarla
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }

        // Animator'u kendinde veya child'larda ara
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }


        // Rigidbody ayarlari
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Baslangic degerleri
        isGrounded = true;
        coyoteTimeCounter = coyoteTime;
    }

    void Update()
    {
        // Ölüyken sadece animasyonu güncelle
        if (isDead)
        {
            UpdateAnimation();
            return;
        }

        // Oyun bittiyse hareket etme
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        // Invincibility timer
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (spriteRenderer != null)
                spriteRenderer.enabled = Mathf.Sin(Time.time * 20f) > 0;

            if (invincibleTimer <= 0)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }
        }

        // Input
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        horizontalInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontalInput = -1f;
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontalInput = 1f;

        // Coyote time (yere degdikten sonra kisa sure ziplama hakki)
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump buffer (ziplama tusuna basildiginda kisa sure bekle)
        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Ziplama
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            currentJumpCount = 1;

            // Efektler
            if (ParticleManager.Instance != null && groundCheck != null)
                ParticleManager.Instance.PlayJumpDust(groundCheck.position);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayJump();

            // Ziplama animasyonu
            isJumpRolling = true;
            jumpRollTimer = jumpRollMinTime;
            wasInAir = false;
            jumpRollAnimStarted = false;
            currentAnimState = "";
            isGrounded = false;
        }
        // Double Jump (Power-up)
        else if (jumpBufferCounter > 0 && !isGrounded && PowerUpManager.Instance != null)
        {
            if (PowerUpManager.Instance.TryDoubleJump())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.9f);
                jumpBufferCounter = 0;
                currentJumpCount++; // Double jump

                // Ziplama sesi
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayJump();
            }
        }

        // === WALL SLIDE & WALL JUMP ===
        int wallDir = CheckWallDirection();

        // Wall Jump timer guncelle
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }

        // === WALL JUMP ===
        // isGrounded kontrolünü kaldırdık - duvardayken zıplayabilsin
        bool canWallJump = !isWallJumping && wallDir != 0 && !isRolling && !isDashing;

        if (canWallJump && jumpBufferCounter > 0)
        {
            isWallJumping = true;
            isWallSliding = false;
            wallJumpTimer = wallJumpDuration;
            wallJumpDirection = -wallDir;

            // Önce velocity'yi sıfırla, sonra yeni değeri ata
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForceX, wallJumpForceY);
            jumpBufferCounter = 0;
            groundContactCount = 0; // Zeminden ayrıldık

            isJumpRolling = true;
            jumpRollTimer = jumpRollMinTime;
            wasInAir = true;
            jumpRollAnimStarted = false;
            currentAnimState = "";

            transform.localScale = new Vector3(wallJumpDirection * Mathf.Abs(transform.localScale.x),
                                               transform.localScale.y, transform.localScale.z);

            if (ParticleManager.Instance != null)
                ParticleManager.Instance.PlayJumpDust(transform.position);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayJump();
        }

        // === WALL SLIDE ===
        bool isFalling = rb.linearVelocity.y < 0;
        bool canStartWallSlide = !isGrounded && !isWallJumping && wallDir != 0 && !isRolling && !isDashing && isFalling;

        if (canStartWallSlide)
        {
            bool movingAwayFromWall = (wallDir == 1 && horizontalInput < -0.5f) || (wallDir == -1 && horizontalInput > 0.5f);

            if (!movingAwayFromWall)
            {
                if (!isWallSliding)
                    currentAnimState = "";

                isWallSliding = true;
                isJumpRolling = false;
                jumpRollAnimStarted = false;

                // Sprite duvardan dışarı baksın
                transform.localScale = new Vector3(wallDir * Mathf.Abs(transform.localScale.x),
                                                   transform.localScale.y, transform.localScale.z);
            }
            else
            {
                isWallSliding = false;
            }
        }
        else
        {
            isWallSliding = false;
        }

        // === ATES ETME (J veya Sol Mouse) ===
        Mouse mouse = Mouse.current;
        bool firePressed = keyboard.jKey.isPressed || (mouse != null && mouse.leftButton.isPressed);
        bool fireJustPressed = keyboard.jKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame);

        // === 8 YONLU NISAN SISTEMI ===
        Vector2 aimDirection = CalculateAimDirection(keyboard);

        // WeaponManager varsa onu kullan
        if (useWeaponManager && WeaponManager.Instance != null)
        {
            // Nisan yonunu WeaponManager'a SUREKLI bildir (FirePoint pozisyonu icin)
            // Bu sayede karakter donunce FirePoint da guncellenir
            WeaponManager.Instance.UpdateAimDirection(aimDirection);

            bool isAutomatic = WeaponManager.Instance.IsCurrentWeaponAutomatic();

            // Otomatik silahlar icin basili tutma, tekli silahlar icin tek atis
            bool shouldFire = isAutomatic ? firePressed : fireJustPressed;

            if (shouldFire && !isRolling)
            {
                // WeaponManager'dan ates et
                if (WeaponManager.Instance.TryFire(aimDirection))
                {
                    // Ates sesi
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayFire();
                }
            }
            isFiring = firePressed;
        }
        else
        {
            // Eski sistem (fallback)
            if (firePressed && Time.time >= nextFireTime && !isRolling)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
            isFiring = firePressed;
        }

        // === TAKLA / ROLL (K veya Left Shift - sadece yerde) ===
        rollCooldownTimer -= Time.deltaTime;

        if ((keyboard.kKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame)
            && !isRolling && !isDashing && rollCooldownTimer <= 0 && isGrounded)
        {
            StartRoll();
        }

        // Roll durumu guncelle
        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0)
            {
                EndRoll();
            }
        }

        // === DASH (Left Ctrl - yerde ve havada) ===
        dashCooldownTimer -= Time.deltaTime;

        // Yere deyince air dash hakkini yenile
        if (isGrounded)
        {
            hasAirDashed = false;
        }

        bool canDash = !isDashing && !isRolling && dashCooldownTimer <= 0;
        bool airDashAllowed = canAirDash && !hasAirDashed;

        if (keyboard.leftCtrlKey.wasPressedThisFrame && canDash && (isGrounded || airDashAllowed))
        {
            StartDash();
        }

        // Dash durumu guncelle
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        // Jump roll timer guncelle
        if (isJumpRolling)
        {
            jumpRollTimer -= Time.deltaTime;

            // Havadayken wasInAir'i true yap
            if (!isGrounded)
            {
                wasInAir = true;
            }

            // Sadece havada olup yere dusunce bitir (ve minimum sure gectiyse)
            // VEYA cok uzun surerse zorla bitir (guvenklik icin)
            bool shouldEnd = (jumpRollTimer <= 0 && isGrounded && wasInAir) || jumpRollTimer < -0.5f;

            if (shouldEnd)
            {
                isJumpRolling = false;
                wasInAir = false;
                jumpRollAnimStarted = false;
            }
        }

        // Havadayken düşme hızını takip et
        if (!isGrounded && rb.linearVelocity.y < 0)
        {
            lastFallSpeed = rb.linearVelocity.y;
        }

        // Yere iniş animasyonu timer
        if (isLanding)
        {
            landingTimer -= Time.deltaTime;
            if (landingTimer <= 0)
            {
                isLanding = false;
            }
        }

        // Karakter yonu (takla ve wall slide sirasinda degistirme)
        if (!isRolling && !isWallSliding)
        {
            if (horizontalInput > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                rollDirection = 1;
            }
            else if (horizontalInput < 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                rollDirection = -1;
            }
        }

        // Animasyon guncelle
        UpdateAnimation();
    }

    // Animasyon state takibi
    private string currentAnimState = "";

    void PlayAnim(string stateName, float transitionTime = 0.1f)
    {
        if (currentAnimState == stateName) return;

        // CrossFade ile yumusak gecis
        animator.CrossFade(stateName, transitionTime);
        currentAnimState = stateName;
    }

    void UpdateAnimation()
    {
        // Animator varsa onu kullan
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsFiring", isFiring);
            animator.SetBool("IsRolling", isRolling || isJumpRolling);
            animator.SetBool("IsWallSliding", isWallSliding);
            animator.SetBool("IsDashing", isDashing);
            animator.SetBool("IsDead", isDead);

            // Düşme kontrolü - yerde değil ve aşağı doğru hareket ediyorsa
            bool isFalling = !isGrounded && rb.linearVelocity.y < -0.5f;
            animator.SetBool("IsFalling", isFalling);

            // Ölüm animasyonu - diğer her şeyden önce kontrol et
            if (isDead)
            {
                animator.speed = 1f;
                PlayAnim("player_Dead", 0.02f);
                return; // Ölüyken başka animasyon oynatma
            }

            // Direkt animasyon kontrolu - transition'lara guvenmiyoruz
            if (isDashing)
            {
                animator.speed = 1.5f; // Dash animasyonu biraz hızlı
                PlayAnim("AskerTakla", 0.02f); // Dash icin hizli takla animasyonu
            }
            else if (isJumpRolling || isRolling)
            {
                animator.speed = 1.2f; // Biraz hizli takla animasyonu

                if (!jumpRollAnimStarted)
                {
                    jumpRollAnimStarted = true;
                    animator.Play("AskerTakla", 0, 0f);
                }
                else
                {
                    PlayAnim("AskerTakla", 0.02f);
                }
            }
            else if (isWallSliding)
            {
                animator.speed = 0.8f;
                if (currentAnimState != "player_Slide")
                {
                    animator.Play("player_Slide", 0, 0f);
                    currentAnimState = "player_Slide";
                }
            }
            else if (isLanding)
            {
                // Yere iniş/destek alma animasyonu - yüksekten düştükten sonra
                animator.speed = 2f; // Animasyonu 2x hızlı oynat
                PlayAnim("player_Fall", 0.02f); // Çok hızlı geçiş
            }
            else if (isFiring)
            {
                animator.speed = 1f; // Normal hız
                PlayAnim("FireAnimation", 0.05f);
            }
            else if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
            {
                animator.speed = 1f; // Normal hız
                PlayAnim("Run", 0.05f); // Kosu animasyonu
            }
            else
            {
                animator.speed = 1f; // Normal hız
                PlayAnim("idle", 0.1f); // Biraz daha yavas gecis
            }

            return;
        }

        // Animator yoksa sprite-based animasyon
        if (spriteRenderer == null) return;
        if (isInvincible) return;

        // Dash durumu
        if (isDashing)
        {
            // Dash icin parlak efekt
            spriteRenderer.color = new Color(1f, 1f, 0.5f); // Parlak sari
            if (jumpSprite != null)
                spriteRenderer.sprite = jumpSprite;
            return;
        }
        // Wall Slide durumu
        else if (isWallSliding)
        {
            // Wall slide icin ozel renk/efekt
            spriteRenderer.color = new Color(0.7f, 0.9f, 1f); // Hafif mavi ton
            if (fallSprite != null)
                spriteRenderer.sprite = fallSprite;
            return;
        }
        else if (!isInvincible && !isPowerUpInvincible)
        {
            spriteRenderer.color = new Color(0f, 0.9f, 1f); // Normal renk
        }

        // Havadayken
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.5f)
            {
                if (jumpSprite != null)
                    spriteRenderer.sprite = jumpSprite;
            }
            else
            {
                if (fallSprite != null)
                    spriteRenderer.sprite = fallSprite;
            }
        }
        else
        {
            // Yerdeyken
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= animationSpeed)
                {
                    animTimer = 0;
                    currentFrame = (currentFrame + 1) % runSprites.Length;
                }

                if (runSprites != null && runSprites.Length > 0)
                    spriteRenderer.sprite = runSprites[currentFrame];
            }
            else
            {
                if (idleSprite != null)
                    spriteRenderer.sprite = idleSprite;
                currentFrame = 0;
                animTimer = 0;
            }
        }
    }

    // Collision-based zemin kontrolü
    private int groundContactCount = 0;

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        wasGrounded = isGrounded;

        // Collision-based zemin kontrolü
        isGrounded = groundContactCount > 0;

        if (isGrounded)
        {
            currentJumpCount = 0;
        }

        // Yere inis efekti
        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0)
        {
            if (ParticleManager.Instance != null && groundCheck != null)
                ParticleManager.Instance.PlayLandDust(groundCheck.position);

            // Sadece çok yüksekten düştüyse iniş animasyonu ve sendeleme
            if (lastFallSpeed < hardFallSpeed)
            {
                isLanding = true;
                currentLandingSlowdown = 0.3f; // %30 hareket hızı
                landingTimer = 0.25f;
                currentAnimState = ""; // Animasyonun yeniden başlamasını sağla
            }
            lastFallSpeed = 0f; // Sıfırla
        }

        // Yatay hareket
        float targetVelocityX;
        float targetVelocityY = rb.linearVelocity.y;

        if (isDashing)
        {
            // Dash sirasinda cok hizli yatay hareket, Y sabit
            targetVelocityX = dashDirection * dashSpeed;
            targetVelocityY = 0; // Havada asili kal
        }
        else if (isWallJumping)
        {
            // Wall jump sirasinda yatay hareketi kontrol etme (momentum korunsun)
            targetVelocityX = rb.linearVelocity.x;
        }
        else if (isRolling)
        {
            // Takla sirasinda hizli hareket
            targetVelocityX = rollDirection * rollSpeed;
        }
        else if (isWallSliding)
        {
            // Duvarda kayarken yatay hareketi sinirla
            targetVelocityX = 0;
        }
        else if (isLanding)
        {
            // İniş sırasında yavaşla (sendeleme efekti) - düşme yüksekliğine göre dinamik
            targetVelocityX = horizontalInput * moveSpeed * currentLandingSlowdown;
        }
        else
        {
            targetVelocityX = horizontalInput * moveSpeed;
        }

        rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);

        // Dash sirasinda yercekimi uygulama
        if (isDashing)
        {
            // Dash sirasinda fizik uygulanmaz
        }
        // Wall Slide - duvarda yavas dus
        else if (isWallSliding)
        {
            // Dusme hizini sinirla (yukari veya asagi)
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, rb.linearVelocity.y);
            if (rb.linearVelocity.y < 0)
            {
                clampedY = -wallSlideSpeed; // Yavas dusme
            }
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, clampedY);
        }
        // Gercekci ziplama fizigi - dususte hizlan (wall slide degilse)
        else if (rb.linearVelocity.y < 0)
        {
            // Dusuyorken daha hizli dus
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0)
        {
            // Ziplama tusunu birakirsan kisa zipla
            Keyboard keyboard = Keyboard.current;
            bool jumpHeld = keyboard != null && (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed);

            if (!jumpHeld)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
    }

    // Returns: 0 = no wall, 1 = wall on right, -1 = wall on left
    int CheckWallDirection()
    {
        float colliderHalfWidth = 0.3f;
        float colliderHalfHeight = 0.4f;
        Vector2 position = (Vector2)transform.position;

        if (boxCollider != null)
        {
            position = (Vector2)transform.TransformPoint(boxCollider.offset);
            colliderHalfWidth = boxCollider.size.x * Mathf.Abs(transform.localScale.x) * 0.5f;
            colliderHalfHeight = boxCollider.size.y * Mathf.Abs(transform.localScale.y) * 0.5f;
        }

        // Wall, Ground ve Default layer'larını kontrol et
        LayerMask combinedLayers = wallLayer | groundLayer | (1 << LayerMask.NameToLayer("Default"));

        bool wallRight = false;
        bool wallLeft = false;

        // OverlapBox kullan - duvara yapışıkken bile çalışır
        // Kutu boyutu: ince ve kısa (sadece gövde seviyesinde, ayak seviyesinde değil)
        Vector2 boxSize = new Vector2(wallCheckDistance * 0.5f, colliderHalfHeight * 0.8f);

        // Sağ taraf kontrolü - kutunun merkezi player'ın göğüs hizasında (biraz yukarıda)
        Vector2 rightBoxCenter = position + Vector2.right * (colliderHalfWidth + wallCheckDistance * 0.3f) + Vector2.up * 0.1f;
        Collider2D[] rightHits = Physics2D.OverlapBoxAll(rightBoxCenter, boxSize, 0f, combinedLayers);

        #if UNITY_EDITOR
        Vector2 rbMin = rightBoxCenter - boxSize * 0.5f;
        Vector2 rbMax = rightBoxCenter + boxSize * 0.5f;
        Debug.DrawLine(new Vector2(rbMin.x, rbMin.y), new Vector2(rbMax.x, rbMin.y), Color.cyan);
        Debug.DrawLine(new Vector2(rbMax.x, rbMin.y), new Vector2(rbMax.x, rbMax.y), Color.cyan);
        Debug.DrawLine(new Vector2(rbMax.x, rbMax.y), new Vector2(rbMin.x, rbMax.y), Color.cyan);
        Debug.DrawLine(new Vector2(rbMin.x, rbMax.y), new Vector2(rbMin.x, rbMin.y), Color.cyan);
        #endif

        foreach (Collider2D col in rightHits)
        {
            if (IsValidWallCollider(col))
            {
                wallRight = true;
                break;
            }
        }

        // Sol taraf kontrolü
        Vector2 leftBoxCenter = position + Vector2.left * (colliderHalfWidth + wallCheckDistance * 0.3f) + Vector2.up * 0.1f;
        Collider2D[] leftHits = Physics2D.OverlapBoxAll(leftBoxCenter, boxSize, 0f, combinedLayers);

        #if UNITY_EDITOR
        Vector2 lbMin = leftBoxCenter - boxSize * 0.5f;
        Vector2 lbMax = leftBoxCenter + boxSize * 0.5f;
        Debug.DrawLine(new Vector2(lbMin.x, lbMin.y), new Vector2(lbMax.x, lbMin.y), Color.cyan);
        Debug.DrawLine(new Vector2(lbMax.x, lbMin.y), new Vector2(lbMax.x, lbMax.y), Color.cyan);
        Debug.DrawLine(new Vector2(lbMax.x, lbMax.y), new Vector2(lbMin.x, lbMax.y), Color.cyan);
        Debug.DrawLine(new Vector2(lbMin.x, lbMax.y), new Vector2(lbMin.x, lbMin.y), Color.cyan);
        #endif

        foreach (Collider2D col in leftHits)
        {
            if (IsValidWallCollider(col))
            {
                wallLeft = true;
                break;
            }
        }

        // Eğer OverlapBox bulamadıysa, Raycast ile de dene (uzak duvarlar için)
        if (!wallRight && !wallLeft)
        {
            float[] yOffsets = { 0.2f, 0f, -0.2f };

            foreach (float yOffset in yOffsets)
            {
                Vector2 checkPos = position + Vector2.up * yOffset;

                // Raycast'i player'ın İÇİNDEN başlat (duvara yapışık olsa bile algılar)
                Vector2 rightOrigin = checkPos;
                RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.right, colliderHalfWidth + wallCheckDistance, combinedLayers);

                #if UNITY_EDITOR
                Debug.DrawRay(rightOrigin, Vector2.right * (colliderHalfWidth + wallCheckDistance), hitRight.collider != null ? Color.green : Color.red);
                #endif

                if (hitRight.collider != null && IsValidWallCollider(hitRight.collider))
                {
                    if (Mathf.Abs(hitRight.normal.x) > 0.3f)
                    {
                        wallRight = true;
                    }
                }

                Vector2 leftOrigin = checkPos;
                RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.left, colliderHalfWidth + wallCheckDistance, combinedLayers);

                #if UNITY_EDITOR
                Debug.DrawRay(leftOrigin, Vector2.left * (colliderHalfWidth + wallCheckDistance), hitLeft.collider != null ? Color.green : Color.red);
                #endif

                if (hitLeft.collider != null && IsValidWallCollider(hitLeft.collider))
                {
                    if (Mathf.Abs(hitLeft.normal.x) > 0.3f)
                    {
                        wallLeft = true;
                    }
                }
            }
        }

        if (wallRight) return 1;
        if (wallLeft) return -1;
        return 0;
    }

    // Gecerli duvar collider'i mi kontrol et
    bool IsValidWallCollider(Collider2D col)
    {
        if (col == null) return false;

        // Kendimizi, trigger'lari ve belirli tag'leri atla
        if (col.gameObject == gameObject) return false;
        if (col.transform.IsChildOf(transform)) return false;
        if (col.isTrigger) return false;
        if (col.CompareTag("Player")) return false;
        if (col.CompareTag("Enemy")) return false;
        if (col.CompareTag("Collectible")) return false;
        if (col.CompareTag("Bullet")) return false;

        // Zemin kontrolü: Collider'ın üst kenarı player'ın AYAKLARININ üstünde olmalı
        // Bu sayede yan duvarlar algılanır, ama üzerinde durduğumuz zemin algılanmaz
        float playerFeetY = transform.position.y - 0.4f; // Ayak seviyesi
        float colliderTopY = col.bounds.max.y;

        // Collider'ın üstü player'ın ayaklarından yukarıda olmalı (yan duvar)
        if (colliderTopY < playerFeetY)
        {
            return false; // Bu zemin, duvar değil (altımızda)
        }

        // Gecerli duvar
        return true;
    }

    bool CheckWall()
    {
        return CheckWallDirection() != 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // ZEMIN KONTROLU - alttan temas varsa
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                groundContactCount++;
                break;
            }
        }

        // Dusmana carpma
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy == null) return;

            float playerBottom = transform.position.y - 0.5f;
            float enemyTop = collision.transform.position.y + 0.3f;

            if (playerBottom > enemyTop && rb.linearVelocity.y < 0)
            {
                Vector3 enemyPos = enemy.transform.position;
                enemy.Die();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EnemyKilled(enemyPos);
                }
            }
            else if (!isInvincible)
            {
                TakeDamage();
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // Zeminden ayrıldık
        groundContactCount--;
        if (groundContactCount < 0) groundContactCount = 0;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // Sürekli zemin teması kontrolü
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                if (groundContactCount <= 0) groundContactCount = 1;
                break;
            }
        }
    }

    public void TakeDamage()
    {
        // Power-up invincibility kontrolu
        if (isInvincible || isPowerUpInvincible) return;

        // Kalkan kontrolu
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.TryAbsorbDamage())
        {
            // Kalkan hasari absorbe etti
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHurt();

            // Hafif screen shake
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.Shake(0.15f, 0.1f);
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(1);

            // Hasar sesi
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHurt();

            if (GameManager.Instance.IsGameOver())
            {
                // Oyuncu öldü - ölüm animasyonunu başlat
                Die();
            }
            else
            {
                // Invincible ol
                isInvincible = true;
                invincibleTimer = invincibleDuration;

                // Geri sekme kaldırıldı - kullanıcı istegi
            }
        }
    }

    /// <summary>
    /// Oyuncu ölümü - animasyon ve fizik
    /// </summary>
    public void Die()
    {
        if (isDead) return; // Zaten ölüyse tekrar ölme

        isDead = true;
        currentAnimState = ""; // Animasyonun hemen başlamasını sağla

        // Rigidbody'yi tamamen durdur
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic; // Tamamen fizikten çıkar

        // Collider'ı kapat
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Freeze();
        }
    }

    /// <summary>
    /// Oyuncuyu yeniden canlandır (respawn için)
    /// </summary>
    public void Respawn()
    {
        isDead = false;
        currentAnimState = "";

        // Fizik ayarlarını geri yükle
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;

        // Collider'ı aç
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }

        // Sprite'ı görünür yap
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Unfreeze();
        }
    }

    // Power-up invincibility
    public void SetInvincible(bool value)
    {
        isPowerUpInvincible = value;

        // Visual feedback - parlama efekti
        if (spriteRenderer != null)
        {
            if (value)
            {
                spriteRenderer.color = new Color(1f, 1f, 0.5f); // Sari ton
            }
            else
            {
                spriteRenderer.color = new Color(0f, 0.9f, 1f); // Normal cyan
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Wall check gizmos - 3 noktadan
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        float colliderHeight = col != null ? col.size.y * transform.localScale.y : 1f;
        float colliderWidth = col != null ? col.size.x * transform.localScale.x * 0.5f : 0.3f;

        float[] yOffsets = { colliderHeight * 0.3f, 0f, -colliderHeight * 0.3f };

        foreach (float yOffset in yOffsets)
        {
            Vector3 rightStart = transform.position + new Vector3(colliderWidth, yOffset, 0);
            Vector3 leftStart = transform.position + new Vector3(-colliderWidth, yOffset, 0);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(rightStart, rightStart + Vector3.right * wallCheckDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftStart, leftStart + Vector3.left * wallCheckDistance);
        }
    }

    // === ATES ETME ===
    void Fire()
    {
        // Animator trigger (artik kullanilmiyor - PlayAnim ile kontrol ediyoruz)
        // Ates animasyonu UpdateAnimation'da handle ediliyor

        // Ates sesi
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFire();
        }

        // Mermi olustur
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                // Karakter yonunu localScale'den al
                float direction = transform.localScale.x > 0 ? 1f : -1f;
                bulletRb.linearVelocity = new Vector2(direction * bulletSpeed, 0);

                // Mermi yonune gore sprite cevir
                if (direction < 0)
                {
                    bullet.transform.localScale = new Vector3(-1, 1, 1);
                }
            }

            // Mermi 3 saniye sonra yok olsun
            Destroy(bullet, 3f);
        }
        else
        {
            // BulletPrefab yoksa basit mermi olustur
            CreateSimpleBullet();
        }
    }

    void CreateSimpleBullet()
    {
        // Karakterin baktigi yone gore FirePoint pozisyonunu ayarla
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + new Vector3(direction * 0.5f, 0.1f, 0);

        GameObject bullet = new GameObject("Bullet");
        bullet.transform.position = spawnPos;
        bullet.layer = LayerMask.NameToLayer("Default");

        // Player ile carpismayi engelle
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider;

        // Sprite (cached - her mermi icin yeni texture olusturma)
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.8f, 0f); // Sari/turuncu mermi

        if (cachedBulletSprite == null)
        {
            Texture2D tex = new Texture2D(8, 4);
            Color[] colors = new Color[32];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            cachedBulletSprite = Sprite.Create(tex, new Rect(0, 0, 8, 4), new Vector2(0.5f, 0.5f), 16);
        }
        sr.sprite = cachedBulletSprite;
        sr.sortingOrder = 5;

        // Rigidbody
        Rigidbody2D bulletRb = bullet.AddComponent<Rigidbody2D>();
        bulletRb.gravityScale = 0;
        bulletRb.freezeRotation = true;
        bulletRb.linearVelocity = new Vector2(direction * bulletSpeed, 0);

        // Collider
        bulletCollider = bullet.AddComponent<BoxCollider2D>();
        ((BoxCollider2D)bulletCollider).size = new Vector2(0.5f, 0.25f);
        bulletCollider.isTrigger = true;

        // Player ile carpismayi engelle
        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(bulletCollider, playerCollider);
        }

        // Projectile script
        Projectile proj = bullet.AddComponent<Projectile>();
        proj.damage = (int)bulletDamage;
        proj.isPlayerBullet = true;
        // Tag kullanmiyoruz - isPlayerBullet yeterli

        Destroy(bullet, 3f);
    }

    // === TAKLA / ROLL ===
    void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;

        // Takla sirasinda hasar alma
        isInvincible = true;
        invincibleTimer = rollDuration;

        jumpRollAnimStarted = false;
        currentAnimState = "";

        // Roll efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayRollEffect(transform.position, rollDirection);
        }

        // Takla sesi
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayRoll();
        }
    }

    void EndRoll()
    {
        isRolling = false;
        rollCooldownTimer = rollCooldown;
        jumpRollAnimStarted = false; // Animasyon flag'ini sifirla

        if (!isPowerUpInvincible)
        {
            isInvincible = false;
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }
    }

    // === DASH ===
    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        // Havada dash yapiyorsak, hakki kullan
        if (!isGrounded)
        {
            hasAirDashed = true;
        }

        // Dash yonu - karakterin baktigi yon
        dashDirection = transform.localScale.x > 0 ? 1 : -1;

        // Dash sirasinda hasar alma
        isInvincible = true;
        invincibleTimer = dashDuration;

        // Y velocity'i sifirla (havada dash daha kontrollü olsun)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        // Dash efekti - neon izi
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayDashEffect(transform.position, dashDirection);
        }

        // Dash sesi (roll sesi kullanilabilir veya yeni ses)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayRoll();
        }

        // Screen shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnDash();
        }
    }

    void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;

        // Hizi azalt (ani durma)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);

        // Invincibility'i normal haline getir
        if (!isPowerUpInvincible)
        {
            isInvincible = false;
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    // Roll sirasinda hareket (FixedUpdate'ten cagrilacak)
    public bool IsRolling()
    {
        return isRolling;
    }

    public int GetRollDirection()
    {
        return rollDirection;
    }

    public float GetRollSpeed()
    {
        return rollSpeed;
    }

    // Wall Slide durumu
    public bool IsWallSliding()
    {
        return isWallSliding;
    }

    public bool IsWallJumping()
    {
        return isWallJumping;
    }

    // Coin ve diger trigger'lar icin
    void OnTriggerEnter2D(Collider2D other)
    {
        // Coin toplama - component ile kontrol et (tag'a gerek yok)
        Coin coin = other.GetComponent<Coin>();
        if (coin != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(coin.value);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin();
            }
            Destroy(other.gameObject);
            return;
        }

        // PowerUp - component ile kontrol et
        PowerUp powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            // PowerUp kendi OnTriggerEnter'inda handle edilir
            return;
        }
    }

    // === 8 YONLU NISAN SISTEMI ===

    /// <summary>
    /// Klavye girisine gore 8 yonlu nisan hesaplar
    /// Yonler: Sag, Sol, Yukari, Asagi, ve 4 capraz
    /// PC: WASD veya Ok tuslari
    /// Mobil: Joystick
    /// </summary>
    Vector2 CalculateAimDirection(Keyboard keyboard)
    {
        // Mobil mod aktifse joystick kullan
        if (UseMobileAim && MobileAimDirection.sqrMagnitude > 0.1f)
        {
            return MobileAimDirection.normalized;
        }

        if (keyboard == null)
            return GetDefaultAimDirection();

        // Yatay ve dikey girisleri al
        float aimX = 0f;
        float aimY = 0f;

        // Dikey yon ONCELIKLI (W/S veya Yukari/Asagi ok)
        // Capraz ates icin yatay da kontrol edilecek
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            aimY = 1f;
        else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            aimY = -1f;

        // Yatay yon - ATES SIRASINDA AYRI KONTROL
        // Eger ates tusuna basiliyorsa, yatay yonu da kontrol et
        bool isFiring = keyboard.jKey.isPressed ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);

        if (isFiring)
        {
            // Ates ederken yatay yonu kontrol et
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                aimX = 1f;
            else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                aimX = -1f;
        }
        else
        {
            // Ates etmiyorken hareket yonunu kullan
            aimX = horizontalInput;
        }

        // Eger sadece dikey giris varsa (yukari veya asagi ates)
        if (aimY != 0f && aimX == 0f)
        {
            // Ileri agirlikli capraz aci (daha yatay)
            float facingDir = transform.localScale.x > 0 ? 1f : -1f;
            return new Vector2(facingDir * 1f, aimY * 0.5f).normalized;
        }

        // Eger hicbir yon tusuna basilmiyorsa, karakterin baktigi yone ates et
        if (aimX == 0f && aimY == 0f)
        {
            return GetDefaultAimDirection();
        }

        // Yatay + dikey kombinasyonu (capraz)
        // Sadece yatay ise duz yatay
        Vector2 direction = new Vector2(aimX, aimY).normalized;

        // Capraz ates icin ileri agirlikli aci
        if (aimX != 0f && aimY != 0f)
        {
            // Ileri agirlikli capraz (yatay daha baskin)
            direction = new Vector2(aimX * 1f, aimY * 0.6f).normalized;
        }

        return direction;
    }

    /// <summary>
    /// Varsayilan nisan yonu - karakterin baktigi yon
    /// </summary>
    Vector2 GetDefaultAimDirection()
    {
        float facingDir = transform.localScale.x > 0 ? 1f : -1f;
        return new Vector2(facingDir, 0f);
    }

    /// <summary>
    /// Mobil icin nisan yonu ayarla (joystick'ten)
    /// </summary>
    public Vector2 MobileAimDirection { get; set; } = Vector2.zero;

    /// <summary>
    /// Mobil giris aktif mi?
    /// </summary>
    public bool UseMobileAim { get; set; } = false;

    /// <summary>
    /// Mobil için nişan yönünü hesapla
    /// </summary>
    Vector2 GetMobileAimDirection()
    {
        if (MobileAimDirection.sqrMagnitude > 0.1f)
            return MobileAimDirection.normalized;
        return GetDefaultAimDirection();
    }
}
