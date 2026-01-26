using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tilemap katmanlarını ve parallax arka planları otomatik oluşturur
/// </summary>
public class TilemapSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool setupOnStart = true;

    [Header("References")]
    public Grid grid;

    void Start()
    {
        if (setupOnStart)
        {
            SetupTilemapLayers();
            SetupParallaxBackground();
        }
    }

    [ContextMenu("Setup Tilemap Layers")]
    public void SetupTilemapLayers()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<Grid>();
            if (grid == null)
            {
                Debug.LogError("Grid bulunamadı!");
                return;
            }
        }

        // Mevcut katmanları kontrol et ve eksikleri oluştur
        CreateTilemapLayer("BackgroundTilemap", -10, "Background");  // Arka plan dekorasyonları
        CreateTilemapLayer("GroundTilemap", 0, "Ground");            // Ana zemin
        CreateTilemapLayer("PlatformTilemap", 1, "Ground");          // Platformlar
        CreateTilemapLayer("HazardTilemap", 2, "Hazard");            // Tehlikeler
        CreateTilemapLayer("DecorationTilemap", 5, "Decoration");    // Ön plan dekorasyonları
        CreateTilemapLayer("SecretTilemap", 3, "Secret");            // Gizli bölgeler

        Debug.Log("Tilemap katmanları oluşturuldu!");
    }

    void CreateTilemapLayer(string name, int sortingOrder, string tag)
    {
        // Zaten varsa atla
        Transform existing = grid.transform.Find(name);
        if (existing != null)
        {
            // Sorting order güncelle
            TilemapRenderer tr = existing.GetComponent<TilemapRenderer>();
            if (tr != null)
            {
                tr.sortingOrder = sortingOrder;
            }
            return;
        }

        // Yeni tilemap oluştur
        GameObject tilemapObj = new GameObject(name);
        tilemapObj.transform.SetParent(grid.transform);
        tilemapObj.transform.localPosition = Vector3.zero;

        // Tag ayarla (varsa)
        try
        {
            tilemapObj.tag = tag;
        }
        catch
        {
            tilemapObj.tag = "Untagged";
        }

        // Tilemap bileşenleri
        Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        // Hazard ve Ground katmanlarına collider ekle
        if (tag == "Ground" || tag == "Hazard")
        {
            TilemapCollider2D collider = tilemapObj.AddComponent<TilemapCollider2D>();

            if (tag == "Hazard")
            {
                collider.isTrigger = true;
            }
        }

        // Secret katmanına SecretArea ekle
        if (tag == "Secret")
        {
            tilemapObj.AddComponent<SecretArea>();
        }

        Debug.Log($"Tilemap oluşturuldu: {name}");
    }

    [ContextMenu("Setup Parallax Background")]
    public void SetupParallaxBackground()
    {
        // Parallax parent objesi
        GameObject parallaxParent = GameObject.Find("ParallaxBackground");
        if (parallaxParent == null)
        {
            parallaxParent = new GameObject("ParallaxBackground");
            parallaxParent.transform.position = Vector3.zero;
        }

        // Arka plan katmanları oluştur
        CreateParallaxLayer(parallaxParent.transform, "Sky", -50, 0.1f, new Color(0.1f, 0.1f, 0.2f));
        CreateParallaxLayer(parallaxParent.transform, "FarClouds", -40, 0.2f, new Color(0.2f, 0.2f, 0.35f));
        CreateParallaxLayer(parallaxParent.transform, "NearClouds", -30, 0.4f, new Color(0.3f, 0.3f, 0.5f));
        CreateParallaxLayer(parallaxParent.transform, "Mountains", -20, 0.6f, new Color(0.15f, 0.15f, 0.25f));
        CreateParallaxLayer(parallaxParent.transform, "City", -15, 0.75f, new Color(0.1f, 0.1f, 0.15f));

        Debug.Log("Parallax arka plan oluşturuldu!");
    }

    void CreateParallaxLayer(Transform parent, string name, float zPos, float parallaxStrength, Color color)
    {
        // Zaten varsa atla
        Transform existing = parent.Find(name);
        if (existing != null) return;

        GameObject layer = new GameObject(name);
        layer.transform.SetParent(parent);
        layer.transform.position = new Vector3(0, 0, zPos);

        // Sprite renderer
        SpriteRenderer sr = layer.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = (int)zPos;

        // Büyük bir kare sprite oluştur
        Texture2D tex = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];

        // Gradyan doldur
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float gradient = (float)y / 64f;
                colors[y * 64 + x] = Color.Lerp(color * 0.5f, color, gradient);
            }
        }
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 1);
        layer.transform.localScale = new Vector3(100, 50, 1);

        // Parallax script
        ParallaxBackground parallax = layer.AddComponent<ParallaxBackground>();
        parallax.parallaxStrength = parallaxStrength;
        parallax.infiniteHorizontal = true;
    }

    [ContextMenu("Create Hazard Prefabs")]
    public void CreateHazardPrefabs()
    {
        // Spike prefab
        CreateHazardPrefab("Spike", Hazard.HazardType.Spikes, new Color(0.8f, 0.8f, 0.8f), CreateSpikeSprite());

        // Lava prefab
        CreateHazardPrefab("Lava", Hazard.HazardType.Lava, new Color(1f, 0.3f, 0f), CreateLavaSprite());

        Debug.Log("Hazard prefab'ları oluşturuldu!");
    }

    void CreateHazardPrefab(string name, Hazard.HazardType type, Color color, Sprite sprite)
    {
        GameObject hazard = new GameObject(name);

        SpriteRenderer sr = hazard.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 2;

        BoxCollider2D col = hazard.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 0.5f);
        col.offset = new Vector2(0f, -0.25f);

        Hazard h = hazard.AddComponent<Hazard>();
        h.hazardType = type;
        h.hazardColor = color;

        // Prefab olarak kaydet (Editor'da)
        #if UNITY_EDITOR
        string path = $"Assets/Prefabs/Hazards/{name}.prefab";

        // Klasör yoksa oluştur
        if (!System.IO.Directory.Exists("Assets/Prefabs/Hazards"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Hazards");
        }

        UnityEditor.PrefabUtility.SaveAsPrefabAsset(hazard, path);
        DestroyImmediate(hazard);
        #endif
    }

    Sprite CreateSpikeSprite()
    {
        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[256];

        // Üçgen diken
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int halfWidth = y / 2;
                int center = 8;
                if (x >= center - halfWidth && x <= center + halfWidth && y < 14)
                {
                    colors[y * 16 + x] = Color.white;
                }
                else
                {
                    colors[y * 16 + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0f), 16);
    }

    Sprite CreateLavaSprite()
    {
        Texture2D tex = new Texture2D(16, 8);
        Color[] colors = new Color[128];

        // Lav dalgaları
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float wave = Mathf.Sin(x * 0.5f) * 2;
                if (y < 6 + wave)
                {
                    float t = (float)y / 8f;
                    colors[y * 16 + x] = Color.Lerp(new Color(1f, 0.8f, 0f), new Color(1f, 0.2f, 0f), t);
                }
                else
                {
                    colors[y * 16 + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0f), 16);
    }
}
