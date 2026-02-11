using UnityEngine;
using System;

/// <summary>
/// Oyun ici istatistik toplama sistemi.
/// Tek bir oturumdaki tum oyun verilerini takip eder.
/// </summary>
public class InGameStatistics : MonoBehaviour
{
    public static InGameStatistics Instance { get; private set; }

    [Header("Zaman Istatistikleri")]
    public float playTime;
    public float pausedTime;
    private float sessionStartTime;
    private float lastPauseTime;
    private bool isPaused;

    [Header("Savaş Istatistikleri")]
    public int enemiesKilled;
    public int bossesKilled;
    public int damageTaken;
    public int damageDealt;
    public int shotsFired;
    public int shotsHit;
    public int criticalHits;
    public int deathCount;

    [Header("Kombo Istatistikleri")]
    public int maxCombo;
    public int totalComboKills;

    [Header("Hareket Istatistikleri")]
    public int jumpsUsed;
    public int dashesUsed;
    public int wallJumpsUsed;
    public float distanceTraveled;
    private Vector3 lastPosition;

    [Header("Koleksiyon Istatistikleri")]
    public int coinsCollected;
    public int powerUpsCollected;
    public int secretsFound;
    public int checkpointsReached;

    [Header("Silah Istatistikleri")]
    public int weaponsPickedUp;
    public int reloadsPerformed;
    public int meleeKills;
    public int rangedKills;

    [Header("Diger")]
    public int scoreGained;
    public int livesLost;

    // Events
    public static event Action<InGameStatistics> OnStatisticsUpdated;

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

    void Start()
    {
        ResetStatistics();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!isPaused)
        {
            playTime = Time.realtimeSinceStartup - sessionStartTime - pausedTime;
        }

