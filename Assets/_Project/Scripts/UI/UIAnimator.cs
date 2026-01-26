using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI animasyonlari ve efektleri icin yardimci sinif
/// </summary>
public class UIAnimator : MonoBehaviour
{
    public static UIAnimator Instance { get; private set; }

    // Aktif animasyonlar
    private Dictionary<int, Coroutine> activeAnimations = new Dictionary<int, Coroutine>();
    private int animationIdCounter = 0;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // === TEMEL ANIMASYONLAR ===

    public int FadeIn(CanvasGroup target, float duration, Action onComplete = null)
    {
        return StartAnimation(FadeCoroutine(target, 0f, 1f, duration, onComplete));
    }

    public int FadeOut(CanvasGroup target, float duration, Action onComplete = null)
    {
        return StartAnimation(FadeCoroutine(target, 1f, 0f, duration, onComplete));
    }

    public int FadeTo(CanvasGroup target, float targetAlpha, float duration, Action onComplete = null)
    {
        return StartAnimation(FadeCoroutine(target, target.alpha, targetAlpha, duration, onComplete));
    }

    IEnumerator FadeCoroutine(CanvasGroup target, float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(elapsed / duration);
            target.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        target.alpha = to;
        onComplete?.Invoke();
    }

    // Scale animasyonlari
    public int ScalePunch(RectTransform target, float punchScale, float duration, Action onComplete = null)
    {
        return StartAnimation(ScalePunchCoroutine(target, punchScale, duration, onComplete));
    }

    public int ScaleTo(RectTransform target, Vector3 targetScale, float duration, Action onComplete = null)
    {
        return StartAnimation(ScaleToCoroutine(target, targetScale, duration, onComplete));
    }

    public int PopIn(RectTransform target, float duration, Action onComplete = null)
    {
        target.localScale = Vector3.zero;
        return StartAnimation(ScaleToCoroutine(target, Vector3.one, duration, onComplete, EaseType.EaseOutBack));
    }

    public int PopOut(RectTransform target, float duration, Action onComplete = null)
    {
        return StartAnimation(ScaleToCoroutine(target, Vector3.zero, duration, onComplete, EaseType.EaseInBack));
    }

    IEnumerator ScalePunchCoroutine(RectTransform target, float punchScale, float duration, Action onComplete)
    {
        Vector3 originalScale = target.localScale;
        Vector3 punchTarget = originalScale * punchScale;

        float elapsed = 0f;
        float halfDuration = duration * 0.3f;

        // Buyut
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(elapsed / halfDuration);
            target.localScale = Vector3.Lerp(originalScale, punchTarget, t);
            yield return null;
        }

        // Geri don
        elapsed = 0f;
        while (elapsed < duration - halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutElastic(elapsed / (duration - halfDuration));
            target.localScale = Vector3.Lerp(punchTarget, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
        onComplete?.Invoke();
    }

    IEnumerator ScaleToCoroutine(RectTransform target, Vector3 to, float duration, Action onComplete, EaseType ease = EaseType.EaseOutQuad)
    {
        Vector3 from = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = ApplyEase(elapsed / duration, ease);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;
        onComplete?.Invoke();
    }

    // Pozisyon animasyonlari
    public int MoveTo(RectTransform target, Vector2 targetPos, float duration, Action onComplete = null)
    {
        return StartAnimation(MoveToCoroutine(target, targetPos, duration, onComplete));
    }

    public int SlideIn(RectTransform target, SlideDirection direction, float duration, Action onComplete = null)
    {
        Vector2 originalPos = target.anchoredPosition;
        Vector2 startPos = GetSlideStartPosition(target, direction);
        target.anchoredPosition = startPos;
        return StartAnimation(MoveToCoroutine(target, originalPos, duration, onComplete, EaseType.EaseOutQuad));
    }

    public int SlideOut(RectTransform target, SlideDirection direction, float duration, Action onComplete = null)
    {
        Vector2 endPos = GetSlideStartPosition(target, direction);
        return StartAnimation(MoveToCoroutine(target, endPos, duration, onComplete, EaseType.EaseInQuad));
    }

    Vector2 GetSlideStartPosition(RectTransform target, SlideDirection direction)
    {
        Vector2 pos = target.anchoredPosition;
        float offset = 500f;

        switch (direction)
        {
            case SlideDirection.Left: return pos + Vector2.left * offset;
            case SlideDirection.Right: return pos + Vector2.right * offset;
            case SlideDirection.Up: return pos + Vector2.up * offset;
            case SlideDirection.Down: return pos + Vector2.down * offset;
        }
        return pos;
    }

    IEnumerator MoveToCoroutine(RectTransform target, Vector2 to, float duration, Action onComplete, EaseType ease = EaseType.EaseOutQuad)
    {
        Vector2 from = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = ApplyEase(elapsed / duration, ease);
            target.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        target.anchoredPosition = to;
        onComplete?.Invoke();
    }

    // Renk animasyonlari
    public int ColorTo(Graphic target, Color to, float duration, Action onComplete = null)
    {
        return StartAnimation(ColorToCoroutine(target, to, duration, onComplete));
    }

    public int ColorPulse(Graphic target, Color pulseColor, float duration, int loops = 1, Action onComplete = null)
    {
        return StartAnimation(ColorPulseCoroutine(target, pulseColor, duration, loops, onComplete));
    }

    IEnumerator ColorToCoroutine(Graphic target, Color to, float duration, Action onComplete)
    {
        Color from = target.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(elapsed / duration);
            target.color = Color.Lerp(from, to, t);
            yield return null;
        }

        target.color = to;
        onComplete?.Invoke();
    }

