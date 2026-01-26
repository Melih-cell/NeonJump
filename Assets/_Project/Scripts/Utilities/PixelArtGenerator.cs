using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pixel Adventure kalitesinde sprite'lar olusturur.
/// Harici asset indirmeye gerek kalmadan tam oyun grafikleri.
/// </summary>
public class PixelArtGenerator : MonoBehaviour
{
    public static PixelArtGenerator Instance { get; private set; }

    // Cached sprites
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite[]> animationCache = new Dictionary<string, Sprite[]>();

    // Color palettes
    public static class Palette
    {
        // Character colors
        public static Color MaskDudeBody = new Color(0.95f, 0.65f, 0.45f); // Bej/ten
        public static Color MaskDudeMask = new Color(0.2f, 0.7f, 0.4f); // Yesil maske
        public static Color NinjaFrogBody = new Color(0.4f, 0.75f, 0.35f); // Yesil
        public static Color PinkManBody = new Color(0.95f, 0.5f, 0.7f); // Pembe
        public static Color VirtualGuyBody = new Color(0.3f, 0.6f, 0.9f); // Mavi

        // Enemy colors
        public static Color SlimeGreen = new Color(0.3f, 0.8f, 0.3f);
        public static Color SlimeBlue = new Color(0.3f, 0.5f, 0.9f);
        public static Color MushroomRed = new Color(0.9f, 0.25f, 0.2f);
        public static Color MushroomCap = new Color(0.95f, 0.95f, 0.9f);
        public static Color GhostWhite = new Color(0.85f, 0.85f, 0.95f);
        public static Color BatPurple = new Color(0.4f, 0.25f, 0.5f);
        public static Color SkullWhite = new Color(0.95f, 0.93f, 0.88f);

        // Terrain colors
        public static Color GrassTop = new Color(0.4f, 0.75f, 0.3f);
        public static Color DirtLight = new Color(0.65f, 0.45f, 0.3f);
        public static Color DirtDark = new Color(0.5f, 0.35f, 0.22f);
        public static Color Stone = new Color(0.55f, 0.55f, 0.6f);

        // Item colors
        public static Color Apple = new Color(0.9f, 0.2f, 0.2f);
        public static Color Banana = new Color(1f, 0.9f, 0.3f);
        public static Color Cherry = new Color(0.85f, 0.15f, 0.25f);
        public static Color Orange = new Color(1f, 0.6f, 0.1f);
        public static Color CoinGold = new Color(1f, 0.85f, 0.2f);

        // UI colors
        public static Color HeartRed = new Color(0.9f, 0.2f, 0.25f);
        public static Color HeartPink = new Color(1f, 0.5f, 0.55f);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region CHARACTER SPRITES

    public Sprite[] GeneratePlayerIdle(Color bodyColor, Color maskColor)
    {
        string key = $"PlayerIdle_{bodyColor}_{maskColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[11];
        for (int i = 0; i < 11; i++)
        {
            float breathOffset = Mathf.Sin(i * 0.57f) * 0.5f;
            frames[i] = CreatePlayerSprite(bodyColor, maskColor, 0, breathOffset, false);
        }

        animationCache[key] = frames;
        return frames;
    }

    public Sprite[] GeneratePlayerRun(Color bodyColor, Color maskColor)
    {
        string key = $"PlayerRun_{bodyColor}_{maskColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[12];
        for (int i = 0; i < 12; i++)
        {
            float legPhase = i / 12f * Mathf.PI * 2f;
            frames[i] = CreatePlayerSprite(bodyColor, maskColor, legPhase, 0, true);
        }

        animationCache[key] = frames;
        return frames;
    }

    public Sprite[] GeneratePlayerJump(Color bodyColor, Color maskColor)
    {
        string key = $"PlayerJump_{bodyColor}_{maskColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[1];
        frames[0] = CreatePlayerJumpSprite(bodyColor, maskColor, true);

        animationCache[key] = frames;
        return frames;
    }

    public Sprite[] GeneratePlayerFall(Color bodyColor, Color maskColor)
    {
        string key = $"PlayerFall_{bodyColor}_{maskColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[1];
        frames[0] = CreatePlayerJumpSprite(bodyColor, maskColor, false);

        animationCache[key] = frames;
        return frames;
    }

    public Sprite[] GeneratePlayerDoubleJump(Color bodyColor, Color maskColor)
    {
        string key = $"PlayerDoubleJump_{bodyColor}_{maskColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            frames[i] = CreatePlayerDoubleJumpSprite(bodyColor, maskColor, i);
        }

