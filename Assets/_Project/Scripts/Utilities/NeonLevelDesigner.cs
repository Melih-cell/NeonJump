using UnityEngine;
using System.Collections.Generic;

public class NeonLevelDesigner : MonoBehaviour
{
    [Header("Level Theme")]
    public Color primaryNeonColor = new Color(0f, 1f, 1f); // Cyan
    public Color secondaryNeonColor = new Color(1f, 0f, 1f); // Magenta
    public Color accentColor = new Color(1f, 1f, 0f); // Yellow

    private Sprite whiteSprite;
    private Transform levelContainer;
    private Transform backgroundContainer;

    void Awake()
    {
        CreateWhiteSprite();
    }

    void Start()
    {
        // Eger editorde zaten olusturulduysa tekrar olusturma
        if (GameObject.Find("LevelContainer") != null) return;

        GenerateLevelInEditor();
    }

    // Editor'dan cagrilabilir
    public void GenerateLevelInEditor()
    {
        CreateWhiteSprite();

        // Container'lar olustur
        levelContainer = new GameObject("LevelContainer").transform;
        backgroundContainer = new GameObject("BackgroundContainer").transform;

        // Sahneyi olustur
        CreateNeonBackground();
        CreateLevel();

        Debug.Log("Neon Level Created!");
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

    #region BACKGROUND

    void CreateNeonBackground()
    {
        // Gradient arka plan
        CreateGradientBackground();

        // Yildizlar
        CreateStars(100);

        // Uzak sehir silueti
        CreateCitySilhouette(-5f, 0.1f, new Color(0.1f, 0.05f, 0.2f));

        // Yakin sehir silueti
        CreateCitySilhouette(-3f, 0.2f, new Color(0.15f, 0.08f, 0.25f));

        // Neon cizgiler (grid)
        CreateNeonGrid();

        // Dekoratif neon tabelalar
        CreateNeonSigns();
    }

    void CreateGradientBackground()
    {
        // Ust kisim - koyu mor
        GameObject bgTop = new GameObject("BG_Top");
        bgTop.transform.SetParent(backgroundContainer);
        bgTop.transform.position = new Vector3(100, 15, 10);
        bgTop.transform.localScale = new Vector3(300, 20, 1);

        SpriteRenderer srTop = bgTop.AddComponent<SpriteRenderer>();
        srTop.sprite = whiteSprite;
        srTop.color = new Color(0.02f, 0.01f, 0.08f);
        srTop.sortingOrder = -100;

        // Alt kisim - biraz daha acik
        GameObject bgBottom = new GameObject("BG_Bottom");
        bgBottom.transform.SetParent(backgroundContainer);
        bgBottom.transform.position = new Vector3(100, -5, 10);
        bgBottom.transform.localScale = new Vector3(300, 20, 1);

        SpriteRenderer srBottom = bgBottom.AddComponent<SpriteRenderer>();
        srBottom.sprite = whiteSprite;
        srBottom.color = new Color(0.05f, 0.02f, 0.12f);
        srBottom.sortingOrder = -100;
    }

    void CreateStars(int count)
    {
        GameObject starsContainer = new GameObject("Stars");
        starsContainer.transform.SetParent(backgroundContainer);

        for (int i = 0; i < count; i++)
        {
            GameObject star = new GameObject("Star_" + i);
            star.transform.SetParent(starsContainer.transform);

            float x = Random.Range(-20f, 250f);
            float y = Random.Range(5f, 25f);
            star.transform.position = new Vector3(x, y, 5);

            float size = Random.Range(0.05f, 0.2f);
            star.transform.localScale = new Vector3(size, size, 1);

            SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;

            // Rastgele yildiz rengi
            float brightness = Random.Range(0.5f, 1f);
            if (Random.value > 0.8f)
                sr.color = new Color(brightness, brightness * 0.8f, brightness * 0.9f); // Hafif mavi
            else
                sr.color = new Color(brightness, brightness, brightness);

            sr.sortingOrder = -90;

            // Bazi yildizlar parlak
            if (Random.value > 0.9f)
            {
                star.AddComponent<StarTwinkle>();
            }
        }
    }

    void CreateCitySilhouette(float depth, float parallaxFactor, Color color)
    {
        GameObject cityContainer = new GameObject("City_" + depth);
        cityContainer.transform.SetParent(backgroundContainer);

        float x = -10f;
        while (x < 280f)
        {
            // Bina
            float buildingWidth = Random.Range(2f, 6f);
            float buildingHeight = Random.Range(3f, 12f);

            GameObject building = new GameObject("Building");
            building.transform.SetParent(cityContainer.transform);
            building.transform.position = new Vector3(x + buildingWidth / 2f, depth + buildingHeight / 2f, 5 - depth);
            building.transform.localScale = new Vector3(buildingWidth, buildingHeight, 1);

            SpriteRenderer sr = building.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            sr.color = color;
            sr.sortingOrder = -80 + (int)(depth * 10);

            // Bazi binalara neon pencere
            if (Random.value > 0.6f)
            {
                CreateBuildingWindows(building.transform, buildingWidth, buildingHeight, depth);
            }

            x += buildingWidth + Random.Range(0.5f, 2f);
        }
    }

    void CreateBuildingWindows(Transform building, float width, float height, float depth)
    {
        int windowRows = (int)(height / 1.5f);
        int windowCols = (int)(width / 1.2f);

        Color[] windowColors = new Color[]
        {
            new Color(0f, 1f, 1f, 0.6f),   // Cyan
            new Color(1f, 0f, 1f, 0.6f),   // Magenta
            new Color(1f, 1f, 0f, 0.6f),   // Yellow
            new Color(1f, 0.5f, 0f, 0.6f), // Orange
        };

        for (int row = 0; row < windowRows; row++)
        {
            for (int col = 0; col < windowCols; col++)
            {
                if (Random.value > 0.5f) continue; // Rastgele pencere

                GameObject window = new GameObject("Window");
                window.transform.SetParent(building);

                float localX = (col - windowCols / 2f + 0.5f) * (1f / width) * 0.8f;
                float localY = (row - windowRows / 2f + 0.5f) * (1f / height) * 0.8f;

                window.transform.localPosition = new Vector3(localX, localY, -0.1f);
                window.transform.localScale = new Vector3(0.15f / width, 0.2f / height, 1);

                SpriteRenderer sr = window.AddComponent<SpriteRenderer>();
                sr.sprite = whiteSprite;
                sr.color = windowColors[Random.Range(0, windowColors.Length)];
                sr.sortingOrder = -79 + (int)(depth * 10);
            }
        }
    }

    void CreateNeonGrid()
    {
        GameObject gridContainer = new GameObject("NeonGrid");
        gridContainer.transform.SetParent(backgroundContainer);

        // Yatay cizgiler
        for (int i = 0; i < 5; i++)
        {
            float y = -8f + i * 0.5f;
            float perspectiveScale = 1f - (i * 0.15f);

            GameObject line = new GameObject("GridLineH_" + i);
            line.transform.SetParent(gridContainer.transform);
            line.transform.position = new Vector3(100, y, 3);
            line.transform.localScale = new Vector3(250, 0.03f * perspectiveScale, 1);

            SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            sr.color = new Color(1f, 0f, 1f, 0.3f * perspectiveScale); // Magenta
            sr.sortingOrder = -70;
        }

        // Dikey cizgiler (perspektif)
        for (float x = -20f; x < 280f; x += 10f)
        {
            GameObject line = new GameObject("GridLineV");
            line.transform.SetParent(gridContainer.transform);
            line.transform.position = new Vector3(x, -6.5f, 3);
            line.transform.localScale = new Vector3(0.02f, 3f, 1);

            SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            sr.color = new Color(1f, 0f, 1f, 0.2f);
            sr.sortingOrder = -70;
        }
    }

    void CreateNeonSigns()
    {
        // Dekoratif neon tabelalar arka planda
        CreateNeonSign(15f, 8f, "decorative", primaryNeonColor);
        CreateNeonSign(60f, 10f, "decorative", secondaryNeonColor);
        CreateNeonSign(120f, 9f, "decorative", accentColor);
        CreateNeonSign(180f, 11f, "decorative", primaryNeonColor);
    }

    void CreateNeonSign(float x, float y, string type, Color color)
    {
        GameObject sign = new GameObject("NeonSign");
        sign.transform.SetParent(backgroundContainer);
        sign.transform.position = new Vector3(x, y, 2);

        // Ana sekil
        float width = Random.Range(3f, 6f);
        float height = Random.Range(1.5f, 3f);
        sign.transform.localScale = new Vector3(width, height, 1);

        SpriteRenderer sr = sign.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.8f); // Koyu arka plan
        sr.sortingOrder = -60;

        // Neon kenar
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(sign.transform);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = new Vector3(1.05f, 1.1f, 1);

        SpriteRenderer srOutline = outline.AddComponent<SpriteRenderer>();
        srOutline.sprite = whiteSprite;
        srOutline.color = new Color(color.r, color.g, color.b, 0.6f);
        srOutline.sortingOrder = -61;

        // Glow efekti
        sign.AddComponent<NeonGlow>();
    }

