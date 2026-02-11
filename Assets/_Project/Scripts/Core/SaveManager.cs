using UnityEngine;
using System;
using System.IO;

[Serializable]
public class SaveData
{
    // Oyuncu ilerleme
    public int highScore = 0;
    public int totalCoins = 0;
    public int totalEnemiesKilled = 0;
    public int totalDeaths = 0;
    public float totalPlayTime = 0f;
    public int gamesPlayed = 0;

    // Acilan ozellikler (Upgrades)
    public int maxHealthUpgrade = 0;      // 0-3 (her biri +1 can)
    public int speedUpgrade = 0;          // 0-3 (her biri +%10 hiz)
    public int dashUpgrade = 0;           // 0-2 (0: normal, 1: daha uzun, 2: cift dash)
    public int jumpUpgrade = 0;           // 0-2 (0: normal, 1: daha yuksek, 2: uclu ziplama)
    public int damageUpgrade = 0;         // 0-3 (her biri +%15 hasar)

    // Istatistikler
    public int maxComboReached = 0;
    public int bossesDefeated = 0;

    // Ayarlar
    public float masterVolume = 1f;
    public float musicVolume = 0.5f;
    public float sfxVolume = 0.7f;
    public bool fullscreen = true;
    public int qualityLevel = 2;
    public int resolutionIndex = -1; // -1 = native

    // Grafik ayarlari
    public float brightness = 1f;

    // Kontrol ayarlari
    public float aimSensitivity = 1f;
    public bool vibrationEnabled = true;

    // Erisilebilirlik ayarlari
    public int colorBlindMode = 0;
    public float uiScale = 1f;
    public bool screenShakeEnabled = true;

    // Dil
    public string language = "tr";

    // HUD ayarlari
    public string hudLayoutPreset = "normal";
    public bool hudMinimapEnabled = true;
    public float hudMinimapSize = 150f;

    // Crafting
    public string unlockedRecipes = "";

    // Level ilerleme
    public int[] levelStars = new int[20];
    public bool[] levelsUnlocked = new bool[20];

    // Son oturum
    public string lastPlayDate = "";
    public int currentLevel = 1;

    /// <summary>
    /// Level verilerini baslat (ilk level acik)
    /// </summary>
    public void InitializeLevelData()
    {
        if (levelStars == null || levelStars.Length < 20)
        {
            int[] oldStars = levelStars;
            levelStars = new int[20];
            if (oldStars != null)
            {
                for (int i = 0; i < oldStars.Length && i < 20; i++)
                    levelStars[i] = oldStars[i];
            }
        }

        if (levelsUnlocked == null || levelsUnlocked.Length < 20)
        {
            bool[] oldUnlocked = levelsUnlocked;
            levelsUnlocked = new bool[20];
            if (oldUnlocked != null)
            {
                for (int i = 0; i < oldUnlocked.Length && i < 20; i++)
                    levelsUnlocked[i] = oldUnlocked[i];
            }
        }

        // Ilk level her zaman acik
        levelsUnlocked[0] = true;
    }
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public SaveData Data { get; private set; }

    private string saveFilePath;
    private float sessionStartTime;
    private bool isDirty = false;

