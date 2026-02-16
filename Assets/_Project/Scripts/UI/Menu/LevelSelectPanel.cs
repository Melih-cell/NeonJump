using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Level secim ekrani.
/// Grid layout ile level butonlari gosterir, kilitli/acik durumu ve yildizlari yonetir.
/// </summary>
public class LevelSelectPanel : MonoBehaviour
{
    [Header("Panel Referanslari")]
    public GameObject panelRoot;
    public Transform levelGridContainer;
    public GameObject levelButtonPrefab;

    [Header("Level Verileri")]
    public List<LevelInfo> levels = new List<LevelInfo>();

    [Header("UI Referanslari")]
    public TMP_Text titleText;
    public TMP_Text totalStarsText;
    public Button backButton;
    public Button nextPageButton;
    public Button prevPageButton;
    public TMP_Text pageText;

    [Header("Ayarlar")]
    public int levelsPerPage = 10;
    public int gridColumns = 5;

    [Header("Neon Stili")]
    public Color unlockedColor = new Color(0f, 1f, 1f);
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
    public Color starFilledColor = new Color(1f, 0.84f, 0f);
    public Color starEmptyColor = new Color(0.3f, 0.3f, 0.3f);

    private int currentPage = 0;
    private List<LevelButtonUI> levelButtons = new List<LevelButtonUI>();

    void Start()
    {
        InitializeLevels();
        SetupListeners();
    }

    void InitializeLevels()
    {
        // Eger level listesi bos ise varsayilan olustur
        if (levels.Count == 0)
        {
            CreateDefaultLevels();
        }

        // Kaydedilmis verileri yukle
        LoadLevelProgress();

        // Grid'i olustur
        CreateLevelGrid();

        // Sayfayi goster
        ShowPage(0);
    }

    void CreateDefaultLevels()
    {
        // 20 level olustur
        for (int i = 0; i < 20; i++)
        {
            levels.Add(new LevelInfo
            {
                levelNumber = i + 1,
                levelName = $"Level {i + 1}",
                sceneName = "1",  // Tum leveller ayni sahneyi yukluyebilir (simdilik)
                requiredStars = i * 2,      // Her level icin 2 yildiz daha gerekli
                isUnlocked = (i == 0),      // Sadece ilk level acik
                earnedStars = 0
            });
        }
    }

    void LoadLevelProgress()
    {
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.Data.InitializeLevelData();

        for (int i = 0; i < levels.Count; i++)
        {
            if (i < SaveManager.Instance.Data.levelStars.Length)
            {
                levels[i].earnedStars = SaveManager.Instance.Data.levelStars[i];
            }

            if (i < SaveManager.Instance.Data.levelsUnlocked.Length)
            {
                levels[i].isUnlocked = SaveManager.Instance.Data.levelsUnlocked[i];
            }

            // Ilk level her zaman acik
            if (i == 0) levels[i].isUnlocked = true;
        }

        // Yildiz sayisina gore levelleri ac
        int totalStars = SaveManager.Instance.GetTotalStars();
        for (int i = 0; i < levels.Count; i++)
        {
            if (totalStars >= levels[i].requiredStars)
            {
                levels[i].isUnlocked = true;
                SaveManager.Instance.UnlockLevel(i);
            }
        }
    }

    void CreateLevelGrid()
    {
        // Eski butonlari temizle
        foreach (Transform child in levelGridContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();

        // Yeni butonlar olustur
        for (int i = 0; i < levelsPerPage; i++)
        {
            GameObject buttonObj;

            if (levelButtonPrefab != null)
            {
                buttonObj = Instantiate(levelButtonPrefab, levelGridContainer);
            }
            else
            {
                buttonObj = CreateDefaultLevelButton();
                buttonObj.transform.SetParent(levelGridContainer, false);
            }

            var buttonUI = buttonObj.GetComponent<LevelButtonUI>();
            if (buttonUI == null)
            {
                buttonUI = buttonObj.AddComponent<LevelButtonUI>();
            }

            levelButtons.Add(buttonUI);
        }
    }

    GameObject CreateDefaultLevelButton()
    {
        // Prefab yoksa basit bir buton olustur
        GameObject button = new GameObject("LevelButton");

        // Background
        var image = button.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.15f);

        // Button component
        var btn = button.AddComponent<Button>();
        btn.targetGraphic = image;

        // Layout
        var layout = button.AddComponent<LayoutElement>();
        layout.preferredWidth = 120;
        layout.preferredHeight = 120;

        // Level number text
        GameObject textObj = new GameObject("LevelNumber");
        textObj.transform.SetParent(button.transform, false);
        var tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "1";
        tmpText.fontSize = 36;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 40);
        textRect.offsetMax = new Vector2(-10, -10);

