using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pixel Adventure asset pack'ini yonetir ve sprite'lari yukler.
/// Ticari kullanim icin uygundur (CC0 lisansi).
/// Asset kaynak: https://pixelfrog-assets.itch.io/pixel-adventure-1
/// </summary>
public class PixelAdventureAssets : MonoBehaviour
{
    public static PixelAdventureAssets Instance { get; private set; }

    [Header("Main Character Sprites")]
    public Sprite[] playerIdle;
    public Sprite[] playerRun;
    public Sprite[] playerJump;
    public Sprite[] playerFall;
    public Sprite[] playerDoubleJump;
    public Sprite[] playerHit;

    [Header("Enemy Sprites")]
    public Sprite[] slimeIdle;
    public Sprite[] slimeRun;
    public Sprite[] slimeHit;
    public Sprite[] mushroomIdle;
    public Sprite[] mushroomRun;
    public Sprite[] ghostIdle;
    public Sprite[] ghostHit;
    public Sprite[] batFly;
    public Sprite[] batHit;

    [Header("Items")]
    public Sprite[] fruits; // Apple, Banana, Cherry, etc.
    public Sprite[] collectEffect;
    public Sprite checkpoint;
    public Sprite checkpointFlag;
    public Sprite[] boxIdle;
    public Sprite[] boxBreak;

    [Header("Traps")]
    public Sprite[] sawOn;
    public Sprite[] spikeHead;
    public Sprite spike;
    public Sprite[] fire;
    public Sprite[] fallingPlatform;

    [Header("Terrain Tiles")]
    public Sprite terrainTopLeft;
    public Sprite terrainTop;
    public Sprite terrainTopRight;
    public Sprite terrainLeft;
    public Sprite terrainCenter;
    public Sprite terrainRight;
    public Sprite terrainBottomLeft;
    public Sprite terrainBottom;
    public Sprite terrainBottomRight;

    [Header("Background")]
    public Sprite backgroundLayer1; // En uzak
    public Sprite backgroundLayer2;
    public Sprite backgroundLayer3; // En yakin

    [Header("Settings")]
    public string characterType = "MaskDude"; // MaskDude, NinjaFrog, PinkMan, VirtualGuy

    private Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();

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

    void Start()
    {
        LoadAllSprites();
    }

    void LoadAllSprites()
    {
        // Resources klasorunden sprite'lari yukle
        LoadPlayerSprites();
        LoadEnemySprites();
        LoadItemSprites();
        LoadTrapSprites();
        LoadTerrainSprites();
        LoadBackgroundSprites();

        Debug.Log("Pixel Adventure assets yuklendi!");
    }

    void LoadPlayerSprites()
    {
        string basePath = "PixelAdventure/MainCharacters/" + characterType;

        playerIdle = Resources.LoadAll<Sprite>(basePath + "/Idle");
        playerRun = Resources.LoadAll<Sprite>(basePath + "/Run");
        playerJump = Resources.LoadAll<Sprite>(basePath + "/Jump");
        playerFall = Resources.LoadAll<Sprite>(basePath + "/Fall");
        playerDoubleJump = Resources.LoadAll<Sprite>(basePath + "/DoubleJump");
        playerHit = Resources.LoadAll<Sprite>(basePath + "/Hit");

        // Eger bulunamazsa varsayilan olustur
        if (playerIdle == null || playerIdle.Length == 0)
        {
            Debug.LogWarning($"Player sprites bulunamadi: {basePath}. Varsayilan kullaniliyor.");
        }
    }

