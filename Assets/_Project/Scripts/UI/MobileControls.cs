using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;

/// <summary>
/// Profesyonel mobil kontroller - Floating joystick, neon butonlar, cooldown overlay.
/// Prosedural gorsellerle programatik olarak olusturulur.
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
    private bool isJoystickActive;
    private int joystickTouchId = -1;
    private Vector2 joystickOrigin; // Floating: dokunulan nokta

    // === BUTTON REFS ===
    private RectTransform jumpButton;
    private RectTransform fireButton;
    private RectTransform dashButton;
    private RectTransform reloadButton;
    private RectTransform weaponSwitchButton;
    private RectTransform rollButton;
    private RectTransform groundPoundButton;
    private RectTransform grappleButton;
    private RectTransform pauseButton;

    // Button glow images
    private Image jumpGlow;
    private Image fireGlow;
    private Image dashGlow;
    private Image reloadGlow;
    private Image switchGlow;
    private Image rollGlow;
    private Image groundPoundGlow;
    private Image grappleGlow;

    // Button icon images (for tinting)
    private Image jumpIcon;
    private Image fireIcon;
    private Image dashIcon;
    private Image reloadIcon;
    private Image switchIcon;
    private Image rollIcon;
    private Image groundPoundIcon;
    private Image grappleIcon;

    // === COOLDOWN ===
    private Image dashCooldownFill;
    private float dashCooldownDuration = 0.8f;
    private float dashCooldownTimer = 0f;
    private bool isDashOnCooldown = false;

    private Image rollCooldownFill;
    private float rollCooldownDuration = 1f;
    private float rollCooldownTimer = 0f;
    private bool isRollOnCooldown = false;

    private Image grappleCooldownFill;
    private float grappleCooldownDuration = 3f;
    private float grappleCooldownTimer = 0f;
    private bool isGrappleOnCooldown = false;

    private Image groundPoundCooldownFill;
    private float groundPoundCooldownDuration = 2f;
    private float groundPoundCooldownTimer = 0f;
    private bool isGroundPoundOnCooldown = false;

    // === SETTINGS ===
    private const string PREF_ENABLED = "MobileControls_Enabled";
    private const string PREF_BUTTON_SIZE = "MobileControls_ButtonSize";
    private const string PREF_OPACITY = "MobileControls_Opacity";

    public float ButtonSizeScale { get; private set; } = 1.0f;
    public float Opacity { get; private set; } = 0.8f;
    public bool IsEnabled { get; private set; } = true;

    // === BUTTON STATES ===
    private bool isJumpPressed;
    private bool isFirePressed;
    private bool isDashPressed;
    private bool isReloadPressed;
    private bool isSwitchPressed;
    private bool isRollPressed;
    private bool isGrapplePressed;
    private bool isGroundPoundPressed;
    private bool isPausePressed;

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
    private float joystickRadius = 80f;
    private float deadZone = 0.1f;
    private int joystickBgSize = 200; // Pixel
    private int joystickHandleSize = 80; // Pixel

    // === BUTTON SIZES (base, before scale) ===
    private float jumpButtonSize = 130f;
    private float actionButtonSize = 100f;
    private float smallButtonSize = 70f;
    private float contextButtonSize = 90f;  // Roll/GroundPound
    private float grappleButtonSize = 85f;
    private float pauseButtonSize = 50f;

    // Public properties for PlayerController
    public Vector2 MoveInput => joystickInput;
    public bool JumpPressed => isJumpPressed;
    public bool FireHeld => isFirePressed;
    public bool DashPressed => isDashPressed;
    public bool ReloadPressed => isReloadPressed;
    public bool SwitchWeaponPressed => isSwitchPressed;
    public bool RollPressed => isRollPressed;
    public bool GrapplePressed => isGrapplePressed;
    public bool GroundPoundPressed => isGroundPoundPressed;
    public bool PausePressed => isPausePressed;

    void Awake()
    {
        Instance = this;
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

        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;

        if (!isMobile && !IsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        // Editor'de sadece acikca etkinlestirildiyse goster
        #if UNITY_EDITOR
        if (!IsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }
        #endif

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
        isReloadPressed = false;
        isSwitchPressed = false;
        isRollPressed = false;
        isGrapplePressed = false;
        isGroundPoundPressed = false;
        isPausePressed = false;

        HandleTouchInput();
        UpdateAnimations();
        UpdateDashCooldown();
        UpdateRollCooldown();
        UpdateGrappleCooldown();
        UpdateGroundPoundCooldown();
        UpdateContextualButtons();
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
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        mainCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        mainCanvasGroup.interactable = false; // Touch'lar manuel handle ediliyor
        mainCanvasGroup.blocksRaycasts = false;

        transform.SetParent(canvasObj.transform, false);
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
        joystickContainer.anchorMin = joystickContainer.anchorMax = new Vector2(0f, 0f);
        joystickContainer.pivot = new Vector2(0.5f, 0.5f);
        joystickContainer.sizeDelta = new Vector2(joystickBgSize, joystickBgSize);
        joystickContainer.anchoredPosition = new Vector2(180, 180); // Varsayilan pozisyon

        // Background
        GameObject bgObj = new GameObject("JoystickBg");
        bgObj.transform.SetParent(containerObj.transform, false);

        joystickBackground = bgObj.AddComponent<RectTransform>();
        joystickBackground.anchorMin = joystickBackground.anchorMax = new Vector2(0.5f, 0.5f);
        joystickBackground.pivot = new Vector2(0.5f, 0.5f);
        joystickBackground.anchoredPosition = Vector2.zero;
        joystickBackground.sizeDelta = new Vector2(joystickBgSize, joystickBgSize);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = MobileControlsVisualFactory.CreateJoystickBackground(128);
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
        joystickGlowImage.sprite = MobileControlsVisualFactory.CreateJoystickGlowRing(128);
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
        handleImage.sprite = MobileControlsVisualFactory.CreateJoystickHandle(48);
        handleImage.type = Image.Type.Simple;
        handleImage.preserveAspect = true;

        // Floating: baslangicta joystick gizli
        SetJoystickVisible(false);
    }

    // === BUTON OLUSTURMA ===

    void CreateActionButtons()
    {
        // Jump - sag alt, en buyuk
        jumpButton = CreateActionButton("JumpButton", "ZIPLA",
            MobileControlsVisualFactory.CreateArrowUpIcon(32),
            MobileControlsVisualFactory.NeonGreen,
            new Vector2(0.88f, 0.15f), jumpButtonSize,
            out jumpGlow, out jumpIcon);

        // Fire - jump'in solunda, buyuk
        fireButton = CreateActionButton("FireButton", "ATES",
            MobileControlsVisualFactory.CreateCrosshairIcon(32),
            MobileControlsVisualFactory.NeonOrange,
            new Vector2(0.72f, 0.08f), jumpButtonSize,
            out fireGlow, out fireIcon);

        // Dash - jump'in sol ustunde
        dashButton = CreateActionButton("DashButton", "DASH",
            MobileControlsVisualFactory.CreateLightningIcon(32),
            MobileControlsVisualFactory.NeonCyan,
            new Vector2(0.72f, 0.32f), actionButtonSize,
            out dashGlow, out dashIcon);

        // Dash cooldown overlay
        CreateDashCooldownOverlay();

        // Reload - fire'in ustunde
        reloadButton = CreateActionButton("ReloadButton", "R",
            MobileControlsVisualFactory.CreateReloadIcon(32),
            MobileControlsVisualFactory.NeonYellow,
            new Vector2(0.58f, 0.18f), smallButtonSize,
            out reloadGlow, out reloadIcon);

        // Weapon Switch - dash'in ustunde
        weaponSwitchButton = CreateActionButton("SwitchButton", "",
            MobileControlsVisualFactory.CreateSwitchIcon(32),
            MobileControlsVisualFactory.NeonPink,
            new Vector2(0.58f, 0.38f), smallButtonSize,
            out switchGlow, out switchIcon);

        // === YENI BUTONLAR ===

        // Roll/Takla - jump'in ustunde, sadece yerdeyken gorunur
        rollButton = CreateActionButton("RollButton", "TAKLA",
            MobileControlsVisualFactory.CreateRollIcon(32),
            MobileControlsVisualFactory.NeonPurple,
            new Vector2(0.88f, 0.38f), contextButtonSize,
            out rollGlow, out rollIcon);
        CreateCooldownOverlay(rollButton, out rollCooldownFill);

        // Ground Pound - roll ile ayni pozisyon, sadece havadayken gorunur
        groundPoundButton = CreateActionButton("GroundPoundButton", "EZME",
            MobileControlsVisualFactory.CreateGroundPoundIcon(32),
            MobileControlsVisualFactory.NeonRed,
            new Vector2(0.88f, 0.38f), contextButtonSize,
            out groundPoundGlow, out groundPoundIcon);
        CreateCooldownOverlay(groundPoundButton, out groundPoundCooldownFill);
        groundPoundButton.gameObject.SetActive(false); // Baslangicta gizli

        // Grapple Hook - sol-orta alanda
        grappleButton = CreateActionButton("GrappleButton", "KANCA",
            MobileControlsVisualFactory.CreateGrappleIcon(32),
            MobileControlsVisualFactory.NeonCyan,
            new Vector2(0.42f, 0.30f), grappleButtonSize,
            out grappleGlow, out grappleIcon);
        CreateCooldownOverlay(grappleButton, out grappleCooldownFill);

        // Pause butonu - sag ust kose, kucuk ve soluk
        CreatePauseButton();
    }

    RectTransform CreateActionButton(string name, string label, Sprite iconSprite, Color neonColor,
        Vector2 anchor, float size, out Image glowImage, out Image iconImage)
    {
        float scaledSize = size * ButtonSizeScale;

        // Ana buton objesi
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(safeAreaPanel, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(scaledSize, scaledSize);

        // Arka plan daire
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.sprite = MobileControlsVisualFactory.CreateButtonCircle(64, neonColor);
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
        glowImage.sprite = MobileControlsVisualFactory.CreateButtonGlowCircle(64, neonColor);
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

    void CreateDashCooldownOverlay()
    {
        CreateCooldownOverlay(dashButton, out dashCooldownFill);
    }

    void CreateCooldownOverlay(RectTransform parentButton, out Image cooldownFill)
    {
        cooldownFill = null;
        if (parentButton == null) return;

        GameObject cdObj = new GameObject("CooldownFill");
        cdObj.transform.SetParent(parentButton.transform, false);

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

    void CreatePauseButton()
    {
        float scaledSize = pauseButtonSize * ButtonSizeScale;

        GameObject btnObj = new GameObject("PauseButton");
        btnObj.transform.SetParent(safeAreaPanel, false);

        pauseButton = btnObj.AddComponent<RectTransform>();
        pauseButton.anchorMin = pauseButton.anchorMax = new Vector2(0.95f, 0.93f);
        pauseButton.pivot = new Vector2(0.5f, 0.5f);
        pauseButton.anchoredPosition = Vector2.zero;
        pauseButton.sizeDelta = new Vector2(scaledSize, scaledSize);

        // Soluk arka plan
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.sprite = MobileControlsVisualFactory.CreateButtonCircle(64,
            new Color(0.7f, 0.7f, 0.8f, 0.6f));
        bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = true;

        // Pause ikonu
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);

        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.2f, 0.2f);
        iconRt.anchorMax = new Vector2(0.8f, 0.8f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = MobileControlsVisualFactory.CreatePauseIcon(32);
        iconImg.type = Image.Type.Simple;
        iconImg.preserveAspect = true;
        iconImg.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);

        buttonScaleTimers[pauseButton] = 0f;
    }

    // === TOUCH INPUT ===

    void HandleTouchInput()
    {
        var touches = Touch.activeTouches;

        // Kalkan dokunuslari temizle
        List<int> toRemove = new List<int>();
        foreach (var kvp in activeTouches)
        {
            bool found = false;
            foreach (var touch in touches)
            {
                if (touch.touchId == kvp.Key) { found = true; break; }
            }
            if (!found)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (int id in toRemove)
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
            }
        }
    }

    string GetTouchedElement(Vector2 screenPos)
    {
        // Pause butonu - en yuksek oncelik (her zaman erisilebilir)
        if (IsPointInButton(screenPos, pauseButton)) return "Pause";

        // Ana aksiyon butonlari
        if (IsPointInButton(screenPos, jumpButton)) return "Jump";
        if (IsPointInButton(screenPos, fireButton)) return "Fire";
        if (IsPointInButton(screenPos, dashButton)) return "Dash";

        // Baglamsal Roll/GroundPound (sadece aktif olan kontrol edilir)
        if (rollButton != null && rollButton.gameObject.activeSelf && IsPointInButton(screenPos, rollButton)) return "Roll";
        if (groundPoundButton != null && groundPoundButton.gameObject.activeSelf && IsPointInButton(screenPos, groundPoundButton)) return "GroundPound";

        // Grapple
        if (IsPointInButton(screenPos, grappleButton)) return "Grapple";

        // Yardimci butonlar
        if (IsPointInButton(screenPos, reloadButton)) return "Reload";
        if (IsPointInButton(screenPos, weaponSwitchButton)) return "Switch";

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
                break;

            case "Fire":
                isFirePressed = true;
                AnimateButtonPress(fireButton, fireGlow, true);
                break;

            case "Dash":
                if (!isDashOnCooldown)
                {
                    isDashPressed = true;
                    AnimateButtonPress(dashButton, dashGlow, true);
                }
                break;

            case "Reload":
                isReloadPressed = true;
                AnimateButtonPress(reloadButton, reloadGlow, true);
                break;

            case "Switch":
                isSwitchPressed = true;
                AnimateButtonPress(weaponSwitchButton, switchGlow, true);
                break;

            case "Roll":
                if (!isRollOnCooldown)
                {
                    isRollPressed = true;
                    AnimateButtonPress(rollButton, rollGlow, true);
                }
                break;

            case "GroundPound":
                if (!isGroundPoundOnCooldown)
                {
                    isGroundPoundPressed = true;
                    AnimateButtonPress(groundPoundButton, groundPoundGlow, true);
                }
                break;

            case "Grapple":
                if (!isGrappleOnCooldown)
                {
                    isGrapplePressed = true;
                    AnimateButtonPress(grappleButton, grappleGlow, true);
                }
                break;

            case "Pause":
                isPausePressed = true;
                AnimateButtonPress(pauseButton, null, true);
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
                AnimateButtonPress(fireButton, fireGlow, false);
                break;

            case "Dash":
                AnimateButtonPress(dashButton, dashGlow, false);
                break;

            case "Reload":
                AnimateButtonPress(reloadButton, reloadGlow, false);
                break;

            case "Switch":
                AnimateButtonPress(weaponSwitchButton, switchGlow, false);
                break;

            case "Roll":
                AnimateButtonPress(rollButton, rollGlow, false);
                break;

            case "GroundPound":
                AnimateButtonPress(groundPoundButton, groundPoundGlow, false);
                break;

            case "Grapple":
                AnimateButtonPress(grappleButton, grappleGlow, false);
                break;

            case "Pause":
                AnimateButtonPress(pauseButton, null, false);
                break;
        }
    }

    // === JOYSTICK LOGIC ===

    void MoveJoystickTo(Vector2 screenPos)
    {
        if (joystickContainer == null || canvas == null) return;

        // Screen pozisyonunu canvas lokal pozisyonuna cevir
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPos, null, out localPoint);

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

    void SetJoystickVisible(bool visible)
    {
        if (joystickContainer == null) return;

        // Tum child image'larin alpha'sini ayarla
        Image[] images = joystickContainer.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if (img == joystickGlowImage) continue; // Glow ayri kontrol
            Color c = img.color;
            c.a = visible ? Mathf.Min(c.a, Opacity) : 0f;
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
        List<RectTransform> keys = new List<RectTransform>(buttonScaleTimers.Keys);
        foreach (RectTransform btn in keys)
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

        if (dashCooldownFill != null)
            dashCooldownFill.fillAmount = 1f;
    }

    void UpdateDashCooldown()
    {
        if (!isDashOnCooldown) return;

        dashCooldownTimer -= Time.deltaTime;
        if (dashCooldownTimer <= 0f)
        {
            dashCooldownTimer = 0f;
            isDashOnCooldown = false;

            if (dashCooldownFill != null)
                dashCooldownFill.fillAmount = 0f;
        }
        else
        {
            if (dashCooldownFill != null)
                dashCooldownFill.fillAmount = dashCooldownTimer / dashCooldownDuration;
        }
    }

    // === ROLL COOLDOWN ===

    public void StartRollCooldown(float duration)
    {
        rollCooldownDuration = duration;
        rollCooldownTimer = duration;
        isRollOnCooldown = true;

        if (rollCooldownFill != null)
            rollCooldownFill.fillAmount = 1f;
    }

    void UpdateRollCooldown()
    {
        if (!isRollOnCooldown) return;

        rollCooldownTimer -= Time.deltaTime;
        if (rollCooldownTimer <= 0f)
        {
            rollCooldownTimer = 0f;
            isRollOnCooldown = false;

            if (rollCooldownFill != null)
                rollCooldownFill.fillAmount = 0f;
        }
        else
        {
            if (rollCooldownFill != null)
                rollCooldownFill.fillAmount = rollCooldownTimer / rollCooldownDuration;
        }
    }

    // === GRAPPLE COOLDOWN ===

    public void StartGrappleCooldown(float duration)
    {
        grappleCooldownDuration = duration;
        grappleCooldownTimer = duration;
        isGrappleOnCooldown = true;

        if (grappleCooldownFill != null)
            grappleCooldownFill.fillAmount = 1f;
    }

    void UpdateGrappleCooldown()
    {
        if (!isGrappleOnCooldown) return;

        grappleCooldownTimer -= Time.deltaTime;
        if (grappleCooldownTimer <= 0f)
        {
            grappleCooldownTimer = 0f;
            isGrappleOnCooldown = false;

            if (grappleCooldownFill != null)
                grappleCooldownFill.fillAmount = 0f;
        }
        else
        {
            if (grappleCooldownFill != null)
                grappleCooldownFill.fillAmount = grappleCooldownTimer / grappleCooldownDuration;
        }
    }

    // === GROUND POUND COOLDOWN ===

    public void StartGroundPoundCooldown(float duration)
    {
        groundPoundCooldownDuration = duration;
        groundPoundCooldownTimer = duration;
        isGroundPoundOnCooldown = true;

        if (groundPoundCooldownFill != null)
            groundPoundCooldownFill.fillAmount = 1f;
    }

    void UpdateGroundPoundCooldown()
    {
        if (!isGroundPoundOnCooldown) return;

        groundPoundCooldownTimer -= Time.deltaTime;
        if (groundPoundCooldownTimer <= 0f)
        {
            groundPoundCooldownTimer = 0f;
            isGroundPoundOnCooldown = false;

            if (groundPoundCooldownFill != null)
                groundPoundCooldownFill.fillAmount = 0f;
        }
        else
        {
            if (groundPoundCooldownFill != null)
                groundPoundCooldownFill.fillAmount = groundPoundCooldownTimer / groundPoundCooldownDuration;
        }
    }

    // === BAGLAMSAL BUTON DEGISTIRME ===

    void UpdateContextualButtons()
    {
        // Oyuncu state'ine gore Roll/GroundPound butonlarini degistir
        if (GameManager.Instance == null || GameManager.Instance.player == null) return;

        PlayerController player = GameManager.Instance.player.GetComponent<PlayerController>();
        if (player == null) return;

        bool isGrounded = player.IsGrounded;

        // Roll: sadece yerde, GroundPound: sadece havada
        if (rollButton != null)
            rollButton.gameObject.SetActive(isGrounded);

        if (groundPoundButton != null)
            groundPoundButton.gameObject.SetActive(!isGrounded);
    }

    // === AYARLAR ===

    void LoadSettings()
    {
        IsEnabled = PlayerPrefs.GetInt(PREF_ENABLED, Application.isMobilePlatform ? 1 : 0) == 1;
        ButtonSizeScale = PlayerPrefs.GetFloat(PREF_BUTTON_SIZE, 1.0f);
        Opacity = PlayerPrefs.GetFloat(PREF_OPACITY, 0.8f);
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
        if (jumpButton != null) jumpButton.sizeDelta = Vector2.one * jumpButtonSize * ButtonSizeScale;
        if (fireButton != null) fireButton.sizeDelta = Vector2.one * jumpButtonSize * ButtonSizeScale;
        if (dashButton != null) dashButton.sizeDelta = Vector2.one * actionButtonSize * ButtonSizeScale;
        if (reloadButton != null) reloadButton.sizeDelta = Vector2.one * smallButtonSize * ButtonSizeScale;
        if (weaponSwitchButton != null) weaponSwitchButton.sizeDelta = Vector2.one * smallButtonSize * ButtonSizeScale;
        if (rollButton != null) rollButton.sizeDelta = Vector2.one * contextButtonSize * ButtonSizeScale;
        if (groundPoundButton != null) groundPoundButton.sizeDelta = Vector2.one * contextButtonSize * ButtonSizeScale;
        if (grappleButton != null) grappleButton.sizeDelta = Vector2.one * grappleButtonSize * ButtonSizeScale;
        if (pauseButton != null) pauseButton.sizeDelta = Vector2.one * pauseButtonSize * ButtonSizeScale;

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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
