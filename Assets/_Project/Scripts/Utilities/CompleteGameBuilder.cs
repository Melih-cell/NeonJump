using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Tek tikla tamamen calisan bir oyun olusturur.
/// Harici asset indirmeye gerek yok - her sey kod ile olusturulur.
/// Pixel Adventure kalitesinde grafikler.
/// </summary>
public class CompleteGameBuilder : MonoBehaviour
{
    [Header("Character Selection")]
    public CharacterType characterType = CharacterType.MaskDude;

    [Header("Game Settings")]
    public int startingHealth = 3;
    public float levelLength = 250f;

    [Header("Difficulty")]
    public Difficulty difficulty = Difficulty.Normal;

    public enum CharacterType { MaskDude, NinjaFrog, PinkMan, VirtualGuy }
    public enum Difficulty { Easy, Normal, Hard }

    // References
    private PixelArtGenerator artGenerator;
    private GameObject player;
    private Transform levelContainer;
    private Transform enemyContainer;
    private Transform itemContainer;

    // Terrain sprites
    private Sprite[] terrainTiles;

    // Character colors
    private Color playerBody;
    private Color playerMask;

    void Awake()
    {
        // Art generator olustur
        GameObject genObj = new GameObject("PixelArtGenerator");
        artGenerator = genObj.AddComponent<PixelArtGenerator>();
    }

    void Start()
    {
        SetCharacterColors();
        BuildCompleteGame();
    }

    void SetCharacterColors()
    {
        switch (characterType)
        {
            case CharacterType.MaskDude:
                playerBody = PixelArtGenerator.Palette.MaskDudeBody;
                playerMask = PixelArtGenerator.Palette.MaskDudeMask;
                break;
            case CharacterType.NinjaFrog:
                playerBody = PixelArtGenerator.Palette.NinjaFrogBody;
                playerMask = new Color(0.3f, 0.6f, 0.25f);
                break;
            case CharacterType.PinkMan:
                playerBody = PixelArtGenerator.Palette.PinkManBody;
                playerMask = new Color(0.8f, 0.3f, 0.5f);
                break;
            case CharacterType.VirtualGuy:
                playerBody = PixelArtGenerator.Palette.VirtualGuyBody;
                playerMask = new Color(0.2f, 0.4f, 0.7f);
                break;
        }
    }

    void BuildCompleteGame()
    {
        Debug.Log("=== OYUN OLUSTURULUYOR ===");

        // 1. Containers
        CreateContainers();

        // 2. Camera
        SetupCamera();

        // 3. Terrain sprites
        terrainTiles = artGenerator.GetAllTerrainTiles();

        // 4. Background
        CreateBackground();

        // 5. Level
        CreateLevel();

        // 6. Player
        CreatePlayer();

        // 7. Managers
        CreateManagers();

        // 8. UI
        CreateUI();

        Debug.Log("=== OYUN HAZIR! ===");
        Debug.Log("Kontroller: A/D veya Ok tuslari = Hareket, Space/W = Zipla, ESC = Duraklat");
    }

    void CreateContainers()
    {
        levelContainer = new GameObject("Level").transform;
        enemyContainer = new GameObject("Enemies").transform;
        itemContainer = new GameObject("Items").transform;
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.backgroundColor = new Color(0.3f, 0.5f, 0.7f);
        cam.transform.position = new Vector3(5, 5, -10);
    }

    void CreateBackground()
    {
        // Sky gradient
        Sprite skySprite = artGenerator.GenerateBackgroundLayer(0, 512);
        GameObject sky = new GameObject("Sky");
        sky.transform.SetParent(levelContainer);
        sky.transform.position = new Vector3(levelLength / 2f, 6, 10);
        sky.transform.localScale = new Vector3(levelLength / 32f + 2, 1, 1);

        SpriteRenderer skySR = sky.AddComponent<SpriteRenderer>();
        skySR.sprite = skySprite;
        skySR.sortingOrder = -100;

        // Mountains
        CreateParallaxLayer(1, -80, 0.3f);

        // Hills
        CreateParallaxLayer(2, -60, 0.5f);
    }

    void CreateParallaxLayer(int layer, int sortOrder, float scale)
    {
        Sprite sprite = artGenerator.GenerateBackgroundLayer(layer, 256);

        int copies = (int)(levelLength / 16) + 2;
        for (int i = 0; i < copies; i++)
        {
            GameObject bg = new GameObject($"BG_Layer{layer}_{i}");
            bg.transform.SetParent(levelContainer);
            bg.transform.position = new Vector3(i * 16 - 8, 3, 5 + layer);
            bg.transform.localScale = new Vector3(scale, scale, 1);

            SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;
        }
    }

