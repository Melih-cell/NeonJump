using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Robot dusmani - ULTRA GELISMIS AI sistemi ile donatilmis boss-seviye dusman
/// EnemyBase'den turetilmis, Animator Controller ile entegre calisir
///
/// === SALDIRI MODLARI ===
/// - Uzak mesafe (>8 birim): Mermi atesi (predictive targeting)
/// - Orta mesafe (4-8 birim): Lazer saldirisi
/// - Yakin-orta mesafe (3-6 birim): Dash/Charge saldirisi
/// - Yakin mesafe (<=3 birim): Melee saldiri + combo
/// - Ground Slam: Havadan yere carpma saldirisi
/// - Teleport: Kisa mesafe isinlanma
///
/// === GELISMIS AI SISTEMLERI ===
/// - Predictive Targeting: Oyuncunun gidecegi yeri tahmin eder
/// - Adaptive AI: Oyuncunun hareketlerini ogrenip uyum saglar
/// - Phase System: Can seviyesine gore farkli davranis modlari
/// - Threat Assessment: Tehdit degerlendirmesi ve onceliklendirme
/// - Counter Attack: Oyuncu yaklasinca otomatik karsi saldiri
/// - Combo System: Zincirleme saldirilar
///
/// === EK SISTEMLER ===
/// - Shield (kalkan) sistemi - vuruslari emer, rejenerasyon yapar
/// - Rage Mode - can %30 altina dustugunde aktif olur
/// - Drone Spawn - Yardimci drone'lar cagirir
/// - Jump System: Platformlara ve engellere ziplayabilir
/// - Item Drop - olumde silah ve esya dusurur
/// - Ground/Edge/Wall Detection - coklu raycast sistemi
/// - Advanced Stuck Prevention - otomatik kurtarma mekanizmalari
/// - Neon Visual Effects - parlama ve renk efektleri
/// </summary>
public class RobotEnemy : EnemyBase
{
    #region Patrol Ayarlari

    [Header("Patrol Ayarlari")]
    [Tooltip("Devriye hizi")]
    public float patrolSpeed = 2f;

    [Tooltip("Baslangic noktasindan sol mesafe")]
    public float patrolLeftDistance = 4f;

    [Tooltip("Baslangic noktasindan sag mesafe")]
    public float patrolRightDistance = 4f;

    [Tooltip("Saga dogru baslasin mi")]
    public bool startMovingRight = true;

    [Tooltip("Donus noktasinda bekleme suresi")]
    public float waitTimeAtEdge = 0.3f;

    #endregion

    #region Algilama Ayarlari

    [Header("Algilama Ayarlari")]
    [Tooltip("Oyuncu algilama mesafesi")]
    public float detectionRange = 15f;

    [Tooltip("Gorus acisi kontrolu yapilsin mi")]
    public bool needsLineOfSight = true;

    [Tooltip("Engel layer'i")]
    public LayerMask obstacleLayer;

    [Tooltip("Oyuncuyu kacirdiktan sonra patrol'e donme suresi")]
    public float loseTargetDelay = 2f;

    #endregion

    #region Ground & Edge Detection Ayarlari

    [Header("Ground & Edge Detection")]
    [Tooltip("Zemin kontrol layer'i (0 ise obstacleLayer kullanilir)")]
    public LayerMask groundCheckLayer;

    [Tooltip("Zemin raycast mesafesi")]
    public float groundCheckDistance = 1.5f;

    [Tooltip("Kenar algilama raycast'i icin ileri mesafe")]
    public float edgeCheckForwardDistance = 1.0f;

    [Tooltip("Kenar algilama raycast'i icin asagi mesafe")]
    public float edgeCheckDownDistance = 2.0f;

    [Tooltip("Duvar algilama raycast mesafesi")]
    public float wallCheckDistance = 0.8f;

    [Tooltip("Zemin raycast'i icin offset (robotun alt kismindan)")]
    public float groundCheckYOffset = -0.5f;

    #endregion

    #region Stuck Detection Ayarlari

    [Header("Stuck Detection")]
    [Tooltip("Takilma tespit suresi (saniye)")]
    public float stuckDetectionTime = 1.0f;

    [Tooltip("Minimum hareket mesafesi (bu kadar hareket etmemisse takili sayilir)")]
    public float stuckMinDistance = 0.15f;

    [Tooltip("Takilma durumunda ziplama gucu")]
    public float stuckJumpForce = 8f;

    [Tooltip("Art arda takilma sayisi (bu kadar takilirsa teleport)")]
    public int stuckTeleportThreshold = 3;

    #endregion

    #region Jump System Ayarlari

    [Header("Jump System")]
    [Tooltip("Ziplama aktif mi")]
    public bool canJump = true;

    [Tooltip("Ziplama gucu")]
    public float jumpForce = 12f;

    [Tooltip("Platform algilama mesafesi")]
    public float platformDetectionRange = 3f;

    [Tooltip("Ziplama cooldown")]
    public float jumpCooldown = 2f;

    [Tooltip("Hava kontrolu (0-1)")]
    [Range(0f, 1f)]
    public float airControl = 0.6f;

    #endregion

    #region Teleport Ayarlari

    [Header("Teleport System")]
    [Tooltip("Teleport aktif mi")]
    public bool canTeleport = true;

    [Tooltip("Teleport menzili")]
    public float teleportRange = 5f;

    [Tooltip("Teleport cooldown")]
    public float teleportCooldown = 8f;

    [Tooltip("Teleport hasar esigi (bu kadar hasar alinca teleport)")]
    public float teleportDamageThreshold = 2f;

    [Tooltip("Teleport efekt rengi")]
    public Color teleportColor = new Color(0f, 1f, 1f, 0.8f);

    #endregion

    #region Ground Slam Ayarlari

    [Header("Ground Slam Attack")]
    [Tooltip("Ground slam aktif mi")]
    public bool canGroundSlam = true;

    [Tooltip("Ground slam hasari")]
    public int groundSlamDamage = 3;

    [Tooltip("Ground slam yukseklik")]
    public float groundSlamHeight = 6f;

    [Tooltip("Ground slam etki alani")]
    public float groundSlamRadius = 4f;

    [Tooltip("Ground slam cooldown")]
    public float groundSlamCooldown = 10f;

    [Tooltip("Ground slam knockback")]
    public float groundSlamKnockback = 12f;

    #endregion

    #region Drone System Ayarlari

    [Header("Drone System")]
    [Tooltip("Drone spawn aktif mi")]
    public bool canSpawnDrones = true;

    [Tooltip("Maksimum drone sayisi")]
    public int maxDrones = 2;

    [Tooltip("Drone spawn cooldown")]
    public float droneSpawnCooldown = 15f;

    [Tooltip("Drone prefab (null ise runtime olusturulur)")]
    public GameObject dronePrefab;

    #endregion

    #region Predictive Targeting Ayarlari

    [Header("Predictive Targeting")]
    [Tooltip("Tahminli nisanlama aktif mi")]
    public bool usePredictiveTargeting = true;

    [Tooltip("Tahmin suresi (saniye)")]
    public float predictionTime = 0.3f;

    [Tooltip("Tahmin hassasiyeti")]
    [Range(0f, 1f)]
    public float predictionAccuracy = 0.7f;

    #endregion

    #region Adaptive AI Ayarlari

    [Header("Adaptive AI")]
    [Tooltip("Uyarlanabilir AI aktif mi")]
    public bool useAdaptiveAI = true;

    [Tooltip("Ogrenme hizi")]
    [Range(0f, 1f)]
    public float learningRate = 0.1f;

    [Tooltip("Hafiza suresi (saniye)")]
    public float memoryDuration = 30f;

    [Tooltip("Kacinma pattern tespiti icin minimum ornek")]
    public int minPatternSamples = 3;

    #endregion

    #region Phase System Ayarlari

    [Header("Phase System")]
    [Tooltip("Phase sistemi aktif mi")]
    public bool usePhaseSystem = true;

    [Tooltip("Phase 2 can esigi")]
    [Range(0f, 1f)]
    public float phase2HealthThreshold = 0.6f;

    [Tooltip("Phase 3 can esigi")]
    [Range(0f, 1f)]
    public float phase3HealthThreshold = 0.3f;

    #endregion

    #region Combo Attack Ayarlari

    [Header("Combo Attack System")]
    [Tooltip("Combo saldiri aktif mi")]
    public bool canComboAttack = true;

    [Tooltip("Combo arasindaki bekleme suresi")]
    public float comboDelay = 0.3f;

    [Tooltip("Maksimum combo uzunlugu")]
    public int maxComboLength = 4;

    [Tooltip("Combo bonus hasar carpani")]
    public float comboDamageMultiplier = 1.2f;

    #endregion

    #region Counter Attack Ayarlari

    [Header("Counter Attack")]
    [Tooltip("Karsi saldiri aktif mi")]
    public bool canCounterAttack = true;

    [Tooltip("Karsi saldiri menzili")]
    public float counterAttackRange = 2.5f;

    [Tooltip("Karsi saldiri sansi")]
    [Range(0f, 1f)]
    public float counterAttackChance = 0.4f;

    [Tooltip("Karsi saldiri cooldown")]
    public float counterAttackCooldown = 3f;

    #endregion

    #region Boss Stats

    [Header("=== BOSS STATS (Black Myth Wukong Style) ===")]
    [Tooltip("Boss toplam cani")]
    public float bossHealth = 30000f;

    [Tooltip("Boss can barı gosterilsin mi")]
    public bool showBossHealthBar = true;

    [Tooltip("Boss ismi (UI'da gosterilir)")]
    public string bossName = "ROBOT MUHAFIZ";

    [Header("Zirh Sistemi")]
    [Tooltip("Temel zirh orani (gelen hasari azaltir)")]
    [Range(0f, 0.9f)]
    public float baseArmorRating = 0.6f;

    [Tooltip("Faz 2'de zirh bonusu")]
    [Range(0f, 0.3f)]
    public float phase2ArmorBonus = 0.1f;

    [Tooltip("Faz 3'te zirh bonusu")]
    [Range(0f, 0.3f)]
    public float phase3ArmorBonus = 0.2f;

    [Tooltip("Kritik vurus direnci (kafaya ziplayinca)")]
    [Range(0.1f, 1f)]
    public float criticalHitResistance = 0.3f;

    [Header("Stagger Sistemi (Sersemletme)")]
    [Tooltip("Stagger icin gereken hasar miktari")]
    public float staggerThreshold = 800f;

    [Tooltip("Stagger suresi (saniye)")]
    public float staggerDuration = 2f;

    [Tooltip("Stagger sirasinda alinacak ekstra hasar carpani")]
    public float staggerDamageMultiplier = 1.5f;

    [Tooltip("Stagger sonrasi bagirsiklik suresi")]
    public float staggerImmunityDuration = 5f;

    [Header("Hyper Armor (Super Zirh)")]
    [Tooltip("Saldiri sirasinda hyper armor aktif mi")]
    public bool useHyperArmor = true;

    [Tooltip("Hyper armor sirasinda hasar azaltma")]
    [Range(0.5f, 0.95f)]
    public float hyperArmorDamageReduction = 0.8f;

    [Header("Rage Modu")]
    [Tooltip("Rage modu aktif mi")]
    public bool useRageMode = true;

    [Tooltip("Rage modu icin can esigi (yuzde)")]
    [Range(0.1f, 0.5f)]
    public float rageHealthThreshold = 0.3f;

    [Tooltip("Rage modunda hasar artisi")]
    public float rageDamageMultiplier = 1.5f;

    [Tooltip("Rage modunda hiz artisi")]
    public float rageSpeedMultiplier = 1.3f;

    [Tooltip("Rage modunda zirh azalmasi")]
    public float rageArmorPenalty = 0.2f;

    [Header("Iyilesme Sistemi")]
    [Tooltip("Boss iyilesebilir mi")]
    public bool canHeal = true;

    [Tooltip("Iyilesme miktari (toplam canin yuzdesı)")]
    [Range(0.01f, 0.1f)]
    public float healPercent = 0.03f;

    [Tooltip("Iyilesme bekleme suresi (saniye)")]
    public float healCooldown = 15f;

    [Tooltip("Iyilesme icin gereken minimum mesafe")]
    public float healMinDistance = 8f;

    [Header("Faz Gecis Efektleri")]
    [Tooltip("Faz gecisinde kisa sure yenilmezlik")]
    public float phaseTransitionImmunity = 2f;

    [Tooltip("Faz gecisinde shockwave saldirisi")]
    public bool phaseTransitionShockwave = true;

    // Runtime boss state
    private float currentArmor;
    private float accumulatedStaggerDamage = 0f;
    private bool isStaggered = false;
    private float staggerTimer = 0f;
    private float staggerImmunityTimer = 0f;
    private bool hasHyperArmor = false;
    private bool isInRageMode = false;
    private float healCooldownTimer = 0f;
    private bool isPhaseTransitioning = false;
    private float phaseTransitionTimer = 0f;
    private int lastPhase = 1;

    #endregion

    #region Mesafe Esikleri

    [Header("Mesafe Esikleri")]
    [Tooltip("Uzak mesafe baslangici (mermi saldirisi)")]
    public float longRangeThreshold = 8f;

    [Tooltip("Orta mesafe baslangici (lazer saldirisi)")]
    public float midRangeThreshold = 4f;

    [Tooltip("Dash mesafe alt siniri")]
    public float dashMinRange = 3f;

    [Tooltip("Dash mesafe ust siniri")]
    public float dashMaxRange = 6f;

    // midRangeThreshold'dan kucuk mesafeler yakin mesafe (melee)

    #endregion

    #region Saldiri Ayarlari - Mermi

    [Header("Mermi Saldirisi (Uzak Mesafe)")]
    [Tooltip("Mermi prefab'i (null ise runtime olusturulur)")]
    public GameObject projectilePrefab;

    [Tooltip("Mermi cikis noktasi")]
    public Transform firePoint;

    [Tooltip("Mermi hizi")]
    public float projectileSpeed = 10f;

    [Tooltip("Mermi hasari")]
    public int projectileDamage = 1;

    [Tooltip("Mermi saldiri cooldown")]
    public float projectileCooldown = 2f;

    [Tooltip("Ayni anda atilan mermi sayisi")]
    public int projectileBurstCount = 3;

    [Tooltip("Burst mermileri arasi gecikme")]
    public float projectileBurstDelay = 0.1f;

    [Tooltip("Mermi yayilma acisi (spread)")]
    public float projectileSpreadAngle = 15f;

    [Tooltip("Mermi boyutu")]
    public float projectileSize = 0.4f;

    [Tooltip("Mermi kuyruk efekti")]
    public bool projectileHasTrail = true;

    [Tooltip("Mermi rengi")]
    public Color projectileColor = new Color(1f, 0.5f, 0f, 1f); // Turuncu

    #endregion

    #region Saldiri Ayarlari - Bomba

    [Header("Bomba Saldirisi")]
    [Tooltip("Bomba saldirisi aktif mi")]
    public bool canThrowBombs = true;

    [Tooltip("Bomba prefab'i (null ise runtime olusturulur)")]
    public GameObject bombPrefab;

    [Tooltip("Bomba hasari")]
    public int bombDamage = 3;

    [Tooltip("Bomba patlama yaricapi")]
    public float bombExplosionRadius = 3f;

    [Tooltip("Bomba firlatma hizi")]
    public float bombThrowSpeed = 8f;

    [Tooltip("Bomba firlatma acisi (derece)")]
    public float bombThrowAngle = 45f;

    [Tooltip("Bomba cooldown")]
    public float bombCooldown = 5f;

    [Tooltip("Bomba patlamadan onceki sure")]
    public float bombFuseTime = 1.5f;

    [Tooltip("Bomba boyutu")]
    public float bombSize = 0.6f;

