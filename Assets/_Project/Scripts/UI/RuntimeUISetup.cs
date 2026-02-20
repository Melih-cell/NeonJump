using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

public class RuntimeUISetup : MonoBehaviour
{
    void Awake()
    {
        // UIManager yoksa olustur
        if (FindObjectOfType<UIManager>() == null)
        {
            SetupGameUI();
        }

        // ParticleManager yoksa olustur
        if (FindObjectOfType<ParticleManager>() == null)
        {
            GameObject pmObj = new GameObject("ParticleManager");
            pmObj.AddComponent<ParticleManager>();
        }

        // NeonHUD olustur (gelismis HUD sistemi)
        if (NeonHUD.Instance == null)
        {
            GameObject neonHudObj = new GameObject("NeonHUD");
            neonHudObj.AddComponent<NeonHUD>();
        }

        // Mobil kontroller - her zaman olustur, gorunurlugu MobileControls.Start() kontrol eder
        if (MobileControls.Instance == null)
        {
            GameObject mobileCtrlObj = new GameObject("MobileControls");
            mobileCtrlObj.AddComponent<MobileControls>();
        }
    }

    void SetupGameUI()
    {
        // Canvas olustur
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Safe area handling - notch'lu cihazlar icin
        GameObject safeArea = new GameObject("SafeArea");
        safeArea.transform.SetParent(canvasObj.transform, false);
        RectTransform safeAreaRT = safeArea.AddComponent<RectTransform>();
        safeAreaRT.anchorMin = Vector2.zero;
        safeAreaRT.anchorMax = Vector2.one;
        safeAreaRT.offsetMin = Vector2.zero;
        safeAreaRT.offsetMax = Vector2.zero;
        safeArea.AddComponent<SafeAreaHandler>();

        // EventSystem - Yeni Input System ile uyumlu
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // UIManager - panelleri kendi Start() metodunda olusturur
        GameObject uiManagerObj = new GameObject("UIManager");
        uiManagerObj.AddComponent<UIManager>();
    }
}
