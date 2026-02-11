using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Lokalizasyon verilerini tutan ScriptableObject.
/// Assets > Create > NeonJump > Localization Data ile olusturulabilir.
/// </summary>
[CreateAssetMenu(fileName = "LocalizationData", menuName = "NeonJump/Localization Data")]
public class LocalizationData : ScriptableObject
{
    [Header("Dil Bilgisi")]
    public string languageCode = "tr";
    public string languageName = "Turkce";
    public string languageNameNative = "Turkce";

    [Header("Ceviriler")]
    public List<LocalizationEntry> entries = new List<LocalizationEntry>();

    // Hizli erisim icin cache
    private Dictionary<string, string> _cache;

    /// <summary>
    /// Belirtilen anahtar icin ceviriyi al
    /// </summary>
    public string Get(string key)
    {
        if (_cache == null)
            BuildCache();

        if (_cache.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[Localization] Anahtar bulunamadi: {key}");
        return key; // Bulunamazsa anahtar dondur
    }

    /// <summary>
    /// Parametreli ceviri al (format: "Merhaba {0}!")
    /// </summary>
    public string GetFormatted(string key, params object[] args)
    {
        string text = Get(key);
        try
        {
            return string.Format(text, args);
        }
        catch
        {
            return text;
        }
    }

    /// <summary>
    /// Cache'i yeniden olustur
    /// </summary>
    public void BuildCache()
    {
        _cache = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
            {
                _cache[entry.key] = entry.value;
            }
        }
    }

    /// <summary>
    /// Cache'i temizle (dil degistiginde cagrilir)
    /// </summary>
    public void ClearCache()
    {
        _cache = null;
    }

