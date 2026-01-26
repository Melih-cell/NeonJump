using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public bool isActivated = false;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
    public Color activeColor = new Color(0f, 1f, 0.5f);

    private SpriteRenderer spriteRenderer;
    private static Checkpoint lastCheckpoint;
    private static Vector3 lastCheckpointPosition;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateCheckpointSprite();
        }

        UpdateVisual();

        // Collider ekle
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 3f);
        }
    }

    void CreateCheckpointSprite()
    {
        // Bayrak benzeri sprite
        Texture2D tex = new Texture2D(16, 32);
        Color[] pixels = new Color[512];

        // Temizle
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Direk
        for (int y = 0; y < 32; y++)
        {
            pixels[y * 16 + 7] = Color.white;
            pixels[y * 16 + 8] = Color.white;
        }

        // Bayrak (ust kisim)
        for (int y = 20; y < 30; y++)
        {
            for (int x = 9; x < 16; x++)
            {
                pixels[y * 16 + x] = Color.white;
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 32), new Vector2(0.5f, 0f), 16);
        spriteRenderer.sortingOrder = 5;
    }

    void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActivated ? activeColor : inactiveColor;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        // Onceki checkpoint'i deaktive et
        if (lastCheckpoint != null && lastCheckpoint != this)
        {
            lastCheckpoint.isActivated = false;
            lastCheckpoint.UpdateVisual();
        }

        // Bu checkpoint'i aktive et
        isActivated = true;
        lastCheckpoint = this;
        lastCheckpointPosition = transform.position + Vector3.up * 1f; // Biraz yukari

        UpdateVisual();

        // Ses cal
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoin(); // Checkpoint sesi olarak kullan
        }

        // Particle efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayCoinCollect(transform.position);
        }
    }

    // Static metodlar - GameManager tarafindan kullanilacak
    public static bool HasCheckpoint()
    {
        return lastCheckpoint != null;
    }

    public static Vector3 GetCheckpointPosition()
    {
        return lastCheckpointPosition;
    }

    public static void ResetCheckpoints()
    {
        lastCheckpoint = null;
        lastCheckpointPosition = Vector3.zero;
    }
}
