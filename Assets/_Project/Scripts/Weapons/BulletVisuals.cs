using UnityEngine;

/// <summary>
/// Detaylı ve güzel mermi görselleri oluşturucu
/// </summary>
public static class BulletVisuals
{
    /// <summary>
    /// Silah tipine göre mermi sprite'ı oluştur
    /// Procedural olarak gerçekçi mermi şekilleri çizer
    /// </summary>
    public static Sprite CreateBulletSprite(WeaponType type, Color baseColor)
    {
        // Procedural mermi sprite'ları kullan (daha gerçekçi)
        return CreateProceduralBullet(type, baseColor);
    }

    /// <summary>
    /// Procedural mermi sprite'ı oluştur (fallback)
    /// </summary>
    public static Sprite CreateProceduralBullet(WeaponType type, Color baseColor)
    {
        switch (type)
        {
            case WeaponType.Pistol:
                return CreatePistolBullet(baseColor);
            case WeaponType.Rifle:
                return CreateRifleBullet(baseColor);
            case WeaponType.Shotgun:
                return CreateShotgunPellet(baseColor);
            case WeaponType.SMG:
                return CreateSMGBullet(baseColor);
            case WeaponType.Sniper:
                return CreateSniperBullet(baseColor);
            case WeaponType.RocketLauncher:
                return CreateRocket(baseColor);
            case WeaponType.Flamethrower:
                return CreateFlameParticle();
            case WeaponType.GrenadeLauncher:
                return CreateGrenade(baseColor);
            default:
                return CreatePistolBullet(baseColor);
        }
    }

    // === PISTOL - Bakır uçlu pirinç kovan mermisi ===
    static Sprite CreatePistolBullet(Color baseColor)
    {
        int w = 14, h = 8;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Metalik renkler
        Color brass = new Color(0.8f, 0.65f, 0.3f);       // Pirinç kovan
        Color brassLight = new Color(0.95f, 0.85f, 0.5f); // Parlak pirinç
        Color brassDark = new Color(0.55f, 0.45f, 0.2f);  // Koyu pirinç
        Color copper = new Color(0.85f, 0.5f, 0.3f);      // Bakır uç
        Color copperLight = new Color(1f, 0.7f, 0.5f);    // Parlak bakır
        Color copperDark = new Color(0.6f, 0.35f, 0.2f);  // Koyu bakır
        Color trail = new Color(0.9f, 0.8f, 0.5f, 0.4f);  // Hareket izi

        // Hareket izi (arkada)
        pixels[3 * w + 0] = new Color(trail.r, trail.g, trail.b, 0.15f);
        pixels[4 * w + 0] = new Color(trail.r, trail.g, trail.b, 0.15f);
        pixels[3 * w + 1] = new Color(trail.r, trail.g, trail.b, 0.3f);
        pixels[4 * w + 1] = new Color(trail.r, trail.g, trail.b, 0.3f);

        // Pirinç kovan (arka kısım) - 3D silindir efekti
        for (int x = 2; x < 6; x++)
        {
            for (int y = 2; y < 6; y++)
            {
                float yNorm = (y - 2f) / 3f; // 0-1 arası
                // Üst parlak, alt koyu (silindir efekti)
                if (yNorm < 0.3f)
                    pixels[y * w + x] = brassLight;
                else if (yNorm > 0.7f)
                    pixels[y * w + x] = brassDark;
                else
                    pixels[y * w + x] = brass;
            }
        }

        // Kovan kenarı (koyu çizgi)
        pixels[2 * w + 5] = brassDark;
        pixels[3 * w + 5] = brassDark;
        pixels[4 * w + 5] = brassDark;
        pixels[5 * w + 5] = brassDark;

        // Bakır mermi ucu - 3D efekti
        for (int x = 6; x < 12; x++)
        {
            for (int y = 2; y < 6; y++)
            {
                float xProgress = (x - 6f) / 5f; // Ucuna doğru daralma
                float yNorm = (y - 2f) / 3f;

                // Üst parlak, alt koyu
                Color baseCopper;
                if (yNorm < 0.3f)
                    baseCopper = copperLight;
                else if (yNorm > 0.7f)
                    baseCopper = copperDark;
                else
                    baseCopper = copper;

                // Uca doğru açılma
                baseCopper = Color.Lerp(baseCopper, copperLight, xProgress * 0.3f);

                // Daralma kontrolü
                float edgeDist = Mathf.Abs(yNorm - 0.5f);
                float maxEdge = 0.5f - (xProgress * 0.3f);

                if (edgeDist <= maxEdge)
                    pixels[y * w + x] = baseCopper;
            }
        }

        // Sivri uç
        pixels[3 * w + 12] = copperLight;
        pixels[4 * w + 12] = copperLight;
        pixels[3 * w + 13] = new Color(1f, 0.85f, 0.7f); // Çok parlak uç
        pixels[4 * w + 13] = new Color(1f, 0.85f, 0.7f);

        // Parlama noktası (sol üst)
        pixels[2 * w + 7] = new Color(1f, 0.9f, 0.8f, 0.9f);
        pixels[2 * w + 8] = copperLight;

        return CreateSprite(tex, pixels, w, h);
    }

