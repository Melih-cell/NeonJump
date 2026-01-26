using UnityEngine;

/// <summary>
/// Hareketli platform - oyuncuyu taşır
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    public enum MoveType
    {
        Horizontal,
        Vertical,
        Circular,
        Path
    }

    [Header("Movement Settings")]
    public MoveType moveType = MoveType.Horizontal;
    public float moveDistance = 5f;
    public float circleRadius { get => moveDistance; set => moveDistance = value; } // Backward compatibility
    public float moveSpeed = 2f;
    public float waitTime = 0.5f;

    [Header("Path Points (for Path type)")]
    public Transform[] pathPoints;

    [Header("Visual")]
    public Color platformColor = new Color(0.3f, 0.6f, 1f);

    private Vector3 startPos;
    private int currentPathIndex = 0;
    private float waitTimer = 0f;
    private bool waiting = false;
    private bool movingForward = true;
    private float circleAngle = 0f;

    // Oyuncuyu taşımak için
    private Transform playerOnPlatform;
    private Vector3 lastPosition;

    void Start()
    {
        startPos = transform.position;
        lastPosition = transform.position;

        // Görsel ayar
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = platformColor;
        }
    }

    void FixedUpdate()
    {
        lastPosition = transform.position;

        if (waiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0)
            {
                waiting = false;
            }
            return;
        }

        Vector3 newPosition = transform.position;

        switch (moveType)
        {
            case MoveType.Horizontal:
                newPosition = MoveHorizontal();
                break;
            case MoveType.Vertical:
                newPosition = MoveVertical();
                break;
            case MoveType.Circular:
                newPosition = MoveCircular();
                break;
            case MoveType.Path:
                newPosition = MovePath();
                break;
        }

        transform.position = newPosition;

        // Oyuncuyu taşı
        if (playerOnPlatform != null)
        {
            Vector3 delta = transform.position - lastPosition;
            playerOnPlatform.position += delta;
        }
    }

    Vector3 MoveHorizontal()
    {
        float targetX = movingForward ? startPos.x + moveDistance : startPos.x;
        Vector3 target = new Vector3(targetX, startPos.y, startPos.z);

        Vector3 newPos = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.fixedDeltaTime);

        if (Vector3.Distance(newPos, target) < 0.01f)
        {
            movingForward = !movingForward;
            waiting = true;
            waitTimer = waitTime;
        }

        return newPos;
    }

    Vector3 MoveVertical()
    {
        float targetY = movingForward ? startPos.y + moveDistance : startPos.y;
        Vector3 target = new Vector3(startPos.x, targetY, startPos.z);

        Vector3 newPos = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.fixedDeltaTime);

        if (Vector3.Distance(newPos, target) < 0.01f)
        {
            movingForward = !movingForward;
            waiting = true;
            waitTimer = waitTime;
        }

        return newPos;
    }

    Vector3 MoveCircular()
    {
        circleAngle += moveSpeed * Time.fixedDeltaTime;
        float x = startPos.x + Mathf.Cos(circleAngle) * moveDistance;
        float y = startPos.y + Mathf.Sin(circleAngle) * moveDistance;
        return new Vector3(x, y, startPos.z);
    }

    Vector3 MovePath()
    {
        if (pathPoints == null || pathPoints.Length == 0)
            return transform.position;

        Vector3 target = pathPoints[currentPathIndex].position;
        Vector3 newPos = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.fixedDeltaTime);

        if (Vector3.Distance(newPos, target) < 0.01f)
        {
            currentPathIndex++;
            if (currentPathIndex >= pathPoints.Length)
            {
                currentPathIndex = 0;
            }
            waiting = true;
            waitTimer = waitTime;
        }

        return newPos;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Üstten çarptıysa
            if (collision.contacts[0].normal.y < -0.5f)
            {
                playerOnPlatform = collision.transform;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPos : transform.position;

        Gizmos.color = Color.yellow;

        switch (moveType)
        {
            case MoveType.Horizontal:
                Gizmos.DrawLine(pos, pos + Vector3.right * moveDistance);
                break;
            case MoveType.Vertical:
                Gizmos.DrawLine(pos, pos + Vector3.up * moveDistance);
                break;
            case MoveType.Circular:
                Gizmos.DrawWireSphere(pos, moveDistance);
                break;
            case MoveType.Path:
                if (pathPoints != null)
                {
                    for (int i = 0; i < pathPoints.Length; i++)
                    {
                        if (pathPoints[i] != null)
                        {
                            Gizmos.DrawSphere(pathPoints[i].position, 0.3f);
                            if (i < pathPoints.Length - 1 && pathPoints[i + 1] != null)
                            {
                                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                            }
                        }
                    }
                    // Son noktadan ilk noktaya
                    if (pathPoints.Length > 1 && pathPoints[0] != null && pathPoints[pathPoints.Length - 1] != null)
                    {
                        Gizmos.DrawLine(pathPoints[pathPoints.Length - 1].position, pathPoints[0].position);
                    }
                }
                break;
        }
    }
}