    void LoadEnemySprites()
    {
        string basePath = "PixelAdventure/Enemies";

        // Slime
        slimeIdle = Resources.LoadAll<Sprite>(basePath + "/Slime/Idle");
        slimeRun = Resources.LoadAll<Sprite>(basePath + "/Slime/Run");
        slimeHit = Resources.LoadAll<Sprite>(basePath + "/Slime/Hit");

        // Mushroom
        mushroomIdle = Resources.LoadAll<Sprite>(basePath + "/Mushroom/Idle");
        mushroomRun = Resources.LoadAll<Sprite>(basePath + "/Mushroom/Run");

        // Ghost
        ghostIdle = Resources.LoadAll<Sprite>(basePath + "/Ghost/Idle");
        ghostHit = Resources.LoadAll<Sprite>(basePath + "/Ghost/Hit");

        // Bat
        batFly = Resources.LoadAll<Sprite>(basePath + "/Bat/Fly");
        batHit = Resources.LoadAll<Sprite>(basePath + "/Bat/Hit");
    }

    void LoadItemSprites()
    {
        string basePath = "PixelAdventure/Items";

        fruits = Resources.LoadAll<Sprite>(basePath + "/Fruits");
        collectEffect = Resources.LoadAll<Sprite>(basePath + "/CollectEffect");

        Sprite[] checkpointSprites = Resources.LoadAll<Sprite>(basePath + "/Checkpoint");
        if (checkpointSprites != null && checkpointSprites.Length > 0)
        {
            checkpoint = checkpointSprites[0];
            if (checkpointSprites.Length > 1)
                checkpointFlag = checkpointSprites[1];
        }

        boxIdle = Resources.LoadAll<Sprite>(basePath + "/Boxes/Idle");
        boxBreak = Resources.LoadAll<Sprite>(basePath + "/Boxes/Break");
    }

    void LoadTrapSprites()
    {
        string basePath = "PixelAdventure/Traps";

        sawOn = Resources.LoadAll<Sprite>(basePath + "/Saw/On");
        spikeHead = Resources.LoadAll<Sprite>(basePath + "/SpikeHead");
        fire = Resources.LoadAll<Sprite>(basePath + "/Fire");
        fallingPlatform = Resources.LoadAll<Sprite>(basePath + "/FallingPlatform");

        Sprite[] spikeSprites = Resources.LoadAll<Sprite>(basePath + "/Spike");
        if (spikeSprites != null && spikeSprites.Length > 0)
            spike = spikeSprites[0];
    }

    void LoadTerrainSprites()
    {
        string basePath = "PixelAdventure/Terrain";

        Sprite[] terrainSprites = Resources.LoadAll<Sprite>(basePath + "/Terrain");

        // Sprite atlas'tan parcalari al (16x16 grid varsayimi)
        if (terrainSprites != null && terrainSprites.Length >= 9)
        {
            // Atlas sirasina gore
            terrainTopLeft = terrainSprites[0];
            terrainTop = terrainSprites[1];
            terrainTopRight = terrainSprites[2];
            terrainLeft = terrainSprites[3];
            terrainCenter = terrainSprites[4];
            terrainRight = terrainSprites[5];
            terrainBottomLeft = terrainSprites[6];
            terrainBottom = terrainSprites[7];
            terrainBottomRight = terrainSprites[8];
        }
    }

    void LoadBackgroundSprites()
    {
        string basePath = "PixelAdventure/Background";

        Sprite[] bgSprites = Resources.LoadAll<Sprite>(basePath);
        if (bgSprites != null && bgSprites.Length >= 3)
        {
            backgroundLayer1 = bgSprites[0];
            backgroundLayer2 = bgSprites[1];
            backgroundLayer3 = bgSprites[2];
        }
    }

    // Helper metodlar
    public Sprite[] GetSprites(string path)
    {
        if (spriteCache.ContainsKey(path))
            return spriteCache[path];

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        if (sprites != null && sprites.Length > 0)
        {
            spriteCache[path] = sprites;
        }
        return sprites;
    }

    public bool HasPlayerSprites()
    {
        return playerIdle != null && playerIdle.Length > 0;
    }

    public bool HasEnemySprites()
    {
        return slimeIdle != null && slimeIdle.Length > 0;
    }
}
