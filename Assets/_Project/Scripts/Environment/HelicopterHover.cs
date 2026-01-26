using UnityEngine;

/// <summary>
/// Helikoptere gerçekçi hovering efekti verir
/// Titreme, sallanma ve hafif yukarı-aşağı hareket
/// </summary>
public class HelicopterHover : MonoBehaviour
{
    [Header("Titreme Efekti")]
    public float shakeIntensity = 0.02f; // Titreme şiddeti
    public float shakeSpeed = 25f; // Titreme hızı

    [Header("Sallanma Efekti")]
    public float swayAmount = 0.3f; // Yatay sallanma miktarı
    public float swaySpeed = 0.8f; // Sallanma hızı

    [Header("Yukarı-Aşağı Hareket")]
    public float bobAmount = 0.15f; // Yukarı-aşağı hareket miktarı
    public float bobSpeed = 1.2f; // Yukarı-aşağı hız

    [Header("Rotasyon Sallanması")]
    public float tiltAmount = 2f; // Eğilme açısı (derece)
    public float tiltSpeed = 0.6f; // Eğilme hızı

    // Başlangıç değerleri
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timeOffset;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Her helikopter farklı zamanlama ile başlasın
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = Time.time + timeOffset;

        // Titreme (hızlı, küçük rastgele hareket)
        float shakeX = (Mathf.PerlinNoise(time * shakeSpeed, 0f) - 0.5f) * 2f * shakeIntensity;
        float shakeY = (Mathf.PerlinNoise(0f, time * shakeSpeed) - 0.5f) * 2f * shakeIntensity;

        // Yatay sallanma (yavaş, düzgün hareket)
        float swayX = Mathf.Sin(time * swaySpeed) * swayAmount;

        // Yukarı-aşağı bobbing
        float bobY = Mathf.Sin(time * bobSpeed) * bobAmount;

        // Pozisyon hesapla
        Vector3 newPosition = startPosition;
        newPosition.x += shakeX + swayX;
        newPosition.y += shakeY + bobY;
        transform.position = newPosition;

        // Rotasyon eğilmesi (hareket yönüne göre)
        float tiltZ = Mathf.Sin(time * tiltSpeed) * tiltAmount;
        float tiltX = Mathf.Cos(time * tiltSpeed * 0.7f) * tiltAmount * 0.5f;

        transform.rotation = startRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
    }

    // Editor'da görselleştirme
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;

        // Hareket alanını göster
        Gizmos.DrawWireCube(pos, new Vector3(swayAmount * 2 + shakeIntensity * 2, bobAmount * 2 + shakeIntensity * 2, 0.1f));
    }
}
