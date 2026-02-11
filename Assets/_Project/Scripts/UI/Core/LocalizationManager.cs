using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Merkezi lokalizasyon yonetimi.
/// Tum UI metinlerinin ceviri islemlerini yonetir.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("Dil Verileri")]
    [Tooltip("Turkce lokalizasyon verisi")]
    public LocalizationData turkishData;

    [Tooltip("Ingilizce lokalizasyon verisi")]
    public LocalizationData englishData;

    [Header("Mevcut Dil")]
    [SerializeField]
    private string currentLanguage = "tr";

    // Aktif lokalizasyon verisi
    private LocalizationData activeData;

    // Kayitli UI elementleri
    private List<LocalizedText> registeredTexts = new List<LocalizedText>();

    /// <summary>
    /// Mevcut dil kodu (tr, en)
    /// </summary>
    public string CurrentLanguage => currentLanguage;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        // Kaydedilmis dili yukle
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            currentLanguage = SaveManager.Instance.Data.language;
        }

        // Eger veri yok ise runtime'da olustur
        if (turkishData == null)
        {
            turkishData = ScriptableObject.CreateInstance<LocalizationData>();
            turkishData.LoadDefaultTurkish();
        }

        if (englishData == null)
        {
            englishData = ScriptableObject.CreateInstance<LocalizationData>();
            englishData.LoadDefaultEnglish();
        }

        // Aktif dili ayarla
        SetLanguage(currentLanguage);

        // Event'e abone ol
        GameEvents.OnLanguageChanged += OnLanguageChangedHandler;
    }

    void OnDestroy()
    {
        GameEvents.OnLanguageChanged -= OnLanguageChangedHandler;
    }

    void OnLanguageChangedHandler(string langCode)
    {
        if (langCode != currentLanguage)
        {
            SetLanguage(langCode);
        }
    }

    /// <summary>
    /// Aktif dili degistir
    /// </summary>
    public void SetLanguage(string langCode)
    {
        currentLanguage = langCode;

        switch (langCode.ToLower())
        {
            case "tr":
            case "turkish":
                activeData = turkishData;
                break;

            case "en":
            case "english":
                activeData = englishData;
                break;

            default:
                Debug.LogWarning($"[Localization] Bilinmeyen dil: {langCode}, Turkce kullaniliyor");
                activeData = turkishData;
                currentLanguage = "tr";
                break;
        }

        // Cache'i yenile
        activeData?.BuildCache();

        // Kayitli tum metinleri guncelle
        RefreshAllTexts();

        // Kaydet
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetLanguage(currentLanguage);
            SaveManager.Instance.SaveSettings();
        }

        Debug.Log($"[Localization] Dil degistirildi: {activeData?.languageNameNative ?? langCode}");
    }

    /// <summary>
    /// Anahtar icin ceviriyi al
    /// </summary>
    public string Get(string key)
    {
        if (activeData == null)
        {
            Debug.LogWarning("[Localization] Aktif dil verisi yok!");
            return key;
        }

        return activeData.Get(key);
    }

    /// <summary>
    /// Parametreli ceviri al
    /// </summary>
    public string GetFormatted(string key, params object[] args)
    {
        if (activeData == null)
            return key;

        return activeData.GetFormatted(key, args);
    }

    /// <summary>
    /// Lokalize edilmis metin komponenti kaydet
    /// </summary>
    public void RegisterText(LocalizedText text)
    {
        if (!registeredTexts.Contains(text))
        {
            registeredTexts.Add(text);
        }
    }

    /// <summary>
    /// Lokalize edilmis metin komponenti kaldir
    /// </summary>
    public void UnregisterText(LocalizedText text)
    {
        registeredTexts.Remove(text);
    }

    /// <summary>
    /// Tum kayitli metinleri guncelle
    /// </summary>
    public void RefreshAllTexts()
    {
        // Null olanlari temizle
        registeredTexts.RemoveAll(t => t == null);

        foreach (var text in registeredTexts)
        {
            text.UpdateText();
        }
    }

    /// <summary>
    /// Mevcut dillerin listesini al
    /// </summary>
    public List<LanguageInfo> GetAvailableLanguages()
    {
        var languages = new List<LanguageInfo>();

        if (turkishData != null)
        {
            languages.Add(new LanguageInfo
            {
                code = turkishData.languageCode,
                name = turkishData.languageName,
                nativeName = turkishData.languageNameNative
            });
        }

        if (englishData != null)
        {
            languages.Add(new LanguageInfo
            {
                code = englishData.languageCode,
                name = englishData.languageName,
                nativeName = englishData.languageNameNative
            });
        }

        return languages;
    }

    /// <summary>
    /// Sonraki dile gec (cycle)
    /// </summary>
    public void CycleLanguage()
    {
        var languages = GetAvailableLanguages();
        if (languages.Count == 0) return;

        int currentIndex = languages.FindIndex(l => l.code == currentLanguage);
        int nextIndex = (currentIndex + 1) % languages.Count;

        SetLanguage(languages[nextIndex].code);
    }
}

/// <summary>
/// Dil bilgisi
/// </summary>
public struct LanguageInfo
{
    public string code;       // tr, en
    public string name;       // Turkish, English
    public string nativeName; // Turkce, English
}

/// <summary>
/// TextMeshProUGUI veya Text icin lokalizasyon komponenti.
/// Bu komponenti UI text elementlerine ekleyerek otomatik ceviri saglanir.
/// </summary>
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Lokalizasyon anahtari")]
    public string localizationKey;

    [Tooltip("Parametreler (format string icin)")]
    public string[] parameters;

    private TMP_Text tmpText;
    private Text legacyText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        legacyText = GetComponent<Text>();
    }

    void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.RegisterText(this);
            UpdateText();
        }
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.UnregisterText(this);
        }
    }

    /// <summary>
    /// Metni guncelle
    /// </summary>
    public void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey))
            return;

        string text;
        if (LocalizationManager.Instance != null)
        {
            if (parameters != null && parameters.Length > 0)
            {
                text = LocalizationManager.Instance.GetFormatted(localizationKey, parameters);
            }
            else
            {
                text = LocalizationManager.Instance.Get(localizationKey);
            }
        }
        else
        {
            text = localizationKey;
        }

        if (tmpText != null)
            tmpText.text = text;
        else if (legacyText != null)
            legacyText.text = text;
    }

    /// <summary>
    /// Anahtar degistir ve guncelle
    /// </summary>
    public void SetKey(string newKey)
    {
        localizationKey = newKey;
        UpdateText();
    }

    /// <summary>
    /// Parametreleri guncelle
    /// </summary>
    public void SetParameters(params string[] newParams)
    {
        parameters = newParams;
        UpdateText();
    }
}
