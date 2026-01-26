using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Dusman gostergeleri - Health bar ve off-screen indicator
/// </summary>
public class EnemyIndicatorManager : MonoBehaviour
{
    public static EnemyIndicatorManager Instance { get; private set; }

    [Header("Settings")]
    public float healthBarWidth = 60f;
    public float healthBarHeight = 8f;
    public float healthBarOffset = 1.2f;
    public float offscreenIndicatorSize = 30f;
    public float offscreenEdgePadding = 50f;

    private Canvas worldCanvas;
    private Canvas screenCanvas;
    private Camera mainCamera;

    // Object pooling
    private Queue<EnemyHealthBarUI> healthBarPool = new Queue<EnemyHealthBarUI>();
    private Queue<OffscreenIndicator> indicatorPool = new Queue<OffscreenIndicator>();
    private Dictionary<EnemyHealth, EnemyHealthBarUI> activeHealthBars = new Dictionary<EnemyHealth, EnemyHealthBarUI>();
    private Dictionary<EnemyHealth, OffscreenIndicator> activeIndicators = new Dictionary<EnemyHealth, OffscreenIndicator>();

    void Awake()
    {
        Instance = this;
        SetupCanvases();
        CreatePools();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        UpdateAllIndicators();
    }

    void SetupCanvases()
    {
        // World space canvas (health bars)
        GameObject worldCanvasObj = new GameObject("EnemyWorldCanvas");
        worldCanvasObj.transform.SetParent(transform);

        worldCanvas = worldCanvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 50;

        // Screen space canvas (off-screen indicators)
        GameObject screenCanvasObj = new GameObject("EnemyScreenCanvas");
        screenCanvasObj.transform.SetParent(transform);

        screenCanvas = screenCanvasObj.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 90;

        CanvasScaler scaler = screenCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }

    void CreatePools()
    {
        // Health bar pool
        for (int i = 0; i < 20; i++)
        {
            EnemyHealthBarUI bar = CreateHealthBar();
            bar.gameObject.SetActive(false);
            healthBarPool.Enqueue(bar);
        }

        // Indicator pool
        for (int i = 0; i < 10; i++)
        {
            OffscreenIndicator indicator = CreateOffscreenIndicator();
            indicator.gameObject.SetActive(false);
            indicatorPool.Enqueue(indicator);
        }
    }

    EnemyHealthBarUI CreateHealthBar()
    {
        GameObject obj = new GameObject("EnemyHealthBar");
        obj.transform.SetParent(worldCanvas.transform, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(obj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(obj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = new Vector2(1, 1);
        fillRt.offsetMax = new Vector2(-1, -1);

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.2f, 0.2f, 1f);

        // Border
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0f, 0f, 0.5f);
        outline.effectDistance = new Vector2(1, 1);

        EnemyHealthBarUI bar = obj.AddComponent<EnemyHealthBarUI>();
        bar.fillImage = fillImg;
        bar.backgroundImage = bgImg;

        return bar;
    }

    OffscreenIndicator CreateOffscreenIndicator()
    {
        GameObject obj = new GameObject("OffscreenIndicator");
        obj.transform.SetParent(screenCanvas.transform, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(offscreenIndicatorSize, offscreenIndicatorSize);

        // Arrow/indicator image
        Image img = obj.AddComponent<Image>();
        img.color = new Color(1f, 0.3f, 0.3f, 0.8f);

        // Distance text
        GameObject textObj = new GameObject("DistanceText");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0);
        textRt.anchorMax = new Vector2(0.5f, 0);
        textRt.pivot = new Vector2(0.5f, 1);
        textRt.anchoredPosition = new Vector2(0, -5);
        textRt.sizeDelta = new Vector2(50, 20);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        OffscreenIndicator indicator = obj.AddComponent<OffscreenIndicator>();
        indicator.arrowImage = img;
        indicator.distanceText = tmp;

        return indicator;
    }

    void UpdateAllIndicators()
    {
        if (mainCamera == null) return;

        // Tum aktif dusmanlar icin (EnemyHealth component'i olanlar)
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead()) continue;
            if (!enemy.showHealthBar) continue;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(enemy.transform.position);
            bool isOnScreen = IsOnScreen(screenPos);

            if (isOnScreen)
            {
                // Health bar goster
                ShowHealthBar(enemy);
                HideOffscreenIndicator(enemy);
            }
            else
            {
                // Off-screen indicator goster
                HideHealthBar(enemy);
                ShowOffscreenIndicator(enemy, screenPos);
            }
        }