    #endregion

    #region LEVEL DESIGN

    void CreateLevel()
    {
        // ===== BOLUM 1: BASLANGIC (x: 0-50) =====
        CreateSection1_Tutorial();

        // ===== BOLUM 2: ORMAN/PARK (x: 50-120) =====
        CreateSection2_NeonForest();

        // ===== BOLUM 3: SEHIR (x: 120-200) =====
        CreateSection3_NeonCity();

        // ===== BOLUM 4: BOSS ARENA (x: 200-250) =====
        CreateSection4_BossArena();
    }

    void CreateSection1_Tutorial()
    {
        Color sectionColor = new Color(0.2f, 0.1f, 0.4f); // Koyu mor
        Color platformColor = new Color(0.4f, 0.2f, 0.6f); // Acik mor

        // Zemin bloklari
        CreateGround(0, 15, sectionColor);
        CreateGround(18, 35, sectionColor);
        CreateGround(38, 50, sectionColor);

        // Basit platformlar - ogrenme
        CreatePlatform(8, 2, 4, platformColor);
        CreatePlatform(14, 4, 3, platformColor);
        CreatePlatform(22, 2, 5, platformColor);
        CreatePlatform(30, 4, 4, platformColor);
        CreatePlatform(42, 3, 4, platformColor);

        // Kolay dusmanlar
        CreateEnemy(12, 1, EnemyType.Basic);
        CreateEnemy(28, 1, EnemyType.Basic);
        CreateEnemy(45, 1, EnemyType.Basic);

        // Coinler - yolu gosterir
        CreateCoinArc(5, 2, 5, 3f);
        CreateCoinRow(18, 2, 6);
        CreateCoinRow(23, 5, 4);
        CreateCoinArc(38, 2, 5, 2.5f);

        // Ilk power-up
        CreatePowerUp(32, 5, PowerUpType.DoubleJump);

        // Ilk checkpoint
        CreateCheckpoint(48, 1);

        // Dekorasyon
        CreateNeonDecoration(5, 6, primaryNeonColor);
        CreateNeonDecoration(25, 7, secondaryNeonColor);
    }

