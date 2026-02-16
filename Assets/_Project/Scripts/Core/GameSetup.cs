using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using TMPro;

public class GameSetup : MonoBehaviour
{
    [Header("Player Settings")]
    public float playerScale = 1.5f;
    public Vector2 playerStartPosition = new Vector2(2, 3);

    [Header("Camera Settings")]
    public float cameraSize = 7f;

    private void Awake()
    {
        SetupGame();
    }

    void SetupGame()
    {
        // 1. Kamera ayarla
        SetupCamera();

        // 2. Oyuncu olustur
        GameObject player = CreatePlayer();

        // 3. GameManager olustur
        CreateGameManager(player.transform);

        // 4. UI olustur
        CreateGameUI();

        // 5. Tilemap ve Level olustur
        CreateLevel();

        // 6. ParticleManager
        if (FindObjectOfType<ParticleManager>() == null)
        {
            GameObject pm = new GameObject("ParticleManager");
            pm.AddComponent<ParticleManager>();
        }

        // 7. AudioManager
        if (FindObjectOfType<AudioManager>() == null)
        {
            GameObject am = new GameObject("AudioManager");
            am.AddComponent<AudioManager>();
        }

        // 8. PowerUpManager
        if (FindObjectOfType<PowerUpManager>() == null)
        {
            GameObject pum = new GameObject("PowerUpManager");
            pum.AddComponent<PowerUpManager>();
        }

        // 9. SaveManager (DontDestroyOnLoad)
        if (FindObjectOfType<SaveManager>() == null)
        {
            GameObject sm = new GameObject("SaveManager");
            sm.AddComponent<SaveManager>();
        }

        // 10. UpgradeManager
        if (FindObjectOfType<UpgradeManager>() == null)
        {
            GameObject um = new GameObject("UpgradeManager");
            um.AddComponent<UpgradeManager>();
        }

        // 11. UIAnimator
        if (FindObjectOfType<UIAnimator>() == null)
        {
            GameObject uiAnimator = new GameObject("UIAnimator");
            uiAnimator.AddComponent<UIAnimator>();
        }

        // 12. FloatingTextManager
        if (FindObjectOfType<FloatingTextManager>() == null)
        {
            GameObject ftm = new GameObject("FloatingTextManager");
            ftm.AddComponent<FloatingTextManager>();
        }

        // 13. NotificationManager
        if (FindObjectOfType<NotificationManager>() == null)
        {
            GameObject nm = new GameObject("NotificationManager");
            nm.AddComponent<NotificationManager>();
        }

        // 14. AdvancedHUD
        if (FindObjectOfType<AdvancedHUD>() == null)
        {
            GameObject hud = new GameObject("AdvancedHUD");
            hud.AddComponent<AdvancedHUD>();
        }

        // 15. BossHealthBar
        if (FindObjectOfType<BossHealthBar>() == null)
        {
            GameObject bhb = new GameObject("BossHealthBar");
            bhb.AddComponent<BossHealthBar>();
        }

        // 16. EnemyIndicatorManager
        if (FindObjectOfType<EnemyIndicatorManager>() == null)
        {
            GameObject eim = new GameObject("EnemyIndicatorManager");
            eim.AddComponent<EnemyIndicatorManager>();
        }

        // 17. ComboManager
        if (FindObjectOfType<ComboManager>() == null)
        {
            GameObject cm = new GameObject("ComboManager");
            cm.AddComponent<ComboManager>();
        }

        // 18. MobileControls - gorunurlugu kendi Start() metodu kontrol eder
        if (FindObjectOfType<MobileControls>() == null)
        {
            GameObject mobileCtrlObj = new GameObject("MobileControls");
            mobileCtrlObj.AddComponent<MobileControls>();
        }

        // 19. NeonHUD
        if (FindObjectOfType<NeonHUD>() == null)
        {
            GameObject neonHudObj = new GameObject("NeonHUD");
            neonHudObj.AddComponent<NeonHUD>();
        }

        // 20. MobileTouchTutorial - ilk oyunda dokunmatik kontrol ipuclari
        if (FindObjectOfType<MobileTouchTutorial>() == null)
        {
            GameObject tutorialObj = new GameObject("MobileTouchTutorial");
            tutorialObj.AddComponent<MobileTouchTutorial>();
        }

        // Oyun muzigini baslat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }

        Debug.Log("Game Setup Complete!");
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = cameraSize;
        cam.backgroundColor = new Color(0.05f, 0.02f, 0.15f);
        cam.transform.position = new Vector3(playerStartPosition.x, playerStartPosition.y + 2, -10);
    }

