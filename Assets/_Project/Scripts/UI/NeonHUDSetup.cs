using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NeonHUD'u otomatik olarak olusturur
/// RuntimeUISetup tarafindan cagrilir
/// </summary>
public class NeonHUDSetup : MonoBehaviour
{
    private static bool isCreated = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        // Sadece oyun sahnesinde olustur
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Loading") return;

        CreateNeonHUD();
    }

    public static void CreateNeonHUD()
    {
        // Zaten varsa olusturma
        if (NeonHUD.Instance != null) return;

        // Mevcut HUD objelerini kontrol et - cakismayi onle
        // WeaponUI ve AdvancedHUD'u deaktive et (NeonHUD bunlari icerir)
        DeactivateOldHUD();

        // NeonHUD olustur
        GameObject hudObj = new GameObject("NeonHUD");
        hudObj.AddComponent<NeonHUD>();

        Debug.Log("[NeonHUDSetup] NeonHUD created successfully");
    }

    static void DeactivateOldHUD()
    {
        // WeaponUI deaktive et (NeonHUD silah bilgisini icerir)
        if (WeaponUI.Instance != null)
        {
            WeaponUI.Instance.gameObject.SetActive(false);
        }

        // AdvancedHUD deaktive et (NeonHUD bunun ozelliklerini icerir)
        if (AdvancedHUD.Instance != null)
        {
            AdvancedHUD.Instance.gameObject.SetActive(false);
        }
    }

    void Awake()
    {
        // Sahne degisikliginde tekrar olusturulmasin
        SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedCallback;
    }

    void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        // MainMenu disindaki sahnelerde HUD'u yeniden kontrol et
        if (scene.name != "MainMenu" && scene.name != "Loading")
        {
            // Biraz gecikme ile kontrol et (scene yuklenmesini bekle)
            StartCoroutine(DelayedCheck());
        }
    }

    System.Collections.IEnumerator DelayedCheck()
    {
        yield return new WaitForSeconds(0.1f);

        if (NeonHUD.Instance == null)
        {
            CreateNeonHUD();
        }

        DeactivateOldHUD();
    }
}
