using UnityEngine;

public class PatrolMovement : MonoBehaviour
{
    [Header("Hareket Ayarlari")]
    public float speed = 3f;
    public float leftDistance = 5f;
    public float rightDistance = 5f;

    [Header("Opsiyonel")]
    public bool flipSprite = true;
    public bool startMovingRight = true;

    [Header("Hortum Etkisi")]
    public bool tornadoEnabled = true;
    public float tornadoRadius = 3f;
    public float pullForce = 10f;
    public float spinForce = 15f;
    public float liftForce = 8f;
    public float throwForce = 12f;

    private Vector3 startPosition;
    private bool movingRight;
    private SpriteRenderer spriteRenderer;
    private Transform playerInZone;
    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private float angleAroundTornado = 0f;

    void Start()
    {
        startPosition = transform.position;
        movingRight = startMovingRight;
        spriteRenderer = GetComponent<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = tornadoRadius;
            circle.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        float direction = movingRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        if (movingRight && transform.position.x >= startPosition.x + rightDistance)
        {
            movingRight = false;
            Flip();
        }
        else if (!movingRight && transform.position.x <= startPosition.x - leftDistance)
        {
            movingRight = true;
            Flip();
        }

        if (playerInZone != null && tornadoEnabled)
        {
            ApplyTornadoEffect();
        }
    }

    void ApplyTornadoEffect()
    {
        if (playerRb == null) return;

        Vector2 tornadoCenter = transform.position;
        Vector2 playerPos = playerInZone.position;
        Vector2 dirToCenter = tornadoCenter - playerPos;
        float distance = dirToCenter.magnitude;

        float forceFactor = 1f - Mathf.Clamp01(distance / tornadoRadius);
        forceFactor = forceFactor * forceFactor;

        Vector2 pullDir = dirToCenter.normalized;
        playerRb.AddForce(pullDir * pullForce * forceFactor, ForceMode2D.Force);

        Vector2 tangent = new Vector2(-dirToCenter.y, dirToCenter.x).normalized;
        playerRb.AddForce(tangent * spinForce * forceFactor, ForceMode2D.Force);

        playerRb.AddForce(Vector2.up * liftForce * forceFactor, ForceMode2D.Force);

        if (playerInZone != null)
        {
            angleAroundTornado += spinForce * forceFactor * Time.deltaTime * 10f;
            float tilt = Mathf.Sin(angleAroundTornado * 0.5f) * 15f * forceFactor;
            playerInZone.rotation = Quaternion.Euler(0, 0, tilt);
        }

        if (CameraFollow.Instance != null && forceFactor > 0.5f)
        {
            CameraFollow.Instance.Shake(0.05f * forceFactor, 0.02f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = other.transform;
            playerRb = other.GetComponent<Rigidbody2D>();
            playerController = other.GetComponent<PlayerController>();
            angleAroundTornado = 0f;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerRb != null && tornadoEnabled)
            {
                Vector2 throwDir = (other.transform.position - transform.position).normalized;
                throwDir.y = Mathf.Abs(throwDir.y) + 0.5f;
                playerRb.AddForce(throwDir * throwForce, ForceMode2D.Impulse);
            }

            if (playerInZone != null)
            {
                playerInZone.rotation = Quaternion.identity;
            }

            playerInZone = null;
            playerRb = null;
            playerController = null;
        }
    }

    void Flip()
    {
        if (flipSprite && spriteRenderer != null)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos + Vector3.left * leftDistance, pos + Vector3.right * rightDistance);
        Gizmos.DrawWireSphere(pos + Vector3.left * leftDistance, 0.3f);
        Gizmos.DrawWireSphere(pos + Vector3.right * rightDistance, 0.3f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, tornadoRadius);
    }
}
