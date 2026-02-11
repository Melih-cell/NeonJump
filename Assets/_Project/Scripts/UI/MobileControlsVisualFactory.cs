using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Prosedural sprite fabrikasi - mobil kontroller icin gorsel ogeleri runtime'da uretir.
/// Tum sprite'lar dictionary cache'de saklanir.
/// </summary>
public static class MobileControlsVisualFactory
{
    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    // Neon renk paleti
    public static readonly Color NeonCyan = new Color(0f, 0.85f, 1f, 0.9f);
    public static readonly Color NeonGreen = new Color(0f, 1f, 0.5f, 0.9f);
    public static readonly Color NeonOrange = new Color(1f, 0.5f, 0f, 0.9f);
    public static readonly Color NeonYellow = new Color(1f, 1f, 0f, 0.9f);
    public static readonly Color NeonPink = new Color(1f, 0f, 0.6f, 0.9f);
    public static readonly Color DarkBg = new Color(0.05f, 0.05f, 0.12f, 0.55f);
    public static readonly Color DarkBgSolid = new Color(0.05f, 0.05f, 0.12f, 0.7f);

    // === JOYSTICK ===

    /// <summary>
    /// Koyu daire + neon kenar - joystick arka plani
    /// </summary>
    public static Sprite CreateJoystickBackground(int size = 128)
    {
        string key = $"joystick_bg_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR - 3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= innerR)
                {
                    // Ic dolgu - koyu arka plan, merkeze dogru hafif gradient
                    float t = dist / innerR;
                    float alpha = Mathf.Lerp(0.5f, 0.6f, t);
                    pixels[y * size + x] = new Color(0.05f, 0.05f, 0.12f, alpha);
                }
                else if (dist <= outerR)
                {
                    // Neon kenar - parlak cyan
                    float edgeFade = 1f - (dist - innerR) / (outerR - innerR);
                    pixels[y * size + x] = new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.7f * edgeFade);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        // 4 yon noktasi (kucuk parlak noktalar)
        DrawDot(pixels, size, (int)center, (int)(center + innerR - 8), NeonCyan * 0.5f, 2);
        DrawDot(pixels, size, (int)center, (int)(center - innerR + 8), NeonCyan * 0.5f, 2);
        DrawDot(pixels, size, (int)(center + innerR - 8), (int)center, NeonCyan * 0.5f, 2);
        DrawDot(pixels, size, (int)(center - innerR + 8), (int)center, NeonCyan * 0.5f, 2);

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Kucuk parlak daire - joystick handle
    /// </summary>
    public static Sprite CreateJoystickHandle(int size = 48)
    {
        string key = $"joystick_handle_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= innerR)
                {
                    // Parlak ic kisim - hafif gradient
                    float t = dist / innerR;
                    Color c = Color.Lerp(
                        new Color(0.6f, 0.95f, 1f, 0.95f),
                        new Color(0.1f, 0.7f, 0.85f, 0.85f),
                        t
                    );
                    pixels[y * size + x] = c;
                }
                else if (dist <= outerR)
                {
                    // Dis kenar glow
                    float edgeFade = 1f - (dist - innerR) / (outerR - innerR);
                    pixels[y * size + x] = new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.6f * edgeFade);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        // Merkez highlight
        DrawDot(pixels, size, (int)(center - 2), (int)(center + 2), new Color(1f, 1f, 1f, 0.6f), 3);

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Neon halka - aktif durum glow efekti
    /// </summary>
    public static Sprite CreateJoystickGlowRing(int size = 128)
    {
        string key = $"joystick_glow_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR - 6f;

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
                    pixels[y * size + x] = new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.8f * t * t);
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
    /// Buton arka plan dairesi - koyu + neon kenar
    /// </summary>
    public static Sprite CreateButtonCircle(int size = 64, Color borderColor = default)
    {
        if (borderColor == default) borderColor = NeonCyan;

        string key = $"btn_circle_{size}_{ColorKey(borderColor)}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center - 1f;
        float borderR = outerR - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= borderR)
                {
                    pixels[y * size + x] = DarkBgSolid;
                }
                else if (dist <= outerR)
                {
                    float edgeFade = 1f - (dist - borderR) / (outerR - borderR);
                    pixels[y * size + x] = new Color(borderColor.r, borderColor.g, borderColor.b, 0.8f * edgeFade);
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
    /// Basili durum parlak kenar dairesi
    /// </summary>
    public static Sprite CreateButtonGlowCircle(int size = 64, Color glowColor = default)
    {
        if (glowColor == default) glowColor = NeonCyan;

        string key = $"btn_glow_{size}_{ColorKey(glowColor)}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size * 0.5f;
        float outerR = center;
        float innerR = outerR - 5f;

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
                    pixels[y * size + x] = new Color(glowColor.r, glowColor.g, glowColor.b, 0.9f * t);
                }
                else if (dist < innerR)
                {
                    // Hafif ic dolgu
                    float t = dist / innerR;
                    pixels[y * size + x] = new Color(glowColor.r, glowColor.g, glowColor.b, 0.15f * (1f - t));
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
    /// Yukari ok ikonu - Ziplama
    /// </summary>
    public static Sprite CreateArrowUpIcon(int size = 32)
    {
        string key = $"icon_arrow_up_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int cx = size / 2;
        int margin = size / 6;

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
        int bodyWidth = size / 6;
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
    /// Yildirim ikonu - Dash
    /// </summary>
    public static Sprite CreateLightningIcon(int size = 32)
    {
        string key = $"icon_lightning_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        // Yildirim zigzag cizimi
        int margin = size / 6;
        int w = size / 4;

        // Ust kisim - genis ucgen asagi
        int topY = size - margin;
        int midY = size / 2 + 2;
        for (int y = midY; y <= topY; y++)
        {
            float t = (float)(y - midY) / (topY - midY);
            int left = Mathf.RoundToInt(Mathf.Lerp(size / 2f - 1, margin, t));
            int right = Mathf.RoundToInt(Mathf.Lerp(size / 2f + w, size / 2f + 1, t));
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
            int left = Mathf.RoundToInt(Mathf.Lerp(size / 2f - 1, size / 2f - w, t));
            int right = Mathf.RoundToInt(Mathf.Lerp(size / 2f + 1, size / 2f + 1, t));
            for (int x = left; x <= right; x++)
            {
                if (x >= 0 && x < size)
                    pixels[y * size + x] = white;
            }
        }

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Nisangah ikonu - Ates
    /// </summary>
    public static Sprite CreateCrosshairIcon(int size = 32)
    {
        string key = $"icon_crosshair_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int cx = size / 2;
        int cy = size / 2;
        int r = size / 3;
        int lineW = Mathf.Max(1, size / 16);

        // Dis daire
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (Mathf.Abs(dist - r) < 1.5f)
                {
                    pixels[y * size + x] = white;
                }
            }
        }

        // Artı cizgileri (merkezde bosluk)
        int gap = size / 8;
        int lineLen = size / 4;

        // Yatay cizgiler
        for (int lw = -lineW / 2; lw <= lineW / 2; lw++)
        {
            for (int x = cx - r - lineLen / 2; x < cx - gap; x++)
                if (x >= 0 && x < size && cy + lw >= 0 && cy + lw < size)
                    pixels[(cy + lw) * size + x] = white;

            for (int x = cx + gap + 1; x <= cx + r + lineLen / 2; x++)
                if (x >= 0 && x < size && cy + lw >= 0 && cy + lw < size)
                    pixels[(cy + lw) * size + x] = white;
        }

        // Dikey cizgiler
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
        DrawDot(pixels, size, cx, cy, white, 1);

        return CacheSprite(key, tex, pixels, size);
    }

    /// <summary>
    /// Daire ok (reload) ikonu
    /// </summary>
    public static Sprite CreateReloadIcon(int size = 32)
    {
        string key = $"icon_reload_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int cx = size / 2;
        int cy = size / 2;
        int r = size / 3;

        // 270 derece arc (saat yonunde, sag usttte bosluk)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                // 45-360 arasi ciz (sag ustte bosluk birak)
                if (Mathf.Abs(dist - r) < 1.5f && (angle > 50f || angle < 5f))
                {
                    pixels[y * size + x] = white;
                }
            }
        }

        // Ok ucu (sag ustte)
        int arrowX = cx + (int)(r * Mathf.Cos(50f * Mathf.Deg2Rad));
        int arrowY = cy + (int)(r * Mathf.Sin(50f * Mathf.Deg2Rad));
        // Kucuk ok ucgen
        for (int i = 0; i < 4; i++)
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

    /// <summary>
    /// Cift ok ikonu - Silah degistir
    /// </summary>
    public static Sprite CreateSwitchIcon(int size = 32)
    {
        string key = $"icon_switch_{size}";
        if (_cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color white = Color.white;

        int margin = size / 5;
        int lineH = Mathf.Max(2, size / 10);

        // Ust yatay cizgi (saga ok)
        int topY = size * 2 / 3;
        for (int y = topY - lineH / 2; y <= topY + lineH / 2; y++)
        {
            for (int x = margin; x < size - margin; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = white;
            }
        }
        // Sag ok ucu
        for (int i = 0; i < 4; i++)
        {
            for (int j = -i; j <= i; j++)
            {
                int px = size - margin - i;
                int py = topY + j;
                if (px >= 0 && px < size && py >= 0 && py < size)
                    pixels[py * size + px] = white;
            }
        }

        // Alt yatay cizgi (sola ok)
        int botY = size / 3;
        for (int y = botY - lineH / 2; y <= botY + lineH / 2; y++)
        {
            for (int x = margin; x < size - margin; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = white;
            }
        }
        // Sol ok ucu
        for (int i = 0; i < 4; i++)
        {
            for (int j = -i; j <= i; j++)
            {
                int px = margin + i;
                int py = botY + j;
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
