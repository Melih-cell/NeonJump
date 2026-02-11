using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Crafting UI sistemi.
/// Tarif listesi, malzeme gosterimi ve craft islemleri.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject craftingPanel;
    public CanvasGroup canvasGroup;

    [Header("Tarif Listesi")]
    public Transform recipeListContainer;
    public GameObject recipeEntryPrefab;
    public ScrollRect recipeScrollRect;

    [Header("Kategori Tab'lari")]
    public Button allRecipesTab;
    public Button consumablesTab;
    public Button equipmentTab;
    public Button materialsTab;

    [Header("Secili Tarif Detayi")]
    public GameObject recipeDetailPanel;
    public Image resultItemIcon;
    public TMP_Text resultItemName;
    public TMP_Text resultItemDescription;
    public Image rarityBackground;

    [Header("Malzeme Listesi")]
    public Transform ingredientListContainer;
    public GameObject ingredientEntryPrefab;

    [Header("Craft Butonu")]
    public Button craftButton;
    public TMP_Text craftButtonText;
    public TMP_Text craftCostText;
    public Slider craftProgressBar;
    public TMP_Text craftProgressText;

    [Header("Animasyon")]
    public float openSpeed = 8f;

    // State
    private List<GameObject> spawnedRecipeEntries = new List<GameObject>();
    private List<GameObject> spawnedIngredientEntries = new List<GameObject>();
    private CraftingRecipeData selectedRecipe;
    private ItemCategory currentFilter = ItemCategory.Consumable;
    private bool showAll = true;
    private bool isOpen = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Initialize();

        if (craftingPanel != null)
            craftingPanel.SetActive(false);
    }

    void Update()
    {
        // C tusu ile ac/kapat
        if (Input.GetKeyDown(KeyCode.C))
        {
            Toggle();
        }

        // ESC ile kapat
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            Close();
        }

        // Canvas alpha
        if (canvasGroup != null)
        {
            float targetAlpha = isOpen ? 1f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * openSpeed);
        }

        // Craft progress
        UpdateCraftProgress();
    }

    void Initialize()
    {
        SetupTabs();
        SetupCraftButton();

        if (craftProgressBar != null)
            craftProgressBar.gameObject.SetActive(false);

        if (recipeDetailPanel != null)
            recipeDetailPanel.SetActive(false);
    }

    void SetupTabs()
    {
        if (allRecipesTab != null)
            allRecipesTab.onClick.AddListener(() => SetFilter(ItemCategory.Consumable, true));

        if (consumablesTab != null)
            consumablesTab.onClick.AddListener(() => SetFilter(ItemCategory.Consumable, false));

        if (equipmentTab != null)
            equipmentTab.onClick.AddListener(() => SetFilter(ItemCategory.Equipment, false));

        if (materialsTab != null)
            materialsTab.onClick.AddListener(() => SetFilter(ItemCategory.Material, false));
    }

    void SetupCraftButton()
    {
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(OnCraftClicked);
        }
    }

    // === PUBLIC METODLAR ===

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;

        if (craftingPanel != null)
            craftingPanel.SetActive(true);

        RefreshRecipeList();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        Debug.Log("[CraftingUI] Crafting paneli acildi");
    }

    public void Close()
    {
        isOpen = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();

        Invoke(nameof(HidePanel), 0.3f);

        Debug.Log("[CraftingUI] Crafting paneli kapatildi");
    }

    void HidePanel()
    {
        if (!isOpen && craftingPanel != null)
            craftingPanel.SetActive(false);
    }

    public void SetFilter(ItemCategory category, bool all = false)
    {
        showAll = all;
        currentFilter = category;
        RefreshRecipeList();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    /// <summary>
    /// Tarif listesini yenile
    /// </summary>
    public void RefreshRecipeList()
    {
        // Eski entryleri temizle
        foreach (var entry in spawnedRecipeEntries)
        {
            Destroy(entry);
        }
        spawnedRecipeEntries.Clear();

        if (CraftingManager.Instance == null || recipeListContainer == null || recipeEntryPrefab == null)
            return;

        var recipes = CraftingManager.Instance.GetUnlockedRecipes();

        foreach (var recipe in recipes)
        {
            // Filtre kontrolu
            if (!showAll)
            {
                var itemData = InventoryItem.Create(recipe.resultItem);
                if (itemData.category != currentFilter)
                    continue;
            }

            CreateRecipeEntry(recipe);
        }
    }

    void CreateRecipeEntry(CraftingRecipeData recipe)
    {
        var entryGo = Instantiate(recipeEntryPrefab, recipeListContainer);
        spawnedRecipeEntries.Add(entryGo);

        // Icon
        var icon = entryGo.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && InventorySprites.Instance != null)
        {
            icon.sprite = InventorySprites.Instance.GetSprite(recipe.resultItem);
        }

        // Name
        var nameText = entryGo.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = recipe.recipeName;
            nameText.color = ItemRarityHelper.GetRarityColor(recipe.resultRarity);
        }

        // Can craft indicator
        bool canCraft = CraftingManager.Instance.CanCraft(recipe.recipeId);
        var craftableIndicator = entryGo.transform.Find("CraftableIndicator")?.GetComponent<Image>();
        if (craftableIndicator != null)
        {
            craftableIndicator.color = canCraft ? Color.green : Color.red;
        }

        // Rarity glow
        var glow = entryGo.transform.Find("RarityGlow")?.GetComponent<Image>();
        if (glow != null)
        {
            glow.color = ItemRarityHelper.GetGlowColor(recipe.resultRarity);
            glow.enabled = recipe.resultRarity >= ItemRarity.Rare;
        }

        // Button
        var button = entryGo.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => SelectRecipe(recipe));
        }
    }

    /// <summary>
    /// Tarif sec
    /// </summary>
    public void SelectRecipe(CraftingRecipeData recipe)
    {
        selectedRecipe = recipe;

        if (recipeDetailPanel != null)
            recipeDetailPanel.SetActive(true);

        UpdateRecipeDetail(recipe);
        UpdateIngredientList(recipe);
        UpdateCraftButton(recipe);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    void UpdateRecipeDetail(CraftingRecipeData recipe)
    {
        // Result icon
        if (resultItemIcon != null && InventorySprites.Instance != null)
        {
            resultItemIcon.sprite = InventorySprites.Instance.GetSprite(recipe.resultItem);
        }

        // Result name
        if (resultItemName != null)
        {
            string rarityName = recipe.resultRarity > ItemRarity.Common
                ? $"{ItemRarityHelper.GetRarityName(recipe.resultRarity)} "
                : "";
            resultItemName.text = $"{rarityName}{recipe.recipeName}";
            resultItemName.color = ItemRarityHelper.GetRarityColor(recipe.resultRarity);
        }

        // Description
        if (resultItemDescription != null)
        {
            var itemData = InventoryItem.Create(recipe.resultItem);
            resultItemDescription.text = itemData.description;
        }

        // Rarity background
        if (rarityBackground != null)
        {
            rarityBackground.color = ItemRarityHelper.GetGlowColor(recipe.resultRarity);
        }
    }

    void UpdateIngredientList(CraftingRecipeData recipe)
    {
        // Eski entryleri temizle
        foreach (var entry in spawnedIngredientEntries)
        {
            Destroy(entry);
        }
        spawnedIngredientEntries.Clear();

        if (ingredientListContainer == null || ingredientEntryPrefab == null)
            return;

        var playerItems = InventoryManager.Instance?.GetAllItemInstances() ?? new List<InventoryItemInstance>();

        foreach (var ingredient in recipe.ingredients)
        {
            CreateIngredientEntry(ingredient, playerItems);
        }
    }

    void CreateIngredientEntry(CraftingIngredient ingredient, List<InventoryItemInstance> playerItems)
    {
        var entryGo = Instantiate(ingredientEntryPrefab, ingredientListContainer);
        spawnedIngredientEntries.Add(entryGo);

        // Icon
        var icon = entryGo.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && InventorySprites.Instance != null)
        {
            icon.sprite = InventorySprites.Instance.GetSprite(ingredient.itemType);
        }

        // Name
        var nameText = entryGo.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            string rarityPrefix = ingredient.minRarity > ItemRarity.Common
                ? $"{ItemRarityHelper.GetRarityName(ingredient.minRarity)} "
                : "";
            var itemData = InventoryItem.Create(ingredient.itemType);
            nameText.text = $"{rarityPrefix}{itemData.name}";
        }

        // Amount
        int owned = GetOwnedCount(ingredient.itemType, ingredient.minRarity, playerItems);
        bool hasEnough = owned >= ingredient.amount;

        var amountText = entryGo.transform.Find("Amount")?.GetComponent<TMP_Text>();
        if (amountText != null)
        {
            amountText.text = $"{owned}/{ingredient.amount}";
            amountText.color = hasEnough ? Color.green : Color.red;
        }

        // Background
        var bg = entryGo.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = hasEnough
                ? new Color(0.2f, 0.4f, 0.2f, 0.5f)
                : new Color(0.4f, 0.2f, 0.2f, 0.5f);
        }
    }

    int GetOwnedCount(ItemType type, ItemRarity minRarity, List<InventoryItemInstance> items)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item.itemType == type && item.rarity >= minRarity)
            {
                count += item.stackCount;
            }
        }
        return count;
    }

    void UpdateCraftButton(CraftingRecipeData recipe)
    {
        bool canCraft = CraftingManager.Instance?.CanCraft(recipe.recipeId) ?? false;

        if (craftButton != null)
        {
            craftButton.interactable = canCraft && !CraftingManager.Instance.IsCrafting();
        }

        if (craftButtonText != null)
        {
            craftButtonText.text = canCraft ? "Üret" : "Malzeme Yetersiz";
        }

        if (craftCostText != null)
        {
            if (recipe.craftingCost > 0)
            {
                craftCostText.text = $"Maliyet: {recipe.craftingCost}";
                craftCostText.gameObject.SetActive(true);
            }
            else
            {
                craftCostText.gameObject.SetActive(false);
            }
        }
    }

    void UpdateCraftProgress()
    {
        if (CraftingManager.Instance == null) return;

        bool isCrafting = CraftingManager.Instance.IsCrafting();

        if (craftProgressBar != null)
        {
            craftProgressBar.gameObject.SetActive(isCrafting);

            if (isCrafting)
            {
                craftProgressBar.value = CraftingManager.Instance.GetCraftProgress();
            }
        }

        if (craftProgressText != null)
        {
            craftProgressText.gameObject.SetActive(isCrafting);

            if (isCrafting)
            {
                float remaining = CraftingManager.Instance.GetRemainingCraftTime();
                craftProgressText.text = $"Kalan: {remaining:F1}s";
            }
        }
    }

    void OnCraftClicked()
    {
        if (selectedRecipe == null || CraftingManager.Instance == null)
            return;

        bool success = CraftingManager.Instance.StartCraft(selectedRecipe.recipeId);

        if (success)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButton();

            // Listeyi yenile
            RefreshRecipeList();
            UpdateIngredientList(selectedRecipe);
            UpdateCraftButton(selectedRecipe);
        }
        else
        {
            // Hata sesi
            Debug.Log("[CraftingUI] Craft baslatilamadi");
        }
    }

    public bool IsOpen() => isOpen;
}
