using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Minimap sistemi.
/// RenderTexture kullanarak oyun dunyasini kucuk harita olarak gosterir.
/// </summary>
public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    [Header("Minimap Ayarlari")]
    public bool isEnabled = true;

    [Range(100f, 300f)]
    public float minimapSize = 150f;

    [Range(10f, 50f)]
    public float viewRadius = 20f;

    [Range(0.5f, 3f)]
    public float zoom = 1f;

    public bool rotateWithPlayer = false;

    [Header("Kamera Referanslari")]
    public Camera minimapCamera;
    public RenderTexture minimapRenderTexture;

    [Header("UI Referanslari")]
    public RawImage minimapDisplay;
    public RectTransform minimapFrame;
    public Image playerMarker;
    public RectTransform markersContainer;

    [Header("Marker Prefab'lari")]
    public GameObject enemyMarkerPrefab;
    public GameObject checkpointMarkerPrefab;
    public GameObject itemMarkerPrefab;

    [Header("Marker Renkleri")]
    public Color playerColor = new Color(0f, 1f, 0f);
    public Color enemyColor = new Color(1f, 0.2f, 0.2f);
    public Color checkpointColor = new Color(0f, 1f, 1f);
    public Color itemColor = new Color(1f, 0.84f, 0f);

    [Header("Player Referansi")]
    public Transform playerTransform;

    // Kayitli objeler
    private Dictionary<Transform, MinimapMarker> trackedObjects = new Dictionary<Transform, MinimapMarker>();
    private bool isExpanded = false;

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
        // Player'i bul
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        // Minimap kamerayi olustur
        if (minimapCamera == null)
        {
            CreateMinimapCamera();
        }

        // RenderTexture olustur
        if (minimapRenderTexture == null)
        {
            CreateRenderTexture();
        }

        // UI olustur
        if (minimapDisplay == null)
        {
            CreateMinimapUI();
        }

        // Ayarlari yukle
        LoadSettings();

        // Event'lere abone ol
        GameEvents.OnHUDSettingsChanged += OnHUDSettingsChanged;
    }

    void OnDestroy()
    {
        GameEvents.OnHUDSettingsChanged -= OnHUDSettingsChanged;

        // RenderTexture'i temizle
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
        }
    }

    void OnHUDSettingsChanged()
    {
        LoadSettings();
    }

    void LoadSettings()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            isEnabled = SaveManager.Instance.Data.hudMinimapEnabled;
            minimapSize = SaveManager.Instance.Data.hudMinimapSize;
        }

        UpdateMinimapSize();
        SetEnabled(isEnabled);
    }

    void CreateMinimapCamera()
    {
        GameObject camObj = new GameObject("MinimapCamera");
        minimapCamera = camObj.AddComponent<Camera>();

        // Kamera ayarlari
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = viewRadius / zoom;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
        minimapCamera.cullingMask = LayerMask.GetMask("Default", "Ground", "Enemy");
        minimapCamera.depth = -10;

        // Yukari bak
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // RenderTexture'a bagla
        if (minimapRenderTexture != null)
        {
            minimapCamera.targetTexture = minimapRenderTexture;
        }
    }

    void CreateRenderTexture()
    {
        int resolution = 256;
        minimapRenderTexture = new RenderTexture(resolution, resolution, 16);
        minimapRenderTexture.filterMode = FilterMode.Bilinear;
        minimapRenderTexture.Create();

        if (minimapCamera != null)
        {
            minimapCamera.targetTexture = minimapRenderTexture;
        }
    }

    void CreateMinimapUI()
    {
        // Canvas bul
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Minimap frame
        GameObject frameObj = new GameObject("MinimapFrame");
        frameObj.transform.SetParent(canvas.transform, false);
        minimapFrame = frameObj.AddComponent<RectTransform>();

        // Sag ust koseye yerlestirelim
        minimapFrame.anchorMin = new Vector2(1, 1);
        minimapFrame.anchorMax = new Vector2(1, 1);
        minimapFrame.pivot = new Vector2(1, 1);
        minimapFrame.anchoredPosition = new Vector2(-20, -80);
        minimapFrame.sizeDelta = new Vector2(minimapSize, minimapSize);

        // Arka plan
        var bgImage = frameObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // Border
        var outline = frameObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        // Mask icin child
        GameObject maskObj = new GameObject("MinimapMask");
        maskObj.transform.SetParent(frameObj.transform, false);
        var maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = new Vector2(4, 4);
        maskRect.offsetMax = new Vector2(-4, -4);

        var mask = maskObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var maskImage = maskObj.AddComponent<Image>();

        // Minimap display
        GameObject displayObj = new GameObject("MinimapDisplay");
        displayObj.transform.SetParent(maskObj.transform, false);
        var displayRect = displayObj.AddComponent<RectTransform>();
        displayRect.anchorMin = Vector2.zero;
        displayRect.anchorMax = Vector2.one;
        displayRect.offsetMin = Vector2.zero;
        displayRect.offsetMax = Vector2.zero;

        minimapDisplay = displayObj.AddComponent<RawImage>();
        minimapDisplay.texture = minimapRenderTexture;

        // Markers container
        GameObject markersObj = new GameObject("Markers");
        markersObj.transform.SetParent(maskObj.transform, false);
        markersContainer = markersObj.AddComponent<RectTransform>();
        markersContainer.anchorMin = Vector2.zero;
        markersContainer.anchorMax = Vector2.one;
        markersContainer.offsetMin = Vector2.zero;
        markersContainer.offsetMax = Vector2.zero;

        // Player marker (ortada)
        GameObject playerMarkerObj = new GameObject("PlayerMarker");
        playerMarkerObj.transform.SetParent(markersContainer, false);
        var playerRect = playerMarkerObj.AddComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.anchoredPosition = Vector2.zero;
        playerRect.sizeDelta = new Vector2(10, 10);

        playerMarker = playerMarkerObj.AddComponent<Image>();
        playerMarker.color = playerColor;

        // Player marker'i ucgen yap (basit)
        // Sprite olmadigi icin daire kalacak
    }

    void LateUpdate()
    {
        if (!isEnabled || playerTransform == null || minimapCamera == null)
            return;

        UpdateCameraPosition();
        UpdateMarkers();
        HandleInput();
    }

    void UpdateCameraPosition()
    {
        // Kamerayi player'in ustune konumla
        Vector3 newPos = playerTransform.position;
        newPos.y = playerTransform.position.y + 50f; // Yukari bak
        minimapCamera.transform.position = newPos;

        // Zoom ayarla
        minimapCamera.orthographicSize = viewRadius / zoom;

        // Player'la birlikte don (opsiyonel)
        if (rotateWithPlayer)
        {
            float playerAngle = playerTransform.eulerAngles.y;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, playerAngle, 0f);

            if (minimapDisplay != null)
            {
                minimapDisplay.rectTransform.localRotation = Quaternion.Euler(0, 0, playerAngle);
            }
        }
    }

    void UpdateMarkers()
    {
        // Tum kayitli objelerin marker'larini guncelle
        List<Transform> toRemove = new List<Transform>();

        foreach (var kvp in trackedObjects)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            UpdateMarkerPosition(kvp.Key, kvp.Value);
        }

        // Silinecekleri kaldir
        foreach (var t in toRemove)
        {
            if (trackedObjects[t].markerObject != null)
                Destroy(trackedObjects[t].markerObject);
            trackedObjects.Remove(t);
        }
    }

    void UpdateMarkerPosition(Transform target, MinimapMarker marker)
    {
        if (marker.markerObject == null || playerTransform == null)
            return;

        // Dunya pozisyonunu minimap pozisyonuna cevir
        Vector3 offset = target.position - playerTransform.position;
        float mapScale = minimapSize / (viewRadius * 2f);

        Vector2 markerPos = new Vector2(offset.x, offset.z) * mapScale * zoom;

        // Sinirlar icinde tut
        float maxDist = minimapSize * 0.45f;
        if (markerPos.magnitude > maxDist)
        {
            markerPos = markerPos.normalized * maxDist;
            marker.markerObject.SetActive(true); // Kenarda goster
        }

        marker.rectTransform.anchoredPosition = markerPos;

        // Mesafeye gore opacity
        float distance = offset.magnitude;
        float alpha = Mathf.Clamp01(1f - (distance / viewRadius) * 0.5f);
        if (marker.image != null)
        {
            Color c = marker.image.color;
            c.a = alpha;
            marker.image.color = c;
        }
    }

    void HandleInput()
    {
        // M tusu ile buyut/kucult
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleExpanded();
        }

        // Scroll ile zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scroll) > 0.01f)
        {
            zoom = Mathf.Clamp(zoom + scroll * 0.5f, 0.5f, 3f);
        }
    }

    // === PUBLIC METODLAR ===

    /// <summary>
    /// Minimap'i ac/kapat
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;

        if (minimapFrame != null)
            minimapFrame.gameObject.SetActive(enabled);

        if (minimapCamera != null)
            minimapCamera.enabled = enabled;
    }

    /// <summary>
    /// Minimap boyutunu guncelle
    /// </summary>
    public void UpdateMinimapSize()
    {
        if (minimapFrame != null)
        {
            minimapFrame.sizeDelta = new Vector2(minimapSize, minimapSize);
        }
    }

    /// <summary>
    /// Buyuk/kucuk gorunum arasinda gecis
    /// </summary>
    public void ToggleExpanded()
    {
        isExpanded = !isExpanded;

        if (minimapFrame != null)
        {
            if (isExpanded)
            {
                // Buyuk gorunum
                minimapFrame.anchorMin = new Vector2(0.2f, 0.2f);
                minimapFrame.anchorMax = new Vector2(0.8f, 0.8f);
                minimapFrame.anchoredPosition = Vector2.zero;
                minimapFrame.offsetMin = Vector2.zero;
                minimapFrame.offsetMax = Vector2.zero;
            }
            else
            {
                // Normal gorunum
                minimapFrame.anchorMin = new Vector2(1, 1);
                minimapFrame.anchorMax = new Vector2(1, 1);
                minimapFrame.pivot = new Vector2(1, 1);
                minimapFrame.anchoredPosition = new Vector2(-20, -80);
                minimapFrame.sizeDelta = new Vector2(minimapSize, minimapSize);
            }
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButton();
    }

    /// <summary>
    /// Bir objeyi minimap'e kaydet
    /// </summary>
    public void RegisterObject(Transform target, MinimapMarkerType type)
    {
        if (target == null || trackedObjects.ContainsKey(target))
            return;

        // Marker olustur
        GameObject markerObj = new GameObject($"Marker_{target.name}");
        markerObj.transform.SetParent(markersContainer, false);

        var rect = markerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(8, 8);

        var image = markerObj.AddComponent<Image>();

        // Tipe gore renk
        switch (type)
        {
            case MinimapMarkerType.Enemy:
                image.color = enemyColor;
                break;
            case MinimapMarkerType.Checkpoint:
                image.color = checkpointColor;
                rect.sizeDelta = new Vector2(12, 12);
                break;
            case MinimapMarkerType.Item:
                image.color = itemColor;
                rect.sizeDelta = new Vector2(6, 6);
                break;
            default:
                image.color = Color.white;
                break;
        }

        var marker = new MinimapMarker
        {
            markerObject = markerObj,
            rectTransform = rect,
            image = image,
            type = type
        };

        trackedObjects[target] = marker;
    }

    /// <summary>
    /// Bir objeyi minimap'ten kaldir
    /// </summary>
    public void UnregisterObject(Transform target)
    {
        if (target == null || !trackedObjects.ContainsKey(target))
            return;

        if (trackedObjects[target].markerObject != null)
            Destroy(trackedObjects[target].markerObject);

        trackedObjects.Remove(target);
    }

    /// <summary>
    /// Zoom seviyesini ayarla
    /// </summary>
    public void SetZoom(float newZoom)
    {
        zoom = Mathf.Clamp(newZoom, 0.5f, 3f);
    }
}

/// <summary>
/// Minimap marker turleri
/// </summary>
public enum MinimapMarkerType
{
    Enemy,
    Checkpoint,
    Item,
    Objective
}

/// <summary>
/// Minimap marker verisi
/// </summary>
public class MinimapMarker
{
    public GameObject markerObject;
    public RectTransform rectTransform;
    public Image image;
    public MinimapMarkerType type;
}