    [Tooltip("Bomba rengi")]
    public Color bombColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Tooltip("Patlama rengi")]
    public Color explosionColor = new Color(1f, 0.6f, 0f, 1f);

    #endregion

    #region Saldiri Ayarlari - Roket

    [Header("Roket Saldirisi (Takipli)")]
    [Tooltip("Roket saldirisi aktif mi")]
    public bool canFireRockets = true;

    [Tooltip("Roket hasari")]
    public int rocketDamage = 2;

    [Tooltip("Roket hizi")]
    public float rocketSpeed = 6f;

    [Tooltip("Roket donus hizi (derece/saniye)")]
    public float rocketTurnSpeed = 180f;

    [Tooltip("Roket takip suresi")]
    public float rocketTrackingDuration = 3f;

    [Tooltip("Roket cooldown")]
    public float rocketCooldown = 6f;

    [Tooltip("Roket patlama yaricapi")]
    public float rocketExplosionRadius = 2f;

    #endregion

    #region Saldiri Ayarlari - Lazer

    [Header("Lazer Saldirisi (Orta Mesafe)")]
    [Tooltip("Lazer icin LineRenderer")]
    public LineRenderer laserLineRenderer;

    [Tooltip("Lazer hasari")]
    public int laserDamage = 2;

    [Tooltip("Lazer saldiri cooldown")]
    public float laserCooldown = 3f;

    [Tooltip("Lazer aktif kalma suresi")]
    public float laserDuration = 0.5f;

    [Tooltip("Lazer genisligi")]
    public float laserWidth = 0.15f;

    [Tooltip("Lazer rengi")]
    public Color laserColor = Color.red;

    [Tooltip("Lazer menzili")]
    public float laserRange = 12f;

    #endregion

    #region Saldiri Ayarlari - Neon Beam Sweep

    [Header("Neon Beam Sweep Saldirisi")]
    [Tooltip("Beam sweep aktif mi")]
    public bool canBeamSweep = true;

    [Tooltip("Beam sweep hasari")]
    public int beamSweepDamage = 2;

    [Tooltip("Beam sweep cooldown")]
    public float beamSweepCooldown = 8f;

    [Tooltip("Beam sweep yay acisi (derece)")]
    public float beamSweepArc = 120f;

    [Tooltip("Beam sweep suresi")]
    public float beamSweepDuration = 2f;

    [Tooltip("Beam sweep uyari suresi")]
    public float beamSweepWarning = 0.8f;

    [Tooltip("Beam sweep menzili")]
    public float beamSweepRange = 10f;

    [Tooltip("Beam sweep rengi")]
    public Color beamSweepColor = new Color(1f, 0f, 0.5f, 0.8f);

    #endregion

    #region Saldiri Ayarlari - Melee

    [Header("Melee Saldirisi (Yakin Mesafe)")]
    [Tooltip("Melee hasari")]
    public int meleeDamage = 1;

    [Tooltip("Melee saldiri cooldown")]
    public float meleeCooldown = 1.5f;

    [Tooltip("Melee saldiri menzili")]
    public float meleeRange = 1.5f;

    [Tooltip("Melee saldiri alani")]
    public Vector2 meleeAttackSize = new Vector2(1.5f, 1f);

    [Tooltip("Melee saldiri offset")]
    public Vector2 meleeAttackOffset = new Vector2(1f, 0f);

    #endregion

    #region Saldiri Ayarlari - Dash/Charge

    [Header("Dash/Charge Saldirisi (Yakin-Orta Mesafe)")]
    [Tooltip("Dash hizi")]
    public float dashSpeed = 18f;

    [Tooltip("Dash hasari")]
    public int dashDamage = 2;

    [Tooltip("Dash saldiri cooldown")]
    public float dashCooldown = 4f;

    [Tooltip("Dash suresi (saniye)")]
    public float dashDuration = 0.35f;

    [Tooltip("Dash knockback gucu")]
    public float dashKnockback = 8f;

    [Tooltip("Dash neon trail rengi")]
    public Color dashTrailColor = new Color(0f, 1f, 1f, 0.8f);

    [Tooltip("Dash trail genisligi")]
    public float dashTrailWidth = 0.3f;

    #endregion

    #region Shield Ayarlari

    [Header("Shield Sistemi")]
    [Tooltip("Shield aktif mi")]
    public bool shieldEnabled = true;

    [Tooltip("Shield maksimum vurus sayisi")]
    public int shieldMaxHits = 3;

    [Tooltip("Shield rejenerasyon gecikmesi (saniye, son vurustan sonra)")]
    public float shieldRegenDelay = 5f;

    [Tooltip("Shield rejenerasyon hizi (vurus/saniye)")]
    public float shieldRegenRate = 1f;

    [Tooltip("Shield rengi")]
    public Color shieldColor = new Color(0f, 0.8f, 1f, 0.35f);

    [Tooltip("Shield kirildigi andaki renk")]
    public Color shieldBreakColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    #endregion

    #region Rage Mode Ayarlari

    [Header("Rage Mode")]
    [Tooltip("Rage mod aktif mi")]
    public bool rageModeEnabled = true;

    [Tooltip("Rage cooldown carpani (0.6 = %40 azalma)")]
    public float rageCooldownMultiplier = 0.6f;

    [Tooltip("Rage flash hizi")]
    public float rageFlashSpeed = 6f;

    [Tooltip("Rage rengi 1")]
    public Color rageColor1 = new Color(1f, 0.1f, 0f, 1f);

    [Tooltip("Rage rengi 2")]
    public Color rageColor2 = new Color(1f, 0.5f, 0f, 1f);

    #endregion

    #region Item Drop Ayarlari

    [Header("Item Drop Sistemi")]
    [Tooltip("Esya dusurme aktif mi")]
    public bool canDropItems = true;

    [Tooltip("Silah dusurme aktif mi")]
    public bool canDropWeapons = true;

    [Tooltip("Esya dusurme sansi")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f;

    [Tooltip("Silah dusurme sansi")]
    [Range(0f, 1f)]
    public float weaponDropChance = 0.15f;

    #endregion

    #region Difficulty Scaling

    [Header("Difficulty Scaling")]
    [Tooltip("Zorluk olcekleme aktif mi")]
    public bool useDifficultyScaling = true;

    [Tooltip("Baslangic zorluk carpani")]
    public float baseDifficultyMultiplier = 1f;

    [Tooltip("Oyuncu kill basina zorluk artisi")]
    public float difficultyPerKill = 0.02f;

    [Tooltip("Maksimum zorluk carpani")]
    public float maxDifficultyMultiplier = 3f;

    // Runtime difficulty
    private float difficultyMultiplier = 1f;

    #endregion

    #region Neon Visual Effects Ayarlari

    [Header("Neon Visual Effects")]
    [Tooltip("Neon glow efekti aktif mi")]
    public bool enableGlowEffect = true;

    [Tooltip("Glow rengi (neon cyan)")]
    public Color glowColor = new Color(0f, 1f, 1f, 1f);

    [Tooltip("Glow nabiz hizi")]
    public float glowPulseSpeed = 3f;

    [Tooltip("Glow yogunlugu")]
    [Range(0f, 1f)]
    public float glowIntensity = 0.5f;

    [Tooltip("Saldiri sirasindaki flash rengi")]
    public Color attackFlashColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Saldiri flash suresi")]
    public float attackFlashDuration = 0.08f;

    #endregion

    #region Animator Parametreleri

    [Header("Animator Parametreleri")]
    public string walkingParam = "isWalking";
    public string attackTrigger = "Attack";
    public string lazerAttackTrigger = "LazerAttack";
    public string meleeAttackTrigger = "MeleeAttack";
    public string dashAttackTrigger = "DashAttack";
    public string dieTrigger = "Die";
    public string rageParam = "IsRage";

    #endregion

    #region Private Degiskenler

    // State
    private enum RobotState { Patrol, Chase, Attack }
    private RobotState currentState = RobotState.Patrol;

    // Patrol
    private Vector3 startPosition;
    private bool movingRight;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    // Hedef
    private Transform player;
    private float loseTargetTimer = 0f;
    private bool playerDetected = false;

    // Saldiri cooldown'lari
    private float projectileCooldownTimer = 0f;
    private float laserCooldownTimer = 0f;
    private float meleeCooldownTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float bombCooldownTimer = 0f;
    private float rocketCooldownTimer = 0f;

    // Saldiri durumu
    private bool isAttacking = false;
    private bool isLaserActive = false;
    private bool isDashing = false;
    private float beamSweepCooldownTimer = 0f;
    private bool isBeamSweeping = false;

    // Cached sprites for projectiles
    private static Sprite cachedBulletSprite;
    private static Sprite cachedBombSprite;
    private static Sprite cachedRocketSprite;

    // Dash trail
    private LineRenderer dashTrailRenderer;

    // Shield
    private int currentShieldHits;
    private float shieldRegenTimer = 0f;
    private float shieldRegenAccumulator = 0f; // FIX #3: Frame'ler arasi birikim
    private bool shieldBroken = false;
    private SpriteRenderer shieldVisual;
    private GameObject shieldObject;

    // Rage Mode
    private bool isRageActive = false;

    // Neon effects
    private float glowTimer = 0f;
    private Coroutine attackFlashCoroutine;

    // Outer glow
    private SpriteRenderer outerGlowSprite;
    private float outerGlowScale = 1.8f;
    private float outerGlowMaxAlpha = 0.4f;

    // Eye glow
    private SpriteRenderer leftEyeGlow;
    private SpriteRenderer rightEyeGlow;

    // Laser glow
    private LineRenderer laserGlowRenderer;

    // Ambient sparks
    private float sparkTimer = 0f;
    private float sparkInterval = 1.5f;

    // Attack flash color constants
    private static readonly Color flashProjectile = new Color(1f, 0.6f, 0f);      // turuncu
    private static readonly Color flashLaser = new Color(1f, 0.2f, 0.2f);          // kirmizi
    private static readonly Color flashMelee = new Color(1f, 1f, 0.3f);            // sari
    private static readonly Color flashBomb = new Color(1f, 0.4f, 0.1f);           // kirmizi-turuncu
    private static readonly Color flashDash = new Color(0f, 1f, 1f);               // cyan
    private static readonly Color flashGroundSlam = new Color(1f, 0f, 1f);         // magenta

    // Initialization
    private bool isInitialized = false;

    // FIX #4: Runtime projectile icin static cached sprite
    private static Sprite cachedProjectileSprite;

    // FIX #5/#6/#7/#8: Ground, edge, wall, stuck detection
    private bool isGrounded = false;
    private bool isEdgeAhead = false;
    private bool isWallAhead = false;
    private LayerMask effectiveGroundLayer; // Calisma zamaninda hesaplanan layer

    // Stuck detection
    private Vector3 lastStuckCheckPosition;
    private float stuckTimer = 0f;

    // FIX #11: Frame basina bir kez hesaplanan mesafe
    private float cachedDistanceToPlayer = Mathf.Infinity;

    // Jump System
    private float jumpCooldownTimer = 0f;
    private bool isJumping = false;

    // Teleport System
    private float teleportCooldownTimer = 0f;
    private float recentDamageTaken = 0f;
    private bool isTeleporting = false;

    // Ground Slam
    private float groundSlamCooldownTimer = 0f;
    private bool isGroundSlamming = false;

    // Drone System
    private float droneSpawnCooldownTimer = 0f;
    private List<GameObject> activeDrones = new List<GameObject>();

    // Predictive Targeting
    private Vector2 lastPlayerPosition;
    private Vector2 playerVelocity;
    private float velocityUpdateTimer = 0f;

    // Adaptive AI
    private List<PlayerActionRecord> playerActionHistory = new List<PlayerActionRecord>();
    private float playerDodgeLeft = 0f;
    private float playerDodgeRight = 0f;
    private float playerJumpFrequency = 0f;
    private float playerAggressiveness = 0f;

    // Phase System
    private int currentPhase = 1;
    private bool phaseTransitioning = false;

    // Combo Attack
    private int currentComboCount = 0;
    private bool isComboActive = false;
    private float comboTimer = 0f;

    // Counter Attack
    private float counterAttackCooldownTimer = 0f;
    private bool isCounterAttacking = false;

    // Stuck prevention advanced
    private int consecutiveStuckCount = 0;
    private Vector3 lastValidPosition;

    // Player action recording
    private struct PlayerActionRecord
    {
        public Vector2 position;
        public Vector2 velocity;
        public float timestamp;
        public bool wasJumping;
        public bool wasDashing;
    }

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        // FIX #10: autoSetupComponents'i devre disi birak, RobotEnemy kendi component'lerini yonetiyor
        autoSetupComponents = false;

        base.Start();

        // === BOSS STATS SETUP ===
        SetupBossStats();

        // Baslangic pozisyonunu kaydet
        startPosition = transform.position;
        movingRight = startMovingRight;

        // Oyuncuyu bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // FirePoint yoksa olustur
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            firePoint = fp.transform;
        }

        // LineRenderer yoksa olustur
        if (laserLineRenderer == null)
        {
            SetupLaserLineRenderer();
        }
        else
        {
            laserLineRenderer.enabled = false;
        }

        // Obstacle layer ayarla
        if (obstacleLayer == 0)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                obstacleLayer = 1 << groundLayer;
            }
        }

        // FIX #5: Ground check layer ayarla (groundCheckLayer atanmamissa obstacleLayer kullan)
        if (groundCheckLayer == 0)
        {
            // Oncelikle "Ground" layer'ini dene
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                effectiveGroundLayer = 1 << groundLayer;
            }
            else
            {
                // Ground layer yoksa obstacleLayer'i kullan
                effectiveGroundLayer = obstacleLayer;
            }
        }
        else
        {
            effectiveGroundLayer = groundCheckLayer;
        }

        // Dash trail LineRenderer kur
        SetupDashTrail();

        // Shield kur
        if (shieldEnabled)
        {
            SetupShield();
        }

        // Stuck detection baslangic pozisyonu
        lastStuckCheckPosition = transform.position;
        lastValidPosition = transform.position;

        // Baslangicta sprite yonunu ayarla (movingRight'a gore)
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = movingRight;
        }

        // Predictive targeting baslangic
        if (player != null)
        {
            lastPlayerPosition = player.position;
        }

        // Active drones listesini temizle
        activeDrones.Clear();

        isInitialized = true;

        // Enhanced visual setup
        if (enableGlowEffect)
        {
            SetupOuterGlow();
            SetupEyeGlows();
            SetupLaserGlowRenderer();
        }

        // Difficulty scaling
        if (useDifficultyScaling && GameManager.Instance != null)
        {
            int totalKills = GameManager.Instance.GetScore() / 100; // Approximate kills from score
            difficultyMultiplier = Mathf.Min(baseDifficultyMultiplier + totalKills * difficultyPerKill, maxDifficultyMultiplier);
            // Apply difficulty
            bossHealth *= difficultyMultiplier;
            if (enemyHealth != null) enemyHealth.SetMaxHealth(bossHealth, true);
            projectileCooldown /= difficultyMultiplier;
            laserCooldown /= difficultyMultiplier;
            meleeCooldown /= difficultyMultiplier;
            if (usePredictiveTargeting)
                predictionAccuracy = Mathf.Min(predictionAccuracy * difficultyMultiplier, 0.95f);
        }

        // Baslangicta yurume animasyonu
        SetWalkingAnimation(true);
    }

    /// <summary>
    /// Boss istatistiklerini ayarlar - yuksek can, zirh, UI
    /// </summary>
    private void SetupBossStats()
    {
        // EnemyHealth varsa boss canini ayarla
        if (enemyHealth != null)
        {
            enemyHealth.SetMaxHealth(bossHealth, true);
            Debug.Log($"[RobotEnemy] Boss health set to: {bossHealth}");
        }
        else
        {
            // EnemyHealth yoksa ekle
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
            enemyHealth.maxHealth = bossHealth;
            Debug.Log($"[RobotEnemy] EnemyHealth added with boss health: {bossHealth}");
        }

        // === ZIRH SISTEMI - DamageModifier ayarla ===
        // Bu sayede tum hasar EnemyHealth'e gelmeden once islenir
        currentArmor = baseArmorRating;
        if (enemyHealth != null)
        {
            enemyHealth.DamageModifier = CalculateBossDamageModifier;
            Debug.Log($"[RobotEnemy] Armor system active: {baseArmorRating * 100f}% damage reduction");
        }

        // Boss can bari UI
        if (showBossHealthBar)
        {
            SetupBossHealthBarUI();
        }

        // Bounce force'u azalt (boss daha agir)
        bounceForce = bounceForce * criticalHitResistance;
    }

    /// <summary>
    /// Zirh sistemine gore hasari hesaplar - EnemyHealth.DamageModifier icin
    /// </summary>
    private float CalculateBossDamageModifier(float incomingDamage)
    {
        // Faz gecisi sirasinda yenilmezlik
        if (isPhaseTransitioning)
        {
            ShowDamageBlockedEffect();
            return 0f;
        }

        // Shield kontrolu
        if (shieldEnabled && currentShieldHits > 0)
        {
            currentShieldHits--;
            if (shieldVisual != null)
            {
                StartCoroutine(ShieldHitFlash());
            }
            // Shield tum hasari emer
            if (currentShieldHits <= 0)
            {
                // Shield kirildi
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowNotification("SHIELD KIRILDI!", "Artik savunmasiz!", NotificationType.Warning);
                }
            }
            return 0f;
        }

        // === ZIRH HESAPLAMASI ===
        float effectiveArmor = currentArmor;

        // Faz bonuslari
        if (currentPhase >= 2) effectiveArmor += phase2ArmorBonus;
        if (currentPhase >= 3) effectiveArmor += phase3ArmorBonus;

        // Rage modu zirh penalti
        if (isInRageMode || isRageActive)
        {
            effectiveArmor -= rageArmorPenalty;
        }

        // Hyper armor (saldiri sirasinda)
        if (hasHyperArmor)
        {
            effectiveArmor = Mathf.Max(effectiveArmor, hyperArmorDamageReduction);
        }

        // Clamp armor
        effectiveArmor = Mathf.Clamp(effectiveArmor, 0f, 0.95f);

        // Hasar azaltma
        float finalDamage = incomingDamage * (1f - effectiveArmor);

        // === STAGGER SISTEMI ===
        if (!isStaggered && staggerImmunityTimer <= 0f)
        {
            accumulatedStaggerDamage += finalDamage;

            if (accumulatedStaggerDamage >= staggerThreshold)
            {
                TriggerStagger();
            }
        }

        // Stagger sirasinda ekstra hasar
        if (isStaggered)
        {
            finalDamage *= staggerDamageMultiplier;
        }

        // Rage mode kontrolu
        CheckRageMode();

        // Faz gecisi kontrolu
        CheckPhaseTransition();

        // Debug log
        Debug.Log($"[BOSS ARMOR] Incoming: {incomingDamage}, Armor: {effectiveArmor * 100f}%, Final: {finalDamage}");

        return finalDamage;
    }

    /// <summary>
    /// Boss can barini ekranda gosterir
    /// </summary>
    private void SetupBossHealthBarUI()
    {
        // BossHealthBar yoksa olustur
        if (BossHealthBar.Instance == null)
        {
            GameObject bossBarObj = new GameObject("BossHealthBar");
            bossBarObj.AddComponent<BossHealthBar>();
        }

        // Boss can barini goster
        if (BossHealthBar.Instance != null && enemyHealth != null)
        {
            BossHealthBar.Instance.ShowBoss(bossName, enemyHealth.currentHealth, enemyHealth.maxHealth);

            // EnemyHealth event'ine baglan
            enemyHealth.OnHealthChanged += OnBossHealthChanged;
            enemyHealth.OnDeath += OnBossDeath;
        }

        // Bildirim goster
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowBossAppear(bossName);
        }
    }

    /// <summary>
    /// Boss can degistiginde UI'i gunceller
    /// </summary>
    private void OnBossHealthChanged(float current, float max)
    {
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.UpdateHealth(current);
        }
    }

    /// <summary>
    /// Boss oldugunde UI'i gizler
    /// </summary>
    private void OnBossDeath()
    {
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.HideBoss();
        }
    }

    /// <summary>
    /// EnemyBase HandleDamaged'i override et - hasar aldiginda ekstra efektler
    /// NOT: Zirh hesaplamasi artik CalculateBossDamageModifier'da yapiliyor
    /// </summary>
    protected override void HandleDamaged(float damage)
    {
        if (isDead) return;

        // Hasar takibi (teleport icin)
        recentDamageTaken += damage;

        // === FAZ KONTROLU ===
        CheckPhaseTransition();

        // === RAGE MODU KONTROLU ===
        CheckRageMode();

        // Flash efekti
        if (spriteRenderer != null)
        {
            StartCoroutine(BossDamageFlash());
        }

        // Ses
        PlaySound(hurtSound);

        // Shield rejenerasyon timer'ini sifirla
        shieldRegenTimer = 0f;
        shieldRegenAccumulator = 0f;

        // Combo sifirla (hasar alinca combo bozulur)
        if (isComboActive)
        {
            ResetCombo();
        }

        Debug.Log($"[BOSS] Damage taken: {damage} (Armor: {currentArmor:P0}, Stagger: {isStaggered}, Rage: {isInRageMode})");
    }

    /// <summary>
    /// Boss hasar hesaplamasi - zirh, hyper armor, faz bonuslari
    /// </summary>
    private float CalculateBossDamage(float rawDamage)
    {
        // Temel zirh
        currentArmor = baseArmorRating;

        // Faz bonuslari
        if (currentPhase >= 2) currentArmor += phase2ArmorBonus;
        if (currentPhase >= 3) currentArmor += phase3ArmorBonus;

        // Rage modu zirh penalty
        if (isInRageMode) currentArmor -= rageArmorPenalty;

        // Hyper armor (saldiri sirasinda)
        if (hasHyperArmor && useHyperArmor)
        {
            currentArmor = Mathf.Max(currentArmor, hyperArmorDamageReduction);
        }

        // Zirhi sinirla
        currentArmor = Mathf.Clamp(currentArmor, 0f, 0.95f);

        // Hasari hesapla
        float finalDamage = rawDamage * (1f - currentArmor);

        // Minimum hasar (boss asla 0 hasar almaz)
        finalDamage = Mathf.Max(finalDamage, 1f);

        return finalDamage;
    }

    /// <summary>
    /// Stagger (sersemletme) tetikle
    /// </summary>
    private void TriggerStagger()
    {
        if (isStaggered) return;

        isStaggered = true;
        staggerTimer = staggerDuration;
        accumulatedStaggerDamage = 0f;

        // Saldiriyi iptal et
        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
        }

        // Stagger animasyonu/efekti
        if (animator != null)
        {
            animator.SetTrigger("Stagger");
        }

        // Efekt
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayDamageEffect(transform.position);
        }

        // Bildirim
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification("STAGGER!", "Boss sersemletildi!", NotificationType.Warning);
        }

        // Ekran sarsmasi
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnCombo(5);
        }

        Debug.Log("[BOSS] STAGGERED!");
    }

    /// <summary>
    /// Rage modu kontrolu
    /// </summary>
    private void CheckRageMode()
    {
        if (!useRageMode || isInRageMode) return;

        if (enemyHealth != null)
        {
            float healthPercent = enemyHealth.GetHealthPercent();
            if (healthPercent <= rageHealthThreshold)
            {
                ActivateRageMode();
            }
        }
    }

    /// <summary>
    /// Rage modunu aktifle
    /// </summary>
    private void ActivateRageMode()
    {
        if (isInRageMode) return;

        isInRageMode = true;

        // Gorseli degistir
        if (spriteRenderer != null)
        {
            // Kirmizi ton
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f);
        }

        // Hiz artisi
        patrolSpeed *= rageSpeedMultiplier;

        // Efekt
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayExplosion(transform.position);
        }

        // Bildirim
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification("RAGE MODE!", "Boss cildirdi!", NotificationType.Warning);
        }

        // Ekran sarsmasi
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnCombo(8);
        }

        Debug.Log("[BOSS] RAGE MODE ACTIVATED!");
    }

    /// <summary>
    /// Faz gecisi kontrolu
    /// </summary>
    private void CheckPhaseTransition()
    {
        if (!usePhaseSystem || enemyHealth == null) return;

        int newPhase = currentPhase;

        float healthPercent = enemyHealth.GetHealthPercent();

        if (healthPercent <= phase3HealthThreshold && currentPhase < 3)
        {
            newPhase = 3;
        }
        else if (healthPercent <= phase2HealthThreshold && currentPhase < 2)
        {
            newPhase = 2;
        }

        if (newPhase != lastPhase)
        {
            StartCoroutine(PhaseTransition(newPhase));
            lastPhase = newPhase;
        }
    }

    /// <summary>
    /// Faz gecis animasyonu
    /// </summary>
    private System.Collections.IEnumerator PhaseTransition(int newPhase)
    {
        isPhaseTransitioning = true;
        phaseTransitionTimer = phaseTransitionImmunity;

        // Saldiriyi durdur
        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
        }

        // Shockwave saldirisi
        if (phaseTransitionShockwave)
        {
            // Buyuk patlama efekti
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayExplosion(transform.position);
            }

            // Ekran sarsmasi
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.ShakeOnCombo(10);
            }

            // Yakin oyuncuya hasar
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist < 5f)
                {
                    PlayerController pc = player.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.TakeDamage();
                    }
                }
            }
        }

        // Bildirim
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"FAZ {newPhase}!", "Boss gucleniyor!", NotificationType.Warning);
        }

        // Kisa bekleme
        yield return new WaitForSeconds(phaseTransitionImmunity);

        currentPhase = newPhase;
        isPhaseTransitioning = false;

        Debug.Log($"[BOSS] Phase transition to {newPhase}");
    }

    /// <summary>
    /// Boss iyilesme girişimi
    /// </summary>
    private void TryBossHeal()
    {
        if (!canHeal || healCooldownTimer > 0f || enemyHealth == null) return;

        // Oyuncudan uzakta miyiz?
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < healMinDistance) return;
        }

        // Can dolu mu?
        if (enemyHealth.GetHealthPercent() >= 0.95f) return;

        // Iyiles
        float healAmount = enemyHealth.maxHealth * healPercent;
        enemyHealth.currentHealth = Mathf.Min(enemyHealth.currentHealth + healAmount, enemyHealth.maxHealth);

        // UI guncelle
        OnBossHealthChanged(enemyHealth.currentHealth, enemyHealth.maxHealth);

        // Efekt
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayHealEffect(transform.position);
        }

        // Floating text
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowHeal(transform.position + Vector3.up, Mathf.RoundToInt(healAmount));
        }

        healCooldownTimer = healCooldown;

        Debug.Log($"[BOSS] Healed for {healAmount}");
    }

    /// <summary>
    /// Hasar engellendi efekti
    /// </summary>
    private void ShowDamageBlockedEffect()
    {
        // "IMMUNE" floating text
        if (FloatingTextManager.Instance != null)
        {
            // FloatingTextManager'da boyle bir metod yoksa basit efekt
        }

        // Spark efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayDamageEffect(transform.position);
        }
    }

    /// <summary>
    /// Boss hasar flash efekti
    /// </summary>
    private System.Collections.IEnumerator BossDamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = isInRageMode ? new Color(1f, 0.5f, 0.5f) : spriteRenderer.color;

        // Beyaz flash
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.05f);

        // Kirmizi flash
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.05f);

        // Normale don
        spriteRenderer.color = originalColor;
    }

    /// <summary>
    /// Hyper armor'i aktifle (saldiri baslangicinda cagrilir)
    /// </summary>
    protected void EnableHyperArmor()
    {
        hasHyperArmor = true;
    }

    /// <summary>
    /// Hyper armor'i deaktifle (saldiri bitisinde cagrilir)
    /// </summary>
    protected void DisableHyperArmor()
    {
        hasHyperArmor = false;
    }

    /// <summary>
    /// Boss sistemleri timer guncellemesi
    /// </summary>
    private void UpdateBossTimers()
    {
        // Stagger timer
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                staggerImmunityTimer = staggerImmunityDuration;
                accumulatedStaggerDamage = 0f;

                // Stagger sonrasi durumu duzelt - oyuncu gorunuyorsa chase'e gec
                if (playerDetected && currentState != RobotState.Chase)
                {
                    ChangeState(RobotState.Chase);
                }
                Debug.Log("[BOSS] Stagger ended, immunity started");
            }
        }

        // Stagger immunity timer
        if (staggerImmunityTimer > 0f)
        {
            staggerImmunityTimer -= Time.deltaTime;
        }

        // Phase transition timer
        if (phaseTransitionTimer > 0f)
        {
            phaseTransitionTimer -= Time.deltaTime;
        }

        // Heal cooldown
        if (healCooldownTimer > 0f)
        {
            healCooldownTimer -= Time.deltaTime;
        }
        else if (canHeal && !isAttacking && !isStaggered)
        {
            // Iyilesme girişimi
            TryBossHeal();
        }

        // Stagger sirasinda saldiri yapma
        if (isStaggered && isAttacking)
        {
            isAttacking = false;
        }
    }

    void Update()
    {
        if (isDead || !isInitialized) return;

        // === BOSS TIMER GUNCELLEMELERI ===
        UpdateBossTimers();

        // FIX #11: Oyuncuya mesafeyi frame basina bir kez hesapla
        if (player != null)
        {
            cachedDistanceToPlayer = Vector2.Distance(transform.position, player.position);
        }
        else
        {
            cachedDistanceToPlayer = Mathf.Infinity;
        }

        // Cooldown timer'lari guncelle
        UpdateCooldowns();
        UpdateAdvancedCooldowns();

        // Shield rejenerasyon
        if (shieldEnabled)
        {
            UpdateShieldRegen();
        }

        // Rage mode kontrolu
        if (rageModeEnabled)
        {
            UpdateRageMode();
        }

        // Phase sistem kontrolu
        if (usePhaseSystem)
        {
            UpdatePhaseSystem();
        }

        // Neon glow efekti
        if (enableGlowEffect && !isRageActive)
        {
            UpdateGlowEffect();
        }

        // FIX #5/#6/#8: Ground, edge ve wall kontrolu
        UpdateEnvironmentChecks();

        // FIX #7: Stuck detection (gelismis)
        UpdateAdvancedStuckDetection();

        // Predictive targeting icin oyuncu hizini hesapla
        if (usePredictiveTargeting && player != null)
        {
            UpdatePlayerVelocityTracking();
        }

        // Adaptive AI icin oyuncu davranislarini kaydet
        if (useAdaptiveAI && player != null)
        {
            UpdatePlayerBehaviorTracking();
        }

        // Drone yonetimi
        if (canSpawnDrones)
        {
            ManageDrones();
        }

        // Combo timer
        if (isComboActive)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }

        // Recent damage decay
        if (recentDamageTaken > 0f)
        {
            recentDamageTaken -= Time.deltaTime * 0.5f;
        }

        // Oyuncu algilamasini kontrol et
        CheckPlayerDetection();

        // State'e gore davran
        switch (currentState)
        {
            case RobotState.Patrol:
                UpdatePatrolState();
                break;

            case RobotState.Chase:
                UpdateChaseState();
                break;

            case RobotState.Attack:
                UpdateAttackState();
                break;
        }

        // FIX #2: spriteRenderer.flipX ile yon degistir (dash sirasinda degistirme)
        if (!isDashing && !isTeleporting && !isGroundSlamming)
        {
            UpdateFacingDirection();
        }
    }

    /// <summary>
    /// Yeni sistemler icin cooldown timer'larini gunceller.
    /// </summary>
    private void UpdateAdvancedCooldowns()
    {
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;

        if (teleportCooldownTimer > 0f)
            teleportCooldownTimer -= Time.deltaTime;

        if (groundSlamCooldownTimer > 0f)
            groundSlamCooldownTimer -= Time.deltaTime;

        if (droneSpawnCooldownTimer > 0f)
            droneSpawnCooldownTimer -= Time.deltaTime;

        if (counterAttackCooldownTimer > 0f)
            counterAttackCooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (isDead || !isInitialized || isAttacking) return;

        // Stagger sirasinda hareket etme
        if (isStaggered)
        {
            StopMovement();
            return;
        }

        // Dash sirasinda fizik hareketi coroutine tarafindan kontrol edilir
        if (isDashing) return;

        // FIX #8: Havadayken hareket komutlari verme
        if (!isGrounded) return;

        switch (currentState)
        {
            case RobotState.Patrol:
                PatrolMovement();
                break;

            case RobotState.Chase:
                ChaseMovement();
                break;

            case RobotState.Attack:
                StopMovement();
                break;
        }
    }

    #endregion

    #region Environment Checks (Ground, Edge, Wall)

    /// <summary>
    /// Zemin, kenar ve duvar kontrollerini her frame gunceller.
    /// Asagi raycast: yerde mi kontrolu.
    /// Ileri+asagi raycast: kenar/ucurum kontrolu.
    /// Ileri raycast: duvar kontrolu.
    /// </summary>
    private void UpdateEnvironmentChecks()
    {
        if (effectiveGroundLayer == 0) return;

        float facingDir = GetFacingDirection();
        Vector2 origin = (Vector2)transform.position + new Vector2(0f, groundCheckYOffset);

        // --- Grounded Check (asagi raycast) ---
        RaycastHit2D groundHit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, effectiveGroundLayer);
        isGrounded = groundHit.collider != null;

        // --- Edge Check (ileri + asagi raycast) ---
        // Oncelikle ileri bir adim at, sonra asagi bak. Zemin yoksa kenar var demek.
        Vector2 edgeCheckOrigin = origin + new Vector2(facingDir * edgeCheckForwardDistance, 0f);
        RaycastHit2D edgeHit = Physics2D.Raycast(edgeCheckOrigin, Vector2.down, edgeCheckDownDistance, effectiveGroundLayer);
        isEdgeAhead = (edgeHit.collider == null) && isGrounded; // Sadece yerdeyken kenar tespiti anlamli

        // --- Wall Check (ileri raycast) ---
        RaycastHit2D wallHit = Physics2D.Raycast(origin, new Vector2(facingDir, 0f), wallCheckDistance, effectiveGroundLayer);
        isWallAhead = wallHit.collider != null;
    }

    #endregion

    #region Stuck Detection


    /// <summary>
    /// Takilma durumunda cozum uygular: yon degistirir ve hafif bir durtme verir.
    /// Gelismis versiyon: Art arda takilmalarda teleport kullanir.
    /// </summary>
    private void HandleStuck()
    {
        consecutiveStuckCount++;

        // Art arda cok fazla takildiysa teleport et
        if (canTeleport && consecutiveStuckCount >= stuckTeleportThreshold)
        {
            Vector3 safePos = FindSafePosition();
            if (safePos != Vector3.zero)
            {
                StartCoroutine(EmergencyTeleport(safePos));
                consecutiveStuckCount = 0;
                return;
            }
        }

        // Patrol'de yon degistir ve zipla
        if (currentState == RobotState.Patrol)
        {
            TurnAround();

            // Ziplama ile kurtul
            if (canJump && isGrounded && rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, stuckJumpForce);
            }
        }
        // Chase'de daha agresif kurtulma
        else if (currentState == RobotState.Chase)
        {
            movingRight = !movingRight;

            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                float nudgeDir = movingRight ? 1f : -1f;

                // Ziplama ile kurtul
                if (canJump && isGrounded)
                {
                    rb.linearVelocity = new Vector2(nudgeDir * patrolSpeed * 1.5f, stuckJumpForce);
                }
                else
                {
                    rb.linearVelocity = new Vector2(nudgeDir * patrolSpeed, 2f);
                }
            }
        }

        // Gecerli pozisyon olarak kaydet (cok takilmadiysa)
        if (consecutiveStuckCount < 2)
        {
            lastValidPosition = transform.position;
        }
    }

    /// <summary>
    /// Gelismis stuck detection - coklu kontrol ve kurtarma mekanizmalari.
    /// </summary>
    private void UpdateAdvancedStuckDetection()
    {
        // Sadece hareket etmesi gereken durumlarda kontrol et
        bool shouldBeMoving = !isWaiting && !isAttacking && !isDashing && !isTeleporting && !isGroundSlamming &&
                              isGrounded && (currentState == RobotState.Patrol || currentState == RobotState.Chase);

        if (!shouldBeMoving)
        {
            stuckTimer = 0f;
            lastStuckCheckPosition = transform.position;

            // Basarili hareket - stuck sayacini sifirla
            float distFromLast = Vector2.Distance(transform.position, lastStuckCheckPosition);
            if (distFromLast > stuckMinDistance * 2f)
            {
                consecutiveStuckCount = 0;
                lastValidPosition = transform.position;
            }
            return;
        }

        stuckTimer += Time.deltaTime;

        if (stuckTimer >= stuckDetectionTime)
        {
            float distanceMoved = Vector2.Distance(transform.position, lastStuckCheckPosition);

            if (distanceMoved < stuckMinDistance)
            {
                // Robot takili!
                HandleStuck();
            }
            else
            {
                // Basarili hareket
                consecutiveStuckCount = 0;
                lastValidPosition = transform.position;
            }

            stuckTimer = 0f;
            lastStuckCheckPosition = transform.position;
        }
    }

    /// <summary>
    /// Acil durum teleportu - takildiginda guvenli bir noktaya isinlanir.
    /// </summary>
    private IEnumerator EmergencyTeleport(Vector3 targetPos)
    {
        isTeleporting = true;

        // Teleport efekti
        if (spriteRenderer != null)
        {
            StartCoroutine(TeleportFlashEffect());
        }

        yield return new WaitForSeconds(0.1f);

        transform.position = targetPos;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(0.1f);

        isTeleporting = false;
    }

    /// <summary>
    /// Guvenli bir pozisyon bulur (zemin uzerinde, engel olmayan).
    /// </summary>
    private Vector3 FindSafePosition()
    {
        // Once son gecerli pozisyonu dene
        if (IsPositionSafe(lastValidPosition))
        {
            return lastValidPosition;
        }

        // Etrafta guvenli nokta ara
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 testPos = transform.position + new Vector3(Mathf.Cos(angle) * 3f, 1f, 0f);

            if (IsPositionSafe(testPos))
            {
                return testPos;
            }
        }

        // Baslangic pozisyonuna don
        if (IsPositionSafe(startPosition))
        {
            return startPosition;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Belirtilen pozisyonun guvenli olup olmadigini kontrol eder.
    /// </summary>
    private bool IsPositionSafe(Vector3 pos)
    {
        // Zemin kontrolu
        RaycastHit2D groundHit = Physics2D.Raycast(pos, Vector2.down, 3f, effectiveGroundLayer);
        if (groundHit.collider == null) return false;

        // Engel kontrolu
        Collider2D obstacle = Physics2D.OverlapCircle(pos, 0.5f, effectiveGroundLayer);
        if (obstacle != null) return false;

        return true;
    }

    #endregion

    #region Jump System

    /// <summary>
    /// Oyuncuya ulasmak icin ziplama gerekip gerekmedigini kontrol eder.
    /// </summary>
    private bool ShouldJumpToPlayer()
    {
        if (!canJump || !isGrounded || jumpCooldownTimer > 0f || player == null) return false;

        // Oyuncu yukarda mi?
        float heightDiff = player.position.y - transform.position.y;
        if (heightDiff > 1f && heightDiff < platformDetectionRange)
        {
            // Yukari platform var mi?
            RaycastHit2D platformHit = Physics2D.Raycast(
                transform.position + Vector3.up * 0.5f,
                Vector2.up,
                platformDetectionRange,
                effectiveGroundLayer
            );

            if (platformHit.collider != null)
            {
                return true;
            }
        }

        // Onde engel varsa zipla
        if (isWallAhead && !isEdgeAhead)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ziplama hareketi gerceklestirir.
    /// </summary>
    private void PerformJump()
    {
        if (!canJump || !isGrounded || rb == null) return;

        isJumping = true;
        jumpCooldownTimer = jumpCooldown;

        float effectiveJumpForce = isRageActive ? jumpForce * rageSpeedMultiplier : jumpForce;

        // Oyuncuya dogru zipla
        float directionX = 0f;
        if (player != null)
        {
            directionX = player.position.x > transform.position.x ? 1f : -1f;
        }

        rb.linearVelocity = new Vector2(directionX * patrolSpeed * airControl, effectiveJumpForce);

        // Ziplama sesi
        PlaySound(attackSound);

        StartCoroutine(ResetJumpState());
    }

    private IEnumerator ResetJumpState()
    {
        yield return new WaitForSeconds(0.5f);
        isJumping = false;
    }

    #endregion

    #region Teleport System

    /// <summary>
    /// Stratejik teleport baslatir - oyuncunun arkasina veya yanina isinlanir.
    /// </summary>
    private void StartTeleport()
    {
        if (!canTeleport || isTeleporting || player == null) return;

        Vector3 targetPos = CalculateTeleportPosition();
        if (targetPos != Vector3.zero)
        {
            StartCoroutine(ExecuteTeleport(targetPos));
        }
    }

    /// <summary>
    /// Teleport icin hedef pozisyonu hesaplar.
    /// Oyuncunun arkasina veya yanina isinlanmaya calisir.
    /// </summary>
    private Vector3 CalculateTeleportPosition()
    {
        if (player == null) return Vector3.zero;

        // Oyuncunun arkasina teleport olmaya calis
        Vector2 playerFacing = playerVelocity.normalized;
        if (playerFacing.magnitude < 0.1f)
        {
            playerFacing = Vector2.right; // Default
        }

        // Farkli pozisyonlari dene
        Vector3[] candidates = new Vector3[]
        {
            player.position - (Vector3)(playerFacing * teleportRange), // Arkasina
            player.position + Vector3.right * teleportRange, // Sagina
            player.position + Vector3.left * teleportRange, // Soluna
            player.position + (Vector3)(playerFacing * teleportRange), // Onune
        };

        foreach (Vector3 pos in candidates)
        {
            Vector3 adjustedPos = pos;
            adjustedPos.y += 0.5f; // Biraz yukari

            if (IsPositionSafe(adjustedPos))
            {
                return adjustedPos;
            }
        }

        return Vector3.zero;
    }

    private IEnumerator ExecuteTeleport(Vector3 targetPos)
    {
        isTeleporting = true;
        isAttacking = true;
        ChangeState(RobotState.Attack);

        // Kaybolma efekti
        StartCoroutine(TeleportFlashEffect());

        // Kisa bekleme
        yield return new WaitForSeconds(0.15f);

        if (isDead) yield break;

        // Teleport
        transform.position = targetPos;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Belirme efekti
        StartCoroutine(TeleportAppearEffect());

        teleportCooldownTimer = teleportCooldown;
        recentDamageTaken = 0f;

        yield return new WaitForSeconds(0.2f);

        isTeleporting = false;
        isAttacking = false;
    }

    private IEnumerator TeleportFlashEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalCol = spriteRenderer.color;

        // Parlama
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = teleportColor;
            yield return new WaitForSeconds(0.03f);
            spriteRenderer.color = originalCol;
            yield return new WaitForSeconds(0.03f);
        }

        spriteRenderer.color = originalCol;
    }

    private IEnumerator TeleportAppearEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalCol = spriteRenderer.color;

        // Belirme
        spriteRenderer.color = new Color(teleportColor.r, teleportColor.g, teleportColor.b, 0f);

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            spriteRenderer.color = Color.Lerp(teleportColor, originalCol, elapsed / duration);
            yield return null;
        }

        spriteRenderer.color = originalCol;
    }

    #endregion

    #region Ground Slam Attack

    /// <summary>
    /// Ground slam saldirisini baslatir - havaya ziplar ve yere carpar.
    /// </summary>
    private void StartGroundSlam()
    {
        if (!canGroundSlam || isGroundSlamming) return;

        isGroundSlamming = true;
        isAttacking = true;
        ChangeState(RobotState.Attack);

        StartCoroutine(ExecuteGroundSlam());
    }

    private IEnumerator ExecuteGroundSlam()
    {
        if (rb == null)
        {
            isGroundSlamming = false;
            isAttacking = false;
            yield break;
        }

        // Yukari zipla
        rb.linearVelocity = new Vector2(0f, groundSlamHeight * 2f);

        // Zirveye ulasmasini bekle
        yield return new WaitForSeconds(0.4f);

        if (isDead) yield break;

        // Havada kisa duraklama
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(0.2f);

        if (isDead) yield break;

        // Hizla asagi in
        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(0f, -groundSlamHeight * 3f);

        // Yere carpmayi bekle
        float timeout = 2f;
        while (!isGrounded && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (isDead) yield break;

        // Yere carpma efekti ve hasar
        GroundSlamImpact();

        groundSlamCooldownTimer = groundSlamCooldown;

        yield return new WaitForSeconds(0.5f);

        isGroundSlamming = false;
        isAttacking = false;
    }

    /// <summary>
    /// Ground slam carpma efektini uygular - alan hasari ve knockback.
    /// </summary>
    private void GroundSlamImpact()
    {
        // Buyuk ekran sarsintisi
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnCombo(8);
        }

        // Shockwave patlama efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayRobotBombExplosion(transform.position, groundSlamRadius, flashGroundSlam);
            // Iki yanda toz
            ParticleManager.Instance.PlayLandDust(transform.position + Vector3.left * 1f);
            ParticleManager.Instance.PlayLandDust(transform.position + Vector3.right * 1f);
        }

        // Alan hasari
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, groundSlamRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // Hasar
                    for (int i = 0; i < groundSlamDamage; i++)
                    {
                        playerController.TakeDamage();
                    }

                    // Knockback
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                        knockbackDir.y = 0.5f;
                        playerRb.AddForce(knockbackDir.normalized * groundSlamKnockback, ForceMode2D.Impulse);
                    }
                }
            }
        }

        // Ses efekti
        PlaySound(attackSound);
    }

    #endregion

    #region Drone System

    /// <summary>
    /// Yardimci drone spawn eder.
    /// </summary>
    private void SpawnDrone()
    {
        if (!canSpawnDrones || activeDrones.Count >= maxDrones) return;

        Vector3 spawnPos = transform.position + Vector3.up * 2f + Vector3.right * Random.Range(-1f, 1f);

        GameObject drone;

        if (dronePrefab != null)
        {
            drone = Instantiate(dronePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            drone = CreateRuntimeDrone(spawnPos);
        }

        if (drone != null)
        {
            activeDrones.Add(drone);
            droneSpawnCooldownTimer = droneSpawnCooldown;

            // Spawn efekti
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayEnemyDeath(spawnPos);
            }
        }
    }

    /// <summary>
    /// Runtime'da basit bir drone olusturur.
    /// </summary>
    private GameObject CreateRuntimeDrone(Vector3 position)
    {
        GameObject drone = new GameObject("RobotDrone");
        drone.transform.position = position;
        drone.tag = "Enemy";
        drone.transform.localScale = Vector3.one * 0.5f;

        // Sprite
        SpriteRenderer sr = drone.AddComponent<SpriteRenderer>();
        sr.color = glowColor;
        sr.sortingOrder = 5;

        // Drone sprite olustur (altigen/hexagon sekli)
        sr.sprite = CreateDroneSprite();

        // Glow efekti
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(drone.transform);
        glow.transform.localPosition = Vector3.zero;
        SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = sr.sprite;
        glowSr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f);
        glowSr.sortingOrder = 4;
        glow.transform.localScale = Vector3.one * 1.5f;

        // Trail efekti
        TrailRenderer trail = drone.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f);
        trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        trail.sortingOrder = 3;

        // Collider
        CircleCollider2D col = drone.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        // Rigidbody
        Rigidbody2D droneRb = drone.AddComponent<Rigidbody2D>();
        droneRb.gravityScale = 0f;

        // Drone AI component
        RobotDroneAI droneAI = drone.AddComponent<RobotDroneAI>();
        droneAI.Initialize(player, this);

        // EnemyHealth ekle
        EnemyHealth health = drone.AddComponent<EnemyHealth>();
        health.maxHealth = 1;

        return drone;
    }

    /// <summary>
    /// Aktif drone'lari yonetir - olen drone'lari listeden cikarir.
    /// </summary>
    private void ManageDrones()
    {
        activeDrones.RemoveAll(d => d == null);
    }

    #endregion

    #region Predictive Targeting

    /// <summary>
    /// Oyuncunun hizini takip eder.
    /// </summary>
    private void UpdatePlayerVelocityTracking()
    {
        if (player == null) return;

        velocityUpdateTimer += Time.deltaTime;

        if (velocityUpdateTimer >= 0.1f) // Her 0.1 saniyede guncelle
        {
            Vector2 currentPos = player.position;
            playerVelocity = (currentPos - lastPlayerPosition) / velocityUpdateTimer;
            lastPlayerPosition = currentPos;
            velocityUpdateTimer = 0f;
        }
    }

    /// <summary>
    /// Oyuncunun gelecekteki pozisyonunu tahmin eder.
    /// </summary>
    private Vector2 PredictPlayerPosition(float time)
    {
        if (player == null) return Vector2.zero;

        if (!usePredictiveTargeting)
        {
            return player.position;
        }

        // Temel tahmin
        Vector2 prediction = (Vector2)player.position + playerVelocity * time * predictionAccuracy;

        // Adaptive AI bonus - oyuncunun kacinma egilimini hesaba kat
        if (useAdaptiveAI)
        {
            float dodgeBias = playerDodgeRight - playerDodgeLeft;
            prediction.x += dodgeBias * 0.5f;

            if (playerJumpFrequency > 0.3f)
            {
                prediction.y += playerJumpFrequency * 0.5f;
            }
        }

        return prediction;
    }

    #endregion

    #region Adaptive AI

    /// <summary>
    /// Oyuncunun davranislarini kaydeder ve analiz eder.
    /// </summary>
    private void UpdatePlayerBehaviorTracking()
    {
        if (player == null) return;

        // Yeni kayit ekle
        PlayerController pc = player.GetComponent<PlayerController>();
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            PlayerActionRecord record = new PlayerActionRecord
            {
                position = player.position,
                velocity = playerRb.linearVelocity,
                timestamp = Time.time,
                wasJumping = !IsPlayerGrounded(player),
                wasDashing = pc != null && pc.IsDashing()
            };

            playerActionHistory.Add(record);
        }

        // Eski kayitlari temizle
        playerActionHistory.RemoveAll(r => Time.time - r.timestamp > memoryDuration);

        // Davranis analizi
        if (playerActionHistory.Count >= minPatternSamples)
        {
            AnalyzePlayerBehavior();
        }
    }

    /// <summary>
    /// Oyuncunun yerde olup olmadigini kontrol eder.
    /// </summary>
    private bool IsPlayerGrounded(Transform playerTransform)
    {
        RaycastHit2D hit = Physics2D.Raycast(playerTransform.position, Vector2.down, 0.2f, effectiveGroundLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// Oyuncunun davranislarini analiz eder.
    /// </summary>
    private void AnalyzePlayerBehavior()
    {
        int totalRecords = playerActionHistory.Count;
        if (totalRecords < minPatternSamples) return;

        int jumpCount = 0;
        int dodgeLeftCount = 0;
        int dodgeRightCount = 0;
        int aggressiveCount = 0;

        Vector2 myPos = transform.position;

        foreach (var record in playerActionHistory)
        {
            if (record.wasJumping) jumpCount++;

            // Kacinma yonu analizi
            Vector2 toPlayer = record.position - myPos;
            if (record.velocity.x < -1f && toPlayer.x > 0f) dodgeLeftCount++;
            if (record.velocity.x > 1f && toPlayer.x < 0f) dodgeRightCount++;

            // Agresiflik analizi - bize dogru hareket
            if (Vector2.Dot(record.velocity.normalized, -toPlayer.normalized) > 0.5f)
            {
                aggressiveCount++;
            }
        }

        // Ogrenme hizina gore guncelle
        playerJumpFrequency = Mathf.Lerp(playerJumpFrequency, (float)jumpCount / totalRecords, learningRate);
        playerDodgeLeft = Mathf.Lerp(playerDodgeLeft, (float)dodgeLeftCount / totalRecords, learningRate);
        playerDodgeRight = Mathf.Lerp(playerDodgeRight, (float)dodgeRightCount / totalRecords, learningRate);
        playerAggressiveness = Mathf.Lerp(playerAggressiveness, (float)aggressiveCount / totalRecords, learningRate);
    }

    #endregion

    #region Phase System

    /// <summary>
    /// Can seviyesine gore phase'i gunceller.
    /// </summary>
    private void UpdatePhaseSystem()
    {
        if (enemyHealth == null || phaseTransitioning) return;

        float healthPercent = enemyHealth.GetHealthPercent();
        int targetPhase = 1;

        if (healthPercent <= phase3HealthThreshold)
        {
            targetPhase = 3;
        }
        else if (healthPercent <= phase2HealthThreshold)
        {
            targetPhase = 2;
        }

        if (targetPhase > currentPhase)
        {
            StartCoroutine(PhaseTransitionVisual(targetPhase));
        }
    }

    private IEnumerator PhaseTransitionVisual(int newPhase)
    {
        phaseTransitioning = true;

        Color phaseColor = newPhase == 3 ? rageColor1 : glowColor;

        // 8 kez hizli renk flash (beyaz/faz rengi)
        if (spriteRenderer != null)
        {
            for (int i = 0; i < 8; i++)
            {
                spriteRenderer.color = Color.white;
                if (outerGlowSprite != null) outerGlowSprite.color = new Color(1f, 1f, 1f, 0.8f);
                yield return new WaitForSeconds(0.06f);
                spriteRenderer.color = phaseColor;
                if (outerGlowSprite != null) outerGlowSprite.color = new Color(phaseColor.r, phaseColor.g, phaseColor.b, outerGlowMaxAlpha);
                yield return new WaitForSeconds(0.06f);
            }
            spriteRenderer.color = originalColor;
        }

        // Shockwave + NeonGlow parcaciklari
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayRobotBombExplosion(transform.position, 3f, phaseColor);
            ParticleManager.Instance.PlayNeonGlow(transform.position, phaseColor, 1f);
        }

        // 3x elektrik kivilcimi
        for (int i = 0; i < 3; i++)
        {
            if (ParticleManager.Instance != null)
            {
                Vector3 sparkPos = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 1f), 0f);
                ParticleManager.Instance.PlayRobotLaserHit(sparkPos);
            }
        }

        // Buyuk ekran sarsintisi
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnCombo(10);
        }

        currentPhase = newPhase;
        ApplyPhaseModifiers();

        // Bildirim
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"FAZ {newPhase}!", "ROBOT GUCLENIYOR!", NotificationType.Warning);
        }

        phaseTransitioning = false;
    }

    /// <summary>
    /// Phase'e gore stat modifier'lari uygular.
    /// </summary>
    private void ApplyPhaseModifiers()
    {
        switch (currentPhase)
        {
            case 2:
                // Phase 2: Hafif buff
                patrolSpeed *= 1.2f;
                projectileCooldown *= 0.85f;
                laserCooldown *= 0.85f;
                break;

            case 3:
                // Phase 3: Agresif mod
                patrolSpeed *= 1.3f;
                projectileCooldown *= 0.7f;
                laserCooldown *= 0.7f;
                meleeCooldown *= 0.7f;
                dashCooldown *= 0.7f;
                break;
        }
    }

    #endregion

    #region Combo Attack System

    /// <summary>
    /// Combo saldiri dizisine devam eder.
    /// </summary>
    private void ContinueCombo()
    {
        if (!canComboAttack || currentComboCount >= maxComboLength)
        {
            ResetCombo();
            return;
        }

        currentComboCount++;
        comboTimer = comboDelay + 0.5f;
        isComboActive = true;

        // Combo saldirisi - farkli hareket patterni
        StartCoroutine(ExecuteComboAttack());
    }

    private IEnumerator ExecuteComboAttack()
    {
        isAttacking = true;
        ChangeState(RobotState.Attack);

        // Animasyon
        if (animator != null)
        {
            animator.SetTrigger(meleeAttackTrigger);
        }

        TriggerAttackFlash();

        yield return new WaitForSeconds(0.15f);

        if (isDead) yield break;

        // Combo hasar (artan)
        float damageMultiplier = 1f + (currentComboCount - 1) * (comboDamageMultiplier - 1f);
        CheckComboMeleeHit(damageMultiplier);

        yield return new WaitForSeconds(comboDelay);

        isAttacking = false;

        // Combo devam edebilir mi kontrol et
        if (currentComboCount < maxComboLength && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= midRangeThreshold)
            {
                comboTimer = comboDelay + 0.3f;
                yield break; // Combo devam edebilir
            }
        }

        ResetCombo();
    }

    private void CheckComboMeleeHit(float damageMultiplier)
    {
        float direction = GetFacingDirection();
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(meleeAttackOffset.x * direction, meleeAttackOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, meleeAttackSize * 1.2f, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.TakeDamage();

                    // Buyuk combo'larda ekstra knockback
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null && currentComboCount >= 3)
                    {
                        Vector2 knockbackDir = new Vector2(direction, 0.5f).normalized;
                        playerRb.AddForce(knockbackDir * contactKnockback * 2f, ForceMode2D.Impulse);
                    }
                }
                break;
            }
        }

        PlaySound(attackSound);
    }

    private void ResetCombo()
    {
        currentComboCount = 0;
        isComboActive = false;
        comboTimer = 0f;
    }

    #endregion

    #region Counter Attack

    /// <summary>
    /// Counter attack kosullarini kontrol eder.
    /// </summary>
    private bool CheckCounterAttackCondition()
    {
        if (player == null) return false;

        // Oyuncu bize dogru hizla yaklasiyorsa
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null) return false;

        Vector2 toPlayer = player.position - transform.position;
        float approachSpeed = -Vector2.Dot(playerRb.linearVelocity, toPlayer.normalized);

        // Hizli yaklasma + sans kontrolu
        if (approachSpeed > 3f && Random.value < counterAttackChance)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Counter attack baslatir.
    /// </summary>
    private void StartCounterAttack()
    {
        isCounterAttacking = true;
        isAttacking = true;
        counterAttackCooldownTimer = counterAttackCooldown;
        ChangeState(RobotState.Attack);

        StartCoroutine(ExecuteCounterAttack());
    }

    private IEnumerator ExecuteCounterAttack()
    {
        // Kisa bekleme (savunma pozu)
        if (animator != null)
        {
            animator.SetTrigger(meleeAttackTrigger);
        }

        // Flash efekti
        if (spriteRenderer != null)
        {
            Color beforeColor = spriteRenderer.color;
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = beforeColor;
        }

        yield return new WaitForSeconds(0.1f);

        if (isDead) yield break;

        // Hizli darbe
        TriggerAttackFlash();

        // Genis alan saldirisi
        float direction = GetFacingDirection();
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(meleeAttackOffset.x * direction * 1.5f, meleeAttackOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, meleeAttackSize * 1.5f, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // Counter attack bonuslu hasar
                    playerController.TakeDamage();
                    playerController.TakeDamage(); // 2x hasar

                    // Guclu knockback
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockbackDir = new Vector2(direction, 0.6f).normalized;
                        playerRb.AddForce(knockbackDir * contactKnockback * 2.5f, ForceMode2D.Impulse);
                    }
                }
                break;
            }
        }

        PlaySound(attackSound);

        yield return new WaitForSeconds(0.3f);

        isCounterAttacking = false;
        isAttacking = false;
    }

    #endregion

    #region State Updates

    private void UpdatePatrolState()
    {
        // Bekleme kontrolu
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                SetWalkingAnimation(true);
            }
            return;
        }

        // Sinir kontrolu
        CheckPatrolBounds();

        // FIX #5: Kenar kontrolu - patrol sirasinda ucurumdan dusmeyi onle
        if (isEdgeAhead && isGrounded)
        {
            TurnAround();
        }

        // FIX #6: Duvar kontrolu - duvara carpinca don
        if (isWallAhead)
        {
            TurnAround();
        }

        // Oyuncu algilanirsa chase'e gec
        if (playerDetected)
        {
            ChangeState(RobotState.Chase);
        }
    }

    private void UpdateChaseState()
    {
        if (!playerDetected)
        {
            // Oyuncu kayboldu, bir sure bekle
            loseTargetTimer += Time.deltaTime;
            if (loseTargetTimer >= loseTargetDelay)
            {
                ChangeState(RobotState.Patrol);
                loseTargetTimer = 0f;
            }
            return;
        }

        loseTargetTimer = 0f;

        // Saldiri yapilamiyorsa bekle
        if (isAttacking || isTeleporting || isGroundSlamming || isStaggered) return;

        // FIX #11: Onceden hesaplanmis mesafeyi kullan
        float distanceToPlayer = cachedDistanceToPlayer;

        // Counter attack kontrolu - oyuncu cok yakin ve hizli yaklasiyorsa
        if (canCounterAttack && distanceToPlayer <= counterAttackRange && counterAttackCooldownTimer <= 0f)
        {
            if (CheckCounterAttackCondition())
            {
                StartCounterAttack();
                return;
            }
        }

        // Phase'e gore saldiri secimi
        AttackDecision decision = MakeAttackDecision(distanceToPlayer);

        switch (decision)
        {
            case AttackDecision.Melee:
                if (canComboAttack && currentComboCount > 0)
                    ContinueCombo();
                else
                    StartMeleeAttack();
                break;

            case AttackDecision.Dash:
                StartDashAttack();
                break;

            case AttackDecision.Laser:
                StartLaserAttack();
                break;

            case AttackDecision.Projectile:
                StartProjectileAttack();
                break;

            case AttackDecision.GroundSlam:
                StartGroundSlam();
                break;

            case AttackDecision.Teleport:
                StartTeleport();
                break;

            case AttackDecision.SpawnDrone:
                SpawnDrone();
                break;

            case AttackDecision.Bomb:
                StartBombAttack();
                break;

            case AttackDecision.Rocket:
                StartRocketAttack();
                break;

            case AttackDecision.None:
                // Hicbir saldiri hazir degilse oyuncuya dogru yuru veya zipla
                if (ShouldJumpToPlayer())
                {
                    PerformJump();
                }
                break;
        }
    }

    /// <summary>
    /// Phase ve duruma gore en iyi saldiriyi secer.
    /// Adaptive AI aktifse oyuncunun davranisina gore karar verir.
    /// </summary>
    private enum AttackDecision
    {
        None, Melee, Dash, Laser, Projectile, GroundSlam, Teleport, SpawnDrone, Bomb, Rocket
    }

    private AttackDecision MakeAttackDecision(float distanceToPlayer)
    {
        // Phase 3'te daha agresif davran
        bool isPhase3 = currentPhase >= 3;
        bool isPhase2 = currentPhase >= 2;

        // Drone spawn kontrolu (Phase 2+)
        if (isPhase2 && canSpawnDrones && droneSpawnCooldownTimer <= 0f && activeDrones.Count < maxDrones)
        {
            if (Random.value < 0.3f) // %30 sans
            {
                return AttackDecision.SpawnDrone;
            }
        }

        // Ground slam kontrolu (Phase 2+, uzak mesafede)
        if (isPhase2 && canGroundSlam && groundSlamCooldownTimer <= 0f && distanceToPlayer > midRangeThreshold && distanceToPlayer < longRangeThreshold)
        {
            if (Random.value < 0.25f) // %25 sans
            {
                return AttackDecision.GroundSlam;
            }
        }

        // Roket saldirisi (Phase 2+, orta-uzak mesafe)
        if (isPhase2 && canFireRockets && rocketCooldownTimer <= 0f && distanceToPlayer > midRangeThreshold)
        {
            if (Random.value < 0.35f) // %35 sans
            {
                return AttackDecision.Rocket;
            }
        }

        // Bomba saldirisi (Phase 2+, orta mesafe)
        if (isPhase2 && canThrowBombs && bombCooldownTimer <= 0f && distanceToPlayer > dashMaxRange && distanceToPlayer < longRangeThreshold)
        {
            if (Random.value < 0.4f) // %40 sans
            {
                return AttackDecision.Bomb;
            }
        }

        // Teleport kontrolu (hasar aldiginda veya Phase 3'te stratejik kullanim)
        if (canTeleport && teleportCooldownTimer <= 0f)
        {
            if (recentDamageTaken >= teleportDamageThreshold || (isPhase3 && distanceToPlayer > longRangeThreshold && Random.value < 0.2f))
            {
                return AttackDecision.Teleport;
            }
        }

        // Yakin mesafe - Melee saldiri (en oncelikli)
        if (distanceToPlayer <= midRangeThreshold && meleeCooldownTimer <= 0f)
        {
            return AttackDecision.Melee;
        }

        // Yakin-orta mesafe - Dash/Charge saldiri
        if (distanceToPlayer >= dashMinRange && distanceToPlayer <= dashMaxRange && dashCooldownTimer <= 0f)
        {
            // Adaptive AI: Oyuncu cok ziplarsa dash kullanma
            if (useAdaptiveAI && playerJumpFrequency > 0.6f && Random.value < playerJumpFrequency)
            {
                // Oyuncu cok ziplayan birisi, dash yerine lazer dene
                if (laserCooldownTimer <= 0f)
                    return AttackDecision.Laser;
            }
            return AttackDecision.Dash;
        }

        // Orta mesafe - Lazer saldiri
        if (distanceToPlayer <= longRangeThreshold && distanceToPlayer > midRangeThreshold && laserCooldownTimer <= 0f)
        {
            return AttackDecision.Laser;
        }

        // Uzak mesafe - Mermi saldiri (burst fire)
        if (distanceToPlayer > longRangeThreshold && projectileCooldownTimer <= 0f)
        {
            return AttackDecision.Projectile;
        }

        return AttackDecision.None;
    }

    private void UpdateAttackState()
    {
        // Saldiri tamamlaninca chase'e don
        if (!isAttacking)
        {
            ChangeState(RobotState.Chase);
        }
    }

    private void ChangeState(RobotState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case RobotState.Patrol:
                SetWalkingAnimation(true);
                break;

            case RobotState.Chase:
                SetWalkingAnimation(true);
                break;

            case RobotState.Attack:
                SetWalkingAnimation(false);
                break;
        }
    }

    #endregion

    #region Patrol

    private void PatrolMovement()
    {
        if (isWaiting) return;

        // FIX #5: Kenar varsa hareket etme
        if (isEdgeAhead) return;

        // FIX #6: Duvar varsa hareket etme
        if (isWallAhead) return;

        float direction = movingRight ? 1f : -1f;
        float currentPatrolSpeed = isRageActive ? patrolSpeed * rageSpeedMultiplier : patrolSpeed;

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(direction * currentPatrolSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.position += Vector3.right * direction * currentPatrolSpeed * Time.fixedDeltaTime;
        }
    }

    private void CheckPatrolBounds()
    {
        if (movingRight && transform.position.x >= startPosition.x + patrolRightDistance)
        {
            TurnAround();
        }
        else if (!movingRight && transform.position.x <= startPosition.x - patrolLeftDistance)
        {
            TurnAround();
        }
    }

    private void TurnAround()
    {
        movingRight = !movingRight;

        if (waitTimeAtEdge > 0f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtEdge;
            SetWalkingAnimation(false);
        }
    }

    private void StopMovement()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    #endregion

    #region Chase Movement

    /// <summary>
    /// Oyuncu algilandiginda ama saldiri menzilinde degilken oyuncuya dogru yurur.
    /// patrolSpeed * 1.5f hizinda hareket eder. Rage modda ek bonus alir.
    /// </summary>
    private void ChaseMovement()
    {
        if (player == null || !playerDetected) return;

        // FIX #5: Kenar varsa chase sirasinda durma
        if (isEdgeAhead)
        {
            StopMovement();
            return;
        }

        // FIX #6: Duvar varsa chase sirasinda durma
        if (isWallAhead)
        {
            StopMovement();
            return;
        }

        // FIX #11: Onceden hesaplanmis mesafeyi kullan
        float distanceToPlayer = cachedDistanceToPlayer;

        // Saldiri menzilindeyse durma (attack state halleder)
        // Ama hicbir saldiri hazir degilse yaklasma devam eder
        bool anyAttackReady = (distanceToPlayer <= midRangeThreshold && meleeCooldownTimer <= 0f) ||
                              (distanceToPlayer >= dashMinRange && distanceToPlayer <= dashMaxRange && dashCooldownTimer <= 0f) ||
                              (distanceToPlayer <= longRangeThreshold && distanceToPlayer > midRangeThreshold && laserCooldownTimer <= 0f) ||
                              (distanceToPlayer > longRangeThreshold && projectileCooldownTimer <= 0f);

        if (anyAttackReady)
        {
            // Saldiri hazir, dur
            StopMovement();
            return;
        }

        // Oyuncuya dogru yuru
        float chaseSpeed = patrolSpeed * 1.5f;
        if (isRageActive)
        {
            chaseSpeed *= rageSpeedMultiplier;
        }

        float directionX = player.position.x > transform.position.x ? 1f : -1f;

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(directionX * chaseSpeed, rb.linearVelocity.y);
        }
    }

    #endregion

    #region Player Detection

    private void CheckPlayerDetection()
    {
        if (player == null)
        {
            playerDetected = false;
            return;
        }

        // FIX #11: Onceden hesaplanmis mesafeyi kullan
        float distanceToPlayer = cachedDistanceToPlayer;

        // Menzil disindaysa
        if (distanceToPlayer > detectionRange)
        {
            playerDetected = false;
            return;
        }

        // Gorus hatti kontrolu
        if (needsLineOfSight)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distanceToPlayer, obstacleLayer);

            if (hit.collider != null)
            {
                // Engel var, oyuncu gorunmuyor
                playerDetected = false;
                return;
            }
        }

        playerDetected = true;
    }

    #endregion

    #region Attacks

    private void StartProjectileAttack()
    {
        isAttacking = true;
        ChangeState(RobotState.Attack);

        // Animasyon tetikle
        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        // Saldiri flash
        TriggerAttackFlash(flashProjectile);

        // Saldiriyi biraz gecikmeyle yap (animasyonla senkron)
        StartCoroutine(ExecuteProjectileAttack());
    }

    private IEnumerator ExecuteProjectileAttack()
    {
        yield return new WaitForSeconds(0.3f);

        if (isDead || player == null) yield break;

        // Burst fire - birden fazla mermi at
        for (int i = 0; i < projectileBurstCount; i++)
        {
            if (isDead || player == null) yield break;

            FireProjectile(i);

            if (i < projectileBurstCount - 1)
            {
                yield return new WaitForSeconds(projectileBurstDelay);
            }
        }

        projectileCooldownTimer = GetEffectiveCooldown(projectileCooldown);

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
    }

    private void FireProjectile(int burstIndex = 0)
    {
        if (player == null) return;

        // Predictive targeting kullan
        Vector2 targetPos = usePredictiveTargeting ? PredictPlayerPosition(predictionTime) : (Vector2)player.position;
        Vector2 baseDirection = (targetPos - (Vector2)firePoint.position).normalized;

        // Spread angle hesapla (burst icin farkli acilar)
        float spreadOffset = 0f;
        if (projectileBurstCount > 1 && projectileSpreadAngle > 0f)
        {
            // Mermileri yay seklinde dagit
            float totalSpread = projectileSpreadAngle * (projectileBurstCount - 1);
            float startAngle = -totalSpread / 2f;
            spreadOffset = startAngle + (projectileSpreadAngle * burstIndex);
        }

        // Yonu spread ile ayarla
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + spreadOffset;
        Vector2 direction = new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        );

        GameObject projectile;

        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        }
        else
        {
            // Runtime mermi olustur
            projectile = CreateRuntimeProjectile();
        }

        if (projectile == null) return;

        // Projectile component
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj == null)
        {
            proj = projectile.AddComponent<Projectile>();
        }

        proj.damage = projectileDamage;
        proj.speed = projectileSpeed;
        proj.isPlayerBullet = false;

        // Rigidbody velocity ayarla
        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.linearVelocity = direction * projectileSpeed;
        }

        // Mermi rotasyonu
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);

        // Ses efekti
        PlaySound(attackSound);
    }

    /// <summary>
    /// Gercekci mermi olusturur - neon trail efektli, doner mermi
    /// </summary>
    private GameObject CreateRuntimeProjectile()
    {
        GameObject projectile = new GameObject("RobotBullet");
        projectile.transform.position = firePoint.position;
        projectile.tag = "EnemyProjectile";

        // Ana mermi sprite
        SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
        sr.color = projectileColor;
        sr.sortingOrder = 10;

        // Mermi sprite'i olustur (uzun oval/mermi sekli)
        if (cachedBulletSprite == null)
        {
            cachedBulletSprite = CreateBulletSprite();
        }
        sr.sprite = cachedBulletSprite;
        projectile.transform.localScale = Vector3.one * projectileSize;

        // Glow efekti icin child obje
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(projectile.transform);
        glow.transform.localPosition = Vector3.zero;
        SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = cachedBulletSprite;
        glowSr.color = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0.3f);
        glowSr.sortingOrder = 9;
        glow.transform.localScale = Vector3.one * 1.5f;

        // Trail efekti
        if (projectileHasTrail)
        {
            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = projectileSize * 0.3f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = projectileColor;
            trail.endColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0f);
            trail.sortingOrder = 8;
        }

        // Collider
        CircleCollider2D projCol = projectile.AddComponent<CircleCollider2D>();
        projCol.radius = projectileSize * 0.4f;
        projCol.isTrigger = true;

        // Rigidbody
        Rigidbody2D projRb = projectile.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0f;

        // Mermi script'i
        RobotBullet bulletScript = projectile.AddComponent<RobotBullet>();
        bulletScript.damage = projectileDamage;
        bulletScript.glowColor = projectileColor;

        // Otomatik yok etme
        Destroy(projectile, 5f);

        return projectile;
    }

    /// <summary>
    /// Mermi seklinde sprite olusturur
    /// </summary>
    private Sprite CreateBulletSprite()
    {
        int width = 16;
        int height = 8;
        Texture2D texture = new Texture2D(width, height);
        Color transparent = new Color(0f, 0f, 0f, 0f);

        // Mermi sekli - oval/roket
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float centerY = height / 2f;
                float distFromCenter = Mathf.Abs(y - centerY) / centerY;

                // Onden daralan, arkadan genis
                float widthAtX = 1f - (float)x / width * 0.5f;

                if (distFromCenter < widthAtX)
                {
                    // Ic kisim - parlak
                    float brightness = 1f - distFromCenter * 0.5f;
                    texture.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16);
    }

    /// <summary>
    /// Drone sprite'i olusturur - altigen/hexagon teknolojik gorunum
    /// </summary>
    private Sprite CreateDroneSprite()
    {
        int size = 24;
        Texture2D texture = new Texture2D(size, size);
        Color transparent = Color.clear;

        float center = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // Hexagon/altigen sekli
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);

                // Altigen formulu
                float hexDist = Mathf.Max(dx * 0.866f + dy * 0.5f, dy);
                float radius = size * 0.4f;

                if (hexDist < radius - 2f)
                {
                    // Ic kisim - parlak merkez
                    float t = hexDist / radius;
                    float brightness = 0.8f - t * 0.3f;

                    // Merkeze yakin daha parlak
                    if (hexDist < radius * 0.3f)
                    {
                        brightness = 1f;
                    }

                    texture.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                }
                else if (hexDist < radius)
                {
                    // Kenar - koyu cerceve
                    texture.SetPixel(x, y, new Color(0.3f, 0.3f, 0.3f, 1f));
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        // Merkeze kucuk bir goz/sensor ekle
        int eyeRadius = 3;
        for (int x = (int)center - eyeRadius; x <= (int)center + eyeRadius; x++)
        {
            for (int y = (int)center - eyeRadius; y <= (int)center + eyeRadius; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < eyeRadius)
                {
                    texture.SetPixel(x, y, new Color(1f, 0.3f, 0.3f, 1f)); // Kirmizi goz
                }
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24);
    }

    /// <summary>
    /// Bomba firlatir - parabolik yol, patlama efekti
    /// </summary>
    private void StartBombAttack()
    {
        if (!canThrowBombs) return;

        isAttacking = true;
        ChangeState(RobotState.Attack);

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        TriggerAttackFlash(flashBomb);
        StartCoroutine(ExecuteBombAttack());
    }

    private IEnumerator ExecuteBombAttack()
    {
        yield return new WaitForSeconds(0.3f);

        if (isDead || player == null) yield break;

        ThrowBomb();

        bombCooldownTimer = GetEffectiveCooldown(bombCooldown);

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    private void ThrowBomb()
    {
        if (player == null) return;

        GameObject bomb;
        if (bombPrefab != null)
        {
            bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.identity);
        }
        else
        {
            bomb = CreateRuntimeBomb();
        }

        if (bomb == null) return;

        // Firlatma yonu ve hizi hesapla
        Vector2 toPlayer = player.position - firePoint.position;
        float distance = toPlayer.magnitude;

        // Parabolik atis icin hiz hesapla
        float angle = bombThrowAngle * Mathf.Deg2Rad;
        float gravity = Physics2D.gravity.magnitude;

        // Basit parabolik hesaplama
        float vx = toPlayer.x / (distance / bombThrowSpeed);
        float vy = bombThrowSpeed * Mathf.Sin(angle) + (gravity * distance / (2f * bombThrowSpeed));

        Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
        if (bombRb != null)
        {
            bombRb.linearVelocity = new Vector2(vx, vy);
            bombRb.angularVelocity = 360f; // Donsun
        }

        PlaySound(attackSound);
    }

    private GameObject CreateRuntimeBomb()
    {
        GameObject bomb = new GameObject("RobotBomb");
        bomb.transform.position = firePoint.position;
        bomb.tag = "EnemyProjectile";

        // Sprite
        SpriteRenderer sr = bomb.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // Bomba sprite'i
        if (cachedBombSprite == null)
        {
            cachedBombSprite = CreateBombSprite();
        }
        sr.sprite = cachedBombSprite;
        sr.color = bombColor;
        bomb.transform.localScale = Vector3.one * bombSize;

        // Fitil efekti
        GameObject fuse = new GameObject("Fuse");
        fuse.transform.SetParent(bomb.transform);
        fuse.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        SpriteRenderer fuseSr = fuse.AddComponent<SpriteRenderer>();
        fuseSr.color = Color.red;
        fuseSr.sortingOrder = 11;

        // Collider
        CircleCollider2D col = bomb.AddComponent<CircleCollider2D>();
        col.radius = bombSize * 0.4f;

        // Rigidbody
        Rigidbody2D rb = bomb.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.5f;

        // Bomba script'i
        RobotBomb bombScript = bomb.AddComponent<RobotBomb>();
        bombScript.damage = bombDamage;
        bombScript.explosionRadius = bombExplosionRadius;
        bombScript.fuseTime = bombFuseTime;
        bombScript.explosionColor = explosionColor;

        return bomb;
    }

    private Sprite CreateBombSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        Color transparent = new Color(0f, 0f, 0f, 0f);

        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist < radius)
                {
                    // Bomba ic kismi - gradient
                    float brightness = 0.3f + (1f - dist / radius) * 0.4f;
                    texture.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                }
                else if (dist < radius + 1f)
                {
                    // Kenar
                    texture.SetPixel(x, y, new Color(0.2f, 0.2f, 0.2f, 1f));
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16);
    }

    /// <summary>
    /// Takipli roket atesler
    /// </summary>
    private void StartRocketAttack()
    {
        if (!canFireRockets) return;

        isAttacking = true;
        ChangeState(RobotState.Attack);

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        TriggerAttackFlash(flashProjectile);
        StartCoroutine(ExecuteRocketAttack());
    }

    private IEnumerator ExecuteRocketAttack()
    {
        yield return new WaitForSeconds(0.4f);

        if (isDead || player == null) yield break;

        FireRocket();

        rocketCooldownTimer = GetEffectiveCooldown(rocketCooldown);

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    private void FireRocket()
    {
        if (player == null) return;

        GameObject rocket = CreateRuntimeRocket();
        if (rocket == null) return;

        Vector2 direction = (player.position - firePoint.position).normalized;

        // Baslangic hizi
        Rigidbody2D rocketRb = rocket.GetComponent<Rigidbody2D>();
        if (rocketRb != null)
        {
            rocketRb.linearVelocity = direction * rocketSpeed;
        }

        // Rotasyon
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rocket.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        PlaySound(attackSound);
    }

    private GameObject CreateRuntimeRocket()
    {
        GameObject rocket = new GameObject("RobotRocket");
        rocket.transform.position = firePoint.position;
        rocket.tag = "EnemyProjectile";

        // Sprite
        SpriteRenderer sr = rocket.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // Roket sprite'i
        if (cachedRocketSprite == null)
        {
            cachedRocketSprite = CreateRocketSprite();
        }
        sr.sprite = cachedRocketSprite;
        sr.color = Color.white;
        rocket.transform.localScale = Vector3.one * 0.5f;

        // Alev efekti (trail)
        TrailRenderer trail = rocket.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.5f, 0f, 1f);
        trail.endColor = new Color(1f, 0f, 0f, 0f);
        trail.sortingOrder = 9;

        // Collider
        CircleCollider2D col = rocket.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.isTrigger = true;

        // Rigidbody
        Rigidbody2D rb = rocket.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // Roket script'i
        RobotRocket rocketScript = rocket.AddComponent<RobotRocket>();
        rocketScript.target = player;
        rocketScript.damage = rocketDamage;
        rocketScript.speed = rocketSpeed;
        rocketScript.turnSpeed = rocketTurnSpeed;
        rocketScript.trackingDuration = rocketTrackingDuration;
        rocketScript.explosionRadius = rocketExplosionRadius;

        Destroy(rocket, rocketTrackingDuration + 2f);

        return rocket;
    }

    private Sprite CreateRocketSprite()
    {
        int width = 20;
        int height = 8;
        Texture2D texture = new Texture2D(width, height);
        Color transparent = new Color(0f, 0f, 0f, 0f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float centerY = height / 2f;
                float distFromCenter = Mathf.Abs(y - centerY) / centerY;

                // Roket sekli - sivri uc
                float widthAtX;
                if (x < width * 0.7f)
                {
                    widthAtX = 0.8f;
                }
                else
                {
                    // Sivri uc
                    widthAtX = 0.8f * (1f - (x - width * 0.7f) / (width * 0.3f));
                }

                if (distFromCenter < widthAtX)
                {
                    // Roket govdesi
                    if (x < 4)
                    {
                        // Kanatlar (arka)
                        texture.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f, 1f));
                    }
                    else
                    {
                        float brightness = 0.7f + (1f - distFromCenter) * 0.3f;
                        texture.SetPixel(x, y, new Color(brightness, brightness * 0.9f, brightness * 0.8f, 1f));
                    }
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.3f, 0.5f), 16);
    }

    private void StartLaserAttack()
    {
        isAttacking = true;
        ChangeState(RobotState.Attack);

        // Animasyon tetikle
        if (animator != null)
        {
            animator.SetTrigger(lazerAttackTrigger);
        }

        // Saldiri flash
        TriggerAttackFlash(flashLaser);

        StartCoroutine(ExecuteLaserAttack());
    }

    private IEnumerator ExecuteLaserAttack()
    {
        yield return new WaitForSeconds(0.2f);

        if (isDead || player == null) yield break;

        // Lazeri aktif et
        isLaserActive = true;

        if (laserLineRenderer != null)
        {
            laserLineRenderer.enabled = true;
            // Cekirdek isin beyaz
            laserLineRenderer.startColor = Color.white;
            laserLineRenderer.endColor = new Color(1f, 1f, 1f, 0.5f);
        }
        // Glow katmani kirmizi/saydam
        if (laserGlowRenderer != null)
        {
            laserGlowRenderer.enabled = true;
        }

        // Lazer suresi boyunca aktif tut
        float elapsed = 0f;
        bool damageDealt = false;

        while (elapsed < laserDuration && !isDead)
        {
            elapsed += Time.deltaTime;

            // Lazer pozisyonunu guncelle
            UpdateLaserPosition();

            // Hasar kontrolu (sadece bir kez)
            if (!damageDealt)
            {
                CheckLaserHit();
                damageDealt = true;
            }

            yield return null;
        }

        // Lazeri kapat
        isLaserActive = false;
        if (laserLineRenderer != null)
        {
            laserLineRenderer.enabled = false;
            // Renkleri varsayilana dondur
            laserLineRenderer.startColor = laserColor;
            laserLineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
        }
        if (laserGlowRenderer != null)
        {
            laserGlowRenderer.enabled = false;
        }

        laserCooldownTimer = GetEffectiveCooldown(laserCooldown);

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
    }

    private void UpdateLaserPosition()
    {
        if (laserLineRenderer == null || player == null) return;

        Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (player.position - startPos).normalized;
        Vector3 endPos = startPos + direction * laserRange;

        // Engel kontrolu
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, laserRange, obstacleLayer);
        if (hit.collider != null)
        {
            endPos = hit.point;
        }

        laserLineRenderer.SetPosition(0, startPos);
        laserLineRenderer.SetPosition(1, endPos);

        // Genislik titresimi
        float widthVibration = laserWidth + Mathf.Sin(Time.time * 20f) * 0.03f;
        laserLineRenderer.startWidth = widthVibration;
        laserLineRenderer.endWidth = widthVibration * 0.5f;

        // Glow layer pozisyon ve boyut
        if (laserGlowRenderer != null && laserGlowRenderer.enabled)
        {
            laserGlowRenderer.SetPosition(0, startPos);
            laserGlowRenderer.SetPosition(1, endPos);
            float glowVibration = laserWidth * 4f + Mathf.Sin(Time.time * 15f) * 0.05f;
            laserGlowRenderer.startWidth = glowVibration;
            laserGlowRenderer.endWidth = glowVibration * 0.5f;
        }
    }

    private void CheckLaserHit()
    {
        if (player == null) return;

        Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (player.position - startPos).normalized;

        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, laserRange);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
            {
                PlayerController playerController = hit.collider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // Lazer hasari birden fazla kez vurabiliyor
                    for (int i = 0; i < laserDamage; i++)
                    {
                        playerController.TakeDamage();
                    }
                }
                break;
            }
        }

        // Ses efekti
        PlaySound(attackSound);
    }

    private void StartMeleeAttack()
    {
        if (isStaggered) return; // Stagger sirasinda saldiri yok

        isAttacking = true;
        ChangeState(RobotState.Attack);

        // HYPER ARMOR AKTIF - melee sirasinda daha az hasar al
        EnableHyperArmor();

        // Combo baslat
        if (canComboAttack)
        {
            currentComboCount = 1;
            isComboActive = true;
            comboTimer = comboDelay + 0.5f;
        }

        // Animasyon tetikle
        if (animator != null)
        {
            animator.SetTrigger(meleeAttackTrigger);
        }

        // Saldiri flash
        TriggerAttackFlash(flashMelee);

        StartCoroutine(ExecuteMeleeAttack());
    }

    private IEnumerator ExecuteMeleeAttack()
    {
        yield return new WaitForSeconds(0.25f);

        if (isDead || isStaggered)
        {
            DisableHyperArmor();
            isAttacking = false;
            yield break;
        }

        // Melee hasar alani kontrolu
        CheckMeleeHit();

        meleeCooldownTimer = GetEffectiveCooldown(meleeCooldown);

        yield return new WaitForSeconds(0.3f);

        DisableHyperArmor();
        isAttacking = false;
    }

    private void CheckMeleeHit()
    {
        float direction = GetFacingDirection();
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(meleeAttackOffset.x * direction, meleeAttackOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, meleeAttackSize, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.TakeDamage();

                    // Knockback
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null && contactKnockback > 0)
                    {
                        Vector2 knockbackDir = new Vector2(direction, 0.3f).normalized;
                        playerRb.AddForce(knockbackDir * contactKnockback * 1.5f, ForceMode2D.Impulse);
                    }
                }
                break;
            }
        }

        // Ses efekti
        PlaySound(attackSound);
    }

    #endregion

    #region Dash/Charge Attack

    /// <summary>
    /// Dash/Charge saldirisi baslatir. Robot yuksek hizda oyuncuya dogru atilir,
    /// temas halinde hasar verir ve arkasinda neon bir iz birakir.
    /// </summary>
    private void StartDashAttack()
    {
        if (isStaggered) return; // Stagger sirasinda saldiri yok

        isAttacking = true;
        isDashing = true;
        ChangeState(RobotState.Attack);

        // HYPER ARMOR AKTIF - dash sirasinda cok daha az hasar al
        EnableHyperArmor();

        // Animasyon tetikle
        if (animator != null)
        {
            animator.SetTrigger(dashAttackTrigger);
        }

        // Saldiri flash
        TriggerAttackFlash(flashDash);

        StartCoroutine(ExecuteDashAttack());
    }

    private IEnumerator ExecuteDashAttack()
    {
        if (player == null || isStaggered)
        {
            isDashing = false;
            isAttacking = false;
            DisableHyperArmor();
            yield break;
        }

        // Dash yonunu hesapla (baslangicta kilitle)
        Vector2 dashDirection = (player.position - transform.position).normalized;
        // Sadece yatay dash (y eksenini minimumda tut)
        dashDirection = new Vector2(dashDirection.x, dashDirection.y * 0.3f).normalized;

        // FIX #2: spriteRenderer.flipX ile yon degistir (scale dokunulmaz)
        // Sprite dogal olarak sola bakar: saga bakmak icin flipX=true
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = dashDirection.x > 0f;
        }

        // Trail'i aktif et
        if (dashTrailRenderer != null)
        {
            dashTrailRenderer.enabled = true;
            dashTrailRenderer.positionCount = 0;
        }

        // Kisa hazirlik gecikmesi
        yield return new WaitForSeconds(0.1f);

        if (isDead)
        {
            isDashing = false;
            isAttacking = false;
            yield break;
        }

        // Dash hareketi
        float elapsed = 0f;
        bool hitPlayer = false;
        int trailIndex = 0;

        while (elapsed < dashDuration && !isDead)
        {
            elapsed += Time.deltaTime;

            // FIX #9: Dash sirasinda zemin kontrolu - ucurumdan dusmeyi onle
            if (CheckDashGroundSafety(dashDirection))
            {
                // Zemin yok, dash'i erken bitir
                break;
            }

            // FIX #6: Dash sirasinda duvar kontrolu
            if (CheckDashWallAhead(dashDirection))
            {
                // Duvar var, dash'i erken bitir
                break;
            }

            // Yuksek hizda hareket
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
            }

            // Trail guncelle
            if (dashTrailRenderer != null)
            {
                trailIndex++;
                dashTrailRenderer.positionCount = trailIndex;
                dashTrailRenderer.SetPosition(trailIndex - 1, transform.position);
            }

            // Oyuncu ile temas kontrolu (dash sirasinda)
            if (!hitPlayer)
            {
                hitPlayer = CheckDashHit();
            }

            yield return null;
        }

        // Dash bitti, yavasla
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        isDashing = false;

        // Trail'i yavasce kapat
        StartCoroutine(FadeDashTrail());

        dashCooldownTimer = GetEffectiveCooldown(dashCooldown);

        yield return new WaitForSeconds(0.3f);

        // Hyper armor kapat
        DisableHyperArmor();
        isAttacking = false;
    }

    /// <summary>
    /// FIX #9: Dash sirasinda ileri yonde zemin olup olmadigini kontrol eder.
    /// Zemin yoksa true doner (dash kesilmeli).
    /// </summary>
    private bool CheckDashGroundSafety(Vector2 dashDirection)
    {
        if (effectiveGroundLayer == 0) return false;

        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(dashDirection.x * edgeCheckForwardDistance, groundCheckYOffset);
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.down, edgeCheckDownDistance, effectiveGroundLayer);
        return hit.collider == null; // Zemin yoksa true
    }

    /// <summary>
    /// FIX #6: Dash sirasinda ileri yonde duvar olup olmadigini kontrol eder.
    /// Duvar varsa true doner (dash kesilmeli).
    /// </summary>
    private bool CheckDashWallAhead(Vector2 dashDirection)
    {
        if (effectiveGroundLayer == 0) return false;

        Vector2 origin = (Vector2)transform.position + new Vector2(0f, groundCheckYOffset);
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dashDirection.x, 0f), wallCheckDistance, effectiveGroundLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// Dash sirasinda oyuncuya temas kontrolu yapar.
    /// Temas halinde hasar ve knockback uygular.
    /// </summary>
    private bool CheckDashHit()
    {
        if (player == null) return false;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // Temas mesafesi kontrolu
        if (distToPlayer <= 1.2f)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage();

                // Guclu knockback
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDir = (player.position - transform.position).normalized;
                    knockbackDir.y = 0.4f;
                    playerRb.AddForce(knockbackDir.normalized * dashKnockback, ForceMode2D.Impulse);
                }
            }

            // Ses efekti
            PlaySound(attackSound);
            return true;
        }

        return false;
    }

    private void SetupDashTrail()
    {
        GameObject trailObj = new GameObject("DashTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;

        dashTrailRenderer = trailObj.AddComponent<LineRenderer>();
        dashTrailRenderer.positionCount = 0;
        dashTrailRenderer.startWidth = dashTrailWidth;
        dashTrailRenderer.endWidth = dashTrailWidth * 0.2f;

        // Material
        dashTrailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        dashTrailRenderer.startColor = dashTrailColor;
        dashTrailRenderer.endColor = new Color(dashTrailColor.r, dashTrailColor.g, dashTrailColor.b, 0.1f);

        dashTrailRenderer.sortingOrder = 8;
        dashTrailRenderer.enabled = false;
    }

    /// <summary>
    /// Dash trail'inin alfa degerini yavasce azaltarak soldurur, sonra kapatir.
    /// </summary>
    private IEnumerator FadeDashTrail()
    {
        if (dashTrailRenderer == null) yield break;

        float fadeDuration = 0.4f;
        float elapsed = 0f;
        Color startColorA = dashTrailRenderer.startColor;
        Color endColorA = dashTrailRenderer.endColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            dashTrailRenderer.startColor = new Color(startColorA.r, startColorA.g, startColorA.b, startColorA.a * alpha);
            dashTrailRenderer.endColor = new Color(endColorA.r, endColorA.g, endColorA.b, endColorA.a * alpha);

            yield return null;
        }

        dashTrailRenderer.enabled = false;
        dashTrailRenderer.positionCount = 0;
        dashTrailRenderer.startColor = startColorA;
        dashTrailRenderer.endColor = endColorA;
    }

    /// <summary>
    /// Neon Beam Sweep saldirisi - 120 derecelik donen lazer isini
    /// </summary>
    private System.Collections.IEnumerator BeamSweepAttack()
    {
        isAttacking = true;
        isBeamSweeping = true;
        EnableHyperArmor();

        // Uyari - yere cizgi ciz
        if (laserLineRenderer != null)
        {
            laserLineRenderer.enabled = true;
            laserLineRenderer.startColor = new Color(beamSweepColor.r, beamSweepColor.g, beamSweepColor.b, 0.3f);
            laserLineRenderer.endColor = new Color(beamSweepColor.r, beamSweepColor.g, beamSweepColor.b, 0.1f);
            laserLineRenderer.startWidth = 0.05f;
            laserLineRenderer.endWidth = 0.05f;
        }

        float warningTimer = 0f;
        float facingDir = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        float startAngle = facingDir > 0 ? -beamSweepArc / 2f : 180f - beamSweepArc / 2f;

        // Warning phase - thin line preview
        while (warningTimer < beamSweepWarning)
        {
            warningTimer += Time.deltaTime;
            float warningPulse = Mathf.Sin(warningTimer * 15f) * 0.5f + 0.5f;
            if (laserLineRenderer != null)
            {
                float angle = startAngle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                laserLineRenderer.SetPosition(0, transform.position);
                laserLineRenderer.SetPosition(1, (Vector2)transform.position + dir * beamSweepRange * warningPulse);
            }
            yield return null;
        }

        // Sweep phase
        if (laserLineRenderer != null)
        {
            laserLineRenderer.startWidth = 0.4f;
            laserLineRenderer.endWidth = 0.2f;
            laserLineRenderer.startColor = beamSweepColor;
            laserLineRenderer.endColor = beamSweepColor;
        }

        float sweepTimer = 0f;
        float damageTickTimer = 0f;

        while (sweepTimer < beamSweepDuration)
        {
            sweepTimer += Time.deltaTime;
            damageTickTimer += Time.deltaTime;
            float progress = sweepTimer / beamSweepDuration;
            float currentAngle = startAngle + beamSweepArc * progress;
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 beamDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            if (laserLineRenderer != null)
            {
                laserLineRenderer.SetPosition(0, transform.position);
                laserLineRenderer.SetPosition(1, (Vector2)transform.position + beamDir * beamSweepRange);
            }

            // Hasar - her 0.2 saniyede kontrol et
            if (damageTickTimer >= 0.2f)
            {
                damageTickTimer = 0f;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, beamDir, beamSweepRange);
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    PlayerController pc = hit.collider.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage();
                }

                // BoxCast for wider beam
                RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, new Vector2(0.4f, 0.4f), currentAngle, beamDir, beamSweepRange);
                foreach (var h in hits)
                {
                    if (h.collider != null && h.collider.CompareTag("Player"))
                    {
                        PlayerController pc = h.collider.GetComponent<PlayerController>();
                        if (pc != null) pc.TakeDamage();
                        break;
                    }
                }
            }

            // Kamera sarsmasi
            if (CameraFollow.Instance != null)
                CameraFollow.Instance.Shake(0.05f, 0.05f);

            yield return null;
        }

        // Bitir
        if (laserLineRenderer != null)
        {
            laserLineRenderer.enabled = false;
        }

        DisableHyperArmor();
        isBeamSweeping = false;
        isAttacking = false;
        beamSweepCooldownTimer = beamSweepCooldown;
    }

    /// <summary>
    /// Charge/dash saldirilarinda neon trail efekti
    /// Solan sprite izi birakir
    /// </summary>
    private void CreateNeonTrail()
    {
        if (spriteRenderer == null) return;

        GameObject trail = new GameObject("NeonTrail");
        trail.transform.position = transform.position;
        trail.transform.localScale = transform.localScale;

        SpriteRenderer trailSr = trail.AddComponent<SpriteRenderer>();
        trailSr.sprite = spriteRenderer.sprite;
        trailSr.color = new Color(dashTrailColor.r, dashTrailColor.g, dashTrailColor.b, 0.5f);
        trailSr.sortingOrder = spriteRenderer.sortingOrder - 1;
        trailSr.flipX = spriteRenderer.flipX;

        // Fade out coroutine - StartCoroutine olmadan basit bir yaklasim
        StartCoroutine(FadeTrail(trailSr));
    }

    private System.Collections.IEnumerator FadeTrail(SpriteRenderer trailSr)
    {
        float fadeDuration = 0.3f;
        float elapsed = 0f;
        Color startColor = trailSr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (trailSr != null)
            {
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
                trailSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        if (trailSr != null)
            Destroy(trailSr.gameObject);
    }

    #endregion

    #region Shield System

    /// <summary>
    /// Shield child objesi ve SpriteRenderer'ini olusturur.
    /// Shield, robotun etrafinda yari saydam renkli bir daire olarak gorunur.
    /// </summary>
    private void SetupShield()
    {
        currentShieldHits = shieldMaxHits;
        shieldBroken = false;
        shieldRegenAccumulator = 0f;

        // Shield child objesi
        shieldObject = new GameObject("Shield");
        shieldObject.transform.SetParent(transform);
        shieldObject.transform.localPosition = Vector3.zero;

        // SpriteRenderer
        shieldVisual = shieldObject.AddComponent<SpriteRenderer>();
        shieldVisual.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 6;

        // Daire sprite olustur (prosedural)
        Texture2D shieldTex = CreateCircleTexture(64, Color.white);
        shieldVisual.sprite = Sprite.Create(shieldTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32);
        shieldVisual.color = shieldColor;

        // Shield boyutu - robotun biraz buyugu
        shieldObject.transform.localScale = Vector3.one * 1.6f;

        UpdateShieldVisual();
    }

    /// <summary>
    /// Prosedural olarak yari saydam daire texture'i olusturur.
    /// Shield gorunumu icin kullanilir.
    /// </summary>
    private Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        float innerRadius = radius * 0.75f;
        Color transparent = new Color(0f, 0f, 0f, 0f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));

                if (dist <= radius && dist >= innerRadius)
                {
                    // Halka kismi (dis cerceve)
                    float edgeFade = 1f - Mathf.Abs(dist - (radius + innerRadius) * 0.5f) / ((radius - innerRadius) * 0.5f);
                    edgeFade = Mathf.Clamp01(edgeFade);
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * edgeFade));
                }
                else if (dist < innerRadius)
                {
                    // Ic kisim (hafif dolu)
                    float innerAlpha = 0.15f;
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, innerAlpha));
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Shield'a gelen hasari isler. Shield aktifse vurusu emer.
    /// Shield kirildiginda gorsel efekt gosterir.
    /// </summary>
    /// <returns>Hasar shield tarafindan emildiyse true doner.</returns>
    public bool TryAbsorbDamage()
    {
        if (!shieldEnabled || shieldBroken || currentShieldHits <= 0) return false;

        currentShieldHits--;
        shieldRegenTimer = 0f; // Rejenerasyon timer'ini sifirla
        shieldRegenAccumulator = 0f; // FIX #3: Accumulator'u da sifirla

        if (currentShieldHits <= 0)
        {
            // Shield kirildi
            shieldBroken = true;
            StartCoroutine(ShieldBreakEffect());
        }

        UpdateShieldVisual();
        return true;
    }

    /// <summary>
    /// Shield rejenerasyon timer'ini gunceller.
    /// FIX #3: shieldRegenAccumulator kullanarak frame'ler arasi fractional birikim saglar.
    /// Her frame'de regenRate * deltaTime kadar birikim yapilir. 1.0'a ulasinca bir hit eklenir.
    /// </summary>
    private void UpdateShieldRegen()
    {
        if (!shieldBroken && currentShieldHits >= shieldMaxHits) return;

        shieldRegenTimer += Time.deltaTime;

        if (shieldRegenTimer >= shieldRegenDelay)
        {
            // FIX #3: Accumulator ile frame'ler arasi birikim
            shieldRegenAccumulator += shieldRegenRate * Time.deltaTime;

            if (shieldRegenAccumulator >= 1f)
            {
                // Bir hit rejenerasyon tamamlandi
                shieldRegenAccumulator -= 1f;
                currentShieldHits = Mathf.Min(currentShieldHits + 1, shieldMaxHits);

                if (currentShieldHits > 0)
                {
                    shieldBroken = false;
                }

                UpdateShieldVisual();
            }
        }
    }

    /// <summary>
    /// Shield gorselini mevcut duruma gore gunceller.
    /// Vurus sayisina gore alfa degeri degisir.
    /// </summary>
    private void UpdateShieldVisual()
    {
        if (shieldVisual == null) return;

        if (shieldBroken || currentShieldHits <= 0)
        {
            shieldVisual.enabled = false;
            return;
        }

        shieldVisual.enabled = true;

        // Shield durumuna gore alfa ayarla
        float healthPercent = (float)currentShieldHits / shieldMaxHits;
        Color currentColor = shieldColor;
        currentColor.a = shieldColor.a * healthPercent;
        shieldVisual.color = currentColor;

        // Shield boyutunu biraz titresim yap
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
        if (shieldObject != null)
        {
            shieldObject.transform.localScale = Vector3.one * 1.6f * pulse;
        }
    }

    /// <summary>
    /// Shield kirildigi anda kisa bir gorsel efekt gosterir.
    /// </summary>
    private IEnumerator ShieldBreakEffect()
    {
        if (shieldVisual == null) yield break;

        // Kirilan shield icin kisa flash
        shieldVisual.enabled = true;
        shieldVisual.color = shieldBreakColor;

        if (shieldObject != null)
        {
            shieldObject.transform.localScale = Vector3.one * 2f;
        }

        float elapsed = 0f;
        float breakDuration = 0.3f;

        while (elapsed < breakDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(shieldBreakColor.a, 0f, elapsed / breakDuration);
            shieldVisual.color = new Color(shieldBreakColor.r, shieldBreakColor.g, shieldBreakColor.b, alpha);

            if (shieldObject != null)
            {
                float scale = Mathf.Lerp(2f, 1.6f, elapsed / breakDuration);
                shieldObject.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        shieldVisual.enabled = false;
    }

    /// <summary>
    /// Shield'a vurus geldiginde kisa bir beyaz flash gosterir.
    /// </summary>
    private IEnumerator ShieldHitFlash()
    {
        if (shieldVisual == null) yield break;

        Color beforeColor = shieldVisual.color;
        shieldVisual.color = Color.white;
        yield return new WaitForSeconds(0.08f);

        // Mevcut duruma geri don
        UpdateShieldVisual();
    }

    #endregion

    #region Rage Mode

    /// <summary>
    /// Rage mode durumunu kontrol eder. Can %30 altina dustugunde aktif olur.
    /// Aktif oldugunda: cooldown'lar %40 azalir, hiz %50 artar, gorsel efekt baslar.
    /// </summary>
    private void UpdateRageMode()
    {
        if (enemyHealth == null) return;

        float healthPercent = enemyHealth.GetHealthPercent();
        bool shouldRage = healthPercent <= rageHealthThreshold && healthPercent > 0f;

        if (shouldRage && !isRageActive)
        {
            ActivateRageModeVisual();
        }
        else if (!shouldRage && isRageActive)
        {
            DeactivateRageMode();
        }

        // Rage gorsel efekti guncelle
        if (isRageActive)
        {
            UpdateRageVisual();
        }
    }

    private void ActivateRageModeVisual()
    {
        isRageActive = true;

        // Animator bilgilendir
        if (animator != null)
        {
            animator.SetBool(rageParam, true);
        }

        // 2x elektrik kivilcimi (yumusak gecis)
        if (ParticleManager.Instance != null)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 sparkPos = transform.position + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-0.5f, 1.5f), 0f);
                ParticleManager.Instance.PlayRobotLaserHit(sparkPos);
            }
        }
    }

    private void DeactivateRageMode()
    {
        isRageActive = false;

        // Animator bilgilendir
        if (animator != null)
        {
            animator.SetBool(rageParam, false);
        }

        // Rengi normale dondur
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Rage mode gorsel efektini gunceller.
    /// Robot kirmizi/turuncu arasinda yanip soner.
    /// </summary>
    private void UpdateRageVisual()
    {
        if (spriteRenderer == null) return;

        float t = (Mathf.Sin(Time.time * rageFlashSpeed) + 1f) * 0.5f;
        Color rageFlash = Color.Lerp(rageColor1, rageColor2, t);

        // Hafif kirmizi tint (robot hala gorunur)
        spriteRenderer.color = Color.Lerp(originalColor, rageFlash, 0.35f);

        // Outer glow kirmizi agresif pulse
        if (outerGlowSprite != null)
        {
            float ragePulse = (Mathf.Sin(Time.time * rageFlashSpeed * 1.5f) + 1f) * 0.5f;
            Color rageGlowColor = Color.Lerp(rageColor1, rageColor2, ragePulse);
            outerGlowSprite.color = new Color(rageGlowColor.r, rageGlowColor.g, rageGlowColor.b, Mathf.Lerp(0.1f, 0.3f, ragePulse));
        }

        // %0.4 sans kivilcim (nadir, incelikli)
        if (Random.value < 0.004f && ParticleManager.Instance != null)
        {
            Vector3 sparkPos = transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.5f, 0.8f), 0f);
            ParticleManager.Instance.PlayNeonGlow(sparkPos, rageFlash, 0.3f);
        }
    }

    /// <summary>
    /// Rage mode aktifse cooldown degerini azaltir.
    /// </summary>
    private float GetEffectiveCooldown(float baseCooldown)
    {
        if (isRageActive)
        {
            return baseCooldown * rageCooldownMultiplier;
        }
        return baseCooldown;
    }

    #endregion

    #region Item Drop System

    /// <summary>
    /// Olum aninda esya ve silah dusurme sistemi.
    /// Enemy.cs ile ayni TryDropItem desenini kullanir.
    /// WeaponDrop.SpawnDrop, AmmoPickup.Spawn, CollectibleItem.Spawn kullanir.
    /// </summary>
    private void TryDropItem()
    {
        Vector3 dropPos = transform.position + Vector3.up * 0.5f;

        // Once silah dusurme sansini kontrol et
        if (canDropWeapons && Random.value <= weaponDropChance)
        {
            WeaponType[] possibleWeapons = new WeaponType[]
            {
                WeaponType.Rifle,
                WeaponType.Shotgun,
                WeaponType.SMG,
                WeaponType.Sniper,
                WeaponType.RocketLauncher,
                WeaponType.GrenadeLauncher
            };

            WeaponType weaponType = possibleWeapons[Random.Range(0, possibleWeapons.Length)];
            WeaponRarity rarity = WeaponRarityHelper.GetRandomRarity();

            WeaponDrop.SpawnDrop(dropPos, weaponType, rarity);
            return;
        }

        // Normal esya dusurme
        if (!canDropItems) return;
        if (Random.value > dropChance) return;

        // Rastgele bir esya turu sec (%50 mermi, %50 diger esyalar)
        if (Random.value < 0.5f)
        {
            // Mermi dusur
            AmmoPickup.Spawn(dropPos);
        }
        else
        {
            // Diger esyalar
            ItemType[] possibleDrops = new ItemType[]
            {
                ItemType.HealthPotion,
                ItemType.Shield,
                ItemType.SpeedBoost,
                ItemType.Bomb
            };

            ItemType dropType = possibleDrops[Random.Range(0, possibleDrops.Length)];
            CollectibleItem.Spawn(dropType, dropPos);
        }
    }

    #endregion

    #region Animation & Visuals

    private void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool(walkingParam, isWalking);
        }
    }

    /// <summary>
    /// FIX #2: spriteRenderer.flipX kullanarak yon degistirir.
    /// transform.localScale'e dokunmaz (kullanici tarafindan 3.8633 olarak ayarlanmis).
    /// Sprite dogal olarak SOLA bakar (flipX=false iken sol yon).
    /// Saga bakmasi icin flipX=true olmali.
    /// </summary>
    private void UpdateFacingDirection()
    {
        if (spriteRenderer == null) return;

        float direction;

        if (playerDetected && player != null)
        {
            // Oyuncuya bak
            direction = player.position.x > transform.position.x ? 1f : -1f;
        }
        else
        {
            // Hareket yonune bak
            direction = movingRight ? 1f : -1f;
        }

        // Sprite dogal olarak sola bakar: saga bakmak icin flipX=true
        spriteRenderer.flipX = direction > 0f;
    }

    /// <summary>
    /// FIX #2: Robotun baktigi yonu doner. flipX=true ise +1 (sag), flipX=false ise -1 (sol).
    /// </summary>
    private float GetFacingDirection()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX ? 1f : -1f;
        }
        return movingRight ? 1f : -1f;
    }

    private void SetupLaserLineRenderer()
    {
        GameObject laserObj = new GameObject("LaserBeam");
        laserObj.transform.SetParent(transform);
        laserObj.transform.localPosition = Vector3.zero;

        laserLineRenderer = laserObj.AddComponent<LineRenderer>();
        laserLineRenderer.positionCount = 2;
        laserLineRenderer.startWidth = laserWidth;
        laserLineRenderer.endWidth = laserWidth * 0.5f;

        // Material
        laserLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        laserLineRenderer.startColor = laserColor;
        laserLineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);

        laserLineRenderer.sortingOrder = 10;
        laserLineRenderer.enabled = false;
    }

    #endregion

    #region Enhanced Visual Setup

    /// <summary>
    /// Dis glow katmani - robotun arkasinda buyuk, yari saydam daire
    /// </summary>
    private void SetupOuterGlow()
    {
        GameObject glowObj = new GameObject("OuterGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * outerGlowScale;

        outerGlowSprite = glowObj.AddComponent<SpriteRenderer>();
        outerGlowSprite.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : 4;

        Texture2D glowTex = CreateFilledCircleTexture(64, Color.white);
        outerGlowSprite.sprite = Sprite.Create(glowTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32);
        outerGlowSprite.color = new Color(glowColor.r, glowColor.g, glowColor.b, outerGlowMaxAlpha * 0.5f);
    }

    /// <summary>
    /// Dolu daire texture olusturur (glow efektleri icin)
    /// </summary>
    private Texture2D CreateFilledCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        Color transparent = Color.clear;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                if (dist < radius)
                {
                    float fade = 1f - (dist / radius);
                    fade = fade * fade; // Quadratic falloff
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, fade));
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Goz glow efekti - iki kucuk parlak nokta
    /// </summary>
    private void SetupEyeGlows()
    {
        Texture2D eyeTex = CreateFilledCircleTexture(16, Color.white);
        Sprite eyeSprite = Sprite.Create(eyeTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);

        int eyeOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 8;

        // Sol goz
        GameObject leftEye = new GameObject("LeftEyeGlow");
        leftEye.transform.SetParent(transform);
        leftEye.transform.localPosition = new Vector3(-0.15f, 0.2f, 0f);
        leftEye.transform.localScale = Vector3.one * 0.15f;
        leftEyeGlow = leftEye.AddComponent<SpriteRenderer>();
        leftEyeGlow.sprite = eyeSprite;
        leftEyeGlow.color = new Color(0f, 1f, 1f, 0.9f);
        leftEyeGlow.sortingOrder = eyeOrder;

        // Sag goz
        GameObject rightEye = new GameObject("RightEyeGlow");
        rightEye.transform.SetParent(transform);
        rightEye.transform.localPosition = new Vector3(0.15f, 0.2f, 0f);
        rightEye.transform.localScale = Vector3.one * 0.15f;
        rightEyeGlow = rightEye.AddComponent<SpriteRenderer>();
        rightEyeGlow.sprite = eyeSprite;
        rightEyeGlow.color = new Color(0f, 1f, 1f, 0.9f);
        rightEyeGlow.sortingOrder = eyeOrder;
    }

    /// <summary>
    /// Gelismis lazer glow renderer - cekirdek isinin etrafinda genis saydam katman
    /// </summary>
    private void SetupLaserGlowRenderer()
    {
        GameObject laserGlowObj = new GameObject("LaserGlow");
        laserGlowObj.transform.SetParent(transform);
        laserGlowObj.transform.localPosition = Vector3.zero;

        laserGlowRenderer = laserGlowObj.AddComponent<LineRenderer>();
        laserGlowRenderer.positionCount = 2;
        laserGlowRenderer.startWidth = laserWidth * 4f;
        laserGlowRenderer.endWidth = laserWidth * 2f;
        laserGlowRenderer.material = new Material(Shader.Find("Sprites/Default"));
        laserGlowRenderer.startColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.2f);
        laserGlowRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.05f);
        laserGlowRenderer.sortingOrder = 9;
        laserGlowRenderer.enabled = false;
    }

    #endregion

    #region Neon Visual Effects

    /// <summary>
    /// Robotun etrafinda nabiz atan neon glow efekti uygular.
    /// Enemy.cs CodeEffects deseninden esinlenmistir.
    /// Rage mode aktifken bu efekt devre disi kalir (rage gorunumu oncelikli).
    /// </summary>
    private void UpdateGlowEffect()
    {
        if (spriteRenderer == null) return;

        glowTimer += Time.deltaTime;

        float intensity = (Mathf.Sin(glowTimer * glowPulseSpeed) + 1f) * 0.5f;
        spriteRenderer.color = Color.Lerp(originalColor, glowColor, intensity * glowIntensity);

        // Outer glow pulse (ana glow'dan yavas, offset)
        if (outerGlowSprite != null)
        {
            float outerPulse = (Mathf.Sin(glowTimer * glowPulseSpeed * 0.6f + 1.5f) + 1f) * 0.5f;
            float outerAlpha = Mathf.Lerp(outerGlowMaxAlpha * 0.3f, outerGlowMaxAlpha, outerPulse);
            outerGlowSprite.color = new Color(glowColor.r, glowColor.g, glowColor.b, outerAlpha);
        }

        // Eye glow pulse
        UpdateEyeGlow();

        // Ambient sparks
        UpdateAmbientSparks();
    }

    /// <summary>
    /// Goz glow efektini gunceller - pulse eden alfa/boyut, rage modda kirmizi
    /// </summary>
    private void UpdateEyeGlow()
    {
        if (leftEyeGlow == null || rightEyeGlow == null) return;

        float eyePulse = (Mathf.Sin(glowTimer * glowPulseSpeed * 1.5f) + 1f) * 0.5f;
        float eyeAlpha = Mathf.Lerp(0.6f, 1f, eyePulse);
        float eyeScale = Mathf.Lerp(0.12f, 0.18f, eyePulse);

        Color eyeColor = isRageActive ? new Color(1f, 0.1f, 0f, eyeAlpha) : new Color(0f, 1f, 1f, eyeAlpha);

        leftEyeGlow.color = eyeColor;
        rightEyeGlow.color = eyeColor;
        leftEyeGlow.transform.localScale = Vector3.one * eyeScale;
        rightEyeGlow.transform.localScale = Vector3.one * eyeScale;

        // Sprite flip'e gore pozisyon guncelle
        bool facingRight = spriteRenderer != null && spriteRenderer.flipX;
        float eyeXOffset = facingRight ? 1f : -1f;
        leftEyeGlow.transform.localPosition = new Vector3(-0.15f * eyeXOffset, 0.2f, 0f);
        rightEyeGlow.transform.localPosition = new Vector3(0.15f * eyeXOffset, 0.2f, 0f);
    }

    /// <summary>
    /// Bosta iken rastgele elektrik kivilcimlari
    /// </summary>
    private void UpdateAmbientSparks()
    {
        // Sadece bosta/yururken, dash/slam sirasinda degil
        if (isDashing || isGroundSlamming || isAttacking) return;

        sparkTimer += Time.deltaTime;
        if (sparkTimer >= sparkInterval)
        {
            sparkTimer = 0f;
            if (ParticleManager.Instance != null)
            {
                Vector3 sparkPos = transform.position + new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-0.3f, 0.5f),
                    0f
                );
                ParticleManager.Instance.PlayNeonGlow(sparkPos, glowColor, 0.3f);
            }
        }
    }

    /// <summary>
    /// Saldiri sirasinda kisa bir flash efekti tetikler.
    /// Sprite rengi gecici olarak attackFlashColor'a degisir.
    /// </summary>
    private void TriggerAttackFlash()
    {
        TriggerAttackFlash(attackFlashColor);
    }

    private void TriggerAttackFlash(Color flashColor)
    {
        if (spriteRenderer == null) return;

        if (attackFlashCoroutine != null)
        {
            StopCoroutine(attackFlashCoroutine);
        }

        attackFlashCoroutine = StartCoroutine(AttackFlashEffect(flashColor));
    }

    private IEnumerator AttackFlashEffect(Color flashColor)
    {
        if (spriteRenderer == null) yield break;

        Color beforeColor = spriteRenderer.color;
        spriteRenderer.color = flashColor;

        // Gozleri de flash et
        if (leftEyeGlow != null) leftEyeGlow.color = new Color(flashColor.r, flashColor.g, flashColor.b, 1f);
        if (rightEyeGlow != null) rightEyeGlow.color = new Color(flashColor.r, flashColor.g, flashColor.b, 1f);

        yield return new WaitForSeconds(attackFlashDuration);

        if (!isRageActive)
        {
            spriteRenderer.color = beforeColor;
        }

        attackFlashCoroutine = null;
    }

    #endregion

    #region Cooldowns

    private void UpdateCooldowns()
    {
        if (projectileCooldownTimer > 0f)
            projectileCooldownTimer -= Time.deltaTime;

        if (laserCooldownTimer > 0f)
            laserCooldownTimer -= Time.deltaTime;

        if (meleeCooldownTimer > 0f)
            meleeCooldownTimer -= Time.deltaTime;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (bombCooldownTimer > 0f)
            bombCooldownTimer -= Time.deltaTime;

        if (rocketCooldownTimer > 0f)
            rocketCooldownTimer -= Time.deltaTime;
    }

    #endregion

    #region Death Override

    /// <summary>
    /// FIX #1: base.Die() dogrudan cagrilmaz. base.Die() kendi FadeDeathAnimation coroutine'ini
    /// baslatir (0.3 saniyede yok eder) ve bu, RobotEnemy'nin WaitForDeathAnimation (1.5s) ile catisir.
    /// Bunun yerine base.Die()'in yaptigi kritik islemler manuel olarak yapilir:
    /// isDead=true, collider disable, kinematic, event'ler, ses, particle.
    /// Sonra sadece kendi olum animasyonumuz kullanilir.
    /// </summary>
    public override void Die()
    {
        if (isDead) return;

        // Saldiriyi durdur
        isAttacking = false;
        isLaserActive = false;
        isDashing = false;
        isTeleporting = false;
        isGroundSlamming = false;
        isComboActive = false;
        isCounterAttacking = false;
        StopAllCoroutines();

        // Drone'lari yok et
        foreach (var drone in activeDrones)
        {
            if (drone != null)
            {
                Destroy(drone);
            }
        }
        activeDrones.Clear();

        // Lazeri kapat
        if (laserLineRenderer != null)
        {
            laserLineRenderer.enabled = false;
        }

        // Dash trail'i kapat
        if (dashTrailRenderer != null)
        {
            dashTrailRenderer.enabled = false;
        }

        // Shield'i kapat
        if (shieldVisual != null)
        {
            shieldVisual.enabled = false;
        }

        // Rage mode'u kapat
        if (isRageActive)
        {
            DeactivateRageMode();
        }

        // Olum animasyonu tetikle
        if (animator != null)
        {
            animator.SetTrigger(dieTrigger);
            animator.SetBool(walkingParam, false);
            animator.SetBool(rageParam, false);
        }

        // Esya dusur (isDead=true olmadan once, cunku TryDropItem'in calismasi lazim)
        TryDropItem();

        // Equipment ve malzeme dusurme
        if (canDropItems && InventoryManager.Instance != null)
        {
            float effectiveDropChance = dropChance;
            // Zorluk carpanina gore drop orani artisi
            if (useDifficultyScaling)
            {
                effectiveDropChance *= (1f + (difficultyMultiplier - 1f) * 0.5f);
            }
            // Equipment drop rate bonus
            if (EquipmentManager.Instance != null)
            {
                effectiveDropChance *= (1f + EquipmentManager.Instance.TotalDropRateBonus);
            }

            if (Random.value < effectiveDropChance)
            {
                // Rastgele equipment veya malzeme dusur
                if (Random.value < 0.4f)
                {
                    // Equipment dusur
                    ItemType[] equipmentTypes = {
                        ItemType.DamageBooster, ItemType.SpeedRing, ItemType.FireRateModule,
                        ItemType.MagnetCore, ItemType.LuckyCharm, ItemType.ShieldGenerator,
                        ItemType.CriticalLens, ItemType.VampireFangs, ItemType.ReflectShield
                    };
                    ItemType dropType = equipmentTypes[Random.Range(0, equipmentTypes.Length)];
                    InventoryManager.Instance.AddItem(dropType, 1);

                    if (NotificationManager.Instance != null)
                    {
                        string itemName = InventoryItem.Create(dropType).name;
                        NotificationManager.Instance.ShowNotification("ESYA DUSTU!", itemName, NotificationType.ItemPickup);
                    }
                }
                else
                {
                    // Malzeme dusur - zorluga gore kalite artar
                    ItemType materialType;
                    int materialCount;

                    if (difficultyMultiplier >= 2.5f)
                    {
                        materialType = ItemType.PlasmaCore;
                        materialCount = 1;
                    }
                    else if (difficultyMultiplier >= 2f)
                    {
                        materialType = ItemType.VoidEssence;
                        materialCount = Random.Range(1, 3);
                    }
                    else if (difficultyMultiplier >= 1.5f)
                    {
                        materialType = ItemType.NeonCrystal;
                        materialCount = Random.Range(1, 4);
                    }
                    else
                    {
                        materialType = ItemType.ScrapMetal;
                        materialCount = Random.Range(2, 6);
                    }

                    InventoryManager.Instance.AddItem(materialType, materialCount);

                    if (NotificationManager.Instance != null)
                    {
                        string matName = InventoryItem.Create(materialType).name;
                        NotificationManager.Instance.ShowNotification("MALZEME!", $"{materialCount}x {matName}", NotificationType.Info);
                    }
                }
            }
        }

        // --- FIX #1: base.Die()'in erken destroy sorununu coz ---
        // base.Die() cagrilmali cunku OnEnemyDeath event'i sadece EnemyBase icinden invoke edilebilir.
        // Sorun: base.Die() FadeDeathAnimation baslatir ve deathFadeDuration (0.3s) sonra destroy eder.
        // Cozum: deathFadeDuration'i cok buyuk yaparak base'in animasyonunun bitmesini onle.
        // Bizim WaitForDeathAnimation (1.5s + fade) Destroy'u cagirdiginda zaten her sey temizlenir.
        useSquashDeath = false;
        deathFadeDuration = 999f; // base.Die()'in FadeDeathAnimation'i asla tamamlanmayacak

        base.Die();

        // base.Die() isDead=true yapar, collider kapatir, kinematic yapar, event'leri atesler,
        // ses calar, particle spawn eder ve FadeDeathAnimation baslatir (ama 999s surececek).
        // --- FIX #1 SONU ---

        // Animasyonun bitmesini bekleyip yok et (kendi olum coroutine'imiz)
        StartCoroutine(WaitForDeathAnimation());
    }

    private IEnumerator WaitForDeathAnimation()
    {
        // Olum animasyonu suresi (animasyona gore ayarla)
        yield return new WaitForSeconds(1.5f);

        // Fade out (deathFadeDuration 999f yapildi, kendi fade suresi kullan)
        float customFadeDuration = 0.5f;
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;

            while (elapsed < customFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / customFadeDuration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;

        // Patrol siniri
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos + Vector3.left * patrolLeftDistance, pos + Vector3.right * patrolRightDistance);
        Gizmos.DrawWireSphere(pos + Vector3.left * patrolLeftDistance, 0.2f);
        Gizmos.DrawWireSphere(pos + Vector3.right * patrolRightDistance, 0.2f);

        // Algilama menzili
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Uzak mesafe (mermi)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, longRangeThreshold);

        // Orta mesafe (lazer)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, midRangeThreshold);

        // Dash menzili (yakin-orta)
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, dashMinRange);
        Gizmos.DrawWireSphere(transform.position, dashMaxRange);

        // Melee saldiri alani
        Gizmos.color = Color.red;
        float direction = Application.isPlaying ? GetFacingDirection() : 1f;
        Vector3 meleeCenter = transform.position + new Vector3(meleeAttackOffset.x * direction, meleeAttackOffset.y, 0f);
        Gizmos.DrawWireCube(meleeCenter, new Vector3(meleeAttackSize.x, meleeAttackSize.y, 0.1f));

        // Shield (aktifse)
        if (shieldEnabled)
        {
            Gizmos.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 0.8f);
        }

        // Ground & Edge detection gizmos
        float gizmoFacing = Application.isPlaying ? GetFacingDirection() : 1f;
        Vector3 groundOrigin = transform.position + new Vector3(0f, groundCheckYOffset, 0f);

        // Ground check ray (yesil)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundCheckDistance);

        // Edge check ray (turuncu)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector3 edgeOrigin = groundOrigin + new Vector3(gizmoFacing * edgeCheckForwardDistance, 0f, 0f);
        Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * edgeCheckDownDistance);

        // Wall check ray (kirmizi)
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawLine(groundOrigin, groundOrigin + new Vector3(gizmoFacing * wallCheckDistance, 0f, 0f));

        // Teleport menzili (mor)
        if (canTeleport)
        {
            Gizmos.color = new Color(0.8f, 0f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, teleportRange);
        }

        // Ground slam menzili (turuncu)
        if (canGroundSlam)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, groundSlamRadius);
        }

        // Counter attack menzili (sari)
        if (canCounterAttack)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, counterAttackRange);
        }

        // Platform detection (ziplama icin)
        if (canJump)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * (0.5f + platformDetectionRange));
        }

        // Phase gostergesi
        if (Application.isPlaying && usePhaseSystem)
        {
            Gizmos.color = currentPhase == 3 ? Color.red : (currentPhase == 2 ? Color.yellow : Color.green);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(0.3f * currentPhase, 0.1f, 0.1f));
        }
    }

    #endregion

    #region Cleanup

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Boss health bar event'lerini temizle
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= OnBossHealthChanged;
            enemyHealth.OnDeath -= OnBossDeath;
        }

        // Boss bar'i gizle
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.HideBoss();
        }
    }

    #endregion
}

