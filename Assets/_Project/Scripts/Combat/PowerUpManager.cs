using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("Power-Up Settings")]
    public float speedBoostMultiplier = 1.5f;
    public float magnetRange = 5f;

    // Aktif power-up'lar
    private Dictionary<PowerUpType, float> activePowerUps = new Dictionary<PowerUpType, float>();

    // Player referansi
    private PlayerController player;
    private float originalSpeed;
    private float originalJumpForce;

    // Shield durumu
    private bool hasShield = false;
    private GameObject shieldVisual;

    // Double jump
    private bool canDoubleJump = false;
    private bool hasUsedDoubleJump = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        // Player'i bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
            if (player != null)
            {
                originalSpeed = player.moveSpeed;
                originalJumpForce = player.jumpForce;
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            // Player'i tekrar bulmaya calis
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
                if (player != null)
                {
                    originalSpeed = player.moveSpeed;
                    originalJumpForce = player.jumpForce;
                }
            }
            return;
        }

        // Aktif power-up'lari guncelle
        UpdatePowerUps();

        // Miknatıs efekti
        if (HasPowerUp(PowerUpType.Magnet))
        {
            AttractCoins();
        }

        // Double jump kontrolu
        if (HasPowerUp(PowerUpType.DoubleJump))
        {
            CheckDoubleJump();
        }
    }

    void UpdatePowerUps()
    {
        List<PowerUpType> expiredPowerUps = new List<PowerUpType>();

        // Her power-up'in suresini azalt
        List<PowerUpType> keys = new List<PowerUpType>(activePowerUps.Keys);
        foreach (PowerUpType type in keys)
        {
            activePowerUps[type] -= Time.deltaTime;

            if (activePowerUps[type] <= 0)
            {
                expiredPowerUps.Add(type);
            }
        }

        // Suresi dolan power-up'lari kaldir
        foreach (PowerUpType type in expiredPowerUps)
        {
            DeactivatePowerUp(type);
        }
    }

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        // Zaten aktifse suresini uzat
        if (activePowerUps.ContainsKey(type))
        {
            activePowerUps[type] = duration;
        }
        else
        {
            activePowerUps.Add(type, duration);
            ApplyPowerUpEffect(type);
        }

        // UI'da goster
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPowerUpIndicator(type, duration);
        }
    }

    void ApplyPowerUpEffect(PowerUpType type)
    {
        if (player == null) return;

        switch (type)
        {
            case PowerUpType.SpeedBoost:
                player.moveSpeed = originalSpeed * speedBoostMultiplier;
                break;

            case PowerUpType.DoubleJump:
                canDoubleJump = true;
                hasUsedDoubleJump = false;
                break;

            case PowerUpType.Shield:
                hasShield = true;
                CreateShieldVisual();
                break;

            case PowerUpType.Magnet:
                // Miknatıs Update'de isleniyor
                break;

            case PowerUpType.Invincibility:
                // Invincibility - PlayerController'da islenecek
                player.SetInvincible(true);
                break;
        }
    }

    void DeactivatePowerUp(PowerUpType type)
    {
        activePowerUps.Remove(type);

        if (player == null) return;

        switch (type)
        {
            case PowerUpType.SpeedBoost:
                player.moveSpeed = originalSpeed;
                break;

            case PowerUpType.DoubleJump:
                canDoubleJump = false;
                break;

            case PowerUpType.Shield:
                hasShield = false;
                if (shieldVisual != null)
                {
                    Destroy(shieldVisual);
                }
                break;

            case PowerUpType.Magnet:
                // Otomatik olarak durur
                break;

            case PowerUpType.Invincibility:
                player.SetInvincible(false);
                break;
        }

        // UI'dan kaldir
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePowerUpIndicator(type);
        }
    }

    void CreateShieldVisual()
    {
        if (player == null) return;

        shieldVisual = new GameObject("ShieldVisual");
        shieldVisual.transform.SetParent(player.transform);
        shieldVisual.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = shieldVisual.AddComponent<SpriteRenderer>();

        // Kalkan sprite'i olustur
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[1024];
        Color shieldColor = new Color(0.3f, 0.5f, 1f, 0.5f);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = x - 15.5f;
                float dy = y - 15.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > 12f && dist < 15f)
                {
                    pixels[y * 32 + x] = shieldColor;
                }
                else
                {
                    pixels[y * 32 + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);
        sr.sortingOrder = 15;
    }

    void AttractCoins()
    {
        if (player == null) return;

        // Yakin coinleri bul
        Collider2D[] colliders = Physics2D.OverlapCircleAll(player.transform.position, magnetRange);

        foreach (Collider2D col in colliders)
        {
            Coin coin = col.GetComponent<Coin>();
            if (coin != null)
            {
                // Coin'i oyuncuya dogru cek
                Vector3 direction = (player.transform.position - coin.transform.position).normalized;
                coin.transform.position += direction * 10f * Time.deltaTime;
            }
        }
    }

    void CheckDoubleJump()
    {
        if (player == null) return;

        // Yerdeyse double jump'i sifirla
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Basit ground check
        bool isGrounded = Physics2D.Raycast(player.transform.position, Vector2.down, 0.6f);

        if (isGrounded)
        {
            hasUsedDoubleJump = false;
        }
    }

    // Double jump kullan
    public bool TryDoubleJump()
    {
        if (canDoubleJump && !hasUsedDoubleJump)
        {
            hasUsedDoubleJump = true;
            return true;
        }
        return false;
    }

    // Kalkan hasari absorbe etsin mi?
    public bool TryAbsorbDamage()
    {
        if (hasShield)
        {
            // Kalkan kirilir
            DeactivatePowerUp(PowerUpType.Shield);
            return true;
        }
        return false;
    }

    public bool HasPowerUp(PowerUpType type)
    {
        return activePowerUps.ContainsKey(type);
    }

    public float GetRemainingTime(PowerUpType type)
    {
        if (activePowerUps.ContainsKey(type))
        {
            return activePowerUps[type];
        }
        return 0f;
    }

    // Power-up spawn helper
    public static void SpawnPowerUp(Vector3 position, PowerUpType type, float duration = 5f)
    {
        GameObject powerUpObj = new GameObject("PowerUp_" + type.ToString());
        powerUpObj.transform.position = position;

        PowerUp powerUp = powerUpObj.AddComponent<PowerUp>();
        powerUp.powerUpType = type;
        powerUp.duration = duration;
    }
}