        // Stars container
        GameObject starsObj = new GameObject("Stars");
        starsObj.transform.SetParent(button.transform, false);
        var starsRect = starsObj.AddComponent<RectTransform>();
        starsRect.anchorMin = new Vector2(0, 0);
        starsRect.anchorMax = new Vector2(1, 0);
        starsRect.pivot = new Vector2(0.5f, 0);
        starsRect.anchoredPosition = new Vector2(0, 10);
        starsRect.sizeDelta = new Vector2(0, 25);

        var starsLayout = starsObj.AddComponent<HorizontalLayoutGroup>();
        starsLayout.spacing = 5;
        starsLayout.childAlignment = TextAnchor.MiddleCenter;
        starsLayout.childForceExpandWidth = false;
        starsLayout.childForceExpandHeight = false;

        // 3 yildiz olustur
        for (int i = 0; i < 3; i++)
        {
            GameObject star = new GameObject($"Star{i}");
            star.transform.SetParent(starsObj.transform, false);
            var starText = star.AddComponent<TextMeshProUGUI>();
            starText.text = "\u2605"; // Yildiz karakteri
            starText.fontSize = 20;
            starText.color = starEmptyColor;

            var starLayout = star.AddComponent<LayoutElement>();
            starLayout.preferredWidth = 25;
            starLayout.preferredHeight = 25;
        }

        // Lock icon
        GameObject lockObj = new GameObject("LockIcon");
        lockObj.transform.SetParent(button.transform, false);
        var lockText = lockObj.AddComponent<TextMeshProUGUI>();
        lockText.text = "\U0001F512"; // Kilit emoji
        lockText.fontSize = 40;
        lockText.alignment = TextAlignmentOptions.Center;
        lockText.color = lockedColor;

        var lockRect = lockObj.GetComponent<RectTransform>();
        lockRect.anchorMin = Vector2.zero;
        lockRect.anchorMax = Vector2.one;
        lockRect.offsetMin = Vector2.zero;
        lockRect.offsetMax = Vector2.zero;

        lockObj.SetActive(false);

        // LevelButtonUI komponenti ekle
        var buttonUI = button.AddComponent<LevelButtonUI>();
        buttonUI.button = btn;
        buttonUI.levelNumberText = tmpText;
        buttonUI.lockIcon = lockObj;
        buttonUI.starImages = new List<TMP_Text>();

        foreach (Transform starTrans in starsObj.transform)
        {
            var starTmp = starTrans.GetComponent<TMP_Text>();
            if (starTmp != null)
                buttonUI.starImages.Add(starTmp);
        }

