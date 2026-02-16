using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;

/// <summary>
/// Profesyonel mobil kontroller - Floating joystick, neon butonlar, cooldown overlay.
/// Prosedural gorsellerle programatik olarak olusturulur.
/// Haptic feedback, auto-fire, swipe gesture destegi icerir.
/// </summary>
public class MobileControls : MonoBehaviour
{
    public static MobileControls Instance { get; private set; }

    // === JOYSTICK STATE ===
    private RectTransform joystickArea; // Sol yarim ekran - dokunma alani
    private RectTransform joystickContainer; // Background + handle parent
    private RectTransform joystickBackground;
    private RectTransform joystickHandle;
    private Image joystickGlowImage;
    private Vector2 joystickInput;
    private Vector2 smoothedJoystickInput; // Smoothed joystick input
    private bool isJoystickActive;
    private int joystickTouchId = -1;
    private Vector2 joystickOrigin; // Floating: dokunulan nokta

    // === BUTTON REFS ===
    private RectTransform jumpButton;
    private RectTransform fireButton;
    private RectTransform dashButton;
    private RectTransform rollButton;

    // Button glow images
    private Image jumpGlow;
    private Image fireGlow;
    private Image dashGlow;
    private Image rollGlow;

    // Button icon images (for tinting)
    private Image jumpIcon;
    private Image fireIcon;
    private Image dashIcon;
    private Image rollIcon;

    // === COOLDOWN ===
    private Image dashCooldownFill;
    private float dashCooldownDuration = 0.8f;
    private float dashCooldownTimer = 0f;
    private bool isDashOnCooldown = false;

    private Image rollCooldownFill;
    private float rollCooldownDuration = 1f;
    private float rollCooldownTimer = 0f;
    private bool isRollOnCooldown = false;

    // === SETTINGS ===
    private const string PREF_ENABLED = "MobileControls_Enabled";
    private const string PREF_BUTTON_SIZE = "MobileControls_ButtonSize";
    private const string PREF_OPACITY = "MobileControls_Opacity";
    private const string PREF_SWIPE_ENABLED = "MobileControls_SwipeEnabled";

    public float ButtonSizeScale { get; private set; } = 1.0f;
    public float Opacity { get; private set; } = 0.85f;
    public bool IsEnabled { get; private set; } = true;

    // === BUTTON STATES ===
    private bool isJumpPressed;
    private bool isFirePressed;
    private bool isDashPressed;
    private bool isRollPressed;

    private Canvas canvas;
    private CanvasGroup mainCanvasGroup;
    private RectTransform safeAreaPanel;
    private Dictionary<int, string> activeTouches = new Dictionary<int, string>();

    // === ANIMATION ===
    private float joystickGlowAlpha = 0f;
    private float handleReturnSpeed = 12f; // Smooth return hizi
    private bool isHandleReturning = false;
    private Dictionary<RectTransform, float> buttonScaleTimers = new Dictionary<RectTransform, float>();

    // === JOYSTICK SETTINGS ===
    private float joystickRadius = 100f;
    private float deadZone = 0.15f;
    private int joystickBgSize = 250; // Pixel (base, ekrana gore olceklenir)
    private int joystickHandleSize = 100; // Pixel (base, ekrana gore olceklenir)
    private float joystickInputSmoothSpeed = 15f; // Joystick input lerp hizi

    // === BUTTON SIZES (base, before scale) ===
    private float jumpButtonSize = 110f;
    private float fireButtonSize = 100f;
    private float actionButtonSize = 80f;
    private float smallButtonSize = 65f;
    private const float MIN_BUTTON_SIZE = 55f; // Minimum buton boyutu (px)
    private const float MIN_BUTTON_SPACING = 15f; // Butonlar arasi minimum bosluk (px)

    // === AUTO-FIRE ===
    private float autoFireRate = 0.15f; // Otomatik ates arasi bekleme
    private float autoFireTimer = 0f;
    private bool isAutoFireActive = false;