    void CreateSection2_NeonForest()
    {
        Color sectionColor = new Color(0.1f, 0.2f, 0.15f); // Koyu yesil
        Color platformColor = new Color(0.2f, 0.5f, 0.3f); // Neon yesil

        // Zemin - bosluklar var
        CreateGround(50, 65, sectionColor);
        CreateGround(70, 85, sectionColor);
        CreateGround(90, 105, sectionColor);
        CreateGround(110, 120, sectionColor);

        // Daha karmasik platformlar
        CreatePlatform(55, 3, 4, platformColor);
        CreatePlatform(62, 5, 3, platformColor);
        CreatePlatform(66, 3, 3, platformColor); // Bosluk uzerinde
        CreatePlatform(75, 4, 5, platformColor);
        CreatePlatform(83, 6, 3, platformColor);
        CreatePlatform(88, 3, 3, platformColor); // Bosluk uzerinde
        CreatePlatform(95, 5, 4, platformColor);
        CreatePlatform(102, 7, 3, platformColor);
        CreatePlatform(108, 4, 3, platformColor); // Bosluk uzerinde
        CreatePlatform(115, 6, 4, platformColor);

        // Hareketli platformlar
        CreateMovingPlatform(68, 2, MovingPlatform.MoveType.Vertical, 4f);
        CreateMovingPlatform(87, 4, MovingPlatform.MoveType.Horizontal, 5f);
        CreateMovingPlatform(106, 3, MovingPlatform.MoveType.Vertical, 3f);

        // Cesitli dusmanlar
        CreateEnemy(58, 1, EnemyType.Basic);
        CreateEnemy(78, 1, EnemyType.Jumping);
        CreateEnemy(83, 7, EnemyType.Basic); // Platform uzerinde
        CreateEnemy(98, 1, EnemyType.Basic);
        CreateEnemy(112, 1, EnemyType.Jumping);
        CreateFlyingEnemy(72, 5, FlyingEnemy.FlyPattern.Vertical);
        CreateFlyingEnemy(100, 6, FlyingEnemy.FlyPattern.Horizontal);

        // Coinler
        CreateCoinRow(52, 4, 5);
        CreateCoinArc(65, 4, 4, 2f);
        CreateCoinRow(76, 5, 6);
        CreateCoinArc(92, 3, 5, 3f);
        CreateCoinRow(113, 7, 4);

        // Power-up'lar
        CreatePowerUp(60, 6, PowerUpType.SpeedBoost);
        CreatePowerUp(95, 8, PowerUpType.Shield);

        // Checkpoint
        CreateCheckpoint(118, 1);

        // Dekorasyon - neon agaclar
        CreateNeonTree(53, 1);
        CreateNeonTree(80, 1);
        CreateNeonTree(97, 1);
        CreateNeonTree(116, 1);
    }