/// <summary>
/// Robot drone'unun basit AI'i.
/// Oyuncuya dogru hareket eder ve temas halinde hasar verir.
/// </summary>
public class RobotDroneAI : MonoBehaviour
{
    private Transform player;
    private RobotEnemy parentRobot;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private float moveSpeed = 4f;
    private float orbitRadius = 2f;
    private float orbitSpeed = 3f;
    private float shootCooldown = 2f;
    private float shootTimer = 0f;

    private bool isOrbitingPlayer = false;
    private float orbitAngle = 0f;

    public void Initialize(Transform target, RobotEnemy parent)
    {
        player = target;
        parentRobot = parent;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Rastgele orbitAngle
        orbitAngle = Random.Range(0f, 360f);
    }

    void Update()
    {
        if (player == null)
        {
            // Oyuncu yoksa kendini yok et
            Destroy(gameObject);
            return;
        }

        // Oyuncuya yakin mi?
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer < orbitRadius * 1.5f)
        {
            // Orbit modu
            OrbitAroundPlayer();
        }
        else
        {
            // Yaklas
            MoveTowardsPlayer();
        }

        // Ates et
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f && distToPlayer < 8f)
        {
            ShootAtPlayer();
            shootTimer = shootCooldown;
        }

        // Glow efekti
        if (sr != null)
        {
            float glow = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            sr.color = Color.Lerp(Color.cyan, Color.white, glow * 0.3f);
        }
    }

    private void MoveTowardsPlayer()
    {
        if (rb == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void OrbitAroundPlayer()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;

        Vector2 targetPos = (Vector2)player.position + new Vector2(
            Mathf.Cos(orbitAngle) * orbitRadius,
            Mathf.Sin(orbitAngle) * orbitRadius
        );

        if (rb != null)
        {
            Vector2 toTarget = targetPos - (Vector2)transform.position;
            rb.linearVelocity = toTarget.normalized * moveSpeed * 1.5f;
        }
    }

    private void ShootAtPlayer()
    {
        if (player == null) return;

        // Drone mermisi olustur - enerji topu seklinde
        GameObject projectile = new GameObject("DroneProjectile");
        projectile.transform.position = transform.position;
        projectile.tag = "EnemyProjectile";

        SpriteRenderer projSr = projectile.AddComponent<SpriteRenderer>();
        projSr.color = new Color(1f, 0.3f, 0.3f);
        projSr.sortingOrder = 5;

        // Enerji topu sprite - yumusak kenarli daire
        projSr.sprite = CreateEnergyBallSprite();
        projectile.transform.localScale = Vector3.one * 0.4f;

        // Glow efekti
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(projectile.transform);
        glow.transform.localPosition = Vector3.zero;
        SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = projSr.sprite;
        glowSr.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        glowSr.sortingOrder = 4;
        glow.transform.localScale = Vector3.one * 1.8f;

        // Trail efekti
        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        trail.endColor = new Color(1f, 0.1f, 0.1f, 0f);
        trail.sortingOrder = 3;

        CircleCollider2D col = projectile.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;
        col.isTrigger = true;

        Rigidbody2D projRb = projectile.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0f;

        Vector2 direction = (player.position - transform.position).normalized;
        projRb.linearVelocity = direction * 8f;

        // Projectile script
        Projectile proj = projectile.AddComponent<Projectile>();
        proj.damage = 1;
        proj.speed = 8f;
        proj.isPlayerBullet = false;

        Destroy(projectile, 5f);
    }

    /// <summary>
    /// Enerji topu sprite'i olusturur - yumusak kenarli parlak daire
    /// </summary>
    private Sprite CreateEnergyBallSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float radius = size / 2f - 1f;

                if (dist < radius)
                {
                    // Merkezden kenara dogru solma
                    float t = dist / radius;
                    float alpha = 1f - (t * t); // Smooth falloff
                    float brightness = 1f - (t * 0.3f);
                    tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage();
            }
        }

        // PlayerBullet ile vurulursa ol
        if (other.CompareTag("PlayerBullet"))
        {
            // Drone patlama efekti
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayDroneExplosion(transform.position);
            }

            // Ses efekti
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyDeath();
            }

            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Robot mermisi - neon glow efekti ve yanip sonen animasyon
