using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    public enum FlyPattern { Horizontal, Vertical, Circular, Chase }

    [Header("Movement")]
    public FlyPattern pattern = FlyPattern.Horizontal;
    public float moveSpeed = 3f;
    public float moveDistance = 4f;

    [Header("Chase Settings")]
    public float chaseRange = 8f;
    public float chaseSpeed = 4f;

    [Header("Circular")]
    public float circleRadius = 2f;

    private Vector3 startPosition;
    private float angle;
    private bool movingForward = true;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (spriteRenderer == null)
        {
            Debug.LogWarning("FlyingEnemy: SpriteRenderer bulunamadi! " + gameObject.name);
        }
    }

    void Update()
    {
        if (isDead) return;

        switch (pattern)
        {
            case FlyPattern.Horizontal:
                HorizontalMovement();
                break;
            case FlyPattern.Vertical:
                VerticalMovement();
                break;
            case FlyPattern.Circular:
                CircularMovement();
                break;
            case FlyPattern.Chase:
                ChaseMovement();
                break;
        }
    }

    void HorizontalMovement()
    {
        float targetX = movingForward ? startPosition.x + moveDistance : startPosition.x - moveDistance;

        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
        {
            movingForward = !movingForward;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = movingForward;
    }

    void VerticalMovement()
    {
        float targetY = movingForward ? startPosition.y + moveDistance : startPosition.y - moveDistance;

        Vector3 targetPos = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.y - targetY) < 0.1f)
        {
            movingForward = !movingForward;
        }
    }

    void CircularMovement()
    {
        angle += moveSpeed * Time.deltaTime;
        float x = startPosition.x + Mathf.Cos(angle) * circleRadius;
        float y = startPosition.y + Mathf.Sin(angle) * circleRadius;
        transform.position = new Vector3(x, y, startPosition.z);

        // Sprite yonu
        if (spriteRenderer != null)
            spriteRenderer.flipX = Mathf.Cos(angle) > 0;
    }

    void ChaseMovement()
    {
        if (player == null)
        {
            HorizontalMovement();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer < chaseRange)
        {
            // Oyuncuya dogru hareket
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * chaseSpeed * Time.deltaTime;

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction.x > 0;
        }
        else
        {
            // Normal hareket
            HorizontalMovement();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Dusme efekti
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
        }

        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 1f);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = Color.cyan;

        switch (pattern)
        {
            case FlyPattern.Horizontal:
                Gizmos.DrawLine(start - Vector3.right * moveDistance, start + Vector3.right * moveDistance);
                break;
            case FlyPattern.Vertical:
                Gizmos.DrawLine(start - Vector3.up * moveDistance, start + Vector3.up * moveDistance);
                break;
            case FlyPattern.Circular:
                Gizmos.DrawWireSphere(start, circleRadius);
                break;
            case FlyPattern.Chase:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, chaseRange);
                break;
        }
    }
}
