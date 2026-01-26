using UnityEngine;

/// <summary>
/// Oyun basladiginda otomatik olarak tum sistemleri kurar.
/// Sahneye hicbir sey eklemenize gerek yok - her sey otomatik!
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        Debug.Log("=== NeonJump Bootstrap Baslatiliyor ===");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        Debug.Log("=== NeonJump Oyun Kurulumu Basliyor ===");

        // GameSetup zaten varsa tekrar olusturma
        if (Object.FindFirstObjectByType<GameSetup>() != null)
        {
            Debug.Log("GameSetup zaten mevcut, kurulum atlaniyor.");
            return;
        }

        // Eger sahnede Player_Asker varsa sadece gerekli manager'lari ekle
        GameObject existingPlayer = GameObject.Find("Player_Asker");
        if (existingPlayer == null)
            existingPlayer = GameObject.FindWithTag("Player");

        if (existingPlayer != null)
        {
            Debug.Log("Mevcut oyuncu bulundu: " + existingPlayer.name);
            SetupManagersOnly(existingPlayer);
        }
        else
        {
            // Tam kurulum yap
            SetupCompleteGame();
        }
    }

    static void SetupManagersOnly(GameObject player)
    {
        // Kamera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.gameObject.GetComponent<CameraFollow>();
            if (camFollow == null)
                camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            camFollow.target = player.transform;
            camFollow.smoothSpeed = 5f;
            camFollow.offset = new Vector3(3, 2, -10);
            camFollow.minX = -100;
            camFollow.maxX = 500;
            camFollow.minY = -500;  // Asagi dogru level destegi
            camFollow.maxY = 100;
        }

        // GameManager
        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.player = player.transform;
            gm.maxHealth = 3;
            gm.deathY = -99999f;  // Devre disi - dusunce olmez
        }

        // UI
        if (Object.FindFirstObjectByType<UIManager>() == null)
        {
            CreateUI();
        }

        // Particle Manager
        if (Object.FindFirstObjectByType<ParticleManager>() == null)
        {
            GameObject pm = new GameObject("ParticleManager");
            pm.AddComponent<ParticleManager>();
        }

        // Audio Manager
        if (Object.FindFirstObjectByType<AudioManager>() == null)
        {
            GameObject am = new GameObject("AudioManager");
            am.AddComponent<AudioManager>();
        }

        // PowerUp Manager
        if (Object.FindFirstObjectByType<PowerUpManager>() == null)
        {
            GameObject pum = new GameObject("PowerUpManager");
            pum.AddComponent<PowerUpManager>();
        }

        // Muzik baslat
        GameObject musicStarter = new GameObject("MusicStarter");
        musicStarter.AddComponent<MusicStarterHelper>();

        Debug.Log("=== Manager Kurulumu Tamamlandi! ===");
    }

    static void SetupCompleteGame()
    {
        Debug.Log("Tam oyun kurulumu basliyor...");

        // 1. Ana Kamera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = 7f;
        mainCam.backgroundColor = new Color(0.05f, 0.02f, 0.15f);

        // 2. Oyuncu
        GameObject player = CreatePlayer();

        // 3. Kamera takip
        CameraFollow camFollow = mainCam.gameObject.GetComponent<CameraFollow>();
        if (camFollow == null)
            camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
        camFollow.target = player.transform;
        camFollow.smoothSpeed = 5f;
        camFollow.offset = new Vector3(3, 2, -10);
        camFollow.minX = -100;
        camFollow.maxX = 500;
        camFollow.minY = -500;  // Asagi dogru level destegi
        camFollow.maxY = 100;

        // 4. GameManager
        if (Object.FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.player = player.transform;
            gm.maxHealth = 3;
            gm.deathY = -99999f;  // Devre disi - dusunce olmez
        }

        // 5. UI
        CreateUI();

        // 6. Particle Manager
        if (Object.FindFirstObjectByType<ParticleManager>() == null)
        {
            GameObject pm = new GameObject("ParticleManager");
            pm.AddComponent<ParticleManager>();
        }

        // 7. Audio Manager
        if (Object.FindFirstObjectByType<AudioManager>() == null)
        {
            GameObject am = new GameObject("AudioManager");
            am.AddComponent<AudioManager>();
        }

        // 8. PowerUp Manager
        if (Object.FindFirstObjectByType<PowerUpManager>() == null)
        {
            GameObject pum = new GameObject("PowerUpManager");
            pum.AddComponent<PowerUpManager>();
        }

        // 9. Level Builder
        if (Object.FindFirstObjectByType<TilemapLevelBuilder>() == null)
        {
            GameObject levelBuilder = new GameObject("LevelBuilder");
            levelBuilder.AddComponent<TilemapLevelBuilder>();
        }

        // 10. Muzik baslat
        GameObject musicStarter = new GameObject("MusicStarter");
        musicStarter.AddComponent<MusicStarterHelper>();

        Debug.Log("=== NeonJump Oyun Kurulumu Tamamlandi! ===");
    }

    static GameObject CreatePlayer()
    {
        // Sahnede zaten Player varsa onu kullan
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer == null)
            existingPlayer = GameObject.Find("Player");
        if (existingPlayer == null)
            existingPlayer = GameObject.Find("Player_Asker");

        if (existingPlayer != null)
        {
            Debug.Log("Sahnedeki mevcut Player kullaniliyor: " + existingPlayer.name);
            existingPlayer.tag = "Player";
            return existingPlayer;
        }

        // Yeni player olustur
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(2, 5, 0);
        player.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.color = new Color(0f, 0.9f, 1f);
        sr.sortingOrder = 10;

        Texture2D tex = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.9f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.moveSpeed = 8f;
        pc.jumpForce = 14f;
        pc.bounceForce = 12f;
        pc.groundCheck = groundCheck.transform;
        pc.groundCheckRadius = 0.2f;
        pc.groundLayer = ~0;

        return player;
    }

    static void CreateUI()
    {
        if (Object.FindFirstObjectByType<UIManager>() != null) return;

        // EventSystem gerekli
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // UIManager olustur - kendi Neon HUD'ini olusturacak
        GameObject uiManagerObj = new GameObject("UIManager");
        uiManagerObj.AddComponent<UIManager>();
    }
}

// Muzik baslatmak icin yardimci sinif
public class MusicStarterHelper : MonoBehaviour
{
    void Start()
    {
        Invoke("StartMusic", 0.1f);
    }

    void StartMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }
        Destroy(gameObject);
    }
}
