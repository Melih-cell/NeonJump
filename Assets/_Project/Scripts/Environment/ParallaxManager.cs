using UnityEngine;

/// <summary>
/// Parallax arka plan yoneticisi - doga temasi
/// Kamerayi takip eder, tilemap'lerin arkasinda kalir
/// </summary>
public class ParallaxManager : MonoBehaviour
{
    public static ParallaxManager Instance;

    [Header("Parallax Ayarlari")]
    public bool autoSetup = true;
    public int numberOfLayers = 5;

    [Header("Gokyuzu Renkleri - Doga")]
    public Color skyColorTop = new Color(0.35f, 0.65f, 0.95f);      // Acik mavi
    public Color skyColorBottom = new Color(0.85f, 0.92f, 0.98f);    // Beyazimsi ufuk

    [Header("Bulut Ayarlari")]
    public int cloudCount = 12;
    public Color cloudColor = new Color(1f, 1f, 1f, 0.7f);

    [Header("Doga Sprite'lari - Inspector'dan atanabilir")]
    public Sprite[] farTreeSprites;
    public Sprite[] midTreeSprites;
    public Sprite[] bushSprites;
    public Sprite[] logSprites;
    public Sprite[] nearTreeSprites;
    public Sprite[] flowerSprites;
    public Sprite[] mushroomSprites;
    public Sprite[] rockSprites;

    [Header("Katman Tint Renkleri")]
    public Color farTint = new Color(0.25f, 0.35f, 0.50f, 0.6f);       // Koyu mavi-yesil, soluk
    public Color midFarTint = new Color(0.35f, 0.50f, 0.40f, 0.75f);   // Orta mesafe
    public Color midTint = new Color(0.50f, 0.65f, 0.45f, 0.85f);      // Orta yakin
    public Color nearTint = new Color(1f, 1f, 1f, 1f);                  // Tam renk
    public Color frontTint = new Color(1f, 1f, 1f, 1f);                 // On plan tam renk

    private Camera mainCamera;
    private Transform cameraTransform;
    private GameObject skyObject;
    private GameObject cloudsObject;
    private GameObject[] natureLayers;

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

