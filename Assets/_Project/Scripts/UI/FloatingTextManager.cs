using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Floating damage numbers, score popups ve diger ekran ustu yazilar
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Settings")]
    public int maxFloatingTexts = 20;
    public float defaultDuration = 1f;
    public float defaultRiseSpeed = 100f;

    private Canvas canvas;
    private Queue<GameObject> textPool = new Queue<GameObject>();
    private HashSet<GameObject> activeTexts = new HashSet<GameObject>();
    private bool isMobile;
    private float mobileFontScale = 1f;

    void Awake()
    {
        Instance = this;

        isMobile = Application.isMobilePlatform ||
                   UnityEngine.InputSystem.Touchscreen.current != null;
        if (isMobile)
        {
            float dpiScale = Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f);
            mobileFontScale = dpiScale;
        }

        SetupCanvas();
        CreatePool();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void SetupCanvas()
    {
        // Floating text icin ozel canvas
        GameObject canvasObj = new GameObject("FloatingTextCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150; // Diger UI'larin ustunde

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = isMobile ? new Vector2(1280, 720) : new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
    }

    void CreatePool()
    {
        for (int i = 0; i < maxFloatingTexts; i++)
        {
            GameObject textObj = CreateTextObject();
            textObj.SetActive(false);
            textPool.Enqueue(textObj);
        }
    }

    GameObject CreateTextObject()
    {
        GameObject obj = new GameObject("FloatingText");
        obj.transform.SetParent(canvas.transform, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        float baseFontSize = isMobile ? Mathf.Max(32 * mobileFontScale, 18f) : 32;
        tmp.fontSize = baseFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        // Outline efekti (mobilde daha kalin)
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        float outlineSize = isMobile ? 3f : 2f;
        outline.effectDistance = new Vector2(outlineSize, -outlineSize);

        return obj;
    }

    GameObject GetFromPool()
    {
        if (textPool.Count > 0)
        {
            return textPool.Dequeue();
        }

        // Pool bos, yeni olustur
        return CreateTextObject();
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        activeTexts.Remove(obj);
        textPool.Enqueue(obj);
    }

    // === PUBLIC METODLAR ===

    /// <summary>
    /// Hasar sayisi goster (kirmizi, asagi kayar)
    /// </summary>
    public void ShowDamage(Vector3 worldPos, int damage, bool isCritical = false)
    {
        Color color = isCritical ? new Color(1f, 0.8f, 0f) : Color.red;
        float scale = isCritical ? 1.5f : 1f;
        string text = isCritical ? $"{damage}!" : damage.ToString();

        Show(worldPos, text, color, scale, FloatingTextStyle.Damage);
    }

    /// <summary>
    /// Skor goster (sari, yukari kayar)
    /// </summary>
    public void ShowScore(Vector3 worldPos, int score)
    {
        Color color = new Color(1f, 1f, 0f);
        Show(worldPos, $"+{score}", color, 1f, FloatingTextStyle.Score);
    }

    /// <summary>
    /// Coin toplama goster (altin rengi)
    /// </summary>
    public void ShowCoinPickup(Vector3 worldPos, int amount)
    {
        Color color = new Color(1f, 0.85f, 0f);
        Show(worldPos, $"+{amount}", color, 0.8f, FloatingTextStyle.Pickup);
    }

    /// <summary>
    /// Iyilesme goster (yesil)
    /// </summary>
    public void ShowHeal(Vector3 worldPos, int amount)
    {
        Color color = new Color(0.3f, 1f, 0.3f);
        Show(worldPos, $"+{amount}", color, 1.2f, FloatingTextStyle.Heal);
    }

    /// <summary>
    /// Combo goster (turuncu, buyuk)
    /// </summary>
    public void ShowCombo(Vector3 worldPos, int combo, int multiplier)
    {
        Color color = GetComboColor(combo);
        float scale = 1f + combo * 0.05f;
        scale = Mathf.Min(scale, 2f);

        string text = multiplier > 1 ? $"x{multiplier} COMBO!" : $"{combo} HIT!";
        Show(worldPos, text, color, scale, FloatingTextStyle.Combo);
    }

    /// <summary>
    /// Ozel metin goster
    /// </summary>
    public void ShowText(Vector3 worldPos, string text, Color color, float scale = 1f)
    {
        Show(worldPos, text, color, scale, FloatingTextStyle.Custom);
    }

    /// <summary>
    /// Level up veya achievement goster
    /// </summary>
    public void ShowAchievement(Vector3 worldPos, string text)
    {
        Color color = new Color(0.5f, 0f, 1f); // Mor
        Show(worldPos, text, color, 1.3f, FloatingTextStyle.Achievement);
    }

    // === ANA GOSTERIM METODU ===

    void Show(Vector3 worldPos, string text, Color color, float scale, FloatingTextStyle style)
    {
        GameObject obj = GetFromPool();
        obj.SetActive(true);
        activeTexts.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        Outline outline = obj.GetComponent<Outline>();

        // Pozisyon
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            rt.position = screenPos;
        }

        // Text ve renk
        tmp.text = text;
        tmp.color = color;
        float baseSize = isMobile ? Mathf.Max(32 * mobileFontScale, 18f) : 32;
        tmp.fontSize = Mathf.RoundToInt(baseSize * scale);

        // Outline rengi
        outline.effectColor = new Color(0, 0, 0, 0.8f);

        // Baslangic scale
        rt.localScale = Vector3.zero;

        // Animasyon baslat
        StartCoroutine(AnimateText(obj, style, scale));
    }

    IEnumerator AnimateText(GameObject obj, FloatingTextStyle style, float targetScale)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        Color originalColor = tmp.color;

        Vector3 startPos = rt.position;
        float duration = GetDuration(style);
        float elapsed = 0f;

        // Pop-in animasyonu
        float popDuration = 0.15f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            // Ease out back
            float scale = EaseOutBack(t) * targetScale;
            rt.localScale = Vector3.one * scale;

            yield return null;
        }

        rt.localScale = Vector3.one * targetScale;
        elapsed = 0f;

        // Ana animasyon
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Hareket
            Vector3 movement = GetMovement(style, t, duration);
            rt.position = startPos + movement;

            // Fade out (son %30)
            if (t > 0.7f)
            {
                float fadeT = (t - 0.7f) / 0.3f;
                tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - fadeT);
            }

            // Stil bazli efektler
            ApplyStyleEffects(rt, tmp, style, t);

            yield return null;
        }

        ReturnToPool(obj);
    }

    float GetDuration(FloatingTextStyle style)
    {
        switch (style)
        {
            case FloatingTextStyle.Damage: return 0.8f;
            case FloatingTextStyle.Score: return 1f;
            case FloatingTextStyle.Pickup: return 0.6f;
            case FloatingTextStyle.Heal: return 1f;
            case FloatingTextStyle.Combo: return 1.2f;
            case FloatingTextStyle.Achievement: return 2f;
            default: return defaultDuration;
        }
    }

    Vector3 GetMovement(FloatingTextStyle style, float t, float duration)
    {
        switch (style)
        {
            case FloatingTextStyle.Damage:
                // Asagi ve yana savrulma
                float xOffset = Mathf.Sin(t * Mathf.PI * 2f) * 20f * (1f - t);
                float yOffset = -t * 80f;
                return new Vector3(xOffset, yOffset, 0);

            case FloatingTextStyle.Score:
            case FloatingTextStyle.Heal:
                // Yukari kayma
                return new Vector3(0, t * 120f, 0);

            case FloatingTextStyle.Pickup:
                // Hizli yukari
                return new Vector3(0, EaseOutQuad(t) * 80f, 0);

            case FloatingTextStyle.Combo:
                // Yukari + hafif dalga
                float wave = Mathf.Sin(t * Mathf.PI * 4f) * 10f * (1f - t);
                return new Vector3(wave, t * 100f, 0);

            case FloatingTextStyle.Achievement:
                // Yukari yavas
                return new Vector3(0, t * 60f, 0);

            default:
                return new Vector3(0, t * defaultRiseSpeed, 0);
        }
    }

    void ApplyStyleEffects(RectTransform rt, TextMeshProUGUI tmp, FloatingTextStyle style, float t)
    {
        switch (style)
        {
            case FloatingTextStyle.Combo:
                // Pulse efekti - onceki frame degerini guvenlice hesapla
                float prevSin = 1f + Mathf.Sin((t - Time.deltaTime) * Mathf.PI * 6f) * 0.1f;
                if (prevSin > 0.01f)
                {
                    float pulse = 1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.1f;
                    rt.localScale = rt.localScale * (pulse / prevSin);
                }
                break;

            case FloatingTextStyle.Achievement:
                // Rainbow efekti
                float hue = (t * 2f) % 1f;
                tmp.color = Color.HSVToRGB(hue, 0.8f, 1f);
                break;

            case FloatingTextStyle.Damage:
                // Kirmizi flash
                if (t < 0.1f)
                {
                    tmp.color = Color.white;
                }
                break;
        }
    }

    Color GetComboColor(int combo)
    {
        if (combo >= 10) return new Color(1f, 0f, 1f);      // Mor
        if (combo >= 7) return new Color(1f, 0.3f, 0f);     // Kirmizi-turuncu
        if (combo >= 5) return new Color(1f, 0.6f, 0f);     // Turuncu
        if (combo >= 3) return new Color(1f, 0.8f, 0f);     // Sari-turuncu
        return new Color(1f, 1f, 0f);                        // Sari
    }

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}

public enum FloatingTextStyle
{
    Damage,
    Score,
    Pickup,
    Heal,
    Combo,
    Achievement,
    Custom
}