    // === RIFLE - 5.56mm tüfek mermisi (küçültülmüş) ===
    static Sprite CreateRifleBullet(Color baseColor)
    {
        int w = 12, h = 5;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Metalik renkler
        Color brass = new Color(0.8f, 0.68f, 0.35f);      // Pirinç kovan
        Color brassLight = new Color(0.95f, 0.85f, 0.55f);
        Color brassDark = new Color(0.5f, 0.42f, 0.2f);
        Color copper = new Color(0.9f, 0.55f, 0.35f);     // Bakır uç (FMJ)
        Color copperLight = new Color(1f, 0.75f, 0.55f);
        Color copperDark = new Color(0.6f, 0.38f, 0.22f);
        Color trail = new Color(0.85f, 0.75f, 0.45f, 0.35f);

        // Hareket izi
        pixels[2 * w + 0] = new Color(trail.r, trail.g, trail.b, 0.2f);

        // Pirinç kovan (3D silindir)
        for (int x = 1; x < 4; x++)
        {
            for (int y = 1; y < 4; y++)
            {
                float yNorm = (y - 1f) / 2f;
                if (yNorm < 0.4f)
                    pixels[y * w + x] = brassLight;
                else if (yNorm > 0.6f)
                    pixels[y * w + x] = brassDark;
                else
                    pixels[y * w + x] = brass;
            }
        }

        // Kovan-mermi birleşimi
        pixels[1 * w + 4] = brassDark;
        pixels[2 * w + 4] = brassDark;
        pixels[3 * w + 4] = brassDark;

        // Bakır mermi gövdesi (3D silindir + sivri uç)
        for (int x = 5; x < 10; x++)
        {
            for (int y = 1; y < 4; y++)
            {
                float xProgress = (x - 5f) / 4f;
                float yNorm = (y - 1f) / 2f;

                Color c;
                if (yNorm < 0.4f)
                    c = copperLight;
                else if (yNorm > 0.6f)
                    c = copperDark;
                else
                    c = copper;

                // Uca doğru parlaklaşma
                c = Color.Lerp(c, copperLight, xProgress * 0.4f);

                // Daralma (ucuna doğru)
                float edgeDist = Mathf.Abs(yNorm - 0.5f);
                if (x < 9 || edgeDist < 0.3f)
                    pixels[y * w + x] = c;
            }
        }

        // Sivri parlak uç
        pixels[2 * w + 10] = copperLight;
        pixels[2 * w + 11] = new Color(1f, 0.9f, 0.75f);

        // Üst parlama çizgisi
        pixels[1 * w + 6] = new Color(1f, 0.9f, 0.8f, 0.8f);
        pixels[1 * w + 7] = copperLight;

        return CreateSprite(tex, pixels, w, h);
    }

