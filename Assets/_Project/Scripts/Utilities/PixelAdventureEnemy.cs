using UnityEngine;

/// <summary>
/// Pixel Adventure asset paketi ile uyumlu dusman sistemi.
/// Farkli dusman tipleri: Slime, Mushroom, Ghost, Bat, vb.
/// </summary>
public class PixelAdventureEnemy : MonoBehaviour
{
    public enum EnemyType
    {
        Slime,      // Yerde yurur, ziplayan
        Mushroom,   // Yerde yurur, hizli
        Ghost,      // Havada suzulur
        Bat,        // Havada ucar, saldirir
        Skull,      // Takip eder
        Trunk,      // Uzaktan saldiri
        Plant,      // Sabit, isirici
        Radish,     // Yerden cikar
        Snail       // Yavag, kabuklu
    }

    [Header("Enemy Settings")]
    public EnemyType enemyType = EnemyType.Slime;
    public float moveSpeed = 2f;
    public float detectionRange = 8f;
    public bool startMovingRight = false;

    [Header("Animation Settings")]
    public float animationFPS = 10f;

    [Header("Behavior")]
    public float patrolDistance = 5f;
    public float jumpForce = 8f;
    public float jumpInterval = 2f;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    // Animation
    private Sprite[] idleSprites;
    private Sprite[] runSprites;
    private Sprite[] hitSprites;
    private Sprite[] currentAnimation;
    private int currentFrame;
    private float animTimer;
    private float frameTime;

    // State
    private bool moveRight;
    private bool isDead;
    private Vector3 startPosition;
    private float jumpTimer;
    private Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();

        // Ayarlar
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        boxCollider.size = new Vector2(0.9f, 0.9f);
        spriteRenderer.sortingOrder = 5;
        gameObject.tag = "Enemy";

        // Dusman tipine gore ayarlar
        SetupEnemyType();

        moveRight = startMovingRight;
        startPosition = transform.position;
        frameTime = 1f / animationFPS;

        // Sprite yukle
        LoadSprites();

        // Player'i bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void SetupEnemyType()
    {
        switch (enemyType)
        {
            case EnemyType.Slime:
                rb.gravityScale = 3f;
                moveSpeed = 1.5f;
                break;

            case EnemyType.Mushroom:
                rb.gravityScale = 3f;
                moveSpeed = 3f;
                break;

            case EnemyType.Ghost:
                rb.gravityScale = 0f;
                moveSpeed = 2f;
                break;

            case EnemyType.Bat:
                rb.gravityScale = 0f;
                moveSpeed = 3f;
                break;

            case EnemyType.Skull:
                rb.gravityScale = 0f;
                moveSpeed = 4f;
                break;

            case EnemyType.Trunk:
                rb.gravityScale = 3f;
                moveSpeed = 0f; // Sabit
                break;

            case EnemyType.Plant:
                rb.gravityScale = 3f;
                moveSpeed = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                break;

            default:
                rb.gravityScale = 3f;
                break;
        }
    }

    void LoadSprites()
    {
        string basePath = "PixelAdventure/Enemies/" + enemyType.ToString();

        idleSprites = Resources.LoadAll<Sprite>(basePath + "/Idle");
        runSprites = Resources.LoadAll<Sprite>(basePath + "/Run");
        hitSprites = Resources.LoadAll<Sprite>(basePath + "/Hit");

        // Bulunamazsa varsayilan
        if (idleSprites == null || idleSprites.Length == 0)
        {
            CreateDefaultSprite();
        }

        currentAnimation = idleSprites != null && idleSprites.Length > 0 ? idleSprites : runSprites;
    }

    void CreateDefaultSprite()
    {
        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[256];
        Color enemyColor = GetEnemyColor();

        // Basit dusman sprite
        for (int y = 2; y < 14; y++)
        {
            for (int x = 2; x < 14; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
                if (dist < 6)
                    pixels[y * 16 + x] = enemyColor;
            }
        }

        // Gozler
        pixels[10 * 16 + 5] = Color.white;
        pixels[10 * 16 + 6] = Color.white;
        pixels[10 * 16 + 10] = Color.white;
        pixels[10 * 16 + 11] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        idleSprites = new Sprite[] { sprite };
        runSprites = idleSprites;
        hitSprites = idleSprites;
    }

    Color GetEnemyColor()
    {
        switch (enemyType)
        {
            case EnemyType.Slime: return new Color(0.2f, 0.8f, 0.2f);
            case EnemyType.Mushroom: return new Color(0.8f, 0.2f, 0.2f);
            case EnemyType.Ghost: return new Color(0.7f, 0.7f, 0.9f);
            case EnemyType.Bat: return new Color(0.3f, 0.2f, 0.4f);
            case EnemyType.Skull: return new Color(0.9f, 0.9f, 0.8f);
            case EnemyType.Trunk: return new Color(0.5f, 0.3f, 0.2f);
            case EnemyType.Plant: return new Color(0.3f, 0.6f, 0.2f);
            default: return Color.red;
        }
    }

    void Update()
    {
        if (isDead) return;

        UpdateBehavior();
        UpdateAnimation();
    }

