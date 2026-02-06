using UnityEngine;

/// <summary>
/// Parallax arka plan yoneticisi - tam ekran kaplayan neon cyberpunk sehir
/// Kamerayi takip eder ve her zaman ekrani kaplar
/// </summary>
public class ParallaxManager : MonoBehaviour
{
    public static ParallaxManager Instance;

    [Header("Parallax Ayarlari")]
    public bool autoSetup = true;
    public int numberOfLayers = 4;

    [Header("Renkler - Neon Cyberpunk")]
    public Color skyColorTop = new Color(0.02f, 0.02f, 0.08f);
    public Color skyColorBottom = new Color(0.08f, 0.03f, 0.15f);
    public Color buildingColorFar = new Color(0.05f, 0.05f, 0.12f);
    public Color buildingColorNear = new Color(0.1f, 0.08f, 0.18f);
    public Color neonColor1 = new Color(0f, 1f, 1f);     // Cyan
    public Color neonColor2 = new Color(1f, 0f, 1f);     // Magenta
    public Color neonColor3 = new Color(1f, 0.5f, 0f);   // Orange
    public Color neonColor4 = new Color(0f, 1f, 0.5f);   // Green

    private Camera mainCamera;
    private Transform cameraTransform;
    private GameObject skyObject;
    private GameObject[] buildingLayers;