        // Mesafe takibi
        if (lastPosition != Vector3.zero)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float dist = Vector3.Distance(player.transform.position, lastPosition);
                if (dist < 10f) // Teleport kontrolu
                {
                    distanceTraveled += dist;
                }
                lastPosition = player.transform.position;
            }
        }
        else
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                lastPosition = player.transform.position;
            }
        }
    }

    void SubscribeToEvents()
    {
        GameEvents.OnEnemyKilled += OnEnemyKilled;
        GameEvents.OnBossDefeated += OnBossDefeated;
        GameEvents.OnPlayerDamaged += OnPlayerDamaged;
        GameEvents.OnPlayerDied += OnPlayerDied;
        GameEvents.OnComboChanged += OnComboChanged;
        GameEvents.OnCoinCollected += OnCoinCollected;
        GameEvents.OnPowerUpCollected += OnPowerUpCollected;
        GameEvents.OnCheckpointReached += OnCheckpointReached;
        GameEvents.OnScoreChanged += OnScoreChanged;
        GameEvents.OnGamePaused += OnGamePaused;
        GameEvents.OnGameResumed += OnGameResumed;
        GameEvents.OnSkillUsed += OnSkillUsed;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
        GameEvents.OnBossDefeated -= OnBossDefeated;
        GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
        GameEvents.OnPlayerDied -= OnPlayerDied;
        GameEvents.OnComboChanged -= OnComboChanged;
        GameEvents.OnCoinCollected -= OnCoinCollected;
        GameEvents.OnPowerUpCollected -= OnPowerUpCollected;
        GameEvents.OnCheckpointReached -= OnCheckpointReached;
        GameEvents.OnScoreChanged -= OnScoreChanged;
        GameEvents.OnGamePaused -= OnGamePaused;
        GameEvents.OnGameResumed -= OnGameResumed;
        GameEvents.OnSkillUsed -= OnSkillUsed;
    }

    // === EVENT HANDLERS ===

    void OnEnemyKilled(string name, Vector3 position, int points)
    {
        enemiesKilled++;
        totalComboKills++;
        NotifyUpdate();
    }

    void OnBossDefeated(string bossName)
    {
        bossesKilled++;
        NotifyUpdate();
    }

    void OnPlayerDamaged(float damage, Vector2 direction)
    {
        damageTaken += Mathf.RoundToInt(damage);
        NotifyUpdate();
    }

    void OnPlayerDied()
    {
        deathCount++;
        livesLost++;
        NotifyUpdate();
    }

    void OnComboChanged(int combo)
    {
        if (combo > maxCombo)
        {
            maxCombo = combo;
            NotifyUpdate();
        }
    }

    void OnCoinCollected(int amount, int total)
    {
        coinsCollected += amount;
        NotifyUpdate();
    }

    void OnPowerUpCollected(string type)
    {
        powerUpsCollected++;
        NotifyUpdate();
    }

    void OnCheckpointReached()
    {
        checkpointsReached++;
        NotifyUpdate();
    }

    void OnScoreChanged(int score)
    {
        scoreGained = score;
    }

    void OnGamePaused()
    {
        isPaused = true;
        lastPauseTime = Time.realtimeSinceStartup;
    }

    void OnGameResumed()
    {
        isPaused = false;
        pausedTime += Time.realtimeSinceStartup - lastPauseTime;
    }

    void OnSkillUsed(string skillName)
    {
        switch (skillName.ToLower())
        {
            case "jump":
            case "ziplama":
                jumpsUsed++;
                break;
            case "dash":
                dashesUsed++;
                break;
            case "walljump":
            case "duvar_ziplama":
                wallJumpsUsed++;
                break;
        }
    }

    // === PUBLIC METODLAR ===

    /// <summary>
    /// Tum istatistikleri sifirla
    /// </summary>
    public void ResetStatistics()
    {
        sessionStartTime = Time.realtimeSinceStartup;
        playTime = 0f;
        pausedTime = 0f;
        isPaused = false;

        enemiesKilled = 0;
        bossesKilled = 0;
        damageTaken = 0;
        damageDealt = 0;
        shotsFired = 0;
        shotsHit = 0;
        criticalHits = 0;
        deathCount = 0;

        maxCombo = 0;
        totalComboKills = 0;

        jumpsUsed = 0;
        dashesUsed = 0;
        wallJumpsUsed = 0;
        distanceTraveled = 0f;
        lastPosition = Vector3.zero;

        coinsCollected = 0;
        powerUpsCollected = 0;
        secretsFound = 0;
        checkpointsReached = 0;

        weaponsPickedUp = 0;
        reloadsPerformed = 0;
        meleeKills = 0;
        rangedKills = 0;

        scoreGained = 0;
        livesLost = 0;

        Debug.Log("[InGameStatistics] Istatistikler sifirlandi");
    }

    /// <summary>
    /// Atis kaydet
    /// </summary>
    public void RecordShot(bool hit, bool critical = false)
    {
        shotsFired++;
        if (hit)
        {
            shotsHit++;
            if (critical)
                criticalHits++;
        }
    }

    /// <summary>
    /// Verilen hasar kaydet
    /// </summary>
    public void RecordDamageDealt(int damage)
    {
        damageDealt += damage;
    }

    /// <summary>
    /// Silah aldi
    /// </summary>
    public void RecordWeaponPickup()
    {
        weaponsPickedUp++;
    }

    /// <summary>
    /// Sarj yaptı
    /// </summary>
    public void RecordReload()
    {
        reloadsPerformed++;
    }

    /// <summary>
    /// Gizli alan bulundu
    /// </summary>
    public void RecordSecretFound()
    {
        secretsFound++;
        NotifyUpdate();
    }

    /// <summary>
    /// Melee/Ranged kill ayirt et
    /// </summary>
    public void RecordKillType(bool isMelee)
    {
        if (isMelee)
            meleeKills++;
        else
            rangedKills++;
    }

    /// <summary>
    /// Istatistik ozetini al
    /// </summary>
    public StatisticsSummary GetSummary()
    {
        return new StatisticsSummary
        {
            playTime = playTime,
            score = scoreGained,
            enemiesKilled = enemiesKilled,
            bossesKilled = bossesKilled,
            maxCombo = maxCombo,
            deathCount = deathCount,
            coinsCollected = coinsCollected,
            damageTaken = damageTaken,
            damageDealt = damageDealt,
            accuracy = shotsFired > 0 ? (float)shotsHit / shotsFired : 0f,
            distanceTraveled = distanceTraveled
        };
    }

    /// <summary>
    /// Formatli oyun suresi
    /// </summary>
    public string GetFormattedPlayTime()
    {
        TimeSpan time = TimeSpan.FromSeconds(playTime);

        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        else
        {
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }

    /// <summary>
    /// Accuracy yuzdesi
    /// </summary>
    public float GetAccuracy()
    {
        if (shotsFired == 0) return 0f;
        return (float)shotsHit / shotsFired * 100f;
    }

    /// <summary>
    /// K/D orani
    /// </summary>
    public float GetKDRatio()
    {
        if (deathCount == 0) return enemiesKilled;
        return (float)enemiesKilled / deathCount;
    }

    /// <summary>
    /// Global istatistiklere kaydet
    /// </summary>
    public void SaveToGlobalStats()
    {
        if (SaveManager.Instance == null) return;

        // En yuksek skor
        SaveManager.Instance.UpdateHighScore(scoreGained);

        // Toplam istatistikler
        SaveManager.Instance.Data.totalEnemiesKilled += enemiesKilled;
        SaveManager.Instance.Data.totalCoins += coinsCollected;
        SaveManager.Instance.Data.totalDeaths += deathCount;
        SaveManager.Instance.UpdateMaxCombo(maxCombo);

        if (bossesKilled > 0)
        {
            SaveManager.Instance.Data.bossesDefeated += bossesKilled;
        }

        SaveManager.Instance.Save();

        Debug.Log("[InGameStatistics] Global istatistiklere kaydedildi");
    }

    void NotifyUpdate()
    {
        OnStatisticsUpdated?.Invoke(this);
    }
}

/// <summary>
/// Istatistik ozeti
/// </summary>
[Serializable]
public struct StatisticsSummary
{
    public float playTime;
    public int score;
    public int enemiesKilled;
    public int bossesKilled;
    public int maxCombo;
    public int deathCount;
    public int coinsCollected;
    public int damageTaken;
    public int damageDealt;
    public float accuracy;
    public float distanceTraveled;
}
