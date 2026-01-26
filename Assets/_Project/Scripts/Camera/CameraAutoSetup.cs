using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kamera takibini yöneten kalıcı manager.
/// Sahne değişikliklerinde bile çalışmaya devam eder.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CameraManager");
            Instance = go.AddComponent<CameraManager>();
            DontDestroyOnLoad(go);
        }

        // Her sahne yüklendiğinde kamerayı kur
        Instance.SetupCameraDelayed();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu sahnesinde çalışma
        if (scene.name == "MainMenu") return;

        SetupCameraDelayed();
    }

    void SetupCameraDelayed()
    {
        // MainMenu sahnesinde çalışma
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        CancelInvoke();
        Invoke("SetupCamera", 0.1f);
        Invoke("SetupCamera", 0.3f);
        Invoke("SetupCamera", 0.5f);
    }

    void SetupCamera()
    {
        // Main Camera'yı bul
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[CameraManager] Main Camera bulunamadi!");
            return;
        }

        // CameraFollow ekle veya bul
        CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
        if (camFollow == null)
        {
            camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            camFollow.smoothSpeed = 5f;
            camFollow.offset = new Vector3(3, 2, -10);
            camFollow.minX = 0;
            camFollow.maxX = 280;
            camFollow.minY = 0;
            camFollow.maxY = 20;
        }

        // Oyuncuyu bul
        Transform playerTransform = FindPlayer();
        if (playerTransform != null)
        {
            camFollow.target = playerTransform;
        }

        // GameManager yoksa oluştur
        if (GameManager.Instance == null)
        {
            GameManager existingGM = FindFirstObjectByType<GameManager>();
            if (existingGM == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                GameManager gm = gmObj.AddComponent<GameManager>();
                gm.player = playerTransform;
                gm.maxHealth = 3;
                gm.deathY = -10f;
            }
        }
        else if (GameManager.Instance.player == null && playerTransform != null)
        {
            GameManager.Instance.player = playerTransform;
        }

        // UIManager yoksa oluştur
        if (UIManager.Instance == null && FindFirstObjectByType<UIManager>() == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiObj.AddComponent<UIManager>();
        }

        // EventSystem kontrolü - sadece bir tane olmalı
        var eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            // Fazla olanları sil
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Destroy(eventSystems[i].gameObject);
            }
        }
        else if (eventSystems.Length == 0)
        {
            // EventSystem yoksa oluştur
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    Transform FindPlayer()
    {
        // PlayerController ile ara
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) return pc.transform;

        // İsimle ara
        GameObject player = GameObject.Find("Player_Asker");
        if (player != null) return player.transform;

        // Tag ile ara
        player = GameObject.FindWithTag("Player");
        if (player != null) return player.transform;

        // Genel isimle ara
        player = GameObject.Find("Player");
        if (player != null) return player.transform;

        return null;
    }
}