    // === SWIPE GESTURE ===
    private bool swipeGesturesEnabled = true;
    private float swipeMinDistance = 80f; // Minimum swipe mesafesi (px)
    private float swipeMaxTime = 0.4f; // Swipe icin max sure (saniye)
    private Dictionary<int, SwipeData> swipeTracking = new Dictionary<int, SwipeData>();

    private struct SwipeData
    {
        public Vector2 startPos;
        public float startTime;
    }

    // Public properties for PlayerController
    public Vector2 MoveInput => smoothedJoystickInput;
    public bool JumpPressed => isJumpPressed;
    public bool FireHeld => isFirePressed;
    public bool DashPressed => isDashPressed;
    public bool RollPressed => isRollPressed;

    // Cached collections for performance (avoid GC allocations in Update)
    private List<int> touchRemoveList = new List<int>(10);
    private List<RectTransform> buttonScaleKeys = new List<RectTransform>(10);
    private Image[] cachedJoystickImages;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        LoadSettings();
        ScaleJoystickToScreen();

        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;

        // Mobil platformda veya Editor'de her zaman etkinlestir
        if (isMobile)
        {
            IsEnabled = true;
        }

        #if UNITY_EDITOR
        // Editor'de her zaman goster (test icin)
        IsEnabled = true;
        #endif

        if (!isMobile && !IsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        EnsureEventSystem();
        CreateCanvas();
        CreateSafeAreaPanel();
        CreateJoystick();
        CreateActionButtons();
        ApplyOpacity(Opacity);
    }

    void Update()
    {
        // Her frame basinda one-shot buton state'lerini sifirla
        isJumpPressed = false;
        isDashPressed = false;
        isRollPressed = false;

        HandleTouchInput();
        UpdateJoystickSmoothing();
        UpdateAutoFire();
        UpdateAnimations();
        UpdateCooldowns();
    }

    // === EKRAN OLCEKLEME ===

    /// <summary>
    /// Joystick boyutunu ekran boyutuna gore olcekler.
    /// Referans: 1080p ekran. Kucuk ekranlarda kuculur, buyuk ekranlarda buyur.
    /// </summary>
    void ScaleJoystickToScreen()
    {
        float referenceHeight = 1080f;
        float screenScale = Mathf.Clamp(Screen.height / referenceHeight, 0.6f, 1.5f);
        joystickBgSize = Mathf.RoundToInt(250 * screenScale);
        joystickHandleSize = Mathf.RoundToInt(100 * screenScale);
        joystickRadius = 100f * screenScale;
    }

    // === CANVAS & SAFE AREA ===

