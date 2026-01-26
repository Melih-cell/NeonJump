using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;
    public float rotateSpeed = 100f;
    public float bounceSpeed = 2f;
    public float bounceHeight = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Donme efekti
        transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);

        // Yukari asagi hareket
        float newY = startPos.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Skor ekle
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(value);
            }

            // Coin sesi
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin();
            }

            // Yok et
            Destroy(gameObject);
        }
    }
}
