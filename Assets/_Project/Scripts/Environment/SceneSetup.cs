using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sahne kurulum yardimcisi - Parallax ve aydinlatma otomatik ayarlari
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("Parallax")]
    public bool setupParallax = true;

    [Header("Lighting")]
    public bool setupLighting = true;
    public float ambientIntensity = 1.2f;
    public Color ambientColor = new Color(0.35f, 0.35f, 0.5f);

    void Start()
    {
        if (setupParallax)
        {
            SetupParallaxBackground();
        }

        if (setupLighting)
        {
            SetupLighting();
        }
    }

    void SetupParallaxBackground()
    {
        // ParallaxManager var mi kontrol et
        ParallaxManager pm = FindFirstObjectByType<ParallaxManager>();
        if (pm == null)
        {
            GameObject pmObj = new GameObject("ParallaxManager");
            pm = pmObj.AddComponent<ParallaxManager>();
            pm.autoSetup = true;
            Debug.Log("[SceneSetup] ParallaxManager olusturuldu");
        }
    }

    void SetupLighting()
    {
        // Global Light 2D ayarla
        var light2d = FindFirstObjectByType<UnityEngine.Rendering.Universal.Light2D>();
        if (light2d != null)
        {
            light2d.intensity = 1.5f;
            light2d.color = Color.white;
            Debug.Log("[SceneSetup] Light2D ayarlandi");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Scene Now")]
    public void SetupSceneNow()
    {
        SetupParallaxBackground();
        SetupLighting();
        Debug.Log("[SceneSetup] Sahne kurulumu tamamlandi!");
    }
#endif
}
