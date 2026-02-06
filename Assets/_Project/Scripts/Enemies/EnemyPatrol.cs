using UnityEngine;

/// <summary>
/// Patrol yapan dusman - EnemyBase'den turetilmis
/// EnemyHealth ve EnemyAI ile entegre calisir
/// </summary>
public class EnemyPatrol : EnemyBase
{
    [Header("Hareket Ayarlari")]
    public float speed = 3f;
    public float leftDistance = 3f;
    public float rightDistance = 3f;
    public bool startMovingRight = true;

    [Header("Donus Ayarlari")]
    public bool flipSprite = true;
    public bool useScaleFlip = true;
    public float waitTimeAtEdge = 0.5f; // Donus noktasinda bekleme suresi
    public float directionChangeCooldown = 0.3f; // Art arda donus engelleme

    [Header("Duvar/Ucurum Algilama")]
    public bool detectWalls = true;
    public bool detectCliffs = true;
    public float wallCheckDistance = 0.5f;
    public float cliffCheckDistance = 1.5f;
    public LayerMask groundLayer;

    [Header("AI Entegrasyonu")]
    public bool useAISystem = false; // EnemyAI varsa onu kullan

    private Vector3 startPosition;
    private bool movingRight;
    private bool isInitialized = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float cooldownTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        startPosition = transform.position;
        movingRight = startMovingRight;

