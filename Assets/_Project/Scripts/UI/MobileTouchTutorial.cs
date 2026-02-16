using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Ilk oyunda basit dokunmatik kontrol ipuclari gosteren tutorial sistemi.
/// Oyuncuya joystick, ziplama ve ates etme kontrollerini ogretir.
/// PlayerPrefs ile sadece bir kez gosterilir.
/// </summary>
public class MobileTouchTutorial : MonoBehaviour
{
    public static MobileTouchTutorial Instance;

    [Header("Tutorial Settings")]
    [Tooltip("Her ipucu ne kadar sure gosterilir")]
    public float tipDisplayDuration = 3.5f;
    [Tooltip("Ipuclari arasi bekleme suresi")]
    public float delayBetweenTips = 1f;
    [Tooltip("Yazilar icin fade suresi")]
    public float fadeDuration = 0.5f;

    // Tutorial UI elemanlari (runtime'da olusturulur)
    private Canvas tutorialCanvas;
    private GameObject tipPanel;
    private Text tipText;
    private Image tipBackground;

    // Tutorial durumu
    private bool tutorialShown = false;
    private bool tutorialActive = false;

    private static readonly string TUTORIAL_PREF_KEY = "MobileTutorialShown";

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Sadece mobilde goster
        bool isMobile = Application.isMobilePlatform;
        #if UNITY_EDITOR
        if (MobileControls.Instance != null && MobileControls.Instance.IsEnabled)
            isMobile = true;
        #endif

        if (!isMobile) return;

        // Daha once gosterildiyse tekrar gosterme
        if (PlayerPrefs.GetInt(TUTORIAL_PREF_KEY, 0) == 1)
        {
            tutorialShown = true;
            return;
        }

        // Tutorial'i biraz gecikmeyle baslat (sahne yuklensin)
        StartCoroutine(StartTutorialDelayed(1.5f));
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    IEnumerator StartTutorialDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tutorialShown) yield break;

        CreateTutorialUI();
        yield return StartCoroutine(ShowTutorialSequence());
    }

    void CreateTutorialUI()
    {
        // Canvas olustur
        GameObject canvasObj = new GameObject("TutorialCanvas");
        tutorialCanvas = canvasObj.AddComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.sortingOrder = 999; // En ustte

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Tip panel olustur
        tipPanel = new GameObject("TipPanel");
        tipPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = tipPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.6f);
        panelRect.anchorMax = new Vector2(0.8f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Arka plan
        tipBackground = tipPanel.AddComponent<Image>();
        tipBackground.color = new Color(0f, 0f, 0f, 0.75f);

        // Yazi
        GameObject textObj = new GameObject("TipText");
        textObj.transform.SetParent(tipPanel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);

        tipText = textObj.AddComponent<Text>();
        tipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tipText.fontSize = 32;
        tipText.color = new Color(0f, 1f, 1f, 1f); // Neon cyan
        tipText.alignment = TextAnchor.MiddleCenter;
        tipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tipText.verticalOverflow = VerticalWrapMode.Overflow;

        // Outline efekti
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        // Baslangicta gizle
        tipPanel.SetActive(false);
    }

    IEnumerator ShowTutorialSequence()
    {
        tutorialActive = true;

        // Ipucu 1: Hareket
        yield return StartCoroutine(ShowTip("Sol joystick ile HAREKET ET"));

        yield return new WaitForSeconds(delayBetweenTips);

        // Ipucu 2: Ziplama
        yield return StartCoroutine(ShowTip("ZIPLA butonuna bas"));

        yield return new WaitForSeconds(delayBetweenTips);

        // Ipucu 3: Ates etme
        yield return StartCoroutine(ShowTip("ATES butonuyla dusmanlari yok et"));

        yield return new WaitForSeconds(delayBetweenTips);

        // Ipucu 4: Dash
        yield return StartCoroutine(ShowTip("DASH ile hizla yon degistir"));

        // Tutorial bitti - kaydet
        tutorialShown = true;
        tutorialActive = false;
        PlayerPrefs.SetInt(TUTORIAL_PREF_KEY, 1);
        PlayerPrefs.Save();

        // Temizle
        if (tutorialCanvas != null)
        {
            Destroy(tutorialCanvas.gameObject, 1f);
        }
    }

    IEnumerator ShowTip(string message)
    {
        if (tipPanel == null || tipText == null) yield break;

        tipText.text = message;
        tipPanel.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadePanel(0f, 1f, fadeDuration));

        // Goster
        yield return new WaitForSeconds(tipDisplayDuration);

        // Fade out
        yield return StartCoroutine(FadePanel(1f, 0f, fadeDuration));

        tipPanel.SetActive(false);
    }

    IEnumerator FadePanel(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            if (tipBackground != null)
                tipBackground.color = new Color(0f, 0f, 0f, 0.75f * alpha);

            if (tipText != null)
                tipText.color = new Color(0f, 1f, 1f, alpha);

            yield return null;
        }
    }

    /// <summary>
    /// Tutorial'i sifirla (tekrar gostermek icin)
    /// </summary>
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_PREF_KEY);
        PlayerPrefs.Save();
        tutorialShown = false;
    }

    /// <summary>
    /// Tutorial aktif mi?
    /// </summary>
    public bool IsTutorialActive => tutorialActive;
}
