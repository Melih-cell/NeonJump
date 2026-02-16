using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;

public class MainMenuSetup : MonoBehaviour
{
    void Awake()
    {
        SetupMainMenu();
    }

    void SetupMainMenu()
    {
        // Kamera ayarla
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.backgroundColor = new Color(0.05f, 0.02f, 0.15f);
        cam.transform.position = new Vector3(0, 0, -10);

        // Canvas
        GameObject canvasObj = new GameObject("MenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Safe area handling - notch'lu cihazlar icin
        GameObject safeArea = new GameObject("SafeArea");
        safeArea.transform.SetParent(canvasObj.transform, false);
        RectTransform safeAreaRT = safeArea.AddComponent<RectTransform>();
        safeAreaRT.anchorMin = Vector2.zero;
        safeAreaRT.anchorMax = Vector2.one;
        safeAreaRT.offsetMin = Vector2.zero;
        safeAreaRT.offsetMax = Vector2.zero;
        safeArea.AddComponent<SafeAreaHandler>();

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

        // === Background ===
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRT = bgPanel.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        Image bgImg = bgPanel.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.02f, 0.15f, 1f);

        // === Stars/Particles Background ===
        CreateStarsBackground(canvasObj.transform);

        // === Title ===
        GameObject titleObj = CreateText(canvasObj.transform, "TitleText", "NEON JUMP", 120,
            new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(900, 150),
            new Color(0f, 1f, 1f), FontStyles.Bold);
        menuManager.titleText = titleObj.GetComponent<TextMeshProUGUI>();

        // Title glow effect (shadow)
        Shadow titleShadow = titleObj.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0.5f, 0.5f, 0.5f);
        titleShadow.effectDistance = new Vector2(4, -4);

        // === Subtitle ===
        CreateText(canvasObj.transform, "SubtitleText", "Bir Platform Macerasi", 36,
            new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(600, 50),
            new Color(1f, 0.5f, 1f), FontStyles.Italic);

        // === High Score ===
        GameObject highScoreObj = CreateText(canvasObj.transform, "HighScoreText", "En Yuksek Skor: 0", 40,
            new Vector2(0.5f, 1), new Vector2(0, -340), new Vector2(500, 55),
            new Color(1f, 0.84f, 0f), FontStyles.Bold);
        menuManager.highScoreText = highScoreObj.GetComponent<TextMeshProUGUI>();

        // === Buttons Container ===
        GameObject buttonsPanel = new GameObject("ButtonsPanel");
        buttonsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform buttonsRT = buttonsPanel.AddComponent<RectTransform>();
        buttonsRT.anchorMin = buttonsRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRT.pivot = new Vector2(0.5f, 0.5f);
        buttonsRT.anchoredPosition = new Vector2(0, -80);
        buttonsRT.sizeDelta = new Vector2(400, 300);

        // Play Button
        CreateMenuButton(buttonsPanel.transform, "PlayButton", "OYNA", new Vector2(0, 80),
            new Color(0.2f, 0.7f, 0.3f), new Color(0.3f, 0.9f, 0.4f), () => {
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.PlayGame();
            });

        // Reset Score Button
        CreateMenuButton(buttonsPanel.transform, "ResetButton", "Skoru Sifirla", new Vector2(0, 0),
            new Color(0.7f, 0.6f, 0.2f), new Color(0.9f, 0.8f, 0.3f), () => {
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.ResetHighScore();
            });

        // Quit Button
        CreateMenuButton(buttonsPanel.transform, "QuitButton", "CIKIS", new Vector2(0, -80),
            new Color(0.7f, 0.2f, 0.2f), new Color(0.9f, 0.3f, 0.3f), () => {
                if (MainMenuManager.Instance != null) MainMenuManager.Instance.QuitGame();
            });

        // === Controls Info ===
        CreateText(canvasObj.transform, "ControlsText",
            "Kontroller: A/D veya Ok Tuslari - Hareket | Space/W - Zipla | Esc - Duraklat", 24,
            new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(1000, 40),
            new Color(0.6f, 0.6f, 0.6f), FontStyles.Normal);

        // === Version ===
        CreateText(canvasObj.transform, "VersionText", "v1.0", 20,
            new Vector2(1, 0), new Vector2(-30, 30), new Vector2(100, 30),
            new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal);

        Debug.Log("Main Menu Setup Complete!");
    }

    void CreateStarsBackground(Transform parent)
    {
        // Basit yildiz efekti - kucuk beyaz noktalar
        GameObject starsPanel = new GameObject("StarsPanel");
        starsPanel.transform.SetParent(parent, false);
        RectTransform starsRT = starsPanel.AddComponent<RectTransform>();
        starsRT.anchorMin = Vector2.zero;
        starsRT.anchorMax = Vector2.one;
        starsRT.sizeDelta = Vector2.zero;

        // Rastgele yildizlar olustur
        for (int i = 0; i < 50; i++)
        {
            GameObject star = new GameObject("Star" + i);
            star.transform.SetParent(starsPanel.transform, false);
            RectTransform starRT = star.AddComponent<RectTransform>();

            float x = Random.Range(-900f, 900f);
            float y = Random.Range(-500f, 500f);
            float size = Random.Range(2f, 6f);

            starRT.anchorMin = starRT.anchorMax = new Vector2(0.5f, 0.5f);
            starRT.anchoredPosition = new Vector2(x, y);
            starRT.sizeDelta = new Vector2(size, size);

            Image starImg = star.AddComponent<Image>();
            float brightness = Random.Range(0.3f, 1f);
            starImg.color = new Color(brightness, brightness, brightness, brightness);
        }
    }

    GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size, Color color, FontStyles style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = style;

        return textObj;
    }

    void CreateMenuButton(Transform parent, string name, string text, Vector2 position, Color normalColor, Color hoverColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;

        // Mobilde minimum 48dp dokunmatik boyut
        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;
        float minHeight = isMobile ? Mathf.Max(80f, 48f * (Screen.dpi > 0 ? Screen.dpi / 160f : 1f)) : 70f;
        float btnWidth = isMobile ? 380f : 320f;
        rt.sizeDelta = new Vector2(btnWidth, minHeight);

        Image img = buttonObj.AddComponent<Image>();
        img.color = normalColor;

        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = normalColor * 0.7f;
        colors.selectedColor = normalColor;
        btn.colors = colors;

        if (onClick != null)
            btn.onClick.AddListener(onClick);

        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
    }
}