    // === SHOTGUN - Metalik saçma pellet ===
    static Sprite CreateShotgunPellet(Color baseColor)
    {
        int w = 10, h = 8;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Metalik renkler
        Color metal = new Color(0.7f, 0.7f, 0.75f);      // Gümüş
        Color metalLight = new Color(0.95f, 0.95f, 1f);  // Parlak beyaz
        Color metalDark = new Color(0.4f, 0.4f, 0.45f);  // Koyu gri
        Color trail = new Color(0.9f, 0.85f, 0.6f, 0.5f); // Sarımsı iz

        // Hareket izi (arkada)
        pixels[3 * w + 0] = new Color(trail.r, trail.g, trail.b, 0.2f);
        pixels[4 * w + 0] = new Color(trail.r, trail.g, trail.b, 0.2f);
        pixels[3 * w + 1] = new Color(trail.r, trail.g, trail.b, 0.4f);
        pixels[4 * w + 1] = new Color(trail.r, trail.g, trail.b, 0.4f);
        pixels[3 * w + 2] = new Color(trail.r, trail.g, trail.b, 0.6f);
        pixels[4 * w + 2] = new Color(trail.r, trail.g, trail.b, 0.6f);

        // Ana saçma gövdesi (oval metalik küre)
        Vector2 center = new Vector2(6f, 3.5f);
        for (int y = 1; y < 7; y++)
        {
            for (int x = 3; x < 10; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 3.2f)
                {
                    // 3D küre efekti - sol üst parlak, sağ alt koyu
                    float lightFactor = 1f - ((x - 3f) / 7f + (y - 1f) / 6f) / 2f;

                    if (dist < 1.5f)
                    {
                        // Merkez - parlak
                        pixels[y * w + x] = Color.Lerp(metal, metalLight, lightFactor);
                    }
                    else if (dist < 2.5f)
                    {
                        // Orta - normal metal
                        pixels[y * w + x] = Color.Lerp(metalDark, metal, lightFactor);
                    }
                    else
                    {
                        // Kenar - koyu
                        pixels[y * w + x] = Color.Lerp(metalDark, metal, lightFactor * 0.5f);
                    }
                }
            }
        }

        // Parlama noktası (sol üst)
        pixels[2 * w + 5] = metalLight;
        pixels[2 * w + 6] = new Color(1f, 1f, 1f, 0.9f);
        pixels[3 * w + 5] = new Color(1f, 1f, 1f, 0.7f);

