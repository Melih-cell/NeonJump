using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;

/// <summary>
/// Mobil kontroller - Sanal joystick ve butonlar
/// </summary>
public class MobileControls : MonoBehaviour
{
    public static MobileControls Instance { get; private set; }

    [Header("Movement Joystick")]
    private RectTransform joystickBackground;
    private RectTransform joystickHandle;
    private Vector2 joystickInput;
    private bool isJoystickActive;
    private int joystickTouchId = -1;

    [Header("Action Buttons")]
    private RectTransform jumpButton;
    private RectTransform fireButton;
    private RectTransform dashButton;
    private RectTransform reloadButton;
    private RectTransform weaponSwitchButton;

    [Header("Settings")]
    public float joystickRadius = 80f;
    public float buttonSize = 80f;
    public float deadZone = 0.1f;

    [Header("Colors")]
    private Color buttonNormalColor = new Color(0.1f, 0.1f, 0.2f, 0.7f);
    private Color buttonPressedColor = new Color(0.2f, 0.4f, 0.6f, 0.9f);
    private Color neonCyan = new Color(0f, 1f, 1f);
    private Color neonPink = new Color(1f, 0f, 0.6f);
    private Color neonYellow = new Color(1f, 1f, 0f);
    private Color neonGreen = new Color(0f, 1f, 0.5f);
    private Color neonOrange = new Color(1f, 0.5f, 0f);

    // Button states
    private bool isJumpPressed;
    private bool isFirePressed;
    private bool isDashPressed;
    private bool isReloadPressed;
    private bool isSwitchPressed;

    private Canvas canvas;
    private Dictionary<int, string> activeTouches = new Dictionary<int, string>();

    // Public properties for PlayerController
    public Vector2 MoveInput => joystickInput;
    public bool JumpPressed => isJumpPressed;
    public bool FireHeld => isFirePressed;
    public bool DashPressed => isDashPressed;
    public bool ReloadPressed => isReloadPressed;
    public bool SwitchWeaponPressed => isSwitchPressed;

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
        // Sadece mobilde goster
        bool isMobile = Application.isMobilePlatform ||
                        UnityEngine.InputSystem.Touchscreen.current != null;

        if (!isMobile)
        {
            gameObject.SetActive(false);
            return;
        }

