using UnityEngine;

/// <summary>
/// Detaylı silah görselleri - pickup ve UI ikonları için
/// </summary>
public static class WeaponVisuals
{
    /// <summary>
    /// Silah tipine göre detaylı sprite oluştur
    /// </summary>
    public static Sprite CreateWeaponSprite(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol:
                return CreatePistol();
            case WeaponType.Rifle:
                return CreateRifle();
            case WeaponType.Shotgun:
                return CreateShotgun();
            case WeaponType.SMG:
                return CreateSMG();
            case WeaponType.Sniper:
                return CreateSniper();
            case WeaponType.RocketLauncher:
                return CreateRocketLauncher();
            case WeaponType.Flamethrower:
                return CreateFlamethrower();
            case WeaponType.GrenadeLauncher:
                return CreateGrenadeLauncher();
            default:
                return CreatePistol();
        }
    }

    // === PISTOL ===
    static Sprite CreatePistol()
    {
        int w = 20, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body = new Color(0.3f, 0.3f, 0.35f);
        Color grip = new Color(0.25f, 0.2f, 0.15f);
        Color metal = new Color(0.5f, 0.5f, 0.55f);

        // Namlu
        FillRect(p, w, 8, 8, 19, 11, body);
        FillRect(p, w, 8, 9, 19, 10, metal); // Üst parlama

        // Gövde
        FillRect(p, w, 4, 6, 12, 12, body);
        FillRect(p, w, 5, 7, 11, 11, metal); // İç detay

        // Kabza
        FillRect(p, w, 4, 1, 9, 6, grip);
        FillRect(p, w, 5, 2, 8, 5, Darken(grip, 0.1f)); // Kabza doku

        // Tetik koruma
        FillRect(p, w, 9, 4, 12, 6, body);

        // Tetik
        p[5 * w + 10] = new Color(0.2f, 0.2f, 0.2f);

        // Nişangah
        p[11 * w + 17] = metal;
        p[12 * w + 17] = metal;

        return CreateSprite(tex, p, w, h);
    }

    // === RIFLE ===
    static Sprite CreateRifle()
    {
        int w = 40, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body = new Color(0.35f, 0.4f, 0.3f); // Yeşilimsi
        Color wood = new Color(0.45f, 0.3f, 0.2f);
        Color metal = new Color(0.4f, 0.4f, 0.45f);

        // Dipçik
        FillRect(p, w, 0, 5, 8, 11, wood);
        FillRect(p, w, 1, 6, 7, 10, Brighten(wood, 0.1f));

        // Gövde
        FillRect(p, w, 8, 6, 28, 10, body);

        // Şarjör
        FillRect(p, w, 16, 2, 20, 6, metal);

        // Namlu
        FillRect(p, w, 28, 7, 39, 9, metal);
        FillRect(p, w, 28, 8, 39, 8, Brighten(metal, 0.15f)); // Parlama

        // Nişangahlar
        p[10 * w + 35] = metal;
        p[11 * w + 35] = metal;
        p[10 * w + 12] = metal;
        p[11 * w + 12] = metal;

        // Kabza
        FillRect(p, w, 20, 2, 24, 6, body);

        // El tutma yeri
        FillRect(p, w, 10, 4, 14, 6, Darken(body, 0.1f));

        return CreateSprite(tex, p, w, h);
    }

    // === SHOTGUN ===
    static Sprite CreateShotgun()
    {
        int w = 36, h = 14;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body = new Color(0.5f, 0.35f, 0.25f); // Kahverengi
        Color metal = new Color(0.35f, 0.35f, 0.4f);
        Color dark = Darken(metal, 0.15f);

        // Dipçik
        FillRect(p, w, 0, 4, 7, 10, body);
        FillRect(p, w, 1, 5, 6, 9, Brighten(body, 0.1f));

        // Gövde
        FillRect(p, w, 7, 5, 22, 9, body);

        // Pompa mekanizması
        FillRect(p, w, 14, 4, 20, 10, metal);
        FillRect(p, w, 15, 5, 19, 9, Brighten(metal, 0.1f));

        // Namlu (çift)
        FillRect(p, w, 22, 5, 35, 7, metal);
        FillRect(p, w, 22, 7, 35, 9, metal);
        // Namlu ayracı
        FillRect(p, w, 22, 7, 35, 7, dark);

        // Namlu ucu
        FillRect(p, w, 34, 5, 36, 9, dark);

        // Tetik
        p[4 * w + 12] = dark;
        FillRect(p, w, 10, 3, 14, 5, body);

        return CreateSprite(tex, p, w, h);
    }

    // === SMG ===
    static Sprite CreateSMG()
    {
        int w = 28, h = 14;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body = new Color(0.25f, 0.25f, 0.3f);
        Color metal = new Color(0.4f, 0.4f, 0.45f);

        // Gövde
        FillRect(p, w, 4, 5, 20, 9, body);

        // Namlu
        FillRect(p, w, 20, 6, 27, 8, metal);

        // Şarjör (uzun)
        FillRect(p, w, 10, 0, 14, 5, metal);

        // Kabza
        FillRect(p, w, 4, 2, 8, 5, body);
        FillRect(p, w, 5, 3, 7, 4, Darken(body, 0.1f));

        // Ön tutma
        FillRect(p, w, 16, 3, 19, 5, body);

        // Nişangah
        p[9 * w + 8] = metal;
        p[10 * w + 8] = metal;

        // Stok (katlanır)
        FillRect(p, w, 0, 7, 4, 9, metal);
        p[8 * w + 0] = metal;
        p[8 * w + 1] = metal;

        return CreateSprite(tex, p, w, h);
    }

    // === SNIPER ===
    static Sprite CreateSniper()
    {
        int w = 48, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body = new Color(0.3f, 0.35f, 0.3f);
        Color metal = new Color(0.35f, 0.35f, 0.4f);
        Color scope = new Color(0.2f, 0.2f, 0.25f);

        // Dipçik
        FillRect(p, w, 0, 5, 10, 11, body);
        // Yanak desteği
        FillRect(p, w, 2, 11, 8, 13, body);

        // Gövde
        FillRect(p, w, 10, 6, 32, 10, body);

        // Dürbün
        FillRect(p, w, 14, 10, 26, 14, scope);
        // Dürbün lensleri
        p[12 * w + 14] = new Color(0.3f, 0.5f, 0.7f);
        p[12 * w + 25] = new Color(0.3f, 0.5f, 0.7f);
        // Dürbün ayar
        FillRect(p, w, 18, 14, 22, 15, metal);

        // Namlu (çok uzun)
        FillRect(p, w, 32, 7, 47, 9, metal);
        // Namlu parlama
        FillRect(p, w, 32, 8, 47, 8, Brighten(metal, 0.15f));

        // Bipod (destek ayakları)
        p[4 * w + 28] = metal;
        p[3 * w + 27] = metal;
        p[2 * w + 26] = metal;
        p[4 * w + 30] = metal;
        p[3 * w + 31] = metal;
        p[2 * w + 32] = metal;

        // Şarjör
        FillRect(p, w, 22, 3, 26, 6, metal);

        return CreateSprite(tex, p, w, h);
    }

    // === ROCKET LAUNCHER ===
    static Sprite CreateRocketLauncher()
    {
        int w = 44, h = 18;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color tube = new Color(0.35f, 0.4f, 0.3f);
        Color metal = new Color(0.4f, 0.4f, 0.45f);
        Color grip = new Color(0.25f, 0.25f, 0.2f);

        // Ana tüp
        FillRect(p, w, 4, 6, 40, 12, tube);
        // Tüp üst parlama
        FillRect(p, w, 4, 6, 40, 7, Brighten(tube, 0.1f));
        // Tüp alt gölge
        FillRect(p, w, 4, 11, 40, 12, Darken(tube, 0.1f));

        // Ön açıklık
        FillRect(p, w, 40, 7, 43, 11, Darken(tube, 0.2f));

        // Arka kapak
        FillRect(p, w, 0, 7, 4, 11, metal);

        // Nişangah
        FillRect(p, w, 8, 12, 12, 15, metal);
        FillRect(p, w, 28, 12, 32, 15, metal);

        // Kabzalar
        FillRect(p, w, 14, 2, 18, 6, grip);
        FillRect(p, w, 24, 2, 28, 6, grip);

        // Tetik
        p[4 * w + 16] = metal;

        // Omuz desteği
        FillRect(p, w, 0, 4, 6, 6, grip);
        FillRect(p, w, 0, 12, 6, 14, grip);

        return CreateSprite(tex, p, w, h);
    }

    // === FLAMETHROWER ===
    static Sprite CreateFlamethrower()
    {
        int w = 40, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color tank = new Color(0.5f, 0.5f, 0.55f);
        Color tube = new Color(0.3f, 0.3f, 0.35f);
        Color nozzle = new Color(0.25f, 0.25f, 0.3f);
        Color flame = new Color(1f, 0.5f, 0.1f);

        // Yakıt tankı (arka)
        FillRect(p, w, 0, 4, 12, 16, tank);
        FillRect(p, w, 1, 5, 11, 15, Brighten(tank, 0.1f));
        // Tank bandı
        FillRect(p, w, 0, 9, 12, 11, Darken(tank, 0.15f));

        // Bağlantı borusu
        FillRect(p, w, 12, 8, 18, 12, tube);

        // Ana gövde
        FillRect(p, w, 18, 7, 32, 13, tube);

        // Namlu
        FillRect(p, w, 32, 8, 38, 12, nozzle);

        // Alev ağzı
        FillRect(p, w, 38, 7, 40, 13, nozzle);
        // Küçük alev efekti
        p[10 * w + 39] = flame;
        p[9 * w + 39] = new Color(1f, 0.8f, 0.3f);
        p[11 * w + 39] = new Color(1f, 0.3f, 0f);

        // Kabza
        FillRect(p, w, 22, 3, 26, 7, tube);

        // Tetik
        p[5 * w + 24] = nozzle;

        // Pilot alev
        FillRect(p, w, 30, 5, 32, 7, flame);

        return CreateSprite(tex, p, w, h);
    }

    // === GRENADE LAUNCHER ===
    static Sprite CreateGrenadeLauncher()
    {
        int w = 36, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body = new Color(0.35f, 0.4f, 0.3f);
        Color drum = new Color(0.3f, 0.3f, 0.35f);
        Color metal = new Color(0.45f, 0.45f, 0.5f);

        // Dipçik
        FillRect(p, w, 0, 5, 6, 11, body);

        // Gövde
        FillRect(p, w, 6, 6, 24, 10, body);

        // Döner tambur (drum)
        for (int y = 2; y < 14; y++)
        {
            for (int x = 12; x < 22; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(17, 8));
                if (dist < 6)
                {
                    p[y * w + x] = drum;
                    if (dist < 5 && dist > 4)
                    {
                        p[y * w + x] = Darken(drum, 0.15f);
                    }
                }
            }
        }
        // Tambur merkezi
        p[8 * w + 17] = metal;
        p[7 * w + 17] = metal;
        p[9 * w + 17] = metal;

        // Namlu
        FillRect(p, w, 24, 7, 35, 9, metal);

        // Kabza
        FillRect(p, w, 6, 2, 10, 6, body);

        // Tetik
        p[4 * w + 8] = metal;

        // Nişangah
        p[10 * w + 32] = metal;
        p[11 * w + 32] = metal;

        return CreateSprite(tex, p, w, h);
    }

    // === UI İKON (küçük versiyon) ===
    public static Sprite CreateWeaponIcon(WeaponType type)
    {
        int w = 32, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p, w, h);

        Color body, metal;

        switch (type)
        {
            case WeaponType.Pistol:
                body = new Color(0.3f, 0.3f, 0.35f);
                metal = new Color(0.5f, 0.5f, 0.55f);
                // Basit tabanca
                FillRect(p, w, 12, 8, 28, 12, body);
                FillRect(p, w, 8, 4, 16, 12, body);
                FillRect(p, w, 8, 2, 14, 8, new Color(0.25f, 0.2f, 0.15f));
                break;

            case WeaponType.Rifle:
                body = new Color(0.35f, 0.4f, 0.3f);
                metal = new Color(0.4f, 0.4f, 0.45f);
                FillRect(p, w, 2, 8, 30, 12, body);
                FillRect(p, w, 0, 6, 6, 14, new Color(0.45f, 0.3f, 0.2f));
                FillRect(p, w, 12, 5, 16, 8, metal);
                break;

            case WeaponType.Shotgun:
                body = new Color(0.5f, 0.35f, 0.25f);
                metal = new Color(0.35f, 0.35f, 0.4f);
                FillRect(p, w, 2, 7, 30, 13, body);
                FillRect(p, w, 20, 8, 30, 10, metal);
                FillRect(p, w, 20, 10, 30, 12, metal);
                break;

            case WeaponType.SMG:
                body = new Color(0.25f, 0.25f, 0.3f);
                metal = new Color(0.4f, 0.4f, 0.45f);
                FillRect(p, w, 6, 7, 26, 13, body);
                FillRect(p, w, 12, 2, 16, 7, metal);
                FillRect(p, w, 2, 9, 6, 11, metal);
                break;

            case WeaponType.Sniper:
                body = new Color(0.3f, 0.35f, 0.3f);
                metal = new Color(0.35f, 0.35f, 0.4f);
                FillRect(p, w, 0, 8, 32, 12, body);
                FillRect(p, w, 10, 12, 20, 16, new Color(0.2f, 0.2f, 0.25f));
                break;

            case WeaponType.RocketLauncher:
                body = new Color(0.35f, 0.4f, 0.3f);
                FillRect(p, w, 2, 6, 30, 14, body);
                FillRect(p, w, 28, 7, 32, 13, Darken(body, 0.2f));
                break;

            case WeaponType.Flamethrower:
                body = new Color(0.5f, 0.5f, 0.55f);
                FillRect(p, w, 0, 4, 10, 16, body);
                FillRect(p, w, 10, 7, 28, 13, new Color(0.3f, 0.3f, 0.35f));
                p[10 * w + 29] = new Color(1f, 0.5f, 0.1f);
                break;

            case WeaponType.GrenadeLauncher:
                body = new Color(0.35f, 0.4f, 0.3f);
                FillRect(p, w, 2, 7, 28, 13, body);
                // Drum
                for (int y = 4; y < 16; y++)
                {
                    for (int x = 10; x < 22; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 10));
                        if (dist < 6)
                            p[y * w + x] = new Color(0.3f, 0.3f, 0.35f);
                    }
                }
                break;
        }

        return CreateSprite(tex, p, w, h);
    }

    // === YARDIMCI FONKSİYONLAR ===

    static void Clear(Color[] p, int w, int h)
    {
        for (int i = 0; i < w * h; i++)
            p[i] = Color.clear;
    }

    static void FillRect(Color[] p, int w, int x1, int y1, int x2, int y2, Color c)
    {
        for (int y = y1; y < y2; y++)
        {
            for (int x = x1; x < x2; x++)
            {
                if (x >= 0 && x < w && y >= 0 && y < p.Length / w)
                    p[y * w + x] = c;
            }
        }
    }

    static Color Brighten(Color c, float amount)
    {
        return new Color(
            Mathf.Min(1f, c.r + amount),
            Mathf.Min(1f, c.g + amount),
            Mathf.Min(1f, c.b + amount),
            c.a
        );
    }

    static Color Darken(Color c, float amount)
    {
        return new Color(
            Mathf.Max(0f, c.r - amount),
            Mathf.Max(0f, c.g - amount),
            Mathf.Max(0f, c.b - amount),
            c.a
        );
    }

    static Sprite CreateSprite(Texture2D tex, Color[] p, int w, int h)
    {
        tex.SetPixels(p);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
    }
}
