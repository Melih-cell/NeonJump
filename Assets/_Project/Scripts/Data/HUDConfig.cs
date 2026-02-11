using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// HUD ayarlarini tutan ScriptableObject.
/// Assets > Create > NeonJump > HUD Config ile olusturulabilir.
/// </summary>
[CreateAssetMenu(fileName = "HUDConfig", menuName = "NeonJump/HUD Config")]
public class HUDConfig : ScriptableObject
{
    [Header("Genel Ayarlar")]
    [Tooltip("HUD gorsel preset'i")]
    public HUDPreset preset = HUDPreset.Normal;

    [Range(0.5f, 1.5f)]
    [Tooltip("Genel HUD boyut carpani")]
    public float globalScale = 1f;

    [Range(0.3f, 1f)]
    [Tooltip("HUD seffafligi")]
    public float opacity = 1f;

    [Header("Element Gorunurlugu")]
    public bool showHealthBar = true;
    public bool showScorePanel = true;
    public bool showCoinCounter = true;
    public bool showWeaponPanel = true;
    public bool showSkillPanel = true;
    public bool showMinimap = true;
    public bool showComboDisplay = true;
    public bool showDamageIndicators = true;
    public bool showKillFeed = true;
    public bool showCrosshair = true;

    [Header("Minimap Ayarlari")]
    [Range(100f, 300f)]
    public float minimapSize = 150f;

    [Range(0.5f, 2f)]
    public float minimapZoom = 1f;

    [Tooltip("Minimap konumu")]
    public MinimapPosition minimapPosition = MinimapPosition.TopRight;

    public bool minimapRotateWithPlayer = false;

    [Header("Health Bar Ayarlari")]
    public HealthBarStyle healthBarStyle = HealthBarStyle.Bar;
    public bool showHealthNumbers = true;
    public bool lowHealthWarning = true;

    [Header("Combo Display Ayarlari")]
    [Range(1f, 5f)]
    public float comboDisplayDuration = 2f;
    public bool comboShakeEffect = true;

    [Header("Kill Feed Ayarlari")]
    [Range(3, 10)]
    public int maxKillFeedEntries = 5;

    [Range(2f, 10f)]
    public float killFeedDuration = 5f;

    [Header("Renk Sema")]
    public Color healthColor = new Color(0f, 1f, 0.5f, 1f);      // Neon yesil
    public Color healthLowColor = new Color(1f, 0.3f, 0.3f, 1f); // Kirmizi
    public Color scoreColor = new Color(1f, 1f, 0f, 1f);         // Sari
    public Color comboColor = new Color(1f, 0.5f, 0f, 1f);       // Turuncu
    public Color coinColor = new Color(1f, 0.84f, 0f, 1f);       // Altin

    [Header("Element Pozisyonlari")]
    public List<HUDElementPosition> elementPositions = new List<HUDElementPosition>();

    /// <summary>
    /// Varsayilan pozisyonlara sifirla
    /// </summary>
    public void ResetToDefaults()
    {
        preset = HUDPreset.Normal;
        globalScale = 1f;
        opacity = 1f;

        showHealthBar = true;
        showScorePanel = true;
        showCoinCounter = true;
        showWeaponPanel = true;
        showSkillPanel = true;
        showMinimap = true;
        showComboDisplay = true;
        showDamageIndicators = true;
        showKillFeed = true;
        showCrosshair = true;

        minimapSize = 150f;
        minimapZoom = 1f;
        minimapPosition = MinimapPosition.TopRight;
        minimapRotateWithPlayer = false;

        healthBarStyle = HealthBarStyle.Bar;
        showHealthNumbers = true;
        lowHealthWarning = true;

        comboDisplayDuration = 2f;
        comboShakeEffect = true;

        maxKillFeedEntries = 5;
        killFeedDuration = 5f;

        // Neon renk sema
        healthColor = new Color(0f, 1f, 0.5f, 1f);
        healthLowColor = new Color(1f, 0.3f, 0.3f, 1f);
        scoreColor = new Color(1f, 1f, 0f, 1f);
        comboColor = new Color(1f, 0.5f, 0f, 1f);
        coinColor = new Color(1f, 0.84f, 0f, 1f);

        elementPositions.Clear();
        InitializeDefaultPositions();
    }