    GameObject CreatePlayer()
    {
        // Sahnede zaten Player varsa onu kullan
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer == null)
        {
            existingPlayer = GameObject.Find("Player");
        }
        if (existingPlayer == null)
        {
            existingPlayer = GameObject.Find("Player_Asker");
        }

        if (existingPlayer != null)
        {
            Debug.Log("Sahnedeki mevcut Player kullaniliyor: " + existingPlayer.name);

            // Gerekli componentleri ekle (yoksa)
            if (existingPlayer.GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = existingPlayer.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (existingPlayer.GetComponent<BoxCollider2D>() == null)
            {
                BoxCollider2D col = existingPlayer.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.9f);
            }

            // Ground Check
            Transform groundCheck = existingPlayer.transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(existingPlayer.transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
                groundCheck = groundCheckObj.transform;
            }

            // PlayerController
            PlayerController pc = existingPlayer.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = existingPlayer.AddComponent<PlayerController>();
                pc.moveSpeed = 8f;
                pc.jumpForce = 7f;
                pc.bounceForce = 12f;
                pc.groundCheckRadius = 0.15f;
                pc.groundLayer = LayerMask.GetMask("Default");
                pc.coyoteTime = 0.15f;
                pc.jumpBufferTime = 0.15f;
                pc.animationSpeed = 0.08f;
            }
            pc.groundCheck = groundCheck;

            existingPlayer.tag = "Player";

            // CameraFollow
            SetupCameraFollow(existingPlayer);

