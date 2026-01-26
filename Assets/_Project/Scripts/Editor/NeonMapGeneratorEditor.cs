using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NeonMapGenerator))]
public class NeonMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NeonMapGenerator generator = (NeonMapGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Harita Oluşturucu", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Harita oluştur butonu
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Harita Oluştur", GUILayout.Height(30)))
        {
            generator.FindTilemaps();
            generator.GenerateMap();
            generator.editorGenerated = true;
            EditorUtility.SetDirty(generator);

            // Sahneyi değişmiş olarak işaretle
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        // Temizle butonu
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Temizle", GUILayout.Height(30)))
        {
            generator.ClearMapAndObjects();
            generator.editorGenerated = false;
            EditorUtility.SetDirty(generator);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Tilemap bul butonu
        if (GUILayout.Button("Tilemap'leri Bul"))
        {
            generator.FindTilemaps();
            EditorUtility.SetDirty(generator);
        }

        // Durum göstergesi
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            generator.editorGenerated
                ? "Harita oluşturuldu. Artık Tile Palette ile düzenleyebilirsin!"
                : "Harita henüz oluşturulmadı. 'Harita Oluştur' butonuna bas.",
            generator.editorGenerated ? MessageType.Info : MessageType.Warning
        );
    }
}
