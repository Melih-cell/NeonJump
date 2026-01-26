using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class TilemapSetup : EditorWindow
{
    [MenuItem("NeonJump/Setup Tilemap Level")]
    public static void SetupTilemapLevel()
    {
        // Enemy tag ekle
        AddTag("Enemy");

        // Grid olustur
        GameObject gridObj = new GameObject("Grid");
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 1);

        // Ground Tilemap
        GameObject groundTilemapObj = new GameObject("GroundTilemap");
        groundTilemapObj.transform.SetParent(gridObj.transform);
        Tilemap groundTilemap = groundTilemapObj.AddComponent<Tilemap>();
        TilemapRenderer groundRenderer = groundTilemapObj.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = 0;
        TilemapCollider2D groundCollider = groundTilemapObj.AddComponent<TilemapCollider2D>();
        Rigidbody2D groundRb = groundTilemapObj.AddComponent<Rigidbody2D>();
        groundRb.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D groundComposite = groundTilemapObj.AddComponent<CompositeCollider2D>();
        groundCollider.usedByComposite = true;
        groundTilemapObj.layer = LayerMask.NameToLayer("Ground");

        // Platform Tilemap
        GameObject platformTilemapObj = new GameObject("PlatformTilemap");
        platformTilemapObj.transform.SetParent(gridObj.transform);
        Tilemap platformTilemap = platformTilemapObj.AddComponent<Tilemap>();
        TilemapRenderer platformRenderer = platformTilemapObj.AddComponent<TilemapRenderer>();
        platformRenderer.sortingOrder = 1;
        TilemapCollider2D platformCollider = platformTilemapObj.AddComponent<TilemapCollider2D>();
        Rigidbody2D platformRb = platformTilemapObj.AddComponent<Rigidbody2D>();
        platformRb.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D platformComposite = platformTilemapObj.AddComponent<CompositeCollider2D>();
        platformCollider.usedByComposite = true;
        platformTilemapObj.layer = LayerMask.NameToLayer("Ground");

        // Decoration Tilemap
        GameObject decorationTilemapObj = new GameObject("DecorationTilemap");
        decorationTilemapObj.transform.SetParent(gridObj.transform);
        Tilemap decorationTilemap = decorationTilemapObj.AddComponent<Tilemap>();
        TilemapRenderer decorationRenderer = decorationTilemapObj.AddComponent<TilemapRenderer>();
        decorationRenderer.sortingOrder = -1;

        // LevelBuilder olustur
        GameObject levelBuilderObj = new GameObject("TilemapLevelBuilder");
        TilemapLevelBuilder levelBuilder = levelBuilderObj.AddComponent<TilemapLevelBuilder>();

        // Tilemap'leri ata
        levelBuilder.groundTilemap = groundTilemap;
        levelBuilder.platformTilemap = platformTilemap;
        levelBuilder.decorationTilemap = decorationTilemap;

        // Tile'lari yukle
        levelBuilder.groundTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/GroundTile.asset");
        levelBuilder.grassTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/GrassTile.asset");
        levelBuilder.platformTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlatformTile.asset");
        levelBuilder.platformLeftTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlatformLeftTile.asset");
        levelBuilder.platformMiddleTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlatformMiddleTile.asset");
        levelBuilder.platformRightTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/PlatformRightTile.asset");

        // Eski LevelGenerator'u devre disi birak
        LevelGenerator oldGenerator = GameObject.FindFirstObjectByType<LevelGenerator>();
        if (oldGenerator != null)
        {
            oldGenerator.gameObject.SetActive(false);
            Debug.Log("Eski LevelGenerator devre disi birakildi");
        }

        // Scene'i kaydet
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Tilemap Level kurulumu tamamlandi!");
        Debug.Log("Oyunu calistirarak haritayi gorebilirsiniz.");
    }

    static void AddTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Tag zaten var mi kontrol et
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                return; // Tag zaten var
            }
        }

        // Yeni tag ekle
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
        Debug.Log("'" + tagName + "' tag'i eklendi");
    }
}
