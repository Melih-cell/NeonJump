using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NeonLevelDesigner))]
public class NeonLevelDesignerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NeonLevelDesigner designer = (NeonLevelDesigner)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Generation", EditorStyles.boldLabel);

        // Generate butonu
        if (GUILayout.Button("Generate Level", GUILayout.Height(40)))
        {
            GenerateLevelInEditor(designer);
        }

        // Clear butonu
        if (GUILayout.Button("Clear Level", GUILayout.Height(30)))
        {
            ClearLevel();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("'Generate Level' butonuna tikla ve editorde sahneyi gor!", MessageType.Info);
    }

    void GenerateLevelInEditor(NeonLevelDesigner designer)
    {
        // Onceki level'i temizle
        ClearLevel();

        // Level'i olustur
        designer.GenerateLevelInEditor();

        // Dirty flag - kaydetmek icin
        EditorUtility.SetDirty(designer);

        Debug.Log("Level editorde olusturuldu!");
    }

    void ClearLevel()
    {
        // LevelContainer'i bul ve sil
        GameObject levelContainer = GameObject.Find("LevelContainer");
        if (levelContainer != null)
            DestroyImmediate(levelContainer);

        // BackgroundContainer'i bul ve sil
        GameObject bgContainer = GameObject.Find("BackgroundContainer");
        if (bgContainer != null)
            DestroyImmediate(bgContainer);

        Debug.Log("Level temizlendi!");
    }
}
