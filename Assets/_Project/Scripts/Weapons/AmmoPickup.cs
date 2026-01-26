using UnityEngine;

/// <summary>
/// Toplanabilir mermi - mevcut silaha mermi ekler
/// </summary>
public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30;

    [Header("Visual")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.15f;
    public bool enableBob = true;

    [Header("Pickup")]
    public float pickupRadius = 0.8f;
    public bool autoPickup = true;

    private Vector3 startPos;
    private SpriteRenderer spriteRenderer;
    private float timeOffset;

    void Start()
    {
        startPos = transform.position;
        timeOffset = Random.value * Mathf.PI * 2f;

        // Sprite oluştur
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Collider ekle
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = pickupRadius;
            col.isTrigger = true;
        }

        // Görsel ayarla
        SetupVisual();
    }

    void SetupVisual()
    {
        spriteRenderer.color = new Color(0.9f, 0.7f, 0.2f); // Altın sarısı

        // Mermi kutusu sprite'ı
        if (spriteRenderer.sprite == null)
        {
            Texture2D tex = new Texture2D(16, 12);
            Color[] colors = new Color[16 * 12];

            // Şeffaf arka plan
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.clear;

            // Kutu şekli
            for (int x = 2; x < 14; x++)
            {
                for (int y = 2; y < 10; y++)
                {
                    // Kenarlar daha koyu
                    if (x == 2 || x == 13 || y == 2 || y == 9)
                        colors[y * 16 + x] = new Color(0.6f, 0.4f, 0.1f);
                    else
                        colors[y * 16 + x] = Color.white;
                }
            }

            // Mermi işaretleri (üstte)
            for (int i = 0; i < 3; i++)
            {
                int x = 5 + i * 3;
                colors[7 * 16 + x] = new Color(0.3f, 0.3f, 0.3f);
                colors[6 * 16 + x] = new Color(0.3f, 0.3f, 0.3f);
            }

            tex.SetPixels(colors);
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 12), new Vector2(0.5f, 0.5f), 16);
        }

        spriteRenderer.sortingOrder = 5;
    }

    void Update()
    {
        // Yukarı aşağı hareket
        if (enableBob)
        {
            float newY = startPos.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoPickup) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            TryPickup();
        }
    }

    public void TryPickup()
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.AddAmmoToCurrentWeapon(ammoAmount);

            // Efekt
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayCoinCollect(transform.position);
            }

            // Ses
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin();
            }

            Debug.Log($"+{ammoAmount} Mermi!");

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Runtime'da mermi pickup oluştur
    /// </summary>
    public static GameObject Spawn(Vector3 position, int amount = 30)
    {
        GameObject obj = new GameObject("AmmoPickup");
        obj.transform.position = position;

        AmmoPickup pickup = obj.AddComponent<AmmoPickup>();
        pickup.ammoAmount = amount;

        return obj;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