    IEnumerator ColorPulseCoroutine(Graphic target, Color pulseColor, float duration, int loops, Action onComplete)
    {
        Color originalColor = target.color;

        for (int i = 0; i < loops; i++)
        {
            float elapsed = 0f;
            float halfDuration = duration / 2f;

            // Pulse rengine
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.color = Color.Lerp(originalColor, pulseColor, elapsed / halfDuration);
                yield return null;
            }

            // Geri don
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.color = Color.Lerp(pulseColor, originalColor, elapsed / halfDuration);
                yield return null;
            }
        }

        target.color = originalColor;
        onComplete?.Invoke();
    }

    // Text animasyonlari
    public int CountTo(TextMeshProUGUI target, int from, int to, float duration, string format = "{0}", Action onComplete = null)
    {
        return StartAnimation(CountToCoroutine(target, from, to, duration, format, onComplete));
    }

    public int TypeWriter(TextMeshProUGUI target, string text, float charDelay, Action onComplete = null)
    {
        return StartAnimation(TypeWriterCoroutine(target, text, charDelay, onComplete));
    }

    IEnumerator CountToCoroutine(TextMeshProUGUI target, int from, int to, float duration, string format, Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(elapsed / duration);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            target.text = string.Format(format, current.ToString("N0"));
            yield return null;
        }

        target.text = string.Format(format, to.ToString("N0"));
        onComplete?.Invoke();
    }

    IEnumerator TypeWriterCoroutine(TextMeshProUGUI target, string text, float charDelay, Action onComplete)
    {
        target.text = "";
        foreach (char c in text)
        {
            target.text += c;
            yield return new WaitForSecondsRealtime(charDelay);
        }
        onComplete?.Invoke();
    }

    // Shake animasyonu
    public int Shake(RectTransform target, float intensity, float duration, Action onComplete = null)
    {
        return StartAnimation(ShakeCoroutine(target, intensity, duration, onComplete));
    }

    IEnumerator ShakeCoroutine(RectTransform target, float intensity, float duration, Action onComplete)
    {
        Vector2 originalPos = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentIntensity = intensity * (1f - elapsed / duration);

            float offsetX = UnityEngine.Random.Range(-currentIntensity, currentIntensity);
            float offsetY = UnityEngine.Random.Range(-currentIntensity, currentIntensity);

            target.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
            yield return null;
        }

        target.anchoredPosition = originalPos;
        onComplete?.Invoke();
    }

    // === YARDIMCI METODLAR ===

    int StartAnimation(IEnumerator animation)
    {
        int id = ++animationIdCounter;
        Coroutine coroutine = StartCoroutine(animation);
        activeAnimations[id] = coroutine;
        return id;
    }

    public void StopAnimation(int id)
    {
        if (activeAnimations.TryGetValue(id, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
            activeAnimations.Remove(id);
        }
    }

    public void StopAllAnimations()
    {
        StopAllCoroutines();
        activeAnimations.Clear();
    }

    // === EASING FONKSIYONLARI ===

    public enum EaseType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseOutBack,
        EaseInBack,
        EaseOutElastic,
        EaseOutBounce
    }

    float ApplyEase(float t, EaseType ease)
    {
        switch (ease)
        {
            case EaseType.Linear: return t;
            case EaseType.EaseInQuad: return EaseInQuad(t);
            case EaseType.EaseOutQuad: return EaseOutQuad(t);
            case EaseType.EaseInOutQuad: return EaseInOutQuad(t);
            case EaseType.EaseOutBack: return EaseOutBack(t);
            case EaseType.EaseInBack: return EaseInBack(t);
            case EaseType.EaseOutElastic: return EaseOutElastic(t);
            case EaseType.EaseOutBounce: return EaseOutBounce(t);
            default: return t;
        }
    }

    float EaseInQuad(float t) => t * t;
    float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    float EaseOutElastic(float t)
    {
        const float c4 = (2f * Mathf.PI) / 3f;
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        else if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        else if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        else
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }
}

public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}