    /// <summary>
    /// Yeni ceviri ekle veya mevcut olani guncelle
    /// </summary>
    public void SetEntry(string key, string value)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].key == key)
            {
                entries[i].value = value;
                if (_cache != null) _cache[key] = value;
                return;
            }
        }

        entries.Add(new LocalizationEntry { key = key, value = value });
        if (_cache != null) _cache[key] = value;
    }

    /// <summary>
    /// Varsayilan Turkce cevirileri yukle
    /// </summary>
    public void LoadDefaultTurkish()
    {
        languageCode = "tr";
        languageName = "Turkish";
        languageNameNative = "Turkce";

        entries.Clear();

        // Ana Menu
        AddEntry("menu_play", "OYNA");
        AddEntry("menu_levels", "LEVELLAR");
        AddEntry("menu_settings", "AYARLAR");
        AddEntry("menu_statistics", "ISTATISTIKLER");
        AddEntry("menu_quit", "CIKIS");
        AddEntry("menu_back", "GERI");
        AddEntry("menu_continue", "DEVAM ET");
        AddEntry("menu_restart", "TEKRAR BASLAT");
        AddEntry("menu_main_menu", "ANA MENU");

        // Ayarlar
        AddEntry("settings_audio", "SES");
        AddEntry("settings_graphics", "GRAFIK");
        AddEntry("settings_controls", "KONTROLLER");
        AddEntry("settings_accessibility", "ERISILEBILIRLIK");
        AddEntry("settings_language", "DIL");
        AddEntry("settings_master_volume", "Ana Ses");
        AddEntry("settings_music_volume", "Muzik");
        AddEntry("settings_sfx_volume", "Efektler");
        AddEntry("settings_fullscreen", "Tam Ekran");
        AddEntry("settings_resolution", "Cozunurluk");
        AddEntry("settings_quality", "Kalite");
        AddEntry("settings_brightness", "Parlaklik");
        AddEntry("settings_colorblind", "Renk Korlugu Modu");
        AddEntry("settings_ui_scale", "UI Boyutu");
        AddEntry("settings_screen_shake", "Ekran Titremesi");
        AddEntry("settings_vibration", "Titresim");
        AddEntry("settings_sensitivity", "Hassasiyet");
        AddEntry("settings_reset", "Varsayilana Sifirla");
        AddEntry("settings_apply", "UYGULA");

        // Kalite seviyeleri
        AddEntry("quality_very_low", "Cok Dusuk");
        AddEntry("quality_low", "Dusuk");
        AddEntry("quality_medium", "Orta");
        AddEntry("quality_high", "Yuksek");
        AddEntry("quality_very_high", "Cok Yuksek");
        AddEntry("quality_ultra", "Ultra");

        // Renk korlugu modlari
        AddEntry("colorblind_none", "Normal");
        AddEntry("colorblind_deuteranopia", "Deuteranopia (Yesil-Kirmizi)");
        AddEntry("colorblind_protanopia", "Protanopia (Kirmizi)");
        AddEntry("colorblind_tritanopia", "Tritanopia (Mavi-Sari)");

        // HUD
        AddEntry("hud_health", "CAN");
        AddEntry("hud_score", "SKOR");
        AddEntry("hud_coins", "PARA");
        AddEntry("hud_combo", "KOMBO");
        AddEntry("hud_ammo", "MERMI");
        AddEntry("hud_reload", "SARJ");

        // Oyun durumu
        AddEntry("game_paused", "OYUN DURDURULDU");
        AddEntry("game_over", "OYUN BITTI");
        AddEntry("game_victory", "TEBRIKLER!");
        AddEntry("game_new_record", "YENI REKOR!");
        AddEntry("game_level_complete", "LEVEL TAMAMLANDI");

        // Istatistikler
        AddEntry("stats_high_score", "En Yuksek Skor");
        AddEntry("stats_total_coins", "Toplam Para");
        AddEntry("stats_enemies_killed", "Oldurulen Dusman");
        AddEntry("stats_deaths", "Olum Sayisi");
        AddEntry("stats_play_time", "Oyun Suresi");
        AddEntry("stats_max_combo", "En Yuksek Kombo");
        AddEntry("stats_bosses_defeated", "Yenilen Boss");
        AddEntry("stats_kd_ratio", "K/D Orani");
        AddEntry("stats_games_played", "Oynanan Oyun");

        // Level secim
        AddEntry("level_locked", "KILITLI");
        AddEntry("level_stars_required", "{0} yildiz gerekli");

        // Power-ups
        AddEntry("powerup_double_jump", "Cift Ziplama");
        AddEntry("powerup_shield", "Kalkan");
        AddEntry("powerup_invincibility", "Yenilmezlik");
        AddEntry("powerup_speed", "Hiz Artisi");

        // Genel
        AddEntry("yes", "EVET");
        AddEntry("no", "HAYIR");
        AddEntry("confirm", "ONAYLA");
        AddEntry("cancel", "IPTAL");
        AddEntry("loading", "Yukleniyor...");
        AddEntry("saving", "Kaydediliyor...");

        BuildCache();
    }

    /// <summary>
    /// Varsayilan Ingilizce cevirileri yukle
    /// </summary>
    public void LoadDefaultEnglish()
    {
        languageCode = "en";
        languageName = "English";
        languageNameNative = "English";

        entries.Clear();

        // Main Menu
        AddEntry("menu_play", "PLAY");
        AddEntry("menu_levels", "LEVELS");
        AddEntry("menu_settings", "SETTINGS");
        AddEntry("menu_statistics", "STATISTICS");
        AddEntry("menu_quit", "QUIT");
        AddEntry("menu_back", "BACK");
        AddEntry("menu_continue", "CONTINUE");
        AddEntry("menu_restart", "RESTART");
        AddEntry("menu_main_menu", "MAIN MENU");

        // Settings
        AddEntry("settings_audio", "AUDIO");
        AddEntry("settings_graphics", "GRAPHICS");
        AddEntry("settings_controls", "CONTROLS");
        AddEntry("settings_accessibility", "ACCESSIBILITY");
        AddEntry("settings_language", "LANGUAGE");
        AddEntry("settings_master_volume", "Master Volume");
        AddEntry("settings_music_volume", "Music");
        AddEntry("settings_sfx_volume", "Sound Effects");
        AddEntry("settings_fullscreen", "Fullscreen");
        AddEntry("settings_resolution", "Resolution");
        AddEntry("settings_quality", "Quality");
        AddEntry("settings_brightness", "Brightness");
        AddEntry("settings_colorblind", "Colorblind Mode");
        AddEntry("settings_ui_scale", "UI Scale");
        AddEntry("settings_screen_shake", "Screen Shake");
        AddEntry("settings_vibration", "Vibration");
        AddEntry("settings_sensitivity", "Sensitivity");
        AddEntry("settings_reset", "Reset to Default");
        AddEntry("settings_apply", "APPLY");

        // Quality levels
        AddEntry("quality_very_low", "Very Low");
        AddEntry("quality_low", "Low");
        AddEntry("quality_medium", "Medium");
        AddEntry("quality_high", "High");
        AddEntry("quality_very_high", "Very High");
        AddEntry("quality_ultra", "Ultra");

        // Colorblind modes
        AddEntry("colorblind_none", "Normal");
        AddEntry("colorblind_deuteranopia", "Deuteranopia (Green-Red)");
        AddEntry("colorblind_protanopia", "Protanopia (Red)");
        AddEntry("colorblind_tritanopia", "Tritanopia (Blue-Yellow)");

        // HUD
        AddEntry("hud_health", "HEALTH");
        AddEntry("hud_score", "SCORE");
        AddEntry("hud_coins", "COINS");
        AddEntry("hud_combo", "COMBO");
        AddEntry("hud_ammo", "AMMO");
        AddEntry("hud_reload", "RELOAD");

        // Game state
        AddEntry("game_paused", "PAUSED");
        AddEntry("game_over", "GAME OVER");
        AddEntry("game_victory", "CONGRATULATIONS!");
        AddEntry("game_new_record", "NEW RECORD!");
        AddEntry("game_level_complete", "LEVEL COMPLETE");

        // Statistics
        AddEntry("stats_high_score", "High Score");
        AddEntry("stats_total_coins", "Total Coins");
        AddEntry("stats_enemies_killed", "Enemies Killed");
        AddEntry("stats_deaths", "Deaths");
        AddEntry("stats_play_time", "Play Time");
        AddEntry("stats_max_combo", "Max Combo");
        AddEntry("stats_bosses_defeated", "Bosses Defeated");
        AddEntry("stats_kd_ratio", "K/D Ratio");
        AddEntry("stats_games_played", "Games Played");

        // Level select
        AddEntry("level_locked", "LOCKED");
        AddEntry("level_stars_required", "{0} stars required");

        // Power-ups
        AddEntry("powerup_double_jump", "Double Jump");
        AddEntry("powerup_shield", "Shield");
        AddEntry("powerup_invincibility", "Invincibility");
        AddEntry("powerup_speed", "Speed Boost");

        // General
        AddEntry("yes", "YES");
        AddEntry("no", "NO");
        AddEntry("confirm", "CONFIRM");
        AddEntry("cancel", "CANCEL");
        AddEntry("loading", "Loading...");
        AddEntry("saving", "Saving...");

        BuildCache();
    }

    private void AddEntry(string key, string value)
    {
        entries.Add(new LocalizationEntry { key = key, value = value });
    }
}

/// <summary>
/// Tek bir lokalizasyon girdisi
/// </summary>
[Serializable]
public class LocalizationEntry
{
    [Tooltip("Benzersiz anahtar (orn: menu_play)")]
    public string key;

    [TextArea(1, 3)]
    [Tooltip("Cevrilen metin")]
    public string value;
}