        animationCache[key] = frames;
        return frames;
    }

    Sprite CreatePlayerSprite(Color body, Color mask, float legPhase, float breathOffset, bool running)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        // Temizle
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color outline = new Color(body.r * 0.4f, body.g * 0.4f, body.b * 0.4f);
        Color maskOutline = new Color(mask.r * 0.5f, mask.g * 0.5f, mask.b * 0.5f);
        Color highlight = new Color(
            Mathf.Min(1, body.r + 0.2f),
            Mathf.Min(1, body.g + 0.2f),
            Mathf.Min(1, body.b + 0.2f)
        );

        int bodyY = 4 + (int)breathOffset;

        // Bacaklar
        int leftLegX = 11;
        int rightLegX = 18;
        int leftLegOffset = running ? (int)(Mathf.Sin(legPhase) * 3) : 0;
        int rightLegOffset = running ? (int)(Mathf.Sin(legPhase + Mathf.PI) * 3) : 0;

        // Sol bacak
        for (int y = bodyY - 4 + leftLegOffset; y < bodyY; y++)
        {
            if (y >= 0 && y < size)
            {
                pixels[y * size + leftLegX] = outline;
                pixels[y * size + leftLegX + 1] = body;
                pixels[y * size + leftLegX + 2] = body;
                pixels[y * size + leftLegX + 3] = outline;
            }
        }

        // Sag bacak
        for (int y = bodyY - 4 + rightLegOffset; y < bodyY; y++)
        {
            if (y >= 0 && y < size)
            {
                pixels[y * size + rightLegX] = outline;
                pixels[y * size + rightLegX + 1] = body;
                pixels[y * size + rightLegX + 2] = body;
                pixels[y * size + rightLegX + 3] = outline;
            }
        }

        // Govde (oval)
        for (int y = bodyY; y < bodyY + 16; y++)
        {
            if (y >= size) continue;
            float t = (y - bodyY) / 16f;
            int width = (int)(12 * Mathf.Sin(t * Mathf.PI) + 4);
            int startX = 16 - width / 2;

            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= size) continue;

