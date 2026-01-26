using UnityEngine;

/// <summary>
/// Parallax arka plan efekti - kameraya göre farklı hızlarda hareket eder
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    public float parallaxStrength = 0.5f; // 0 = sabit, 1 = kamerayla aynı hız
    public bool infiniteHorizontal = true;
    public bool infiniteVertical = false;

    private Transform cam;
    private Vector3 lastCamPos;
    private float textureUnitSizeX;
    private float textureUnitSizeY;
    private Vector3 startPos;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
        startPos = transform.position;

        // Sprite boyutunu hesapla (sonsuz scroll için)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Sprite sprite = sr.sprite;
            textureUnitSizeX = sprite.texture.width / sprite.pixelsPerUnit * transform.localScale.x;
            textureUnitSizeY = sprite.texture.height / sprite.pixelsPerUnit * transform.localScale.y;
        }
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cam.position - lastCamPos;

        // Parallax hareketi
        transform.position += new Vector3(
            deltaMovement.x * parallaxStrength,
            deltaMovement.y * parallaxStrength,
            0
        );

        lastCamPos = cam.position;

        // Sonsuz scroll
        if (infiniteHorizontal && textureUnitSizeX > 0)
        {
            float offsetX = (cam.position.x - startPos.x) % textureUnitSizeX;
            transform.position = new Vector3(startPos.x + offsetX, transform.position.y, transform.position.z);
        }

        if (infiniteVertical && textureUnitSizeY > 0)
        {
            float offsetY = (cam.position.y - startPos.y) % textureUnitSizeY;
            transform.position = new Vector3(transform.position.x, startPos.y + offsetY, transform.position.z);
        }
    }
}