    /// <summary>
    /// Varsayilan element pozisyonlarini olustur
    /// </summary>
    public void InitializeDefaultPositions()
    {
        elementPositions = new List<HUDElementPosition>
        {
            new HUDElementPosition
            {
                elementId = "health",
                anchorMin = new Vector2(0, 1),
                anchorMax = new Vector2(0, 1),
                pivot = new Vector2(0, 1),
                anchoredPosition = new Vector2(20, -20),
                sizeDelta = new Vector2(250, 60)
            },
            new HUDElementPosition
            {
                elementId = "score",
                anchorMin = new Vector2(0.5f, 1),
                anchorMax = new Vector2(0.5f, 1),
                pivot = new Vector2(0.5f, 1),
                anchoredPosition = new Vector2(0, -20),
                sizeDelta = new Vector2(200, 50)
            },
            new HUDElementPosition
            {
                elementId = "coins",
                anchorMin = new Vector2(1, 1),
                anchorMax = new Vector2(1, 1),
                pivot = new Vector2(1, 1),
                anchoredPosition = new Vector2(-20, -20),
                sizeDelta = new Vector2(150, 40)
            },
            new HUDElementPosition
            {
                elementId = "weapon",
                anchorMin = new Vector2(0, 0),
                anchorMax = new Vector2(0, 0),
                pivot = new Vector2(0, 0),
                anchoredPosition = new Vector2(20, 20),
                sizeDelta = new Vector2(200, 120)
            },
            new HUDElementPosition
            {
                elementId = "skills",
                anchorMin = new Vector2(1, 0),
                anchorMax = new Vector2(1, 0),
                pivot = new Vector2(1, 0),
                anchoredPosition = new Vector2(-20, 20),
                sizeDelta = new Vector2(180, 60)
            },
            new HUDElementPosition
            {
                elementId = "minimap",
                anchorMin = new Vector2(1, 1),
                anchorMax = new Vector2(1, 1),
                pivot = new Vector2(1, 1),
                anchoredPosition = new Vector2(-20, -80),
                sizeDelta = new Vector2(150, 150)
            },
            new HUDElementPosition
            {
                elementId = "combo",
                anchorMin = new Vector2(0.5f, 0.5f),
                anchorMax = new Vector2(0.5f, 0.5f),
                pivot = new Vector2(0.5f, 0.5f),
                anchoredPosition = new Vector2(0, 100),
                sizeDelta = new Vector2(200, 80)
            }
        };
    }

    /// <summary>
    /// Belirli bir element'in pozisyonunu al
    /// </summary>
    public HUDElementPosition GetElementPosition(string elementId)
    {
        foreach (var pos in elementPositions)
        {
            if (pos.elementId == elementId)
                return pos;
        }
        return null;
    }

    /// <summary>
    /// Element pozisyonunu guncelle
    /// </summary>
    public void SetElementPosition(string elementId, Vector2 anchoredPosition)
    {
        foreach (var pos in elementPositions)
        {
            if (pos.elementId == elementId)
            {
                pos.anchoredPosition = anchoredPosition;
                return;
            }
        }
    }

    /// <summary>
    /// Preset'e gore ayarlari uygula
    /// </summary>
    public void ApplyPreset(HUDPreset newPreset)
    {
        preset = newPreset;

        switch (newPreset)
        {
            case HUDPreset.Minimal:
                showMinimap = false;
                showKillFeed = false;
                showDamageIndicators = false;
                showSkillPanel = false;
                globalScale = 0.8f;
                opacity = 0.7f;
                break;

            case HUDPreset.Normal:
                showMinimap = true;
                showKillFeed = true;
                showDamageIndicators = true;
                showSkillPanel = true;
                globalScale = 1f;
                opacity = 1f;
                break;

            case HUDPreset.Full:
                showMinimap = true;
                showKillFeed = true;
                showDamageIndicators = true;
                showSkillPanel = true;
                showHealthNumbers = true;
                globalScale = 1.1f;
                opacity = 1f;
                minimapSize = 200f;
                break;

            case HUDPreset.Custom:
                // Ozel ayarlar - degisiklik yapma
                break;
        }
    }
}

/// <summary>
/// HUD preset turleri
/// </summary>
public enum HUDPreset
{
    Minimal = 0,  // Sadece temel bilgiler
    Normal = 1,   // Standart gorunum
    Full = 2,     // Tum bilgiler
    Custom = 3    // Ozellestirilmis
}

/// <summary>
/// Minimap pozisyon secenekleri
/// </summary>
public enum MinimapPosition
{
    TopLeft = 0,
    TopRight = 1,
    BottomLeft = 2,
    BottomRight = 3
}

/// <summary>
/// Health bar gosterim stilleri
/// </summary>
public enum HealthBarStyle
{
    Bar = 0,      // Klasik bar
    Hearts = 1,   // Kalp ikonlari
    Both = 2      // Ikisi birden
}

/// <summary>
/// Tek bir HUD element'inin pozisyon bilgisi
/// </summary>
[Serializable]
public class HUDElementPosition
{
    public string elementId;
    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 pivot;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public float localScale = 1f;
    public bool isVisible = true;
}