                bool isEdge = x == startX || x == startX + width - 1 || y == bodyY || y == bodyY + 15;
                if (isEdge)
                    pixels[y * size + x] = outline;
                else if (x < startX + 3)
                    pixels[y * size + x] = body;
                else if (x > startX + width - 4)
                    pixels[y * size + x] = body;
                else
                    pixels[y * size + x] = highlight;
            }
        }

        // Maske (yuz bolgesinde)
        int maskY = bodyY + 8;
        for (int y = maskY; y < maskY + 7; y++)
        {
            if (y >= size) continue;
            for (int x = 10; x < 22; x++)
            {
                bool isEdge = x == 10 || x == 21 || y == maskY || y == maskY + 6;
                if (isEdge)
                    pixels[y * size + x] = maskOutline;
                else
                    pixels[y * size + x] = mask;
            }
        }

        // Gozler (maske uzerinde)
        int eyeY = maskY + 3;
        // Sol goz
        pixels[eyeY * size + 12] = Color.white;
        pixels[eyeY * size + 13] = Color.white;
        pixels[(eyeY + 1) * size + 12] = Color.white;
        pixels[(eyeY + 1) * size + 13] = Color.black;

        // Sag goz
        pixels[eyeY * size + 18] = Color.white;
        pixels[eyeY * size + 19] = Color.white;
        pixels[(eyeY + 1) * size + 18] = Color.black;
        pixels[(eyeY + 1) * size + 19] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), size);
    }

    Sprite CreatePlayerJumpSprite(Color body, Color mask, bool goingUp)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color outline = new Color(body.r * 0.4f, body.g * 0.4f, body.b * 0.4f);
        Color maskOutline = new Color(mask.r * 0.5f, mask.g * 0.5f, mask.b * 0.5f);

        int bodyY = goingUp ? 6 : 2;

        // Bacaklar (kivrik)
        if (goingUp)
        {
            // Yukari cekilmis bacaklar
            for (int x = 10; x < 14; x++)
            {
                pixels[(bodyY - 1) * size + x] = body;
                pixels[(bodyY - 2) * size + x] = outline;
            }
            for (int x = 18; x < 22; x++)
            {
                pixels[(bodyY - 1) * size + x] = body;
                pixels[(bodyY - 2) * size + x] = outline;
            }
        }
        else
        {
            // Asagi uzanmis bacaklar
            for (int y = 0; y < bodyY; y++)
            {
                pixels[y * size + 12] = body;
                pixels[y * size + 13] = outline;
                pixels[y * size + 19] = body;
                pixels[y * size + 20] = outline;
            }
        }

        // Govde
        for (int y = bodyY; y < bodyY + 16; y++)
        {
            if (y >= size) continue;
            float t = (y - bodyY) / 16f;
            int width = (int)(12 * Mathf.Sin(t * Mathf.PI) + 4);
            int startX = 16 - width / 2;

            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= size) continue;
                bool isEdge = x == startX || x == startX + width - 1;
                pixels[y * size + x] = isEdge ? outline : body;
            }
        }

        // Maske
        int maskY = bodyY + 8;
        for (int y = maskY; y < Mathf.Min(maskY + 7, size); y++)
        {
            for (int x = 10; x < 22; x++)
            {
                bool isEdge = x == 10 || x == 21;
                pixels[y * size + x] = isEdge ? maskOutline : mask;
            }
        }

        // Gozler
        int eyeY = maskY + 3;
        if (eyeY + 1 < size)
        {
            pixels[eyeY * size + 12] = Color.white;
            pixels[eyeY * size + 13] = Color.black;
            pixels[eyeY * size + 18] = Color.black;
            pixels[eyeY * size + 19] = Color.white;
        }

        // Kollar (yukari veya asagi)
        if (goingUp)
        {
            // Kollar yukari
            for (int y = bodyY + 10; y < bodyY + 15; y++)
            {
                if (y < size)
                {
                    pixels[y * size + 6] = body;
                    pixels[y * size + 7] = outline;
                    pixels[y * size + 24] = outline;
                    pixels[y * size + 25] = body;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), size);
    }

    Sprite CreatePlayerDoubleJumpSprite(Color body, Color mask, int frame)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        float rotation = frame * 60f * Mathf.Deg2Rad;

        // Donme efekti ile karakter
        Color outline = new Color(body.r * 0.4f, body.g * 0.4f, body.b * 0.4f);

        // Basit donen govde
        int centerX = 16;
        int centerY = 12;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Donen koordinatlar
                float angle = Mathf.Atan2(dy, dx) + rotation;
                float rx = Mathf.Cos(angle) * dist;
                float ry = Mathf.Sin(angle) * dist;

                // Elips kontrol (govde)
                float bodyCheck = (rx * rx) / 36f + (ry * ry) / 64f;
                if (bodyCheck < 1f)
                {
                    if (bodyCheck > 0.85f)
                        pixels[y * size + x] = outline;
                    else if (ry > 2) // Ust kisim (maske)
                        pixels[y * size + x] = mask;
                    else
                        pixels[y * size + x] = body;
                }
            }
        }

        // Spin efekti cizgileri
        Color spinColor = new Color(1f, 1f, 1f, 0.5f);
        for (int i = 0; i < 8; i++)
        {
            float a = rotation + i * Mathf.PI / 4f;
            int px = centerX + (int)(Mathf.Cos(a) * 12);
            int py = centerY + (int)(Mathf.Sin(a) * 8);
            if (px >= 0 && px < size && py >= 0 && py < size)
            {
                pixels[py * size + px] = spinColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.375f), size);
    }

    #endregion

    #region ENEMY SPRITES

    public Sprite[] GenerateSlimeIdle(Color color)
    {
        string key = $"SlimeIdle_{color}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            float squash = 1f + Mathf.Sin(i * 0.628f) * 0.15f;
            frames[i] = CreateSlimeSprite(color, squash);
        }

        animationCache[key] = frames;
        return frames;
    }

    public Sprite[] GenerateSlimeRun(Color color)
    {
        string key = $"SlimeRun_{color}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            float squash = 0.7f + Mathf.Abs(Mathf.Sin(i * 0.628f)) * 0.5f;
            frames[i] = CreateSlimeSprite(color, squash, i * 0.5f);
        }

        animationCache[key] = frames;
        return frames;
    }

    Sprite CreateSlimeSprite(Color color, float squash, float bounceOffset = 0)
    {
        int size = 24;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color outline = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f);
        Color highlight = new Color(
            Mathf.Min(1, color.r + 0.3f),
            Mathf.Min(1, color.g + 0.3f),
            Mathf.Min(1, color.b + 0.3f),
            0.6f
        );

        int centerX = size / 2;
        int centerY = (int)(size / 3 + bounceOffset);
        float radiusX = 8 / squash;
        float radiusY = 6 * squash;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - centerX) / radiusX;
                float dy = (y - centerY) / radiusY;
                float dist = dx * dx + dy * dy;

                if (dist < 1f)
                {
                    if (dist > 0.75f)
                        pixels[y * size + x] = outline;
                    else if (dx < -0.3f && dy < -0.2f)
                        pixels[y * size + x] = highlight;
                    else
                        pixels[y * size + x] = color;
                }
            }
        }

        // Gozler
        int eyeY = centerY + (int)(radiusY * 0.3f);
        int eyeSize = 2;

        // Sol goz
        for (int ey = 0; ey < eyeSize; ey++)
        {
            for (int ex = 0; ex < eyeSize; ex++)
            {
                int px = centerX - 3 + ex;
                int py = eyeY + ey;
                if (px >= 0 && px < size && py >= 0 && py < size)
                    pixels[py * size + px] = Color.white;
            }
        }
        pixels[(eyeY + 1) * size + centerX - 2] = Color.black;

        // Sag goz
        for (int ey = 0; ey < eyeSize; ey++)
        {
            for (int ex = 0; ex < eyeSize; ex++)
            {
                int px = centerX + 2 + ex;
                int py = eyeY + ey;
                if (px >= 0 && px < size && py >= 0 && py < size)
                    pixels[py * size + px] = Color.white;
            }
        }
        pixels[(eyeY + 1) * size + centerX + 3] = Color.black;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), size);
    }

    public Sprite[] GenerateMushroomIdle(Color capColor)
    {
        string key = $"MushroomIdle_{capColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[14];
        for (int i = 0; i < 14; i++)
        {
            float bounce = Mathf.Sin(i * 0.45f) * 0.5f;
            frames[i] = CreateMushroomSprite(capColor, bounce);
        }

        animationCache[key] = frames;
        return frames;
    }

    public Sprite[] GenerateMushroomRun(Color capColor)
    {
        string key = $"MushroomRun_{capColor}";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[16];
        for (int i = 0; i < 16; i++)
        {
            float bounce = Mathf.Abs(Mathf.Sin(i * 0.4f)) * 2f;
            float legPhase = i * 0.4f;
            frames[i] = CreateMushroomSprite(capColor, bounce, legPhase);
        }

        animationCache[key] = frames;
        return frames;
    }

    Sprite CreateMushroomSprite(Color capColor, float bounce, float legPhase = 0)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color capOutline = new Color(capColor.r * 0.5f, capColor.g * 0.5f, capColor.b * 0.5f);
        Color stem = Palette.MushroomCap;
        Color stemOutline = new Color(0.7f, 0.7f, 0.65f);
        Color spots = Color.white;

        int baseY = 2 + (int)bounce;

        // Bacaklar
        int leftLeg = (int)(Mathf.Sin(legPhase) * 2);
        int rightLeg = (int)(Mathf.Sin(legPhase + Mathf.PI) * 2);

        for (int y = 0; y < baseY; y++)
        {
            // Sol bacak
            pixels[y * size + 11 + leftLeg] = stemOutline;
            pixels[y * size + 12 + leftLeg] = stem;

            // Sag bacak
            pixels[y * size + 19 + rightLeg] = stem;
            pixels[y * size + 20 + rightLeg] = stemOutline;
        }

        // Govde (sapka alti)
        for (int y = baseY; y < baseY + 8; y++)
        {
            if (y >= size) continue;
            int width = 8 + (y - baseY);
            int startX = 16 - width / 2;

            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= size) continue;
                bool isEdge = x == startX || x == startX + width - 1;
                pixels[y * size + x] = isEdge ? stemOutline : stem;
            }
        }

        // Sapka (yarim daire)
        int capY = baseY + 8;
        int capRadius = 10;

        for (int y = capY; y < capY + capRadius; y++)
        {
            if (y >= size) continue;
            float t = (float)(y - capY) / capRadius;
            int width = (int)(capRadius * 2 * Mathf.Sqrt(1 - t * t));
            int startX = 16 - width / 2;

            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= size) continue;
                bool isEdge = x == startX || x == startX + width - 1 || y == capY + capRadius - 1;
                pixels[y * size + x] = isEdge ? capOutline : capColor;
            }
        }

        // Sapka noktalari
        int[] spotX = { 12, 19, 15 };
        int[] spotY = { capY + 4, capY + 5, capY + 7 };
        for (int i = 0; i < 3; i++)
        {
            if (spotY[i] < size && spotX[i] < size)
            {
                pixels[spotY[i] * size + spotX[i]] = spots;
                pixels[spotY[i] * size + spotX[i] + 1] = spots;
                pixels[(spotY[i] + 1) * size + spotX[i]] = spots;
                pixels[(spotY[i] + 1) * size + spotX[i] + 1] = spots;
            }
        }

        // Gozler
        int eyeY = baseY + 4;
        pixels[eyeY * size + 13] = Color.black;
        pixels[eyeY * size + 14] = Color.black;
        pixels[eyeY * size + 18] = Color.black;
        pixels[eyeY * size + 19] = Color.black;

        // Kizgin kaslar
        pixels[(eyeY + 1) * size + 12] = capOutline;
        pixels[(eyeY + 1) * size + 13] = capOutline;
        pixels[(eyeY + 1) * size + 19] = capOutline;
        pixels[(eyeY + 1) * size + 20] = capOutline;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.125f), size);
    }

    public Sprite[] GenerateBatFly()
    {
        string key = "BatFly";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[7];
        for (int i = 0; i < 7; i++)
        {
            float wingAngle = Mathf.Sin(i * 0.9f) * 45f;
            frames[i] = CreateBatSprite(wingAngle);
        }

        animationCache[key] = frames;
        return frames;
    }

    Sprite CreateBatSprite(float wingAngle)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color body = Palette.BatPurple;
        Color outline = new Color(body.r * 0.5f, body.g * 0.5f, body.b * 0.5f);
        Color wing = new Color(body.r * 0.7f, body.g * 0.7f, body.b * 0.7f);

        // Govde (kucuk oval)
        int centerY = 12;
        for (int y = centerY - 4; y < centerY + 4; y++)
        {
            for (int x = 13; x < 19; x++)
            {
                float dy = (y - centerY) / 4f;
                float dx = (x - 16) / 3f;
                if (dx * dx + dy * dy < 1)
                {
                    pixels[y * size + x] = body;
                }
            }
        }

        // Kanatlar
        float rad = wingAngle * Mathf.Deg2Rad;
        float wingY = Mathf.Sin(rad);

        // Sol kanat
        for (int wx = 0; wx < 10; wx++)
        {
            int wy = centerY + (int)(wingY * (10 - wx) * 0.3f);
            int x = 13 - wx;
            if (x >= 0 && wy >= 0 && wy < size)
            {
                pixels[wy * size + x] = wing;
                if (wy + 1 < size) pixels[(wy + 1) * size + x] = wing;
                if (wy - 1 >= 0) pixels[(wy - 1) * size + x] = outline;
            }
        }

        // Sag kanat
        for (int wx = 0; wx < 10; wx++)
        {
            int wy = centerY + (int)(wingY * (10 - wx) * 0.3f);
            int x = 19 + wx;
            if (x < size && wy >= 0 && wy < size)
            {
                pixels[wy * size + x] = wing;
                if (wy + 1 < size) pixels[(wy + 1) * size + x] = wing;
                if (wy - 1 >= 0) pixels[(wy - 1) * size + x] = outline;
            }
        }

        // Gozler
        pixels[(centerY + 1) * size + 14] = new Color(1f, 0.8f, 0f);
        pixels[(centerY + 1) * size + 17] = new Color(1f, 0.8f, 0f);

        // Kulaklar
        pixels[(centerY + 4) * size + 14] = body;
        pixels[(centerY + 5) * size + 14] = outline;
        pixels[(centerY + 4) * size + 17] = body;
        pixels[(centerY + 5) * size + 17] = outline;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.375f), size);
    }

    public Sprite[] GenerateGhostIdle()
    {
        string key = "GhostIdle";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            float wave = Mathf.Sin(i * 0.628f) * 2f;
            frames[i] = CreateGhostSprite(wave);
        }

        animationCache[key] = frames;
        return frames;
    }

    Sprite CreateGhostSprite(float wave)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color body = Palette.GhostWhite;
        Color outline = new Color(0.7f, 0.7f, 0.8f);

        // Ana govde
        for (int y = 8; y < 28; y++)
        {
            float t = (y - 8) / 20f;
            int width;

            if (t < 0.6f)
                width = (int)(14 * Mathf.Sin(t / 0.6f * Mathf.PI / 2f));
            else
                width = 14;

            int startX = 16 - width / 2;

            // Dalga efekti alt kisimda
            if (y > 20)
            {
                float waveOffset = Mathf.Sin((y + wave) * 0.5f) * 2f;
                startX += (int)waveOffset;
            }

            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= size) continue;
                bool isEdge = x == startX || x == startX + width - 1;
                pixels[y * size + x] = isEdge ? outline : body;
            }
        }

        // Alt dalga kesimi
        for (int i = 0; i < 4; i++)
        {
            int peakX = 10 + i * 4 + (int)(Mathf.Sin(wave + i) * 1);
            for (int y = 24; y < 28; y++)
            {
                int cutWidth = (y - 24);
                for (int x = peakX - cutWidth; x <= peakX + cutWidth; x++)
                {
                    if (x >= 0 && x < size)
                        pixels[y * size + x] = Color.clear;
                }
            }
        }

        // Gozler
        for (int ey = 0; ey < 4; ey++)
        {
            for (int ex = 0; ex < 3; ex++)
            {
                // Sol goz
                pixels[(15 + ey) * size + 11 + ex] = Color.black;
                // Sag goz
                pixels[(15 + ey) * size + 18 + ex] = Color.black;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), size);
    }

    #endregion

    #region TERRAIN SPRITES

    public Sprite GenerateTerrainTile(int tileType)
    {
        // tileType: 0=topLeft, 1=top, 2=topRight, 3=left, 4=center, 5=right, 6=bottomLeft, 7=bottom, 8=bottomRight
        string key = $"Terrain_{tileType}";
        if (spriteCache.ContainsKey(key)) return spriteCache[key];

        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        bool hasTop = tileType < 3;
        bool hasBottom = tileType > 5;
        bool hasLeft = tileType % 3 == 0;
        bool hasRight = tileType % 3 == 2;

        Color grass = Palette.GrassTop;
        Color dirtLight = Palette.DirtLight;
        Color dirtDark = Palette.DirtDark;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c;

                // Ust kenar (cimen)
                if (hasTop && y >= size - 4)
                {
                    if (y == size - 1)
                        c = new Color(grass.r * 0.7f, grass.g * 0.7f, grass.b * 0.7f);
                    else if (y >= size - 2)
                        c = grass;
                    else
                        c = new Color(grass.r * 0.85f, grass.g * 0.85f, grass.b * 0.85f);
                }
                // Toprak
                else
                {
                    // Rastgele toprak deseni
                    float noise = Mathf.PerlinNoise(x * 0.5f + tileType * 10, y * 0.5f);
                    c = Color.Lerp(dirtDark, dirtLight, noise);

                    // Kenarlari koyu yap
                    if ((hasLeft && x < 2) || (hasRight && x >= size - 2) ||
                        (hasBottom && y < 2))
                    {
                        c = new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f);
                    }
                }

                pixels[y * size + x] = c;
            }
        }

        // Cimen detaylari (ust kenarda)
        if (hasTop)
        {
            for (int x = 1; x < size - 1; x += 2)
            {
                int grassHeight = Random.Range(1, 3);
                for (int gy = 0; gy < grassHeight; gy++)
                {
                    int py = size - 3 - gy;
                    if (py >= 0)
                    {
                        Color gc = new Color(grass.r * 0.9f, grass.g * 1.1f, grass.b * 0.8f);
                        pixels[py * size + x] = gc;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        spriteCache[key] = sprite;
        return sprite;
    }

    public Sprite[] GetAllTerrainTiles()
    {
        Sprite[] tiles = new Sprite[9];
        for (int i = 0; i < 9; i++)
        {
            tiles[i] = GenerateTerrainTile(i);
        }
        return tiles;
    }

    #endregion

    #region ITEM SPRITES

    public Sprite[] GenerateFruitSprites()
    {
        string key = "Fruits";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] fruits = new Sprite[5];
        fruits[0] = CreateFruitSprite(Palette.Apple, FruitShape.Apple);
        fruits[1] = CreateFruitSprite(Palette.Banana, FruitShape.Banana);
        fruits[2] = CreateFruitSprite(Palette.Cherry, FruitShape.Cherry);
        fruits[3] = CreateFruitSprite(Palette.Orange, FruitShape.Orange);
        fruits[4] = CreateFruitSprite(Palette.CoinGold, FruitShape.Coin);

        animationCache[key] = fruits;
        return fruits;
    }

    enum FruitShape { Apple, Banana, Cherry, Orange, Coin }

    Sprite CreateFruitSprite(Color color, FruitShape shape)
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color outline = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f);
        Color highlight = new Color(
            Mathf.Min(1, color.r + 0.3f),
            Mathf.Min(1, color.g + 0.3f),
            Mathf.Min(1, color.b + 0.3f)
        );

        switch (shape)
        {
            case FruitShape.Apple:
            case FruitShape.Orange:
                // Yuvarlak meyve
                for (int y = 2; y < 14; y++)
                {
                    for (int x = 3; x < 13; x++)
                    {
                        float dx = (x - 8) / 5f;
                        float dy = (y - 8) / 6f;
                        float dist = dx * dx + dy * dy;

                        if (dist < 1f)
                        {
                            if (dist > 0.8f)
                                pixels[y * size + x] = outline;
                            else if (dx < -0.2f && dy < -0.2f)
                                pixels[y * size + x] = highlight;
                            else
                                pixels[y * size + x] = color;
                        }
                    }
                }
                // Sap
                pixels[13 * size + 8] = new Color(0.4f, 0.25f, 0.1f);
                pixels[14 * size + 8] = new Color(0.4f, 0.25f, 0.1f);
                // Yaprak
                pixels[14 * size + 9] = new Color(0.3f, 0.6f, 0.2f);
                pixels[14 * size + 10] = new Color(0.3f, 0.6f, 0.2f);
                break;

            case FruitShape.Banana:
                // Muz sekli (egri)
                for (int x = 3; x < 13; x++)
                {
                    int y = 8 + (int)(Mathf.Sin((x - 3) / 10f * Mathf.PI) * 4);
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (y + dy >= 0 && y + dy < size)
                        {
                            pixels[(y + dy) * size + x] = (dy == 0) ? color : outline;
                        }
                    }
                }
                break;

            case FruitShape.Cherry:
                // Iki kiraz
                for (int cx = 0; cx < 2; cx++)
                {
                    int centerX = 5 + cx * 6;
                    for (int y = 3; y < 11; y++)
                    {
                        for (int x = centerX - 3; x < centerX + 3; x++)
                        {
                            float dx = (x - centerX) / 3f;
                            float dy = (y - 7) / 4f;
                            if (dx * dx + dy * dy < 1f)
                                pixels[y * size + x] = color;
                        }
                    }
                }
                // Saplar
                pixels[11 * size + 5] = new Color(0.3f, 0.5f, 0.2f);
                pixels[12 * size + 6] = new Color(0.3f, 0.5f, 0.2f);
                pixels[12 * size + 7] = new Color(0.3f, 0.5f, 0.2f);
                pixels[12 * size + 9] = new Color(0.3f, 0.5f, 0.2f);
                pixels[12 * size + 10] = new Color(0.3f, 0.5f, 0.2f);
                pixels[11 * size + 11] = new Color(0.3f, 0.5f, 0.2f);
                break;

            case FruitShape.Coin:
                // Altin para
                for (int y = 2; y < 14; y++)
                {
                    for (int x = 4; x < 12; x++)
                    {
                        float dx = (x - 8) / 4f;
                        float dy = (y - 8) / 6f;
                        float dist = dx * dx + dy * dy;

                        if (dist < 1f)
                        {
                            if (dist > 0.75f)
                                pixels[y * size + x] = outline;
                            else
                                pixels[y * size + x] = color;
                        }
                    }
                }
                // $ isareti
                for (int y = 5; y < 12; y++)
                {
                    pixels[y * size + 8] = outline;
                }
                pixels[6 * size + 7] = outline;
                pixels[7 * size + 6] = outline;
                pixels[10 * size + 9] = outline;
                pixels[11 * size + 10] = outline;
                break;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public Sprite GenerateHeartSprite(bool filled)
    {
        string key = $"Heart_{filled}";
        if (spriteCache.ContainsKey(key)) return spriteCache[key];

        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color main = filled ? Palette.HeartRed : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        Color highlight = filled ? Palette.HeartPink : new Color(0.4f, 0.4f, 0.4f, 0.5f);
        Color outline = new Color(main.r * 0.5f, main.g * 0.5f, main.b * 0.5f);

        // Kalp sekli
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - 8) / 7f;
                float ny = (y - 6) / 7f;

                // Kalp denklemi
                float heart = Mathf.Pow(nx * nx + ny * ny - 1, 3) - nx * nx * ny * ny * ny;

                if (heart < 0)
                {
                    if (heart > -0.15f)
                        pixels[y * size + x] = outline;
                    else if (nx < -0.2f && ny > 0)
                        pixels[y * size + x] = highlight;
                    else
                        pixels[y * size + x] = main;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        spriteCache[key] = sprite;
        return sprite;
    }

    #endregion

    #region TRAP SPRITES

    public Sprite GenerateSpikeSprite()
    {
        string key = "Spike";
        if (spriteCache.ContainsKey(key)) return spriteCache[key];

        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color metal = new Color(0.6f, 0.6f, 0.65f);
        Color metalDark = new Color(0.4f, 0.4f, 0.45f);
        Color metalLight = new Color(0.8f, 0.8f, 0.85f);

        // Uc adet sivri diken
        int[] peakX = { 3, 8, 13 };

        for (int p = 0; p < 3; p++)
        {
            int px = peakX[p];
            for (int y = 0; y < 12; y++)
            {
                int width = (12 - y) / 3;
                for (int x = px - width; x <= px + width; x++)
                {
                    if (x >= 0 && x < size)
                    {
                        if (x == px - width)
                            pixels[y * size + x] = metalDark;
                        else if (x == px + width)
                            pixels[y * size + x] = metalDark;
                        else if (x == px)
                            pixels[y * size + x] = metalLight;
                        else
                            pixels[y * size + x] = metal;
                    }
                }
            }
        }

        // Taban
        for (int x = 1; x < 15; x++)
        {
            pixels[0 * size + x] = metalDark;
            pixels[1 * size + x] = metal;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size);
        spriteCache[key] = sprite;
        return sprite;
    }

    public Sprite[] GenerateSawSprites()
    {
        string key = "Saw";
        if (animationCache.ContainsKey(key)) return animationCache[key];

        Sprite[] frames = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            float rotation = i * 45f;
            frames[i] = CreateSawSprite(rotation);
        }

        animationCache[key] = frames;
        return frames;
    }

    Sprite CreateSawSprite(float rotation)
    {
        int size = 24;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Color metal = new Color(0.65f, 0.65f, 0.7f);
        Color metalDark = new Color(0.4f, 0.4f, 0.45f);

        int centerX = size / 2;
        int centerY = size / 2;
        float rad = rotation * Mathf.Deg2Rad;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) + rad;

                // Dis profili
                float toothRadius = 10 + Mathf.Sin(angle * 12) * 2;

                if (dist < toothRadius)
                {
                    if (dist > toothRadius - 2)
                        pixels[y * size + x] = metalDark;
                    else if (dist < 4)
                        pixels[y * size + x] = metalDark; // Merkez delik
                    else
                        pixels[y * size + x] = metal;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    #endregion

    #region BACKGROUND SPRITES

    public Sprite GenerateBackgroundLayer(int layer, int width)
    {
        // layer: 0=sky, 1=mountains, 2=hills
        string key = $"BG_{layer}_{width}";
        if (spriteCache.ContainsKey(key)) return spriteCache[key];

        int height = 200;
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        Color[] pixels = new Color[width * height];

        switch (layer)
        {
            case 0: // Gokyuzu
                for (int y = 0; y < height; y++)
                {
                    float t = (float)y / height;
                    Color skyColor = Color.Lerp(
                        new Color(0.1f, 0.1f, 0.3f), // Alt (koyu mavi)
                        new Color(0.4f, 0.6f, 0.9f), // Ust (acik mavi)
                        t
                    );
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = skyColor;
                    }
                }

                // Yildizlar
                for (int i = 0; i < width / 5; i++)
                {
                    int sx = Random.Range(0, width);
                    int sy = Random.Range(height / 2, height);
                    float brightness = Random.Range(0.5f, 1f);
                    pixels[sy * width + sx] = new Color(brightness, brightness, brightness);
                }
                break;

            case 1: // Daglar
                for (int x = 0; x < width; x++)
                {
                    // Dag profili
                    float mountainHeight = 80 +
                        Mathf.PerlinNoise(x * 0.02f, 0) * 60 +
                        Mathf.PerlinNoise(x * 0.05f, 10) * 30;

                    for (int y = 0; y < height; y++)
                    {
                        if (y < mountainHeight)
                        {
                            float shade = 0.3f + (float)y / mountainHeight * 0.3f;
                            pixels[y * width + x] = new Color(shade * 0.4f, shade * 0.5f, shade * 0.6f);
                        }
                        else
                        {
                            pixels[y * width + x] = Color.clear;
                        }
                    }
                }
                break;

            case 2: // Tepeler
                for (int x = 0; x < width; x++)
                {
                    float hillHeight = 40 +
                        Mathf.PerlinNoise(x * 0.03f, 100) * 40 +
                        Mathf.PerlinNoise(x * 0.08f, 200) * 20;

                    for (int y = 0; y < height; y++)
                    {
                        if (y < hillHeight)
                        {
                            float shade = 0.2f + (float)y / hillHeight * 0.2f;
                            pixels[y * width + x] = new Color(shade * 0.3f, shade * 0.5f, shade * 0.3f);
                        }
                        else
                        {
                            pixels[y * width + x] = Color.clear;
                        }
                    }
                }
                break;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0, 0), 16);
        spriteCache[key] = sprite;
        return sprite;
    }

    #endregion
}
