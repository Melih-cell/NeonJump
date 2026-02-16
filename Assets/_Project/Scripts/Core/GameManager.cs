using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Transform player;
    public float deathY = -99999f;  // Devre disi - dusunce olmez

    [Header("Settings")]
    public int maxHealth = 5;

    private int score = 0;
    private int coins = 0;
    private int health = 3;
    private bool isGameOver = false;
    private bool hasWon = false;

    [Header("Combo System")]
    public float comboTimeWindow = 2f; // Combo suresi (saniye)
    public int maxComboMultiplier = 10; // Maksimum carpan
    private int currentCombo = 0;
    private float comboTimer = 0f;
    private int comboMultiplier = 1;

    void Awake()
    {
        // Her zaman yeni instance'ı kullan (sahne yüklendiğinde)
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Yeni sahne yüklendiğinde değişkenleri sıfırla
        if (scene.name != "MainMenu")
        {
            // Static cache'leri temizle (sahne yeniden yüklendiğinde sprite referansları geçersiz olur)
            WeaponSpriteLoader.ClearCache();
            ResetGame();
        }
    }

    void ResetGame()
    {
        score = 0;
        coins = 0;
        health = maxHealth;
        isGameOver = false;
        hasWon = false;
        player = null;
        Time.timeScale = 1f;

        // Combo sifirla
        currentCombo = 0;
        comboTimer = 0f;
        comboMultiplier = 1;

        FindPlayer();
        UpdateAllUI();

        // Combo UI'i da sifirla
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCombo(0, 1, 0f);
        }
    }

    // Mobil kolay mod aktif mi
    private bool isMobileEasyMode = false;
    // Kolay modda dusman hasari carpani
    private float easyModeDamageMultiplier = 0.5f;
    // Kolay modda ekstra can bonusu
    private int easyModeExtraHealth = 2;

    /// <summary>
    /// Mobil kolay mod aktif mi?
    /// </summary>
    public bool IsMobileEasyMode => isMobileEasyMode;

    void Start()
    {
        // Upgrade'lerden bonus can al
        if (UpgradeManager.Instance != null)
        {
            maxHealth = UpgradeManager.Instance.GetAdjustedMaxHealth(maxHealth);
        }

        // Mobil kolay mod kontrolu
        ApplyMobileDifficultyScaling();

        // Tüm değişkenleri sıfırla
        score = 0;
        coins = 0;
        health = maxHealth;
        isGameOver = false;
        hasWon = false;
        Time.timeScale = 1f;

        // Combo sifirla
        currentCombo = 0;
        comboTimer = 0f;
        comboMultiplier = 1;

        FindPlayer();
        UpdateAllUI();
    }

    void FindPlayer()
    {
        // PlayerController ile ara (en guvenilir)
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            return;
        }

        // Oncelikle Player_Asker'i ara
        GameObject playerObj = GameObject.Find("Player_Asker");

        // Yoksa tag ile ara
        if (playerObj == null)
            playerObj = GameObject.FindWithTag("Player");

        // Yoksa "Player" ismini ara
        if (playerObj == null)
            playerObj = GameObject.Find("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    // === MOBILE LIFECYCLE ===

    void OnApplicationPause(bool pauseStatus)
    {
        // Uygulama minimize edildiginde oyunu duraklat
        if (pauseStatus && !isGameOver && !hasWon)
        {
            PauseGame();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Odak kaybedildiginde oyunu duraklat (mobilde app switch)
        if (!hasFocus && !isGameOver && !hasWon)
        {
            PauseGame();
        }
    }

    private bool isPaused = false;

    public void PauseGame()
    {
        if (isPaused || isGameOver || hasWon) return;
        isPaused = true;
        Time.timeScale = 0f;

        // Pil tasarrufu: pause'da frame rate dusur
        MobileOptimizer.SetMenuFrameRate();

        // PauseMenuController varsa goster
        if (PauseMenuController.Instance != null)
        {
            PauseMenuController.Instance.Pause();
        }
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;

        // Pil tasarrufu: oyun devam edince frame rate yukselt
        MobileOptimizer.SetGameplayFrameRate();

        if (PauseMenuController.Instance != null)
        {
            PauseMenuController.Instance.Resume();
        }
    }

    public bool IsPaused() => isPaused;

    void Update()
    {
        // Android geri butonu destegi
        if (Application.platform == RuntimePlatform.Android)
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (isGameOver || hasWon)
                {
                    // Ana menuye don
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                    return;
                }
                else if (isPaused)
                {
                    ResumeGame();
                    return;
                }
                else
                {
                    PauseGame();
                    return;
                }
            }
        }

        if (isGameOver || hasWon)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return;
        }

        // Player yoksa bul
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // Dusme kontrolu
        if (player.position.y < deathY)
        {
            TakeDamage(health); // Tum cani al
        }

        // Combo timer
        if (currentCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
            else
            {
                // UI guncelle (combo timer gostermek icin)
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCombo(currentCombo, comboMultiplier, comboTimer / comboTimeWindow);
                }
            }
        }
    }

    void UpdateAllUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(score);
            UIManager.Instance.UpdateCoins(coins);
            UIManager.Instance.UpdateHealth(health, maxHealth);
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(score);

        // Floating score text
        if (FloatingTextManager.Instance != null && player != null)
        {
            FloatingTextManager.Instance.ShowScore(player.position + Vector3.up * 0.5f, amount);
        }
    }

    public void AddCoin(int amount)
    {
        coins += amount;
        score += amount * 10;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(score);
            UIManager.Instance.UpdateCoins(coins);
        }

        // Coin toplama efekti
        if (ParticleManager.Instance != null && player != null)
            ParticleManager.Instance.PlayCoinCollect(player.position);

        // Floating coin text
        if (FloatingTextManager.Instance != null && player != null)
        {
            FloatingTextManager.Instance.ShowCoinPickup(player.position + Vector3.up * 0.3f, amount);
        }

        // AdvancedHUD coin display
        if (AdvancedHUD.Instance != null)
        {
            AdvancedHUD.Instance.UpdateCoinDisplay(coins);
        }

        // 100 coin = 1 can
        if (coins >= 100)
        {
            coins -= 100;
            health++;
            if (health > maxHealth) maxHealth = health;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCoins(coins);
                UIManager.Instance.UpdateHealth(health, maxHealth);
            }

            // Bildirim goster
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification("EKSTRA CAN!", "+1 Can kazandin", NotificationType.LevelUp);
            }

            // Floating text
            if (FloatingTextManager.Instance != null && player != null)
            {
                FloatingTextManager.Instance.ShowHeal(player.position + Vector3.up, 1);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isGameOver) return;

        // Mobil kolay modda hasar azalt
        damage = GetAdjustedDamage(damage);

        health -= damage;

        // Hasar alinca combo sifirlanir
        ResetCombo();

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealth(health, maxHealth);

        // Hasar efekti
        if (ParticleManager.Instance != null && player != null)
            ParticleManager.Instance.PlayDamageEffect(player.position);

        if (health <= 0)
        {
            // Checkpoint varsa oradan devam et
            if (Checkpoint.HasCheckpoint())
            {
                RespawnAtCheckpoint();
            }
            else
            {
                GameOver();
            }
        }
    }

    void RespawnAtCheckpoint()
    {
        if (player == null) return;

        // Can ver
        health = 1;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealth(health, maxHealth);

        // Pozisyon
        player.position = Checkpoint.GetCheckpointPosition();

        // Rigidbody sifirla
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Kamera ani gecis
        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SnapToTarget();
            }
        }
    }

    public void Heal(int amount)
    {
        health += amount;
        if (health > maxHealth) health = maxHealth;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealth(health, maxHealth);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Sadece state'i ayarla, UI'ı hemen gösterme
        // PlayerController.Die() ölüm animasyonundan sonra ShowGameOverUI() çağıracak
        Debug.Log("Game Over tetiklendi - animasyon bekleniyor");
    }

    /// <summary>
    /// Ölüm animasyonundan sonra çağrılır - UI'ı gösterir ve oyunu dondurur
    /// </summary>
    public void ShowGameOverUI()
    {
        // Pil tasarrufu: game over ekraninda frame rate dusur
        MobileOptimizer.SetMenuFrameRate();

        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.simulated = false;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(score);

        // Game over sesi ve muzigi durdur
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayGameOver();
        }

        // SaveManager'a istatistikleri kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateHighScore(score);
            SaveManager.Instance.AddDeath();
            SaveManager.Instance.AddCoins(coins);
            SaveManager.Instance.UpdateMaxCombo(currentCombo);
            SaveManager.Instance.Save();
        }

        Debug.Log($"Game Over! Skor: {score}");
    }

    public void Win()
    {
        if (hasWon) return;
        hasWon = true;

        // Pil tasarrufu: kazanma ekraninda frame rate dusur
        MobileOptimizer.SetMenuFrameRate();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowWin(score);

        // Kazanma sesi ve muzigi durdur
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayWin();
        }

        // SaveManager'a istatistikleri kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateHighScore(score);
            SaveManager.Instance.AddCoins(coins);
            SaveManager.Instance.UpdateMaxCombo(currentCombo);
            SaveManager.Instance.Save();
        }

        Debug.Log($"Kazandin! Skor: {score}");
    }

    public void EnemyKilled(Vector3 position)
    {
        // Combo artir
        currentCombo++;
        comboTimer = comboTimeWindow;

        // Multiplier hesapla (her 2 combo'da 1 artir, max'a kadar)
        comboMultiplier = Mathf.Min(1 + (currentCombo / 2), maxComboMultiplier);

        // Combo ile carpilmis skor
        int baseScore = 100;
        int comboScore = baseScore * comboMultiplier;
        AddScore(comboScore);

        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayEnemyDeath(position);

            // Combo efekti
            if (currentCombo >= 3)
            {
                ParticleManager.Instance.PlayComboEffect(position, currentCombo);
            }
        }

        // Screen shake - combo seviyesine gore
        if (CameraFollow.Instance != null)
        {
            if (currentCombo >= 5)
            {
                CameraFollow.Instance.ShakeOnCombo(currentCombo);
            }
            else
            {
                CameraFollow.Instance.ShakeOnEnemyKill();
            }
        }

        // Dusman olum sesi
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDeath();

        // SaveManager'a kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddEnemyKill();
        }

        // Combo UI guncelle
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCombo(currentCombo, comboMultiplier, 1f);

            // Buyuk combo'larda ekstra geri bildirim
            if (currentCombo >= 5)
            {
                UIManager.Instance.ShowComboText($"x{comboMultiplier} COMBO!", position);
            }
        }

        // Floating combo text
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowCombo(position + Vector3.up * 0.5f, currentCombo, comboMultiplier);
        }

        // Yuksek combo bildirimi
        if (currentCombo == 10 && NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowAchievement("10x COMBO!", "Muhtesem bir seri!");
        }
        else if (currentCombo == 20 && NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowAchievement("20x COMBO!", "Inanilmaz!");
        }
    }

    void ResetCombo()
    {
        currentCombo = 0;
        comboMultiplier = 1;
        comboTimer = 0f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCombo(0, 1, 0f);
        }
    }

    // Hasar alinca combo sifirlanir
    void OnPlayerDamaged()
    {
        ResetCombo();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        MobileOptimizer.SetGameplayFrameRate();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool IsGameOver()
    {
        return isGameOver || hasWon;
    }

    public int GetScore() { return score; }
    public int GetCoins() { return coins; }
    public int GetHealth() { return health; }
    public int GetCombo() { return currentCombo; }
    public int GetComboMultiplier() { return comboMultiplier; }

    // AdvancedHUD icin property'ler
    public float currentHealth => health;
    public int MaxHealth => maxHealth;

    #region Mobile Difficulty Scaling

    /// <summary>
    /// Mobil kolay mod parametrelerini uygula.
    /// SaveManager'dan ayari okur, mobil platformda ekstra can ve daha az hasar verir.
    /// </summary>
    void ApplyMobileDifficultyScaling()
    {
        bool isMobile = Application.isMobilePlatform;
        #if UNITY_EDITOR
        if (MobileControls.Instance != null && MobileControls.Instance.IsEnabled)
            isMobile = true;
        #endif

        if (!isMobile) return;

        // SaveManager'dan kolay mod ayarini al
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            isMobileEasyMode = SaveManager.Instance.Data.mobileEasyMode;
        }

        if (isMobileEasyMode)
        {
            maxHealth += easyModeExtraHealth; // +2 can
        }
    }

    /// <summary>
    /// Dusman hasarini mobil kolay moda gore ayarla.
    /// Kolay modda hasar %50 azalir (minimum 1).
    /// </summary>
    public int GetAdjustedDamage(int baseDamage)
    {
        if (isMobileEasyMode)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * easyModeDamageMultiplier));
        }
        return baseDamage;
    }

    /// <summary>
    /// Mobil kolay modu ac/kapa ve kaydet
    /// </summary>
    public void SetMobileEasyMode(bool enabled)
    {
        isMobileEasyMode = enabled;

        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.mobileEasyMode = enabled;
            SaveManager.Instance.SaveSettings();
        }
    }

    #endregion
}