    void CreateLevel()
    {
        // ========== BOLUM 1: TUTORIAL (x: 0-60) ==========
        CreateSection1_Tutorial();

        // ========== BOLUM 2: ORMAN (x: 60-130) ==========
        CreateSection2_Forest();

        // ========== BOLUM 3: MAGARA (x: 130-200) ==========
        CreateSection3_Cave();

        // ========== BOLUM 4: BOSS (x: 200-250) ==========
        CreateSection4_Boss();
    }

    void CreateSection1_Tutorial()
    {
        // Zemin
        CreateGround(0, 0, 20, 3);
        CreateGround(25, 0, 15, 3);
        CreateGround(45, 0, 15, 3);

        // Platformlar
        CreatePlatform(10, 3, 5);
        CreatePlatform(20, 5, 4);
        CreatePlatform(32, 4, 5);
        CreatePlatform(48, 3, 4);

        // Basit dusmanlar
        CreateSlime(15, 3.5f);
        CreateSlime(38, 3.5f);
        CreateMushroom(52, 3.5f);

        // Meyveler
        CreateFruitRow(5, 2, 5);
        CreateFruitArc(22, 6, 5, 2f);
        CreateFruitRow(34, 5, 5);
        CreateFruitRow(50, 4, 4);

        // Power-up
        CreatePowerUp(40, 5, PowerUpType.DoubleJump);

        // Checkpoint
        CreateCheckpoint(58, 3.5f);
    }

    void CreateSection2_Forest()
    {
        // Zemin (bosluklar var)
        CreateGround(60, 0, 18, 3);
        CreateGround(82, 0, 15, 3);
        CreateGround(102, 0, 28, 3);

        // Platformlar
        CreatePlatform(65, 3, 4);
        CreatePlatform(72, 5, 4);
        CreatePlatform(78, 3, 3); // Bosluk uzerinde
        CreatePlatform(88, 4, 5);
        CreatePlatform(96, 6, 4); // Bosluk uzerinde
        CreatePlatform(108, 4, 5);
        CreatePlatform(118, 6, 4);

        // Hareketli platformlar
        CreateMovingPlatform(80, 2, true, 4f);
        CreateMovingPlatform(99, 3, false, 3f);

        // Dusmanlar
        CreateSlime(68, 3.5f);
        CreateMushroom(85, 3.5f);
        CreateSlime(92, 5f);
        CreateMushroom(112, 3.5f);
        CreateBat(75, 6);
        CreateGhost(105, 7);

        // Tuzaklar
        CreateSpike(77, 3);
        CreateSaw(95, 3);

        // Meyveler
        CreateFruitRow(62, 4, 4);
        CreateFruitArc(74, 6, 4, 2f);
        CreateFruitRow(90, 5, 5);
        CreateFruitArc(110, 5, 5, 2.5f);
        CreateFruitRow(120, 7, 4);

        // Power-up
        CreatePowerUp(83, 7, PowerUpType.Shield);

        // Checkpoint
        CreateCheckpoint(128, 3.5f);
    }

    void CreateSection3_Cave()
    {
        // Zemin
        CreateGround(130, 0, 18, 3);
        CreateGround(152, 0, 15, 3);
        CreateGround(172, 0, 28, 3);

        // Platformlar - daha zor
        CreatePlatform(135, 3, 3);
        CreatePlatform(142, 5, 3);
        CreatePlatform(148, 7, 3);
        CreatePlatform(150, 3, 3); // Bosluk uzerinde
        CreatePlatform(158, 5, 4);
        CreatePlatform(165, 7, 3);
        CreatePlatform(168, 3, 3); // Bosluk uzerinde
        CreatePlatform(178, 5, 4);
        CreatePlatform(188, 7, 4);

        // Hareketli platformlar
        CreateMovingPlatform(147, 3, false, 4f);
        CreateMovingPlatform(166, 4, true, 5f);
        CreateMovingPlatform(185, 3, false, 3f);

        // Dusmanlar - daha zor
        CreateMushroom(138, 3.5f);
        CreateMushroom(155, 3.5f);
        CreateSlime(148, 8f);
        CreateSlime(175, 3.5f);
        CreateMushroom(182, 3.5f);
        CreateMushroom(188, 8f);
        CreateBat(143, 6);
        CreateBat(162, 8);
        CreateGhost(180, 6);

        // Tuzaklar
        CreateSpike(149, 3);
        CreateSpike(150, 3);
        CreateSpike(167, 3);
        CreateSaw(160, 3);
        CreateSaw(176, 3);

        // Meyveler
        CreateFruitRow(132, 4, 4);
        CreateFruitArc(144, 6, 4, 2f);
        CreateFruitRow(159, 6, 5);
        CreateFruitArc(173, 4, 5, 2f);
        CreateFruitRow(190, 8, 4);

        // Power-up'lar
        CreatePowerUp(152, 8, PowerUpType.SpeedBoost);
        CreatePowerUp(183, 8, PowerUpType.Invincibility);

        // Checkpoint
        CreateCheckpoint(198, 3.5f);
    }

