using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefabs - Assign in Inspector")]
    public GameObject platformPrefab;
    public GameObject enemyPrefab;
    public GameObject coinPrefab;
    public GameObject goalPrefab;

    [Header("Level Settings")]
    public float levelLength = 200f;
    public float groundY = -2f;

    [Header("Platform Settings")]
    public float minPlatformGap = 3f;
    public float maxPlatformGap = 6f;
    public float minPlatformY = -1f;
    public float maxPlatformY = 4f;

    [Header("Sprites")]
    public Sprite groundSprite;
    public Sprite enemySprite;
    public Sprite coinSprite;

    private List<GameObject> generatedObjects = new List<GameObject>();

    void Start()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        // Onceki leveli temizle
        foreach (var obj in generatedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        generatedObjects.Clear();

        // Ana zemin
        CreateGround(0, groundY, 15f);
        CreateGround(20f, groundY, 20f);
        CreateGround(50f, groundY, 15f);
        CreateGround(75f, groundY, 25f);
        CreateGround(110f, groundY, 20f);
        CreateGround(140f, groundY, 30f);
        CreateGround(180f, groundY, 25f);

        // Platformlar
        CreatePlatform(8f, 0f, 3f);
        CreatePlatform(14f, 2f, 3f);
        CreatePlatform(22f, 1f, 4f);
        CreatePlatform(30f, 3f, 3f);
        CreatePlatform(38f, 0f, 5f);
        CreatePlatform(42f, 2f, 3f);
        CreatePlatform(48f, 4f, 3f);

        CreatePlatform(58f, 1f, 4f);
        CreatePlatform(65f, 3f, 3f);
        CreatePlatform(70f, 0f, 3f);

        CreatePlatform(85f, 2f, 4f);
        CreatePlatform(92f, 4f, 3f);
        CreatePlatform(98f, 1f, 5f);
        CreatePlatform(105f, 3f, 3f);

        CreatePlatform(118f, 0f, 4f);
        CreatePlatform(125f, 2f, 3f);
        CreatePlatform(132f, 4f, 4f);
        CreatePlatform(138f, 1f, 3f);

        CreatePlatform(155f, 2f, 5f);
        CreatePlatform(165f, 4f, 3f);
        CreatePlatform(172f, 1f, 4f);

        CreatePlatform(190f, 2f, 4f);
        CreatePlatform(198f, 4f, 3f);

        // Dusmanlar
        CreateEnemy(12f, groundY + 1.5f);
        CreateEnemy(25f, groundY + 1.5f);
        CreateEnemy(35f, groundY + 1.5f);
        CreateEnemy(55f, groundY + 1.5f);
        CreateEnemy(68f, groundY + 1.5f);
        CreateEnemy(80f, groundY + 1.5f);
        CreateEnemy(95f, groundY + 1.5f);
        CreateEnemy(115f, groundY + 1.5f);
        CreateEnemy(130f, groundY + 1.5f);
        CreateEnemy(150f, groundY + 1.5f);
        CreateEnemy(175f, groundY + 1.5f);
        CreateEnemy(188f, groundY + 1.5f);

        // Platform uzerinde dusmanlar
        CreateEnemy(30f, 4.5f);
        CreateEnemy(92f, 5.5f);
        CreateEnemy(165f, 5.5f);

        // Coinler
        CreateCoinRow(5f, 1f, 5);
        CreateCoinRow(18f, 3f, 4);
        CreateCoinRow(32f, 5f, 3);
        CreateCoinRow(45f, 2f, 5);
        CreateCoinRow(60f, 3f, 4);
        CreateCoinRow(78f, 1f, 6);
        CreateCoinRow(88f, 4f, 4);
        CreateCoinRow(102f, 2f, 5);
        CreateCoinRow(120f, 1f, 4);
        CreateCoinRow(135f, 5f, 3);
        CreateCoinRow(158f, 4f, 5);
        CreateCoinRow(178f, 2f, 4);
        CreateCoinRow(195f, 3f, 3);

        // Bitis noktasi (Bayrak)
        CreateGoal(205f, groundY + 1f);
    }

    void CreateGround(float x, float y, float width)
    {
        GameObject ground = new GameObject("Ground_" + x);
        ground.transform.position = new Vector3(x + width/2, y, 0);
        ground.transform.localScale = new Vector3(width, 1f, 1f);
        ground.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = groundSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, 1f);
        sr.color = new Color(0.6f, 0.4f, 0.2f);

        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        generatedObjects.Add(ground);
    }

    void CreatePlatform(float x, float y, float width)
    {
        GameObject platform = new GameObject("Platform_" + x);
        platform.transform.position = new Vector3(x, y, 0);
        platform.transform.localScale = new Vector3(width, 1f, 1f);
        platform.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = groundSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, 1f);
        sr.color = new Color(0.5f, 0.7f, 0.3f);

        BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        generatedObjects.Add(platform);
    }

    void CreateEnemy(float x, float y)
    {
        GameObject enemy = new GameObject("Enemy_" + x);
        enemy.transform.position = new Vector3(x, y, 0);
        enemy.tag = "Enemy";

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = enemySprite;
        sr.color = Color.red;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        Enemy enemyScript = enemy.AddComponent<Enemy>();
        enemyScript.groundLayer = LayerMask.GetMask("Ground");

        generatedObjects.Add(enemy);
    }

    void CreateCoinRow(float startX, float y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateCoin(startX + i * 1.2f, y);
        }
    }

    void CreateCoin(float x, float y)
    {
        GameObject coin = new GameObject("Coin_" + x);
        coin.transform.position = new Vector3(x, y, 0);

        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite;
        sr.color = Color.yellow;

        CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        coin.AddComponent<Coin>();

        generatedObjects.Add(coin);
    }

    void CreateGoal(float x, float y)
    {
        GameObject goal = new GameObject("Goal");
        goal.transform.position = new Vector3(x, y + 2f, 0);

        // Bayrak diregi
        SpriteRenderer sr = goal.AddComponent<SpriteRenderer>();
        sr.color = Color.green;

        BoxCollider2D col = goal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 5f);

        goal.AddComponent<Goal>();

        generatedObjects.Add(goal);
    }
}
