using UnityEngine;

public class JumpingEnemy : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpInterval = 2f;
    public float moveSpeed = 2f;

    [Header("Detection")]
    public float groundCheckRadius = 0.2f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Behavior")]
    public bool chasePlayer = false;
    public float chaseRange = 5f;

    [Header("Patrol (Optional)")]
    public bool patrolBetweenJumps = true;  // Ziplamalar arasi yurume
    public float patrolSpeed = 1.5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float jumpTimer;
    private bool isGrounded;
    private Transform player;
    private bool isDead = false;
    private bool isInitialized = false;

    // Patrol
    private bool movingRight = true;
    private float patrolCooldown = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        jumpTimer = jumpInterval;

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = gc.transform;
        }

        // Component kontrolu
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Ground layer otomatik ayarla
        if (groundLayer == 0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer >= 0)
                groundLayer = 1 << layer;
        }

        isInitialized = true;
        gameObject.tag = "Enemy";
    }

    void Update()
    {
        if (isDead || !isInitialized || rb == null) return;

        // Cooldown guncelle
        if (patrolCooldown > 0)
            patrolCooldown -= Time.deltaTime;

        // Yerde mi kontrolu - once layer, sonra tum collider'lara bak
        CheckGrounded();

        // Ziplama zamanlayicisi
        jumpTimer -= Time.deltaTime;

        if (jumpTimer <= 0 && isGrounded)
        {
            Jump();
            jumpTimer = jumpInterval;
            patrolCooldown = 0.3f; // Zipladiktan sonra kisa bekleme
        }

        // Ziplamalar arasi patrol
        if (patrolBetweenJumps && isGrounded && patrolCooldown <= 0)
        {
            Patrol();
        }
    }

    void CheckGrounded()
    {
        if (groundCheck == null) return;

        isGrounded = false;

        // Önce layer ile kontrol
        if (groundLayer != 0)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // Layer bulunamadiysa tum collider'lara bak
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

    void Patrol()
    {
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);

        // Sprite yonu
        if (spriteRenderer != null)
            spriteRenderer.flipX = !movingRight;

        // Duvar veya ucurum kontrolu
        Vector2 wallCheck = (Vector2)transform.position + new Vector2(direction * 0.5f, 0);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck, Vector2.right * direction, 0.3f);

        Vector2 groundCheckPos = (Vector2)transform.position + new Vector2(direction * 0.5f, -0.5f);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, 1f);

        bool shouldTurn = false;
        if (wallHit.collider != null && !wallHit.collider.isTrigger && wallHit.collider.gameObject != gameObject)
            shouldTurn = true;
        if (groundHit.collider == null)
            shouldTurn = true;

        if (shouldTurn)
            movingRight = !movingRight;
    }

    void Jump()
    {
        Vector2 jumpDirection = Vector2.up * jumpForce;

        // Oyuncuya dogru ziplama
        if (chasePlayer && player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer < chaseRange)
            {
                float dirX = Mathf.Sign(player.position.x - transform.position.x);
                jumpDirection += Vector2.right * dirX * moveSpeed;
                if (spriteRenderer != null)
                    spriteRenderer.flipX = dirX > 0;
            }
        }

        if (rb != null)
            rb.linearVelocity = jumpDirection;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Ezilme
        transform.localScale = new Vector3(transform.localScale.x, 0.3f, 1f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (chasePlayer)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }
}