        // Ground layer otomatik ayarla - birden fazla yontemle dene
        if (groundLayer == 0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer != -1)
            {
                groundLayer = 1 << layer;
            }
            else
            {
                // Platform veya Terrain layer'larini da dene
                int platformLayer = LayerMask.NameToLayer("Platform");
                if (platformLayer != -1)
                {
                    groundLayer = 1 << platformLayer;
                }
                else
                {
                    // Fallback: Default layer
                    groundLayer = 1 << LayerMask.NameToLayer("Default");
                }
            }
        }

        // EnemyAI varsa ve kullanilacaksa, patrol bounds'lari ayarla
        if (useAISystem && enemyAI != null)
        {
            enemyAI.SetPatrolBounds(startPosition.x - leftDistance, startPosition.x + rightDistance);
            // EnemyMovement varsa hiz ayarla
            EnemyMovement movement = GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.walkSpeed = speed;
            }
        }

        isInitialized = true;
    }

    void Update()
    {
        if (isDead || !isInitialized) return;

        // EnemyAI kullaniliyorsa, sadece onu birak
        if (useAISystem && enemyAI != null) return;

        // Cooldown timer guncelle
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Bekleme durumunu yonet
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
            }
            return;
        }

        CheckBounds();
        CheckObstacles();
    }

    void FixedUpdate()
    {
        if (isDead || !isInitialized) return;

        // EnemyAI kullaniliyorsa, o halleder
        if (useAISystem && enemyAI != null) return;

        // Bekliyorsa hareketi durdur
        if (isWaiting)
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            return;
        }

        Move();
    }

    void Move()
    {
        float direction = movingRight ? 1f : -1f;

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += Vector3.right * direction * speed * Time.fixedDeltaTime;
        }
    }

    void CheckBounds()
    {
        if (cooldownTimer > 0f) return;

        if (movingRight && transform.position.x >= startPosition.x + rightDistance)
        {
            // Sinir disina tasmamasi icin pozisyonu sinirla
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                transform.position = new Vector3(startPosition.x + rightDistance, transform.position.y, transform.position.z);
            }
            Turn();
        }
        else if (!movingRight && transform.position.x <= startPosition.x - leftDistance)
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                transform.position = new Vector3(startPosition.x - leftDistance, transform.position.y, transform.position.z);
            }
            Turn();
        }
    }

    void CheckObstacles()
    {
        if (cooldownTimer > 0f) return;

        float direction = movingRight ? 1f : -1f;

        // Duvar kontrolu
        if (detectWalls)
        {
            Vector2 wallCheckOrigin = (Vector2)transform.position + Vector2.up * 0.2f;
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

            // groundLayer ile bulamadiysa tum layer'lara bak
            if (wallHit.collider == null)
            {
                wallHit = Physics2D.Raycast(wallCheckOrigin, Vector2.right * direction, wallCheckDistance);
                // Kendini, trigger'lari ve diger dusmanlari filtrele
                if (wallHit.collider != null &&
                    (wallHit.collider.gameObject == gameObject ||
                     wallHit.collider.isTrigger ||
                     wallHit.collider.CompareTag("Enemy") ||
                     wallHit.collider.GetComponent<EnemyBase>() != null))
                {
                    wallHit = new RaycastHit2D();
                }
            }
            else
            {
                // groundLayer'da bulduysa da dusman kontrolu yap
                if (wallHit.collider.CompareTag("Enemy") || wallHit.collider.GetComponent<EnemyBase>() != null)
                {
                    wallHit = new RaycastHit2D();
                }
            }

            if (wallHit.collider != null)
            {
                Turn();
                return;
            }
        }

        // Ucurum kontrolu
        if (detectCliffs)
        {
            Vector2 cliffCheckOrigin = (Vector2)transform.position + new Vector2(direction * 0.5f, -0.3f);
            RaycastHit2D groundHit = Physics2D.Raycast(cliffCheckOrigin, Vector2.down, cliffCheckDistance, groundLayer);

            if (groundHit.collider == null)
            {
                // Fallback: tum layer'lara bak
                groundHit = Physics2D.Raycast(cliffCheckOrigin, Vector2.down, cliffCheckDistance);
                if (groundHit.collider != null &&
                    (groundHit.collider.gameObject == gameObject ||
                     groundHit.collider.isTrigger ||
                     groundHit.collider.CompareTag("Enemy")))
                {
                    groundHit = new RaycastHit2D();
                }
            }

            if (groundHit.collider == null)
            {
                Turn();
                return;
            }
        }
    }

    void Turn()
    {
        movingRight = !movingRight;
        cooldownTimer = directionChangeCooldown;

        // Donus noktasinda bekle
        if (waitTimeAtEdge > 0f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtEdge;
        }

        if (flipSprite)
        {
            if (useScaleFlip)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (movingRight ? 1f : -1f);
                transform.localScale = scale;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !movingRight;
            }
        }
    }

    // Hareket yonunu degistir (dis erisim icin)
    public void SetDirection(bool right)
    {
        if (movingRight != right)
        {
            Turn();
        }
    }

    // Patrol sinirlarini guncelle
    public void SetPatrolBounds(float left, float right)
    {
        leftDistance = left;
        rightDistance = right;
    }

    // Mevcut hareket yonunu al
    public bool IsMovingRight()
    {
        return movingRight;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        float direction = movingRight ? 1f : -1f;

        // Patrol siniri
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos + Vector3.left * leftDistance, pos + Vector3.right * rightDistance);
        Gizmos.DrawWireSphere(pos + Vector3.left * leftDistance, 0.2f);
        Gizmos.DrawWireSphere(pos + Vector3.right * rightDistance, 0.2f);

        // Duvar check
        if (detectWalls)
        {
            Gizmos.color = Color.red;
            Vector3 wallCheckOrigin = transform.position + Vector3.up * 0.2f;
            Gizmos.DrawLine(wallCheckOrigin, wallCheckOrigin + Vector3.right * direction * wallCheckDistance);
        }

        // Ucurum check
        if (detectCliffs)
        {
            Gizmos.color = Color.blue;
            Vector3 cliffCheckOrigin = transform.position + Vector3.right * direction * 0.5f + Vector3.down * 0.3f;
            Gizmos.DrawLine(cliffCheckOrigin, cliffCheckOrigin + Vector3.down * cliffCheckDistance);
        }
    }
}
