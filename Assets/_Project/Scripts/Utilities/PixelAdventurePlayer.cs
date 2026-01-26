using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pixel Adventure asset paketi ile tam uyumlu oyuncu kontrolcusu.
/// Gelismis animasyon sistemi ve efektler icin.
/// </summary>
public class PixelAdventurePlayer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 14f;
    public float bounceForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Coyote Time & Jump Buffer")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Invincibility")]
    public float invincibleDuration = 2f;

    [Header("Sprite Settings")]
    public string characterType = "MaskDude"; // MaskDude, NinjaFrog, PinkMan, VirtualGuy
    public float animationFPS = 20f;

    [Header("Dust Effects")]
    public bool enableDustEffects = true;
    public Color dustColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    // Animation
    private Sprite[] currentAnimation;
    private Sprite[] idleSprites;
    private Sprite[] runSprites;
    private Sprite[] jumpSprites;
    private Sprite[] fallSprites;
    private Sprite[] doubleJumpSprites;
    private Sprite[] hitSprites;

    private int currentFrame;
    private float animTimer;
    private float frameTime;

    // State
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isInvincible;
    private float invincibleTimer;
    private bool isPowerUpInvincible;
    private bool canDoubleJump;
    private bool hasDoubleJumped;

    // Animation State
    private enum AnimState { Idle, Run, Jump, Fall, DoubleJump, Hit }
    private AnimState currentAnimState = AnimState.Idle;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();

        // Rigidbody ayarlari
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Collider ayarlari
        boxCollider.size = new Vector2(0.8f, 0.9f);

        // Ground check olustur
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = gc.transform;
        }

        // Sprite'lari yukle
        LoadSprites();

        // Animasyon ayarlari
        frameTime = 1f / animationFPS;
        spriteRenderer.sortingOrder = 10;
    }

    void LoadSprites()
    {
        string basePath = "PixelAdventure/MainCharacters/" + characterType;

        // Resources'tan yukle
        idleSprites = Resources.LoadAll<Sprite>(basePath + "/Idle");
        runSprites = Resources.LoadAll<Sprite>(basePath + "/Run");
        jumpSprites = Resources.LoadAll<Sprite>(basePath + "/Jump");
        fallSprites = Resources.LoadAll<Sprite>(basePath + "/Fall");
        doubleJumpSprites = Resources.LoadAll<Sprite>(basePath + "/Double Jump");
        hitSprites = Resources.LoadAll<Sprite>(basePath + "/Hit");

        // Bulunamazsa varsayilan sprite olustur
        if (idleSprites == null || idleSprites.Length == 0)
        {
            Debug.Log("Pixel Adventure sprites bulunamadi. Varsayilan kullaniliyor.");
            Debug.Log("Sprite'lari su klasore koy: Assets/Resources/" + basePath);
            CreateDefaultSprites();
        }
        else
        {
            Debug.Log($"Pixel Adventure sprites yuklendi: {characterType}");
        }

        currentAnimation = idleSprites;
    }

    void CreateDefaultSprites()
    {
        // Varsayilan neon karakter sprite'lari
        idleSprites = new Sprite[] { CreateCharacterSprite(0) };
        runSprites = new Sprite[] {
            CreateCharacterSprite(0),
            CreateCharacterSprite(1),
            CreateCharacterSprite(2),
            CreateCharacterSprite(3)
        };
        jumpSprites = new Sprite[] { CreateCharacterSprite(10) };
        fallSprites = new Sprite[] { CreateCharacterSprite(11) };
        doubleJumpSprites = new Sprite[] { CreateCharacterSprite(10), CreateCharacterSprite(12) };
        hitSprites = new Sprite[] { CreateCharacterSprite(20) };
    }

    Sprite CreateCharacterSprite(int type)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Neon cyan karakter
        Color body = new Color(0f, 0.9f, 1f);
        Color outline = new Color(0f, 0.5f, 0.7f);
        Color eye = Color.white;

        int bodyY = (type == 10) ? 10 : (type == 11 ? 6 : 8);

        // Govde
        for (int x = 10; x < 22; x++)
        {
            for (int y = bodyY; y < bodyY + 14; y++)
            {
                if (y < size && y >= 0)
                {
                    if (x == 10 || x == 21 || y == bodyY || y == bodyY + 13)
                        pixels[y * size + x] = outline;
                    else
                        pixels[y * size + x] = body;
                }
            }
        }

        // Gozler
        int eyeY = bodyY + 10;
        if (eyeY < size)
        {
            pixels[eyeY * size + 13] = eye;
            pixels[eyeY * size + 14] = eye;
            pixels[eyeY * size + 18] = eye;
            pixels[eyeY * size + 19] = eye;
        }

        // Bacaklar (kosarken hareket)
        int legOffset = (type % 4) * 2 - 2;
        for (int y = Mathf.Max(0, bodyY - 4); y < bodyY; y++)
        {
            int leftX = 12 + (type < 4 ? legOffset : 0);
            int rightX = 18 + (type < 4 ? -legOffset : 0);

            if (leftX >= 0 && leftX < size - 2)
            {
                pixels[y * size + leftX] = outline;
                pixels[y * size + leftX + 1] = body;
            }
            if (rightX >= 0 && rightX < size - 2)
            {
                pixels[y * size + rightX] = outline;
                pixels[y * size + rightX + 1] = body;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        HandleInvincibility();
        HandleInput();
        HandleJump();
        UpdateAnimation();
    }

    void HandleInvincibility()
    {
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
    }

    void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        horizontalInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontalInput = -1f;
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontalInput = 1f;

        // Sprite yonu
        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;
    }

    void HandleJump()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasDoubleJumped = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump buffer
        bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame ||
                          keyboard.wKey.wasPressedThisFrame ||
                          keyboard.upArrowKey.wasPressedThisFrame;

        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Normal ziplama
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            SpawnDustEffect();
            PlayJumpSound();
        }
        // Double jump
        else if (jumpPressed && !isGrounded && !hasDoubleJumped && CanDoubleJump())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.85f);
            hasDoubleJumped = true;
            currentAnimState = AnimState.DoubleJump;
            currentFrame = 0;
            PlayJumpSound();
        }
    }

    bool CanDoubleJump()
    {
        if (PowerUpManager.Instance != null)
            return PowerUpManager.Instance.TryDoubleJump();
        return canDoubleJump;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        wasGrounded = isGrounded;
        CheckGround();

        // Yere inis efekti
        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0)
        {
            SpawnDustEffect();
        }

        // Hareket
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    void CheckGround()
    {
        if (groundCheck == null) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (!isGrounded)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
            foreach (Collider2D col in colliders)
            {
                if (col.gameObject != gameObject && !col.CompareTag("Enemy") && !col.isTrigger)
                {
                    isGrounded = true;
                    break;
                }
            }
        }
    }

    void UpdateAnimation()
    {
        // Animasyon durumunu belirle
        AnimState newState = currentAnimState;

        if (currentAnimState != AnimState.DoubleJump || currentFrame >= doubleJumpSprites.Length - 1)
        {
            if (!isGrounded)
            {
                if (rb.linearVelocity.y > 0.5f)
                    newState = AnimState.Jump;
                else
                    newState = AnimState.Fall;
            }
            else
            {
                if (Mathf.Abs(horizontalInput) > 0.1f)
                    newState = AnimState.Run;
                else
                    newState = AnimState.Idle;
            }
        }

        // Durum degisti mi?
        if (newState != currentAnimState)
        {
            currentAnimState = newState;
            currentFrame = 0;
            animTimer = 0;

            switch (currentAnimState)
            {
                case AnimState.Idle: currentAnimation = idleSprites; break;
                case AnimState.Run: currentAnimation = runSprites; break;
                case AnimState.Jump: currentAnimation = jumpSprites; break;
                case AnimState.Fall: currentAnimation = fallSprites; break;
                case AnimState.DoubleJump: currentAnimation = doubleJumpSprites; break;
                case AnimState.Hit: currentAnimation = hitSprites; break;
            }
        }

        // Animasyonu guncelle
        if (currentAnimation != null && currentAnimation.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= frameTime)
            {
                animTimer = 0;
                currentFrame = (currentFrame + 1) % currentAnimation.Length;
            }

            spriteRenderer.sprite = currentAnimation[currentFrame];
        }
    }

    void SpawnDustEffect()
    {
        if (!enableDustEffects || groundCheck == null) return;

        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayJumpDust(groundCheck.position);
        }
    }

    void PlayJumpSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJump();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            HandleEnemyCollision(collision);
        }
    }

    void HandleEnemyCollision(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        PixelAdventureEnemy paEnemy = collision.gameObject.GetComponent<PixelAdventureEnemy>();

        float playerBottom = transform.position.y - 0.5f;
        float enemyTop = collision.transform.position.y + 0.3f;

        // Ustten carpma
        if (playerBottom > enemyTop && rb.linearVelocity.y < 0)
        {
            Vector3 enemyPos = collision.transform.position;

            if (enemy != null) enemy.Die();
            if (paEnemy != null) paEnemy.Die();

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);

            if (GameManager.Instance != null)
                GameManager.Instance.EnemyKilled(enemyPos);
        }
        else if (!isInvincible && !isPowerUpInvincible)
        {
            TakeDamage();
        }
    }

    public void TakeDamage()
    {
        if (isInvincible || isPowerUpInvincible) return;

        // Kalkan kontrolu
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.TryAbsorbDamage())
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHurt();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(1);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHurt();

            if (!GameManager.Instance.IsGameOver())
            {
                isInvincible = true;
                invincibleTimer = invincibleDuration;
                rb.linearVelocity = new Vector2(-horizontalInput * 5f, 8f);

                // Hit animasyonu
                currentAnimState = AnimState.Hit;
                currentFrame = 0;
            }
        }
    }

    public void SetInvincible(bool value)
    {
        isPowerUpInvincible = value;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = value ? new Color(1f, 1f, 0.5f) : Color.white;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
