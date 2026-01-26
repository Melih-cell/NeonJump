using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UISetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("NeonJump/Setup Game UI")]
    public static void SetupGameUI()
    {
        // Canvas olustur
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // UIManager
        GameObject uiManagerObj = new GameObject("UIManager");
        UIManager uiManager = uiManagerObj.AddComponent<UIManager>();

        // HUD Panel
        GameObject hudPanel = CreatePanel(canvasObj.transform, "HUDPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(400, 100));

        // Score Text
        GameObject scoreObj = CreateTMPText(hudPanel.transform, "ScoreText", "0", 48, TextAlignmentOptions.Left);
        RectTransform scoreRT = scoreObj.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1);
        scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 1);
        scoreRT.anchoredPosition = new Vector2(20, -20);
        scoreRT.sizeDelta = new Vector2(200, 60);
        uiManager.scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

        // Coin Text
        GameObject coinObj = CreateTMPText(hudPanel.transform, "CoinText", "0", 32, TextAlignmentOptions.Left);
        RectTransform coinRT = coinObj.GetComponent<RectTransform>();
        coinRT.anchorMin = new Vector2(0, 1);
        coinRT.anchorMax = new Vector2(0, 1);
        coinRT.pivot = new Vector2(0, 1);
        coinRT.anchoredPosition = new Vector2(20, -80);
        coinRT.sizeDelta = new Vector2(150, 40);
        TextMeshProUGUI coinTMP = coinObj.GetComponent<TextMeshProUGUI>();
        coinTMP.color = new Color(1f, 0.84f, 0f); // Gold
        uiManager.coinText = coinTMP;

        // Hearts Container
        GameObject heartsContainer = new GameObject("HeartsContainer");
        heartsContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform heartsRT = heartsContainer.AddComponent<RectTransform>();
        heartsRT.anchorMin = new Vector2(1, 1);
        heartsRT.anchorMax = new Vector2(1, 1);
        heartsRT.pivot = new Vector2(1, 1);
        heartsRT.anchoredPosition = new Vector2(-20, -20);
        heartsRT.sizeDelta = new Vector2(200, 50);
        HorizontalLayoutGroup hlg = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.UpperRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        uiManager.heartsContainer = heartsRT;

        // Game Over Panel
        GameObject gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 400));
        Image goImage = gameOverPanel.GetComponent<Image>();
        goImage.color = new Color(0.1f, 0.05f, 0.15f, 0.95f);
        uiManager.gameOverPanel = gameOverPanel;

        // Game Over Title
        GameObject goTitle = CreateTMPText(gameOverPanel.transform, "Title", "GAME OVER", 64, TextAlignmentOptions.Center);
        RectTransform goTitleRT = goTitle.GetComponent<RectTransform>();
        goTitleRT.anchorMin = new Vector2(0.5f, 1);
        goTitleRT.anchorMax = new Vector2(0.5f, 1);
        goTitleRT.anchoredPosition = new Vector2(0, -60);
        goTitleRT.sizeDelta = new Vector2(400, 80);
        goTitle.GetComponent<TextMeshProUGUI>().color = Color.red;

        // Game Over Score
        GameObject goScore = CreateTMPText(gameOverPanel.transform, "ScoreText", "Skor: 0", 36, TextAlignmentOptions.Center);
        RectTransform goScoreRT = goScore.GetComponent<RectTransform>();
        goScoreRT.anchoredPosition = new Vector2(0, 20);
        goScoreRT.sizeDelta = new Vector2(400, 50);
        uiManager.gameOverScoreText = goScore.GetComponent<TextMeshProUGUI>();

        // Game Over High Score
        GameObject goHighScore = CreateTMPText(gameOverPanel.transform, "HighScoreText", "En Yuksek: 0", 28, TextAlignmentOptions.Center);
        RectTransform goHighScoreRT = goHighScore.GetComponent<RectTransform>();
        goHighScoreRT.anchoredPosition = new Vector2(0, -30);
        goHighScoreRT.sizeDelta = new Vector2(400, 40);
        goHighScore.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0f);
        uiManager.gameOverHighScoreText = goHighScore.GetComponent<TextMeshProUGUI>();

        // Restart Button
        CreateButton(gameOverPanel.transform, "RestartButton", "Tekrar Oyna", new Vector2(0, -100), () => {
            if (UIManager.Instance != null) UIManager.Instance.RestartGame();
        });

        // Main Menu Button
        CreateButton(gameOverPanel.transform, "MenuButton", "Ana Menu", new Vector2(0, -160), () => {
            if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu();
        });

        // Win Panel
        GameObject winPanel = CreatePanel(canvasObj.transform, "WinPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 400));
        Image winImage = winPanel.GetComponent<Image>();
        winImage.color = new Color(0.05f, 0.15f, 0.1f, 0.95f);
        uiManager.winPanel = winPanel;

        // Win Title
        GameObject winTitle = CreateTMPText(winPanel.transform, "Title", "KAZANDIN!", 64, TextAlignmentOptions.Center);
        RectTransform winTitleRT = winTitle.GetComponent<RectTransform>();
        winTitleRT.anchorMin = new Vector2(0.5f, 1);
        winTitleRT.anchorMax = new Vector2(0.5f, 1);
        winTitleRT.anchoredPosition = new Vector2(0, -60);
        winTitleRT.sizeDelta = new Vector2(400, 80);
        winTitle.GetComponent<TextMeshProUGUI>().color = Color.green;

        // Win Score
        GameObject winScore = CreateTMPText(winPanel.transform, "ScoreText", "Skor: 0", 36, TextAlignmentOptions.Center);
        RectTransform winScoreRT = winScore.GetComponent<RectTransform>();
        winScoreRT.anchoredPosition = new Vector2(0, 20);
        winScoreRT.sizeDelta = new Vector2(400, 50);
        uiManager.winScoreText = winScore.GetComponent<TextMeshProUGUI>();

        // Win Time
        GameObject winTime = CreateTMPText(winPanel.transform, "TimeText", "Sure: 00:00", 28, TextAlignmentOptions.Center);
        RectTransform winTimeRT = winTime.GetComponent<RectTransform>();
        winTimeRT.anchoredPosition = new Vector2(0, -30);
        winTimeRT.sizeDelta = new Vector2(400, 40);
        uiManager.winTimeText = winTime.GetComponent<TextMeshProUGUI>();

        // Win Buttons
        CreateButton(winPanel.transform, "NextButton", "Sonraki Bolum", new Vector2(0, -100), null);
        CreateButton(winPanel.transform, "MenuButton", "Ana Menu", new Vector2(0, -160), () => {
            if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu();
        });

        // Pause Panel
        GameObject pausePanel = CreatePanel(canvasObj.transform, "PausePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 350));
        Image pauseImage = pausePanel.GetComponent<Image>();
        pauseImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        uiManager.pausePanel = pausePanel;

        // Pause Title
        GameObject pauseTitle = CreateTMPText(pausePanel.transform, "Title", "DURAKLATILDI", 48, TextAlignmentOptions.Center);
        RectTransform pauseTitleRT = pauseTitle.GetComponent<RectTransform>();
        pauseTitleRT.anchorMin = new Vector2(0.5f, 1);
        pauseTitleRT.anchorMax = new Vector2(0.5f, 1);
        pauseTitleRT.anchoredPosition = new Vector2(0, -50);
        pauseTitleRT.sizeDelta = new Vector2(350, 60);

        // Pause Buttons
        CreateButton(pausePanel.transform, "ResumeButton", "Devam Et", new Vector2(0, -20), () => {
            if (UIManager.Instance != null) UIManager.Instance.ResumeGame();
        });
        CreateButton(pausePanel.transform, "RestartButton", "Yeniden Basla", new Vector2(0, -80), () => {
            if (UIManager.Instance != null) UIManager.Instance.RestartGame();
        });
        CreateButton(pausePanel.transform, "MenuButton", "Ana Menu", new Vector2(0, -140), () => {
            if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu();
        });

        // ParticleManager
        GameObject particleManagerObj = new GameObject("ParticleManager");
        particleManagerObj.AddComponent<ParticleManager>();

        // Panelleri gizle
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        pausePanel.SetActive(false);

        Debug.Log("Game UI setup completed!");
        EditorUtility.SetDirty(canvasObj);
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        return panel;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return textObj;
    }

    static void CreateButton(Transform parent, string name, string text, Vector2 position, System.Action onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(250, 50);

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.2f, 0.5f, 1f);

        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.5f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.3f, 0.7f, 1f);
        colors.pressedColor = new Color(0.2f, 0.1f, 0.3f, 1f);
        btn.colors = colors;

        GameObject textObj = CreateTMPText(buttonObj.transform, "Text", text, 24, TextAlignmentOptions.Center);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;
    }

    [MenuItem("NeonJump/Setup Main Menu")]
    public static void SetupMainMenu()
    {
        // Canvas olustur
        GameObject canvasObj = new GameObject("MenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // MainMenuManager
        GameObject menuManagerObj = new GameObject("MainMenuManager");
        MainMenuManager menuManager = menuManagerObj.AddComponent<MainMenuManager>();

        // Title
        GameObject titleObj = CreateTMPText(canvasObj.transform, "TitleText", "NEON JUMP", 72, TextAlignmentOptions.Center);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -150);
        titleRT.sizeDelta = new Vector2(600, 100);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.color = new Color(0f, 1f, 1f); // Cyan
        menuManager.titleText = titleTMP;

        // High Score
        GameObject highScoreObj = CreateTMPText(canvasObj.transform, "HighScoreText", "En Yuksek Skor: 0", 28, TextAlignmentOptions.Center);
        RectTransform highScoreRT = highScoreObj.GetComponent<RectTransform>();
        highScoreRT.anchorMin = new Vector2(0.5f, 1);
        highScoreRT.anchorMax = new Vector2(0.5f, 1);
        highScoreRT.anchoredPosition = new Vector2(0, -250);
        highScoreRT.sizeDelta = new Vector2(400, 40);
        highScoreObj.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0f);
        menuManager.highScoreText = highScoreObj.GetComponent<TextMeshProUGUI>();

        // Play Button
        CreateButton(canvasObj.transform, "PlayButton", "OYNA", new Vector2(0, 0), () => {
            if (MainMenuManager.Instance != null) MainMenuManager.Instance.PlayGame();
        });

        // Quit Button
        CreateButton(canvasObj.transform, "QuitButton", "CIKIS", new Vector2(0, -70), () => {
            if (MainMenuManager.Instance != null) MainMenuManager.Instance.QuitGame();
        });

        Debug.Log("Main Menu setup completed!");
        EditorUtility.SetDirty(canvasObj);
    }
#endif
}