/// </summary>
public class RobotBullet : MonoBehaviour
{
    public int damage = 1;
    public Color glowColor = new Color(1f, 0.5f, 0f, 1f);
    public float glowPulseSpeed = 8f;

    private SpriteRenderer sr;
    private SpriteRenderer glowSr;
    private float pulseTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Transform glowTransform = transform.Find("Glow");
        if (glowTransform != null)
        {
            glowSr = glowTransform.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        // Glow pulse efekti
        pulseTimer += Time.deltaTime * glowPulseSpeed;
        float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;

        if (glowSr != null)
        {
            float alpha = 0.2f + pulse * 0.3f;
            glowSr.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
            glowSr.transform.localScale = Vector3.one * (1.3f + pulse * 0.4f);
        }

        // Ana mermi de hafif parlaklik
        if (sr != null)
        {
            float brightness = 0.8f + pulse * 0.2f;
            sr.color = new Color(glowColor.r * brightness, glowColor.g * brightness, glowColor.b * brightness, 1f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage();
            }

            // Carpma efekti
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayDamageEffect(transform.position);
            }

            Destroy(gameObject);
        }
        else if (!other.isTrigger && !other.CompareTag("Enemy") && !other.CompareTag("EnemyProjectile"))
        {
            // Zemine veya duvara carpti
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.PlayDamageEffect(transform.position);
            }
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Robot bombasi - parabolik ucus, fitil efekti, alan hasari patlama
/// </summary>
public class RobotBomb : MonoBehaviour
{
    public int damage = 3;
    public float explosionRadius = 3f;
    public float fuseTime = 1.5f;
    public Color explosionColor = new Color(1f, 0.3f, 0f, 1f);