            return existingPlayer;
        }

        // Sahnede Player yoksa yeni olustur
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = 0;
        player.transform.position = new Vector3(playerStartPosition.x, playerStartPosition.y, 0);
        player.transform.localScale = new Vector3(playerScale, playerScale, 1f);

        // Sprite Renderer
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.color = new Color(0f, 0.9f, 1f); // Cyan neon
        sr.sortingOrder = 10;

        // Gecici sprite (PlayerController kendi sprite'ini olusturacak)
        Texture2D tex = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

        // Rigidbody2D
        Rigidbody2D newRb = player.AddComponent<Rigidbody2D>();
        newRb.gravityScale = 3f;
        newRb.freezeRotation = true;
        newRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        newRb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Box Collider
        BoxCollider2D newCol = player.AddComponent<BoxCollider2D>();
        newCol.size = new Vector2(0.8f, 0.9f);
        newCol.offset = new Vector2(0, 0);

        // Ground Check
        GameObject groundCheckNew = new GameObject("GroundCheck");
        groundCheckNew.transform.SetParent(player.transform);
        groundCheckNew.transform.localPosition = new Vector3(0, -0.5f, 0);

        // PlayerController
        PlayerController pc2 = player.AddComponent<PlayerController>();
        pc2.moveSpeed = 8f;
        pc2.jumpForce = 14f;
        pc2.bounceForce = 12f;
        pc2.groundCheck = groundCheckNew.transform;
        pc2.groundCheckRadius = 0.15f;
        pc2.groundLayer = LayerMask.GetMask("Default");
        pc2.coyoteTime = 0.15f;
        pc2.jumpBufferTime = 0.15f;
        pc2.animationSpeed = 0.08f;

        // CameraFollow
        SetupCameraFollow(player);

        return player;
    }

    void SetupCameraFollow(GameObject player)
    {
        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.gameObject.GetComponent<CameraFollow>();
            if (camFollow == null)
                camFollow = Camera.main.gameObject.AddComponent<CameraFollow>();
            camFollow.target = player.transform;
            camFollow.smoothSpeed = 5f;
            camFollow.offset = new Vector3(3, 2, -10);
            camFollow.minX = 0;
            camFollow.maxX = 250;
            camFollow.minY = 0;
            camFollow.maxY = 20;
        }
    }

    void CreateGameManager(Transform player)
    {
        if (FindObjectOfType<GameManager>() != null) return;

        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.player = player;
        gm.maxHealth = 3;
        gm.deathY = -10f;
    }

    void CreateGameUI()
    {
        if (FindObjectOfType<UIManager>() != null) return;

        // Canvas
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

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

        // === HUD Panel (Sol Ust) ===
        GameObject hudPanel = CreateUIPanel(canvasObj.transform, "HUDPanel",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(300, 150), new Color(0, 0, 0, 0.7f));

        // Score Label
        CreateUIText(hudPanel.transform, "ScoreLabel", "SKOR", 24,
            new Vector2(0, 1), new Vector2(15, -10), new Vector2(150, 30),
            TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));

        // Score Text
        TextMeshProUGUI scoreText = CreateUIText(hudPanel.transform, "ScoreText", "0", 48,
            new Vector2(0, 1), new Vector2(15, -40), new Vector2(250, 55),
            TextAlignmentOptions.Left, Color.white);
        uiManager.scoreText = scoreText;

        // Coin Icon
        GameObject coinIcon = new GameObject("CoinIcon");
        coinIcon.transform.SetParent(hudPanel.transform, false);
        RectTransform coinIconRT = coinIcon.AddComponent<RectTransform>();
        coinIconRT.anchorMin = coinIconRT.anchorMax = coinIconRT.pivot = new Vector2(0, 1);
        coinIconRT.anchoredPosition = new Vector2(15, -105);
        coinIconRT.sizeDelta = new Vector2(30, 30);
        Image coinIconImg = coinIcon.AddComponent<Image>();
        coinIconImg.color = new Color(1f, 0.84f, 0f);

        // Coin Text
        TextMeshProUGUI coinText = CreateUIText(hudPanel.transform, "CoinText", "x 0", 32,
            new Vector2(0, 1), new Vector2(55, -105), new Vector2(150, 35),
            TextAlignmentOptions.Left, new Color(1f, 0.84f, 0f));
        uiManager.coinText = coinText;

        // === Hearts Panel (Sag Ust) ===
        GameObject heartsPanel = CreateUIPanel(canvasObj.transform, "HeartsPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(200, 70), new Color(0, 0, 0, 0.7f));

        // Hearts Container
        GameObject heartsContainer = new GameObject("HeartsContainer");
        heartsContainer.transform.SetParent(heartsPanel.transform, false);
        RectTransform heartsRT = heartsContainer.AddComponent<RectTransform>();
        heartsRT.anchorMin = heartsRT.anchorMax = new Vector2(0.5f, 0.5f);
        heartsRT.pivot = new Vector2(0.5f, 0.5f);
        heartsRT.anchoredPosition = Vector2.zero;
        heartsRT.sizeDelta = new Vector2(180, 50);
        HorizontalLayoutGroup hlg = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        uiManager.heartsContainer = heartsRT;

        // === Game Over Panel ===
        GameObject gameOverPanel = CreateUIPanel(canvasObj.transform, "GameOverPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(600, 500), new Color(0.1f, 0.05f, 0.15f, 0.95f));
        uiManager.gameOverPanel = gameOverPanel;

        CreateUIText(gameOverPanel.transform, "Title", "GAME OVER", 72,
            new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(500, 90),
            TextAlignmentOptions.Center, Color.red);

        uiManager.gameOverScoreText = CreateUIText(gameOverPanel.transform, "ScoreText", "Skor: 0", 42,
            new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(400, 55),
            TextAlignmentOptions.Center, Color.white);

        uiManager.gameOverHighScoreText = CreateUIText(gameOverPanel.transform, "HighScoreText", "En Yuksek: 0", 32,
            new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(400, 45),
            TextAlignmentOptions.Center, new Color(1f, 0.84f, 0f));

        CreateUIButton(gameOverPanel.transform, "RestartButton", "Tekrar Oyna",
            new Vector2(0, -90), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.RestartGame();
            });

        CreateUIButton(gameOverPanel.transform, "MenuButton", "Ana Menu",
            new Vector2(0, -160), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu();
            });

        // === Win Panel ===
        GameObject winPanel = CreateUIPanel(canvasObj.transform, "WinPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(600, 500), new Color(0.05f, 0.15f, 0.1f, 0.95f));
        uiManager.winPanel = winPanel;

        CreateUIText(winPanel.transform, "Title", "KAZANDIN!", 72,
            new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(500, 90),
            TextAlignmentOptions.Center, Color.green);

        uiManager.winScoreText = CreateUIText(winPanel.transform, "ScoreText", "Skor: 0", 42,
            new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(400, 55),
            TextAlignmentOptions.Center, Color.white);

        uiManager.winTimeText = CreateUIText(winPanel.transform, "TimeText", "Sure: 00:00", 32,
            new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(400, 45),
            TextAlignmentOptions.Center, new Color(0.7f, 1f, 0.7f));

        CreateUIButton(winPanel.transform, "RestartButton", "Tekrar Oyna",
            new Vector2(0, -90), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.RestartGame();
            });

        CreateUIButton(winPanel.transform, "MenuButton", "Ana Menu",
            new Vector2(0, -160), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu();
            });

        // === Pause Panel ===
        GameObject pausePanel = CreateUIPanel(canvasObj.transform, "PausePanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500, 400), new Color(0.1f, 0.1f, 0.15f, 0.95f));
        uiManager.pausePanel = pausePanel;

        CreateUIText(pausePanel.transform, "Title", "DURAKLATILDI", 56,
            new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(450, 70),
            TextAlignmentOptions.Center, Color.white);

        CreateUIButton(pausePanel.transform, "ResumeButton", "Devam Et",
            new Vector2(0, 20), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.ResumeGame();
            });

        CreateUIButton(pausePanel.transform, "RestartButton", "Yeniden Basla",
            new Vector2(0, -50), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.RestartGame();
            });

        CreateUIButton(pausePanel.transform, "MenuButton", "Ana Menu",
            new Vector2(0, -120), new Vector2(280, 60), () => {
                if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu();
            });

        // Panelleri gizle
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        pausePanel.SetActive(false);
    }

    void CreateLevel()
    {
        if (FindObjectOfType<NeonLevelDesigner>() != null) return;
        if (FindObjectOfType<TilemapLevelBuilder>() != null) return;

        // Neon Level Designer - guzel neon temali sahne
        GameObject levelBuilderObj = new GameObject("NeonLevelDesigner");
        levelBuilderObj.AddComponent<NeonLevelDesigner>();
        // Yeni neon temali sahne olusturulacak
    }

    // === UI Helper Methods ===

    GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = panel.AddComponent<Image>();
        img.color = bgColor;

        return panel;
    }

    TextMeshProUGUI CreateUIText(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color color)
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
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;

        return tmp;
    }

    void CreateUIButton(Transform parent, string name, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.2f, 0.5f, 1f);

        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.5f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.3f, 0.7f, 1f);
        colors.pressedColor = new Color(0.2f, 0.1f, 0.3f, 1f);
        colors.selectedColor = new Color(0.4f, 0.25f, 0.6f, 1f);
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
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
    }
}
