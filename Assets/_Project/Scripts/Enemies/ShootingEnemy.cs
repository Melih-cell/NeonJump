using UnityEngine;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Shooting")]
    public float shootInterval = 2f;
    public float projectileSpeed = 8f;
    public float projectileLifetime = 3f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public bool needsLineOfSight = true;
    public LayerMask obstacleLayer;

    [Header("Movement")]
    public bool canMove = false;
    public float moveSpeed = 2f;
    public float moveDistance = 3f;

    private float shootTimer;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private bool movingRight = true;
    private bool isDead = false;
    private bool isInitialized = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        shootTimer = shootInterval;

        if (spriteRenderer == null)
        {
            Debug.LogWarning("ShootingEnemy: SpriteRenderer bulunamadi! " + gameObject.name);
        }

        isInitialized = true;
    }

    void Update()
    {
        if (isDead || !isInitialized) return;

        // Hareket
        if (canMove)
        {
            Move();
        }

        // Ates zamanlayicisi
        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0 && CanShoot())
        {
            Shoot();
            shootTimer = shootInterval;
        }

        // Oyuncuya bak
        if (player != null && spriteRenderer != null)
        {
            spriteRenderer.flipX = player.position.x > transform.position.x;
        }
    }

    void Move()
    {
        float targetX = movingRight ? startPosition.x + moveDistance : startPosition.x - moveDistance;

        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
        {
            movingRight = !movingRight;
        }
    }

    bool CanShoot()
    {
        if (player == null) return false;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > detectionRange) return false;

        if (needsLineOfSight)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
            if (hit.collider != null) return false;
        }

        return true;
    }

    void Shoot()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;

        // Mermi olustur
        GameObject projectile = new GameObject("Projectile");
        projectile.transform.position = transform.position + (Vector3)direction * 0.5f;
        projectile.tag = "EnemyProjectile";

        // Sprite
        SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
        sr.color = Color.red;
        sr.sortingOrder = 5;

        // Runtime sprite olustur
        Texture2D texture = new Texture2D(8, 8);
        Color[] colors = new Color[64];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);

        // Collider
        CircleCollider2D col = projectile.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.isTrigger = true;

        // Rigidbody
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearVelocity = direction * projectileSpeed;

        // Projectile script
        Projectile proj = projectile.AddComponent<Projectile>();
        proj.damage = 1;

        Destroy(projectile, projectileLifetime);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        transform.localScale = new Vector3(transform.localScale.x, 0.3f, 1f);
        GetComponent<Collider2D>().enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (canMove)
        {
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start - Vector3.right * moveDistance, start + Vector3.right * moveDistance);
        }
    }
}
