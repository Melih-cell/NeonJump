using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;

public class RuntimeUISetup : MonoBehaviour
{
    void Awake()
    {
        // UIManager yoksa olustur
        if (FindObjectOfType<UIManager>() == null)
        {
            SetupGameUI();
        }

        // ParticleManager yoksa olustur
        if (FindObjectOfType<ParticleManager>() == null)
        {
            GameObject pmObj = new GameObject("ParticleManager");
            pmObj.AddComponent<ParticleManager>();
        }

        // NeonHUD olustur (gelismis HUD sistemi)
        if (NeonHUD.Instance == null)
        {
            GameObject neonHudObj = new GameObject("NeonHUD");
            neonHudObj.AddComponent<NeonHUD>();
        }

        // Mobil kontroller (sadece mobilde aktif olur)
        if (MobileControls.Instance == null)
        {
            bool isMobile = Application.isMobilePlatform ||
                            UnityEngine.InputSystem.Touchscreen.current != null;
            if (isMobile)
            {
                GameObject mobileCtrlObj = new GameObject("MobileControls");
                mobileCtrlObj.AddComponent<MobileControls>();
            }
        }
    }

    void SetupGameUI()
    {
        // Canvas olustur
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem - Yeni Input System ile uyumlu
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // UIManager
        GameObject uiManagerObj = new GameObject("UIManager");
        UIManager uiManager = uiManagerObj.AddComponent<UIManager>();

        // === HUD ===
        // HUD Background Panel (sol ust)
        GameObject hudBg = new GameObject("HUDBackground");
        hudBg.transform.SetParent(canvasObj.transform, false);
        RectTransform hudBgRT = hudBg.AddComponent<RectTransform>();
        hudBgRT.anchorMin = new Vector2(0, 1);
        hudBgRT.anchorMax = new Vector2(0, 1);
        hudBgRT.pivot = new Vector2(0, 1);
        hudBgRT.anchoredPosition = new Vector2(10, -10);
        hudBgRT.sizeDelta = new Vector2(220, 120);
        Image hudBgImg = hudBg.AddComponent<Image>();
        hudBgImg.color = new Color(0, 0, 0, 0.5f);

        // Score Label
        GameObject scoreLabelObj = CreateTMPText(hudBg.transform, "ScoreLabel", "SKOR", 20);
        RectTransform scoreLabelRT = scoreLabelObj.GetComponent<RectTransform>();
        scoreLabelRT.anchorMin = new Vector2(0, 1);
        scoreLabelRT.anchorMax = new Vector2(0, 1);
        scoreLabelRT.pivot = new Vector2(0, 1);
        scoreLabelRT.anchoredPosition = new Vector2(15, -10);
        scoreLabelRT.sizeDelta = new Vector2(100, 25);
        scoreLabelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        scoreLabelObj.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

        // Score Text (sol ust)
        GameObject scoreObj = CreateTMPText(hudBg.transform, "ScoreText", "0", 42);
        RectTransform scoreRT = scoreObj.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1);
        scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 1);
        scoreRT.anchoredPosition = new Vector2(15, -32);
        scoreRT.sizeDelta = new Vector2(190, 50);
        scoreObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        uiManager.scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

        // Coin Icon (basit kare)
        GameObject coinIcon = new GameObject("CoinIcon");
        coinIcon.transform.SetParent(hudBg.transform, false);
        RectTransform coinIconRT = coinIcon.AddComponent<RectTransform>();
        coinIconRT.anchorMin = new Vector2(0, 1);
        coinIconRT.anchorMax = new Vector2(0, 1);
        coinIconRT.pivot = new Vector2(0, 1);
        coinIconRT.anchoredPosition = new Vector2(15, -85);
        coinIconRT.sizeDelta = new Vector2(25, 25);
        Image coinIconImg = coinIcon.AddComponent<Image>();
        coinIconImg.color = new Color(1f, 0.84f, 0f);

        // Coin Text (sol ust, skor altinda)
        GameObject coinObj = CreateTMPText(hudBg.transform, "CoinText", "x 0", 28);
        RectTransform coinRT = coinObj.GetComponent<RectTransform>();
        coinRT.anchorMin = new Vector2(0, 1);
        coinRT.anchorMax = new Vector2(0, 1);
        coinRT.pivot = new Vector2(0, 1);
        coinRT.anchoredPosition = new Vector2(45, -82);
        coinRT.sizeDelta = new Vector2(150, 35);
        TextMeshProUGUI coinTMP = coinObj.GetComponent<TextMeshProUGUI>();
        coinTMP.alignment = TextAlignmentOptions.Left;
        coinTMP.color = new Color(1f, 0.84f, 0f);
        uiManager.coinText = coinTMP;

        // Hearts Background (sag ust)
        GameObject heartsBg = new GameObject("HeartsBackground");
        heartsBg.transform.SetParent(canvasObj.transform, false);
        RectTransform heartsBgRT = heartsBg.AddComponent<RectTransform>();
        heartsBgRT.anchorMin = new Vector2(1, 1);
        heartsBgRT.anchorMax = new Vector2(1, 1);
        heartsBgRT.pivot = new Vector2(1, 1);
        heartsBgRT.anchoredPosition = new Vector2(-10, -10);
        heartsBgRT.sizeDelta = new Vector2(180, 60);
        Image heartsBgImg = heartsBg.AddComponent<Image>();
        heartsBgImg.color = new Color(0, 0, 0, 0.5f);

        // Hearts Container (sag ust)
        GameObject heartsContainer = new GameObject("HeartsContainer");
        heartsContainer.transform.SetParent(heartsBg.transform, false);
        RectTransform heartsRT = heartsContainer.AddComponent<RectTransform>();
        heartsRT.anchorMin = new Vector2(0.5f, 0.5f);
        heartsRT.anchorMax = new Vector2(0.5f, 0.5f);
        heartsRT.pivot = new Vector2(0.5f, 0.5f);
        heartsRT.anchoredPosition = Vector2.zero;
        heartsRT.sizeDelta = new Vector2(160, 50);
        HorizontalLayoutGroup hlg = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        uiManager.heartsContainer = heartsRT;

        // === GAME OVER PANEL ===
        GameObject gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel", new Color(0.1f, 0.05f, 0.15f, 0.95f));
        uiManager.gameOverPanel = gameOverPanel;

        GameObject goTitle = CreateTMPText(gameOverPanel.transform, "Title", "GAME OVER", 64);
        RectTransform goTitleRT = goTitle.GetComponent<RectTransform>();
        goTitleRT.anchoredPosition = new Vector2(0, 120);
        goTitle.GetComponent<TextMeshProUGUI>().color = Color.red;

        GameObject goScore = CreateTMPText(gameOverPanel.transform, "ScoreText", "Skor: 0", 36);
        goScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
        uiManager.gameOverScoreText = goScore.GetComponent<TextMeshProUGUI>();

        GameObject goHighScore = CreateTMPText(gameOverPanel.transform, "HighScoreText", "En Yuksek: 0", 28);
        RectTransform goHighScoreRT = goHighScore.GetComponent<RectTransform>();
        goHighScoreRT.anchoredPosition = new Vector2(0, -10);
        goHighScore.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0f);
        uiManager.gameOverHighScoreText = goHighScore.GetComponent<TextMeshProUGUI>();

        CreateButton(gameOverPanel.transform, "RestartButton", "Tekrar Oyna", new Vector2(0, -80), uiManager.RestartGame);
        CreateButton(gameOverPanel.transform, "MenuButton", "Ana Menu", new Vector2(0, -140), uiManager.GoToMainMenu);

        // === WIN PANEL ===
        GameObject winPanel = CreatePanel(canvasObj.transform, "WinPanel", new Color(0.05f, 0.15f, 0.1f, 0.95f));
        uiManager.winPanel = winPanel;

        GameObject winTitle = CreateTMPText(winPanel.transform, "Title", "KAZANDIN!", 64);
        RectTransform winTitleRT = winTitle.GetComponent<RectTransform>();
        winTitleRT.anchoredPosition = new Vector2(0, 120);
        winTitle.GetComponent<TextMeshProUGUI>().color = Color.green;

        GameObject winScore = CreateTMPText(winPanel.transform, "ScoreText", "Skor: 0", 36);
        winScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
        uiManager.winScoreText = winScore.GetComponent<TextMeshProUGUI>();

        GameObject winTime = CreateTMPText(winPanel.transform, "TimeText", "Sure: 00:00", 28);
        winTime.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);
        uiManager.winTimeText = winTime.GetComponent<TextMeshProUGUI>();

        CreateButton(winPanel.transform, "RestartButton", "Tekrar Oyna", new Vector2(0, -80), uiManager.RestartGame);
        CreateButton(winPanel.transform, "MenuButton", "Ana Menu", new Vector2(0, -140), uiManager.GoToMainMenu);

        // === PAUSE PANEL ===
        GameObject pausePanel = CreatePanel(canvasObj.transform, "PausePanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
        uiManager.pausePanel = pausePanel;

        GameObject pauseTitle = CreateTMPText(pausePanel.transform, "Title", "DURAKLATILDI", 48);
        pauseTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        CreateButton(pausePanel.transform, "ResumeButton", "Devam Et", new Vector2(0, 0), uiManager.ResumeGame);
        CreateButton(pausePanel.transform, "RestartButton", "Yeniden Basla", new Vector2(0, -60), uiManager.RestartGame);
        CreateButton(pausePanel.transform, "MenuButton", "Ana Menu", new Vector2(0, -120), uiManager.GoToMainMenu);

        // Panelleri gizle
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        pausePanel.SetActive(false);
    }

    GameObject CreatePanel(Transform parent, string name, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500, 400);

        Image img = panel.AddComponent<Image>();
        img.color = bgColor;

        return panel;
    }

    GameObject CreateTMPText(Transform parent, string name, string text, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 60);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return textObj;
    }

    void CreateButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
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

        if (onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        GameObject textObj = CreateTMPText(buttonObj.transform, "Text", text, 24);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;
    }
}
