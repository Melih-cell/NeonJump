using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Neon tarzı harita oluşturucu - Editor'da çalışır
/// </summary>
[ExecuteInEditMode]
public class NeonMapGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap platformTilemap;
    public Tilemap hazardTilemap;
    public Tilemap backgroundTilemap;
    public Tilemap decorationTilemap;

    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 30;
    public int groundLevel = 0;

    [Header("Generation Settings")]
    [Tooltip("Runtime'da otomatik oluşturma (Editor'da oluşturulduysa false yapın)")]
    public bool generateOnStart = false;
    public int seed = 0; // 0 = random

    [Header("Editor Buttons")]
    [Tooltip("Inspector'da butona bas veya sağ tık menüsünden Generate Map seç")]
    public bool editorGenerated = false;

    [Header("Tiles (Auto-generated if null)")]
    public TileBase groundTile;
    public TileBase platformTile;
    public TileBase hazardTile;
    public TileBase bgTile;

    // Prefabs
    [Header("Prefabs")]
    public GameObject movingPlatformPrefab;
    public GameObject crumblingPlatformPrefab;
    public GameObject enemyPrefab;
    public GameObject coinPrefab;

    void Start()
    {
        if (generateOnStart)
        {
            FindTilemaps();
            GenerateMap();
        }
    }

    [ContextMenu("Find Tilemaps")]
    public void FindTilemaps()
    {
        Grid grid = FindFirstObjectByType<Grid>();
        if (grid == null) return;

        foreach (Transform child in grid.transform)
        {
            Tilemap tm = child.GetComponent<Tilemap>();
            if (tm == null) continue;

            if (child.name.Contains("Ground") && groundTilemap == null)
                groundTilemap = tm;
            else if (child.name.Contains("Platform") && platformTilemap == null)
                platformTilemap = tm;
            else if (child.name.Contains("Hazard") && hazardTilemap == null)
                hazardTilemap = tm;
            else if (child.name.Contains("Background") && backgroundTilemap == null)
                backgroundTilemap = tm;
            else if (child.name.Contains("Decoration") && decorationTilemap == null)
                decorationTilemap = tm;
        }
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (seed == 0)
            seed = Random.Range(1, 99999);
        Random.InitState(seed);

        // Tile'ları oluştur
        CreateTiles();

        // Haritayı temizle
        ClearMap();

        // Ana zemin
        GenerateGround();

        // Platformlar
        GeneratePlatforms();

        // Tehlikeler
        GenerateHazards();

        // Arka plan
        GenerateBackground();

        // Özel objeler
        GenerateSpecialObjects();

        Debug.Log($"Harita oluşturuldu! Seed: {seed}");
    }

    void CreateTiles()
    {
        // Programatik tile oluşturma
        if (groundTile == null)
            groundTile = CreateColorTile(new Color(0.2f, 0.6f, 0.3f)); // Yeşil zemin

        if (platformTile == null)
            platformTile = CreateColorTile(new Color(0.3f, 0.5f, 0.8f)); // Mavi platform

        if (hazardTile == null)
            hazardTile = CreateColorTile(new Color(1f, 0.3f, 0.2f)); // Kırmızı tehlike

        if (bgTile == null)
            bgTile = CreateColorTile(new Color(0.1f, 0.1f, 0.15f, 0.5f)); // Koyu arka plan
    }

    TileBase CreateColorTile(Color color)
    {
        // Basit renkli tile oluştur
        Tile tile = ScriptableObject.CreateInstance<Tile>();

        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[256];

        for (int i = 0; i < 256; i++)
        {
            // Kenar efekti
            int x = i % 16;
            int y = i / 16;

            if (x == 0 || x == 15 || y == 0 || y == 15)
                colors[i] = color * 0.7f; // Koyu kenar
            else
                colors[i] = color;
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        tile.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        tile.color = Color.white;

        return tile;
    }

    void ClearMap()
    {
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (platformTilemap != null) platformTilemap.ClearAllTiles();
        if (hazardTilemap != null) hazardTilemap.ClearAllTiles();
        if (backgroundTilemap != null) backgroundTilemap.ClearAllTiles();
        if (decorationTilemap != null) decorationTilemap.ClearAllTiles();
    }

    void GenerateGround()
    {
        if (groundTilemap == null || groundTile == null) return;

        // Ana zemin - dalgalı arazi
        int currentHeight = groundLevel;

        for (int x = -10; x < mapWidth; x++)
        {
            // Perlin noise ile yükseklik değişimi
            float noise = Mathf.PerlinNoise(x * 0.1f, seed * 0.01f);
            int heightChange = Mathf.RoundToInt((noise - 0.5f) * 4);

            // Yükseklik sınırları
            currentHeight = Mathf.Clamp(currentHeight + (Random.value > 0.7f ? heightChange : 0), -5, 5);

            // Zemin tile'ları
            for (int y = currentHeight; y >= currentHeight - 5; y--)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
            }

            // Arada boşluklar (çukurlar)
            if (x > 10 && x < mapWidth - 10 && Random.value < 0.03f)
            {
                int gapWidth = Random.Range(3, 6);
                for (int gx = 0; gx < gapWidth && x + gx < mapWidth; gx++)
                {
                    for (int y = currentHeight; y >= currentHeight - 5; y--)
                    {
                        groundTilemap.SetTile(new Vector3Int(x + gx, y, 0), null);
                    }
                }
                x += gapWidth;
            }
        }
    }

    void GeneratePlatforms()
    {
        if (platformTilemap == null || platformTile == null) return;

        // Rastgele platformlar
        int platformCount = mapWidth / 8;

        for (int i = 0; i < platformCount; i++)
        {
            int x = Random.Range(5, mapWidth - 5);
            int y = Random.Range(groundLevel + 3, groundLevel + 12);
            int width = Random.Range(3, 8);

            // Platform tipi
            int platformType = Random.Range(0, 3);

            switch (platformType)
            {
                case 0: // Normal platform
                    CreatePlatform(x, y, width);
                    break;

                case 1: // Merdiven platformları
                    for (int step = 0; step < 3; step++)
                    {
                        CreatePlatform(x + step * 3, y + step * 2, 3);
                    }
                    break;

                case 2: // Zıplama parkuru
                    for (int jump = 0; jump < 4; jump++)
                    {
                        CreatePlatform(x + jump * 4, y + (jump % 2) * 2, 2);
                    }
                    break;
            }
        }

        // Yüksek platformlar (ödül alanları)
        for (int i = 0; i < 5; i++)
        {
            int x = Random.Range(15, mapWidth - 15);
            int y = Random.Range(groundLevel + 15, groundLevel + 20);
            CreatePlatform(x, y, Random.Range(4, 7));
        }
    }

    void CreatePlatform(int x, int y, int width)
    {
        for (int px = 0; px < width; px++)
        {
            platformTilemap.SetTile(new Vector3Int(x + px, y, 0), platformTile);
        }
    }

    void GenerateHazards()
    {
        if (hazardTilemap == null || hazardTile == null) return;

        // Zemin üstüne dikenler
        for (int x = 10; x < mapWidth - 10; x++)
        {
            if (Random.value < 0.05f)
            {
                // Diken grubu
                int spikeWidth = Random.Range(2, 5);
                int groundY = GetGroundHeight(x);

                for (int sx = 0; sx < spikeWidth; sx++)
                {
                    hazardTilemap.SetTile(new Vector3Int(x + sx, groundY + 1, 0), hazardTile);
                }
                x += spikeWidth + 2;
            }
        }

        // Çukurlarda lav
        for (int x = -10; x < mapWidth; x++)
        {
            int groundY = GetGroundHeight(x);
            if (groundY < groundLevel - 3)
            {
                // Lav
                for (int y = groundY; y >= groundY - 2; y--)
                {
                    hazardTilemap.SetTile(new Vector3Int(x, y, 0), hazardTile);
                }
            }
        }
    }

    int GetGroundHeight(int x)
    {
        if (groundTilemap == null) return groundLevel;

        for (int y = groundLevel + 10; y >= groundLevel - 10; y--)
        {
            if (groundTilemap.GetTile(new Vector3Int(x, y, 0)) != null)
                return y;
        }
        return groundLevel - 10;
    }

    void GenerateBackground()
    {
        if (backgroundTilemap == null || bgTile == null) return;

        // Arka plan deseni
        for (int x = -15; x < mapWidth + 5; x += 2)
        {
            for (int y = groundLevel - 10; y < groundLevel + 25; y += 2)
            {
                if (Random.value < 0.3f)
                {
                    backgroundTilemap.SetTile(new Vector3Int(x, y, 0), bgTile);
                }
            }
        }
    }

    void GenerateSpecialObjects()
    {
        // Hareketli platformlar
        CreateMovingPlatforms();

        // Kırılan platformlar
        CreateCrumblingPlatforms();

        // Düşmanlar
        CreateEnemies();

        // Coinler
        CreateCoins();
    }

    void CreateMovingPlatforms()
    {
        for (int i = 0; i < 5; i++)
        {
            int x = Random.Range(20, mapWidth - 20);
            int y = Random.Range(groundLevel + 5, groundLevel + 15);

            GameObject platform = new GameObject($"MovingPlatform_{i}");
            platform.transform.position = new Vector3(x, y, 0);

            // Sprite
            SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlatformSprite(4);
            sr.color = new Color(0.3f, 0.8f, 0.5f);
            sr.sortingOrder = 1;

            // Collider
            BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 0.5f);

            // Moving Platform script
            MovingPlatform mp = platform.AddComponent<MovingPlatform>();
            mp.moveType = (MovingPlatform.MoveType)Random.Range(0, 3);
            mp.moveDistance = Random.Range(3f, 6f);
            mp.moveSpeed = Random.Range(1.5f, 3f);
        }
    }

    void CreateCrumblingPlatforms()
    {
        for (int i = 0; i < 8; i++)
        {
            int x = Random.Range(15, mapWidth - 15);
            int y = Random.Range(groundLevel + 8, groundLevel + 18);

            GameObject platform = new GameObject($"CrumblingPlatform_{i}");
            platform.transform.position = new Vector3(x, y, 0);

            // Sprite
            SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlatformSprite(3);
            sr.color = new Color(0.8f, 0.6f, 0.3f);
            sr.sortingOrder = 1;

            // Collider
            BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            // Crumbling Platform script
            CrumblingPlatform cp = platform.AddComponent<CrumblingPlatform>();
            cp.shakeTime = 0.4f;
            cp.respawnTime = 4f;
        }
    }

    void CreateEnemies()
    {
        // Mevcut düşman prefab'ı varsa kullan
        GameObject enemyPrefabToUse = enemyPrefab;

        if (enemyPrefabToUse == null)
        {
            // Basit düşman oluştur
            for (int i = 0; i < 15; i++)
            {
                int x = Random.Range(15, mapWidth - 10);
                int groundY = GetGroundHeight(x);

                if (groundY > groundLevel - 5)
                {
                    Vector3 pos = new Vector3(x, groundY + 1.5f, 0);
                    CreateSimpleEnemy(pos, i);
                }
            }
        }
    }

    void CreateSimpleEnemy(Vector3 position, int index)
    {
        GameObject enemy = new GameObject($"Enemy_{index}");
        enemy.transform.position = position;
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Default");

        // Sprite
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[256];
        Color enemyColor = new Color(1f, 0.3f, 0.3f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
                if (dist < 7)
                    colors[y * 16 + x] = enemyColor;
                else
                    colors[y * 16 + x] = Color.clear;
            }
        }
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        sr.sortingOrder = 5;

        // Collider
        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        // Rigidbody
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;

        // Enemy components
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<EnemyPatrol>();
    }

    void CreateCoins()
    {
        for (int i = 0; i < 30; i++)
        {
            int x = Random.Range(5, mapWidth - 5);
            int y = Random.Range(groundLevel + 2, groundLevel + 18);

            // Zemin veya platform üstünde mi kontrol et
            bool validPosition = false;
            for (int checkY = y - 1; checkY >= y - 3; checkY--)
            {
                if (groundTilemap.GetTile(new Vector3Int(x, checkY, 0)) != null ||
                    platformTilemap.GetTile(new Vector3Int(x, checkY, 0)) != null)
                {
                    validPosition = true;
                    break;
                }
            }

            if (validPosition || Random.value < 0.3f)
            {
                CreateCoin(new Vector3(x + 0.5f, y + 0.5f, 0), i);
            }
        }
    }

    void CreateCoin(Vector3 position, int index)
    {
        GameObject coin = new GameObject($"Coin_{index}");
        coin.transform.position = position;

        // Sprite
        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(12, 12);
        Color[] colors = new Color[144];
        Color coinColor = new Color(1f, 0.85f, 0.2f);

        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(6, 6));
                if (dist < 5)
                    colors[y * 12 + x] = coinColor;
                else if (dist < 6)
                    colors[y * 12 + x] = coinColor * 0.7f;
                else
                    colors[y * 12 + x] = Color.clear;
            }
        }
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 16);
        sr.sortingOrder = 4;

        // Collider (trigger)
        CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Coin script
        coin.AddComponent<Coin>();
    }

    Sprite CreatePlatformSprite(int width)
    {
        int pixelWidth = width * 16;
        Texture2D tex = new Texture2D(pixelWidth, 8);
        Color[] colors = new Color[pixelWidth * 8];

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < pixelWidth; x++)
            {
                if (y == 0 || y == 7 || x == 0 || x == pixelWidth - 1)
                    colors[y * pixelWidth + x] = Color.white * 0.7f;
                else
                    colors[y * pixelWidth + x] = Color.white;
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, pixelWidth, 8), new Vector2(0.5f, 0.5f), 16);
    }

    [ContextMenu("Clear Map")]
    public void ClearMapAndObjects()
    {
        ClearMap();

        // Oluşturulan objeleri temizle
        foreach (var mp in FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
            DestroyImmediate(mp.gameObject);
        foreach (var cp in FindObjectsByType<CrumblingPlatform>(FindObjectsSortMode.None))
            DestroyImmediate(cp.gameObject);
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsSortMode.None))
            if (coin.gameObject.name.StartsWith("Coin_"))
                DestroyImmediate(coin.gameObject);
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            if (enemy.name.StartsWith("Enemy_"))
                DestroyImmediate(enemy);
    }
}
