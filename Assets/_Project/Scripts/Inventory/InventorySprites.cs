using UnityEngine;

/// <summary>
/// Envanter eşyaları için detaylı sprite'lar oluşturur
/// </summary>
public static class InventorySprites
{
    /// <summary>
    /// Eşya tipine göre sprite oluştur
    /// </summary>
    public static Sprite CreateItemSprite(ItemType type)
    {
        switch (type)
        {
            case ItemType.HealthPotion:
                return CreateHealthPotion();
            case ItemType.Shield:
                return CreateShield();
            case ItemType.SpeedBoost:
                return CreateSpeedBoost();
            case ItemType.DoubleDamage:
                return CreateDoubleDamage();
            case ItemType.Magnet:
                return CreateMagnet();
            case ItemType.Bomb:
                return CreateBomb();
            case ItemType.ExtraLife:
                return CreateExtraLife();
            default:
                return CreateHealthPotion();
        }
    }

    // === CAN İKSİRİ - Kırmızı şişe ===
    static Sprite CreateHealthPotion()
    {
        int w = 16, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color glass = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        Color glassLight = new Color(1f, 0.4f, 0.4f);
        Color glassDark = new Color(0.6f, 0.1f, 0.1f);
        Color cork = new Color(0.6f, 0.4f, 0.25f);
        Color liquid = new Color(1f, 0.15f, 0.15f);

        // Şişe gövdesi (oval)
        for (int y = 2; y < 14; y++)
        {
            for (int x = 3; x < 13; x++)
            {
                float dx = (x - 8f) / 5f;
                float dy = (y - 8f) / 6f;
                if (dx * dx + dy * dy < 1f)
                {
                    // 3D efekti
                    float lightFactor = 1f - (x - 3f) / 10f;
                    if (lightFactor > 0.7f)
                        p[y * w + x] = glassLight;
                    else if (lightFactor < 0.3f)
                        p[y * w + x] = glassDark;
                    else
                        p[y * w + x] = glass;
                }
            }
        }

        // Sıvı içi (daha koyu)
        for (int y = 3; y < 12; y++)
        {
            for (int x = 5; x < 11; x++)
            {
                float dx = (x - 8f) / 3f;
                float dy = (y - 7f) / 4.5f;
                if (dx * dx + dy * dy < 1f)
                {
                    p[y * w + x] = liquid;
                }
            }
        }

        // Şişe boynu
        FillRect(p, w, 6, 14, 10, 17, glass);
        FillRect(p, w, 7, 14, 9, 16, glassLight);

        // Mantar tıpa
        FillRect(p, w, 6, 17, 10, 20, cork);
        FillRect(p, w, 7, 18, 9, 19, Brighten(cork, 0.15f));

        // Parlama noktası
        p[6 * w + 5] = new Color(1f, 1f, 1f, 0.8f);
        p[7 * w + 5] = new Color(1f, 1f, 1f, 0.5f);

        return CreateSprite(tex, p, w, h);
    }

    // === KALKAN - Mavi kalkan ===
    static Sprite CreateShield()
    {
        int w = 18, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color shield = new Color(0.2f, 0.4f, 0.9f);
        Color shieldLight = new Color(0.4f, 0.6f, 1f);
        Color shieldDark = new Color(0.1f, 0.2f, 0.6f);
        Color gold = new Color(0.9f, 0.75f, 0.3f);
        Color goldDark = new Color(0.7f, 0.5f, 0.15f);

        // Kalkan şekli (üst geniş, alt sivri)
        for (int y = 0; y < 18; y++)
        {
            float widthRatio = y < 10 ? 1f : 1f - (y - 10f) / 8f;
            int halfWidth = (int)(7 * widthRatio);

            for (int x = 9 - halfWidth; x <= 9 + halfWidth; x++)
            {
                if (x >= 0 && x < w)
                {
                    float lightFactor = (x - 2f) / 14f;
                    if (lightFactor < 0.3f)
                        p[y * w + x] = shieldLight;
                    else if (lightFactor > 0.7f)
                        p[y * w + x] = shieldDark;
                    else
                        p[y * w + x] = shield;
                }
            }
        }

        // Altın kenar (üst)
        FillRect(p, w, 2, 0, 16, 2, gold);
        FillRect(p, w, 3, 1, 15, 1, goldDark);

        // Altın merkez sembol (yıldız benzeri)
        p[8 * w + 9] = gold;
        p[7 * w + 9] = gold;
        p[9 * w + 9] = gold;
        p[8 * w + 8] = gold;
        p[8 * w + 10] = gold;
        p[6 * w + 9] = goldDark;
        p[10 * w + 9] = goldDark;

        // Parlama
        p[4 * w + 5] = new Color(1f, 1f, 1f, 0.7f);
        p[5 * w + 5] = new Color(1f, 1f, 1f, 0.4f);

        return CreateSprite(tex, p, w, h);
    }

