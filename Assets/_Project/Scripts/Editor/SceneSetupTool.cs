using UnityEngine;
using UnityEditor;

public class SceneSetupTool : EditorWindow
{
    [MenuItem("NeonJump/Sahne Kurulumu")]
    public static void ShowWindow()
    {
        GetWindow<SceneSetupTool>("Sahne Kurulumu");
    }

    [MenuItem("NeonJump/Tum Manager'lari Olustur")]
    public static void CreateAllManagers()
    {
        CreateGameManager();
        CreateCameraFollow();
        CreateUIManager();
        CreateAudioManager();
        CreateParticleManager();
        CreatePowerUpManager();
        Debug.Log("Tum manager'lar olusturuldu!");
    }

    void OnGUI()
    {
        GUILayout.Label("NeonJump Sahne Kurulum Araci", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Tek Tek Olustur:", EditorStyles.boldLabel);

        if (GUILayout.Button("GameManager Olustur"))
            CreateGameManager();

        if (GUILayout.Button("CameraFollow Olustur (Main Camera'ya)"))
            CreateCameraFollow();

        if (GUILayout.Button("UIManager Olustur"))
            CreateUIManager();

        if (GUILayout.Button("AudioManager Olustur"))
            CreateAudioManager();

        if (GUILayout.Button("ParticleManager Olustur"))
            CreateParticleManager();

        if (GUILayout.Button("PowerUpManager Olustur"))
            CreatePowerUpManager();

        GUILayout.Space(20);
        GUILayout.Label("Toplu Islemler:", EditorStyles.boldLabel);

        if (GUILayout.Button("TUMUNU OLUSTUR", GUILayout.Height(40)))
        {
            CreateAllManagers();
        }

        GUILayout.Space(10);

        GUI.color = Color.yellow;
        if (GUILayout.Button("GameBootstrap'i Devre Disi Birak"))
        {
            DisableGameBootstrap();
        }
        GUI.color = Color.white;

        GUILayout.Space(20);
        GUILayout.Label("Not: Manager'lar zaten varsa tekrar olusturulmaz.", EditorStyles.miniLabel);
    }

    static void CreateGameManager()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            Debug.Log("GameManager zaten mevcut!");
            return;
        }

        GameObject go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();
        gm.maxHealth = 3;
        gm.deathY = -99999f;

        // Player'i bul ve ata
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player_Asker");
        if (player != null) gm.player = player.transform;

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
        Debug.Log("GameManager olusturuldu!");
    }

    static void CreateCameraFollow()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera bulunamadi!");
            return;
        }

        CameraFollow existing = mainCam.GetComponent<CameraFollow>();
        if (existing != null)
        {
            Debug.Log("CameraFollow zaten mevcut!");
            Selection.activeGameObject = mainCam.gameObject;
            return;
        }

        CameraFollow cf = mainCam.gameObject.AddComponent<CameraFollow>();
        cf.smoothSpeed = 5f;
        cf.offset = new Vector3(3, 2, -10);
        cf.minX = -100;
        cf.maxX = 500;
        cf.minY = -500;
        cf.maxY = 100;

        // Player'i bul ve ata
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player_Asker");
        if (player != null) cf.target = player.transform;

        Selection.activeGameObject = mainCam.gameObject;
        Undo.RegisterCompleteObjectUndo(mainCam.gameObject, "Add CameraFollow");
        Debug.Log("CameraFollow Main Camera'ya eklendi!");
    }

    static void CreateUIManager()
    {
        if (Object.FindFirstObjectByType<UIManager>() != null)
        {
            Debug.Log("UIManager zaten mevcut!");
            return;
        }

        GameObject go = new GameObject("UIManager");
        go.AddComponent<UIManager>();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create UIManager");
        Debug.Log("UIManager olusturuldu!");
    }

    static void CreateAudioManager()
    {
        if (Object.FindFirstObjectByType<AudioManager>() != null)
        {
            Debug.Log("AudioManager zaten mevcut!");
            return;
        }

        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
        Debug.Log("AudioManager olusturuldu!");
    }

    static void CreateParticleManager()
    {
        if (Object.FindFirstObjectByType<ParticleManager>() != null)
        {
            Debug.Log("ParticleManager zaten mevcut!");
            return;
        }

        GameObject go = new GameObject("ParticleManager");
        go.AddComponent<ParticleManager>();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create ParticleManager");
        Debug.Log("ParticleManager olusturuldu!");
    }

    static void CreatePowerUpManager()
    {
        if (Object.FindFirstObjectByType<PowerUpManager>() != null)
        {
            Debug.Log("PowerUpManager zaten mevcut!");
            return;
        }

        GameObject go = new GameObject("PowerUpManager");
        go.AddComponent<PowerUpManager>();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create PowerUpManager");
        Debug.Log("PowerUpManager olusturuldu!");
    }

    static void DisableGameBootstrap()
    {
        // GameBootstrap.cs dosyasinin adini degistirerek devre disi birak
        string path = "Assets/Scripts/GameBootstrap.cs";
        string disabledPath = "Assets/Scripts/GameBootstrap.cs.disabled";

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Move(path, disabledPath);
            AssetDatabase.Refresh();
            Debug.Log("GameBootstrap devre disi birakildi! (GameBootstrap.cs.disabled olarak yeniden adlandirildi)");
        }
        else if (System.IO.File.Exists(disabledPath))
        {
            Debug.Log("GameBootstrap zaten devre disi!");
        }
        else
        {
            Debug.LogWarning("GameBootstrap.cs bulunamadi!");
        }
    }
}
