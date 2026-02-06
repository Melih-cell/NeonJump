using UnityEngine;

/// <summary>
/// Parallax arka plan efekti - kameraya gore farkli hizlarda hareket eder
/// parallaxEffect = 0: Arka plan sabit kalir (en yavas, en uzak)
/// parallaxEffect = 1: Arka plan kamerayla ayni hizda gider (on plan)
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    [Tooltip("0 = sabit (en uzak), 1 = kamerayla ayni hiz (en yakin)")]
    public float parallaxEffect = 0.5f;

    [Tooltip("Yatay parallax aktif")]
    public bool parallaxX = true;

    [Tooltip("Dikey parallax aktif")]
    public bool parallaxY = false;

    [Header("Infinite Scroll")]
    public bool infiniteScrollX = false;
    public bool infiniteScrollY = false;

    private Transform cam;
    private Vector3 startPos;
    private Vector3 startCamPos;
    private float spriteWidth;
    private float spriteHeight;

    void Start()
    {
        cam = Camera.main?.transform;
        if (cam == null)
        {
            Debug.LogWarning("[ParallaxBackground] Main Camera bulunamadi!");
            enabled = false;
            return;
        }

        startPos = transform.position;
        startCamPos = cam.position;

        // Sprite boyutunu hesapla (sonsuz scroll icin)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            spriteWidth = sr.bounds.size.x;
            spriteHeight = sr.bounds.size.y;
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Kameranin baslangictan ne kadar hareket ettigi
        Vector3 camDelta = cam.position - startCamPos;

        // Parallax pozisyonu hesapla
        // parallaxEffect = 0: arka plan yerinde kalir
        // parallaxEffect = 1: arka plan kamerayla birlikte hareket eder
        float newX = startPos.x;
        float newY = startPos.y;

        if (parallaxX)
        {
            // Arka plan, kameranin hareketinin bir kismini takip eder
            newX = startPos.x + (camDelta.x * parallaxEffect);
        }

        if (parallaxY)
        {
            newY = startPos.y + (camDelta.y * parallaxEffect);
        }

        transform.position = new Vector3(newX, newY, transform.position.z);

        // Sonsuz scroll (opsiyonel)
        if (infiniteScrollX && spriteWidth > 0)
        {
            // Kameraya gore pozisyonu ayarla
            float relativeX = cam.position.x * (1 - parallaxEffect);
            float wrapX = Mathf.Repeat(relativeX, spriteWidth);
            transform.position = new Vector3(startPos.x + wrapX - spriteWidth / 2, transform.position.y, transform.position.z);
        }

        if (infiniteScrollY && spriteHeight > 0)
        {
            float relativeY = cam.position.y * (1 - parallaxEffect);
            float wrapY = Mathf.Repeat(relativeY, spriteHeight);
            transform.position = new Vector3(transform.position.x, startPos.y + wrapY - spriteHeight / 2, transform.position.z);
        }
    }

    /// <summary>
    /// Parallax ayarlarini runtime'da degistirmek icin
    /// </summary>
    public void SetParallaxEffect(float effect)
    {
        parallaxEffect = Mathf.Clamp01(effect);
    }

    /// <summary>
    /// Baslangic pozisyonunu guncelle (sahne degisikliklerinde)
    /// </summary>
    public void ResetStartPosition()
    {
        if (cam != null)
        {
            startPos = transform.position;
            startCamPos = cam.position;
        }
    }
}
