using UnityEngine;

/// <summary>
/// Oyuncunun nisan yonunu gosteren gorsel indicator
/// 8 yonlu ates sistemi icin
/// </summary>
public class AimIndicator : MonoBehaviour
{
    public static AimIndicator Instance;

    [Header("Settings")]
    public float distance = 1.2f;        // Karakterden uzaklik
    public float size = 0.4f;            // Indicator boyutu
    public Color normalColor = new Color(1f, 1f, 0f, 0.8f);      // Sari
    public Color firingColor = new Color(1f, 0.5f, 0f, 1f);      // Turuncu
    public bool showOnlyWhenAiming = true;  // Sadece nisan alirken goster
    public float fadeSpeed = 8f;

    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private PlayerController playerController;
    private Vector2 currentAimDirection;
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateVisual();
        FindPlayer();
    }

    void CreateVisual()
    {
        // Sprite renderer ekle
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 100;

        // Nisan sprite'i olustur (ok seklinde)
        Texture2D tex = CreateAimTexture(32);
        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        spriteRenderer.color = normalColor;

        transform.localScale = Vector3.one * size;
    }

    Texture2D CreateAimTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        // Seffaf arka plan
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.clear;

        int centerX = size / 2;
        int centerY = size / 2;

        // Cember ciz
        float radius = size * 0.35f;
        float innerRadius = size * 0.2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));

                // Dis cember
                if (dist >= innerRadius && dist <= radius)
                {
                    float edgeFade = 1f - Mathf.Abs(dist - (innerRadius + radius) / 2f) / ((radius - innerRadius) / 2f);
                    colors[y * size + x] = new Color(1, 1, 1, edgeFade * 0.9f);
                }
            }
        }

        // Ok (sag tarafa bakan)
        int arrowStartX = centerX + 2;
        int arrowEndX = size - 4;
        int arrowWidth = 3;

        // Ok govdesi
        for (int x = arrowStartX; x < arrowEndX - 3; x++)
        {
            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                if (y >= 0 && y < size)
                    colors[y * size + x] = Color.white;
            }
        }

        // Ok ucu
        for (int i = 0; i < 5; i++)
        {
            int x = arrowEndX - i;
            int yTop = centerY + i;
            int yBottom = centerY - i;

            if (x >= 0 && x < size)
            {
                if (yTop < size) colors[yTop * size + x] = Color.white;
                if (yBottom >= 0) colors[yBottom * size + x] = Color.white;
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }

    void FindPlayer()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Nisan yonunu hesapla
        UpdateAimDirection();

        // Pozisyon ve rotasyon guncelle
        UpdatePosition();

        // Alpha animasyonu
        UpdateAlpha();
    }

    void UpdateAimDirection()
    {
        // WeaponManager'dan gercek aim direction'i al
        if (WeaponManager.Instance != null)
        {
            currentAimDirection = WeaponManager.Instance.GetAimDirection();
        }
        else if (playerTransform != null)
        {
            // Fallback: karakterin baktigi yon
            float facingDir = playerTransform.localScale.x > 0 ? 1f : -1f;
            currentAimDirection = new Vector2(facingDir, 0f);
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
        {
            targetAlpha = 0f;
            return;
        }

        // Dikey veya capraz nisan varsa goster
        bool hasVerticalAim = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ||
                              keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;

        bool isFiring = keyboard.jKey.isPressed ||
            (UnityEngine.InputSystem.Mouse.current != null &&
             UnityEngine.InputSystem.Mouse.current.leftButton.isPressed);

        // Gosterge gorunurlugu
        if (hasVerticalAim)
        {
            targetAlpha = 1f;  // Dikey nisan varsa tam gorun
        }
        else if (isFiring)
        {
            targetAlpha = 0.7f;  // Ates ederken gorun
        }
        else if (!showOnlyWhenAiming)
        {
            targetAlpha = 0.3f;  // Her zaman hafif gorun
        }
        else
        {
            targetAlpha = 0f;  // Gizle
        }
    }

    void UpdatePosition()
    {
        if (playerTransform == null) return;

        // Karakterin merkezinden nisan yonune dogru pozisyon
        Vector3 offset = (Vector3)(currentAimDirection * distance);
        transform.position = playerTransform.position + offset + Vector3.up * 0.3f;

        // Rotasyon - nisan yonune dogru
        float angle = Mathf.Atan2(currentAimDirection.y, currentAimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void UpdateAlpha()
    {
        // Yumusak gecis
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (spriteRenderer != null)
        {
            // Ates ederken renk degistir
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool isFiring = keyboard != null && keyboard.jKey.isPressed;

            Color targetColor = isFiring ? firingColor : normalColor;
            targetColor.a = currentAlpha;
            spriteRenderer.color = targetColor;
        }
    }

    /// <summary>
    /// Mobil icin nisan yonunu ayarla
    /// </summary>
    public void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.1f)
        {
            currentAimDirection = direction.normalized;
            targetAlpha = 1f;
        }
    }

    /// <summary>
    /// Indicator'u goster/gizle
    /// </summary>
    public void SetVisible(bool visible)
    {
        targetAlpha = visible ? 1f : 0f;
    }
}