    void CreateSection4_Boss()
    {
        // Arena zemini
        CreateGround(200, 0, 50, 3);

        // Arena duvarlari
        CreateWall(200, 3, 12);
        CreateWall(249, 3, 12);

        // Arena platformlari
        CreatePlatform(210, 5, 5);
        CreatePlatform(235, 5, 5);
        CreatePlatform(220, 8, 8);

        // Meyveler
        CreateFruitRow(212, 6, 4);
        CreateFruitRow(237, 6, 4);
        CreateFruitArc(223, 9, 6, 1.5f);

        // Boss trigger
        CreateBossTrigger(205);

        // Bitis
        CreateGoal(255, 3.5f);
    }

    #region TERRAIN BUILDERS

    void CreateGround(float x, float y, int width, int height)
    {
        for (int tx = 0; tx < width; tx++)
        {
            for (int ty = 0; ty < height; ty++)
            {
                int tileType = GetTileType(tx, ty, width, height);
                CreateTile(x + tx, y + ty, terrainTiles[tileType], 0);
            }
        }

        // Collider
        GameObject col = new GameObject($"GroundCol_{x}");
        col.transform.SetParent(levelContainer);
        col.transform.position = new Vector3(x + width / 2f, y + height / 2f, 0);
        BoxCollider2D box = col.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);
    }

    void CreatePlatform(float x, float y, int width)
    {
        for (int tx = 0; tx < width; tx++)
        {
            int tileType = (tx == 0) ? 0 : (tx == width - 1) ? 2 : 1;
            CreateTile(x + tx, y, terrainTiles[tileType], 1);
        }

        // Collider
        GameObject col = new GameObject($"PlatformCol_{x}");
        col.transform.SetParent(levelContainer);
        col.transform.position = new Vector3(x + width / 2f, y + 0.5f, 0);
        BoxCollider2D box = col.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, 1);
    }

    void CreateWall(float x, float y, int height)
    {
        for (int ty = 0; ty < height; ty++)
        {
            int tileType = (ty == height - 1) ? 1 : (ty == 0) ? 7 : 4;
            CreateTile(x, y + ty, terrainTiles[tileType], 1);
        }

        // Collider
        GameObject col = new GameObject($"WallCol_{x}");
        col.transform.SetParent(levelContainer);
        col.transform.position = new Vector3(x + 0.5f, y + height / 2f, 0);
        BoxCollider2D box = col.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1, height);
    }

    int GetTileType(int tx, int ty, int width, int height)
    {
        bool isLeft = tx == 0;
        bool isRight = tx == width - 1;
        bool isTop = ty == height - 1;
        bool isBottom = ty == 0;

        if (isTop && isLeft) return 0;
        if (isTop && isRight) return 2;
        if (isTop) return 1;
        if (isBottom && isLeft) return 6;
        if (isBottom && isRight) return 8;
        if (isBottom) return 7;
        if (isLeft) return 3;
        if (isRight) return 5;
        return 4;
    }

    void CreateTile(float x, float y, Sprite sprite, int sortOrder)
    {
        GameObject tile = new GameObject($"Tile");
        tile.transform.SetParent(levelContainer);
        tile.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortOrder;
    }

    void CreateMovingPlatform(float x, float y, bool horizontal, float distance)
    {
        GameObject platform = new GameObject("MovingPlatform");
        platform.transform.SetParent(levelContainer);
        platform.transform.position = new Vector3(x, y, 0);

        // 3 tile platform
        for (int i = 0; i < 3; i++)
        {
            GameObject tile = new GameObject($"Tile_{i}");
            tile.transform.SetParent(platform.transform);
            tile.transform.localPosition = new Vector3(i, 0, 0);

            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = terrainTiles[(i == 0) ? 0 : (i == 2) ? 2 : 1];
            sr.color = new Color(0.7f, 0.85f, 1f);
            sr.sortingOrder = 2;
        }

        Rigidbody2D rb = platform.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3, 1);
        col.offset = new Vector2(1, 0.5f);

        MovingPlatform mp = platform.AddComponent<MovingPlatform>();
        mp.moveType = horizontal ? MovingPlatform.MoveType.Horizontal : MovingPlatform.MoveType.Vertical;
        mp.moveDistance = distance;
        mp.moveSpeed = 2f;
    }

    #endregion

    #region ENEMY BUILDERS

    void CreateSlime(float x, float y)
    {
        GameObject enemy = new GameObject("Slime");
        enemy.transform.SetParent(enemyContainer);
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        Sprite[] idleSprites = artGenerator.GenerateSlimeIdle(PixelArtGenerator.Palette.SlimeGreen);
        sr.sprite = idleSprites[0];
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 0.8f);

        AnimatedEnemy ae = enemy.AddComponent<AnimatedEnemy>();
        ae.idleSprites = idleSprites;
        ae.runSprites = artGenerator.GenerateSlimeRun(PixelArtGenerator.Palette.SlimeGreen);
        ae.moveSpeed = GetEnemySpeed(1.5f);
        ae.jumps = true;
        ae.jumpForce = 8f;
        ae.jumpInterval = 2f;
    }

    void CreateMushroom(float x, float y)
    {
        GameObject enemy = new GameObject("Mushroom");
        enemy.transform.SetParent(enemyContainer);
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        Sprite[] idleSprites = artGenerator.GenerateMushroomIdle(PixelArtGenerator.Palette.MushroomRed);
        sr.sprite = idleSprites[0];
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1.2f);

        AnimatedEnemy ae = enemy.AddComponent<AnimatedEnemy>();
        ae.idleSprites = idleSprites;
        ae.runSprites = artGenerator.GenerateMushroomRun(PixelArtGenerator.Palette.MushroomRed);
        ae.moveSpeed = GetEnemySpeed(3f);
        ae.chasesPlayer = true;
        ae.chaseRange = 8f;
    }

    void CreateBat(float x, float y)
    {
        GameObject enemy = new GameObject("Bat");
        enemy.transform.SetParent(enemyContainer);
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        Sprite[] flySprites = artGenerator.GenerateBatFly();
        sr.sprite = flySprites[0];
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        AnimatedEnemy ae = enemy.AddComponent<AnimatedEnemy>();
        ae.idleSprites = flySprites;
        ae.runSprites = flySprites;
        ae.isFlying = true;
        ae.moveSpeed = GetEnemySpeed(3f);
        ae.chasesPlayer = true;
        ae.chaseRange = 6f;
    }

    void CreateGhost(float x, float y)
    {
        GameObject enemy = new GameObject("Ghost");
        enemy.transform.SetParent(enemyContainer);
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        Sprite[] idleSprites = artGenerator.GenerateGhostIdle();
        sr.sprite = idleSprites[0];
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1.2f);

        AnimatedEnemy ae = enemy.AddComponent<AnimatedEnemy>();
        ae.idleSprites = idleSprites;
        ae.runSprites = idleSprites;
        ae.isFlying = true;
        ae.moveSpeed = GetEnemySpeed(2f);
        ae.floats = true;
    }

    float GetEnemySpeed(float baseSpeed)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return baseSpeed * 0.7f;
            case Difficulty.Hard: return baseSpeed * 1.4f;
            default: return baseSpeed;
        }
    }

    #endregion

    #region TRAP BUILDERS

    void CreateSpike(float x, float y)
    {
        GameObject spike = new GameObject("Spike");
        spike.transform.SetParent(levelContainer);
        spike.transform.position = new Vector3(x + 0.5f, y + 0.3f, 0);
        spike.tag = "Enemy";

        SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
        sr.sprite = artGenerator.GenerateSpikeSprite();
        sr.sortingOrder = 3;

        BoxCollider2D col = spike.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.5f);
    }

    void CreateSaw(float x, float y)
    {
        GameObject saw = new GameObject("Saw");
        saw.transform.SetParent(levelContainer);
        saw.transform.position = new Vector3(x, y, 0);
        saw.tag = "Enemy";

        SpriteRenderer sr = saw.AddComponent<SpriteRenderer>();
        Sprite[] sawSprites = artGenerator.GenerateSawSprites();
        sr.sprite = sawSprites[0];
        sr.sortingOrder = 4;

        CircleCollider2D col = saw.AddComponent<CircleCollider2D>();
        col.radius = 0.6f;

        AnimatedSaw asaw = saw.AddComponent<AnimatedSaw>();
        asaw.sprites = sawSprites;
    }

    #endregion

    #region ITEM BUILDERS

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

        SpriteRenderer sr = fruit.AddComponent<SpriteRenderer>();
        Sprite[] fruits = artGenerator.GenerateFruitSprites();
        sr.sprite = fruits[Random.Range(0, 4)]; // Random meyve
        sr.sortingOrder = 5;

        CircleCollider2D col = fruit.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        fruit.AddComponent<Coin>();
        fruit.AddComponent<FloatingItem>();
    }

    void CreatePowerUp(float x, float y, PowerUpType type)
    {
        GameObject powerUp = new GameObject($"PowerUp_{type}");
        powerUp.transform.SetParent(itemContainer);
        powerUp.transform.position = new Vector3(x, y, 0);

        PowerUp pu = powerUp.AddComponent<PowerUp>();
        pu.powerUpType = type;

        powerUp.AddComponent<FloatingItem>();
    }

    void CreateCheckpoint(float x, float y)
    {
        GameObject checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.SetParent(itemContainer);
        checkpoint.transform.position = new Vector3(x, y, 0);

        checkpoint.AddComponent<Checkpoint>();
    }

    void CreateBossTrigger(float x)
    {
        GameObject trigger = new GameObject("BossTrigger");
        trigger.transform.SetParent(levelContainer);
        trigger.transform.position = new Vector3(x, 5, 0);

        BoxCollider2D col = trigger.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3, 8);
        col.isTrigger = true;

        BossTrigger bt = trigger.AddComponent<BossTrigger>();
        bt.bossSpawnPosition = new Vector3(x + 20, 10, 0);
        bt.arenaMinX = 200;
        bt.arenaMaxX = 250;
    }

    void CreateGoal(float x, float y)
    {
        GameObject goal = new GameObject("Goal");
        goal.transform.SetParent(itemContainer);
        goal.transform.position = new Vector3(x, y + 2, 0);

        // Flag pole
        GameObject pole = new GameObject("Pole");
        pole.transform.SetParent(goal.transform);
        pole.transform.localPosition = Vector3.zero;
        pole.transform.localScale = new Vector3(0.2f, 4f, 1);

        SpriteRenderer poleSR = pole.AddComponent<SpriteRenderer>();
        poleSR.sprite = artGenerator.GenerateTerrainTile(4);
        poleSR.color = new Color(0.6f, 0.4f, 0.2f);
        poleSR.sortingOrder = 4;

        // Flag
        GameObject flag = new GameObject("Flag");
        flag.transform.SetParent(goal.transform);
        flag.transform.localPosition = new Vector3(0.8f, 1.5f, 0);
        flag.transform.localScale = new Vector3(1.5f, 1f, 1);

        SpriteRenderer flagSR = flag.AddComponent<SpriteRenderer>();
        flagSR.sprite = artGenerator.GenerateFruitSprites()[4]; // Coin sprite as flag for now
        flagSR.color = new Color(0.2f, 0.9f, 0.3f);
        flagSR.sortingOrder = 5;

        BoxCollider2D col = goal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2, 5);

        goal.AddComponent<Goal>();
    }

    #endregion

    #region PLAYER

    void CreatePlayer()
    {
        player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(3, 5, 0);

        // Sprite
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        Sprite[] idleSprites = artGenerator.GeneratePlayerIdle(playerBody, playerMask);
        sr.sprite = idleSprites[0];
        sr.sortingOrder = 10;

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.9f);
        col.offset = new Vector2(0, 0.1f);

        // Ground check
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

        // Animated Player Controller
        AnimatedPlayer ap = player.AddComponent<AnimatedPlayer>();
        ap.idleSprites = idleSprites;
        ap.runSprites = artGenerator.GeneratePlayerRun(playerBody, playerMask);
        ap.jumpSprites = artGenerator.GeneratePlayerJump(playerBody, playerMask);
        ap.fallSprites = artGenerator.GeneratePlayerFall(playerBody, playerMask);
        ap.doubleJumpSprites = artGenerator.GeneratePlayerDoubleJump(playerBody, playerMask);
        ap.groundCheck = groundCheck.transform;

        // Camera follow
        CameraFollow camFollow = Camera.main.gameObject.AddComponent<CameraFollow>();
        camFollow.target = player.transform;
        camFollow.smoothSpeed = 5f;
        camFollow.offset = new Vector3(4, 2, -10);
        camFollow.minX = 0;
        camFollow.maxX = levelLength + 10;
        camFollow.minY = 0;
        camFollow.maxY = 20;
    }

    #endregion

    #region MANAGERS

    void CreateManagers()
    {
        // Game Manager
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.player = player.transform;
        gm.maxHealth = startingHealth;
        gm.deathY = -5f;

        // Particle Manager
        GameObject pmObj = new GameObject("ParticleManager");
        pmObj.AddComponent<ParticleManager>();

        // Audio Manager
        GameObject amObj = new GameObject("AudioManager");
        amObj.AddComponent<AudioManager>();

        // Power Up Manager
        GameObject pumObj = new GameObject("PowerUpManager");
        pumObj.AddComponent<PowerUpManager>();
    }

    #endregion

    #region UI

    void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // UI Manager
        GameObject uiManagerObj = new GameObject("UIManager");
        UIManager uiManager = uiManagerObj.AddComponent<UIManager>();

        // HUD artik NeonHUD tarafindan yonetiliyor

        // Panels
        CreateGameOverPanel(canvasObj.transform, uiManager);
        CreateWinPanel(canvasObj.transform, uiManager);
        CreatePausePanel(canvasObj.transform, uiManager);
    }

    void CreateGameOverPanel(Transform canvas, UIManager uiManager)
    {
        GameObject panel = CreatePanel(canvas, "GameOverPanel",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 400),
            new Color(0.1f, 0.05f, 0.15f, 0.95f));
        panel.SetActive(false);
        uiManager.gameOverPanel = panel;

        CreateText(panel.transform, "GAME OVER", 56, new Vector2(0, 100),
            TextAnchor.MiddleCenter, Color.red);

        uiManager.gameOverScoreText = CreateText(panel.transform, "Skor: 0", 32,
            new Vector2(0, 30), TextAnchor.MiddleCenter, Color.white);

        uiManager.gameOverHighScoreText = CreateText(panel.transform, "En Yuksek: 0", 24,
            new Vector2(0, -10), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        CreateButton(panel.transform, "Tekrar Oyna", new Vector2(0, -70),
            () => { if (UIManager.Instance != null) UIManager.Instance.RestartGame(); });

        CreateButton(panel.transform, "Ana Menu", new Vector2(0, -130),
            () => { if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu(); });
    }

    void CreateWinPanel(Transform canvas, UIManager uiManager)
    {
        GameObject panel = CreatePanel(canvas, "WinPanel",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 400),
            new Color(0.05f, 0.15f, 0.1f, 0.95f));
        panel.SetActive(false);
        uiManager.winPanel = panel;

        CreateText(panel.transform, "KAZANDIN!", 56, new Vector2(0, 100),
            TextAnchor.MiddleCenter, Color.green);

        uiManager.winScoreText = CreateText(panel.transform, "Skor: 0", 32,
            new Vector2(0, 30), TextAnchor.MiddleCenter, Color.white);

        uiManager.winTimeText = CreateText(panel.transform, "Sure: 00:00", 24,
            new Vector2(0, -10), TextAnchor.MiddleCenter, new Color(0.7f, 1f, 0.7f));

        CreateButton(panel.transform, "Tekrar Oyna", new Vector2(0, -70),
            () => { if (UIManager.Instance != null) UIManager.Instance.RestartGame(); });
    }

    void CreatePausePanel(Transform canvas, UIManager uiManager)
    {
        GameObject panel = CreatePanel(canvas, "PausePanel",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 350),
            new Color(0.1f, 0.1f, 0.15f, 0.95f));
        panel.SetActive(false);
        uiManager.pausePanel = panel;

        CreateText(panel.transform, "DURAKLATILDI", 42, new Vector2(0, 100),
            TextAnchor.MiddleCenter, Color.white);

        CreateButton(panel.transform, "Devam Et", new Vector2(0, 20),
            () => { if (UIManager.Instance != null) UIManager.Instance.ResumeGame(); });

        CreateButton(panel.transform, "Yeniden Basla", new Vector2(0, -40),
            () => { if (UIManager.Instance != null) UIManager.Instance.RestartGame(); });

        CreateButton(panel.transform, "Ana Menu", new Vector2(0, -100),
            () => { if (UIManager.Instance != null) UIManager.Instance.GoToMainMenu(); });
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color? bgColor = null)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        if (bgColor.HasValue)
        {
            Image img = panel.AddComponent<Image>();
            img.color = bgColor.Value;
        }

        return panel;
    }

    TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Vector2 position, TextAnchor alignment, Color color)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return tmp;
    }

    void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject("Button");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(250, 50);

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.2f, 0.5f);

        Button btn = buttonObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.5f, 0.3f, 0.7f);
        colors.pressedColor = new Color(0.2f, 0.1f, 0.3f);
        btn.colors = colors;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }

    #endregion
}