    void UpdateBehavior()
    {
        switch (enemyType)
        {
            case EnemyType.Slime:
                SlimeBehavior();
                break;

            case EnemyType.Mushroom:
                MushroomBehavior();
                break;

            case EnemyType.Ghost:
                GhostBehavior();
                break;

            case EnemyType.Bat:
                BatBehavior();
                break;

            case EnemyType.Skull:
                SkullBehavior();
                break;

            case EnemyType.Trunk:
                TrunkBehavior();
                break;

            case EnemyType.Plant:
                PlantBehavior();
                break;

            default:
                PatrolBehavior();
                break;
        }
    }

    void SlimeBehavior()
    {
        // Ziplayan patrol
        PatrolBehavior();

        jumpTimer -= Time.deltaTime;
        if (jumpTimer <= 0 && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpTimer = jumpInterval;
        }
    }

    void MushroomBehavior()
    {
        // Hizli patrol, player gorurse kostur
        if (player != null && Vector2.Distance(transform.position, player.position) < detectionRange)
        {
            // Player'a dogru kos
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * moveSpeed * 1.5f, rb.linearVelocity.y);
            spriteRenderer.flipX = direction.x < 0;
        }
        else
        {
            PatrolBehavior();
        }
    }

    void GhostBehavior()
    {
        // Yukari asagi hareket
        float newY = startPosition.y + Mathf.Sin(Time.time * 2f) * 1.5f;
        float newX = startPosition.x + Mathf.Sin(Time.time * 0.5f) * patrolDistance;

        transform.position = Vector3.Lerp(transform.position, new Vector3(newX, newY, 0), Time.deltaTime * 2f);

        // Player'a bak
        if (player != null)
        {
            spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    void BatBehavior()
    {
        if (player != null && Vector2.Distance(transform.position, player.position) < detectionRange)
        {
            // Player'a dogru uc
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, direction * moveSpeed, Time.deltaTime * 3f);
            spriteRenderer.flipX = direction.x < 0;
        }
        else
        {
            // Dairesel hareket
            float angle = Time.time * 2f;
            float x = startPosition.x + Mathf.Cos(angle) * patrolDistance;
            float y = startPosition.y + Mathf.Sin(angle) * patrolDistance * 0.5f;
            transform.position = Vector3.Lerp(transform.position, new Vector3(x, y, 0), Time.deltaTime * 2f);
        }
    }

    void SkullBehavior()
    {
        if (player != null)
        {
            // Surekli takip
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    void TrunkBehavior()
    {
        // Sabit dur, player'a bak
        if (player != null)
        {
            spriteRenderer.flipX = player.position.x < transform.position.x;

            // TODO: Mermi at
        }
    }

    void PlantBehavior()
    {
        // Sabit, sadece animasyon
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            // Yakinsa saldiri animasyonu
        }
    }

    void PatrolBehavior()
    {
        // Patrol hareketi
        float direction = moveRight ? 1f : -1f;

        if (rb.gravityScale > 0)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, 0);
        }

        spriteRenderer.flipX = !moveRight;

        // Patrol mesafesini kontrol et
        float distFromStart = transform.position.x - startPosition.x;
        if (Mathf.Abs(distFromStart) > patrolDistance)
        {
            moveRight = distFromStart < 0;
        }

        // Duvar kontrolu
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(direction * 0.5f, 0);
        RaycastHit2D wallHit = Physics2D.Raycast(rayOrigin, Vector2.right * direction, 0.3f);
        if (wallHit.collider != null && !wallHit.collider.isTrigger && wallHit.collider.gameObject != gameObject)
        {
            moveRight = !moveRight;
        }

        // Ucurum kontrolu
        if (rb.gravityScale > 0)
        {
            Vector2 groundCheckPos = (Vector2)transform.position + new Vector2(direction * 0.5f, -0.6f);
            RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, 0.5f);
            if (groundHit.collider == null)
            {
                moveRight = !moveRight;
            }
        }
    }

    bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    void UpdateAnimation()
    {
        if (currentAnimation == null || currentAnimation.Length == 0) return;

        // Hareket ediyorsa run, degilse idle
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f || Mathf.Abs(rb.linearVelocity.y) > 0.1f;

        if (isMoving && runSprites != null && runSprites.Length > 0)
            currentAnimation = runSprites;
        else if (idleSprites != null && idleSprites.Length > 0)
            currentAnimation = idleSprites;

        animTimer += Time.deltaTime;
        if (animTimer >= frameTime)
        {
            animTimer = 0;
            currentFrame = (currentFrame + 1) % currentAnimation.Length;
        }

        if (currentAnimation.Length > 0)
            spriteRenderer.sprite = currentAnimation[currentFrame];
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Hit animasyonu
        if (hitSprites != null && hitSprites.Length > 0)
        {
            spriteRenderer.sprite = hitSprites[0];
        }

        // Ezilme efekti
        transform.localScale = new Vector3(transform.localScale.x, 0.3f, 1);

        // Collider kapat
        if (boxCollider != null)
            boxCollider.enabled = false;

        // Hareketi durdur
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Yok et
        Destroy(gameObject, 0.3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawLine(start + Vector3.left * patrolDistance, start + Vector3.right * patrolDistance);
    }
}
