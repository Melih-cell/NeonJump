using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapLevelBuilder : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap platformTilemap;
    public Tilemap decorationTilemap;

    [Header("Tiles")]
    public TileBase groundTile;
    public TileBase grassTile;
    public TileBase platformTile;
    public TileBase platformLeftTile;
    public TileBase platformRightTile;
    public TileBase platformMiddleTile;

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject coinPrefab;
    public GameObject goalPrefab;

    [Header("Level Settings")]
    public int levelWidth = 200;
    public int groundHeight = 3;

    // Runtime sprite
    private Sprite whiteSprite;

    void Awake()
    {
        Debug.Log("TilemapLevelBuilder Awake - Sprite olusturuluyor");
        CreateWhiteSprite();
    }

    void CreateWhiteSprite()
    {
        // 16x16 beyaz texture olustur
        Texture2D texture = new Texture2D(16, 16);
        Color[] colors = new Color[16 * 16];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        // Sprite olustur
        whiteSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    [Header("Runtime Build")]
    public bool buildOnStart = false;  // Inspector'dan kontrol edin

    void Start()
    {
        if (!buildOnStart)
        {
            Debug.Log("TilemapLevelBuilder: Manuel tasarim modu - Runtime build devre disi");
            return;
        }

        Debug.Log("TilemapLevelBuilder Start - Level olusturuluyor");
        // Sprite kesinlikle hazir olsun
        EnsureWhiteSprite();
        BuildLevel();
    }

    public void BuildLevel()
    {
        if (groundTilemap == null) { BuildLevelWithGameObjects(); return; }

        // Ana zemin bloklari
        BuildGroundSection(0, 20);
        BuildGroundSection(25, 45);
        BuildGroundSection(50, 75);
        BuildGroundSection(80, 110);
        BuildGroundSection(115, 145);
        BuildGroundSection(150, 180);
        BuildGroundSection(185, 220);

        // Platformlar
        BuildPlatform(10, 3, 5);
        BuildPlatform(18, 5, 4);
        BuildPlatform(28, 4, 6);
        BuildPlatform(38, 6, 4);
        BuildPlatform(48, 3, 5);

        BuildPlatform(55, 5, 4);
        BuildPlatform(63, 7, 3);
        BuildPlatform(70, 4, 5);

        BuildPlatform(85, 5, 6);
        BuildPlatform(95, 7, 4);
        BuildPlatform(103, 4, 5);

        BuildPlatform(120, 5, 4);
        BuildPlatform(128, 7, 5);
        BuildPlatform(138, 4, 4);

        BuildPlatform(155, 6, 5);
        BuildPlatform(165, 8, 4);
        BuildPlatform(175, 5, 5);

        BuildPlatform(190, 6, 4);
        BuildPlatform(200, 8, 5);
        BuildPlatform(210, 5, 4);

        // Kirmizi dusmanlar kaldirildi

        // Ucan dusmanlar
        SpawnFlyingEnemy(40, 6, FlyingEnemy.FlyPattern.Horizontal);
        SpawnFlyingEnemy(75, 5, FlyingEnemy.FlyPattern.Vertical);
        SpawnFlyingEnemy(110, 7, FlyingEnemy.FlyPattern.Circular);
        SpawnFlyingEnemy(150, 5, FlyingEnemy.FlyPattern.Chase);
        SpawnFlyingEnemy(185, 6, FlyingEnemy.FlyPattern.Horizontal);

        // Ziplayan dusmanlar
        SpawnJumpingEnemy(65, 1);
        SpawnJumpingEnemy(135, 1);
        SpawnJumpingEnemy(175, 1);

        // Ates eden dusmanlar
        SpawnShootingEnemy(95, 1);
        SpawnShootingEnemy(155, 6);

        // Hareketli platformlar
        SpawnMovingPlatform(22, 3, MovingPlatform.MoveType.Vertical);
        SpawnMovingPlatform(47, 4, MovingPlatform.MoveType.Horizontal);
        SpawnMovingPlatform(78, 5, MovingPlatform.MoveType.Circular);
        SpawnMovingPlatform(112, 4, MovingPlatform.MoveType.Vertical);
        SpawnMovingPlatform(147, 5, MovingPlatform.MoveType.Horizontal);
        SpawnMovingPlatform(188, 4, MovingPlatform.MoveType.Vertical);

        // Coinler
        SpawnCoinRow(5, 2, 6);
        SpawnCoinRow(12, 4, 4);
        SpawnCoinRow(22, 5, 5);
        SpawnCoinRow(32, 5, 4);
        SpawnCoinRow(45, 4, 5);

        SpawnCoinRow(52, 6, 4);
        SpawnCoinRow(60, 8, 3);
        SpawnCoinRow(68, 5, 5);

        SpawnCoinRow(82, 6, 5);
        SpawnCoinRow(92, 8, 4);
        SpawnCoinRow(105, 5, 4);

        SpawnCoinRow(118, 6, 4);
        SpawnCoinRow(132, 8, 4);
        SpawnCoinRow(145, 5, 5);

        SpawnCoinRow(158, 7, 4);
        SpawnCoinRow(170, 9, 3);
        SpawnCoinRow(182, 6, 5);

        SpawnCoinRow(193, 7, 4);
        SpawnCoinRow(205, 9, 4);
        SpawnCoinRow(215, 6, 4);

        // Power-up'lar
        SpawnPowerUp(25, 5, PowerUpType.SpeedBoost);
        SpawnPowerUp(60, 4, PowerUpType.DoubleJump);
        SpawnPowerUp(100, 6, PowerUpType.Shield);
        SpawnPowerUp(140, 5, PowerUpType.Magnet);
        SpawnPowerUp(180, 7, PowerUpType.Invincibility);

        // Checkpoint'ler
        SpawnCheckpoint(50, 1);
        SpawnCheckpoint(100, 1);
        SpawnCheckpoint(150, 1);
        SpawnCheckpoint(200, 1);

        // Boss arena (level sonunda)
        CreateBossArena(225);

        // Bitis noktasi (boss'tan sonra)
        SpawnGoal(260, 1);
    }

    void CreateBossArena(int startX)
    {
        // Boss arenasi icin genis zemin
        CreateArenaGround(startX, startX + 30);

        // Arena duvarlari (kacis engeli)
        CreateArenaWall(startX, 0, 10);
        CreateArenaWall(startX + 30, 0, 10);

        // Boss trigger
        SpawnBossTrigger(startX + 5, 3);

        // Arena icinde platformlar
        CreatePlatformBlock(startX + 5, 4, 4);
        CreatePlatformBlock(startX + 20, 4, 4);
        CreatePlatformBlock(startX + 12, 7, 5);
    }

    void CreateArenaGround(int startX, int endX)
    {
        float width = endX - startX;
        float centerX = startX + width / 2f;

        GameObject ground = new GameObject("BossArenaGround");
        ground.transform.position = new Vector3(centerX, -0.5f, 0); // Ust kisim y=1 olacak
        ground.transform.localScale = new Vector3(width, 3f, 1f);

        EnsureWhiteSprite();

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.3f, 0.1f, 0.3f); // Koyu mor
        sr.sortingOrder = 0;

        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();

        Debug.Log($"Arena ground created at {ground.transform.position}");
    }

    void CreateArenaWall(int x, int y, int height)
    {
        GameObject wall = new GameObject("ArenaWall");
        wall.transform.position = new Vector3(x, y + height / 2f, 0);
        wall.transform.localScale = new Vector3(1f, height, 1f);

        EnsureWhiteSprite();

        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.5f, 0.2f, 0.5f);
        sr.sortingOrder = 1;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
    }

    void SpawnBossTrigger(int x, int y)
    {
        GameObject trigger = new GameObject("BossTrigger");
        trigger.transform.position = new Vector3(x, y, 0);

        BoxCollider2D col = trigger.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3f, 5f);
        col.isTrigger = true;

        BossTrigger bossTrigger = trigger.AddComponent<BossTrigger>();
        bossTrigger.bossSpawnPosition = new Vector3(x + 15, 8, 0);
        bossTrigger.arenaMinX = x - 5;
        bossTrigger.arenaMaxX = x + 25;
    }

    void SpawnCheckpoint(int x, int y)
    {
        GameObject checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.position = new Vector3(x + 0.5f, y + 1.5f, 0);
        checkpoint.AddComponent<Checkpoint>();
    }

    void SpawnPowerUp(int x, int y, PowerUpType type)
    {
        GameObject powerUpObj = new GameObject("PowerUp_" + type.ToString());
        powerUpObj.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);

        PowerUp powerUp = powerUpObj.AddComponent<PowerUp>();
        powerUp.powerUpType = type;

        // Tur bazli sure ayarla
        switch (type)
        {
            case PowerUpType.SpeedBoost:
                powerUp.duration = 5f;
                break;
            case PowerUpType.DoubleJump:
                powerUp.duration = 10f;
                break;
            case PowerUpType.Shield:
                powerUp.duration = 999f; // Tek kullanimlik
                break;
            case PowerUpType.Magnet:
                powerUp.duration = 8f;
                break;
            case PowerUpType.Invincibility:
                powerUp.duration = 5f;
                break;
        }
    }

    void BuildGroundSection(int startX, int endX)
    {
        for (int x = startX; x < endX; x++)
        {
            // Ust katman (cimen)
            if (grassTile != null)
                groundTilemap.SetTile(new Vector3Int(x, 0, 0), grassTile);
            else if (groundTile != null)
                groundTilemap.SetTile(new Vector3Int(x, 0, 0), groundTile);

            // Alt katmanlar (toprak)
            for (int y = -1; y >= -groundHeight; y--)
            {
                if (groundTile != null)
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
            }
        }
    }

    void BuildPlatform(int x, int y, int width)
    {
        Tilemap tm = platformTilemap != null ? platformTilemap : groundTilemap;

        for (int i = 0; i < width; i++)
        {
            TileBase tile = platformTile;

            // Sol, orta, sag tile secimi
            if (platformLeftTile != null && platformRightTile != null && platformMiddleTile != null)
            {
                if (i == 0)
                    tile = platformLeftTile;
                else if (i == width - 1)
                    tile = platformRightTile;
                else
                    tile = platformMiddleTile;
            }

            if (tile != null)
                tm.SetTile(new Vector3Int(x + i, y, 0), tile);
        }
    }

    void SpawnEnemy(int x, int y)
    {
        if (enemyPrefab != null)
        {
            Instantiate(enemyPrefab, new Vector3(x + 0.5f, y + 0.5f, 0), Quaternion.identity);
        }
        else
        {
            CreateSimpleEnemy(x + 0.5f, y + 0.5f);
        }
    }

    void SpawnCoinRow(int startX, int y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnCoin(startX + i, y);
        }
    }

    void SpawnCoin(int x, int y)
    {
        if (coinPrefab != null)
        {
            Instantiate(coinPrefab, new Vector3(x + 0.5f, y + 0.5f, 0), Quaternion.identity);
        }
        else
        {
            CreateSimpleCoin(x + 0.5f, y + 0.5f);
        }
    }

    void SpawnGoal(int x, int y)
    {
        if (goalPrefab != null)
        {
            Instantiate(goalPrefab, new Vector3(x + 0.5f, y + 2f, 0), Quaternion.identity);
        }
        else
        {
            CreateSimpleGoal(x + 0.5f, y + 2f);
        }
    }

    void CreateSimpleEnemy(float x, float y)
    {
        GameObject enemy = new GameObject("Enemy");
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.tag = "Enemy";

        EnsureWhiteSprite();

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.8f, 0.2f, 0.2f); // Kirmizi
        sr.sortingOrder = 5;

        // Boyut ayarla
        enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        Enemy enemyScript = enemy.AddComponent<Enemy>();
        enemyScript.groundLayer = LayerMask.GetMask("Ground");
        enemyScript.moveSpeed = 2f;
    }

    void CreateSimpleCoin(float x, float y)
    {
        GameObject coin = new GameObject("Coin");
        coin.transform.position = new Vector3(x, y, 0);

        EnsureWhiteSprite();

        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(1f, 0.85f, 0f); // Altin sarisi
        sr.sortingOrder = 5;

        // Boyut ayarla (daire gibi gozukmesi icin)
        coin.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        coin.AddComponent<Coin>();
    }

    void CreateSimpleGoal(float x, float y)
    {
        GameObject goal = new GameObject("Goal");
        goal.transform.position = new Vector3(x, y, 0);

        EnsureWhiteSprite();

        SpriteRenderer sr = goal.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.2f, 0.9f, 0.3f); // Yesil
        sr.sortingOrder = 5;

        // Bayrak gibi uzun bir sekil
        goal.transform.localScale = new Vector3(1f, 4f, 1f);

        BoxCollider2D col = goal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        goal.AddComponent<Goal>();
    }

    void SpawnFlyingEnemy(int x, int y, FlyingEnemy.FlyPattern pattern)
    {
        GameObject enemy = new GameObject("FlyingEnemy");
        enemy.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
        enemy.tag = "Enemy";

        EnsureWhiteSprite();

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.6f, 0.2f, 0.8f); // Mor (ucan dusman)
        sr.sortingOrder = 5;

        enemy.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        FlyingEnemy flyingEnemy = enemy.AddComponent<FlyingEnemy>();
        flyingEnemy.pattern = pattern;
        flyingEnemy.moveSpeed = 3f;
        flyingEnemy.moveDistance = 3f;
        flyingEnemy.circleRadius = 2f;
        flyingEnemy.chaseRange = 6f;
    }

    void SpawnJumpingEnemy(int x, int y)
    {
        GameObject enemy = new GameObject("JumpingEnemy");
        enemy.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
        enemy.tag = "Enemy";

        EnsureWhiteSprite();

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.2f, 0.8f, 0.2f); // Yesil (ziplayan dusman)
        sr.sortingOrder = 5;

        enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        JumpingEnemy jumpEnemy = enemy.AddComponent<JumpingEnemy>();
        jumpEnemy.groundLayer = LayerMask.GetMask("Ground");
        jumpEnemy.jumpForce = 12f;
        jumpEnemy.jumpInterval = 2f;
        jumpEnemy.chasePlayer = true;
        jumpEnemy.chaseRange = 6f;
    }

    void SpawnShootingEnemy(int x, int y)
    {
        GameObject enemy = new GameObject("ShootingEnemy");
        enemy.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
        enemy.tag = "Enemy";

        EnsureWhiteSprite();

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(1f, 0.5f, 0f); // Turuncu (ates eden dusman)
        sr.sortingOrder = 5;

        enemy.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        ShootingEnemy shootEnemy = enemy.AddComponent<ShootingEnemy>();
        shootEnemy.shootInterval = 2.5f;
        shootEnemy.projectileSpeed = 6f;
        shootEnemy.detectionRange = 10f;
        shootEnemy.obstacleLayer = LayerMask.GetMask("Ground");
    }

    void SpawnMovingPlatform(int x, int y, MovingPlatform.MoveType moveType)
    {
        GameObject platform = new GameObject("MovingPlatform");
        platform.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
        // Ground layer varsa kullan, yoksa default (0)
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            platform.layer = groundLayer;

        EnsureWhiteSprite();

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.4f, 0.4f, 0.6f); // Gri-mavi
        sr.sortingOrder = 2;

        platform.transform.localScale = new Vector3(3f, 0.5f, 1f);

        Rigidbody2D rb = platform.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        // Surtunmesiz physics material olustur
        PhysicsMaterial2D frictionlessMat = new PhysicsMaterial2D("PlatformMaterial");
        frictionlessMat.friction = 0f;
        frictionlessMat.bounciness = 0f;
        col.sharedMaterial = frictionlessMat;

        MovingPlatform mp = platform.AddComponent<MovingPlatform>();
        mp.moveType = moveType;
        mp.moveDistance = 3f;
        mp.moveSpeed = 2f;
        mp.circleRadius = 2f;
        mp.waitTime = 0.3f;
    }

    void BuildLevelWithGameObjects()
    {
        Debug.Log("========================================");
        Debug.Log("BuildLevelWithGameObjects BASLIYOR!");
        Debug.Log("WhiteSprite durumu: " + (whiteSprite != null ? "HAZIR" : "NULL - SORUN!"));
        Debug.Log("========================================");

        // Sprite yoksa olustur
        if (whiteSprite == null)
        {
            Debug.LogWarning("WhiteSprite null! Yeniden olusturuluyor...");
            CreateWhiteSprite();
        }

        // Zemin bloklari
        CreateGroundBlock(0, 20);
        CreateGroundBlock(25, 45);
        CreateGroundBlock(50, 75);
        CreateGroundBlock(80, 110);
        CreateGroundBlock(115, 145);
        CreateGroundBlock(150, 180);
        CreateGroundBlock(185, 220);

        // Platformlar
        CreatePlatformBlock(10, 3, 5);
        CreatePlatformBlock(18, 5, 4);
        CreatePlatformBlock(28, 4, 6);
        CreatePlatformBlock(38, 6, 4);
        CreatePlatformBlock(48, 3, 5);
        CreatePlatformBlock(55, 5, 4);
        CreatePlatformBlock(63, 7, 3);
        CreatePlatformBlock(70, 4, 5);
        CreatePlatformBlock(85, 5, 6);
        CreatePlatformBlock(95, 7, 4);
        CreatePlatformBlock(103, 4, 5);
        CreatePlatformBlock(120, 5, 4);
        CreatePlatformBlock(128, 7, 5);
        CreatePlatformBlock(138, 4, 4);
        CreatePlatformBlock(155, 6, 5);
        CreatePlatformBlock(165, 8, 4);
        CreatePlatformBlock(175, 5, 5);
        CreatePlatformBlock(190, 6, 4);
        CreatePlatformBlock(200, 8, 5);
        CreatePlatformBlock(210, 5, 4);

        // Kirmizi dusmanlar kaldirildi

        // Ucan dusmanlar
        SpawnFlyingEnemy(40, 6, FlyingEnemy.FlyPattern.Horizontal);
        SpawnFlyingEnemy(75, 5, FlyingEnemy.FlyPattern.Vertical);
        SpawnFlyingEnemy(110, 7, FlyingEnemy.FlyPattern.Circular);
        SpawnFlyingEnemy(150, 5, FlyingEnemy.FlyPattern.Chase);
        SpawnFlyingEnemy(185, 6, FlyingEnemy.FlyPattern.Horizontal);

        // Ziplayan ve ates eden
        SpawnJumpingEnemy(65, 1); SpawnJumpingEnemy(135, 1); SpawnJumpingEnemy(175, 1);
        SpawnShootingEnemy(95, 1); SpawnShootingEnemy(155, 6);

        // Hareketli platformlar
        SpawnMovingPlatform(22, 3, MovingPlatform.MoveType.Vertical);
        SpawnMovingPlatform(47, 4, MovingPlatform.MoveType.Horizontal);
        SpawnMovingPlatform(78, 5, MovingPlatform.MoveType.Circular);
        SpawnMovingPlatform(112, 4, MovingPlatform.MoveType.Vertical);
        SpawnMovingPlatform(147, 5, MovingPlatform.MoveType.Horizontal);
        SpawnMovingPlatform(188, 4, MovingPlatform.MoveType.Vertical);

        // Coinler
        SpawnCoinRow(5, 2, 6); SpawnCoinRow(12, 4, 4); SpawnCoinRow(22, 5, 5);
        SpawnCoinRow(32, 5, 4); SpawnCoinRow(45, 4, 5); SpawnCoinRow(52, 6, 4);
        SpawnCoinRow(60, 8, 3); SpawnCoinRow(68, 5, 5); SpawnCoinRow(82, 6, 5);
        SpawnCoinRow(92, 8, 4); SpawnCoinRow(105, 5, 4); SpawnCoinRow(118, 6, 4);
        SpawnCoinRow(132, 8, 4); SpawnCoinRow(145, 5, 5); SpawnCoinRow(158, 7, 4);
        SpawnCoinRow(170, 9, 3); SpawnCoinRow(182, 6, 5); SpawnCoinRow(193, 7, 4);
        SpawnCoinRow(205, 9, 4); SpawnCoinRow(215, 6, 4);

        // Power-up'lar
        SpawnPowerUp(25, 5, PowerUpType.SpeedBoost);
        SpawnPowerUp(60, 4, PowerUpType.DoubleJump);
        SpawnPowerUp(100, 6, PowerUpType.Shield);
        SpawnPowerUp(140, 5, PowerUpType.Magnet);
        SpawnPowerUp(180, 7, PowerUpType.Invincibility);

        // Checkpoint'ler
        SpawnCheckpoint(50, 1);
        SpawnCheckpoint(100, 1);
        SpawnCheckpoint(150, 1);
        SpawnCheckpoint(200, 1);

        SpawnGoal(218, 1);
    }

    void CreateGroundBlock(int startX, int endX)
    {
        float width = endX - startX;
        float centerX = startX + width / 2f;

        GameObject ground = new GameObject("Ground_" + startX);
        ground.transform.position = new Vector3(centerX, -0.5f, 0); // Biraz asagi - ust kisim y=1 olacak
        ground.transform.localScale = new Vector3(width, 3f, 1f);

        // Sprite olustur
        EnsureWhiteSprite();

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.2f, 0.1f, 0.4f);
        sr.sortingOrder = 0;

        // Collider - BoxCollider2D scale ile birlikte calisir
        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
        // Size varsayilan olarak (1,1) ve scale ile carpilir

        Debug.Log($"Ground created: {ground.name} at {ground.transform.position}, scale: {ground.transform.localScale}");
    }

    void CreatePlatformBlock(int x, int y, int width)
    {
        float centerX = x + width / 2f;

        GameObject platform = new GameObject("Platform_" + x);
        platform.transform.position = new Vector3(centerX, y + 0.5f, 0);
        platform.transform.localScale = new Vector3(width, 1f, 1f);

        // Sprite olustur
        EnsureWhiteSprite();

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.6f, 0.2f, 0.6f);
        sr.sortingOrder = 1;

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();

        Debug.Log($"Platform created: {platform.name} at {platform.transform.position}");
    }

    void EnsureWhiteSprite()
    {
        if (whiteSprite == null)
        {
            CreateWhiteSprite();
        }
    }
}