// ========== YARDIMCI SCRIPTLER ==========

/// <summary>
/// Animasyonlu oyuncu kontrolcusu
/// </summary>
public class AnimatedPlayer : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] idleSprites;
    public Sprite[] runSprites;
    public Sprite[] jumpSprites;
    public Sprite[] fallSprites;
    public Sprite[] doubleJumpSprites;

    [Header("Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 14f;
    public float bounceForce = 12f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public float animFPS = 12f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool hasDoubleJumped;
    private bool isInvincible;
    private float invincibleTimer;

    private Sprite[] currentAnimation;
    private int currentFrame;
    private float animTimer;
    private bool isDoubleJumping;
    private int doubleJumpFrame;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentAnimation = idleSprites;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;

        HandleInvincibility();
        HandleInput();
        HandleJump();
        UpdateAnimation();
    }

    void HandleInvincibility()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            sr.enabled = Mathf.Sin(Time.time * 20f) > 0;
            if (invincibleTimer <= 0)
            {
                isInvincible = false;
                sr.enabled = true;
            }
        }
    }

    void HandleInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        horizontalInput = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontalInput = -1f;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontalInput = 1f;

        if (horizontalInput != 0) sr.flipX = horizontalInput < 0;
    }

    void HandleJump()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (isGrounded)
        {
            coyoteTimeCounter = 0.15f;
            hasDoubleJumped = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        bool jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;

        if (jumpPressed) jumpBufferCounter = 0.15f;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
        }
        else if (jumpPressed && !isGrounded && !hasDoubleJumped && CanDoubleJump())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.85f);
            hasDoubleJumped = true;
            isDoubleJumping = true;
            doubleJumpFrame = 0;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
        }
    }

    bool CanDoubleJump()
    {
        if (PowerUpManager.Instance != null) return PowerUpManager.Instance.TryDoubleJump();
        return false;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;

        wasGrounded = isGrounded;
        CheckGround();
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius) != null;

        Collider2D[] cols = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        isGrounded = false;
        foreach (var col in cols)
        {
            if (col.gameObject != gameObject && !col.CompareTag("Enemy") && !col.isTrigger)
            {
                isGrounded = true;
                break;
            }
        }
    }

    void UpdateAnimation()
    {
        Sprite[] targetAnim;

        if (isDoubleJumping && doubleJumpSprites != null && doubleJumpSprites.Length > 0)
        {
            targetAnim = doubleJumpSprites;
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFPS)
            {
                animTimer = 0;
                doubleJumpFrame++;
                if (doubleJumpFrame >= doubleJumpSprites.Length)
                {
                    isDoubleJumping = false;
                }
            }
            currentFrame = Mathf.Clamp(doubleJumpFrame, 0, doubleJumpSprites.Length - 1);
        }
        else if (!isGrounded)
        {
            targetAnim = rb.linearVelocity.y > 0.5f ? jumpSprites : fallSprites;
            currentFrame = 0;
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            targetAnim = runSprites;
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFPS)
            {
                animTimer = 0;
                currentFrame = (currentFrame + 1) % runSprites.Length;
            }
        }
        else
        {
            targetAnim = idleSprites;
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / (animFPS * 0.5f))
            {
                animTimer = 0;
                currentFrame = (currentFrame + 1) % idleSprites.Length;
            }
        }

        if (targetAnim != currentAnimation)
        {
            currentAnimation = targetAnim;
            if (!isDoubleJumping) currentFrame = 0;
        }

        if (currentAnimation != null && currentAnimation.Length > 0)
        {
            sr.sprite = currentAnimation[Mathf.Clamp(currentFrame, 0, currentAnimation.Length - 1)];
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            float playerBottom = transform.position.y - 0.4f;
            float enemyTop = collision.transform.position.y + 0.3f;

            if (playerBottom > enemyTop && rb.linearVelocity.y < 0)
            {
                Vector3 pos = collision.transform.position;

                var ae = collision.gameObject.GetComponent<AnimatedEnemy>();
                if (ae != null) ae.Die();
                else Destroy(collision.gameObject);

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                if (GameManager.Instance != null) GameManager.Instance.EnemyKilled(pos);
            }
            else if (!isInvincible)
            {
                TakeDamage();
            }
        }
    }

    public void TakeDamage()
    {
        if (isInvincible) return;

        if (PowerUpManager.Instance != null && PowerUpManager.Instance.TryAbsorbDamage())
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHurt();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(1);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHurt();

            if (!GameManager.Instance.IsGameOver())
            {
                isInvincible = true;
                invincibleTimer = 2f;
                rb.linearVelocity = new Vector2(-horizontalInput * 5f, 8f);
            }
        }
    }
}