    void CreateSection3_NeonCity()
    {
        Color sectionColor = new Color(0.15f, 0.1f, 0.25f); // Koyu mavi-mor
        Color platformColor = new Color(0.3f, 0.2f, 0.5f); // Neon mor

        // Zemin
        CreateGround(120, 140, sectionColor);
        CreateGround(145, 165, sectionColor);
        CreateGround(170, 190, sectionColor);
        CreateGround(195, 200, sectionColor);

        // Daha zor platform dizilimi
        CreatePlatform(125, 3, 4, platformColor);
        CreatePlatform(132, 5, 3, platformColor);
        CreatePlatform(138, 7, 3, platformColor);
        CreatePlatform(143, 4, 3, platformColor); // Bosluk uzerinde
        CreatePlatform(150, 6, 4, platformColor);
        CreatePlatform(158, 8, 3, platformColor);
        CreatePlatform(165, 5, 3, platformColor); // Bosluk uzerinde
        CreatePlatform(168, 3, 3, platformColor); // Bosluk uzerinde
        CreatePlatform(175, 6, 4, platformColor);
        CreatePlatform(183, 8, 3, platformColor);
        CreatePlatform(190, 5, 4, platformColor);

        // Hareketli platformlar
        CreateMovingPlatform(142, 2, MovingPlatform.MoveType.Horizontal, 4f);
        CreateMovingPlatform(163, 4, MovingPlatform.MoveType.Circular, 3f);
        CreateMovingPlatform(192, 3, MovingPlatform.MoveType.Vertical, 5f);

        // Zor dusmanlar
        CreateEnemy(128, 1, EnemyType.Basic);
        CreateEnemy(138, 8, EnemyType.Basic); // Yuksek platform
        CreateEnemy(153, 1, EnemyType.Jumping);
        CreateEnemy(158, 9, EnemyType.Shooting); // Ates eden
        CreateEnemy(178, 1, EnemyType.Basic);
        CreateEnemy(188, 1, EnemyType.Jumping);
        CreateFlyingEnemy(135, 6, FlyingEnemy.FlyPattern.Chase);
        CreateFlyingEnemy(160, 7, FlyingEnemy.FlyPattern.Horizontal);
        CreateFlyingEnemy(185, 6, FlyingEnemy.FlyPattern.Circular);
        CreateShootingEnemy(170, 7);

        // Coinler
        CreateCoinRow(123, 4, 5);
        CreateCoinArc(140, 5, 4, 3f);
        CreateCoinRow(152, 7, 5);
        CreateCoinArc(172, 4, 5, 2.5f);
        CreateCoinRow(182, 9, 4);

        // Power-up'lar
        CreatePowerUp(148, 7, PowerUpType.Magnet);
        CreatePowerUp(180, 9, PowerUpType.Invincibility);

        // Checkpoint
        CreateCheckpoint(198, 1);

        // Dekorasyon - neon tabelalar
        CreateNeonDecoration(130, 10, primaryNeonColor);
        CreateNeonDecoration(155, 11, secondaryNeonColor);
        CreateNeonDecoration(185, 10, accentColor);
    }