    private SpriteRenderer sr;
    private Transform fuseTransform;
    private float timer = 0f;
    private bool hasExploded = false;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalColor = sr.color;
        }

        fuseTransform = transform.Find("Fuse");
    }

    void Update()
    {
        if (hasExploded) return;

        timer += Time.deltaTime;

        // Fitil yanip sonme efekti - patlama yaklastikca hizlanir
        float flashSpeed = 2f + (timer / fuseTime) * 10f;
        float flash = (Mathf.Sin(timer * flashSpeed) + 1f) * 0.5f;

        if (sr != null)
        {
            // Patlama yaklastikca kirmiziya don
            Color targetColor = Color.Lerp(originalColor, Color.red, timer / fuseTime);
            sr.color = Color.Lerp(targetColor, Color.white, flash * 0.5f);
        }

        // Fitil efekti
        if (fuseTransform != null)
        {
            SpriteRenderer fuseSr = fuseTransform.GetComponent<SpriteRenderer>();
            if (fuseSr != null)
            {
                fuseSr.color = flash > 0.5f ? Color.yellow : Color.red;
            }
        }

        // Patlama zamani
        if (timer >= fuseTime)
        {
            Explode();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Zemine carpinca biraz bekle, sonra patla
        // veya direkt patlatmak icin:
        // Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Alan hasari
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController player = hit.GetComponent<PlayerController>();
                if (player != null)
                {
                    // Mesafeye gore hasar (merkeze yakin = daha cok hasar)
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float damageMultiplier = 1f - (dist / explosionRadius);

                    int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));
                    for (int i = 0; i < actualDamage; i++)
                    {
                        player.TakeDamage();
                    }

                    // Patlama knockback
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                        knockbackDir.y = 0.5f;
                        float knockbackForce = 10f * damageMultiplier;
                        playerRb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }
        }

        // Patlama efekti
        CreateExplosionEffect();

        // Ekran sarsmasi
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnCombo(3);
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDeath(); // Patlama sesi
        }

        Destroy(gameObject);
    }

    void CreateExplosionEffect()
    {
        // Yeni gelismis particle efekti
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayRobotBombExplosion(transform.position, explosionRadius, explosionColor);
        }

        // Ek olarak patlama dairesi sprite efekti
        GameObject explosion = new GameObject("ExplosionRing");
        explosion.transform.position = transform.position;

        SpriteRenderer expSr = explosion.AddComponent<SpriteRenderer>();
        expSr.sortingOrder = 15;

        // Patlama halkasi sprite - ici bos daire
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;
        float innerRadius = size * 0.35f;
        float outerRadius = size * 0.48f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist >= innerRadius && dist <= outerRadius)
                {
                    // Halka icinde - merkezden uzaklasinca solma
                    float t = (dist - innerRadius) / (outerRadius - innerRadius);
                    float alpha = Mathf.Sin(t * Mathf.PI); // Ortada en parlak
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else if (dist < innerRadius)
                {
                    // Ic kisim - hafif parlaklik
                    float alpha = 0.3f * (1f - dist / innerRadius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        expSr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16);
        expSr.color = explosionColor;

        // Animasyonlu buyume ve solma
        explosion.AddComponent<ExplosionEffect>().Setup(explosionRadius * 2.5f, 0.4f);
    }
}

