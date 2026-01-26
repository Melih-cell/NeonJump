using UnityEngine;

/// <summary>
/// Enemy hareket sistemi - Yumusak ve dogal hareket
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Hiz Ayarlari")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    [Header("Ivme Ayarlari")]
    public float acceleration = 8f;      // Hizlanma gucu
    public float deceleration = 10f;     // Yavasalama gucu
    public float turnSpeed = 12f;        // Donus hizi

    [Header("Zemin Kontrolu")]
    public float groundCheckDistance = 0.1f;
    public float groundDrag = 5f;        // Yerde surtunen
    public LayerMask groundLayer;

    [Header("Animasyon")]
    public float animSpeedMultiplier = 1f;

    // Public state
    public bool IsFacingRight { get; private set; } = true;
    public bool IsMoving => Mathf.Abs(currentVelocity) > 0.1f;
    public float CurrentSpeed => Mathf.Abs(currentVelocity);
    public float NormalizedSpeed => maxSpeed > 0 ? Mathf.Abs(currentVelocity) / maxSpeed : 0f;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // Movement state
    private float currentVelocity = 0f;
    private float targetVelocity = 0f;
    private float maxSpeed;
    private float moveInput = 0f;
    private bool isGrounded = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        maxSpeed = walkSpeed;

        // Ground layer
        if (groundLayer == 0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer != -1)
                groundLayer = 1 << layer;
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        ApplyMovement();
        UpdateAnimator();
    }

    void CheckGround()
    {
        if (rb == null) return;

        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.4f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    void ApplyMovement()
    {
        if (rb == null) return;

        // Hedef hiz
        targetVelocity = moveInput * maxSpeed;

        // Ivmelenme veya yavasalama
        float accel;
        if (Mathf.Abs(targetVelocity) > 0.1f)
        {
            // Hareket ediyoruz
            if (Mathf.Sign(targetVelocity) != Mathf.Sign(currentVelocity) && Mathf.Abs(currentVelocity) > 0.1f)
            {
                // Yon degisimi - once dur sonra don
                accel = turnSpeed;
            }
            else
            {
                accel = acceleration;
            }
        }
        else
        {
            // Duruyoruz
            accel = deceleration;
        }

        // Yumusak gecis
        currentVelocity = Mathf.MoveTowards(currentVelocity, targetVelocity, accel * Time.fixedDeltaTime);

        // Velocity uygula
        rb.linearVelocity = new Vector2(currentVelocity, rb.linearVelocity.y);

        // Sprite yonu
        if (Mathf.Abs(currentVelocity) > 0.1f)
        {
            bool shouldFaceRight = currentVelocity > 0;
            if (shouldFaceRight != IsFacingRight)
            {
                Flip(shouldFaceRight);
            }
        }
    }

    void Flip(bool faceRight)
    {
        IsFacingRight = faceRight;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !faceRight;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceRight ? 1f : -1f);
            transform.localScale = scale;
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = NormalizedSpeed * animSpeedMultiplier;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsMoving", IsMoving);
        animator.SetBool("IsGrounded", isGrounded);
    }

    #region Public Methods

    /// <summary>
    /// Yurume hareketi (-1 to 1)
    /// </summary>
    public void Walk(float direction)
    {
        maxSpeed = walkSpeed;
        moveInput = Mathf.Clamp(direction, -1f, 1f);
    }

    /// <summary>
    /// Kosma hareketi (-1 to 1)
    /// </summary>
    public void Run(float direction)
    {
        maxSpeed = runSpeed;
        moveInput = Mathf.Clamp(direction, -1f, 1f);
    }

    /// <summary>
    /// Hareketi durdur
    /// </summary>
    public void Stop()
    {
        moveInput = 0f;
    }

    /// <summary>
    /// Ani durus
    /// </summary>
    public void ForceStop()
    {
        moveInput = 0f;
        currentVelocity = 0f;
        targetVelocity = 0f;
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Yon degistir
    /// </summary>
    public void Turn()
    {
        Flip(!IsFacingRight);
    }

    /// <summary>
    /// Belirli bir hedefe dogru hareket
    /// </summary>
    public void MoveTowards(float targetX, bool run = false)
    {
        float diff = targetX - transform.position.x;
        float dir = Mathf.Sign(diff);

        if (run)
            Run(dir);
        else
            Walk(dir);
    }

    /// <summary>
    /// Hareket hizini ayarla
    /// </summary>
    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }

    public void SetRunSpeed(float speed)
    {
        runSpeed = speed;
    }

    /// <summary>
    /// Mevcut hareket inputu
    /// </summary>
    public float GetMoveInput()
    {
        return moveInput;
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.down * 0.4f;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);

        // Velocity
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * currentVelocity * 0.2f);

        // Facing direction
        Gizmos.color = Color.yellow;
        float facingDir = IsFacingRight ? 1f : -1f;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.3f,
                       transform.position + Vector3.up * 0.3f + Vector3.right * facingDir * 0.5f);
    }
}