    void CreateSection4_BossArena()
    {
        Color arenaColor = new Color(0.2f, 0.05f, 0.1f); // Koyu kirmizi
        Color platformColor = new Color(0.5f, 0.1f, 0.2f); // Neon kirmizi

        // Boss arena zemini
        CreateGround(200, 250, arenaColor);

        // Arena duvarlari
        CreateArenaWall(200, 0, 15);
        CreateArenaWall(250, 0, 15);

        // Arena platformlari
        CreatePlatform(210, 4, 5, platformColor);
        CreatePlatform(235, 4, 5, platformColor);
        CreatePlatform(220, 7, 6, platformColor);

        // Boss trigger
        CreateBossTrigger(205, 2);

        // Son coinler
        CreateCoinRow(212, 5, 4);
        CreateCoinRow(237, 5, 4);
        CreateCoinArc(222, 8, 5, 2f);

        // Bitis noktasi (boss olunce acilacak)
        CreateGoal(255, 1);

        // Arena dekorasyonu
        CreateNeonDecoration(205, 12, new Color(1f, 0.2f, 0.2f));
        CreateNeonDecoration(225, 13, new Color(1f, 0.2f, 0.2f));
        CreateNeonDecoration(245, 12, new Color(1f, 0.2f, 0.2f));
    }

    #endregion

    #region CREATION HELPERS

    void CreateGround(float startX, float endX, Color color)
    {
        float width = endX - startX;
        float centerX = startX + width / 2f;

        GameObject ground = new GameObject("Ground_" + startX);
        ground.transform.SetParent(levelContainer);
        ground.transform.position = new Vector3(centerX, -0.5f, 0);
        ground.transform.localScale = new Vector3(width, 3f, 1);

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;
        sr.sortingOrder = 0;

        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();

        // Ust kenar - neon cizgi
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(ground.transform);
        topLine.transform.localPosition = new Vector3(0, 0.52f, 0);
        topLine.transform.localScale = new Vector3(1, 0.02f, 1);

        SpriteRenderer srLine = topLine.AddComponent<SpriteRenderer>();
        srLine.sprite = whiteSprite;
        srLine.color = new Color(color.r + 0.3f, color.g + 0.3f, color.b + 0.3f);
        srLine.sortingOrder = 1;
    }

