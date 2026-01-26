using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class UpgradeInfo
{
    public string id;
    public string displayName;
    public string description;
    public int maxLevel;
    public int[] costs;  // Her seviye icin maliyet
    public Color iconColor;
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Upgrade Definitions")]
    public List<UpgradeInfo> upgrades;

    [Header("UI References")]
    public GameObject upgradePanel;
    public Transform upgradeListContainer;
    public TextMeshProUGUI totalCoinsText;
    public GameObject upgradeItemPrefab;

    [Header("Runtime")]
    private List<GameObject> upgradeItems = new List<GameObject>();
    private bool isInitialized = false;

    // Upgrade etkileri
    public int BonusMaxHealth => GetUpgradeLevel("health");
    public float SpeedMultiplier => 1f + GetUpgradeLevel("speed") * 0.1f;
    public float DashDurationMultiplier => 1f + GetUpgradeLevel("dash") * 0.3f;
    public int ExtraJumps => GetUpgradeLevel("jump");
    public float DamageMultiplier => 1f + GetUpgradeLevel("damage") * 0.15f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeUpgrades();
    }

    void InitializeUpgrades()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Varsayilan upgrade tanimlari
        if (upgrades == null || upgrades.Count == 0)
        {
            upgrades = new List<UpgradeInfo>
            {
                new UpgradeInfo
                {
                    id = "health",
                    displayName = "EKSTRA CAN",
                    description = "Maksimum can +1",
                    maxLevel = 3,
                    costs = new int[] { 100, 200, 400 },
                    iconColor = new Color(1f, 0.3f, 0.3f) // Kirmizi
                },
                new UpgradeInfo
                {
                    id = "speed",
                    displayName = "HIZ ARTISI",
                    description = "Hareket hizi +%10",
                    maxLevel = 3,
                    costs = new int[] { 150, 300, 600 },
                    iconColor = new Color(0.3f, 1f, 0.5f) // Yesil
                },
                new UpgradeInfo
                {
                    id = "dash",
                    displayName = "GELISMIS DASH",
                    description = "Dash suresi +%30",
                    maxLevel = 2,
                    costs = new int[] { 200, 500 },
                    iconColor = new Color(0f, 0.8f, 1f) // Cyan
                },
                new UpgradeInfo
                {
                    id = "jump",
                    displayName = "EKSTRA ZIPLAMA",
                    description = "Havada ekstra ziplama hakki",
                    maxLevel = 2,
                    costs = new int[] { 250, 600 },
                    iconColor = new Color(1f, 1f, 0.3f) // Sari
                },
                new UpgradeInfo
                {
                    id = "damage",
                    displayName = "HASAR ARTISI",
                    description = "Silah hasari +%15",
                    maxLevel = 3,
                    costs = new int[] { 200, 400, 800 },
                    iconColor = new Color(1f, 0.5f, 0f) // Turuncu
                }
            };
        }
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.GetUpgradeLevel(upgradeId);
        }
        return 0;
    }

    public int GetUpgradeCost(string upgradeId)
    {
        int currentLevel = GetUpgradeLevel(upgradeId);
        UpgradeInfo info = upgrades.Find(u => u.id == upgradeId);

        if (info == null || currentLevel >= info.maxLevel)
            return -1;

        if (currentLevel < info.costs.Length)
            return info.costs[currentLevel];

        return -1;
    }

    public bool CanAffordUpgrade(string upgradeId)
    {
        int cost = GetUpgradeCost(upgradeId);
        if (cost < 0) return false;

        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.Data.totalCoins >= cost;
        }
        return false;
    }

    public bool IsUpgradeMaxed(string upgradeId)
    {
        UpgradeInfo info = upgrades.Find(u => u.id == upgradeId);
        if (info == null) return true;

        return GetUpgradeLevel(upgradeId) >= info.maxLevel;
    }

    public bool TryPurchaseUpgrade(string upgradeId)
    {
        if (SaveManager.Instance == null) return false;

        bool success = SaveManager.Instance.TryPurchaseUpgrade(upgradeId);

        if (success)
        {
            // UI guncelle
            RefreshUI();

            // Ses efekti
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPowerUp();
            }

            Debug.Log($"Upgrade satin alindi: {upgradeId}");
        }

        return success;
    }

    // === UI ===

    public void ShowUpgradePanel()
    {
        if (upgradePanel == null)
        {
            CreateUpgradePanel();
        }

        upgradePanel.SetActive(true);
        RefreshUI();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }
    }

    public void HideUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }
    }

    void RefreshUI()
    {
        // Toplam coin guncelle
        if (totalCoinsText != null && SaveManager.Instance != null)
        {
            totalCoinsText.text = $"COIN: {SaveManager.Instance.Data.totalCoins}";
        }

        // Upgrade itemlarini guncelle
        foreach (var item in upgradeItems)
        {
            if (item != null)
            {
                UpdateUpgradeItem(item);
            }
        }
    }

    void UpdateUpgradeItem(GameObject item)
    {
        string upgradeId = item.name;
        UpgradeInfo info = upgrades.Find(u => u.id == upgradeId);
        if (info == null) return;

        int currentLevel = GetUpgradeLevel(upgradeId);
        int cost = GetUpgradeCost(upgradeId);
        bool isMaxed = IsUpgradeMaxed(upgradeId);
        bool canAfford = CanAffordUpgrade(upgradeId);

        // Level text
        TextMeshProUGUI levelText = item.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        if (levelText != null)
        {
            levelText.text = $"Seviye: {currentLevel}/{info.maxLevel}";
        }

        // Cost text
        TextMeshProUGUI costText = item.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
        if (costText != null)
        {
            if (isMaxed)
            {
                costText.text = "MAKSIMUM";
                costText.color = new Color(0f, 1f, 0.5f);
            }
            else
            {
                costText.text = $"{cost} Coin";
                costText.color = canAfford ? Color.white : Color.red;
            }
        }

        // Buy button
        Button buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();
        if (buyButton != null)
        {
            buyButton.interactable = !isMaxed && canAfford;

            Image btnImage = buyButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = (isMaxed || !canAfford)
                    ? new Color(0.3f, 0.3f, 0.3f)
                    : new Color(0f, 0.7f, 0.3f);
            }
        }

        // Progress bar
        Image progressBar = item.transform.Find("ProgressBar")?.GetComponent<Image>();
        if (progressBar != null)
        {
            progressBar.fillAmount = (float)currentLevel / info.maxLevel;
        }
    }

    void CreateUpgradePanel()
    {
        // Canvas bul veya olustur
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UpgradeCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Panel olustur
        upgradePanel = new GameObject("UpgradePanel");
        upgradePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = upgradePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Arka plan
        Image bgImage = upgradePanel.AddComponent<Image>();
        bgImage.color = new Color(0.02f, 0.02f, 0.08f, 0.95f);

        // Baslik
        CreateNeonText(upgradePanel.transform, "YUKSELTMELER", 48, new Vector2(0, 250));

        // Toplam coin
        GameObject coinObj = CreateNeonText(upgradePanel.transform, "COIN: 0", 28, new Vector2(0, 190));
        totalCoinsText = coinObj.GetComponent<TextMeshProUGUI>();
        totalCoinsText.color = new Color(1f, 0.8f, 0f);

        // Upgrade listesi container
        GameObject container = new GameObject("UpgradeList");
        container.transform.SetParent(upgradePanel.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -20);
        containerRect.sizeDelta = new Vector2(500, 350);

        upgradeListContainer = container.transform;

        // Upgrade itemlari olustur
        float yOffset = 140;
        foreach (var upgrade in upgrades)
        {
            GameObject item = CreateUpgradeItem(upgradeListContainer, upgrade, new Vector2(0, yOffset));
            upgradeItems.Add(item);
            yOffset -= 75;
        }

        // Kapat butonu
        CreateCloseButton(upgradePanel.transform);

        upgradePanel.SetActive(false);
    }

    GameObject CreateUpgradeItem(Transform parent, UpgradeInfo info, Vector2 position)
    {
        GameObject item = new GameObject(info.id);
        item.transform.SetParent(parent, false);

        RectTransform rect = item.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(480, 70);

        // Arka plan
        Image bg = item.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(item.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(10, 0);
        iconRect.sizeDelta = new Vector2(50, 50);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = info.iconColor;

        // Isim
        GameObject nameObj = CreateNeonText(item.transform, info.displayName, 22, new Vector2(-60, 15));
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(-60, 15);
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = info.iconColor;

        // Aciklama
        GameObject descObj = CreateNeonText(item.transform, info.description, 14, new Vector2(-60, -10));
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.anchoredPosition = new Vector2(-60, -10);
        TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
        descTmp.alignment = TextAlignmentOptions.Left;
        descTmp.color = new Color(0.7f, 0.7f, 0.7f);

        // Seviye
        GameObject levelObj = CreateNeonText(item.transform, "Seviye: 0/3", 16, new Vector2(120, 15));
        levelObj.name = "LevelText";
        TextMeshProUGUI levelTmp = levelObj.GetComponent<TextMeshProUGUI>();
        levelTmp.alignment = TextAlignmentOptions.Right;

        // Progress bar (arka plan)
        GameObject progressBg = new GameObject("ProgressBg");
        progressBg.transform.SetParent(item.transform, false);
        RectTransform progressBgRect = progressBg.AddComponent<RectTransform>();
        progressBgRect.anchorMin = progressBgRect.anchorMax = new Vector2(1, 0.5f);
        progressBgRect.pivot = new Vector2(1, 0.5f);
        progressBgRect.anchoredPosition = new Vector2(-100, -15);
        progressBgRect.sizeDelta = new Vector2(100, 8);
        Image progressBgImg = progressBg.AddComponent<Image>();
        progressBgImg.color = new Color(0.2f, 0.2f, 0.2f);

        // Progress bar (dolum)
        GameObject progressFill = new GameObject("ProgressBar");
        progressFill.transform.SetParent(progressBg.transform, false);
        RectTransform progressFillRect = progressFill.AddComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;
        Image progressFillImg = progressFill.AddComponent<Image>();
        progressFillImg.color = info.iconColor;
        progressFillImg.type = Image.Type.Filled;
        progressFillImg.fillMethod = Image.FillMethod.Horizontal;

        // Maliyet
        GameObject costObj = CreateNeonText(item.transform, "100 Coin", 16, new Vector2(180, -15));
        costObj.name = "CostText";

        // Satin al butonu
        GameObject btnObj = new GameObject("BuyButton");
        btnObj.transform.SetParent(item.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(1, 0.5f);
        btnRect.pivot = new Vector2(1, 0.5f);
        btnRect.anchoredPosition = new Vector2(-10, 0);
        btnRect.sizeDelta = new Vector2(70, 50);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0f, 0.7f, 0.3f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        string upgradeId = info.id;
        btn.onClick.AddListener(() => TryPurchaseUpgrade(upgradeId));

        // Buton text
        GameObject btnTextObj = CreateNeonText(btnObj.transform, "AL", 18, Vector2.zero);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        return item;
    }

    void CreateCloseButton(Transform parent)
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 50);
        rect.sizeDelta = new Vector2(200, 50);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.6f, 0.1f, 0.1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(HideUpgradePanel);

        GameObject textObj = CreateNeonText(btnObj.transform, "KAPAT", 24, Vector2.zero);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    GameObject CreateNeonText(Transform parent, string text, int fontSize, Vector2 position)
    {
        GameObject textObj = new GameObject("Text_" + text);
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(400, fontSize + 20);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0f, 1f, 1f);
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
    }

    // === OYUN ICI ETKILERI ===

    // GameManager'dan cagrilacak
    public int GetAdjustedMaxHealth(int baseHealth)
    {
        return baseHealth + BonusMaxHealth;
    }

    // PlayerController'dan cagrilacak
    public float GetAdjustedMoveSpeed(float baseSpeed)
    {
        return baseSpeed * SpeedMultiplier;
    }

    public float GetAdjustedDashDuration(float baseDuration)
    {
        return baseDuration * DashDurationMultiplier;
    }

    public int GetExtraJumpCount()
    {
        return ExtraJumps;
    }

    // WeaponManager'dan cagrilacak
    public float GetAdjustedDamage(float baseDamage)
    {
        return baseDamage * DamageMultiplier;
    }
}
