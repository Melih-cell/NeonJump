using UnityEngine;

public class SimpleEnemyMove : MonoBehaviour
{
    public float speed = 3f;
    public float distance = 5f;

    private Vector3 startPos;
    private bool goingRight = true;

    void Start()
    {
        startPos = transform.position;
        gameObject.tag = "Enemy";
        Debug.Log("SimpleEnemyMove BASLADI: " + gameObject.name);
    }

    void Update()
    {
        // Basit hareket - transform ile
        float dir = goingRight ? 1f : -1f;
        transform.position += Vector3.right * dir * speed * Time.deltaTime;

        // Sinir kontrolu
        if (goingRight && transform.position.x > startPos.x + distance)
        {
            goingRight = false;
            Flip();
        }
        else if (!goingRight && transform.position.x < startPos.x - distance)
        {
            goingRight = true;
            Flip();
        }
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // Oyuncuya hasar ver
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            PlayerController player = col.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                // Ustten mi geldi?
                if (col.transform.position.y > transform.position.y + 0.5f)
                {
                    // Dusmanı oldur
                    if (GameManager.Instance != null)
                        GameManager.Instance.EnemyKilled(transform.position);

                    Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 10f);

                    Destroy(gameObject);
                }
                else
                {
                    // Hasar ver
                    player.TakeDamage();
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            PlayerController player = col.GetComponent<PlayerController>();
            if (player != null)
                player.TakeDamage();
        }
    }
}