        CreateCanvas();
        CreateJoystick();
        CreateActionButtons();
    }

    void Update()
    {
        // Her frame basinda buton state'lerini sifirla (one-shot icin)
        isJumpPressed = false;
        isDashPressed = false;
        isReloadPressed = false;
        isSwitchPressed = false;

        HandleTouchInput();
    }

    void CreateCanvas()
    {
        // Mevcut canvas'i bul veya olustur
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MobileControlsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform, false);
        }
    }

    void CreateJoystick()
    {
        // Joystick Container (sol alt)
        GameObject joystickContainer = new GameObject("JoystickContainer");
        joystickContainer.transform.SetParent(canvas.transform, false);

        RectTransform containerRt = joystickContainer.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0, 0);
        containerRt.anchorMax = new Vector2(0, 0);
        containerRt.pivot = new Vector2(0, 0);
        containerRt.anchoredPosition = new Vector2(40, 40);
        containerRt.sizeDelta = new Vector2(200, 200);

        // Joystick Background
        GameObject bgObj = new GameObject("JoystickBackground");
        bgObj.transform.SetParent(joystickContainer.transform, false);

        joystickBackground = bgObj.AddComponent<RectTransform>();
        joystickBackground.anchorMin = joystickBackground.anchorMax = new Vector2(0.5f, 0.5f);
        joystickBackground.pivot = new Vector2(0.5f, 0.5f);
        joystickBackground.anchoredPosition = Vector2.zero;
        joystickBackground.sizeDelta = new Vector2(joystickRadius * 2 + 20, joystickRadius * 2 + 20);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.6f);

        // Neon border
        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.5f);
        bgOutline.effectDistance = new Vector2(2, 2);

        // Joystick Handle
        GameObject handleObj = new GameObject("JoystickHandle");
        handleObj.transform.SetParent(bgObj.transform, false);

        joystickHandle = handleObj.AddComponent<RectTransform>();
        joystickHandle.anchorMin = joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandle.pivot = new Vector2(0.5f, 0.5f);
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickHandle.sizeDelta = new Vector2(60, 60);

        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = neonCyan;

        // Handle glow
        Outline handleGlow = handleObj.AddComponent<Outline>();
        handleGlow.effectColor = new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.7f);
        handleGlow.effectDistance = new Vector2(3, 3);

        // Arka planda yon oklari
        CreateDirectionIndicators(bgObj.transform);
    }

    void CreateDirectionIndicators(Transform parent)
    {
        // Sadece 4 yon icin kucuk cizgiler
        Vector2[] positions = {
            new Vector2(0, joystickRadius - 5),     // Up
            new Vector2(joystickRadius - 5, 0),     // Right
            new Vector2(0, -joystickRadius + 5),    // Down
            new Vector2(-joystickRadius + 5, 0)     // Left
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = new GameObject($"DirectionIndicator_{i}");
            indicator.transform.SetParent(parent, false);

            RectTransform rt = indicator.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = positions[i];
            rt.sizeDelta = new Vector2(10, 10);

            Image img = indicator.AddComponent<Image>();
            img.color = new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.3f);
        }
    }

    void CreateActionButtons()
    {
        // Sag tarafta aksiyon butonlari
        float rightMargin = 40f;
        float bottomMargin = 40f;
        float spacing = 20f;

        // Jump Button (en altta, buyuk)
        jumpButton = CreateButton("JumpButton", "ZIPLA",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-rightMargin, bottomMargin),
            new Vector2(100, 100), neonGreen);

        // Fire Button (jump'in solunda, buyuk)
        fireButton = CreateButton("FireButton", "ATES",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-rightMargin - 120, bottomMargin),
            new Vector2(100, 100), neonOrange);

        // Dash Button (jump'in ustunde)
        dashButton = CreateButton("DashButton", "DASH",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-rightMargin, bottomMargin + 120),
            new Vector2(70, 70), neonCyan);

        // Reload Button (fire'in ustunde)
        reloadButton = CreateButton("ReloadButton", "R",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-rightMargin - 120, bottomMargin + 120),
            new Vector2(60, 60), neonYellow);

        // Weapon Switch Button (dash'in ustunde)
        weaponSwitchButton = CreateButton("SwitchButton", "<<>>",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-rightMargin - 40, bottomMargin + 200),
            new Vector2(80, 50), neonPink);
    }

    RectTransform CreateButton(string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = buttonNormalColor;

        // Neon border
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, 2);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);

        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = size.x > 80 ? 18 : 14;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = color;
        labelText.alignment = TextAnchor.MiddleCenter;

        return rt;
    }

    void HandleTouchInput()
    {
        var touches = Touch.activeTouches;

        // Aktif olmayan dokunuslari temizle
        List<int> toRemove = new List<int>();
        foreach (var kvp in activeTouches)
        {
            bool found = false;
            foreach (var touch in touches)
            {
                if (touch.touchId == kvp.Key)
                {
                    found = true;
                    break;
                }
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

        // Yeni ve devam eden dokunuslari isle
        foreach (var touch in touches)
        {
            Vector2 screenPos = touch.screenPosition;

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Hangi elemente dokunuldu
                string element = GetTouchedElement(screenPos);
                if (!string.IsNullOrEmpty(element))
                {
                    activeTouches[touch.touchId] = element;
                    OnTouchBegan(element, screenPos);
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
        // Joystick alani (sol yarim ekran)
        if (screenPos.x < Screen.width * 0.4f && screenPos.y < Screen.height * 0.5f)
        {
            return "Joystick";
        }

        // Buton kontrolleri
        if (IsPointInButton(screenPos, jumpButton)) return "Jump";
        if (IsPointInButton(screenPos, fireButton)) return "Fire";
        if (IsPointInButton(screenPos, dashButton)) return "Dash";
        if (IsPointInButton(screenPos, reloadButton)) return "Reload";
        if (IsPointInButton(screenPos, weaponSwitchButton)) return "Switch";

        return null;
    }

    bool IsPointInButton(Vector2 screenPos, RectTransform button)
    {
        if (button == null) return false;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            null,
            out localPoint);

        return RectTransformUtility.RectangleContainsScreenPoint(button, screenPos, null);
    }

    void OnTouchBegan(string element, Vector2 screenPos)
    {
        switch (element)
        {
            case "Joystick":
                isJoystickActive = true;
                UpdateJoystick(screenPos);
                break;
            case "Jump":
                isJumpPressed = true;
                SetButtonPressed(jumpButton, true);
                break;
            case "Fire":
                isFirePressed = true;
                SetButtonPressed(fireButton, true);
                break;
            case "Dash":
                isDashPressed = true;
                SetButtonPressed(dashButton, true);
                break;
            case "Reload":
                isReloadPressed = true;
                SetButtonPressed(reloadButton, true);
                break;
            case "Switch":
                isSwitchPressed = true;
                SetButtonPressed(weaponSwitchButton, true);
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
                joystickInput = Vector2.zero;
                joystickHandle.anchoredPosition = Vector2.zero;
                break;
            case "Jump":
                SetButtonPressed(jumpButton, false);
                break;
            case "Fire":
                isFirePressed = false;
                SetButtonPressed(fireButton, false);
                break;
            case "Dash":
                SetButtonPressed(dashButton, false);
                break;
            case "Reload":
                SetButtonPressed(reloadButton, false);
                break;
            case "Switch":
                SetButtonPressed(weaponSwitchButton, false);
                break;
        }
    }

    void UpdateJoystick(Vector2 screenPos)
    {
        if (joystickBackground == null) return;

        // Screen pozisyonunu joystick'in lokal pozisyonuna cevir
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
        Vector2 direction = screenPos - joystickCenter;

        // Radius'a gore sinirla
        float magnitude = direction.magnitude;
        if (magnitude > joystickRadius)
        {
            direction = direction.normalized * joystickRadius;
        }

        // Handle pozisyonunu guncelle
        joystickHandle.anchoredPosition = direction;

        // Input degerini hesapla (-1 ile 1 arasi)
        joystickInput = direction / joystickRadius;

        // Dead zone uygula
        if (joystickInput.magnitude < deadZone)
        {
            joystickInput = Vector2.zero;
        }
    }

    void SetButtonPressed(RectTransform button, bool pressed)
    {
        if (button == null) return;

        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = pressed ? buttonPressedColor : buttonNormalColor;
        }

        // Scale efekti
        button.localScale = pressed ? Vector3.one * 0.9f : Vector3.one;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
