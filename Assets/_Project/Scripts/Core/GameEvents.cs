using UnityEngine;
using System;

/// <summary>
/// Merkezi event sistemi - Tum oyun olaylari burada tanimlanir.
/// Observer pattern ile gevset baglanti saglar.
/// </summary>
public static class GameEvents
{
    // === OYUNCU OLAYLARI ===

    /// <summary>Skor degistiginde (yeni skor)</summary>
    public static event Action<int> OnScoreChanged;

    /// <summary>Saglik degistiginde (mevcut, maksimum)</summary>
    public static event Action<float, float> OnHealthChanged;

    /// <summary>Oyuncu oldugunde</summary>
    public static event Action OnPlayerDied;

    /// <summary>Oyuncu hasar aldiginda (hasar miktari, yon)</summary>
    public static event Action<float, Vector2> OnPlayerDamaged;

    /// <summary>Oyuncu iyilestigi zaman (iyilesme miktari)</summary>
    public static event Action<float> OnPlayerHealed;

    // === COMBO SISTEMI ===

    /// <summary>Combo degistiginde (combo sayisi)</summary>
    public static event Action<int> OnComboChanged;

    /// <summary>Combo bittiginde</summary>
    public static event Action OnComboEnded;

    /// <summary>Combo carpani degistiginde (carpan)</summary>
    public static event Action<float> OnComboMultiplierChanged;

    // === PARA VE ESYA ===

    /// <summary>Coin toplandiginda (toplanan miktar, toplam)</summary>
    public static event Action<int, int> OnCoinCollected;

    /// <summary>Power-up alindiginda (power-up tipi)</summary>
    public static event Action<string> OnPowerUpCollected;

    /// <summary>Power-up bittiginde (power-up tipi)</summary>
    public static event Action<string> OnPowerUpExpired;

    // === SILAH SISTEMI ===

    /// <summary>Silah degistiginde (silah adi, slot)</summary>
    public static event Action<string, int> OnWeaponChanged;

    /// <summary>Mermi degistiginde (mevcut, maksimum)</summary>
    public static event Action<int, int> OnAmmoChanged;

    /// <summary>Sarj basladiginda</summary>
    public static event Action OnReloadStarted;

    /// <summary>Sarj bittiginde</summary>
    public static event Action OnReloadFinished;

    // === DUSMAN OLAYLARI ===

    /// <summary>Dusman oldugunde (dusman adi, pozisyon, puan)</summary>
    public static event Action<string, Vector3, int> OnEnemyKilled;

    /// <summary>Boss gorundiginde (boss adi)</summary>
    public static event Action<string> OnBossAppeared;

    /// <summary>Boss yenildiginde (boss adi)</summary>
    public static event Action<string> OnBossDefeated;

    /// <summary>Boss sagligi degistiginde (mevcut, maksimum)</summary>
    public static event Action<float, float> OnBossHealthChanged;

    // === OYUN DURUMU ===

    /// <summary>Oyun basladiginda</summary>
    public static event Action OnGameStarted;

    /// <summary>Oyun duraklatildiginda</summary>
    public static event Action OnGamePaused;

    /// <summary>Oyun devam ettiginde</summary>
    public static event Action OnGameResumed;

    /// <summary>Oyun bittiginde (kazandi mi)</summary>
    public static event Action<bool> OnGameEnded;

    /// <summary>Level tamamlandiginda (level no, yildiz sayisi)</summary>
    public static event Action<int, int> OnLevelCompleted;

    /// <summary>Checkpoint'e ulasildiginda</summary>
    public static event Action OnCheckpointReached;

    // === AYARLAR ===

    /// <summary>Dil degistiginde (yeni dil kodu)</summary>
    public static event Action<string> OnLanguageChanged;

    /// <summary>Herhangi bir ayar degistiginde</summary>
    public static event Action OnSettingsChanged;

    /// <summary>HUD ayarlari degistiginde</summary>
    public static event Action OnHUDSettingsChanged;

    // === SKILL/YETENEK ===

    /// <summary>Skill kullanildiginda (skill adi)</summary>
    public static event Action<string> OnSkillUsed;

    /// <summary>Skill hazir oldugunda (skill adi)</summary>
    public static event Action<string> OnSkillReady;

    /// <summary>Skill cooldown guncellendinde (skill adi, kalan sure, toplam sure)</summary>
    public static event Action<string, float, float> OnSkillCooldownUpdate;

