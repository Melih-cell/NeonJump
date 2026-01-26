using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Envanter sistemini otomatik olarak başlatır
/// </summary>
public static class InventorySetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        // MainMenu sahnesinde çalışma
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        // InventoryManager yoksa oluştur
        if (InventoryManager.Instance == null)
        {
            GameObject invManager = new GameObject("InventoryManager");
            invManager.AddComponent<InventoryManager>();
        }

        // InventoryUI yoksa oluştur
        if (InventoryUI.Instance == null)
        {
            GameObject invUI = new GameObject("InventoryUI");
            invUI.AddComponent<InventoryUI>();
        }

        // Klavye kontrolü için helper
        if (Object.FindFirstObjectByType<InventoryInputHandler>() == null)
        {
            GameObject inputHandler = new GameObject("InventoryInputHandler");
            inputHandler.AddComponent<InventoryInputHandler>();
        }
    }
}

/// <summary>
/// Envanter klavye/dokunmatik kontrolü
/// </summary>
public class InventoryInputHandler : MonoBehaviour
{
    void Update()
    {
        if (InventoryManager.Instance == null) return;

        // Oyun duraklatıldıysa çalışma
        if (Time.timeScale == 0) return;

        // Klavye ile hızlı slot kullanımı (1-4)
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            if (keyboard.digit1Key.wasPressedThisFrame)
                InventoryManager.Instance.UseQuickSlot(0);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                InventoryManager.Instance.UseQuickSlot(1);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                InventoryManager.Instance.UseQuickSlot(2);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                InventoryManager.Instance.UseQuickSlot(3);

            // Test için T tuşu ile eşya ekle
            if (keyboard.tKey.wasPressedThisFrame)
            {
                InventoryManager.Instance.AddTestItems();
                Debug.Log("Test eşyaları eklendi!");
            }
        }
    }
}
