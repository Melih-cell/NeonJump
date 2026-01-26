using UnityEngine;
using System.Collections.Generic;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Platform Settings")]
    public GameObject platformPrefab;
    public Transform player;

    [Header("Spawn Settings")]
    public float spawnDistance = 15f;
    public float minY = -2f;
    public float maxY = 4f;
    public float minXGap = 2f;
    public float maxXGap = 5f;
    public float minWidth = 2f;
    public float maxWidth = 5f;

    [Header("Cleanup")]
    public float destroyDistance = 20f;

    [Header("Runtime Spawning")]
    public bool enableRuntimeSpawning = false;  // Inspector'dan kontrol edin

    private float lastSpawnX = 70f;
    private List<GameObject> platforms = new List<GameObject>();

    void Start()
    {
        // Platform spawner baslangic noktasi (ana zemin bittikten sonra)
    }

    void Update()
    {
        if (!enableRuntimeSpawning || player == null) return;

        // Yeni platform olustur
        while (lastSpawnX < player.position.x + spawnDistance)
        {
            SpawnRandomPlatform();
        }

        // Eski platformlari temizle
        CleanupPlatforms();
    }

    void SpawnRandomPlatform()
    {
        float xGap = Random.Range(minXGap, maxXGap);
        float newX = lastSpawnX + xGap;
        float newY = Random.Range(minY, maxY);
        float width = Random.Range(minWidth, maxWidth);

        SpawnPlatform(newX, newY, width);
        lastSpawnX = newX + width;
    }

    void SpawnPlatform(float x, float y, float width)
    {
        GameObject platform;

        if (platformPrefab != null)
        {
            platform = Instantiate(platformPrefab, new Vector3(x, y, 0), Quaternion.identity);
        }
        else
        {
            // Prefab yoksa basit platform olustur
            platform = new GameObject("Platform");
            platform.transform.position = new Vector3(x, y, 0);

            SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.4f, 0.8f, 0.4f);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, 1f);

            BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 1f);
        }

        platform.transform.localScale = new Vector3(width, 1f, 1f);
        platform.layer = LayerMask.NameToLayer("Ground");

        platforms.Add(platform);
    }

    void CleanupPlatforms()
    {
        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            if (platforms[i] == null)
            {
                platforms.RemoveAt(i);
                continue;
            }

            if (platforms[i].transform.position.x < player.position.x - destroyDistance)
            {
                Destroy(platforms[i]);
                platforms.RemoveAt(i);
            }
        }
    }
}