        // Artik var olmayan dusmanlar icin temizlik
        CleanupDeadEnemies();
    }

    bool IsOnScreen(Vector3 screenPos)
    {
        return screenPos.z > 0 &&
               screenPos.x > 0 && screenPos.x < Screen.width &&
               screenPos.y > 0 && screenPos.y < Screen.height;
    }

    void ShowHealthBar(EnemyHealth enemy)
    {
        if (!activeHealthBars.TryGetValue(enemy, out EnemyHealthBarUI bar))
        {
            bar = GetHealthBarFromPool();
            activeHealthBars[enemy] = bar;
        }

        bar.gameObject.SetActive(true);
        bar.UpdateBar(enemy);

        // Pozisyon
        Vector3 pos = enemy.transform.position + Vector3.up * enemy.healthBarYOffset;
        bar.transform.position = pos;

        // Kameraya bak
        bar.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
    }

    void HideHealthBar(EnemyHealth enemy)
    {
        if (activeHealthBars.TryGetValue(enemy, out EnemyHealthBarUI bar))
        {
            bar.gameObject.SetActive(false);
            healthBarPool.Enqueue(bar);
            activeHealthBars.Remove(enemy);
        }
    }

    void ShowOffscreenIndicator(EnemyHealth enemy, Vector3 screenPos)
    {
        if (!activeIndicators.TryGetValue(enemy, out OffscreenIndicator indicator))
        {
            indicator = GetIndicatorFromPool();
            activeIndicators[enemy] = indicator;
        }

        indicator.gameObject.SetActive(true);

        // Ekran kenarinda pozisyon hesapla
        Vector2 edgePos = CalculateEdgePosition(screenPos);
        indicator.GetComponent<RectTransform>().position = edgePos;

        // Rotasyon (dusmana dogru)
        float angle = CalculateAngleToEnemy(edgePos, enemy.transform.position);
        indicator.transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        // Mesafe
        float distance = Vector3.Distance(mainCamera.transform.position, enemy.transform.position);
        indicator.SetDistance(distance);

        // Renk (mesafeye gore)
        Color indicatorColor = GetIndicatorColor(enemy, distance);
        indicator.arrowImage.color = indicatorColor;
    }

    void HideOffscreenIndicator(EnemyHealth enemy)
    {
        if (activeIndicators.TryGetValue(enemy, out OffscreenIndicator indicator))
        {
            indicator.gameObject.SetActive(false);
            indicatorPool.Enqueue(indicator);
            activeIndicators.Remove(enemy);
        }
    }

    Vector2 CalculateEdgePosition(Vector3 screenPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = new Vector2(screenPos.x, screenPos.y) - screenCenter;

        // Ekran kenarinda kes
        float maxX = Screen.width / 2f - offscreenEdgePadding;
        float maxY = Screen.height / 2f - offscreenEdgePadding;

        float scale = Mathf.Min(
            maxX / Mathf.Abs(direction.x + 0.001f),
            maxY / Mathf.Abs(direction.y + 0.001f)
        );

        scale = Mathf.Min(scale, 1f);

        return screenCenter + direction.normalized * Mathf.Min(direction.magnitude * scale, new Vector2(maxX, maxY).magnitude);
    }

    float CalculateAngleToEnemy(Vector2 indicatorPos, Vector3 enemyWorldPos)
    {
        Vector3 enemyScreenPos = mainCamera.WorldToScreenPoint(enemyWorldPos);
        Vector2 direction = new Vector2(enemyScreenPos.x, enemyScreenPos.y) - indicatorPos;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    Color GetIndicatorColor(EnemyHealth enemy, float distance)
    {
        // Tehlikeli dusmanlar kirmizi, normal dusmanlar turuncu
        float healthPercent = enemy.GetHealthPercent();

        if (distance < 5f)
            return new Color(1f, 0.2f, 0.2f, 0.9f); // Yakin - Kirmizi
        else if (distance < 10f)
            return new Color(1f, 0.5f, 0f, 0.8f);   // Orta - Turuncu
        else
            return new Color(1f, 0.8f, 0f, 0.6f);   // Uzak - Sari
    }

    void CleanupDeadEnemies()
    {
        List<EnemyHealth> toRemove = new List<EnemyHealth>();

        foreach (var kvp in activeHealthBars)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy || kvp.Key.IsDead())
            {
                kvp.Value.gameObject.SetActive(false);
                healthBarPool.Enqueue(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var enemy in toRemove)
        {
            activeHealthBars.Remove(enemy);
        }

        toRemove.Clear();

        foreach (var kvp in activeIndicators)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy || kvp.Key.IsDead())
            {
                kvp.Value.gameObject.SetActive(false);
                indicatorPool.Enqueue(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var enemy in toRemove)
        {
            activeIndicators.Remove(enemy);
        }
    }

    EnemyHealthBarUI GetHealthBarFromPool()
    {
        if (healthBarPool.Count > 0)
            return healthBarPool.Dequeue();
        return CreateHealthBar();
    }

    OffscreenIndicator GetIndicatorFromPool()
    {
        if (indicatorPool.Count > 0)
            return indicatorPool.Dequeue();
        return CreateOffscreenIndicator();
    }

    // === PUBLIC METODLAR ===

    public void OnEnemyDamaged(EnemyHealth enemy, float damage)
    {
        if (activeHealthBars.TryGetValue(enemy, out EnemyHealthBarUI bar))
        {
            bar.OnDamage();
        }
    }

    public void OnEnemyDied(EnemyHealth enemy)
    {
        HideHealthBar(enemy);
        HideOffscreenIndicator(enemy);
    }
}

