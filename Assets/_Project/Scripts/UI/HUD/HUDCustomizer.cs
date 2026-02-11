using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUD ozellestirme sistemi.
/// Kullanicinin HUD elementlerini suruleyerek tasimasi ve boyutlandirmasina izin verir.
/// </summary>
public class HUDCustomizer : MonoBehaviour
{
    public static HUDCustomizer Instance { get; private set; }

    [Header("Durum")]
    public bool isEditMode = false;

    [Header("HUD Referanslari")]
    public List<HUDDraggableElement> draggableElements = new List<HUDDraggableElement>();

    [Header("Edit Mode UI")]
    public GameObject editModeOverlay;
    public TMP_Text editModeText;
    public Button saveButton;
    public Button resetButton;
    public Button closeButton;
    public TMP_Dropdown presetDropdown;

    [Header("Ayarlar")]
    public Color editModeHighlightColor = new Color(0f, 1f, 1f, 0.3f);
    public Color selectedElementColor = new Color(1f, 1f, 0f, 0.5f);

    private HUDDraggableElement selectedElement;
    private Vector2 dragOffset;
    private bool isDragging = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Edit mode overlay'i gizle
        if (editModeOverlay != null)
            editModeOverlay.SetActive(false);

        SetupListeners();
        SetupPresetDropdown();
    }

    void SetupListeners()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveLayout);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefault);

        if (closeButton != null)
            closeButton.onClick.AddListener(ExitEditMode);

        if (presetDropdown != null)
            presetDropdown.onValueChanged.AddListener(OnPresetChanged);
    }

    void SetupPresetDropdown()
    {
        if (presetDropdown == null) return;

        presetDropdown.ClearOptions();

        var options = new List<string>();

        if (LocalizationManager.Instance != null)
        {
            options.Add("Minimal");
            options.Add("Normal");
            options.Add("Tam");
            options.Add("Ozel");
        }
        else
        {
            options.Add("Minimal");
            options.Add("Normal");
            options.Add("Full");
            options.Add("Custom");
        }

        presetDropdown.AddOptions(options);

        // Kaydedilmis preset'i yukle
        if (SaveManager.Instance != null)
        {
            string preset = SaveManager.Instance.Data.hudLayoutPreset;
            int index = preset switch
            {
                "minimal" => 0,
                "normal" => 1,
                "full" => 2,
                _ => 3
            };
            presetDropdown.value = index;
        }
    }

    void Update()
    {
        if (!isEditMode) return;

        HandleInput();
        HandleDrag();
    }

    void HandleInput()
    {
        // F10 ile edit mode ac/kapat
        if (Input.GetKeyDown(KeyCode.F10))
        {
            if (isEditMode)
                ExitEditMode();
            else
                EnterEditMode();
        }

        // ESC ile cik
        if (Input.GetKeyDown(KeyCode.Escape) && isEditMode)
        {
            ExitEditMode();
        }
    }

    void HandleDrag()
    {
        if (!isEditMode) return;

        // Sol mouse tikla
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectElement();
        }

        // Surukle
        if (Input.GetMouseButton(0) && isDragging && selectedElement != null)
        {
            DragSelectedElement();
        }

        // Birak
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Sag tik ile boyut ayarla
        if (Input.GetMouseButton(1) && selectedElement != null)
        {
            ResizeSelectedElement();
        }
    }

    void TrySelectElement()
    {
        // Mouse pozisyonunda element var mi?
        Vector2 mousePos = Input.mousePosition;

        foreach (var element in draggableElements)
        {
            if (element == null || element.rectTransform == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(element.rectTransform, mousePos))
            {
                SelectElement(element);
                isDragging = true;

                // Drag offset hesapla
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    element.rectTransform.parent as RectTransform,
                    mousePos,
                    null,
                    out Vector2 localPoint);

                dragOffset = element.rectTransform.anchoredPosition - localPoint;
                return;
            }
        }

        // Bos alana tiklandi
        DeselectElement();
    }

    void SelectElement(HUDDraggableElement element)
    {
        // Onceki secimi kaldir
        if (selectedElement != null && selectedElement.highlightImage != null)
        {
            selectedElement.highlightImage.color = editModeHighlightColor;
        }

        selectedElement = element;

        // Yeni secimi vurgula
        if (selectedElement.highlightImage != null)
        {
            selectedElement.highlightImage.color = selectedElementColor;
        }
    }

    void DeselectElement()
    {
        if (selectedElement != null && selectedElement.highlightImage != null)
        {
            selectedElement.highlightImage.color = editModeHighlightColor;
        }

        selectedElement = null;
    }

    void DragSelectedElement()
    {
        if (selectedElement == null || selectedElement.rectTransform == null)
            return;

        Vector2 mousePos = Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            selectedElement.rectTransform.parent as RectTransform,
            mousePos,
            null,
            out Vector2 localPoint);

        selectedElement.rectTransform.anchoredPosition = localPoint + dragOffset;
    }

    void ResizeSelectedElement()
    {
        if (selectedElement == null || !selectedElement.allowResize)
            return;

        // Mouse hareketiyle boyut degistir
        float delta = Input.GetAxis("Mouse Y") * 2f;
        Vector2 newSize = selectedElement.rectTransform.sizeDelta;
        newSize *= (1f + delta * 0.01f);

        // Sinirla
        newSize.x = Mathf.Clamp(newSize.x, 50f, 500f);
        newSize.y = Mathf.Clamp(newSize.y, 30f, 300f);

        selectedElement.rectTransform.sizeDelta = newSize;
    }

    // === PUBLIC METODLAR ===

    /// <summary>
    /// Edit moduna gir
    /// </summary>
    public void EnterEditMode()
    {
        isEditMode = true;

        // Overlay'i goster
        if (editModeOverlay != null)
            editModeOverlay.SetActive(true);

        // Tum elementleri vurgula
        foreach (var element in draggableElements)
        {
            if (element == null) continue;

            ShowElementHighlight(element, true);
        }

        // Oyunu duraklat (opsiyonel)
        // Time.timeScale = 0f;

        Debug.Log("[HUDCustomizer] Edit mode aktif - F10 ile cik");
    }

    /// <summary>
    /// Edit modundan cik
    /// </summary>
    public void ExitEditMode()
    {
        isEditMode = false;

        // Overlay'i gizle
        if (editModeOverlay != null)
            editModeOverlay.SetActive(false);

        // Vurgulamalari kaldir
        foreach (var element in draggableElements)
        {
            if (element == null) continue;

            ShowElementHighlight(element, false);
        }

        DeselectElement();

        // Oyunu devam ettir
        // Time.timeScale = 1f;

        Debug.Log("[HUDCustomizer] Edit mode kapatildi");
    }

    /// <summary>
    /// Mevcut layout'u kaydet
    /// </summary>
    public void SaveLayout()
    {
        if (SaveManager.Instance == null) return;

        // Her elementin pozisyonunu kaydet
        // JSON olarak veya ayri ayri PlayerPrefs'e

        foreach (var element in draggableElements)
        {
            if (element == null || string.IsNullOrEmpty(element.elementId))
                continue;

            string key = $"HUD_{element.elementId}";
            Vector2 pos = element.rectTransform.anchoredPosition;
            Vector2 size = element.rectTransform.sizeDelta;

            PlayerPrefs.SetFloat($"{key}_PosX", pos.x);
            PlayerPrefs.SetFloat($"{key}_PosY", pos.y);
            PlayerPrefs.SetFloat($"{key}_SizeX", size.x);
            PlayerPrefs.SetFloat($"{key}_SizeY", size.y);
        }

        PlayerPrefs.Save();

        // Preset'i "custom" olarak ayarla
        SaveManager.Instance.SetHUDLayoutPreset("custom");
        SaveManager.Instance.SaveSettings();

        if (presetDropdown != null)
            presetDropdown.value = 3; // Custom

        Debug.Log("[HUDCustomizer] Layout kaydedildi");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    /// <summary>
    /// Varsayilan layout'a don
    /// </summary>
    public void ResetToDefault()
    {
        foreach (var element in draggableElements)
        {
            if (element == null) continue;

            element.rectTransform.anchoredPosition = element.defaultPosition;
            element.rectTransform.sizeDelta = element.defaultSize;
        }

        // Preset'i "normal" olarak ayarla
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetHUDLayoutPreset("normal");
        }

        if (presetDropdown != null)
            presetDropdown.value = 1; // Normal

        Debug.Log("[HUDCustomizer] Varsayilan layout yuklendi");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    /// <summary>
    /// Kaydedilmis layout'u yukle
    /// </summary>
    public void LoadLayout()
    {
        foreach (var element in draggableElements)
        {
            if (element == null || string.IsNullOrEmpty(element.elementId))
                continue;

            string key = $"HUD_{element.elementId}";

            if (PlayerPrefs.HasKey($"{key}_PosX"))
            {
                float posX = PlayerPrefs.GetFloat($"{key}_PosX");
                float posY = PlayerPrefs.GetFloat($"{key}_PosY");
                float sizeX = PlayerPrefs.GetFloat($"{key}_SizeX", element.defaultSize.x);
                float sizeY = PlayerPrefs.GetFloat($"{key}_SizeY", element.defaultSize.y);

                element.rectTransform.anchoredPosition = new Vector2(posX, posY);
                element.rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            }
        }
    }

    /// <summary>
    /// Preset degistiginde
    /// </summary>
    void OnPresetChanged(int index)
    {
        string preset = index switch
        {
            0 => "minimal",
            1 => "normal",
            2 => "full",
            _ => "custom"
        };

        ApplyPreset(preset);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetHUDLayoutPreset(preset);
        }
    }

    /// <summary>
    /// Preset uygula
    /// </summary>
    public void ApplyPreset(string presetName)
    {
        switch (presetName)
        {
            case "minimal":
                ApplyMinimalPreset();
                break;
            case "normal":
                ResetToDefault();
                break;
            case "full":
                ApplyFullPreset();
                break;
            case "custom":
                LoadLayout();
                break;
        }

        GameEvents.RaiseHUDSettingsChanged();
    }

    void ApplyMinimalPreset()
    {
        foreach (var element in draggableElements)
        {
            if (element == null) continue;

            // Minimal preset - sadece onemli elementler gorunur
            bool show = element.elementId == "health" ||
                       element.elementId == "ammo" ||
                       element.elementId == "score";

            if (element.canvasGroup != null)
            {
                element.canvasGroup.alpha = show ? 1f : 0f;
            }
            else if (element.rectTransform != null)
            {
                element.rectTransform.gameObject.SetActive(show);
            }

            // Boyutu kucult
            if (show)
            {
                element.rectTransform.localScale = Vector3.one * 0.8f;
            }
        }
    }

    void ApplyFullPreset()
    {
        foreach (var element in draggableElements)
        {
            if (element == null) continue;

            // Full preset - tum elementler gorunur ve buyuk
            if (element.canvasGroup != null)
            {
                element.canvasGroup.alpha = 1f;
            }
            else if (element.rectTransform != null)
            {
                element.rectTransform.gameObject.SetActive(true);
            }

            // Boyutu buyut
            element.rectTransform.localScale = Vector3.one * 1.1f;
        }
    }

    void ShowElementHighlight(HUDDraggableElement element, bool show)
    {
        if (element.highlightImage != null)
        {
            element.highlightImage.enabled = show;
            element.highlightImage.color = editModeHighlightColor;
        }
        else if (show)
        {
            // Highlight image yoksa olustur
            var highlight = element.rectTransform.gameObject.AddComponent<Image>();
            highlight.color = editModeHighlightColor;
            highlight.raycastTarget = false;
            element.highlightImage = highlight;
        }
    }

    /// <summary>
    /// Yeni bir draggable element kaydet
    /// </summary>
    public void RegisterElement(string elementId, RectTransform rectTransform, bool allowResize = true)
    {
        var element = new HUDDraggableElement
        {
            elementId = elementId,
            rectTransform = rectTransform,
            defaultPosition = rectTransform.anchoredPosition,
            defaultSize = rectTransform.sizeDelta,
            allowResize = allowResize
        };

        draggableElements.Add(element);
    }
}

/// <summary>
/// Suruklenebilir HUD elementi
/// </summary>
[System.Serializable]
public class HUDDraggableElement
{
    public string elementId;
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Image highlightImage;
    public Vector2 defaultPosition;
    public Vector2 defaultSize;
    public bool allowResize = true;
}
