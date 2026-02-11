using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Crafting sistemi yonetici sinifi.
/// Tarif kilidi acma, craft yapma ve malzeme tuketimi islerini yonetir.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Crafting Ayarlari")]
    [Tooltip("ScriptableObject tarifler (opsiyonel)")]
    public List<CraftingRecipe> recipeAssets = new List<CraftingRecipe>();

    [Tooltip("Crafting suresi carpani")]
    public float craftingTimeMultiplier = 1f;

    [Header("Debug")]
    public bool showDebugLog = false;

    // Acilmis tarifler
    private HashSet<string> unlockedRecipes = new HashSet<string>();

    // Aktif crafting islemi
    private CraftingProcess currentCraft = null;

    // Events
    public static event Action<string> OnRecipeUnlocked;
    public static event Action<CraftingRecipeData, InventoryItemInstance> OnCraftCompleted;
    public static event Action<string> OnCraftFailed;
    public static event Action<float> OnCraftProgress;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadUnlockedRecipes();
        UnlockDefaultRecipes();
    }

    void Update()
    {
        // Aktif crafting islemi varsa guncelle
        if (currentCraft != null)
        {
            UpdateCraftingProcess();
        }
    }

    void LoadUnlockedRecipes()
    {
        // SaveManager'dan yukle
        if (SaveManager.Instance != null)
        {
            string saved = SaveManager.Instance.Data.unlockedRecipes;
            if (!string.IsNullOrEmpty(saved))
            {
                string[] ids = saved.Split(',');
                foreach (var id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                        unlockedRecipes.Add(id);
                }
            }
        }
    }

    void UnlockDefaultRecipes()
    {
        // Varsayilan tarifler
        var allRecipes = DefaultCraftingRecipes.GetAllRecipes();

        foreach (var recipe in allRecipes.Values)
        {
            // Baslangicta acik olan tarifleri ekle
            if (!unlockedRecipes.Contains(recipe.recipeId))
            {
                // Temel tarifler varsayilan acik
                if (recipe.resultRarity <= ItemRarity.Uncommon)
                {
                    unlockedRecipes.Add(recipe.recipeId);
                }
            }
        }
    }

    void SaveUnlockedRecipes()
    {
        if (SaveManager.Instance == null) return;

        string saved = string.Join(",", unlockedRecipes);
        SaveManager.Instance.Data.unlockedRecipes = saved;
        SaveManager.Instance.Save();
    }

    // === PUBLIC METODLAR ===

    /// <summary>
    /// Tarif acik mi kontrol et
    /// </summary>
    public bool IsRecipeUnlocked(string recipeId)
    {
        return unlockedRecipes.Contains(recipeId);
    }

    /// <summary>
    /// Tarif kilidi ac
    /// </summary>
    public bool UnlockRecipe(string recipeId)
    {
        if (unlockedRecipes.Contains(recipeId))
            return false;

        unlockedRecipes.Add(recipeId);
        SaveUnlockedRecipes();

        OnRecipeUnlocked?.Invoke(recipeId);

        if (showDebugLog)
            Debug.Log($"[CraftingManager] Tarif acildi: {recipeId}");

        return true;
    }

    /// <summary>
    /// Mevcut tum tarifleri al
    /// </summary>
    public List<CraftingRecipeData> GetAllRecipes()
    {
        var result = new List<CraftingRecipeData>();

        // Kod tabanli tarifler
        foreach (var recipe in DefaultCraftingRecipes.GetAllRecipes().Values)
        {
            result.Add(recipe);
        }

        return result;
    }

    /// <summary>
    /// Acilmis tarifleri al
    /// </summary>
    public List<CraftingRecipeData> GetUnlockedRecipes()
    {
        var result = new List<CraftingRecipeData>();

        foreach (var recipe in DefaultCraftingRecipes.GetAllRecipes().Values)
        {
            if (IsRecipeUnlocked(recipe.recipeId))
            {
                result.Add(recipe);
            }
        }

        return result;
    }

    /// <summary>
    /// Craft yapilabilir mi kontrol et
    /// </summary>
    public bool CanCraft(string recipeId)
    {
        var recipe = DefaultCraftingRecipes.GetRecipe(recipeId);
        if (recipe == null) return false;

        // Tarif acik mi?
        if (!IsRecipeUnlocked(recipeId))
            return false;

        // Malzemeler yeterli mi?
        if (InventoryManager.Instance == null)
            return false;

        var items = InventoryManager.Instance.GetAllItemInstances();
        if (!recipe.CanCraft(items))
            return false;

        // Para yeterli mi? (GameManager kullan)
        if (recipe.craftingCost > 0)
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.GetCoins() < recipe.craftingCost)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Craft baslat
    /// </summary>
    public bool StartCraft(string recipeId)
    {
        if (!CanCraft(recipeId))
        {
            OnCraftFailed?.Invoke(recipeId);
            return false;
        }

        var recipe = DefaultCraftingRecipes.GetRecipe(recipeId);

        // Mevcut craft varsa iptal
        if (currentCraft != null)
        {
            Debug.LogWarning("[CraftingManager] Zaten bir craft islemi devam ediyor");
            return false;
        }

        // Malzemeleri tuket
        ConsumeIngredients(recipe);

        // Para tuket - TODO: GameManager'a SpendCoins ekle
        // Simdilik crafting cost'u atliyoruz
        // if (recipe.craftingCost > 0 && GameManager.Instance != null)
        // {
        //     GameManager.Instance.SpendCoins(recipe.craftingCost);
        // }

        // Aninda craft mi?
        if (recipe.craftingCost <= 0 || craftingTimeMultiplier == 0)
        {
            CompleteCraft(recipe);
            return true;
        }

        // Sureli craft
        currentCraft = new CraftingProcess
        {
            recipeId = recipeId,
            startTime = Time.time,
            duration = recipe.craftingCost * craftingTimeMultiplier
        };

        if (showDebugLog)
            Debug.Log($"[CraftingManager] Craft basladi: {recipe.recipeName}");

        return true;
    }

    /// <summary>
    /// Craft aninda tamamla (sureli craft icin)
    /// </summary>
    public bool InstantCompleteCraft(int gemCost = 0)
    {
        if (currentCraft == null) return false;

        // Gem kontrolu (premium currency)
        // TODO: Gem sistemi eklenebilir

        var recipe = DefaultCraftingRecipes.GetRecipe(currentCraft.recipeId);
        if (recipe != null)
        {
            CompleteCraft(recipe);
        }

        currentCraft = null;
        return true;
    }

    /// <summary>
    /// Craft iptal et
    /// </summary>
    public void CancelCraft()
    {
        if (currentCraft == null) return;

        var recipe = DefaultCraftingRecipes.GetRecipe(currentCraft.recipeId);
        if (recipe != null)
        {
            // Malzemeleri geri ver (kismen veya tamamen)
            RefundIngredients(recipe, 0.5f); // %50 iade
        }

        currentCraft = null;

        if (showDebugLog)
            Debug.Log("[CraftingManager] Craft iptal edildi");
    }

    void UpdateCraftingProcess()
    {
        if (currentCraft == null) return;

        float elapsed = Time.time - currentCraft.startTime;
        float progress = Mathf.Clamp01(elapsed / currentCraft.duration);

        OnCraftProgress?.Invoke(progress);

        if (progress >= 1f)
        {
            var recipe = DefaultCraftingRecipes.GetRecipe(currentCraft.recipeId);
            if (recipe != null)
            {
                CompleteCraft(recipe);
            }
            currentCraft = null;
        }
    }

    void ConsumeIngredients(CraftingRecipeData recipe)
    {
        if (InventoryManager.Instance == null) return;

        foreach (var ingredient in recipe.ingredients)
        {
            InventoryManager.Instance.RemoveItemByType(ingredient.itemType, ingredient.amount);
        }

        if (showDebugLog)
            Debug.Log($"[CraftingManager] Malzemeler tuketildi: {recipe.recipeName}");
    }

    void RefundIngredients(CraftingRecipeData recipe, float refundPercent)
    {
        if (InventoryManager.Instance == null) return;

        foreach (var ingredient in recipe.ingredients)
        {
            int refundAmount = Mathf.FloorToInt(ingredient.amount * refundPercent);
            if (refundAmount > 0)
            {
                var item = InventoryItemInstance.Create(ingredient.itemType, ingredient.minRarity, refundAmount);
                InventoryManager.Instance.TryAddItemInstance(item);
            }
        }
    }

    void CompleteCraft(CraftingRecipeData recipe)
    {
        // Sonuc item'i olustur
        ItemRarity resultRarity = recipe.resultRarity;

        if (recipe.useRandomRarity)
        {
            resultRarity = ItemRarityHelper.RollRarity(10f);
        }

        var resultItem = InventoryItemInstance.Create(
            recipe.resultItem,
            resultRarity,
            recipe.resultAmount
        );

        // Envantere ekle
        bool added = false;
        if (InventoryManager.Instance != null)
        {
            added = InventoryManager.Instance.TryAddItemInstance(resultItem);
        }

        if (added)
        {
            OnCraftCompleted?.Invoke(recipe, resultItem);

            if (showDebugLog)
                Debug.Log($"[CraftingManager] Craft tamamlandi: {resultItem.GetDisplayName()}");

            // Ses
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPowerUp();
            }
        }
        else
        {
            // Envanter dolu - yere drop et
            if (showDebugLog)
                Debug.LogWarning("[CraftingManager] Envanter dolu, item drop edildi");

            // TODO: Player pozisyonuna drop
        }
    }

    /// <summary>
    /// Crafting sureci aktif mi?
    /// </summary>
    public bool IsCrafting()
    {
        return currentCraft != null;
    }

    /// <summary>
    /// Aktif craft ilerlemesi
    /// </summary>
    public float GetCraftProgress()
    {
        if (currentCraft == null) return 0f;

        float elapsed = Time.time - currentCraft.startTime;
        return Mathf.Clamp01(elapsed / currentCraft.duration);
    }

    /// <summary>
    /// Aktif craft kalan sure
    /// </summary>
    public float GetRemainingCraftTime()
    {
        if (currentCraft == null) return 0f;

        float elapsed = Time.time - currentCraft.startTime;
        return Mathf.Max(0f, currentCraft.duration - elapsed);
    }
}

/// <summary>
/// Aktif crafting sureci
/// </summary>
public class CraftingProcess
{
    public string recipeId;
    public float startTime;
    public float duration;
}
