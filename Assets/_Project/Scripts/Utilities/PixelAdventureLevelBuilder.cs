using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pixel Adventure tileset'lerini kullanarak level olusturur.
/// Asset kaynak: https://pixelfrog-assets.itch.io/pixel-adventure-1
/// </summary>
public class PixelAdventureLevelBuilder : MonoBehaviour
{
    [Header("Tileset Settings")]
    public string terrainPath = "PixelAdventure/Terrain/Terrain (16x16)";
    public string backgroundPath = "PixelAdventure/Background";
    public int pixelsPerUnit = 16;

    [Header("Level Settings")]
    public float levelLength = 250f;

    [Header("Visual Settings")]
    public Color groundTint = Color.white;
    public Color platformTint = new Color(0.9f, 0.95f, 1f);

    // Terrain sprite'lari (3x3 grid)
    private Sprite[] terrainSprites;
    private Sprite topLeft, top, topRight;
    private Sprite left, center, right;
    private Sprite bottomLeft, bottom, bottomRight;

    // Background sprite'lari
    private Sprite[] backgroundSprites;

    // Containers
    private Transform levelContainer;
    private Transform backgroundContainer;
    private Transform enemyContainer;
    private Transform itemContainer;

    // Fallback sprite
    private Sprite whiteSprite;

    void Awake()
    {
        CreateWhiteSprite();
    }

    void Start()
    {
        SetupContainers();
        LoadTerrainSprites();
        LoadBackgroundSprites();
        BuildLevel();
    }

    void CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[256];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    void SetupContainers()
    {
        levelContainer = new GameObject("LevelContainer").transform;
        backgroundContainer = new GameObject("BackgroundContainer").transform;
        enemyContainer = new GameObject("EnemyContainer").transform;
        itemContainer = new GameObject("ItemContainer").transform;
    }

    void LoadTerrainSprites()
    {
        terrainSprites = Resources.LoadAll<Sprite>(terrainPath);

        if (terrainSprites != null && terrainSprites.Length >= 9)
        {
            // Pixel Adventure Terrain atlas sirasi (soldan saga, yukardan asagi)
            // Satir 1: TopLeft, Top, TopRight
            // Satir 2: Left, Center, Right
            // Satir 3: BottomLeft, Bottom, BottomRight

            topLeft = terrainSprites[0];
            top = terrainSprites[1];
            topRight = terrainSprites[2];
            left = terrainSprites[3];
            center = terrainSprites[4];
            right = terrainSprites[5];
            bottomLeft = terrainSprites[6];
            bottom = terrainSprites[7];
            bottomRight = terrainSprites[8];

            Debug.Log($"Terrain sprites yuklendi: {terrainSprites.Length} sprite");
        }
        else
        {
            Debug.LogWarning("Terrain sprites bulunamadi. Varsayilan kullaniliyor.");
            Debug.Log($"Sprite'lari su klasore koyun: Assets/Resources/{terrainPath}");
        }
    }

    void LoadBackgroundSprites()
    {
        backgroundSprites = Resources.LoadAll<Sprite>(backgroundPath);

        if (backgroundSprites != null && backgroundSprites.Length > 0)
        {
            Debug.Log($"Background sprites yuklendi: {backgroundSprites.Length} sprite");
        }
    }

    void BuildLevel()
    {
        // Arka plan
        CreateBackground();

        // ===== BOLUM 1: BASLANGIC =====
        CreateSection1();

        // ===== BOLUM 2: ORMAN =====
        CreateSection2();

        // ===== BOLUM 3: MAGARALAR =====
        CreateSection3();

        // ===== BOLUM 4: BOSS =====
        CreateSection4();

        Debug.Log("Pixel Adventure Level olusturuldu!");
    }

    void CreateBackground()
    {
        if (backgroundSprites == null || backgroundSprites.Length == 0)
        {
            // Varsayilan gradient arka plan
            CreateDefaultBackground();
            return;
        }

        // Parallax katmanlari
        for (int layer = 0; layer < Mathf.Min(backgroundSprites.Length, 3); layer++)
        {
            float depth = layer + 1;
            CreateBackgroundLayer(backgroundSprites[layer], depth, 0.2f + layer * 0.15f);
        }
    }

