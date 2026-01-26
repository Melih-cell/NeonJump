using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gelişmiş slot bazlı silah yönetim sistemi
/// 3 slot: Primary (Ana), Secondary (İkincil), Special (Özel)
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance;

    [Header("Weapon Slots")]
    public WeaponInstance primaryWeapon;      // Ana silah (Rifle, Shotgun, SMG, Sniper)
    public WeaponInstance secondaryWeapon;    // İkincil silah (Pistol - her zaman var)
    public WeaponInstance specialWeapon;      // Özel silah (Rocket, Flamethrower, Grenade)

    [Header("Current State")]
    public int currentSlot = 1;               // 0=Primary, 1=Secondary, 2=Special
    public bool isReloading = false;
    public float lastFireTime = 0f;

    [Header("References")]
    public Transform firePoint;
    public GameObject muzzleFlashPrefab;

    // Events
    public System.Action<WeaponInstance> OnWeaponChanged;
    public System.Action<int, int> OnAmmoChanged;
    public System.Action OnReloadStarted;
    public System.Action OnReloadFinished;
    public System.Action<WeaponInstance, int> OnWeaponUpgraded; // Silah, yeni level

    private Coroutine reloadCoroutine;
    private SpriteRenderer playerSpriteRenderer;
    private Transform playerTransform; // Player'ın ana transform'u (flip kontrolü için)

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        // Sahne yeniden yüklendiğinde static referansı temizle
        if (Instance == this)
        {
            Instance = null;
        }

        // Event'leri temizle
        OnWeaponChanged = null;
        OnAmmoChanged = null;
        OnReloadStarted = null;
        OnReloadFinished = null;
        OnWeaponUpgraded = null;
    }

    void Start()
    {
        // Önce cache'leri temizle (sahne yeniden yüklendiğinde eski referanslar temizlenir)
        WeaponSpriteLoader.ClearCache();

        // Tüm silahları sıfırla
        primaryWeapon = null;
        secondaryWeapon = null;
        specialWeapon = null;
        isReloading = false;
        lastFireTime = 0f;

        // Başlangıçta sadece tabanca var
        secondaryWeapon = new WeaponInstance(WeaponType.Pistol);
        secondaryWeapon.isUnlocked = true;

        // Diğer slotlar boş başlar
        primaryWeapon = null;
        specialWeapon = null;

        // İkincil silahla başla
        currentSlot = 1;

        // Player transform'unu bul (flip kontrolü için)
        // Önce PlayerController'ı ara, yoksa parent'a bak
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
        else
        {
            pc = GetComponentInParent<PlayerController>();
            if (pc != null)
                playerTransform = pc.transform;
            else
                playerTransform = transform.root; // Fallback: root transform
        }

        // Player sprite renderer bul
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Fire point yoksa oluştur - Player'ın child'ı olarak
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(playerTransform); // Player'a parent yap, WeaponManager'a değil
            fp.transform.localPosition = new Vector3(0.6f, 0.35f, 0); // Silah/kol hizası
            firePoint = fp.transform;
        }

        // Silah UI sistemlerini otomatik oluştur
        CreateWeaponUISystems();

        NotifyWeaponChanged();
    }

    /// <summary>
    /// Silah UI sistemlerini oluştur - Yeni Hotbar sistemi
    /// </summary>
    void CreateWeaponUISystems()
    {
        // Yeni Hotbar sistemi
        if (WeaponHotbar.Instance == null)
        {
            GameObject hotbarObj = new GameObject("WeaponHotbar");
            hotbarObj.AddComponent<WeaponHotbar>();
        }

        // WeaponUpgradeUI (upgrade icin hala gerekli)
        if (WeaponUpgradeUI.Instance == null)
        {
            GameObject upgradeObj = new GameObject("WeaponUpgradeUI");
            upgradeObj.AddComponent<WeaponUpgradeUI>();
        }
    }

    void Update()
    {
        HandleWeaponSwitch();
        HandleReloadInput();
        UpdateFirePointPosition();
    }

    // Son nisan yonu (FirePoint pozisyonu icin)
    private Vector2 lastAimDirection = Vector2.right;

    // FirePoint offset ayarlari
    [Header("FirePoint Settings")]
    public float firePointDistance = 0.55f;
    public float firePointBaseY = 0.25f;  // Karakterin ortasindan biraz yukari

    void UpdateFirePointPosition()
    {
        if (firePoint == null || playerTransform == null) return;

        // World space'de FirePoint pozisyonunu hesapla
        Vector3 playerCenter = playerTransform.position + Vector3.up * firePointBaseY;

        // Nisan yonune gore offset (world space)
        Vector3 aimOffset;

        if (lastAimDirection.sqrMagnitude > 0.1f)
        {
            // Nisan yonu varsa, o yone dogru offset
            aimOffset = new Vector3(
                lastAimDirection.x * firePointDistance,
                lastAimDirection.y * firePointDistance,
                0
            );
        }
        else
        {
            // Varsayilan: karakterin baktigi yone
            float facingDir = playerTransform.localScale.x > 0 ? 1f : -1f;
            aimOffset = new Vector3(facingDir * firePointDistance, 0, 0);
        }

        // World space pozisyon
        Vector3 worldPos = playerCenter + aimOffset;

        // FirePoint'i world pozisyonuna tasi
        firePoint.position = worldPos;
    }

    /// <summary>
    /// Nisan yonunu guncelle (FirePoint pozisyonu icin)
    /// </summary>
    public void UpdateAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.1f)
        {
            lastAimDirection = direction.normalized;
        }
        UpdateFirePointPosition();
    }

    /// <summary>
    /// Mevcut nisan yonunu dondur
    /// </summary>
    public Vector2 GetAimDirection()
    {
        return lastAimDirection;
    }

    void HandleWeaponSwitch()
    {
        // Silah degistirme artik WeaponHotbar tarafindan yonetiliyor
        // Bu fonksiyon bos birakildi - eski kod kaldirildi
    }

    void HandleReloadInput()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame && !isReloading)
        {
            TryReload();
        }
    }

    public void SwitchToSlot(int slot)
    {
        WeaponInstance target = GetWeaponAtSlot(slot);
        if (target == null || !target.isUnlocked) return;

        if (currentSlot != slot)
        {
            // Reload iptal
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                isReloading = false;
                OnReloadFinished?.Invoke();
            }

            currentSlot = slot;
            NotifyWeaponChanged();

            // Silah değiştirme sesi
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }
    }

    void SwitchToNextWeapon()
    {
        List<int> availableSlots = GetAvailableSlots();
        if (availableSlots.Count <= 1) return;

        int currentIndex = availableSlots.IndexOf(currentSlot);
        int nextIndex = (currentIndex + 1) % availableSlots.Count;
        SwitchToSlot(availableSlots[nextIndex]);
    }

    void SwitchToPreviousWeapon()
    {
        List<int> availableSlots = GetAvailableSlots();
        if (availableSlots.Count <= 1) return;

        int currentIndex = availableSlots.IndexOf(currentSlot);
        int prevIndex = (currentIndex - 1 + availableSlots.Count) % availableSlots.Count;
        SwitchToSlot(availableSlots[prevIndex]);
    }

    List<int> GetAvailableSlots()
    {
        List<int> slots = new List<int>();

        if (primaryWeapon != null && primaryWeapon.isUnlocked) slots.Add(0);
        if (secondaryWeapon != null && secondaryWeapon.isUnlocked) slots.Add(1);
        if (specialWeapon != null && specialWeapon.isUnlocked) slots.Add(2);

        return slots;
    }

    WeaponInstance GetWeaponAtSlot(int slot)
    {
        switch (slot)
        {
            case 0: return primaryWeapon;
            case 1: return secondaryWeapon;
            case 2: return specialWeapon;
            default: return null;
        }
    }

    public WeaponInstance GetCurrentWeapon()
    {
        return GetWeaponAtSlot(currentSlot);
    }

    /// <summary>
    /// Ateş et - PlayerController'dan çağrılır
    /// </summary>
    public bool TryFire(Vector2 direction)
    {
        if (isReloading) return false;

        WeaponInstance weapon = GetCurrentWeapon();
        if (weapon == null) return false;

        // Ateş hızı kontrolü - Efektif fire rate kullan (Rarity + Level bonuslu)
        if (Time.time - lastFireTime < weapon.GetEffectiveFireRate()) return false;

        // Mermi kontrolü
        if (!weapon.CanFire())
        {
            // Otomatik reload
            TryReload();
            return false;
        }

        // Ateş!
        lastFireTime = Time.time;
        weapon.Fire();

        // Mermileri oluştur
        SpawnBullets(weapon, direction);

        // Muzzle flash (yön bilgisiyle)
        CreateMuzzleFlash(weapon, direction);

        // Ammo UI güncelle
        OnAmmoChanged?.Invoke(weapon.currentAmmo, weapon.reserveAmmo);

        return true;
    }

    void SpawnBullets(WeaponInstance weapon, Vector2 direction)
    {
        WeaponData data = weapon.data;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        for (int i = 0; i < data.bulletsPerShot; i++)
        {
            // Yayılım hesapla
            float spreadAngle = 0f;
            if (data.spread > 0)
            {
                // Shotgun için düzenli yayılım
                if (data.type == WeaponType.Shotgun && data.bulletsPerShot > 1)
                {
                    float totalSpread = data.spread * 2;
                    spreadAngle = -data.spread + (totalSpread / (data.bulletsPerShot - 1)) * i;
                }
                else
                {
                    spreadAngle = Random.Range(-data.spread, data.spread);
                }
            }

            // Yönü döndür
            Vector2 bulletDir = RotateVector(direction.normalized, spreadAngle);

            // Mermi oluştur
            CreateBullet(spawnPos, bulletDir, data);
        }
    }

    void CreateBullet(Vector3 position, Vector2 direction, WeaponData data)
    {
        GameObject bullet = new GameObject("Bullet_" + data.type.ToString());
        bullet.transform.position = position;
        bullet.layer = LayerMask.NameToLayer("Default");

        // Sprite
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // Silah tipine göre mermi görünümü
        CreateBulletSprite(sr, data);

        // Mermi boyutunu ayarla (indirilen sprite'lar 16x16, çok büyük görünüyor)
        float bulletScale = GetBulletScale(data.type);
        bullet.transform.localScale = new Vector3(bulletScale, bulletScale, 1f);

        // Rotasyon
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Rigidbody
        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();

        // Silah tipine göre fizik
        switch (data.type)
        {
            case WeaponType.GrenadeLauncher:
                rb.gravityScale = 1f;
                rb.linearVelocity = direction * data.bulletSpeed + Vector2.up * 3f;
                break;

            case WeaponType.RocketLauncher:
                rb.gravityScale = 0.1f;
                rb.linearVelocity = direction * data.bulletSpeed;
                break;

            case WeaponType.Flamethrower:
                rb.gravityScale = -0.3f; // Yukarı doğru hareket
                rb.linearVelocity = direction * data.bulletSpeed + (Vector2)Random.insideUnitCircle * 2f;
                rb.angularVelocity = Random.Range(-180f, 180f);
                break;

            default:
                rb.gravityScale = 0f;
                rb.linearVelocity = direction * data.bulletSpeed;
                break;
        }

        // Collider
        if (data.type == WeaponType.GrenadeLauncher)
        {
            CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            // Grenade için collision (trigger değil)
        }
        else
        {
            BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
            col.size = GetColliderSize(data.type);
            col.isTrigger = true;
        }

        // Bullet component - Efektif statları kullan (Rarity + Level bonuslu)
        WeaponInstance currentWeapon = GetCurrentWeapon();
        Bullet bulletComp = bullet.AddComponent<Bullet>();
        bulletComp.damage = currentWeapon != null ? currentWeapon.GetEffectiveDamage() : data.damage;
        bulletComp.weaponType = data.type;
        bulletComp.hasExplosion = data.hasExplosion;
        bulletComp.explosionRadius = data.explosionRadius;
        bulletComp.isPiercing = data.isPiercing;
        bulletComp.maxRange = currentWeapon != null ? currentWeapon.GetEffectiveRange() : data.range;
        bulletComp.startPosition = position;
        bulletComp.bulletColor = data.bulletColor;

        // Rarity'ye göre mermi rengi tonu
        if (currentWeapon != null && currentWeapon.rarity != WeaponRarity.Common)
        {
            Color rarityTint = WeaponRarityHelper.GetRarityColor(currentWeapon.rarity);
            bulletComp.bulletColor = Color.Lerp(data.bulletColor, rarityTint, 0.3f);
        }

        // Otomatik yok olma
        float lifetime = data.type == WeaponType.Flamethrower ? 0.5f :
                        data.type == WeaponType.GrenadeLauncher ? 3f : 5f;
        Destroy(bullet, lifetime);
    }

    void CreateBulletSprite(SpriteRenderer sr, WeaponData data)
    {
        // BulletVisuals sistemini kullan (kendi içinde renkler tanımlı)
        sr.sprite = BulletVisuals.CreateBulletSprite(data.type, data.bulletColor);

        // Yeni procedural sprite'lar kendi renklerini içeriyor
        // Bu yüzden sr.color'ı beyaz bırakıyoruz (tint yok)
        sr.color = Color.white;

        // Flamethrower için rastgele renk varyasyonu
        if (data.type == WeaponType.Flamethrower)
        {
            sr.color = new Color(1f, Random.Range(0.8f, 1f), Random.Range(0.7f, 1f), 0.9f);
        }
    }

    Vector2 GetColliderSize(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol: return new Vector2(0.15f, 0.15f);
            case WeaponType.Rifle:
            case WeaponType.SMG: return new Vector2(0.2f, 0.1f);
            case WeaponType.Shotgun: return new Vector2(0.1f, 0.1f);
            case WeaponType.Sniper: return new Vector2(0.3f, 0.08f);
            case WeaponType.RocketLauncher: return new Vector2(0.25f, 0.15f);
            case WeaponType.Flamethrower: return new Vector2(0.15f, 0.15f);
            case WeaponType.GrenadeLauncher: return new Vector2(0.2f, 0.2f);
            default: return new Vector2(0.15f, 0.1f);
        }
    }

    /// <summary>
    /// Silah tipine göre mermi ölçeği
    /// Procedural sprite'lar için optimize edilmiş (küçültülmüş)
    /// </summary>
    float GetBulletScale(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol:
                return 0.5f;        // Küçük mermi
            case WeaponType.Rifle:
                return 0.55f;       // Uzun mermi
            case WeaponType.SMG:
                return 0.45f;       // Küçük hızlı mermi
            case WeaponType.Shotgun:
                return 0.4f;        // Metalik saçma pellet
            case WeaponType.Sniper:
                return 0.6f;        // Uzun ince mermi
            case WeaponType.RocketLauncher:
                return 0.7f;        // Büyük roket
            case WeaponType.Flamethrower:
                return 0.4f;        // Alev parçacığı
            case WeaponType.GrenadeLauncher:
                return 0.55f;       // Bomba
            default:
                return 0.5f;
        }
    }

    // === MUZZLE FLASH ===

    void CreateMuzzleFlash(WeaponInstance weapon, Vector2 direction)
    {
        if (firePoint == null) return;

        WeaponData data = weapon.data;
        Vector3 pos = firePoint.position;

        // Muzzle flash sprite
        GameObject flash = new GameObject("MuzzleFlash");
        flash.transform.position = pos;

        // Ateş yönüne göre rotasyon
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        flash.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Sola bakıyorsa sprite'ı flip et
        if (direction.x < 0)
        {
            flash.transform.localScale = new Vector3(1, -1, 1);
        }

        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 11;

        // Silah tipine göre flash
        int flashSize = data.type == WeaponType.Shotgun ? 16 :
                       data.type == WeaponType.RocketLauncher ? 20 :
                       data.type == WeaponType.Flamethrower ? 12 : 10;

        Texture2D tex = CreateMuzzleFlashTexture(flashSize, data.muzzleFlashColor);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, flashSize, flashSize), new Vector2(0.2f, 0.5f), 16);
        sr.color = data.muzzleFlashColor;

        // Hızlı fade out
        StartCoroutine(FadeMuzzleFlash(flash, sr));
    }

    Texture2D CreateMuzzleFlashTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size * 0.3f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Yıldız şekli
                float distX = Mathf.Abs(x - center.x);
                float distY = Mathf.Abs(y - center.y);

                bool inFlash = (distX < size * 0.4f && distY < size * 0.15f) ||
                              (distX < size * 0.15f && distY < size * 0.3f);

                if (inFlash)
                {
                    float intensity = 1f - (distX + distY) / size;
                    colors[y * size + x] = new Color(1f, 1f, 1f, intensity);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }

    IEnumerator FadeMuzzleFlash(GameObject flash, SpriteRenderer sr)
    {
        float duration = 0.05f;
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            // Büyüme efekti
            float scale = Mathf.Lerp(0.8f, 1.2f, elapsed / duration);
            flash.transform.localScale = new Vector3(scale, scale, 1);

            yield return null;
        }

        Destroy(flash);
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    /// <summary>
    /// Reload başlat
    /// </summary>
    public void TryReload()
    {
        if (isReloading) return;

        WeaponInstance weapon = GetCurrentWeapon();
        if (weapon == null) return;

        if (!weapon.CanReload()) return;

        reloadCoroutine = StartCoroutine(ReloadCoroutine(weapon));
    }

    IEnumerator ReloadCoroutine(WeaponInstance weapon)
    {
        isReloading = true;
        OnReloadStarted?.Invoke();

        // Reload süresi - Efektif reload time kullan (Level bonuslu)
        yield return new WaitForSeconds(weapon.GetEffectiveReloadTime());

        // Reload tamamla
        weapon.Reload();
        isReloading = false;

        OnReloadFinished?.Invoke();
        OnAmmoChanged?.Invoke(weapon.currentAmmo, weapon.reserveAmmo);
    }

    /// <summary>
    /// Yeni silah ekle/değiştir (rastgele rarity ile)
    /// </summary>
    public bool AddWeapon(WeaponType type)
    {
        return AddWeapon(type, WeaponRarityHelper.GetRandomRarity());
    }

    /// <summary>
    /// Yeni silah ekle/değiştir (belirli rarity ile)
    /// </summary>
    public bool AddWeapon(WeaponType type, WeaponRarity rarity)
    {
        WeaponData tempData = WeaponData.Create(type);
        WeaponCategory category = tempData.category;

        WeaponInstance newWeapon = new WeaponInstance(type, rarity);
        newWeapon.isUnlocked = true;

        switch (category)
        {
            case WeaponCategory.Primary:
                if (primaryWeapon != null && primaryWeapon.isUnlocked && primaryWeapon.data.type == type)
                {
                    // Aynı silah varsa: daha iyi rarity ise değiştir, değilse mermi ekle
                    if (rarity > primaryWeapon.rarity)
                    {
                        primaryWeapon = newWeapon;
                        ShowRarityNotification(newWeapon);
                    }
                    else
                    {
                        primaryWeapon.AddAmmo(newWeapon.data.maxReserveAmmo / 2);
                    }
                    OnAmmoChanged?.Invoke(GetCurrentWeapon().currentAmmo, GetCurrentWeapon().reserveAmmo);
                    NotifyWeaponChanged();
                    return true;
                }
                primaryWeapon = newWeapon;
                SwitchToSlot(0);
                ShowRarityNotification(newWeapon);
                break;

            case WeaponCategory.Secondary:
                if (secondaryWeapon != null)
                {
                    if (rarity > secondaryWeapon.rarity)
                    {
                        secondaryWeapon = newWeapon;
                        ShowRarityNotification(newWeapon);
                    }
                    else
                    {
                        secondaryWeapon.AddAmmo(newWeapon.data.maxReserveAmmo / 2);
                    }
                    OnAmmoChanged?.Invoke(GetCurrentWeapon().currentAmmo, GetCurrentWeapon().reserveAmmo);
                    NotifyWeaponChanged();
                    return true;
                }
                secondaryWeapon = newWeapon;
                break;

            case WeaponCategory.Special:
                if (specialWeapon != null && specialWeapon.isUnlocked && specialWeapon.data.type == type)
                {
                    if (rarity > specialWeapon.rarity)
                    {
                        specialWeapon = newWeapon;
                        ShowRarityNotification(newWeapon);
                    }
                    else
                    {
                        specialWeapon.AddAmmo(newWeapon.data.maxReserveAmmo / 2);
                    }
                    OnAmmoChanged?.Invoke(GetCurrentWeapon().currentAmmo, GetCurrentWeapon().reserveAmmo);
                    NotifyWeaponChanged();
                    return true;
                }
                specialWeapon = newWeapon;
                SwitchToSlot(2);
                ShowRarityNotification(newWeapon);
                break;
        }

        NotifyWeaponChanged();
        return true;
    }

    /// <summary>
    /// Nadir silah bildirim göster
    /// </summary>
    void ShowRarityNotification(WeaponInstance weapon)
    {
        if (weapon.rarity >= WeaponRarity.Rare && NotificationManager.Instance != null)
        {
            string rarityName = WeaponRarityHelper.GetRarityName(weapon.rarity);
            NotificationManager.Instance.ShowNotification(
                $"{rarityName.ToUpper()} SILAH!",
                weapon.GetDisplayName(),
                weapon.rarity >= WeaponRarity.Epic ? NotificationType.Achievement : NotificationType.WeaponPickup
            );
        }
    }

    /// <summary>
    /// Mevcut silahı upgrade et
    /// </summary>
    public bool TryUpgradeCurrentWeapon()
    {
        WeaponInstance weapon = GetCurrentWeapon();
        if (weapon == null) return false;

        if (!weapon.CanUpgrade())
        {
            Debug.Log("Silah maksimum seviyede!");
            return false;
        }

        int cost = weapon.GetUpgradeCost();

        // GameManager'dan coin al
        if (GameManager.Instance == null) return false;

        int currentCoins = GameManager.Instance.GetCoins();
        if (currentCoins < cost)
        {
            Debug.Log($"Yetersiz coin! Gereken: {cost}, Mevcut: {currentCoins}");

            // Yetersiz coin bildirimi
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    "YETERSIZ COIN",
                    $"Gereken: {cost} coin",
                    NotificationType.Warning
                );
            }
            return false;
        }

        // Coin harca (SaveManager üzerinden)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SpendCoins(cost);
        }

        // Upgrade yap
        int oldLevel = weapon.level;
        weapon.level++;

        Debug.Log($"{weapon.data.weaponName} Level {oldLevel} -> {weapon.level}!");

        // Event tetikle
        OnWeaponUpgraded?.Invoke(weapon, weapon.level);
        NotifyWeaponChanged();

        // Başarı bildirimi
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification(
                "UPGRADE BASARILI!",
                $"{weapon.data.weaponName} +{weapon.level}",
                NotificationType.LevelUp
            );
        }

        // Ses efekti
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPowerUp();
        }

        return true;
    }

    /// <summary>
    /// Belirli slottaki silahı upgrade et
    /// </summary>
    public bool TryUpgradeWeapon(int slot)
    {
        WeaponInstance weapon = GetWeaponAtSlot(slot);
        if (weapon == null || !weapon.isUnlocked) return false;

        int originalSlot = currentSlot;
        currentSlot = slot;
        bool result = TryUpgradeCurrentWeapon();
        currentSlot = originalSlot;

        return result;
    }

    /// <summary>
    /// Mevcut silaha mermi ekle
    /// </summary>
    public void AddAmmoToCurrentWeapon(int amount)
    {
        WeaponInstance weapon = GetCurrentWeapon();
        if (weapon != null)
        {
            weapon.AddAmmo(amount);
            OnAmmoChanged?.Invoke(weapon.currentAmmo, weapon.reserveAmmo);
        }
    }

    void NotifyWeaponChanged()
    {
        WeaponInstance current = GetCurrentWeapon();
        OnWeaponChanged?.Invoke(current);

        if (current != null)
        {
            OnAmmoChanged?.Invoke(current.currentAmmo, current.reserveAmmo);
        }
    }

    /// <summary>
    /// Otomatik silah mı?
    /// </summary>
    public bool IsCurrentWeaponAutomatic()
    {
        WeaponInstance weapon = GetCurrentWeapon();
        return weapon != null && weapon.data.isAutomatic;
    }

    /// <summary>
    /// Test için silah ekle (farklı rarity'ler ile)
    /// </summary>
    public void AddTestWeapons()
    {
        AddWeapon(WeaponType.Rifle, WeaponRarity.Rare);
        AddWeapon(WeaponType.RocketLauncher, WeaponRarity.Epic);
        Debug.Log("Test silahları eklendi: Rare Rifle ve Epic RocketLauncher");
    }

    /// <summary>
    /// Tüm rarity'lerde silah test et
    /// </summary>
    [ContextMenu("Add All Rarity Test Weapons")]
    public void AddAllRarityTestWeapons()
    {
        AddWeapon(WeaponType.SMG, WeaponRarity.Common);
        AddWeapon(WeaponType.Rifle, WeaponRarity.Uncommon);
        AddWeapon(WeaponType.Shotgun, WeaponRarity.Rare);
        AddWeapon(WeaponType.Sniper, WeaponRarity.Epic);
        AddWeapon(WeaponType.RocketLauncher, WeaponRarity.Legendary);
        Debug.Log("Tüm rarity'lerde test silahları eklendi!");
    }

    /// <summary>
    /// Mevcut silah bilgisi
    /// </summary>
    public string GetCurrentWeaponInfo()
    {
        WeaponInstance weapon = GetCurrentWeapon();
        if (weapon == null) return "Silah yok";

        return $"{weapon.GetDisplayName()}: {weapon.currentAmmo}/{weapon.GetEffectiveMaxAmmo()} | {weapon.reserveAmmo}";
    }

    /// <summary>
    /// Detaylı silah bilgisi (UI için)
    /// </summary>
    public string GetCurrentWeaponDetailedInfo()
    {
        WeaponInstance weapon = GetCurrentWeapon();
        if (weapon == null) return "Silah yok";

        string rarityName = WeaponRarityHelper.GetRarityName(weapon.rarity);
        return $"[{rarityName}] {weapon.data.weaponName} +{weapon.level}\n" +
               $"Hasar: {weapon.GetEffectiveDamage()} | Hiz: {weapon.GetEffectiveFireRate():F2}s\n" +
               $"Sarjor: {weapon.currentAmmo}/{weapon.GetEffectiveMaxAmmo()} | Yedek: {weapon.reserveAmmo}";
    }
}