    void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    void CreateCanvas()
    {
        // Ozel canvas olustur - diger UI'larin ustunde
        GameObject canvasObj = new GameObject("MobileControlsCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // Mobilde yukseklik oncelikli (landscape modda daha tutarli)
        scaler.matchWidthOrHeight = Application.isMobilePlatform ? 0.6f : 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        mainCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        mainCanvasGroup.interactable = false; // Touch'lar manuel handle ediliyor
        mainCanvasGroup.blocksRaycasts = false;

        transform.SetParent(canvasObj.transform, false);

        // Sahne degisikliklerinde yok edilmesini engelle
        DontDestroyOnLoad(canvasObj);
    }

    void CreateSafeAreaPanel()
    {
        GameObject safeObj = new GameObject("SafeAreaPanel");
        safeObj.transform.SetParent(canvas.transform, false);

        safeAreaPanel = safeObj.AddComponent<RectTransform>();
        safeAreaPanel.anchorMin = Vector2.zero;
        safeAreaPanel.anchorMax = Vector2.one;
        safeAreaPanel.offsetMin = Vector2.zero;
        safeAreaPanel.offsetMax = Vector2.zero;

        safeObj.AddComponent<SafeAreaHandler>();
    }

    // === JOYSTICK OLUSTURMA ===

    void CreateJoystick()
    {
        // Floating joystick alani - sol yarim ekranin alt kismi
        GameObject areaObj = new GameObject("JoystickArea");
        areaObj.transform.SetParent(safeAreaPanel, false);

        joystickArea = areaObj.AddComponent<RectTransform>();
        joystickArea.anchorMin = new Vector2(0f, 0f);
        joystickArea.anchorMax = new Vector2(0.5f, 0.55f);
        joystickArea.offsetMin = Vector2.zero;
        joystickArea.offsetMax = Vector2.zero;

        // Joystick container (baslangicta gizli, dokunulunca belirir)
        GameObject containerObj = new GameObject("JoystickContainer");
        containerObj.transform.SetParent(safeAreaPanel, false);

        joystickContainer = containerObj.AddComponent<RectTransform>();
        joystickContainer.anchorMin = joystickContainer.anchorMax = new Vector2(0.5f, 0.5f);
        joystickContainer.pivot = new Vector2(0.5f, 0.5f);
        joystickContainer.sizeDelta = new Vector2(joystickBgSize, joystickBgSize);
        joystickContainer.anchoredPosition = Vector2.zero;

        // Background
        GameObject bgObj = new GameObject("JoystickBg");
        bgObj.transform.SetParent(containerObj.transform, false);

        joystickBackground = bgObj.AddComponent<RectTransform>();
        joystickBackground.anchorMin = joystickBackground.anchorMax = new Vector2(0.5f, 0.5f);
        joystickBackground.pivot = new Vector2(0.5f, 0.5f);
        joystickBackground.anchoredPosition = Vector2.zero;
        joystickBackground.sizeDelta = new Vector2(joystickBgSize, joystickBgSize);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = MobileControlsVisualFactory.CreateJoystickBackground(256);
        bgImage.type = Image.Type.Simple;
        bgImage.preserveAspect = true;

        // Glow ring (baslangicta gorulmez)
        GameObject glowObj = new GameObject("JoystickGlow");
        glowObj.transform.SetParent(containerObj.transform, false);

        RectTransform glowRt = glowObj.AddComponent<RectTransform>();
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.anchoredPosition = Vector2.zero;
        glowRt.sizeDelta = new Vector2(joystickBgSize + 10, joystickBgSize + 10);

        joystickGlowImage = glowObj.AddComponent<Image>();
        joystickGlowImage.sprite = MobileControlsVisualFactory.CreateJoystickGlowRing(256);
        joystickGlowImage.type = Image.Type.Simple;
        joystickGlowImage.preserveAspect = true;
        joystickGlowImage.color = new Color(1f, 1f, 1f, 0f); // Baslangicta gorulmez

        // Handle
        GameObject handleObj = new GameObject("JoystickHandle");
        handleObj.transform.SetParent(containerObj.transform, false);

        joystickHandle = handleObj.AddComponent<RectTransform>();
        joystickHandle.anchorMin = joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandle.pivot = new Vector2(0.5f, 0.5f);
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickHandle.sizeDelta = new Vector2(joystickHandleSize, joystickHandleSize);

        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.sprite = MobileControlsVisualFactory.CreateJoystickHandle(256);
        handleImage.type = Image.Type.Simple;
        handleImage.preserveAspect = true;

        // Floating: baslangicta joystick gizli
        SetJoystickVisible(false);
    }

    // === BUTON OLUSTURMA ===

    void CreateActionButtons()
    {
        // Sadece 4 buton: Jump, Fire, Dash, Roll
        // Tum butonlar sag alt koseye (1,0) sabitlenir, offset ile konumlandirilir.
        Vector2 rightBottom = new Vector2(1f, 0f);

        // Buton boyutlarini minimum ile sinirla
        float effJumpSize = Mathf.Max(jumpButtonSize, MIN_BUTTON_SIZE);
        float effFireSize = Mathf.Max(fireButtonSize, MIN_BUTTON_SIZE);
        float effActionSize = Mathf.Max(actionButtonSize, MIN_BUTTON_SIZE);
        float effSmallSize = Mathf.Max(smallButtonSize, MIN_BUTTON_SIZE);

        // Jump - sag alt, en buyuk (birincil aksiyon, basparmaga en yakin)
        jumpButton = CreateActionButton("JumpButton", "ZIPLA",
            MobileControlsVisualFactory.CreateArrowUpIcon(128),
            MobileControlsVisualFactory.NeonGreen,
            rightBottom, new Vector2(-100, 95), effJumpSize,
            out jumpGlow, out jumpIcon);

        // Fire - jump'in solunda (ikincil aksiyon, basparmaga yakin)
        float fireOffsetX = -100 - effJumpSize * 0.5f - MIN_BUTTON_SPACING - effFireSize * 0.5f;
        fireButton = CreateActionButton("FireButton", "ATES",
            MobileControlsVisualFactory.CreateCrosshairIcon(128),
            MobileControlsVisualFactory.NeonOrange,
            rightBottom, new Vector2(fireOffsetX, 80), effFireSize,
            out fireGlow, out fireIcon);

        // Dash - jump ile fire arasinda, ustlerinde
        dashButton = CreateActionButton("DashButton", "DASH",
            MobileControlsVisualFactory.CreateLightningIcon(128),
            MobileControlsVisualFactory.NeonCyan,
            rightBottom, new Vector2(-180, 245), effActionSize,
            out dashGlow, out dashIcon);

        // Dash cooldown overlay
        CreateCooldownOverlay(dashButton, out dashCooldownFill);

        // Roll/Takla - dash'in saginda, ustunde
        rollButton = CreateActionButton("RollButton", "TAKLA",
            MobileControlsVisualFactory.CreateLightningIcon(128),
            new Color(1f, 0.4f, 1f), // Mor/pembe
            rightBottom, new Vector2(-80, 340), effSmallSize,
            out rollGlow, out rollIcon);

        CreateCooldownOverlay(rollButton, out rollCooldownFill);
    }

    RectTransform CreateActionButton(string name, string label, Sprite iconSprite, Color neonColor,
        Vector2 anchor, Vector2 offset, float size, out Image glowImage, out Image iconImage)
    {
        float scaledSize = Mathf.Max(size * ButtonSizeScale, MIN_BUTTON_SIZE);

        // Ana buton objesi
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(safeAreaPanel, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(scaledSize, scaledSize);

        // Arka plan daire
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.sprite = MobileControlsVisualFactory.CreateButtonCircle(256, neonColor);
        bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = true;

        // Glow overlay (basili durumda gorulur)
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(btnObj.transform, false);

        RectTransform glowRt = glowObj.AddComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.offsetMin = new Vector2(-4, -4);
        glowRt.offsetMax = new Vector2(4, 4);

        glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = MobileControlsVisualFactory.CreateButtonGlowCircle(256, neonColor);
        glowImage.type = Image.Type.Simple;
        glowImage.preserveAspect = true;
        glowImage.color = new Color(1f, 1f, 1f, 0f); // Gizli

        // Ikon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);

        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.2f, 0.25f);
        iconRt.anchorMax = new Vector2(0.8f, 0.85f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.color = neonColor;

        // Label (kucuk yazi altta)
        if (!string.IsNullOrEmpty(label))
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);

            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, -0.05f);
            labelRt.anchorMax = new Vector2(1f, 0.2f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = scaledSize > 100 ? 14 : 11;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = new Color(neonColor.r, neonColor.g, neonColor.b, 0.7f);
            labelText.alignment = TextAnchor.MiddleCenter;
        }

        // Scale animasyon takibi icin
        buttonScaleTimers[rt] = 0f;

        return rt;
    }