// Health bar component
public class EnemyHealthBarUI : MonoBehaviour
{
    public Image fillImage;
    public Image backgroundImage;

    private float displayedHealth;
    private Color normalColor = new Color(1f, 0.2f, 0.2f);
    private Color damageColor = Color.white;
    private float damageFlashTimer;

    public void UpdateBar(EnemyHealth enemy)
    {
        float healthPercent = enemy.GetHealthPercent();

        // Yumusak gecis
        displayedHealth = Mathf.Lerp(displayedHealth, healthPercent, Time.deltaTime * 10f);

        fillImage.rectTransform.anchorMax = new Vector2(displayedHealth, 1);

        // Renk
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= Time.deltaTime;
            fillImage.color = Color.Lerp(normalColor, damageColor, damageFlashTimer / 0.1f);
        }
        else
        {
            // Can durumuna gore renk
            if (healthPercent > 0.5f)
                fillImage.color = new Color(0.2f, 1f, 0.2f); // Yesil
            else if (healthPercent > 0.25f)
                fillImage.color = new Color(1f, 0.8f, 0f);   // Sari
            else
                fillImage.color = new Color(1f, 0.2f, 0.2f); // Kirmizi
        }
    }

    public void OnDamage()
    {
        damageFlashTimer = 0.1f;
    }
}

// Off-screen indicator component
public class OffscreenIndicator : MonoBehaviour
{
    public Image arrowImage;
    public TextMeshProUGUI distanceText;

    public void SetDistance(float distance)
    {
        if (distance < 10f)
            distanceText.text = $"{distance:F1}m";
        else
            distanceText.text = $"{Mathf.RoundToInt(distance)}m";
    }
}