/// <summary>
/// Animasyonlu dusman
/// </summary>
public class AnimatedEnemy : MonoBehaviour
{
    public Sprite[] idleSprites;
    public Sprite[] runSprites;
    public float moveSpeed = 2f;
    public bool jumps = false;
    public float jumpForce = 8f;
    public float jumpInterval = 2f;
    public bool chasesPlayer = false;
    public float chaseRange = 8f;
    public bool isFlying = false;
    public bool floats = false;
    public float animFPS = 10f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool moveRight = false;
    private float jumpTimer;
    private Vector3 startPos;
    private Transform player;
    private bool isDead = false;

    private Sprite[] currentAnimation;
    private int currentFrame;
    private float animTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        jumpTimer = jumpInterval;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentAnimation = idleSprites;
    }

    void Update()
    {
        if (isDead) return;

        UpdateBehavior();
        UpdateAnimation();
    }

    void UpdateBehavior()
    {
        if (floats)
        {
            // Havada suzulme
            float newY = startPos.y + Mathf.Sin(Time.time * 2f) * 1f;
            transform.position = new Vector3(transform.position.x, newY, 0);
        }

        if (chasesPlayer && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < chaseRange)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                if (isFlying)
                {
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, dir * moveSpeed, Time.deltaTime * 3f);
                }
                else
                {
                    rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
                }
                sr.flipX = dir.x < 0;
                return;
            }
        }

        // Patrol
        float direction = moveRight ? 1f : -1f;
        if (!isFlying)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else if (!floats)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, 0);
        }
        sr.flipX = !moveRight;

        // Patrol distance
        if (Mathf.Abs(transform.position.x - startPos.x) > 5f)
        {
            moveRight = transform.position.x < startPos.x;
        }

        // Jumping
        if (jumps && !isFlying)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0 && IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpTimer = jumpInterval;
            }
        }
    }

    bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.6f).collider != null;
    }

    void UpdateAnimation()
    {
        Sprite[] targetAnim = (Mathf.Abs(rb.linearVelocity.x) > 0.1f || isFlying) ? runSprites : idleSprites;

        if (targetAnim != currentAnimation)
        {
            currentAnimation = targetAnim;
            currentFrame = 0;
        }

        if (currentAnimation != null && currentAnimation.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFPS)
            {
                animTimer = 0;
                currentFrame = (currentFrame + 1) % currentAnimation.Length;
            }
            sr.sprite = currentAnimation[currentFrame];
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        transform.localScale = new Vector3(transform.localScale.x, 0.3f, 1);
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.3f);
    }
}

/// <summary>
/// Animasyonlu testere
/// </summary>
public class AnimatedSaw : MonoBehaviour
{
    public Sprite[] sprites;
    public float animFPS = 15f;

    private SpriteRenderer sr;
    private int currentFrame;
    private float animTimer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= 1f / animFPS)
        {
            animTimer = 0;
            currentFrame = (currentFrame + 1) % sprites.Length;
            sr.sprite = sprites[currentFrame];
        }

        // Also rotate visually
        transform.Rotate(0, 0, 360 * Time.deltaTime);
    }
}

/// <summary>
/// Yuzen item efekti
/// </summary>
public class FloatingItem : MonoBehaviour
{
    private Vector3 startPos;
    private float offset;

    void Start()
    {
        startPos = transform.position;
        offset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * 2f + offset) * 0.2f;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