    void CreateCooldownOverlay(RectTransform button, out Image cooldownFill)
    {
        cooldownFill = null;
        if (button == null) return;

        GameObject cdObj = new GameObject("CooldownFill");
        cdObj.transform.SetParent(button.transform, false);

        RectTransform cdRt = cdObj.AddComponent<RectTransform>();
        cdRt.anchorMin = new Vector2(0.05f, 0.05f);
        cdRt.anchorMax = new Vector2(0.95f, 0.95f);
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;

        cooldownFill = cdObj.AddComponent<Image>();
        cooldownFill.sprite = MobileControlsVisualFactory.CreateRadialFillCircle(64);
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Radial360;
        cooldownFill.fillOrigin = (int)Image.Origin360.Top;
        cooldownFill.fillClockwise = false;
        cooldownFill.fillAmount = 0f;
        cooldownFill.color = new Color(0f, 0f, 0f, 0.6f);
        cooldownFill.raycastTarget = false;
    }

    // === HAPTIC FEEDBACK ===

    /// <summary>
    /// Aksiyon turune gore haptic feedback tetikler.
    /// Android'de daha ince kontrol, diger platformlarda Handheld.Vibrate().
    /// </summary>
    void TriggerHapticFeedback(string action)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            long milliseconds = 20; // Varsayilan kisa titresim
            switch (action)
            {
                case "Jump": milliseconds = 15; break;
                case "Fire": milliseconds = 10; break;
                case "Dash": milliseconds = 30; break;
                case "Roll": milliseconds = 25; break;
            }
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator != null)
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
        }
        catch (System.Exception)
        {
            // Vibration desteklenmiyorsa sessizce devam et
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    // === TOUCH INPUT ===

    void HandleTouchInput()
    {
        var touches = Touch.activeTouches;

        // Kalkan dokunuslari temizle (pre-allocated list - no GC)
        touchRemoveList.Clear();
        foreach (var kvp in activeTouches)
        {
            bool found = false;
            foreach (var touch in touches)
            {
                if (touch.touchId == kvp.Key) { found = true; break; }
            }
            if (!found)
            {
                touchRemoveList.Add(kvp.Key);
            }
        }
        foreach (int id in touchRemoveList)
        {
            OnTouchReleased(id, activeTouches[id]);
            activeTouches.Remove(id);
        }

        // Dokunuslari isle
        foreach (var touch in touches)
        {
            Vector2 screenPos = touch.screenPosition;

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                string element = GetTouchedElement(screenPos);
                if (!string.IsNullOrEmpty(element))
                {
                    activeTouches[touch.touchId] = element;
                    OnTouchBegan(element, screenPos, touch.touchId);
                }

                // Swipe tracking baslat (joystick alani disinda, sag tarafta)
                if (swipeGesturesEnabled && screenPos.x > Screen.width * 0.5f)
                {
                    swipeTracking[touch.touchId] = new SwipeData
                    {
                        startPos = screenPos,
                        startTime = Time.time
                    };
                }
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                if (activeTouches.TryGetValue(touch.touchId, out string element))
                {
                    OnTouchMoved(element, screenPos);
                }
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (activeTouches.TryGetValue(touch.touchId, out string element))
                {
                    OnTouchReleased(touch.touchId, element);
                    activeTouches.Remove(touch.touchId);
                }

                // Swipe detection
                if (swipeGesturesEnabled && swipeTracking.TryGetValue(touch.touchId, out SwipeData swipeData))
                {
                    DetectSwipe(swipeData, screenPos);
                    swipeTracking.Remove(touch.touchId);
                }
            }
        }
    }

    string GetTouchedElement(Vector2 screenPos)
    {
        // Butonlari once kontrol et (oncelik)
        if (IsPointInButton(screenPos, jumpButton)) return "Jump";
        if (IsPointInButton(screenPos, fireButton)) return "Fire";
        if (IsPointInButton(screenPos, dashButton)) return "Dash";
        if (IsPointInButton(screenPos, rollButton)) return "Roll";

        // Sol yarim ekran = joystick alani
        if (screenPos.x < Screen.width * 0.5f && screenPos.y < Screen.height * 0.6f)
        {
            return "Joystick";
        }

        return null;
    }

    bool IsPointInButton(Vector2 screenPos, RectTransform button)
    {
        if (button == null) return false;
        // Hit area'yi biraz buyut (parmak dostu)
        return RectTransformUtility.RectangleContainsScreenPoint(button, screenPos, null);
    }

    void OnTouchBegan(string element, Vector2 screenPos, int touchId)
    {
        switch (element)
        {
            case "Joystick":
                isJoystickActive = true;
                joystickTouchId = touchId;
                isHandleReturning = false;

                // Floating: joystick dokunulan noktada belirir
                MoveJoystickTo(screenPos);
                SetJoystickVisible(true);
                UpdateJoystick(screenPos);
                break;

            case "Jump":
                isJumpPressed = true;
                AnimateButtonPress(jumpButton, jumpGlow, true);
                TriggerHapticFeedback("Jump");
                break;

            case "Fire":
                isFirePressed = true;
                isAutoFireActive = true;
                autoFireTimer = 0f;
                AnimateButtonPress(fireButton, fireGlow, true);
                TriggerHapticFeedback("Fire");
                break;

            case "Dash":
                if (!isDashOnCooldown)
                {
                    isDashPressed = true;
                    AnimateButtonPress(dashButton, dashGlow, true);
                    TriggerHapticFeedback("Dash");
                }
                break;

            case "Roll":
                if (!isRollOnCooldown)
                {
                    isRollPressed = true;
                    AnimateButtonPress(rollButton, rollGlow, true);
                    TriggerHapticFeedback("Roll");
                }
                break;
        }
    }

    void OnTouchMoved(string element, Vector2 screenPos)
    {
        if (element == "Joystick" && isJoystickActive)
        {
            UpdateJoystick(screenPos);
        }
    }

    void OnTouchReleased(int touchId, string element)
    {
        switch (element)
        {
            case "Joystick":
                isJoystickActive = false;
                joystickTouchId = -1;
                joystickInput = Vector2.zero;
                isHandleReturning = true; // Smooth return baslat
                break;

            case "Jump":
                AnimateButtonPress(jumpButton, jumpGlow, false);
                break;

            case "Fire":
                isFirePressed = false;
                isAutoFireActive = false;
                AnimateButtonPress(fireButton, fireGlow, false);
                break;

            case "Dash":
                AnimateButtonPress(dashButton, dashGlow, false);
                break;

            case "Roll":
                AnimateButtonPress(rollButton, rollGlow, false);
                break;
        }
    }

    // === SWIPE GESTURE ===

    void DetectSwipe(SwipeData swipeData, Vector2 endPos)
    {
        float elapsed = Time.time - swipeData.startTime;
        if (elapsed > swipeMaxTime) return;

        Vector2 delta = endPos - swipeData.startPos;
        float distance = delta.magnitude;
        if (distance < swipeMinDistance) return;

        // Dikey swipe kontrolu (yatay hareketten buyuk olmali)
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
        {
            if (delta.y > 0)
            {
                // Yukari swipe = ziplama
                isJumpPressed = true;
                TriggerHapticFeedback("Jump");
            }
        }
    }

    /// <summary>
    /// Swipe gesture destegini ac/kapat (ayarlardan)
    /// </summary>
    public void SetSwipeGesturesEnabled(bool enabled)
    {
        swipeGesturesEnabled = enabled;
        PlayerPrefs.SetInt(PREF_SWIPE_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsSwipeGesturesEnabled => swipeGesturesEnabled;

    // === AUTO-FIRE ===

    void UpdateAutoFire()
    {
        if (!isAutoFireActive) return;

        autoFireTimer += Time.deltaTime;
        if (autoFireTimer >= autoFireRate)
        {
            autoFireTimer = 0f;
            isFirePressed = true;
            TriggerHapticFeedback("Fire");
        }
    }

    // === JOYSTICK LOGIC ===

    void MoveJoystickTo(Vector2 screenPos)
    {
        if (joystickContainer == null || safeAreaPanel == null) return;

        // Screen pozisyonunu safe area lokal pozisyonuna cevir
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            safeAreaPanel, screenPos, null, out localPoint);

        joystickContainer.anchoredPosition = localPoint;
    }

    void UpdateJoystick(Vector2 screenPos)
    {
        if (joystickContainer == null) return;

        // Screen pozisyonunu joystick merkezine gore hesapla
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(null, joystickContainer.position);
        Vector2 direction = screenPos - joystickCenter;

        // Radius siniri
        float scaledRadius = joystickRadius * ButtonSizeScale;
        float magnitude = direction.magnitude;
        if (magnitude > scaledRadius)
        {
            direction = direction.normalized * scaledRadius;
        }

        // Handle pozisyonunu guncelle
        joystickHandle.anchoredPosition = direction;

        // Input degeri (-1, 1)
        joystickInput = direction / scaledRadius;

        // Dead zone
        if (joystickInput.magnitude < deadZone)
        {
            joystickInput = Vector2.zero;
        }
        else
        {
            // Dead zone sonrasi remap
            float mag = joystickInput.magnitude;
            joystickInput = joystickInput.normalized * ((mag - deadZone) / (1f - deadZone));
        }
    }

    /// <summary>
    /// Joystick inputunu smooth lerp ile yumusatir (ani hareketleri onler)
    /// </summary>
    void UpdateJoystickSmoothing()
    {
        smoothedJoystickInput = Vector2.Lerp(
            smoothedJoystickInput,
            joystickInput,
            Time.deltaTime * joystickInputSmoothSpeed
        );

        // Cok kucuk degerler icin sifirla (floating point birikimi onleme)
        if (smoothedJoystickInput.sqrMagnitude < 0.001f)
        {
            smoothedJoystickInput = Vector2.zero;
        }
    }

    void SetJoystickVisible(bool visible)
    {
        if (joystickContainer == null) return;

        // Cache joystick images (avoid GetComponentsInChildren every frame)
        if (cachedJoystickImages == null)
            cachedJoystickImages = joystickContainer.GetComponentsInChildren<Image>(true);

        foreach (Image img in cachedJoystickImages)
        {
            if (img == joystickGlowImage) continue; // Glow ayri kontrol
            Color c = img.color;
            c.a = visible ? Opacity : 0f;
            img.color = c;
        }

        joystickContainer.gameObject.SetActive(true); // Her zaman aktif, alpha ile goster/gizle
    }

    // === ANIMASYONLAR ===

    void UpdateAnimations()
    {
        // Joystick glow animasyonu
        float targetGlowAlpha = isJoystickActive ? 0.8f : 0f;
        joystickGlowAlpha = Mathf.MoveTowards(joystickGlowAlpha, targetGlowAlpha, Time.deltaTime * 8f);
        if (joystickGlowImage != null)
        {
            joystickGlowImage.color = new Color(1f, 1f, 1f, joystickGlowAlpha);
        }

        // Handle smooth return
        if (isHandleReturning && joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.Lerp(
                joystickHandle.anchoredPosition,
                Vector2.zero,
                Time.deltaTime * handleReturnSpeed
            );

            if (joystickHandle.anchoredPosition.sqrMagnitude < 1f)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
                isHandleReturning = false;

                // Joystick'i gizle (floating mode)
                SetJoystickVisible(false);
            }
        }

        // Buton scale punch animasyonlari
        UpdateButtonScales();
    }

    void AnimateButtonPress(RectTransform button, Image glow, bool pressed)
    {
        if (button == null) return;

        if (pressed)
        {
            // Scale punch baslat
            buttonScaleTimers[button] = 0.2f;
            button.localScale = Vector3.one * 1.15f;

            // Glow goster
            if (glow != null)
                glow.color = new Color(1f, 1f, 1f, 0.9f);
        }
        else
        {
            // Glow gizle
            if (glow != null)
                glow.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    void UpdateButtonScales()
    {
        buttonScaleKeys.Clear();
        foreach (var kvp in buttonScaleTimers)
            buttonScaleKeys.Add(kvp.Key);
        foreach (RectTransform btn in buttonScaleKeys)
        {
            if (btn == null) continue;

            float timer = buttonScaleTimers[btn];
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                buttonScaleTimers[btn] = timer;

                // Smoothstep scale punch (1.15 -> 1.0)
                float t = 1f - Mathf.Clamp01(timer / 0.2f);
                float scale = Mathf.Lerp(1.15f, 1f, t * t * (3f - 2f * t));
                btn.localScale = Vector3.one * scale;
            }
        }
    }

    // === DASH COOLDOWN ===

    /// <summary>
    /// Dash cooldown'u baslat (PlayerController'dan cagirilir)
    /// </summary>
    public void StartDashCooldown(float duration)
    {
        dashCooldownDuration = duration;
        dashCooldownTimer = duration;
        isDashOnCooldown = true;
        if (dashCooldownFill != null) dashCooldownFill.fillAmount = 1f;
    }

    public void StartRollCooldown(float duration)
    {
        rollCooldownDuration = duration;
        rollCooldownTimer = duration;
        isRollOnCooldown = true;
        if (rollCooldownFill != null) rollCooldownFill.fillAmount = 1f;
    }

    void UpdateCooldowns()
    {
        UpdateSingleCooldown(ref dashCooldownTimer, ref isDashOnCooldown, dashCooldownDuration, dashCooldownFill);
        UpdateSingleCooldown(ref rollCooldownTimer, ref isRollOnCooldown, rollCooldownDuration, rollCooldownFill);
    }

    void UpdateSingleCooldown(ref float timer, ref bool isOnCooldown, float duration, Image fill)
    {
        if (!isOnCooldown) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            isOnCooldown = false;
            if (fill != null) fill.fillAmount = 0f;
        }
        else
        {
            if (fill != null) fill.fillAmount = timer / duration;
        }
    }

    // === AYARLAR ===

    void LoadSettings()
    {
        IsEnabled = PlayerPrefs.GetInt(PREF_ENABLED, Application.isMobilePlatform ? 1 : 0) == 1;
        ButtonSizeScale = PlayerPrefs.GetFloat(PREF_BUTTON_SIZE, 1.0f);
        Opacity = PlayerPrefs.GetFloat(PREF_OPACITY, 0.85f);
        swipeGesturesEnabled = PlayerPrefs.GetInt(PREF_SWIPE_ENABLED, 1) == 1;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        PlayerPrefs.SetInt(PREF_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();

        gameObject.SetActive(enabled);

        if (canvas != null)
            canvas.gameObject.SetActive(enabled);
    }

    public void SetButtonSize(float scale)
    {
        ButtonSizeScale = Mathf.Clamp(scale, 0.5f, 1.5f);
        PlayerPrefs.SetFloat(PREF_BUTTON_SIZE, ButtonSizeScale);
        PlayerPrefs.Save();

        ApplyButtonSize();
    }

    public void SetOpacity(float alpha)
    {
        Opacity = Mathf.Clamp(alpha, 0.3f, 1.0f);
        PlayerPrefs.SetFloat(PREF_OPACITY, Opacity);
        PlayerPrefs.Save();

        ApplyOpacity(Opacity);
    }

    void ApplyButtonSize()
    {
        // Buton boyutlarini guncelle
        if (jumpButton != null) jumpButton.sizeDelta = Vector2.one * Mathf.Max(jumpButtonSize * ButtonSizeScale, MIN_BUTTON_SIZE);
        if (fireButton != null) fireButton.sizeDelta = Vector2.one * Mathf.Max(fireButtonSize * ButtonSizeScale, MIN_BUTTON_SIZE);
        if (dashButton != null) dashButton.sizeDelta = Vector2.one * Mathf.Max(actionButtonSize * ButtonSizeScale, MIN_BUTTON_SIZE);
        if (rollButton != null) rollButton.sizeDelta = Vector2.one * Mathf.Max(smallButtonSize * ButtonSizeScale, MIN_BUTTON_SIZE);

        // Joystick boyutu
        float jScale = joystickBgSize * ButtonSizeScale;
        if (joystickBackground != null)
            joystickBackground.sizeDelta = new Vector2(jScale, jScale);
        if (joystickContainer != null)
            joystickContainer.sizeDelta = new Vector2(jScale, jScale);
    }

    void ApplyOpacity(float alpha)
    {
        if (mainCanvasGroup != null)
            mainCanvasGroup.alpha = alpha;
    }
}