        return CreateSprite(tex, pixels, w, h);
    }

    // === SMG - Tracer efektli küçük mermi ===
    static Sprite CreateSMGBullet(Color baseColor)
    {
        int w = 10, h = 4;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Tracer renkleri (kırmızımsı turuncu iz)
        Color brass = new Color(0.75f, 0.62f, 0.32f);
        Color brassLight = new Color(0.92f, 0.8f, 0.5f);
        Color copper = new Color(0.88f, 0.52f, 0.32f);
        Color copperLight = new Color(1f, 0.72f, 0.52f);
        Color tracer = new Color(1f, 0.4f, 0.15f, 0.7f);  // Turuncu tracer
        Color tracerGlow = new Color(1f, 0.6f, 0.3f, 0.4f);

        // Tracer izi (arkada parlak)
        pixels[1 * w + 0] = new Color(tracerGlow.r, tracerGlow.g, tracerGlow.b, 0.2f);
        pixels[2 * w + 0] = new Color(tracerGlow.r, tracerGlow.g, tracerGlow.b, 0.2f);
        pixels[1 * w + 1] = tracer;
        pixels[2 * w + 1] = tracer;

        // Kovan (küçük)
        pixels[1 * w + 2] = brassLight;
        pixels[2 * w + 2] = brass;
        pixels[1 * w + 3] = brassLight;
        pixels[2 * w + 3] = brass;

        // Bakır mermi gövdesi
        for (int x = 4; x < 8; x++)
        {
            float xProgress = (x - 4f) / 3f;
            pixels[1 * w + x] = Color.Lerp(copper, copperLight, xProgress);
            pixels[2 * w + x] = Color.Lerp(copper, copperLight, xProgress * 0.7f);
        }

        // Sivri uç
        pixels[1 * w + 8] = copperLight;
        pixels[2 * w + 8] = copperLight;
        pixels[1 * w + 9] = new Color(1f, 0.88f, 0.72f);
        pixels[2 * w + 9] = new Color(1f, 0.88f, 0.72f);

        return CreateSprite(tex, pixels, w, h);
    }

    // === SNIPER - .50 BMG zırh delici mermi ===
    static Sprite CreateSniperBullet(Color baseColor)
    {
        int w = 20, h = 6;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Zırh delici özel renkler
        Color brass = new Color(0.78f, 0.65f, 0.32f);
        Color brassLight = new Color(0.95f, 0.85f, 0.55f);
        Color brassDark = new Color(0.48f, 0.4f, 0.2f);
        Color steel = new Color(0.45f, 0.5f, 0.55f);        // Çelik çekirdek
        Color steelLight = new Color(0.7f, 0.75f, 0.82f);   // Parlak çelik
        Color steelDark = new Color(0.28f, 0.32f, 0.38f);
        Color apGlow = new Color(0.6f, 0.85f, 1f, 0.4f);    // Mavi AP parıltısı
        Color trail = new Color(0.7f, 0.85f, 1f, 0.3f);     // Mavimsi iz

        // Hareket izi (uzun, mavimsi)
        for (int x = 0; x < 3; x++)
        {
            float alpha = 0.15f + (x * 0.1f);
            pixels[2 * w + x] = new Color(trail.r, trail.g, trail.b, alpha);
            pixels[3 * w + x] = new Color(trail.r, trail.g, trail.b, alpha);
        }

        // Pirinç kovan (büyük)
        for (int x = 3; x < 8; x++)
        {
            for (int y = 1; y < 5; y++)
            {
                float yNorm = (y - 1f) / 3f;
                if (yNorm < 0.35f)
                    pixels[y * w + x] = brassLight;
                else if (yNorm > 0.65f)
                    pixels[y * w + x] = brassDark;
                else
                    pixels[y * w + x] = brass;
            }
        }

        // Kovan-mermi birleşimi
        for (int y = 1; y < 5; y++)
            pixels[y * w + 8] = brassDark;

        // Çelik zırh delici uç (3D silindir + sivri)
        for (int x = 9; x < 17; x++)
        {
            for (int y = 1; y < 5; y++)
            {
                float xProgress = (x - 9f) / 7f;
                float yNorm = (y - 1f) / 3f;

                Color c;
                if (yNorm < 0.35f)
                    c = steelLight;
                else if (yNorm > 0.65f)
                    c = steelDark;
                else
                    c = steel;

                // Uca doğru parlaklaşma
                c = Color.Lerp(c, steelLight, xProgress * 0.5f);

                // Daralma (ucuna doğru)
                float edgeDist = Mathf.Abs(yNorm - 0.5f);
                float maxEdge = 0.5f - (xProgress * 0.25f);

                if (edgeDist <= maxEdge)
                    pixels[y * w + x] = c;
            }
        }

        // Sivri AP uç
        pixels[2 * w + 17] = steelLight;
        pixels[3 * w + 17] = steelLight;
        pixels[2 * w + 18] = new Color(0.85f, 0.9f, 0.98f);
        pixels[3 * w + 18] = new Color(0.85f, 0.9f, 0.98f);
        pixels[2 * w + 19] = new Color(1f, 1f, 1f);  // Çok parlak uç
        pixels[3 * w + 19] = new Color(1f, 1f, 1f);

        // AP mavi parıltı (üst ve alt)
        pixels[0 * w + 14] = apGlow;
        pixels[0 * w + 15] = apGlow;
        pixels[5 * w + 14] = apGlow;
        pixels[5 * w + 15] = apGlow;

        // Üst parlama çizgisi
        pixels[1 * w + 11] = new Color(0.9f, 0.95f, 1f, 0.9f);
        pixels[1 * w + 12] = steelLight;

        return CreateSprite(tex, pixels, w, h);
    }

    // === ROCKET - RPG-7 tipi detaylı roket ===
    static Sprite CreateRocket(Color baseColor)
    {
        int w = 24, h = 12;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Roket renkleri
        Color bodyDark = new Color(0.32f, 0.35f, 0.32f);
        Color body = new Color(0.45f, 0.48f, 0.45f);
        Color bodyLight = new Color(0.58f, 0.62f, 0.58f);
        Color warhead = new Color(0.65f, 0.22f, 0.12f);      // Koyu kırmızı savaş başlığı
        Color warheadLight = new Color(0.85f, 0.35f, 0.2f);
        Color warheadDark = new Color(0.45f, 0.15f, 0.08f);
        Color fin = new Color(0.28f, 0.3f, 0.32f);
        Color finLight = new Color(0.42f, 0.45f, 0.48f);

        // Alev renkleri (gradient)
        Color flameCore = new Color(1f, 1f, 0.7f);           // Beyazımsı sarı merkez
        Color flameInner = new Color(1f, 0.75f, 0.2f);       // Sarı
        Color flameOuter = new Color(1f, 0.4f, 0.08f);       // Turuncu
        Color flameEdge = new Color(0.8f, 0.2f, 0.05f, 0.6f); // Kırmızı kenar
        Color smoke = new Color(0.4f, 0.38f, 0.35f, 0.4f);   // Duman

        // Duman izi (en arkada)
        pixels[4 * w + 0] = new Color(smoke.r, smoke.g, smoke.b, 0.2f);
        pixels[7 * w + 0] = new Color(smoke.r, smoke.g, smoke.b, 0.2f);
        pixels[5 * w + 0] = new Color(smoke.r, smoke.g, smoke.b, 0.3f);
        pixels[6 * w + 0] = new Color(smoke.r, smoke.g, smoke.b, 0.3f);

        // Alev efekti (gradient daireler)
        Vector2 flameCenter = new Vector2(2f, 5.5f);
        for (int y = 3; y < 9; y++)
        {
            for (int x = 1; x < 5; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), flameCenter);
                if (dist < 3f)
                {
                    float t = dist / 3f;
                    Color flameColor;
                    if (t < 0.3f)
                        flameColor = Color.Lerp(flameCore, flameInner, t / 0.3f);
                    else if (t < 0.7f)
                        flameColor = Color.Lerp(flameInner, flameOuter, (t - 0.3f) / 0.4f);
                    else
                        flameColor = Color.Lerp(flameOuter, flameEdge, (t - 0.7f) / 0.3f);

                    pixels[y * w + x] = flameColor;
                }
            }
        }

        // Roket gövdesi (3D silindir)
        for (int x = 5; x < 17; x++)
        {
            for (int y = 4; y < 8; y++)
            {
                float yNorm = (y - 4f) / 3f;
                Color c;
                if (yNorm < 0.3f)
                    c = bodyLight;
                else if (yNorm > 0.7f)
                    c = bodyDark;
                else
                    c = body;
                pixels[y * w + x] = c;
            }
        }

        // Gövde detay çizgileri
        for (int y = 4; y < 8; y++)
        {
            pixels[y * w + 7] = bodyDark;   // Halka 1
            pixels[y * w + 12] = bodyDark;  // Halka 2
        }

        // Kırmızı savaş başlığı (3D koni)
        for (int x = 17; x < 22; x++)
        {
            float xProgress = (x - 17f) / 4f;
            float halfHeight = 2f - (xProgress * 1.2f); // Daralma

            for (int y = 4; y < 8; y++)
            {
                float yFromCenter = Mathf.Abs(y - 5.5f);
                if (yFromCenter <= halfHeight)
                {
                    float yNorm = (y - 4f) / 3f;
                    Color c;
                    if (yNorm < 0.3f)
                        c = warheadLight;
                    else if (yNorm > 0.7f)
                        c = warheadDark;
                    else
                        c = warhead;

                    // Uca doğru parlaklaşma
                    c = Color.Lerp(c, warheadLight, xProgress * 0.3f);
                    pixels[y * w + x] = c;
                }
            }
        }

        // Sivri uç
        pixels[5 * w + 22] = warheadLight;
        pixels[6 * w + 22] = warheadLight;
        pixels[5 * w + 23] = new Color(1f, 0.6f, 0.45f);
        pixels[6 * w + 23] = new Color(1f, 0.6f, 0.45f);

        // Kanatlar (3D görünüm)
        for (int x = 5; x < 9; x++)
        {
            float xNorm = (x - 5f) / 3f;
            // Üst kanat
            pixels[2 * w + x] = finLight;
            pixels[3 * w + x] = Color.Lerp(fin, finLight, xNorm * 0.5f);
            // Alt kanat
            pixels[8 * w + x] = Color.Lerp(fin, finLight, xNorm * 0.5f);
            pixels[9 * w + x] = fin;
        }
        // Kanat uçları
        pixels[1 * w + 5] = finLight;
        pixels[1 * w + 6] = fin;
        pixels[10 * w + 5] = fin;
        pixels[10 * w + 6] = fin;

        return CreateSprite(tex, pixels, w, h);
    }

    // === FLAME - Gerçekçi alev parçacığı ===
    static Sprite CreateFlameParticle()
    {
        int w = 14, h = 12;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Alev renk paleti
        Color flameWhite = new Color(1f, 1f, 0.95f);         // Çok sıcak merkez
        Color flameYellow = new Color(1f, 0.95f, 0.4f);      // Sarı
        Color flameOrange = new Color(1f, 0.65f, 0.15f);     // Turuncu
        Color flameRed = new Color(1f, 0.35f, 0.08f);        // Kırmızı
        Color flameDark = new Color(0.7f, 0.15f, 0.02f, 0.6f); // Koyu kırmızı kenar

        // Alev şekli - Organik görünüm için offset merkezler
        Vector2 mainCenter = new Vector2(7f, 5f);
        Vector2 topFlame = new Vector2(8f, 8f);   // Yukarı uzanan alev
        Vector2 sideFlame = new Vector2(10f, 5f); // Yana uzanan alev

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Ana alev gövdesi
                float mainDist = Vector2.Distance(new Vector2(x, y), mainCenter);
                float topDist = Vector2.Distance(new Vector2(x, y), topFlame);
                float sideDist = Vector2.Distance(new Vector2(x, y), sideFlame);

                // Birleşik alev şekli
                float combinedDist = Mathf.Min(mainDist * 0.9f, topDist * 1.1f, sideDist * 1.2f);

                if (combinedDist < 5.5f)
                {
                    float t = combinedDist / 5.5f;

                    Color flameColor;
                    if (t < 0.2f)
                        flameColor = Color.Lerp(flameWhite, flameYellow, t / 0.2f);
                    else if (t < 0.45f)
                        flameColor = Color.Lerp(flameYellow, flameOrange, (t - 0.2f) / 0.25f);
                    else if (t < 0.75f)
                        flameColor = Color.Lerp(flameOrange, flameRed, (t - 0.45f) / 0.3f);
                    else
                        flameColor = Color.Lerp(flameRed, flameDark, (t - 0.75f) / 0.25f);

                    // Kenar alpha azaltma
                    if (t > 0.7f)
                        flameColor.a = Mathf.Lerp(1f, 0.3f, (t - 0.7f) / 0.3f);

                    pixels[y * w + x] = flameColor;
                }
            }
        }

        // Ekstra parlak merkez noktaları
        pixels[5 * w + 6] = flameWhite;
        pixels[5 * w + 7] = flameWhite;
        pixels[6 * w + 7] = new Color(1f, 1f, 0.9f, 0.95f);

        // Alev sivri uçları (üst)
        pixels[9 * w + 8] = new Color(flameOrange.r, flameOrange.g, flameOrange.b, 0.7f);
        pixels[10 * w + 8] = new Color(flameRed.r, flameRed.g, flameRed.b, 0.5f);
        pixels[10 * w + 9] = new Color(flameRed.r, flameRed.g, flameRed.b, 0.3f);

        return CreateSprite(tex, pixels, w, h);
    }

    // === GRENADE - M67 tipi el bombası ===
    static Sprite CreateGrenade(Color baseColor)
    {
        int w = 14, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        Clear(pixels);

        // Bomba renkleri (Zeytin yeşili)
        Color body = new Color(0.28f, 0.35f, 0.22f);
        Color bodyLight = new Color(0.4f, 0.48f, 0.32f);
        Color bodyDark = new Color(0.18f, 0.24f, 0.14f);
        Color bodyVeryDark = new Color(0.12f, 0.16f, 0.1f);

        // Metal parçalar
        Color metal = new Color(0.48f, 0.5f, 0.46f);
        Color metalLight = new Color(0.65f, 0.68f, 0.62f);
        Color metalDark = new Color(0.32f, 0.35f, 0.3f);

        // Kıvılcım ve fitil
        Color spark = new Color(1f, 0.85f, 0.3f);
        Color sparkGlow = new Color(1f, 0.7f, 0.2f, 0.6f);
        Color fuse = new Color(0.35f, 0.28f, 0.2f);

        Vector2 center = new Vector2(7, 7);

        // Bomba gövdesi (3D küre efekti)
        for (int y = 2; y < 13; y++)
        {
            for (int x = 2; x < 12; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 5.5f)
                {
                    // 3D küre gölgeleme - sol üst parlak, sağ alt koyu
                    float xNorm = (x - 2f) / 9f;
                    float yNorm = (y - 2f) / 10f;
                    float lightFactor = 1f - (xNorm * 0.4f + yNorm * 0.6f);

                    Color c;
                    if (lightFactor > 0.7f)
                        c = bodyLight;
                    else if (lightFactor > 0.4f)
                        c = body;
                    else if (lightFactor > 0.2f)
                        c = bodyDark;
                    else
                        c = bodyVeryDark;

                    pixels[y * w + x] = c;
                }
            }
        }

        // Oluklu doku (waffle pattern) - daha detaylı
        for (int y = 3; y < 12; y++)
        {
            for (int x = 3; x < 11; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 5f)
                {
                    // Grid pattern
                    bool isGroove = (y % 2 == 0) || (x % 2 == 0);
                    if (isGroove)
                    {
                        Color current = pixels[y * w + x];
                        pixels[y * w + x] = Color.Lerp(current, bodyDark, 0.4f);
                    }
                }
            }
        }

        // Üst metal kapak (3D silindir)
        for (int x = 5; x < 9; x++)
        {
            float xNorm = (x - 5f) / 3f;
            // Alt kenar
            pixels[12 * w + x] = metalDark;
            // Gövde
            if (xNorm < 0.3f)
            {
                pixels[13 * w + x] = metalLight;
                pixels[14 * w + x] = metalLight;
            }
            else if (xNorm > 0.7f)
            {
                pixels[13 * w + x] = metalDark;
                pixels[14 * w + x] = metalDark;
            }
            else
            {
                pixels[13 * w + x] = metal;
                pixels[14 * w + x] = metal;
            }
        }

        // Güvenlik kolu (spoon) - yan tarafta
        pixels[10 * w + 11] = metalDark;
        pixels[11 * w + 11] = metal;
        pixels[12 * w + 11] = metal;
        pixels[13 * w + 11] = metalLight;
        pixels[13 * w + 12] = metal;

        // Fitil halkası
        pixels[14 * w + 5] = metalDark;
        pixels[14 * w + 8] = metalDark;

        // Fitil gövdesi
        pixels[15 * w + 6] = fuse;
        pixels[15 * w + 7] = fuse;

        // Kıvılcım efekti (animasyonlu görünüm)
        pixels[15 * w + 8] = spark;
        pixels[15 * w + 9] = new Color(spark.r, spark.g, spark.b, 0.8f);
        pixels[14 * w + 9] = sparkGlow;
        pixels[14 * w + 10] = new Color(sparkGlow.r, sparkGlow.g, sparkGlow.b, 0.4f);

        // Ekstra kıvılcım parçacıkları
        pixels[13 * w + 10] = new Color(1f, 0.9f, 0.5f, 0.5f);

        // Parlama noktası (sol üst)
        pixels[4 * w + 4] = new Color(bodyLight.r + 0.15f, bodyLight.g + 0.15f, bodyLight.b + 0.1f);
        pixels[5 * w + 4] = bodyLight;

        return CreateSprite(tex, pixels, w, h);
    }

    // === YARDIMCI FONKSİYONLAR ===

    static void Clear(Color[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
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

    static Sprite CreateSprite(Texture2D tex, Color[] pixels, int w, int h)
    {
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
    }
}
