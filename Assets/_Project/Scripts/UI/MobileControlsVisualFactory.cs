using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Prosedural sprite fabrikasi - mobil kontroller icin doga temasina uygun gorsel ogeler.
/// Tum sprite'lar dictionary cache'de saklanir.
/// Yumusak renkler, soft gradient, buzlu cam efekti.
/// </summary>
public static class MobileControlsVisualFactory
{
    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    // Doga renk paleti
    public static readonly Color ThemeGreen = new Color(0.35f, 0.75f, 0.45f, 0.85f);   // Yesil (Jump)
    public static readonly Color ThemeAmber = new Color(0.92f, 0.68f, 0.21f, 0.85f);   // Amber (Fire)
    public static readonly Color ThemeSky = new Color(0.45f, 0.72f, 0.88f, 0.85f);     // Gok mavisi (Dash)
    public static readonly Color ThemeRose = new Color(0.85f, 0.42f, 0.52f, 0.85f);    // Gul (Roll)
    public static readonly Color DarkBg = new Color(0.12f, 0.12f, 0.12f, 0.45f);       // Seffaf koyu
    public static readonly Color LightBg = new Color(0.95f, 0.95f, 0.95f, 0.15f);      // Buzlu cam

    // === JOYSTICK ===

    /// <summary>
    /// Yumusak koyu daire + ince beyaz kenarlik - joystick arka plani
    /// </summary>
    public static Sprite CreateJoystickBackground(int size = 256)
    {
        string key = $"joystick_bg_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 2f;
        float borderWidth = 2f;
        float innerR = outerR - borderWidth;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= innerR)
                {
                    // Ic dolgu - yumusak koyu, merkeze dogru hafif acilma
                    float t = dist / innerR;
                    float alpha = Mathf.Lerp(0.38f, 0.48f, t);
                    pixels[y * size + x] = new Color(0.12f, 0.12f, 0.12f, alpha);
                }
                else if (dist <= outerR)
                {
                    // Ince beyaz kenarlik
                    float edgeT = (dist - innerR) / borderWidth;
                    float aa = Mathf.Clamp01(outerR - dist);
                    float alpha = 0.35f * (1f - edgeT * 0.5f) * Mathf.Clamp01(aa + 0.5f);
                    pixels[y * size + x] = new Color(0.95f, 0.95f, 0.95f, alpha);
                }
                else if (dist <= outerR + 1.5f)
                {
                    // AA fringe
                    float aa = Mathf.Clamp01(outerR + 1.5f - dist);
                    pixels[y * size + x] = new Color(0.95f, 0.95f, 0.95f, 0.08f * aa);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        // 4 yon noktasi (kucuk soluk beyaz noktalar)
        int dotOffset = Mathf.RoundToInt(innerR - size * 0.08f);
        int dotRadius = Mathf.Max(2, size / 80);
        Color dotColor = new Color(0.9f, 0.9f, 0.9f, 0.25f);
        DrawDot(pixels, size, (int)center, (int)(center + dotOffset), dotColor, dotRadius);
        DrawDot(pixels, size, (int)center, (int)(center - dotOffset), dotColor, dotRadius);
        DrawDot(pixels, size, (int)(center + dotOffset), (int)center, dotColor, dotRadius);
        DrawDot(pixels, size, (int)(center - dotOffset), (int)center, dotColor, dotRadius);

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Parlak handle noktasi - joystick handle
    /// </summary>
    public static Sprite CreateJoystickHandle(int size = 256)
    {
        string key = $"joystick_handle_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 2f;
        float innerR = outerR * 0.75f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= innerR)
                {
                    // Parlak ic kisim - beyaz/acik gri gradient
                    float t = dist / innerR;
                    Color c = Color.Lerp(
                        new Color(0.98f, 0.98f, 1f, 0.92f),
                        new Color(0.75f, 0.78f, 0.82f, 0.78f),
                        t * t
                    );
                    pixels[y * size + x] = c;
                }
                else if (dist <= outerR)
                {
                    // Yumusak dis kenar
                    float edgeT = (dist - innerR) / (outerR - innerR);
                    float alpha = Mathf.Lerp(0.78f, 0f, edgeT * edgeT);
                    pixels[y * size + x] = new Color(0.8f, 0.82f, 0.85f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        // Merkez parlak highlight noktasi
        int hlRadius = Mathf.Max(3, size / 12);
        int hlOffset = Mathf.Max(2, size / 20);
        DrawSoftDot(pixels, size, (int)(center - hlOffset), (int)(center + hlOffset),
            new Color(1f, 1f, 1f, 0.5f), hlRadius);

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Ince beyaz halka - aktif durum glow efekti (neon yerine yumusak beyaz)
    /// </summary>
    public static Sprite CreateJoystickGlowRing(int size = 256)
    {
        string key = $"joystick_glow_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist >= innerR && dist <= outerR)
                {
                    float mid = (innerR + outerR) * 0.5f;
                    float t = 1f - Mathf.Abs(dist - mid) / ((outerR - innerR) * 0.5f);
                    pixels[y * size + x] = new Color(0.95f, 0.95f, 0.95f, 0.55f * t * t);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    // === BUTONLAR ===

    /// <summary>
    /// Yuvarlak buton - renkli arka plan, soft gradient, ince beyaz kenarlik
    /// </summary>
    public static Sprite CreateButtonCircle(int size = 256, Color themeColor = default)
    {
        if (themeColor == default) themeColor = ThemeGreen;

        string key = $"btn_circle_{size}_{ColorKey(themeColor)}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 2f;
        float borderWidth = 2f;
        float innerR = outerR - borderWidth;

        // Koyu versiyon (gradient icin)
        Color darkVariant = new Color(
            themeColor.r * 0.5f,
            themeColor.g * 0.5f,
            themeColor.b * 0.5f,
            themeColor.a
        );

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= innerR)
                {
                    // Soft gradient dolgu - merkezden kenara koyulasan
                    float t = dist / innerR;
                    // Ust kisim daha acik (isik efekti)
                    float lightBias = Mathf.Clamp01(0.5f + (dy / innerR) * 0.3f);
                    Color baseColor = Color.Lerp(themeColor, darkVariant, t * 0.4f + lightBias * 0.2f);
                    baseColor.a = themeColor.a * 0.85f;
                    pixels[y * size + x] = baseColor;
                }
                else if (dist <= outerR)
                {
                    // Ince beyaz kenarlik
                    float edgeT = (dist - innerR) / borderWidth;
                    float aa = Mathf.Clamp01(outerR - dist);
                    float alpha = 0.4f * (1f - edgeT * 0.3f) * Mathf.Clamp01(aa + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else if (dist <= outerR + 1.5f)
                {
                    float aa = Mathf.Clamp01(outerR + 1.5f - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 0.06f * aa);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Basili durum hafif parlama dairesi (neon glow yerine yumusak beyaz)
    /// </summary>
    public static Sprite CreateButtonGlowCircle(int size = 256, Color glowColor = default)
    {
        if (glowColor == default) glowColor = ThemeGreen;

        string key = $"btn_glow_{size}_{ColorKey(glowColor)}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center;
        float innerR = outerR - 4f;

        // Acik versiyon
        Color lightColor = new Color(
            Mathf.Lerp(glowColor.r, 1f, 0.5f),
            Mathf.Lerp(glowColor.g, 1f, 0.5f),
            Mathf.Lerp(glowColor.b, 1f, 0.5f),
            1f
        );

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist >= innerR && dist <= outerR)
                {
                    float mid = (innerR + outerR) * 0.5f;
                    float t = 1f - Mathf.Abs(dist - mid) / ((outerR - innerR) * 0.5f);
                    pixels[y * size + x] = new Color(lightColor.r, lightColor.g, lightColor.b, 0.6f * t);
                }
                else if (dist < innerR)
                {
                    // Hafif ic parlama
                    float t = dist / innerR;
                    pixels[y * size + x] = new Color(lightColor.r, lightColor.g, lightColor.b, 0.1f * (1f - t));
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    // === IKONLAR ===

    /// <summary>
    /// Yukari ok ikonu - Ziplama (beyaz, temiz flat design)
    /// </summary>
    public static Sprite CreateArrowUpIcon(int size = 128)
    {
        string key = $"icon_arrow_up_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int cx = size / 2;
        int margin = size / 6;
        int thickness = Mathf.Max(2, size / 16);

        // Ok ucu (ucgen)
        int tipY = size - margin - 1;
        int baseY = size / 2;
        for (int y = baseY; y <= tipY; y++)
        {
            float t = (float)(y - baseY) / (tipY - baseY);
            int halfWidth = Mathf.RoundToInt(Mathf.Lerp(size / 3f, 0f, t));
            for (int x = cx - halfWidth; x <= cx + halfWidth; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = white;
            }
        }

        // Ok govdesi (dikdortgen)
        int bodyWidth = size / 5;
        for (int y = margin; y < baseY; y++)
        {
            for (int x = cx - bodyWidth; x <= cx + bodyWidth; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = white;
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Yildirim ikonu - Dash (beyaz, temiz flat design)
    /// </summary>
    public static Sprite CreateLightningIcon(int size = 128)
    {
        string key = $"icon_lightning_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int margin = size / 6;
        int w = size / 3;

        // Ust kisim - genis ucgen asagi
        int topY = size - margin;
        int midY = size / 2 + 2;
        for (int y = midY; y <= topY; y++)
        {
            float t = (float)(y - midY) / (topY - midY);
            int left = Mathf.RoundToInt(Mathf.Lerp(size / 2f - 2, margin, t));
            int right = Mathf.RoundToInt(Mathf.Lerp(size / 2f + w, size / 2f + 2, t));
            for (int x = left; x <= right; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = white;
            }
        }

        // Alt kisim - dar ucgen asagi
        int botY = margin;
        for (int y = botY; y <= midY; y++)
        {
            float t = (float)(y - botY) / (midY - botY);
            int left = Mathf.RoundToInt(Mathf.Lerp(size / 2f - 2, size / 2f - w, t));
            int right = Mathf.RoundToInt(Mathf.Lerp(size / 2f + 2, size / 2f + 2, t));
            for (int x = left; x <= right; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = white;
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Nisangah ikonu - Ates (beyaz, temiz flat design)
    /// </summary>
    public static Sprite CreateCrosshairIcon(int size = 128)
    {
        string key = $"icon_crosshair_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int cx = size / 2;
        int cy = size / 2;
        int r = size / 3;
        int lineW = Mathf.Max(2, size / 8);

        // Dis daire
        float ringThickness = Mathf.Max(2f, size / 16f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (Mathf.Abs(dist - r) < ringThickness)
                {
                    pixels[y * size + x] = white;
                }
            }
        }

        // Arti cizgileri (merkezde bosluk)
        int gap = size / 8;
        int lineLen = size / 4;

        for (int lw = -lineW / 2; lw <= lineW / 2; lw++)
        {
            for (int x = cx - r - lineLen / 2; x < cx - gap; x++)
                if (x >= 0 && x < size && cy + lw >= 0 && cy + lw < size)
                    pixels[(cy + lw) * size + x] = white;

            for (int x = cx + gap + 1; x <= cx + r + lineLen / 2; x++)
                if (x >= 0 && x < size && cy + lw >= 0 && cy + lw < size)
                    pixels[(cy + lw) * size + x] = white;
        }

        for (int lw = -lineW / 2; lw <= lineW / 2; lw++)
        {
            for (int y = cy - r - lineLen / 2; y < cy - gap; y++)
                if (y >= 0 && y < size && cx + lw >= 0 && cx + lw < size)
                    pixels[y * size + (cx + lw)] = white;

            for (int y = cy + gap + 1; y <= cy + r + lineLen / 2; y++)
                if (y >= 0 && y < size && cx + lw >= 0 && cx + lw < size)
                    pixels[y * size + (cx + lw)] = white;
        }

        // Merkez nokta
        int dotR = Mathf.Max(2, size / 24);
        DrawDot(pixels, size, cx, cy, white, dotR);

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Takla/Roll ikonu - yuvarlanma hareket cizgileri (beyaz, flat design)
    /// </summary>
    public static Sprite CreateRollIcon(int size = 128)
    {
        string key = $"icon_roll_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int cx = size / 2;
        int cy = size / 2;
        int r = size / 3;

        // 270 derece arc (dairesel ok)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                if (Mathf.Abs(dist - r) < 1.8f && (angle > 50f || angle < 5f))
                {
                    pixels[y * size + x] = white;
                }
            }
        }

        // Ok ucu
        int arrowX = cx + (int)(r * Mathf.Cos(50f * Mathf.Deg2Rad));
        int arrowY = cy + (int)(r * Mathf.Sin(50f * Mathf.Deg2Rad));
        for (int i = 0; i < 5; i++)
        {
            for (int j = -i; j <= i; j++)
            {
                int px = arrowX + j;
                int py = arrowY + i;
                if (px >= 0 && px < size && py >= 0 && py < size)
                    pixels[py * size + px] = white;
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    // === COOLDOWN ===

    /// <summary>
    /// Radial cooldown dolgusu icin tam beyaz daire (Image.Filled ile kullanilir)
    /// </summary>
    public static Sprite CreateRadialFillCircle(int size = 64)
    {
        string key = $"radial_fill_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    // === YARDIMCI FONKSIYONLAR ===

    private static void DrawDot(Color[] pixels, int texSize, int cx, int cy, Color color, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= radius * radius)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                    {
                        pixels[py * texSize + px] = color;
                    }
                }
            }
        }
    }

    private static void DrawSoftDot(Color[] pixels, int texSize, int cx, int cy, Color color, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                    {
                        float falloff = 1f - (dist / radius);
                        Color c = color;
                        c.a *= falloff * falloff;
                        // Alpha blending
                        Color existing = pixels[py * texSize + px];
                        float outA = c.a + existing.a * (1f - c.a);
                        if (outA > 0f)
                        {
                            pixels[py * texSize + px] = new Color(
                                (c.r * c.a + existing.r * existing.a * (1f - c.a)) / outA,
                                (c.g * c.a + existing.g * existing.a * (1f - c.a)) / outA,
                                (c.b * c.a + existing.b * existing.a * (1f - c.a)) / outA,
                                outA
                            );
                        }
                    }
                }
            }
        }
    }

    private static string ColorKey(Color c)
    {
        return $"{(int)(c.r * 255)}_{(int)(c.g * 255)}_{(int)(c.b * 255)}";
    }

    private static Sprite CacheSprite(string key, Texture2D tex, Color[] pixels, int size)
    {
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = key;
        _cache[key] = sprite;
        return sprite;
    }

    /// <summary>
    /// Tum cache'i temizle
    /// </summary>
    public static void ClearCache()
    {
        foreach (var kvp in _cache)
        {
            if (kvp.Value != null && kvp.Value.texture != null)
            {
                Object.Destroy(kvp.Value.texture);
                Object.Destroy(kvp.Value);
            }
        }
        _cache.Clear();
    }
}
