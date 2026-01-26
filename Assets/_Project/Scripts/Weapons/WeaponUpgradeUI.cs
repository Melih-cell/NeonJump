using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Mobil icin silah upgrade UI'i
/// Oyun icinde acilip kapanabilir panel
/// </summary>
public class WeaponUpgradeUI : MonoBehaviour
{
    public static WeaponUpgradeUI Instance;

    [Header("UI References")]
    public GameObject upgradePanel;
    public Text weaponNameText;
    public Text rarityText;
    public Text levelText;
    public Text statsText;
    public Text costText;
    public Button upgradeButton;
    public Button closeButton;
    public Image weaponIcon;
    public Image rarityGlow;

    [Header("Slot Buttons")]
    public Button[] slotButtons;  // 3 slot butonu

    private Canvas canvas;
    private int selectedSlot = -1;
    private bool isOpen = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateUI();
        ClosePanel();
    }

    void Update()
    {
        // U tusu ile ac/kapa (PC icin)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.uKey.wasPressedThisFrame)
        {
            TogglePanel();
        }

        // Panel acikken guncelle
        if (isOpen)
        {
            UpdateSelectedWeaponInfo();
        }
    }

    void CreateUI()
    {
        // Canvas bul veya olustur
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UpgradeCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ana panel
        upgradePanel = new GameObject("UpgradePanel");
        upgradePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = upgradePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(400, 500);

        // Panel arka plan
        Image panelBg = upgradePanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Baslik
        CreateTitle();

        // Slot butonlari
        CreateSlotButtons();

        // Silah bilgisi
        CreateWeaponInfoSection();

        // Upgrade butonu
        CreateUpgradeButton();

        // Kapat butonu
        CreateCloseButton();
    }

    void CreateTitle()
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(upgradePanel.transform, false);

        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -10);
        rect.sizeDelta = new Vector2(-20, 40);

        Text text = titleObj.AddComponent<Text>();
        text.text = "SILAH UPGRADE";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.8f, 0.2f);
        text.alignment = TextAnchor.MiddleCenter;
    }

    void CreateSlotButtons()
    {
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(upgradePanel.transform, false);

        RectTransform containerRect = slotsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.anchoredPosition = new Vector2(0, -60);
        containerRect.sizeDelta = new Vector2(-20, 60);

        HorizontalLayoutGroup layout = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        slotButtons = new Button[3];
        string[] slotNames = { "Primary", "Secondary", "Special" };

        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i; // Closure icin

            GameObject btnObj = new GameObject("SlotBtn_" + slotNames[i]);
            btnObj.transform.SetParent(slotsContainer.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(110, 50);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.25f);

            slotButtons[i] = btnObj.AddComponent<Button>();
            slotButtons[i].targetGraphic = btnImg;
            slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));

            // Slot text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text btnText = textObj.AddComponent<Text>();
            btnText.text = slotNames[i];
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
        }
    }

    void CreateWeaponInfoSection()
    {
        GameObject infoSection = new GameObject("InfoSection");
        infoSection.transform.SetParent(upgradePanel.transform, false);

        RectTransform sectionRect = infoSection.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0, 0.25f);
        sectionRect.anchorMax = new Vector2(1, 0.75f);
        sectionRect.offsetMin = new Vector2(20, 0);
        sectionRect.offsetMax = new Vector2(-20, 0);

        // Silah adi
        weaponNameText = CreateInfoText(infoSection, "WeaponName", new Vector2(0.5f, 0.9f), 24, FontStyle.Bold);
        weaponNameText.text = "Silah Sec";

        // Rarity
        rarityText = CreateInfoText(infoSection, "Rarity", new Vector2(0.5f, 0.75f), 18, FontStyle.Italic);
        rarityText.text = "-";

        // Level
        levelText = CreateInfoText(infoSection, "Level", new Vector2(0.5f, 0.6f), 20, FontStyle.Bold);
        levelText.text = "Level: -";

        // Stats
        statsText = CreateInfoText(infoSection, "Stats", new Vector2(0.5f, 0.35f), 16, FontStyle.Normal);
        statsText.text = "Hasar: - | Hiz: -";

        // Maliyet
        costText = CreateInfoText(infoSection, "Cost", new Vector2(0.5f, 0.15f), 20, FontStyle.Bold);
        costText.text = "Maliyet: - Coin";
        costText.color = Color.yellow;
    }

    Text CreateInfoText(GameObject parent, string name, Vector2 anchorY, int fontSize, FontStyle style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, anchorY.y - 0.05f);
        rect.anchorMax = new Vector2(1, anchorY.y + 0.05f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        return text;
    }

    void CreateUpgradeButton()
    {
        GameObject btnObj = new GameObject("UpgradeButton");
        btnObj.transform.SetParent(upgradePanel.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 70);
        rect.sizeDelta = new Vector2(200, 50);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f);

        upgradeButton = btnObj.AddComponent<Button>();
        upgradeButton.targetGraphic = btnImg;
        upgradeButton.onClick.AddListener(OnUpgradeClick);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "UPGRADE";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 22;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
    }

    void CreateCloseButton()
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(upgradePanel.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-5, -5);
        rect.sizeDelta = new Vector2(40, 40);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.2f, 0.2f);

        closeButton = btnObj.AddComponent<Button>();
        closeButton.targetGraphic = btnImg;
        closeButton.onClick.AddListener(ClosePanel);

        // X text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "X";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
    }

    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        isOpen = true;
        upgradePanel.SetActive(true);
        Time.timeScale = 0f; // Oyunu durdur

        // Mevcut silahi sec
        if (WeaponManager.Instance != null)
        {
            SelectSlot(WeaponManager.Instance.currentSlot);
        }
    }

    public void ClosePanel()
    {
        isOpen = false;
        upgradePanel.SetActive(false);
        Time.timeScale = 1f; // Oyunu devam ettir
    }

    void SelectSlot(int slot)
    {
        selectedSlot = slot;

        // Buton renklerini guncelle
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Image img = slotButtons[i].GetComponent<Image>();
            if (i == slot)
                img.color = new Color(0.3f, 0.5f, 0.8f); // Secili
            else
                img.color = new Color(0.2f, 0.2f, 0.25f); // Normal
        }

        UpdateSelectedWeaponInfo();
    }

    void UpdateSelectedWeaponInfo()
    {
        if (WeaponManager.Instance == null || selectedSlot < 0) return;

        WeaponInstance weapon = null;
        switch (selectedSlot)
        {
            case 0: weapon = WeaponManager.Instance.primaryWeapon; break;
            case 1: weapon = WeaponManager.Instance.secondaryWeapon; break;
            case 2: weapon = WeaponManager.Instance.specialWeapon; break;
        }

        if (weapon == null || !weapon.isUnlocked)
        {
            weaponNameText.text = "Bos Slot";
            weaponNameText.color = Color.gray;
            rarityText.text = "-";
            levelText.text = "Level: -";
            statsText.text = "-";
            costText.text = "-";
            upgradeButton.interactable = false;
            return;
        }

        // Silah bilgileri
        weaponNameText.text = weapon.data.weaponName.ToUpper();
        weaponNameText.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);

        rarityText.text = WeaponRarityHelper.GetRarityName(weapon.rarity);
        rarityText.color = WeaponRarityHelper.GetRarityColor(weapon.rarity);

        levelText.text = $"Level: {weapon.level} / {WeaponInstance.MaxLevel}";
        levelText.color = weapon.level >= WeaponInstance.MaxLevel ? Color.yellow : Color.white;

        statsText.text = $"Hasar: {weapon.GetEffectiveDamage()} | Hiz: {weapon.GetEffectiveFireRate():F2}s\n" +
                        $"Sarjor: {weapon.GetEffectiveMaxAmmo()} | Reload: {weapon.GetEffectiveReloadTime():F1}s";

        // Upgrade durumu
        if (weapon.CanUpgrade())
        {
            int cost = weapon.GetUpgradeCost();
            int coins = GameManager.Instance != null ? GameManager.Instance.GetCoins() : 0;

            costText.text = $"Maliyet: {cost} Coin";
            costText.color = coins >= cost ? Color.green : Color.red;

            upgradeButton.interactable = coins >= cost;
        }
        else
        {
            costText.text = "MAX LEVEL!";
            costText.color = Color.yellow;
            upgradeButton.interactable = false;
        }
    }

    void OnUpgradeClick()
    {
        if (WeaponManager.Instance == null || selectedSlot < 0) return;

        bool success = WeaponManager.Instance.TryUpgradeWeapon(selectedSlot);

        if (success)
        {
            // Basari efekti
            StartCoroutine(UpgradeSuccessEffect());
        }

        UpdateSelectedWeaponInfo();
    }

    IEnumerator UpgradeSuccessEffect()
    {
        // Buton rengi degistir
        Image btnImg = upgradeButton.GetComponent<Image>();
        Color originalColor = btnImg.color;
        btnImg.color = Color.yellow;

        yield return new WaitForSecondsRealtime(0.2f);

        btnImg.color = originalColor;
    }
}