        return button;
    }

    void SetupListeners()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PrevPage);
    }

    public void ShowPage(int page)
    {
        int maxPages = Mathf.CeilToInt((float)levels.Count / levelsPerPage);
        currentPage = Mathf.Clamp(page, 0, maxPages - 1);

        int startIndex = currentPage * levelsPerPage;

        for (int i = 0; i < levelButtons.Count; i++)
        {
            int levelIndex = startIndex + i;

            if (levelIndex < levels.Count)
            {
                levelButtons[i].gameObject.SetActive(true);
                SetupLevelButton(levelButtons[i], levels[levelIndex], levelIndex);
            }
            else
            {
                levelButtons[i].gameObject.SetActive(false);
            }
        }

        // Sayfa gostergesi
        if (pageText != null)
            pageText.text = $"{currentPage + 1} / {maxPages}";

        // Sayfa butonlari
        if (prevPageButton != null)
            prevPageButton.interactable = currentPage > 0;
        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < maxPages - 1;

        // Toplam yildiz
        UpdateTotalStars();
    }

    void SetupLevelButton(LevelButtonUI buttonUI, LevelInfo level, int index)
    {
        // Level numarasi
        if (buttonUI.levelNumberText != null)
            buttonUI.levelNumberText.text = level.levelNumber.ToString();

        // Kilit durumu
        bool isLocked = !level.isUnlocked;
        if (buttonUI.lockIcon != null)
            buttonUI.lockIcon.SetActive(isLocked);

        if (buttonUI.levelNumberText != null)
            buttonUI.levelNumberText.gameObject.SetActive(!isLocked);

        // Buton rengi
        var image = buttonUI.GetComponent<Image>();
        if (image != null)
        {
            image.color = isLocked ? lockedColor : unlockedColor;
        }

        // Yildizlar
        for (int i = 0; i < buttonUI.starImages.Count; i++)
        {
            if (i < level.earnedStars)
                buttonUI.starImages[i].color = starFilledColor;
            else
                buttonUI.starImages[i].color = starEmptyColor;

            // Kilitli ise yildizlari gizle
            buttonUI.starImages[i].transform.parent.gameObject.SetActive(!isLocked);
        }

        // Buton click
        if (buttonUI.button != null)
        {
            buttonUI.button.onClick.RemoveAllListeners();

            if (!isLocked)
            {
                int levelIdx = index; // Closure icin kopyala
                buttonUI.button.onClick.AddListener(() => OnLevelClicked(levelIdx));
                buttonUI.button.interactable = true;
            }
            else
            {
                buttonUI.button.interactable = false;
            }
        }
    }

    void UpdateTotalStars()
    {
        if (totalStarsText == null) return;

        int total = 0;
        int max = levels.Count * 3;

        foreach (var level in levels)
            total += level.earnedStars;

        totalStarsText.text = $"\u2605 {total} / {max}";
    }

    // === BUTON CALLBACK'LERI ===

    void OnLevelClicked(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count) return;

        var level = levels[levelIndex];

        if (!level.isUnlocked)
        {
            Debug.Log($"Level {level.levelNumber} kilitli!");
            return;
        }

        Debug.Log($"Level {level.levelNumber} yukleniyor: {level.sceneName}");

        // Current level'i kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.currentLevel = levelIndex + 1;
            SaveManager.Instance.Save();
        }

        // Sahneyi yukle
        if (!string.IsNullOrEmpty(level.sceneName))
        {
            SceneManager.LoadScene(level.sceneName);
        }
    }

    void OnBackClicked()
    {
        Hide();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    void NextPage()
    {
        ShowPage(currentPage + 1);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    void PrevPage()
    {
        ShowPage(currentPage - 1);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    // === PANEL KONTROLU ===

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        LoadLevelProgress();
        ShowPage(0);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Level tamamlandiginda cagrilir
    /// </summary>
    public void OnLevelCompleted(int levelIndex, int starsEarned)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count) return;

        // Yildizlari guncelle (sadece daha yuksekse)
        if (starsEarned > levels[levelIndex].earnedStars)
        {
            levels[levelIndex].earnedStars = starsEarned;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetLevelStars(levelIndex, starsEarned);
            }
        }

        // Sonraki level'i ac
        if (levelIndex + 1 < levels.Count)
        {
            levels[levelIndex + 1].isUnlocked = true;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.UnlockLevel(levelIndex + 1);
            }
        }

        // Event fire
        GameEvents.RaiseLevelCompleted(levelIndex + 1, starsEarned);
    }
}

/// <summary>
/// Tek bir level'in bilgisi
/// </summary>
[System.Serializable]
public class LevelInfo
{
    public int levelNumber;
    public string levelName;
    public string sceneName;
    public int requiredStars;
    public bool isUnlocked;
    public int earnedStars;
    public Sprite thumbnail;
}

/// <summary>
/// Level buton UI komponenti
/// </summary>
public class LevelButtonUI : MonoBehaviour
{
    public Button button;
    public TMP_Text levelNumberText;
    public GameObject lockIcon;
    public List<TMP_Text> starImages = new List<TMP_Text>();
    public Image backgroundImage;
}