    void CreatePlatform(float x, float y, float width, Color color)
    {
        float centerX = x + width / 2f;

        GameObject platform = new GameObject("Platform_" + x);
        platform.transform.SetParent(levelContainer);
        platform.transform.position = new Vector3(centerX, y + 0.25f, 0);
        platform.transform.localScale = new Vector3(width, 0.5f, 1);

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;
        sr.sortingOrder = 1;

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();

        // Neon glow
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(platform.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = new Vector3(1.1f, 1.3f, 1);

        SpriteRenderer srGlow = glow.AddComponent<SpriteRenderer>();
        srGlow.sprite = whiteSprite;
        srGlow.color = new Color(color.r, color.g, color.b, 0.3f);
        srGlow.sortingOrder = 0;
    }

    void CreateMovingPlatform(float x, float y, MovingPlatform.MoveType moveType, float distance)
    {
        GameObject platform = new GameObject("MovingPlatform_" + x);
        platform.transform.SetParent(levelContainer);
        platform.transform.position = new Vector3(x, y, 0);
        platform.transform.localScale = new Vector3(3f, 0.5f, 1);

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.3f, 0.5f, 0.7f); // Mavi
        sr.sortingOrder = 2;

        Rigidbody2D rb = platform.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();

        MovingPlatform mp = platform.AddComponent<MovingPlatform>();
        mp.moveType = moveType;
        mp.moveDistance = distance;
        mp.moveSpeed = 2f;
        mp.circleRadius = distance * 0.5f;
        mp.waitTime = 0.3f;

        // Neon kenar
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(platform.transform);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = new Vector3(1.05f, 1.2f, 1);

        SpriteRenderer srOutline = outline.AddComponent<SpriteRenderer>();
        srOutline.sprite = whiteSprite;
        srOutline.color = new Color(0.5f, 0.8f, 1f, 0.5f);
        srOutline.sortingOrder = 1;
    }

    enum EnemyType { Basic, Jumping, Shooting }

    void CreateEnemy(float x, float y, EnemyType type)
    {
        GameObject enemy = new GameObject("Enemy_" + type);
        enemy.transform.SetParent(levelContainer);
        enemy.transform.position = new Vector3(x, y + 0.5f, 0);
        enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        switch (type)
        {
            case EnemyType.Basic:
                sr.color = new Color(0.8f, 0.2f, 0.2f); // Kirmizi
                Enemy e = enemy.AddComponent<Enemy>();
                e.moveSpeed = 2f;
                break;

            case EnemyType.Jumping:
                sr.color = new Color(0.2f, 0.8f, 0.2f); // Yesil
                JumpingEnemy je = enemy.AddComponent<JumpingEnemy>();
                je.jumpForce = 12f;
                je.jumpInterval = 2f;
                je.chasePlayer = true;
                je.chaseRange = 6f;
                break;

            case EnemyType.Shooting:
                sr.color = new Color(1f, 0.5f, 0f); // Turuncu
                enemy.transform.localScale = new Vector3(0.9f, 0.9f, 1);
                ShootingEnemy se = enemy.AddComponent<ShootingEnemy>();
                se.shootInterval = 2.5f;
                se.projectileSpeed = 6f;
                se.detectionRange = 10f;
                break;
        }
    }

