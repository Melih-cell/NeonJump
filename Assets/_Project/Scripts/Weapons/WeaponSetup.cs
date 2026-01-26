using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Silah sistemini otomatik olarak başlatır
/// </summary>
public static class WeaponSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        // MainMenu sahnesinde çalışma
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        // WeaponManager yoksa oluştur
        if (WeaponManager.Instance == null)
        {
            // Player'ı bul ve WeaponManager ekle
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                WeaponManager wm = player.gameObject.AddComponent<WeaponManager>();
                wm.firePoint = player.firePoint;
            }
            else
            {
                // Player yoksa ayrı obje olarak oluştur
                GameObject wmObj = new GameObject("WeaponManager");
                wmObj.AddComponent<WeaponManager>();
            }
        }

        // WeaponUI yoksa oluştur
        if (WeaponUI.Instance == null)
        {
            GameObject uiObj = new GameObject("WeaponUI");
            uiObj.AddComponent<WeaponUI>();
        }

        // Test input handler
        if (Object.FindFirstObjectByType<WeaponTestHandler>() == null)
        {
            GameObject testHandler = new GameObject("WeaponTestHandler");
            testHandler.AddComponent<WeaponTestHandler>();
        }
    }
}

/// <summary>
/// Test için silah ekleme kontrolü
/// </summary>
public class WeaponTestHandler : MonoBehaviour
{
    void Update()
    {
        if (WeaponManager.Instance == null) return;

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        // Y tuşu ile test silahları ekle
        if (keyboard.yKey.wasPressedThisFrame)
        {
            WeaponManager.Instance.AddTestWeapons();
            Debug.Log("Test silahları eklendi! (1: Rifle, 2: Pistol, 3: Rocket)");
        }

        // G tuşu ile Shotgun ekle
        if (keyboard.gKey.wasPressedThisFrame)
        {
            WeaponManager.Instance.AddWeapon(WeaponType.Shotgun);
            Debug.Log("Shotgun eklendi!");
        }

        // H tuşu ile SMG ekle
        if (keyboard.hKey.wasPressedThisFrame)
        {
            WeaponManager.Instance.AddWeapon(WeaponType.SMG);
            Debug.Log("SMG eklendi!");
        }
    }
}