    // Events
    public static event Action<SaveData> OnDataLoaded;
    public static event Action<SaveData> OnDataSaved;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "neonjump_save.json");
        sessionStartTime = Time.realtimeSinceStartup;

        Load();

        // Son oturum tarihini guncelle
        Data.lastPlayDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Data.gamesPlayed++;
    }

    void OnApplicationQuit()
    {
        // Oturum suresini ekle
        Data.totalPlayTime += Time.realtimeSinceStartup - sessionStartTime;
        Save();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Mobilde arka plana gidince kaydet
            Data.totalPlayTime += Time.realtimeSinceStartup - sessionStartTime;
            sessionStartTime = Time.realtimeSinceStartup;
            Save();
        }
    }

    // === KAYDETME / YUKLEME ===

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(saveFilePath, json);
            isDirty = false;

            Debug.Log($"[SaveManager] Oyun kaydedildi: {saveFilePath}");
            OnDataSaved?.Invoke(Data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Kaydetme hatasi: {e.Message}");
        }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                Data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveManager] Oyun yuklendi: {saveFilePath}");
            }
            else
            {
                Data = new SaveData();
                Debug.Log("[SaveManager] Yeni kayit dosyasi olusturuldu");
            }

            // PlayerPrefs'ten eski verileri aktar (uyumluluk icin)
            MigrateFromPlayerPrefs();

            OnDataLoaded?.Invoke(Data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Yukleme hatasi: {e.Message}");
            Data = new SaveData();
        }
    }

    void MigrateFromPlayerPrefs()
    {
        // Eski PlayerPrefs verilerini aktar
        if (PlayerPrefs.HasKey("HighScore"))
        {
            int oldHighScore = PlayerPrefs.GetInt("HighScore", 0);
            if (oldHighScore > Data.highScore)
            {
                Data.highScore = oldHighScore;
            }
        }

        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            Data.masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            Data.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            Data.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        }
    }

    public void ResetProgress()
    {
        // Ayarlari koru, sadece ilerlemeyi sifirla
        float masterVol = Data.masterVolume;
        float musicVol = Data.musicVolume;
        float sfxVol = Data.sfxVolume;
        bool fs = Data.fullscreen;
        int quality = Data.qualityLevel;
        int res = Data.resolutionIndex;

        Data = new SaveData
        {
            masterVolume = masterVol,
            musicVolume = musicVol,
            sfxVolume = sfxVol,
            fullscreen = fs,
            qualityLevel = quality,
            resolutionIndex = res
        };

        Save();
        Debug.Log("[SaveManager] Ilerleme sifirlandi");
    }

    public void DeleteAllData()
    {
        Data = new SaveData();
        Save();

        // PlayerPrefs'i de temizle
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] Tum veriler silindi");
    }

    // === ILERLEME GUNCELLEME ===

    public void UpdateHighScore(int score)
    {
        if (score > Data.highScore)
        {
            Data.highScore = score;
            isDirty = true;
            Save(); // Yuksek skor hemen kaydedilsin
        }
    }

    public void AddCoins(int amount)
    {
        Data.totalCoins += amount;
        isDirty = true;
    }

    /// <summary>
    /// Coin harca (silah upgrade vs. için)
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (Data.totalCoins < amount)
        {
            Debug.Log($"[SaveManager] Yetersiz coin: {Data.totalCoins}/{amount}");
            return false;
        }

        Data.totalCoins -= amount;
        isDirty = true;
        Save();
        return true;
    }

    /// <summary>
    /// Mevcut toplam coin
    /// </summary>
    public int GetTotalCoins()
    {
        return Data.totalCoins;
    }

    public void AddEnemyKill()
    {
        Data.totalEnemiesKilled++;
        isDirty = true;
    }

    public void AddDeath()
    {
        Data.totalDeaths++;
        isDirty = true;
    }

    public void UpdateMaxCombo(int combo)
    {
        if (combo > Data.maxComboReached)
        {
            Data.maxComboReached = combo;
            isDirty = true;
        }
    }

    public void AddBossDefeat()
    {
        Data.bossesDefeated++;
        isDirty = true;
        Save();
    }

    // === UPGRADE SISTEMI ===

    public int GetUpgradeCost(string upgradeType, int currentLevel)
    {
        // Her seviye daha pahali
        int baseCost = upgradeType switch
        {
            "health" => 100,
            "speed" => 150,
            "dash" => 200,
            "jump" => 200,
            "damage" => 250,
            _ => 100
        };

        return baseCost * (currentLevel + 1);
    }

    public bool TryPurchaseUpgrade(string upgradeType)
    {
        int currentLevel = GetUpgradeLevel(upgradeType);
        int maxLevel = GetMaxUpgradeLevel(upgradeType);

        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[SaveManager] {upgradeType} zaten maksimum seviyede");
            return false;
        }

        int cost = GetUpgradeCost(upgradeType, currentLevel);

        if (Data.totalCoins < cost)
        {
            Debug.Log($"[SaveManager] Yetersiz coin: {Data.totalCoins}/{cost}");
            return false;
        }

        Data.totalCoins -= cost;

        switch (upgradeType)
        {
            case "health": Data.maxHealthUpgrade++; break;
            case "speed": Data.speedUpgrade++; break;
            case "dash": Data.dashUpgrade++; break;
            case "jump": Data.jumpUpgrade++; break;
            case "damage": Data.damageUpgrade++; break;
        }

        Save();
        Debug.Log($"[SaveManager] {upgradeType} yukseltildi: Seviye {currentLevel + 1}");
        return true;
    }

    public int GetUpgradeLevel(string upgradeType)
    {
        return upgradeType switch
        {
            "health" => Data.maxHealthUpgrade,
            "speed" => Data.speedUpgrade,
            "dash" => Data.dashUpgrade,
            "jump" => Data.jumpUpgrade,
            "damage" => Data.damageUpgrade,
            _ => 0
        };
    }

    public int GetMaxUpgradeLevel(string upgradeType)
    {
        return upgradeType switch
        {
            "health" => 3,
            "speed" => 3,
            "dash" => 2,
            "jump" => 2,
            "damage" => 3,
            _ => 3
        };
    }

    // === LEVEL ILERLEME ===

    /// <summary>
    /// Toplam kazanilan yildiz sayisini al
    /// </summary>
    public int GetTotalStars()
    {
        Data.InitializeLevelData();
        int total = 0;
        for (int i = 0; i < Data.levelStars.Length; i++)
        {
            total += Data.levelStars[i];
        }
        return total;
    }

    /// <summary>
    /// Belirtilen level'i ac
    /// </summary>
    public void UnlockLevel(int levelIndex)
    {
        Data.InitializeLevelData();
        if (levelIndex >= 0 && levelIndex < Data.levelsUnlocked.Length)
        {
            Data.levelsUnlocked[levelIndex] = true;
            isDirty = true;
        }
    }

    /// <summary>
    /// Level yildiz sayisini ayarla
    /// </summary>
    public void SetLevelStars(int levelIndex, int stars)
    {
        Data.InitializeLevelData();
        if (levelIndex >= 0 && levelIndex < Data.levelStars.Length)
        {
            Data.levelStars[levelIndex] = stars;
            isDirty = true;
        }
    }

    /// <summary>
    /// Level yildiz sayisini al
    /// </summary>
    public int GetLevelStars(int levelIndex)
    {
        Data.InitializeLevelData();
        if (levelIndex >= 0 && levelIndex < Data.levelStars.Length)
        {
            return Data.levelStars[levelIndex];
        }
        return 0;
    }

    // === AYARLAR ===

    public void SaveSettings()
    {
        Save();
    }

    public void SetMasterVolume(float value)
    {
        Data.masterVolume = value;
        isDirty = true;
    }

    public void SetMusicVolume(float value)
    {
        Data.musicVolume = value;
        isDirty = true;
    }

    public void SetSFXVolume(float value)
    {
        Data.sfxVolume = value;
        isDirty = true;
    }

    public void SetFullscreen(bool value)
    {
        Data.fullscreen = value;
        Screen.fullScreen = value;
        isDirty = true;
    }

    public void SetQualityLevel(int level)
    {
        Data.qualityLevel = level;
        QualitySettings.SetQualityLevel(level);
        isDirty = true;
    }

    public void SetResolution(int index)
    {
        Data.resolutionIndex = index;

        if (index >= 0 && index < Screen.resolutions.Length)
        {
            Resolution res = Screen.resolutions[index];
            Screen.SetResolution(res.width, res.height, Data.fullscreen);
        }

        isDirty = true;
    }

    public void SetHUDLayoutPreset(string preset)
    {
        Data.hudLayoutPreset = preset;
        isDirty = true;
    }

    public void SetAimSensitivity(float value)
    {
        Data.aimSensitivity = value;
        isDirty = true;
    }

    public void SetVibration(bool value)
    {
        Data.vibrationEnabled = value;
        isDirty = true;
    }

    public void SetLanguage(string langCode)
    {
        Data.language = langCode;
        isDirty = true;
    }

    public void SetBrightness(float value)
    {
        Data.brightness = value;
        isDirty = true;
    }

    public void SetColorBlindMode(int mode)
    {
        Data.colorBlindMode = mode;
        isDirty = true;
    }

    public void SetUIScale(float scale)
    {
        Data.uiScale = scale;
        isDirty = true;
    }

    public void SetScreenShake(bool enabled)
    {
        Data.screenShakeEnabled = enabled;
        isDirty = true;
    }

    // === ISTATISTIKLER ===

    public string GetFormattedPlayTime()
    {
        float totalSeconds = Data.totalPlayTime + (Time.realtimeSinceStartup - sessionStartTime);
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);

        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}s {time.Minutes}d {time.Seconds}sn";
        }
        else if (time.TotalMinutes >= 1)
        {
            return $"{time.Minutes}d {time.Seconds}sn";
        }
        else
        {
            return $"{time.Seconds}sn";
        }
    }

    public float GetKillDeathRatio()
    {
        if (Data.totalDeaths == 0) return Data.totalEnemiesKilled;
        return (float)Data.totalEnemiesKilled / Data.totalDeaths;
    }

    // === YARDIMCI ===

    void Update()
    {
        // Her 60 saniyede otomatik kaydet
        if (isDirty && Time.frameCount % 3600 == 0)
        {
            Save();
        }
    }
}