    // === HIZ GÜCÜ - Sarı yıldırım ===
    static Sprite CreateSpeedBoost()
    {
        int w = 16, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color bolt = new Color(1f, 0.9f, 0.2f);
        Color boltLight = new Color(1f, 1f, 0.6f);
        Color boltDark = new Color(0.8f, 0.6f, 0.1f);
        Color glow = new Color(1f, 0.95f, 0.5f, 0.4f);

        // Yıldırım şekli
        // Üst kısım
        FillRect(p, w, 8, 17, 12, 19, bolt);
        FillRect(p, w, 7, 15, 11, 17, bolt);
        FillRect(p, w, 6, 13, 10, 15, bolt);
        FillRect(p, w, 5, 11, 12, 13, bolt);

        // Orta kısım (zikzak)
        FillRect(p, w, 7, 9, 13, 11, bolt);
        FillRect(p, w, 6, 7, 10, 9, bolt);
        FillRect(p, w, 5, 5, 9, 7, bolt);
        FillRect(p, w, 4, 3, 8, 5, bolt);
        FillRect(p, w, 3, 1, 7, 3, bolt);

        // Parlaklık (sol kenar)
        p[18 * w + 8] = boltLight;
        p[16 * w + 7] = boltLight;
        p[14 * w + 6] = boltLight;
        p[12 * w + 5] = boltLight;
        p[10 * w + 7] = boltLight;
        p[8 * w + 6] = boltLight;
        p[6 * w + 5] = boltLight;
        p[4 * w + 4] = boltLight;
        p[2 * w + 3] = boltLight;

        // Glow efekti
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (p[y * w + x].a == 0)
                {
                    // Yakın piksel kontrolü
                    bool nearBolt = false;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                            {
                                if (p[ny * w + nx].a > 0.5f)
                                    nearBolt = true;
                            }
                        }
                    }
                    if (nearBolt)
                        p[y * w + x] = glow;
                }
            }
        }

        return CreateSprite(tex, p, w, h);
    }

    // === ÇİFT HASAR - Turuncu kılıç ===
    static Sprite CreateDoubleDamage()
    {
        int w = 16, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color blade = new Color(0.85f, 0.85f, 0.9f);
        Color bladeLight = new Color(1f, 1f, 1f);
        Color bladeDark = new Color(0.6f, 0.6f, 0.7f);
        Color handle = new Color(0.4f, 0.25f, 0.15f);
        Color guard = new Color(1f, 0.6f, 0.1f);
        Color guardDark = new Color(0.8f, 0.4f, 0.05f);

        // Kılıç bıçağı (çapraz)
        for (int i = 0; i < 14; i++)
        {
            int x = 6 + i / 2;
            int y = 5 + i;
            if (x < w && y < h)
            {
                p[y * w + x] = blade;
                if (x > 0) p[y * w + x - 1] = bladeLight;
                if (x < w - 1) p[y * w + x + 1] = bladeDark;
            }
        }

        // Keskin uç
        p[19 * w + 12] = bladeLight;
        p[18 * w + 11] = blade;

        // Koruyucu (guard)
        FillRect(p, w, 3, 4, 11, 6, guard);
        FillRect(p, w, 4, 5, 10, 5, guardDark);

        // Kabza
        FillRect(p, w, 5, 1, 8, 4, handle);
        FillRect(p, w, 6, 2, 7, 3, Brighten(handle, 0.1f));

        // Pommel (kabza ucu)
        FillRect(p, w, 5, 0, 8, 1, guard);

        // Parlama
        p[10 * w + 7] = new Color(1f, 1f, 1f, 0.8f);
        p[14 * w + 9] = new Color(1f, 1f, 1f, 0.6f);

        return CreateSprite(tex, p, w, h);
    }

    // === MIKNATIS - Mor U şekli ===
    static Sprite CreateMagnet()
    {
        int w = 18, h = 18;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color magnetRed = new Color(0.9f, 0.2f, 0.2f);
        Color magnetBlue = new Color(0.2f, 0.3f, 0.9f);
        Color metalLight = new Color(0.7f, 0.7f, 0.75f);
        Color metalDark = new Color(0.4f, 0.4f, 0.45f);

        // Sol bacak (kırmızı)
        FillRect(p, w, 2, 0, 6, 14, magnetRed);
        FillRect(p, w, 3, 1, 5, 13, Brighten(magnetRed, 0.15f));

        // Sağ bacak (mavi)
        FillRect(p, w, 12, 0, 16, 14, magnetBlue);
        FillRect(p, w, 13, 1, 15, 13, Brighten(magnetBlue, 0.15f));

        // Üst bağlantı (metal)
        FillRect(p, w, 2, 14, 16, 18, metalDark);
        FillRect(p, w, 3, 15, 15, 17, metalLight);

        // Uçlar (parlak)
        FillRect(p, w, 2, 0, 6, 2, metalLight);
        FillRect(p, w, 12, 0, 16, 2, metalLight);

        // Manyetik çizgiler (dekoratif)
        p[7 * w + 8] = new Color(0.8f, 0.3f, 0.8f, 0.5f);
        p[7 * w + 9] = new Color(0.8f, 0.3f, 0.8f, 0.5f);
        p[7 * w + 10] = new Color(0.8f, 0.3f, 0.8f, 0.5f);
        p[9 * w + 7] = new Color(0.8f, 0.3f, 0.8f, 0.3f);
        p[9 * w + 11] = new Color(0.8f, 0.3f, 0.8f, 0.3f);

        return CreateSprite(tex, p, w, h);
    }

    // === BOMBA - Siyah bomba ===
    static Sprite CreateBomb()
    {
        int w = 18, h = 20;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color bombBody = new Color(0.15f, 0.15f, 0.18f);
        Color bombLight = new Color(0.35f, 0.35f, 0.4f);
        Color bombDark = new Color(0.08f, 0.08f, 0.1f);
        Color fuse = new Color(0.5f, 0.35f, 0.2f);
        Color spark = new Color(1f, 0.8f, 0.2f);
        Color sparkGlow = new Color(1f, 0.5f, 0.1f);

        // Bomba gövdesi (daire)
        Vector2 center = new Vector2(9, 8);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 8)
                {
                    float lightFactor = 1f - (x - 2f) / 14f;
                    if (lightFactor > 0.7f)
                        p[y * w + x] = bombLight;
                    else if (lightFactor < 0.3f)
                        p[y * w + x] = bombDark;
                    else
                        p[y * w + x] = bombBody;
                }
            }
        }

        // Fitil deliği
        FillRect(p, w, 8, 15, 11, 17, bombDark);

        // Fitil
        p[17 * w + 9] = fuse;
        p[17 * w + 10] = fuse;
        p[18 * w + 10] = fuse;
        p[18 * w + 11] = fuse;
        p[19 * w + 11] = fuse;

        // Kıvılcım
        p[19 * w + 12] = spark;
        p[19 * w + 13] = sparkGlow;
        p[18 * w + 13] = new Color(spark.r, spark.g, spark.b, 0.6f);

        // Parlama noktası
        p[5 * w + 5] = new Color(1f, 1f, 1f, 0.5f);
        p[6 * w + 6] = new Color(1f, 1f, 1f, 0.3f);

        // Dekoratif çizgi
        FillRect(p, w, 4, 7, 14, 9, bombLight);

        return CreateSprite(tex, p, w, h);
    }

    // === EKSTRA CAN - Yeşil kalp ===
    static Sprite CreateExtraLife()
    {
        int w = 18, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color[] p = new Color[w * h];
        Clear(p);

        Color heart = new Color(0.2f, 0.85f, 0.4f);
        Color heartLight = new Color(0.4f, 1f, 0.6f);
        Color heartDark = new Color(0.1f, 0.6f, 0.25f);
        Color glow = new Color(0.3f, 1f, 0.5f, 0.3f);

        // Kalp şekli
        int[] heartShape = {
            0,0,0,1,1,1,0,0,0,0,1,1,1,0,0,0,0,0,
            0,0,1,1,1,1,1,0,0,1,1,1,1,1,0,0,0,0,
            0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,
            0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,
            0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,
            0,0,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,
            0,0,0,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,
            0,0,0,0,1,1,1,1,1,1,1,1,0,0,0,0,0,0,
            0,0,0,0,0,1,1,1,1,1,1,0,0,0,0,0,0,0,
            0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        };

        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = (11 - y) * 18 + x;
                if (idx < heartShape.Length && heartShape[idx] == 1)
                {
                    // 3D efekti
                    float lightFactor = 1f - (x - 2f) / 14f;
                    if (lightFactor > 0.65f)
                        p[(y + 2) * w + x] = heartLight;
                    else if (lightFactor < 0.35f)
                        p[(y + 2) * w + x] = heartDark;
                    else
                        p[(y + 2) * w + x] = heart;
                }
            }
        }

        // Parlama
        p[11 * w + 5] = new Color(1f, 1f, 1f, 0.8f);
        p[10 * w + 5] = new Color(1f, 1f, 1f, 0.5f);
        p[11 * w + 6] = new Color(1f, 1f, 1f, 0.4f);

        // + işareti (ekstra için)
        p[7 * w + 15] = heartLight;
        p[6 * w + 15] = heartLight;
        p[8 * w + 15] = heartLight;
        p[7 * w + 14] = heartLight;
        p[7 * w + 16] = heartLight;

        return CreateSprite(tex, p, w, h);
    }

    // === YARDIMCI FONKSİYONLAR ===

    static void Clear(Color[] p)
    {
        for (int i = 0; i < p.Length; i++)
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

    static Sprite CreateSprite(Texture2D tex, Color[] p, int w, int h)
    {
        tex.SetPixels(p);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
    }
}
