using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ekran bildirimleri (achievements, item pickups, warnings, etc.)
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Settings")]
    public int maxNotifications = 5;
    public float defaultDuration = 3f;
    public float slideInDuration = 0.3f;
    public float slideOutDuration = 0.2f;

    private Canvas canvas;
    private RectTransform notificationContainer;
    private Queue<UINotificationData> pendingNotifications = new Queue<UINotificationData>();
    private List<GameObject> activeNotifications = new List<GameObject>();
    private bool isMobile;
    private float mobileFontScale = 1f;

    void Awake()
    {
        Instance = this;

        isMobile = Application.isMobilePlatform ||
                   UnityEngine.InputSystem.Touchscreen.current != null;
        if (isMobile)
        {
            mobileFontScale = Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f);
        }

        SetupUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void SetupUI()
    {
        // Canvas bul veya olustur
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("NotificationCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = isMobile ? new Vector2(1280, 720) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Container (sag ust kose)
        GameObject containerObj = new GameObject("NotificationContainer");
        containerObj.transform.SetParent(canvas.transform, false);

        notificationContainer = containerObj.AddComponent<RectTransform>();
        notificationContainer.anchorMin = new Vector2(1, 1);
        notificationContainer.anchorMax = new Vector2(1, 1);
        notificationContainer.pivot = new Vector2(1, 1);
        float containerWidth = isMobile ? 420f : 350f;
        notificationContainer.anchoredPosition = new Vector2(-20, -80);
        notificationContainer.sizeDelta = new Vector2(containerWidth, 400);

        VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperRight;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = containerObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // === PUBLIC METODLAR ===

    public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        UINotificationData data = new UINotificationData
        {
            title = title,
            message = message,
            type = type,
            duration = defaultDuration
        };

        EnqueueNotification(data);
    }

    public void ShowItemPickup(string itemName, Sprite icon = null)
    {
        UINotificationData data = new UINotificationData
        {
            title = "ESYA ALINDI",
            message = itemName,
            type = NotificationType.ItemPickup,
            icon = icon,
            duration = 2f
        };

        EnqueueNotification(data);
    }

    public void ShowWeaponPickup(string weaponName, Sprite icon = null)
    {
        UINotificationData data = new UINotificationData
        {
            title = "SILAH ALINDI",
            message = weaponName,
            type = NotificationType.WeaponPickup,
            icon = icon,
            duration = 2.5f
        };

        EnqueueNotification(data);
    }

    public void ShowAchievement(string title, string description)
    {
        UINotificationData data = new UINotificationData
        {
            title = title,
            message = description,
            type = NotificationType.Achievement,
            duration = 4f
        };

        EnqueueNotification(data);
    }

    public void ShowWarning(string message)
    {
        UINotificationData data = new UINotificationData
        {
            title = "UYARI",
            message = message,
            type = NotificationType.Warning,
            duration = 3f
        };

        EnqueueNotification(data);
    }

    public void ShowBossAppear(string bossName)
    {
        UINotificationData data = new UINotificationData
        {
            title = "BOSS ORTAYA CIKTI",
            message = bossName,
            type = NotificationType.Boss,
            duration = 3f
        };

        EnqueueNotification(data);
    }

    public void ShowLevelUp(int level)
    {
        UINotificationData data = new UINotificationData
        {
            title = "SEVIYE ATLADIN!",
            message = $"Seviye {level}",
            type = NotificationType.LevelUp,
            duration = 3f
        };

        EnqueueNotification(data);
    }

    // === INTERNAL ===

    void EnqueueNotification(UINotificationData data)
    {
        if (activeNotifications.Count < maxNotifications)
        {
            CreateNotification(data);
        }
        else
        {
            pendingNotifications.Enqueue(data);
        }
    }

    void CreateNotification(UINotificationData data)
    {
        GameObject notifObj = new GameObject("Notification");
        notifObj.transform.SetParent(notificationContainer, false);
        activeNotifications.Add(notifObj);

        // Layout Element (mobilde daha buyuk)
        LayoutElement le = notifObj.AddComponent<LayoutElement>();
        float baseHeight = data.type == NotificationType.Achievement ? 80 : 60;
        float baseWidth = 330;
        if (isMobile)
        {
            baseHeight *= 1.2f;
            baseWidth = 400f;
        }
        le.preferredHeight = baseHeight;
        le.preferredWidth = baseWidth;

        // Arka plan
        Image bg = notifObj.AddComponent<Image>();
        bg.color = GetBackgroundColor(data.type);

        // Canvas Group (fade icin)
        CanvasGroup cg = notifObj.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        // Neon border
        Outline outline = notifObj.AddComponent<Outline>();
        outline.effectColor = GetBorderColor(data.type);
        outline.effectDistance = new Vector2(2, 2);

        // Icerik container
        RectTransform contentRt = notifObj.GetComponent<RectTransform>();

        // Icon (varsa)
        float textOffsetX = 15f;
        if (data.icon != null || data.type != NotificationType.Info)
        {
            textOffsetX = 55f;
            CreateIcon(notifObj.transform, data);
        }

        // Title
        CreateTitle(notifObj.transform, data, textOffsetX);

        // Message
        CreateMessage(notifObj.transform, data, textOffsetX);

        // Progress bar (duration gostergesi)
        CreateProgressBar(notifObj.transform, data);

        // Animasyon baslat
        StartCoroutine(AnimateNotification(notifObj, data, cg));
    }

    void CreateIcon(Transform parent, UINotificationData data)
    {
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(parent, false);

        RectTransform rt = iconObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(10, 0);
        rt.sizeDelta = new Vector2(40, 40);

        Image img = iconObj.AddComponent<Image>();

        if (data.icon != null)
        {
            img.sprite = data.icon;
        }
        else
        {
            img.color = GetIconColor(data.type);
        }
    }

    void CreateTitle(Transform parent, UINotificationData data, float offsetX)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);

        RectTransform rt = titleObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(offsetX, -8);
        rt.sizeDelta = new Vector2(-offsetX - 10, 22);

        TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
        tmp.text = data.title;
        tmp.fontSize = isMobile ? Mathf.Max(16 * mobileFontScale, 14f) : 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = GetTitleColor(data.type);
        tmp.alignment = TextAlignmentOptions.Left;
    }

    void CreateMessage(Transform parent, UINotificationData data, float offsetX)
    {
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(parent, false);

        RectTransform rt = msgObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(offsetX, 12);
        rt.sizeDelta = new Vector2(-offsetX - 10, -35);

        TextMeshProUGUI tmp = msgObj.AddComponent<TextMeshProUGUI>();
        tmp.text = data.message;
        tmp.fontSize = isMobile ? Mathf.Max(14 * mobileFontScale, 12f) : 14;
        tmp.color = new Color(0.9f, 0.9f, 0.9f);
        tmp.alignment = TextAlignmentOptions.Left;
    }

    void CreateProgressBar(Transform parent, UINotificationData data)
    {
        GameObject barObj = new GameObject("ProgressBar");
        barObj.transform.SetParent(parent, false);

        RectTransform rt = barObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(0, 3);

        Image img = barObj.AddComponent<Image>();
        img.color = GetBorderColor(data.type);
    }

    IEnumerator AnimateNotification(GameObject notifObj, UINotificationData data, CanvasGroup cg)
    {
        RectTransform rt = notifObj.GetComponent<RectTransform>();
        Image progressBar = notifObj.transform.Find("ProgressBar")?.GetComponent<Image>();

        // Baslangic pozisyonu (sag disarida)
        Vector2 originalPos = rt.anchoredPosition;
        rt.anchoredPosition = originalPos + new Vector2(400, 0);

        // Slide in
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(elapsed / slideInDuration);

            rt.anchoredPosition = Vector2.Lerp(originalPos + new Vector2(400, 0), originalPos, t);
            cg.alpha = t;

            yield return null;
        }

        rt.anchoredPosition = originalPos;
        cg.alpha = 1;

        // Sure bekle + progress bar animasyonu
        elapsed = 0f;
        while (elapsed < data.duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (progressBar != null)
            {
                float progress = 1f - (elapsed / data.duration);
                progressBar.rectTransform.anchorMax = new Vector2(progress, 1);
            }

            yield return null;
        }

        // Slide out
        elapsed = 0f;
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInQuad(elapsed / slideOutDuration);

            rt.anchoredPosition = Vector2.Lerp(originalPos, originalPos + new Vector2(400, 0), t);
            cg.alpha = 1f - t;

            yield return null;
        }

        // Temizle
        activeNotifications.Remove(notifObj);
        Destroy(notifObj);

        // Sirada bekleyen var mi
        if (pendingNotifications.Count > 0)
        {
            UINotificationData next = pendingNotifications.Dequeue();
            CreateNotification(next);
        }
    }

    // === RENK YARDIMCILARI ===

    Color GetBackgroundColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Achievement: return new Color(0.1f, 0.05f, 0.2f, 0.95f);
            case NotificationType.Warning: return new Color(0.2f, 0.1f, 0f, 0.95f);
            case NotificationType.Boss: return new Color(0.2f, 0f, 0f, 0.95f);
            case NotificationType.LevelUp: return new Color(0f, 0.15f, 0.1f, 0.95f);
            case NotificationType.ItemPickup: return new Color(0.05f, 0.1f, 0.15f, 0.95f);
            case NotificationType.WeaponPickup: return new Color(0.1f, 0.1f, 0.05f, 0.95f);
            default: return new Color(0.05f, 0.05f, 0.1f, 0.95f);
        }
    }

    Color GetBorderColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Achievement: return new Color(0.8f, 0.4f, 1f);
            case NotificationType.Warning: return new Color(1f, 0.6f, 0f);
            case NotificationType.Boss: return new Color(1f, 0f, 0f);
            case NotificationType.LevelUp: return new Color(0f, 1f, 0.5f);
            case NotificationType.ItemPickup: return new Color(0f, 0.8f, 1f);
            case NotificationType.WeaponPickup: return new Color(1f, 0.8f, 0f);
            default: return new Color(0f, 1f, 1f);
        }
    }

    Color GetTitleColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Achievement: return new Color(0.8f, 0.4f, 1f);
            case NotificationType.Warning: return new Color(1f, 0.6f, 0f);
            case NotificationType.Boss: return new Color(1f, 0.2f, 0.2f);
            case NotificationType.LevelUp: return new Color(0.3f, 1f, 0.5f);
            case NotificationType.ItemPickup: return new Color(0f, 0.9f, 1f);
            case NotificationType.WeaponPickup: return new Color(1f, 0.9f, 0.3f);
            default: return new Color(0f, 1f, 1f);
        }
    }

    Color GetIconColor(NotificationType type)
    {
        return GetBorderColor(type);
    }

    float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    float EaseInQuad(float t) => t * t;
}

public enum NotificationType
{
    Info,
    Warning,
    Achievement,
    Boss,
    LevelUp,
    ItemPickup,
    WeaponPickup
}

public struct UINotificationData
{
    public string title;
    public string message;
    public NotificationType type;
    public Sprite icon;
    public float duration;
}