    void CreateDefaultBackground()
    {
        // Gok
        GameObject sky = new GameObject("Sky");
        sky.transform.SetParent(backgroundContainer);
        sky.transform.position = new Vector3(levelLength / 2f, 10, 10);
        sky.transform.localScale = new Vector3(levelLength + 50, 40, 1);

        SpriteRenderer sr = sky.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.4f, 0.7f, 0.9f); // Acik mavi
        sr.sortingOrder = -100;
    }

    void CreateBackgroundLayer(Sprite sprite, float depth, float parallax)
    {
        float tileWidth = sprite.bounds.size.x;
        int tilesNeeded = Mathf.CeilToInt((levelLength + 100) / tileWidth) + 2;

        for (int i = -1; i < tilesNeeded; i++)
        {
            GameObject bg = new GameObject($"BG_Layer{depth}_{i}");
            bg.transform.SetParent(backgroundContainer);
            bg.transform.position = new Vector3(i * tileWidth, 5, depth * 2);

            SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -90 + (int)depth;
        }
    }

    // ===== SECTION BUILDERS =====

    void CreateSection1()
    {
        // Baslangic bolumu (x: 0-60)
        float startX = 0;

        // Ana zemin bloklari
        CreateTiledGround(0, -2, 20, 3);
        CreateTiledGround(25, -2, 15, 3);
        CreateTiledGround(45, -2, 15, 3);

        // Platformlar
        CreateTiledPlatform(8, 2, 5);
        CreateTiledPlatform(18, 4, 4);
        CreateTiledPlatform(30, 3, 5);
        CreateTiledPlatform(42, 5, 4);
        CreateTiledPlatform(52, 3, 4);

        // Dusmanlar
        CreateEnemy(15, 1, PixelAdventureEnemy.EnemyType.Slime);
        CreateEnemy(35, 1, PixelAdventureEnemy.EnemyType.Mushroom);
        CreateEnemy(50, 1, PixelAdventureEnemy.EnemyType.Slime);

        // Meyveler (coin yerine)
        CreateFruitRow(5, 2, 5);
        CreateFruitRow(20, 5, 4);
        CreateFruitRow(32, 4, 5);
        CreateFruitRow(48, 4, 4);

        // Checkpoint
        CreateCheckpoint(58, 1);
    }

    void CreateSection2()
    {
        // Orman bolumu (x: 60-130)

        // Zemin bloklari (bosluklar var)
        CreateTiledGround(60, -2, 20, 3);
        CreateTiledGround(85, -2, 15, 3);
        CreateTiledGround(105, -2, 25, 3);

        // Platformlar
        CreateTiledPlatform(65, 3, 4);
        CreateTiledPlatform(75, 5, 4);
        CreateTiledPlatform(82, 2, 3); // Bosluk uzerinde
        CreateTiledPlatform(90, 4, 5);
        CreateTiledPlatform(100, 6, 4);
        CreateTiledPlatform(110, 3, 5);
        CreateTiledPlatform(120, 5, 4);

        // Hareketli platform
        CreateMovingPlatform(80, 3, true, 4f); // Yatay
        CreateMovingPlatform(102, 4, false, 3f); // Dikey

        // Dusmanlar
        CreateEnemy(68, 1, PixelAdventureEnemy.EnemyType.Mushroom);
        CreateEnemy(88, 1, PixelAdventureEnemy.EnemyType.Slime);
        CreateEnemy(95, 5, PixelAdventureEnemy.EnemyType.Slime);
        CreateEnemy(115, 1, PixelAdventureEnemy.EnemyType.Mushroom);
        CreateFlyingEnemy(72, 5, PixelAdventureEnemy.EnemyType.Bat);
        CreateFlyingEnemy(108, 6, PixelAdventureEnemy.EnemyType.Ghost);

        // Meyveler
        CreateFruitRow(62, 4, 4);
        CreateFruitArc(77, 3, 5, 2f);
        CreateFruitRow(92, 5, 5);
        CreateFruitRow(112, 4, 6);
        CreateFruitArc(122, 6, 4, 1.5f);

        // Tuzaklar
        CreateSpike(78, 1);
        CreateSpike(79, 1);
        CreateSaw(95, 2);

        // Power-up
        CreatePowerUp(85, 6, PowerUpType.DoubleJump);

        // Checkpoint
        CreateCheckpoint(128, 1);
    }

    void CreateSection3()
    {
        // Magara bolumu (x: 130-200)

        // Zemin
        CreateTiledGround(130, -2, 20, 3);
        CreateTiledGround(155, -2, 15, 3);
        CreateTiledGround(175, -2, 25, 3);

        // Platformlar - daha zor
        CreateTiledPlatform(135, 3, 3);
        CreateTiledPlatform(142, 5, 3);
        CreateTiledPlatform(150, 7, 3);
        CreateTiledPlatform(153, 4, 3); // Bosluk uzerinde
        CreateTiledPlatform(162, 6, 4);
        CreateTiledPlatform(170, 3, 3); // Bosluk uzerinde
        CreateTiledPlatform(178, 5, 4);
        CreateTiledPlatform(185, 7, 3);
        CreateTiledPlatform(192, 4, 4);

        // Hareketli platformlar
        CreateMovingPlatform(148, 3, false, 4f);
        CreateMovingPlatform(167, 4, true, 5f);
        CreateMovingPlatform(190, 3, false, 3f);

        // Dusmanlar - daha zor
        CreateEnemy(138, 1, PixelAdventureEnemy.EnemyType.Mushroom);
        CreateEnemy(150, 8, PixelAdventureEnemy.EnemyType.Slime);
        CreateEnemy(165, 1, PixelAdventureEnemy.EnemyType.Mushroom);
        CreateEnemy(182, 1, PixelAdventureEnemy.EnemyType.Slime);
        CreateEnemy(185, 8, PixelAdventureEnemy.EnemyType.Mushroom);
        CreateFlyingEnemy(145, 6, PixelAdventureEnemy.EnemyType.Bat);
        CreateFlyingEnemy(172, 5, PixelAdventureEnemy.EnemyType.Skull);
        CreateFlyingEnemy(188, 6, PixelAdventureEnemy.EnemyType.Ghost);

        // Meyveler
        CreateFruitRow(132, 4, 4);
        CreateFruitArc(143, 6, 4, 2f);
        CreateFruitRow(158, 7, 5);
        CreateFruitArc(175, 4, 5, 2.5f);
        CreateFruitRow(193, 5, 4);

        // Tuzaklar
        CreateSpike(152, 1);
        CreateSpike(153, 1);
        CreateSpike(154, 1);
        CreateSaw(163, 2);
        CreateSaw(180, 2);

        // Power-up'lar
        CreatePowerUp(155, 8, PowerUpType.Shield);
        CreatePowerUp(195, 6, PowerUpType.SpeedBoost);

        // Checkpoint
        CreateCheckpoint(198, 1);
    }

    void CreateSection4()
    {
        // Boss arenasi (x: 200-250)

        // Arena zemini
        CreateTiledGround(200, -2, 50, 3);

        // Arena duvarlari
        CreateWall(200, 1, 12);
        CreateWall(250, 1, 12);

        // Arena platformlari
        CreateTiledPlatform(210, 4, 5);
        CreateTiledPlatform(235, 4, 5);
        CreateTiledPlatform(220, 7, 8);

        // Meyveler
        CreateFruitRow(212, 5, 4);
        CreateFruitRow(237, 5, 4);
        CreateFruitArc(223, 8, 6, 1.5f);

        // Boss trigger
        CreateBossTrigger(205);

        // Bitis noktasi
        CreateGoal(255, 1);
    }

    // ===== TERRAIN HELPERS =====

    void CreateTiledGround(float x, float y, int width, int height)
    {
        for (int tx = 0; tx < width; tx++)
        {
            for (int ty = 0; ty < height; ty++)
            {
                Sprite tileSprite = GetTerrainTile(tx, ty, width, height);
                CreateTile(x + tx, y + ty, tileSprite, groundTint, 0);
            }
        }

        // Collider
        GameObject groundCollider = new GameObject($"GroundCollider_{x}");
        groundCollider.transform.SetParent(levelContainer);
        groundCollider.transform.position = new Vector3(x + width / 2f, y + height / 2f, 0);

        BoxCollider2D col = groundCollider.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
    }

    void CreateTiledPlatform(float x, float y, int width)
    {
        for (int tx = 0; tx < width; tx++)
        {
            Sprite tileSprite;
            if (tx == 0)
                tileSprite = topLeft ?? whiteSprite;
            else if (tx == width - 1)
                tileSprite = topRight ?? whiteSprite;
            else
                tileSprite = top ?? whiteSprite;

            CreateTile(x + tx, y, tileSprite, platformTint, 1);
        }

        // Collider
        GameObject platformCollider = new GameObject($"PlatformCollider_{x}");
        platformCollider.transform.SetParent(levelContainer);
        platformCollider.transform.position = new Vector3(x + width / 2f, y + 0.5f, 0);

        BoxCollider2D col = platformCollider.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 1);
    }

    Sprite GetTerrainTile(int tx, int ty, int width, int height)
    {
        bool isLeft = tx == 0;
        bool isRight = tx == width - 1;
        bool isTop = ty == height - 1;
        bool isBottom = ty == 0;

        if (isTop && isLeft) return topLeft ?? whiteSprite;
        if (isTop && isRight) return topRight ?? whiteSprite;
        if (isTop) return top ?? whiteSprite;
        if (isBottom && isLeft) return bottomLeft ?? whiteSprite;
        if (isBottom && isRight) return bottomRight ?? whiteSprite;
        if (isBottom) return bottom ?? whiteSprite;
        if (isLeft) return left ?? whiteSprite;
        if (isRight) return right ?? whiteSprite;
        return center ?? whiteSprite;
    }

    void CreateTile(float x, float y, Sprite sprite, Color tint, int sortOrder)
    {
        GameObject tile = new GameObject($"Tile_{x}_{y}");
        tile.transform.SetParent(levelContainer);
        tile.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = sortOrder;
    }

    void CreateWall(float x, float y, int height)
    {
        for (int ty = 0; ty < height; ty++)
        {
            Sprite tileSprite;
            if (ty == height - 1)
                tileSprite = top ?? whiteSprite;
            else if (ty == 0)
                tileSprite = bottom ?? whiteSprite;
            else
                tileSprite = center ?? whiteSprite;

            CreateTile(x, y + ty, tileSprite, groundTint, 1);
        }

        // Collider
        GameObject wallCollider = new GameObject($"WallCollider_{x}");
        wallCollider.transform.SetParent(levelContainer);
        wallCollider.transform.position = new Vector3(x + 0.5f, y + height / 2f, 0);

        BoxCollider2D col = wallCollider.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1, height);
    }

    // ===== ENTITY HELPERS =====

    void CreateEnemy(float x, float y, PixelAdventureEnemy.EnemyType type)
    {
        GameObject enemy = new GameObject($"Enemy_{type}_{x}");
        enemy.transform.SetParent(enemyContainer);
        enemy.transform.position = new Vector3(x, y + 0.5f, 0);

        PixelAdventureEnemy pae = enemy.AddComponent<PixelAdventureEnemy>();
        pae.enemyType = type;
    }

    void CreateFlyingEnemy(float x, float y, PixelAdventureEnemy.EnemyType type)
    {
        CreateEnemy(x, y, type);
    }

    void CreateFruitRow(float x, float y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateFruit(x + i * 1.2f, y);
        }
    }

    void CreateFruitArc(float centerX, float y, int count, float height)
    {
        float spacing = 1.2f;
        float startX = centerX - (count - 1) * spacing / 2f;

        for (int i = 0; i < count; i++)
        {
            float t = (count > 1) ? (float)i / (count - 1) : 0.5f;
            float arcY = y + Mathf.Sin(t * Mathf.PI) * height;
            CreateFruit(startX + i * spacing, arcY);
        }
    }

    void CreateFruit(float x, float y)
    {
        GameObject fruit = new GameObject("Fruit");
        fruit.transform.SetParent(itemContainer);
        fruit.transform.position = new Vector3(x, y, 0);
        fruit.transform.localScale = new Vector3(0.8f, 0.8f, 1);

        SpriteRenderer sr = fruit.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Fruit sprite'ini yukle veya varsayilan
        Sprite[] fruitSprites = Resources.LoadAll<Sprite>("PixelAdventure/Items/Fruits/Apple");
        if (fruitSprites != null && fruitSprites.Length > 0)
        {
            sr.sprite = fruitSprites[0];
        }
        else
        {
            sr.sprite = whiteSprite;
            sr.color = Color.red; // Elma rengi
        }

        CircleCollider2D col = fruit.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        fruit.AddComponent<Coin>(); // Coin script'ini kullan
    }

    void CreateSpike(float x, float y)
    {
        GameObject spike = new GameObject("Spike");
        spike.transform.SetParent(levelContainer);
        spike.transform.position = new Vector3(x + 0.5f, y + 0.25f, 0);
        spike.tag = "Enemy";

        SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        Sprite[] spikeSprites = Resources.LoadAll<Sprite>("PixelAdventure/Traps/Spikes/Idle");
        if (spikeSprites != null && spikeSprites.Length > 0)
        {
            sr.sprite = spikeSprites[0];
        }
        else
        {
            sr.sprite = whiteSprite;
            sr.color = new Color(0.5f, 0.5f, 0.5f);
            spike.transform.localScale = new Vector3(1, 0.5f, 1);
        }

        BoxCollider2D col = spike.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.4f);
    }

    void CreateSaw(float x, float y)
    {
        GameObject saw = new GameObject("Saw");
        saw.transform.SetParent(levelContainer);
        saw.transform.position = new Vector3(x, y, 0);
        saw.tag = "Enemy";

        SpriteRenderer sr = saw.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

        Sprite[] sawSprites = Resources.LoadAll<Sprite>("PixelAdventure/Traps/Saw/On");
        if (sawSprites != null && sawSprites.Length > 0)
        {
            sr.sprite = sawSprites[0];
            // TODO: Animasyon ekle
        }
        else
        {
            sr.sprite = whiteSprite;
            sr.color = new Color(0.6f, 0.6f, 0.6f);
        }

        CircleCollider2D col = saw.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        // Donme animasyonu
        saw.AddComponent<SawRotation>();
    }

    void CreateMovingPlatform(float x, float y, bool horizontal, float distance)
    {
        GameObject platform = new GameObject("MovingPlatform");
        platform.transform.SetParent(levelContainer);
        platform.transform.position = new Vector3(x, y, 0);

        // 3 tile genisliginde platform
        for (int i = 0; i < 3; i++)
        {
            GameObject tile = new GameObject($"PlatformTile_{i}");
            tile.transform.SetParent(platform.transform);
            tile.transform.localPosition = new Vector3(i - 1, 0, 0);

            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = (i == 0) ? (topLeft ?? whiteSprite) :
                       (i == 2) ? (topRight ?? whiteSprite) :
                       (top ?? whiteSprite);
            sr.color = new Color(0.7f, 0.85f, 1f);
            sr.sortingOrder = 2;
        }

        Rigidbody2D rb = platform.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3, 1);

        MovingPlatform mp = platform.AddComponent<MovingPlatform>();
        mp.moveType = horizontal ? MovingPlatform.MoveType.Horizontal : MovingPlatform.MoveType.Vertical;
        mp.moveDistance = distance;
        mp.moveSpeed = 2f;
    }

    void CreatePowerUp(float x, float y, PowerUpType type)
    {
        GameObject powerUp = new GameObject($"PowerUp_{type}");
        powerUp.transform.SetParent(itemContainer);
        powerUp.transform.position = new Vector3(x, y, 0);

        PowerUp pu = powerUp.AddComponent<PowerUp>();
        pu.powerUpType = type;
    }

    void CreateCheckpoint(float x, float y)
    {
        GameObject checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.SetParent(itemContainer);
        checkpoint.transform.position = new Vector3(x, y + 1, 0);

        checkpoint.AddComponent<Checkpoint>();
    }

    void CreateBossTrigger(float x)
    {
        GameObject trigger = new GameObject("BossTrigger");
        trigger.transform.SetParent(levelContainer);
        trigger.transform.position = new Vector3(x, 3, 0);

        BoxCollider2D col = trigger.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3, 6);
        col.isTrigger = true;

        BossTrigger bt = trigger.AddComponent<BossTrigger>();
        bt.bossSpawnPosition = new Vector3(x + 20, 8, 0);
        bt.arenaMinX = 200;
        bt.arenaMaxX = 250;
    }

    void CreateGoal(float x, float y)
    {
        GameObject goal = new GameObject("Goal");
        goal.transform.SetParent(itemContainer);
        goal.transform.position = new Vector3(x, y + 2, 0);

        SpriteRenderer sr = goal.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.2f, 1f, 0.3f);
        sr.sortingOrder = 5;
        goal.transform.localScale = new Vector3(1, 4, 1);

        BoxCollider2D col = goal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        goal.AddComponent<Goal>();
    }
}

// Saw donme scripti
public class SawRotation : MonoBehaviour
{
    public float rotationSpeed = 360f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