    // === RAISE METODLARI ===

    public static void RaiseScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void RaiseHealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);
    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaisePlayerDamaged(float damage, Vector2 direction) => OnPlayerDamaged?.Invoke(damage, direction);
    public static void RaisePlayerHealed(float amount) => OnPlayerHealed?.Invoke(amount);

    public static void RaiseComboChanged(int combo) => OnComboChanged?.Invoke(combo);
    public static void RaiseComboEnded() => OnComboEnded?.Invoke();
    public static void RaiseComboMultiplierChanged(float multiplier) => OnComboMultiplierChanged?.Invoke(multiplier);

    public static void RaiseCoinCollected(int amount, int total) => OnCoinCollected?.Invoke(amount, total);
    public static void RaisePowerUpCollected(string type) => OnPowerUpCollected?.Invoke(type);
    public static void RaisePowerUpExpired(string type) => OnPowerUpExpired?.Invoke(type);

    public static void RaiseWeaponChanged(string weaponName, int slot) => OnWeaponChanged?.Invoke(weaponName, slot);
    public static void RaiseAmmoChanged(int current, int max) => OnAmmoChanged?.Invoke(current, max);
    public static void RaiseReloadStarted() => OnReloadStarted?.Invoke();
    public static void RaiseReloadFinished() => OnReloadFinished?.Invoke();

    public static void RaiseEnemyKilled(string name, Vector3 position, int points) => OnEnemyKilled?.Invoke(name, position, points);
    public static void RaiseBossAppeared(string bossName) => OnBossAppeared?.Invoke(bossName);
    public static void RaiseBossDefeated(string bossName) => OnBossDefeated?.Invoke(bossName);
    public static void RaiseBossHealthChanged(float current, float max) => OnBossHealthChanged?.Invoke(current, max);

    public static void RaiseGameStarted() => OnGameStarted?.Invoke();
    public static void RaiseGamePaused() => OnGamePaused?.Invoke();
    public static void RaiseGameResumed() => OnGameResumed?.Invoke();
    public static void RaiseGameEnded(bool won) => OnGameEnded?.Invoke(won);
    public static void RaiseLevelCompleted(int levelNo, int stars) => OnLevelCompleted?.Invoke(levelNo, stars);
    public static void RaiseCheckpointReached() => OnCheckpointReached?.Invoke();

    public static void RaiseLanguageChanged(string langCode) => OnLanguageChanged?.Invoke(langCode);
    public static void RaiseSettingsChanged() => OnSettingsChanged?.Invoke();
    public static void RaiseHUDSettingsChanged() => OnHUDSettingsChanged?.Invoke();

    public static void RaiseSkillUsed(string skillName) => OnSkillUsed?.Invoke(skillName);
    public static void RaiseSkillReady(string skillName) => OnSkillReady?.Invoke(skillName);
    public static void RaiseSkillCooldownUpdate(string skillName, float remaining, float total) => OnSkillCooldownUpdate?.Invoke(skillName, remaining, total);

    /// <summary>
    /// Tum event'lerin aboneligini temizle (sahne gecislerinde cagrilabilir)
    /// </summary>
    public static void ClearAllListeners()
    {
        OnScoreChanged = null;
        OnHealthChanged = null;
        OnPlayerDied = null;
        OnPlayerDamaged = null;
        OnPlayerHealed = null;
        OnComboChanged = null;
        OnComboEnded = null;
        OnComboMultiplierChanged = null;
        OnCoinCollected = null;
        OnPowerUpCollected = null;
        OnPowerUpExpired = null;
        OnWeaponChanged = null;
        OnAmmoChanged = null;
        OnReloadStarted = null;
        OnReloadFinished = null;
        OnEnemyKilled = null;
        OnBossAppeared = null;
        OnBossDefeated = null;
        OnBossHealthChanged = null;
        OnGameStarted = null;
        OnGamePaused = null;
        OnGameResumed = null;
        OnGameEnded = null;
        OnLevelCompleted = null;
        OnCheckpointReached = null;
        OnLanguageChanged = null;
        OnSettingsChanged = null;
        OnHUDSettingsChanged = null;
        OnSkillUsed = null;
        OnSkillReady = null;
        OnSkillCooldownUpdate = null;
    }
}
