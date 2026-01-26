using UnityEngine;
using System;

/// <summary>
/// Gelismis dusman AI sistemi - State Machine tabanli
/// EnemyMovement ile entegre, akici hareket
/// </summary>
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Idle, Patrol, Alert, Chase, Attack, Hurt, Dead }

    [Header("Mevcut Durum")]
    [SerializeField] private AIState currentState = AIState.Idle;
    public AIState CurrentState => currentState;

    [Header("Algilama Ayarlari")]
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float loseTargetRange = 12f;
    public bool needsLineOfSight = true;
    public LayerMask obstacleLayer;
    public float fieldOfView = 180f;

    [Header("Patrol Ayarlari")]
    public float patrolDistance = 5f;
    public float patrolWaitMin = 1f;
    public float patrolWaitMax = 3f;
    [Range(0f, 1f)]
    public float patrolWaitChance = 0.5f;

    [Header("Engel Algilama")]
    public bool detectWalls = true;
    public bool detectCliffs = false; // Varsayilan kapali - patrol sinirlarini kullan
    public float wallCheckDistance = 0.5f;
    public float cliffCheckDistance = 1f;
    public LayerMask groundLayer;

    [Header("Saldiri Ayarlari")]
    public float attackCooldown = 1.5f;
    public float attackDuration = 0.5f;
    public float attackWindup = 0.2f; // Saldiri oncesi bekleme
    public int attackDamage = 1;
    public float attackKnockback = 5f;

    [Header("Davranis Ayarlari")]
    public bool canPatrol = true;
    public bool canChase = true;
    public bool canAttack = true;
    public bool returnToStart = true;
    public float maxChaseDistance = 15f;
    public float alertDuration = 0.5f; // Oyuncuyu gordugunde sasirma

    [Header("Tepki Sureleri")]
    public float reactionTime = 0.1f; // Karar verme gecikmesi
    public float directionChangeDelay = 0.2f; // Yon degistirme gecikmesi

    [Header("Gorsel Geri Bildirim")]
    public bool showAlertIndicator = true;
    public GameObject alertIndicatorPrefab;
    public Vector3 alertIndicatorOffset = new Vector3(0, 1f, 0);

    // Events
    public event Action<AIState, AIState> OnStateChanged;
    public event Action OnPlayerDetected;
    public event Action OnPlayerLost;
    public event Action OnAttackStart;
    public event Action OnAttackHit;

    // Components
    private EnemyMovement movement;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth enemyHealth;
    private Animator animator;
    private GameObject alertIndicatorInstance;

    // State
    private Vector3 startPosition;
    private Transform player;
    private float stateTimer;
    private float attackTimer;
    private float reactionTimer;
    private float lastKnownPlayerX;
    private bool playerDetected;
    private bool hasAttacked;

    // Patrol
    private float patrolLeftBound;
    private float patrolRightBound;
    private int patrolDirection = 1;
    private bool waitingAtPatrolPoint;
    private float patrolWaitTimer;
    private float obstacleCheckCooldown = 0f;

    // Chase
    private float lastDirectionChangeTime;
    private int lastChaseDirection;

    void Start()
    {
        startPosition = transform.position;
        patrolLeftBound = startPosition.x - patrolDistance;
        patrolRightBound = startPosition.x + patrolDistance;

        // Components
        movement = GetComponent<EnemyMovement>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        enemyHealth = GetComponent<EnemyHealth>();

        // Health events
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged += OnDamaged;
            enemyHealth.OnDeath += OnDeath;
        }

        // Ground layer otomatik ayarla
        if (groundLayer == 0)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer != -1)
                groundLayer = 1 << layer;
            else
                groundLayer = 1 << LayerMask.NameToLayer("Default");
        }

        // Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Alert indicator
        CreateAlertIndicator();

        // Baslangic state
        ChangeState(canPatrol ? AIState.Patrol : AIState.Idle);
    }

    void Update()
    {
        if (currentState == AIState.Dead) return;

        // Timers
        stateTimer += Time.deltaTime;
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (reactionTimer > 0) reactionTimer -= Time.deltaTime;

        // Player detection
        UpdatePlayerDetection();

        // State machine
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Alert:
                UpdateAlert();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Hurt:
                UpdateHurt();
                break;
        }

        UpdateAnimator();
    }

    #region Detection

    void UpdatePlayerDetection()
    {
        if (player == null) return;

        bool wasDetected = playerDetected;
        bool inRange = IsPlayerInRange(detectionRange);
        bool canSee = !needsLineOfSight || HasLineOfSight();
        bool inFOV = IsPlayerInFOV();

        playerDetected = inRange && canSee && inFOV;

        // Yeni algilama
        if (playerDetected && !wasDetected)
        {
            lastKnownPlayerX = player.position.x;
            OnPlayerDetected?.Invoke();
            ShowAlertIndicator();

            if (canChase && currentState != AIState.Hurt && currentState != AIState.Attack)
            {
                // Alert state'e gec (sasirma)
                ChangeState(AIState.Alert);
            }
        }
        // Kaybetme
        else if (!playerDetected && wasDetected)
        {
            if (!IsPlayerInRange(loseTargetRange))
            {
                OnPlayerLost?.Invoke();
                HideAlertIndicator();

                if (currentState == AIState.Chase || currentState == AIState.Alert)
                {
                    ChangeState(returnToStart ? AIState.Patrol : AIState.Idle);
                }
            }
        }

        // Surekli takip icin pozisyon guncelle
        if (playerDetected)
        {
            lastKnownPlayerX = player.position.x;
        }
    }

    bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        return Vector2.Distance(transform.position, player.position) <= range;
    }

    bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
        return hit.collider == null;
    }

    bool IsPlayerInFOV()
    {
        if (player == null || fieldOfView >= 360f) return true;

        Vector2 toPlayer = (player.position - transform.position).normalized;
        Vector2 facing = movement != null ?
            (movement.IsFacingRight ? Vector2.right : Vector2.left) :
            (spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right);

        float angle = Vector2.Angle(facing, toPlayer);
        return angle <= fieldOfView / 2f;
    }

    #endregion

    #region State Updates

    void UpdateIdle()
    {
        if (movement != null)
            movement.Stop();

        // Bir sure sonra patrol'e gec
        if (canPatrol && stateTimer >= 2f)
        {
            ChangeState(AIState.Patrol);
        }
    }

    void UpdatePatrol()
    {
        if (movement == null) return;

        // Bekleme durumu
        if (waitingAtPatrolPoint)
        {
            movement.Stop();
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                waitingAtPatrolPoint = false;
                patrolDirection *= -1; // Yon degistir
                obstacleCheckCooldown = 0.3f; // Yon degistikten sonra kisa sure engel kontrolu yapma
            }
            return;
        }

        // Cooldown guncelle
        if (obstacleCheckCooldown > 0)
        {
            obstacleCheckCooldown -= Time.deltaTime;
        }

        // Engel kontrolu (cooldown yoksa)
        if (obstacleCheckCooldown <= 0 && CheckForObstacles())
        {
            // Engel var, yon degistir
            patrolDirection *= -1;
            obstacleCheckCooldown = 0.3f; // Tekrar kontrol etmeden once bekle
        }

        // Hareket - her zaman yuru
        movement.Walk(patrolDirection);

        // Sinir kontrolu
        bool atLeftBound = transform.position.x <= patrolLeftBound;
        bool atRightBound = transform.position.x >= patrolRightBound;

        if ((atLeftBound && patrolDirection < 0) || (atRightBound && patrolDirection > 0))
        {
            // Sinira ulasti
            movement.Stop();

            // Bekleme sansi
            if (UnityEngine.Random.value < patrolWaitChance)
            {
                waitingAtPatrolPoint = true;
                patrolWaitTimer = UnityEngine.Random.Range(patrolWaitMin, patrolWaitMax);
            }
            else
            {
                patrolDirection *= -1;
                obstacleCheckCooldown = 0.3f;
            }
        }
    }

    /// <summary>
    /// Duvar ve ucurum kontrolu
    /// </summary>
    bool CheckForObstacles()
    {
        float direction = patrolDirection;

        // Duvar kontrolu
        if (detectWalls)
        {
            Vector2 wallCheckOrigin = (Vector2)transform.position + Vector2.up * 0.2f;
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

            if (wallHit.collider != null)
            {
                // Baska dusman mi kontrol et
                if (!wallHit.collider.CompareTag("Enemy") && wallHit.collider.GetComponent<EnemyBase>() == null)
                {
                    return true; // Gercek duvar
                }
            }
        }

        // Ucurum kontrolu
        if (detectCliffs)
        {
            Vector2 cliffCheckOrigin = (Vector2)transform.position + new Vector2(direction * 0.5f, -0.3f);
            RaycastHit2D groundHit = Physics2D.Raycast(cliffCheckOrigin, Vector2.down, cliffCheckDistance, groundLayer);

            if (groundHit.collider == null)
            {
                // Zemin yok, ucurum var
                return true;
            }
        }

        return false;
    }

    void UpdateAlert()
    {
        // Dur ve sasir
        if (movement != null)
            movement.ForceStop();

        // Alert suresi doldu, kovalamaya basla
        if (stateTimer >= alertDuration)
        {
            if (canChase && playerDetected)
            {
                ChangeState(AIState.Chase);
            }
            else
            {
                ChangeState(AIState.Patrol);
            }
        }
    }

    void UpdateChase()
    {
        if (movement == null || player == null)
        {
            ChangeState(AIState.Patrol);
            return;
        }

        // Max mesafe kontrolu
        float distFromStart = Vector2.Distance(transform.position, startPosition);
        if (distFromStart > maxChaseDistance)
        {
            playerDetected = false;
            HideAlertIndicator();
            ChangeState(AIState.Patrol);
            return;
        }

        // Oyuncuya dogru kos
        float dirToPlayer = Mathf.Sign(lastKnownPlayerX - transform.position.x);

        // Yon degistirme gecikmesi (daha dogal gorunum)
        if ((int)dirToPlayer != lastChaseDirection)
        {
            if (Time.time - lastDirectionChangeTime >= directionChangeDelay)
            {
                lastChaseDirection = (int)dirToPlayer;
                lastDirectionChangeTime = Time.time;
            }
            else
            {
                dirToPlayer = lastChaseDirection;
            }
        }

        movement.Run(dirToPlayer);

        // Saldiri mesafesi kontrolu
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        if (canAttack && distToPlayer <= attackRange && attackTimer <= 0)
        {
            ChangeState(AIState.Attack);
        }
    }

    void UpdateAttack()
    {
        if (movement != null)
            movement.ForceStop();

        // Windup suresi
        if (stateTimer < attackWindup)
        {
            // Hazirlaniyor
            return;
        }

        // Saldiri
        if (!hasAttacked)
        {
            PerformAttack();
            hasAttacked = true;
        }

        // Saldiri suresi doldu
        if (stateTimer >= attackWindup + attackDuration)
        {
            attackTimer = attackCooldown;

            if (playerDetected && IsPlayerInRange(detectionRange))
            {
                ChangeState(AIState.Chase);
            }
            else
            {
                playerDetected = false;
                HideAlertIndicator();
                ChangeState(AIState.Patrol);
            }
        }
    }

    void UpdateHurt()
    {
        // Geri sekme sirasinda bekle
        if (stateTimer >= 0.4f)
        {
            if (playerDetected && canChase)
            {
                ChangeState(AIState.Chase);
            }
            else
            {
                ChangeState(canPatrol ? AIState.Patrol : AIState.Idle);
            }
        }
    }

    #endregion

    #region State Management

    void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        if (currentState == AIState.Dead) return;

        AIState oldState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // State giris
        switch (newState)
        {
            case AIState.Alert:
                // Oyuncuya don
                if (player != null && movement != null)
                {
                    float dir = Mathf.Sign(player.position.x - transform.position.x);
                    if ((dir > 0) != movement.IsFacingRight)
                    {
                        movement.Turn();
                    }
                }
                break;

            case AIState.Attack:
                hasAttacked = false;
                OnAttackStart?.Invoke();
                break;

            case AIState.Patrol:
                if (returnToStart)
                {
                    patrolDirection = transform.position.x < startPosition.x ? 1 : -1;
                }
                waitingAtPatrolPoint = false;
                break;

            case AIState.Chase:
                lastChaseDirection = 0;
                lastDirectionChangeTime = 0;
                break;

            case AIState.Hurt:
                if (movement != null)
                    movement.ForceStop();
                break;
        }

        OnStateChanged?.Invoke(oldState, newState);
    }

    void PerformAttack()
    {
        OnAttackHit?.Invoke();

        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange * 1.3f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage();

                // Knockback
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null && attackKnockback > 0)
                {
                    Vector2 knockDir = (player.position - transform.position).normalized;
                    knockDir.y = 0.3f;
                    playerRb.AddForce(knockDir.normalized * attackKnockback, ForceMode2D.Impulse);
                }
            }
        }
    }

    #endregion

    #region Event Handlers

    void OnDamaged(float damage)
    {
        if (currentState == AIState.Dead) return;

        ChangeState(AIState.Hurt);

        // Knockback
        if (rb != null && player != null)
        {
            Vector2 knockDir = (transform.position - player.position).normalized;
            rb.AddForce(knockDir * 4f, ForceMode2D.Impulse);
        }

        // Oyuncuyu farkettiyse alert goster
        if (!playerDetected && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < loseTargetRange)
            {
                playerDetected = true;
                lastKnownPlayerX = player.position.x;
                ShowAlertIndicator();
            }
        }
    }

    void OnDeath()
    {
        ChangeState(AIState.Dead);

        if (movement != null)
            movement.ForceStop();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        HideAlertIndicator();
    }


    #endregion

    #region Alert Indicator

    void CreateAlertIndicator()
    {
        if (!showAlertIndicator) return;

        if (alertIndicatorPrefab != null)
        {
            alertIndicatorInstance = Instantiate(alertIndicatorPrefab, transform);
            alertIndicatorInstance.transform.localPosition = alertIndicatorOffset;
        }
        else
        {
            // Basit sprite olustur
            alertIndicatorInstance = new GameObject("AlertIndicator");
            alertIndicatorInstance.transform.SetParent(transform);
            alertIndicatorInstance.transform.localPosition = alertIndicatorOffset;

            SpriteRenderer sr = alertIndicatorInstance.AddComponent<SpriteRenderer>();
            sr.color = Color.red;
            sr.sortingOrder = 100;

            // Basit ! sprite
            Texture2D tex = new Texture2D(4, 8);
            Color[] colors = new Color[32];
            for (int i = 0; i < 32; i++)
            {
                int y = i / 4;
                colors[i] = (y >= 1 && y <= 5) || y == 7 ? Color.white : Color.clear;
            }
            tex.SetPixels(colors);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 8), new Vector2(0.5f, 0.5f), 8);
        }

        alertIndicatorInstance.SetActive(false);
    }

    void ShowAlertIndicator()
    {
        if (alertIndicatorInstance != null)
        {
            alertIndicatorInstance.SetActive(true);

            // Animasyon
            StartCoroutine(AlertPopAnimation());
        }
    }

    void HideAlertIndicator()
    {
        if (alertIndicatorInstance != null)
            alertIndicatorInstance.SetActive(false);
    }

    System.Collections.IEnumerator AlertPopAnimation()
    {
        if (alertIndicatorInstance == null) yield break;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * 0.5f;

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float progress = t / 0.2f;
            // Overshoot
            float scale = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(progress));
            alertIndicatorInstance.transform.localScale = endScale * scale;
            yield return null;
        }

        alertIndicatorInstance.transform.localScale = endScale;
    }

    float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    #endregion

    #region Public Methods

    public void SetTarget(Transform target)
    {
        player = target;
    }

    public void ForceChase()
    {
        if (currentState != AIState.Dead)
        {
            playerDetected = true;
            if (player != null)
                lastKnownPlayerX = player.position.x;
            ChangeState(AIState.Chase);
            ShowAlertIndicator();
        }
    }

    public void ForcePatrol()
    {
        if (currentState != AIState.Dead)
        {
            playerDetected = false;
            HideAlertIndicator();
            ChangeState(AIState.Patrol);
        }
    }

    public void SetPatrolBounds(float left, float right)
    {
        patrolLeftBound = left;
        patrolRightBound = right;
    }

    public bool IsPlayerDetected()
    {
        return playerDetected;
    }

    #endregion

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = movement != null ? movement.NormalizedSpeed : 0f;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsChasing", currentState == AIState.Chase);
        animator.SetBool("IsAttacking", currentState == AIState.Attack);
        animator.SetBool("IsHurt", currentState == AIState.Hurt);
        animator.SetBool("IsDead", currentState == AIState.Dead);
        animator.SetBool("IsAlert", currentState == AIState.Alert);
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged -= OnDamaged;
            enemyHealth.OnDeath -= OnDeath;
        }

    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        float direction = patrolDirection != 0 ? patrolDirection : 1f;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrol bounds
        Gizmos.color = Color.cyan;
        float left = Application.isPlaying ? patrolLeftBound : pos.x - patrolDistance;
        float right = Application.isPlaying ? patrolRightBound : pos.x + patrolDistance;
        Gizmos.DrawLine(new Vector3(left, pos.y, 0), new Vector3(right, pos.y, 0));
        Gizmos.DrawWireSphere(new Vector3(left, pos.y, 0), 0.2f);
        Gizmos.DrawWireSphere(new Vector3(right, pos.y, 0), 0.2f);

        // Wall check
        if (detectWalls)
        {
            Gizmos.color = Color.red;
            Vector3 wallCheckOrigin = transform.position + Vector3.up * 0.2f;
            Gizmos.DrawLine(wallCheckOrigin, wallCheckOrigin + Vector3.right * direction * wallCheckDistance);
        }

        // Cliff check
        if (detectCliffs)
        {
            Gizmos.color = Color.blue;
            Vector3 cliffCheckOrigin = transform.position + Vector3.right * direction * 0.5f + Vector3.down * 0.3f;
            Gizmos.DrawLine(cliffCheckOrigin, cliffCheckOrigin + Vector3.down * cliffCheckDistance);
        }

        // Max chase distance
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(pos, maxChaseDistance);

        // State indicator
        Gizmos.color = currentState switch
        {
            AIState.Idle => Color.gray,
            AIState.Patrol => Color.blue,
            AIState.Alert => Color.yellow,
            AIState.Chase => Color.red,
            AIState.Attack => Color.magenta,
            AIState.Hurt => Color.white,
            _ => Color.black
        };
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, 0.15f);
    }
}