/// <summary>
/// Patlama efekti animasyonu
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    private float targetSize = 2f;
    private float duration = 0.3f;
    private float timer = 0f;
    private SpriteRenderer sr;

    public void Setup(float size, float dur)
    {
        targetSize = size;
        duration = dur;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Hizli buyume, yavas solma
        float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * targetSize;
        transform.localScale = Vector3.one * scale;

        if (sr != null)
        {
            float alpha = 1f - t;
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}

/// <summary>
/// Takipli roket - oyuncuyu izler, patlama efekti
/// </summary>
public class RobotRocket : MonoBehaviour
{
    public Transform target;
    public int damage = 2;
    public float speed = 6f;
    public float turnSpeed = 180f;
    public float trackingDuration = 3f;
    public float explosionRadius = 2f;

    private Rigidbody2D rb;
    private float trackingTimer = 0f;
    private bool isTracking = true;
    private bool hasExploded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (hasExploded) return;

        // Izleme suresi kontrolu
        trackingTimer += Time.deltaTime;
        if (trackingTimer >= trackingDuration)
        {
            isTracking = false;
        }

        // Hedefe dogru don
        if (isTracking && target != null)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;

            // Yumusak donus
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        // Ileri hareket
        if (rb != null)
        {
            Vector2 forward = transform.right;
            rb.linearVelocity = forward * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        // Oyuncuya veya zemine carpinca patla
        if (other.CompareTag("Player") || (!other.isTrigger && !other.CompareTag("Enemy") && !other.CompareTag("EnemyProjectile")))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Alan hasari
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerController player = hit.GetComponent<PlayerController>();
                if (player != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float damageMultiplier = 1f - (dist / explosionRadius) * 0.5f;

                    int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));
                    for (int i = 0; i < actualDamage; i++)
                    {
                        player.TakeDamage();
                    }

                    // Knockback
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                        knockbackDir.y = 0.4f;
                        playerRb.AddForce(knockbackDir.normalized * 8f, ForceMode2D.Impulse);
                    }
                }
            }
        }

        // Gelismis patlama efektleri
        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayRobotRocketExplosion(transform.position, new Color(1f, 0.5f, 0f));
        }

        // Ekran sarsmasi
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeOnCombo(2);
        }

        // Ses
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDeath();
        }

        Destroy(gameObject);
    }
}