    // Kamera gorunum alani
    private float screenHeight;
    private float screenWidth;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[ParallaxManager] Main Camera bulunamadi!");
            return;
        }
        cameraTransform = mainCamera.transform;

        // Ekran boyutlarini hesapla
        CalculateScreenSize();

        if (autoSetup)
        {
            CreateParallaxBackground();
        }
    }

    void CalculateScreenSize()
    {
        if (mainCamera.orthographic)
        {
            screenHeight = mainCamera.orthographicSize * 2f;
            screenWidth = screenHeight * mainCamera.aspect;
        }
        else
        {
            // Perspective kamera icin yaklaşık değer
            screenHeight = 20f;
            screenWidth = screenHeight * mainCamera.aspect;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Gokyuzunu kameraya kilitle (her zaman arkada)
        if (skyObject != null)
        {
            Vector3 skyPos = cameraTransform.position;
            skyPos.z = 100f;
            skyObject.transform.position = skyPos;
        }
    }

    public void CreateParallaxBackground()
    {
        // Mevcut katmanlari temizle
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        CalculateScreenSize();

        // Gokyuzu - kameraya kilitli, hareket etmez
        CreateSky();

        // Bina katmanlari
        buildingLayers = new GameObject[numberOfLayers];
        for (int i = 0; i < numberOfLayers; i++)
        {
            float t = (float)i / Mathf.Max(1, numberOfLayers - 1);
            CreateBuildingLayer(i, t);
        }

        Debug.Log($"[ParallaxManager] Parallax arka plan olusturuldu - {numberOfLayers} katman");
    }

    void CreateSky()
    {
        skyObject = new GameObject("Sky");
        skyObject.transform.SetParent(transform);

        SpriteRenderer sr = skyObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -1000;

        // Gradient texture
        int texHeight = 128;
        Texture2D tex = new Texture2D(1, texHeight);
        for (int y = 0; y < texHeight; y++)
        {
            float t = (float)y / (texHeight - 1);
            tex.SetPixel(0, y, Color.Lerp(skyColorBottom, skyColorTop, t));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, texHeight), new Vector2(0.5f, 0.5f), 1f);

        // Ekrani kaplayacak kadar buyuk
        float scaleX = screenWidth * 3f;
        float scaleY = screenHeight * 3f;
        skyObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Kameranin arkasina yerleştir
        Vector3 pos = cameraTransform.position;
        pos.z = 100f;
        skyObject.transform.position = pos;
    }

    void CreateBuildingLayer(int index, float depthT)
    {
        GameObject layerObj = new GameObject($"BuildingLayer_{index}");
        layerObj.transform.SetParent(transform);

        // Derinlige gore parallax hizi (0 = sabit, 1 = kamerayla ayni)
        float parallaxEffect = Mathf.Lerp(0.05f, 0.6f, depthT);
        float zPos = Mathf.Lerp(90f, 20f, depthT);

        // Bina rengi
        Color buildingColor = Color.Lerp(buildingColorFar, buildingColorNear, depthT);

        // Katman genisligi (ekranin 3 kati - sonsuz scroll icin)
        float layerWidth = screenWidth * 4f;
        float layerHeight = screenHeight * 1.5f;

        // Sprite olustur
        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -900 + index * 10;

        Texture2D tex = CreateBuildingSilhouette(index, buildingColor, depthT, (int)layerWidth * 4, (int)layerHeight * 4);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 4f);

        // Pozisyon
        Vector3 startPos = cameraTransform.position;
        startPos.y = cameraTransform.position.y - screenHeight * 0.3f;
        startPos.z = zPos;
        layerObj.transform.position = startPos;

        // Parallax component
        ParallaxBackground pb = layerObj.AddComponent<ParallaxBackground>();
        pb.parallaxEffect = parallaxEffect;
        pb.parallaxX = true;
        pb.parallaxY = true;

        buildingLayers[index] = layerObj;

        // Neon isiklari (yakin katmanlar icin)
        if (depthT > 0.4f)
        {
            AddNeonLightsToLayer(layerObj.transform, index, depthT);
        }
    }

    Texture2D CreateBuildingSilhouette(int seed, Color baseColor, float depthT, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        // Transparan baslat
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        Random.InitState(seed * 54321);

        // Binalar
        int x = 0;
        while (x < width)
        {
            int buildingWidth = Random.Range(width / 30, width / 12);
            int buildingHeight = Random.Range(height / 4, height);
            int gap = Random.Range(2, width / 40);

            Color bColor = baseColor * Random.Range(0.7f, 1.1f);
            bColor.a = 1f;

            // Bina gövdesi
            for (int bx = 0; bx < buildingWidth && x + bx < width; bx++)
            {
                for (int by = 0; by < buildingHeight && by < height; by++)
                {
                    pixels[by * width + (x + bx)] = bColor;
                }
            }

            // Pencereler (yakin katmanlar icin)
            if (depthT > 0.3f)
            {
                int windowSize = Mathf.Max(2, (int)(6 * depthT));
                int windowSpacingX = windowSize + Random.Range(3, 6);
                int windowSpacingY = windowSize + Random.Range(4, 8);

                for (int wx = windowSize; wx < buildingWidth - windowSize; wx += windowSpacingX)
                {
                    for (int wy = windowSize + 5; wy < buildingHeight - 15; wy += windowSpacingY)
                    {
                        Color windowColor;
                        if (Random.value > 0.35f)
                        {
                            // Isikli pencere
                            windowColor = GetRandomNeonColor() * Random.Range(0.4f, 0.8f);
                        }
                        else
                        {
                            // Karanlik pencere
                            windowColor = new Color(0.01f, 0.01f, 0.02f);
                        }
                        windowColor.a = 1f;

                        for (int pwx = 0; pwx < windowSize; pwx++)
                        {
                            for (int pwy = 0; pwy < windowSize; pwy++)
                            {
                                int px = x + wx + pwx;
                                int py = wy + pwy;
                                if (px >= 0 && px < width && py >= 0 && py < height)
                                {
                                    pixels[py * width + px] = windowColor;
                                }
                            }
                        }
                    }
                }
            }

            x += buildingWidth + gap;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        return tex;
    }

    void AddNeonLightsToLayer(Transform parent, int layerIndex, float depthT)
    {
        int lightCount = Mathf.RoundToInt(depthT * 12);

        for (int i = 0; i < lightCount; i++)
        {
            GameObject lightObj = new GameObject($"Neon_{i}");
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = new Vector3(
                Random.Range(-screenWidth * 1.5f, screenWidth * 1.5f),
                Random.Range(screenHeight * 0.1f, screenHeight * 0.8f),
                -0.1f
            );

            SpriteRenderer sr = lightObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Background";
            sr.sortingOrder = -800 + layerIndex * 10;

            // Neon cizgi
            Color neonCol = GetRandomNeonColor();
            Texture2D tex = CreateNeonTexture(neonCol);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 8f);
            sr.color = neonCol;

            float scaleX = Random.Range(0.5f, 3f);
            float scaleY = Random.Range(0.1f, 0.3f);
            lightObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            lightObj.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));

            // Titreme efekti
            NeonFlicker flicker = lightObj.AddComponent<NeonFlicker>();
            flicker.baseColor = neonCol;
        }
    }

    Texture2D CreateNeonTexture(Color color)
    {
        int width = 32;
        int height = 8;
        Texture2D tex = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float edgeFadeX = 1f - Mathf.Abs(x - width / 2f) / (width / 2f);
                float edgeFadeY = 1f - Mathf.Abs(y - height / 2f) / (height / 2f);
                float fade = edgeFadeX * edgeFadeY;

                Color c = color * fade;
                c.a = fade;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    Color GetRandomNeonColor()
    {
        switch (Random.Range(0, 4))
        {
            case 0: return neonColor1;
            case 1: return neonColor2;
            case 2: return neonColor3;
            default: return neonColor4;
        }
    }
}

/// <summary>
/// Neon isik titremesi
/// </summary>
public class NeonFlicker : MonoBehaviour
{
    public Color baseColor = Color.cyan;
    public float flickerSpeed = 4f;
    public float minBrightness = 0.6f;

    private SpriteRenderer sr;
    private float offset;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        offset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (sr == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + offset, offset * 0.5f);
        float brightness = Mathf.Lerp(minBrightness, 1f, noise);

        // Ara sira hizli yanip sonme
        if (Random.value < 0.002f)
        {
            brightness = Random.Range(0.2f, 0.4f);
        }

        sr.color = baseColor * brightness;
    }
}