        // Mobilde katman sayisini azalt
        if (Application.isMobilePlatform && numberOfLayers > 3)
        {
            numberOfLayers = 3;
            cloudCount = 6;
        }

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
            screenHeight = 14f;
            screenWidth = screenHeight * mainCamera.aspect;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Gokyuzu ve bulutlari kameraya kilitle
        if (skyObject != null)
        {
            Vector3 skyPos = cameraTransform.position;
            skyPos.z = 100f;
            skyObject.transform.position = skyPos;
        }
        if (cloudsObject != null)
        {
            Vector3 cloudPos = cameraTransform.position;
            cloudPos.z = 99f;
            cloudsObject.transform.position = cloudPos;
        }
    }

    public void CreateParallaxBackground()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        CalculateScreenSize();

        CreateSky();
        CreateClouds();

        natureLayers = new GameObject[numberOfLayers];
        for (int i = 0; i < numberOfLayers; i++)
        {
            float t = (float)i / Mathf.Max(1, numberOfLayers - 1);
            CreateNatureLayer(i, t);
        }
    }

    // === GOKYUZU ===
    void CreateSky()
    {
        skyObject = new GameObject("Sky");
        skyObject.transform.SetParent(transform);

        SpriteRenderer sr = skyObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -1000;

        // Mavi gokyuzu gradient
        int texH = 64;
        Texture2D tex = new Texture2D(1, texH);
        for (int y = 0; y < texH; y++)
        {
            float t = (float)y / (texH - 1);
            tex.SetPixel(0, y, Color.Lerp(skyColorBottom, skyColorTop, t));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, texH), new Vector2(0.5f, 0.5f), 1f);

        float scaleX = screenWidth * 4f;
        float scaleY = screenHeight * 4f;
        skyObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        Vector3 pos = cameraTransform.position;
        pos.z = 100f;
        skyObject.transform.position = pos;
    }

    // === BULUTLAR ===
    void CreateClouds()
    {
        cloudsObject = new GameObject("Clouds");
        cloudsObject.transform.SetParent(transform);

        int texSize = 128;
        Texture2D tex = new Texture2D(texSize, texSize);
        Color[] pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Random.InitState(12345);
        for (int c = 0; c < cloudCount; c++)
        {
            int cx = Random.Range(4, texSize - 4);
            int cy = Random.Range(texSize / 2, texSize - 6);
            int cloudW = Random.Range(6, 18);
            int cloudH = Random.Range(3, 7);
            float alpha = Random.Range(0.3f, 0.7f);

            // Basit elips seklinde bulut
            for (int bx = -cloudW / 2; bx <= cloudW / 2; bx++)
            {
                for (int by = -cloudH / 2; by <= cloudH / 2; by++)
                {
                    float dx = (float)bx / (cloudW / 2f);
                    float dy = (float)by / (cloudH / 2f);
                    float dist = dx * dx + dy * dy;
                    if (dist > 1f) continue;

                    int px = cx + bx;
                    int py = cy + by;
                    if (px < 0 || px >= texSize || py < 0 || py >= texSize) continue;

                    float edgeFade = 1f - Mathf.Sqrt(dist);
                    Color c2 = cloudColor;
                    c2.a = alpha * edgeFade;
                    pixels[py * texSize + px] = c2;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        SpriteRenderer sr = cloudsObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -999;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 1f);

        float scale = Mathf.Max(screenWidth, screenHeight) * 3f / texSize;
        cloudsObject.transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 pos = cameraTransform.position;
        pos.z = 99f;
        cloudsObject.transform.position = pos;
    }

    // === DOGA KATMANLARI ===
    void CreateNatureLayer(int index, float depthT)
    {
        GameObject layerObj = new GameObject($"NatureLayer_{index}");
        layerObj.transform.SetParent(transform);

        float parallaxEffect = Mathf.Lerp(0.02f, 0.5f, depthT);
        float zPos = Mathf.Lerp(95f, 15f, depthT);

        // Katman tint rengi - atmosferik perspektif
        Color layerTint = GetLayerTint(index);

        // Katman icin sprite'lari ve dagilimlari olustur
        int texWidth = Mathf.RoundToInt(screenWidth * 10f);
        int texHeight = Mathf.RoundToInt(screenHeight * 5f);
        texWidth = Mathf.Clamp(texWidth, 128, 512);
        texHeight = Mathf.Clamp(texHeight, 64, 256);

        SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -900 + index * 10;

        Texture2D tex = CreateNatureTexture(index, layerTint, depthT, texWidth, texHeight);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), texWidth / (screenWidth * 3f));

        Vector3 startPos = cameraTransform.position;
        startPos.y = cameraTransform.position.y - screenHeight * 0.4f;
        startPos.z = zPos;
        layerObj.transform.position = startPos;

        ParallaxBackground pb = layerObj.AddComponent<ParallaxBackground>();
        pb.parallaxEffect = parallaxEffect;
        pb.parallaxX = true;
        pb.parallaxY = true;

        natureLayers[index] = layerObj;
    }

    Color GetLayerTint(int index)
    {
        if (numberOfLayers <= 3)
        {
            // Mobil - 3 katman
            switch (index)
            {
                case 0: return farTint;
                case 1: return midTint;
                default: return nearTint;
            }
        }

        // 5 katman
        switch (index)
        {
            case 0: return farTint;
            case 1: return midFarTint;
            case 2: return midTint;
            case 3: return nearTint;
            case 4: return frontTint;
            default: return nearTint;
        }
    }

    Texture2D CreateNatureTexture(int seed, Color tintColor, float depthT, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Random.InitState(seed * 54321 + 777);

        // Katmana gore hangi doga elemanlari cizilecek
        int layerIndex = seed;
        if (numberOfLayers <= 3)
        {
            // Mobil modda katman eslestirmesi
            switch (layerIndex)
            {
                case 0: DrawFarTrees(pixels, width, height, tintColor, depthT); break;
                case 1: DrawMidTrees(pixels, width, height, tintColor, depthT);
                        DrawBushes(pixels, width, height, tintColor, depthT); break;
                case 2: DrawNearTrees(pixels, width, height, tintColor, depthT);
                        DrawFlowers(pixels, width, height, tintColor, depthT); break;
            }
        }
        else
        {
            // 5 katman modu
            switch (layerIndex)
            {
                case 0: DrawFarTrees(pixels, width, height, tintColor, depthT); break;
                case 1: DrawMidTrees(pixels, width, height, tintColor, depthT);
                        DrawRocks(pixels, width, height, tintColor, depthT); break;
                case 2: DrawBushes(pixels, width, height, tintColor, depthT);
                        DrawLogs(pixels, width, height, tintColor, depthT); break;
                case 3: DrawNearTrees(pixels, width, height, tintColor, depthT);
                        DrawFlowers(pixels, width, height, tintColor, depthT); break;
                case 4: DrawMushrooms(pixels, width, height, tintColor, depthT);
                        DrawFlowers(pixels, width, height, tintColor, depthT); break;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;

        return tex;
    }

    // === UZAK AGAC SILUETLERI ===
    void DrawFarTrees(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        int treeCount = Random.Range(8, 14);
        for (int t = 0; t < treeCount; t++)
        {
            int tx = Random.Range(0, width);
            int trunkH = Random.Range(height / 6, height / 3);
            int crownW = Random.Range(4, 10);
            int crownH = Random.Range(6, 14);

            Color trunkColor = new Color(0.25f, 0.18f, 0.12f) * tint;
            trunkColor.a = tint.a;
            Color leafColor = new Color(0.15f, 0.30f, 0.18f) * tint;
            leafColor.a = tint.a;

            // Govde
            int trunkW = Mathf.Max(1, crownW / 4);
            for (int y = 0; y < trunkH; y++)
            {
                for (int x = -trunkW / 2; x <= trunkW / 2; x++)
                {
                    int px = tx + x;
                    if (px >= 0 && px < width && y >= 0 && y < height)
                        pixels[y * width + px] = trunkColor;
                }
            }

            // Yapraklar - ucgen seklinde
            for (int y = 0; y < crownH; y++)
            {
                float widthRatio = 1f - (float)y / crownH;
                int rowW = Mathf.Max(1, (int)(crownW * widthRatio));
                for (int x = -rowW; x <= rowW; x++)
                {
                    int px = tx + x;
                    int py = trunkH + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        Color c = leafColor;
                        // Hafif renk varyasyonu
                        c.g += Random.Range(-0.02f, 0.02f);
                        pixels[py * width + px] = c;
                    }
                }
            }
        }
    }

    // === ORTA MESAFE AGACLAR ===
    void DrawMidTrees(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        int treeCount = Random.Range(5, 9);
        for (int t = 0; t < treeCount; t++)
        {
            int tx = Random.Range(0, width);
            int trunkH = Random.Range(height / 4, height / 2);
            int crownW = Random.Range(8, 16);
            int crownH = Random.Range(10, 20);

            Color trunkColor = new Color(0.35f, 0.22f, 0.12f) * tint;
            trunkColor.a = tint.a;
            Color leafColor = new Color(0.20f, 0.45f, 0.20f) * tint;
            leafColor.a = tint.a;

            // Govde
            int trunkW = Mathf.Max(1, crownW / 5);
            for (int y = 0; y < trunkH; y++)
            {
                for (int x = -trunkW / 2; x <= trunkW / 2; x++)
                {
                    int px = tx + x;
                    if (px >= 0 && px < width && y >= 0 && y < height)
                        pixels[y * width + px] = trunkColor;
                }
            }

            // Yapraklar - yuvarlak tac
            for (int y = -crownH / 2; y <= crownH / 2; y++)
            {
                for (int x = -crownW / 2; x <= crownW / 2; x++)
                {
                    float dx = (float)x / (crownW / 2f);
                    float dy = (float)y / (crownH / 2f);
                    if (dx * dx + dy * dy > 1f) continue;

                    int px = tx + x;
                    int py = trunkH + crownH / 2 + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        Color c = leafColor;
                        c.g += Random.Range(-0.03f, 0.05f);
                        pixels[py * width + px] = c;
                    }
                }
            }
        }
    }

    // === KAYALAR ===
    void DrawRocks(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        int rockCount = Random.Range(3, 7);
        for (int r = 0; r < rockCount; r++)
        {
            int rx = Random.Range(0, width);
            int rockW = Random.Range(5, 12);
            int rockH = Random.Range(4, 9);

            Color rockColor = new Color(0.45f, 0.42f, 0.38f) * tint;
            rockColor.a = tint.a;

            for (int y = 0; y < rockH; y++)
            {
                float widthRatio = 1f - ((float)y / rockH) * 0.4f;
                int rowW = (int)(rockW * widthRatio);
                for (int x = -rowW / 2; x <= rowW / 2; x++)
                {
                    int px = rx + x;
                    if (px >= 0 && px < width && y >= 0 && y < height)
                    {
                        Color c = rockColor;
                        c.r += Random.Range(-0.03f, 0.03f);
                        c.g += Random.Range(-0.03f, 0.03f);
                        c.b += Random.Range(-0.03f, 0.03f);
                        pixels[y * width + px] = c;
                    }
                }
            }
        }
    }

    // === CALILAR ===
    void DrawBushes(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        int bushCount = Random.Range(6, 12);
        for (int b = 0; b < bushCount; b++)
        {
            int bx = Random.Range(0, width);
            int bushW = Random.Range(6, 14);
            int bushH = Random.Range(4, 8);

            Color bushColor = new Color(0.18f, 0.42f, 0.15f) * tint;
            bushColor.a = tint.a;

            // Yari daire seklinde cali
            for (int y = 0; y < bushH; y++)
            {
                for (int x = -bushW / 2; x <= bushW / 2; x++)
                {
                    float dx = (float)x / (bushW / 2f);
                    float dy = (float)y / bushH;
                    if (dx * dx + (1f - dy) * (1f - dy) > 1.2f) continue;

                    int px = bx + x;
                    if (px >= 0 && px < width && y >= 0 && y < height)
                    {
                        Color c = bushColor;
                        c.g += Random.Range(-0.04f, 0.06f);
                        pixels[y * width + px] = c;
                    }
                }
            }
        }
    }

    // === KUTUKLER ===
    void DrawLogs(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        int logCount = Random.Range(2, 5);
        for (int l = 0; l < logCount; l++)
        {
            int lx = Random.Range(0, width);
            int logW = Random.Range(8, 18);
            int logH = Random.Range(3, 5);

            Color logColor = new Color(0.40f, 0.28f, 0.15f) * tint;
            logColor.a = tint.a;

            for (int y = 0; y < logH; y++)
            {
                for (int x = 0; x < logW; x++)
                {
                    int px = lx + x;
                    if (px >= 0 && px < width && y >= 0 && y < height)
                    {
                        Color c = logColor;
                        // Yatay cizgi deseni
                        if (y == logH / 2) c *= 0.85f;
                        c.a = tint.a;
                        pixels[y * width + px] = c;
                    }
                }
            }
        }
    }

    // === YAKIN AGACLAR ===
    void DrawNearTrees(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        int treeCount = Random.Range(4, 7);
        for (int t = 0; t < treeCount; t++)
        {
            int tx = Random.Range(0, width);
            int trunkH = Random.Range(height / 3, (int)(height * 0.6f));
            int crownW = Random.Range(12, 22);
            int crownH = Random.Range(14, 24);

            Color trunkColor = new Color(0.40f, 0.25f, 0.12f) * tint;
            trunkColor.a = tint.a;
            Color leafColor = new Color(0.22f, 0.55f, 0.18f) * tint;
            leafColor.a = tint.a;

            // Govde - daha kalin
            int trunkW = Mathf.Max(2, crownW / 4);
            for (int y = 0; y < trunkH; y++)
            {
                for (int x = -trunkW / 2; x <= trunkW / 2; x++)
                {
                    int px = tx + x;
                    if (px >= 0 && px < width && y >= 0 && y < height)
                    {
                        Color c = trunkColor;
                        // Kabuk deseni
                        if (Random.value < 0.1f) c *= 0.85f;
                        c.a = tint.a;
                        pixels[y * width + px] = c;
                    }
                }
            }

            // Buyuk yuvarlak tac
            for (int y = -crownH / 2; y <= crownH / 2; y++)
            {
                for (int x = -crownW / 2; x <= crownW / 2; x++)
                {
                    float dx = (float)x / (crownW / 2f);
                    float dy = (float)y / (crownH / 2f);
                    float dist = dx * dx + dy * dy;
                    if (dist > 1f) continue;

                    int px = tx + x;
                    int py = trunkH + crownH / 2 + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        Color c = leafColor;
                        // Yaprak detayi
                        c.g += Random.Range(-0.05f, 0.08f);
                        // Isik efekti - ust kisim daha acik
                        if (dy < 0) c *= 1.1f;
                        c.a = tint.a;
                        pixels[py * width + px] = c;
                    }
                }
            }
        }
    }

    // === CICEKLER ===
    void DrawFlowers(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        Color[] flowerColors = new Color[]
        {
            new Color(0.95f, 0.3f, 0.3f),   // Kirmizi
            new Color(0.95f, 0.8f, 0.2f),   // Sari
            new Color(0.9f, 0.4f, 0.7f),    // Pembe
            new Color(0.6f, 0.3f, 0.85f),   // Mor
            new Color(1f, 0.6f, 0.2f),      // Turuncu
        };

        int flowerCount = Random.Range(8, 16);
        for (int f = 0; f < flowerCount; f++)
        {
            int fx = Random.Range(0, width);
            int stemH = Random.Range(2, 5);
            Color flowerCol = flowerColors[Random.Range(0, flowerColors.Length)] * tint;
            flowerCol.a = tint.a;

            // Sap
            Color stemColor = new Color(0.2f, 0.5f, 0.15f) * tint;
            stemColor.a = tint.a;
            for (int y = 0; y < stemH; y++)
            {
                if (fx >= 0 && fx < width && y >= 0 && y < height)
                    pixels[y * width + fx] = stemColor;
            }

            // Cicek basi - kucuk daire
            int petalSize = Random.Range(1, 3);
            for (int py = -petalSize; py <= petalSize; py++)
            {
                for (int px = -petalSize; px <= petalSize; px++)
                {
                    if (Mathf.Abs(px) + Mathf.Abs(py) > petalSize + 1) continue;
                    int x = fx + px;
                    int y = stemH + py;
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        pixels[y * width + x] = flowerCol;
                }
            }
        }
    }

    // === MANTARLAR ===
    void DrawMushrooms(Color[] pixels, int width, int height, Color tint, float depthT)
    {
        Color[] capColors = new Color[]
        {
            new Color(0.85f, 0.2f, 0.15f),  // Kirmizi
            new Color(0.75f, 0.55f, 0.2f),  // Kahverengi
            new Color(0.9f, 0.85f, 0.7f),   // Bej
        };

        int mushCount = Random.Range(5, 10);
        for (int m = 0; m < mushCount; m++)
        {
            int mx = Random.Range(0, width);
            int stemH = Random.Range(2, 4);
            int capW = Random.Range(3, 6);
            int capH = Random.Range(2, 4);

            Color stemColor = new Color(0.85f, 0.80f, 0.70f) * tint;
            stemColor.a = tint.a;
            Color capColor = capColors[Random.Range(0, capColors.Length)] * tint;
            capColor.a = tint.a;

            // Sap
            for (int y = 0; y < stemH; y++)
            {
                int px = mx;
                if (px >= 0 && px < width && y >= 0 && y < height)
                    pixels[y * width + px] = stemColor;
                if (px + 1 < width)
                    pixels[y * width + px + 1] = stemColor;
            }

            // Sapka - yari daire
            for (int y = 0; y < capH; y++)
            {
                float widthRatio = 1f - (float)y / capH;
                int rowW = (int)(capW * widthRatio);
                for (int x = -rowW; x <= rowW; x++)
                {
                    int px = mx + x;
                    int py = stemH + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        Color c = capColor;
                        // Benekler
                        if (Random.value < 0.15f)
                            c = Color.Lerp(c, Color.white, 0.5f);
                        c.a = tint.a;
                        pixels[py * width + px] = c;
                    }
                }
            }
        }
    }
}
