using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneSetupEditor : Editor
{
    [MenuItem("NeonJump/Setup SampleScene (Game)", priority = 1)]
    public static void SetupSampleScene()
    {
        // SampleScene'i ac
        string scenePath = "Assets/Scenes/SampleScene.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Tum mevcut objeleri sil (kamera haric)
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (obj.GetComponent<Camera>() == null)
            {
                DestroyImmediate(obj);
            }
        }

        // Kamera yoksa olustur
        if (Camera.main == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7;
            cam.backgroundColor = new Color(0.05f, 0.02f, 0.15f);
            camObj.transform.position = new Vector3(5, 5, -10);
            camObj.AddComponent<AudioListener>();
        }

        // Grid ve Tilemap olustur
        GameObject gridObj = new GameObject("Grid");
        UnityEngine.Grid grid = gridObj.AddComponent<UnityEngine.Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        // Ground Tilemap
        GameObject groundTilemapObj = new GameObject("GroundTilemap");
        groundTilemapObj.transform.SetParent(gridObj.transform);
        UnityEngine.Tilemaps.Tilemap groundTilemap = groundTilemapObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        UnityEngine.Tilemaps.TilemapRenderer groundRenderer = groundTilemapObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        groundRenderer.sortingOrder = 0;
        groundTilemapObj.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();

        // Platform Tilemap
        GameObject platformTilemapObj = new GameObject("PlatformTilemap");
        platformTilemapObj.transform.SetParent(gridObj.transform);
        UnityEngine.Tilemaps.Tilemap platformTilemap = platformTilemapObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        UnityEngine.Tilemaps.TilemapRenderer platformRenderer = platformTilemapObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        platformRenderer.sortingOrder = 1;
        platformTilemapObj.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();

        // TilemapLevelBuilder
        GameObject levelBuilderObj = new GameObject("LevelBuilder");
        TilemapLevelBuilder builder = levelBuilderObj.AddComponent<TilemapLevelBuilder>();
        builder.groundTilemap = groundTilemap;
        builder.platformTilemap = platformTilemap;

        // Scene'i kaydet
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("SampleScene setup completed! Press Play to test the game.");
    }

    [MenuItem("NeonJump/Setup MainMenu Scene", priority = 2)]
    public static void SetupMainMenuScene()
    {
        // MainMenu scene'i ac
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Tum mevcut objeleri sil
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            DestroyImmediate(obj);
        }

        // Kamera olustur
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.backgroundColor = new Color(0.05f, 0.02f, 0.15f);
        camObj.transform.position = new Vector3(0, 0, -10);
        camObj.AddComponent<AudioListener>();

        // MainMenuSetup ekle
        GameObject menuSetupObj = new GameObject("MainMenuSetup");
        menuSetupObj.AddComponent<MainMenuSetup>();

        // Scene'i kaydet
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("MainMenu scene setup completed!");
    }

    [MenuItem("NeonJump/Add Both Scenes to Build Settings", priority = 10)]
    public static void AddScenesToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("Both scenes added to Build Settings! MainMenu is first (index 0).");
    }

    [MenuItem("NeonJump/Quick Start - Setup Everything", priority = 0)]
    public static void QuickStart()
    {
        // Once scene'leri build settings'e ekle
        AddScenesToBuildSettings();

        // MainMenu'yu kur
        SetupMainMenuScene();

        // SampleScene'i kur
        SetupSampleScene();

        Debug.Log("=== SETUP COMPLETE ===");
        Debug.Log("1. MainMenu and SampleScene have been configured");
        Debug.Log("2. Both scenes added to Build Settings");
        Debug.Log("3. Press PLAY to start the game!");
    }
}
#endif