    void CreateFlyingEnemy(float x, float y, FlyingEnemy.FlyPattern pattern)
    {
        GameObject enemy = new GameObject("FlyingEnemy");
        enemy.transform.SetParent(levelContainer);
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.transform.localScale = new Vector3(0.7f, 0.7f, 1);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.6f, 0.2f, 0.8f); // Mor
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        FlyingEnemy fe = enemy.AddComponent<FlyingEnemy>();
        fe.pattern = pattern;
        fe.moveSpeed = 3f;
        fe.moveDistance = 3f;
        fe.circleRadius = 2f;
        fe.chaseRange = 6f;
    }

    void CreateShootingEnemy(float x, float y)
    {
        CreateEnemy(x, y, EnemyType.Shooting);
    }

    void CreateCoinRow(float startX, float y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateCoin(startX + i * 1.2f, y);
        }
    }

    void CreateCoinArc(float centerX, float y, int count, float arcHeight)
    {
        float spacing = 1.2f;
        float startX = centerX - (count - 1) * spacing / 2f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float arcY = y + Mathf.Sin(t * Mathf.PI) * arcHeight;
            CreateCoin(startX + i * spacing, arcY);
        }
    }

    void CreateCoin(float x, float y)
    {
        GameObject coin = new GameObject("Coin");
        coin.transform.SetParent(levelContainer);
        coin.transform.position = new Vector3(x, y, 0);
        coin.transform.localScale = new Vector3(0.5f, 0.5f, 1);

        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(1f, 0.85f, 0f); // Altin
        sr.sortingOrder = 5;

        CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        coin.AddComponent<Coin>();
    }

    void CreatePowerUp(float x, float y, PowerUpType type)
    {
        GameObject powerUp = new GameObject("PowerUp_" + type);
        powerUp.transform.SetParent(levelContainer);
        powerUp.transform.position = new Vector3(x, y, 0);

        PowerUp pu = powerUp.AddComponent<PowerUp>();
        pu.powerUpType = type;

        switch (type)
        {
            case PowerUpType.SpeedBoost: pu.duration = 5f; break;
            case PowerUpType.DoubleJump: pu.duration = 10f; break;
            case PowerUpType.Shield: pu.duration = 999f; break;
            case PowerUpType.Magnet: pu.duration = 8f; break;
            case PowerUpType.Invincibility: pu.duration = 5f; break;
        }
    }

    void CreateCheckpoint(float x, float y)
    {
        GameObject checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.SetParent(levelContainer);
        checkpoint.transform.position = new Vector3(x, y + 1.5f, 0);
        checkpoint.AddComponent<Checkpoint>();
    }

    void CreateArenaWall(float x, float y, float height)
    {
        GameObject wall = new GameObject("ArenaWall");
        wall.transform.SetParent(levelContainer);
        wall.transform.position = new Vector3(x, y + height / 2f, 0);
        wall.transform.localScale = new Vector3(1f, height, 1);

        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.4f, 0.1f, 0.1f);
        sr.sortingOrder = 1;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();

        // Neon kenar
        GameObject neonLine = new GameObject("NeonLine");
        neonLine.transform.SetParent(wall.transform);
        neonLine.transform.localPosition = new Vector3(0.52f, 0, 0);
        neonLine.transform.localScale = new Vector3(0.02f, 1, 1);

        SpriteRenderer srNeon = neonLine.AddComponent<SpriteRenderer>();
        srNeon.sprite = whiteSprite;
        srNeon.color = new Color(1f, 0.2f, 0.2f);
        srNeon.sortingOrder = 2;
    }

    void CreateBossTrigger(float x, float y)
    {
        GameObject trigger = new GameObject("BossTrigger");
        trigger.transform.SetParent(levelContainer);
        trigger.transform.position = new Vector3(x, y, 0);

        BoxCollider2D col = trigger.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3f, 5f);
        col.isTrigger = true;

        BossTrigger bt = trigger.AddComponent<BossTrigger>();
        bt.bossSpawnPosition = new Vector3(x + 20, 8, 0);
        bt.arenaMinX = 200;
        bt.arenaMaxX = 250;
    }

    void CreateGoal(float x, float y)
    {
        GameObject goal = new GameObject("Goal");
        goal.transform.SetParent(levelContainer);
        goal.transform.position = new Vector3(x, y + 2f, 0);
        goal.transform.localScale = new Vector3(1f, 4f, 1);

        SpriteRenderer sr = goal.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(0.2f, 1f, 0.3f); // Yesil
        sr.sortingOrder = 5;

        BoxCollider2D col = goal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        goal.AddComponent<Goal>();
    }

    #endregion

    #region DECORATIONS

    void CreateNeonDecoration(float x, float y, Color color)
    {
        GameObject deco = new GameObject("NeonDeco");
        deco.transform.SetParent(levelContainer);
        deco.transform.position = new Vector3(x, y, 1);

        // Rastgele sekil
        float width = Random.Range(1f, 3f);
        float height = Random.Range(0.5f, 1.5f);
        deco.transform.localScale = new Vector3(width, height, 1);

        SpriteRenderer sr = deco.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.8f);
        sr.sortingOrder = -10;

        // Neon outline
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(deco.transform);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = new Vector3(1.1f, 1.2f, 1);

        SpriteRenderer srOutline = outline.AddComponent<SpriteRenderer>();
        srOutline.sprite = whiteSprite;
        srOutline.color = new Color(color.r, color.g, color.b, 0.6f);
        srOutline.sortingOrder = -11;

        deco.AddComponent<NeonGlow>();
    }

    void CreateNeonTree(float x, float y)
    {
        GameObject tree = new GameObject("NeonTree");
        tree.transform.SetParent(levelContainer);
        tree.transform.position = new Vector3(x, y, 1);

        // Govde
        GameObject trunk = new GameObject("Trunk");
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
        trunk.transform.localScale = new Vector3(0.3f, 3f, 1);

        SpriteRenderer srTrunk = trunk.AddComponent<SpriteRenderer>();
        srTrunk.sprite = whiteSprite;
        srTrunk.color = new Color(0.3f, 0.15f, 0.1f);
        srTrunk.sortingOrder = -5;

        // Yapraklar (neon)
        Color leafColor = new Color(0.2f, 0.8f, 0.4f);
        CreateTreeLeaves(tree.transform, 0, 3.5f, 2f, leafColor);
        CreateTreeLeaves(tree.transform, 0, 4.5f, 1.5f, leafColor);
        CreateTreeLeaves(tree.transform, 0, 5.3f, 1f, leafColor);
    }

    void CreateTreeLeaves(Transform parent, float x, float y, float size, Color color)
    {
        GameObject leaves = new GameObject("Leaves");
        leaves.transform.SetParent(parent);
        leaves.transform.localPosition = new Vector3(x, y, 0);
        leaves.transform.localScale = new Vector3(size, size * 0.6f, 1);

        SpriteRenderer sr = leaves.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;
        sr.sortingOrder = -4;

        // Glow
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(leaves.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = new Vector3(1.2f, 1.3f, 1);

        SpriteRenderer srGlow = glow.AddComponent<SpriteRenderer>();
        srGlow.sprite = whiteSprite;
        srGlow.color = new Color(color.r, color.g, color.b, 0.3f);
        srGlow.sortingOrder = -5;
    }

    #endregion
}

// Yildiz titresmesi efekti
public class StarTwinkle : MonoBehaviour
{
    private SpriteRenderer sr;
    private float baseAlpha;
    private float speed;
    private float offset;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            baseAlpha = sr.color.a;
        }
        speed = Random.Range(2f, 5f);
        offset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (sr == null) return;

        float alpha = baseAlpha * (0.5f + 0.5f * Mathf.Sin(Time.time * speed + offset));
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}

// Neon parlama efekti
public class NeonGlow : MonoBehaviour
{
    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float speed;
    private float offset;

    void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            baseColors[i] = renderers[i].color;
        }

        speed = Random.Range(1f, 3f);
        offset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * speed + offset);

        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = baseColors[i];
            c.a = c.a * pulse;
            renderers[i].color = c;
        }
    }
}
