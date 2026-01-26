using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance;

    [Header("Particle Prefabs")]
    public ParticleSystem coinCollectPrefab;
    public ParticleSystem enemyDeathPrefab;
    public ParticleSystem jumpDustPrefab;
    public ParticleSystem landDustPrefab;
    public ParticleSystem damageEffectPrefab;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayCoinCollect(Vector3 position)
    {
        if (coinCollectPrefab != null)
        {
            ParticleSystem ps = Instantiate(coinCollectPrefab, position, Quaternion.identity);
            Destroy(ps.gameObject, 2f);
        }
        else
        {
            CreateSimpleParticle(position, Color.yellow, 20);
        }
    }

    public void PlayEnemyDeath(Vector3 position)
    {
        if (enemyDeathPrefab != null)
        {
            ParticleSystem ps = Instantiate(enemyDeathPrefab, position, Quaternion.identity);
            Destroy(ps.gameObject, 2f);
        }
        else
        {
            CreateSimpleParticle(position, Color.red, 30);
        }
    }

    public void PlayJumpDust(Vector3 position)
    {
        if (jumpDustPrefab != null)
        {
            ParticleSystem ps = Instantiate(jumpDustPrefab, position, Quaternion.identity);
            Destroy(ps.gameObject, 2f);
        }
        else
        {
            CreateSimpleParticle(position, new Color(0.7f, 0.6f, 0.5f), 10);
        }
    }

    public void PlayLandDust(Vector3 position)
    {
        if (landDustPrefab != null)
        {
            ParticleSystem ps = Instantiate(landDustPrefab, position, Quaternion.identity);
            Destroy(ps.gameObject, 2f);
        }
        else
        {
            CreateSimpleParticle(position, new Color(0.7f, 0.6f, 0.5f), 15);
        }
    }

    public void PlayDamageEffect(Vector3 position)
    {
        if (damageEffectPrefab != null)
        {
            ParticleSystem ps = Instantiate(damageEffectPrefab, position, Quaternion.identity);
            Destroy(ps.gameObject, 2f);
        }
        else
        {
            CreateSimpleParticle(position, Color.white, 25);
        }
    }

    public void PlayPowerUpCollect(Vector3 position)
    {
        // Renkli parlama efekti
        CreateSimpleParticle(position, new Color(0.5f, 1f, 0.5f), 20);
    }

    public void PlayItemUse(Vector3 position, Color color)
    {
        CreateSimpleParticle(position, color, 15);
    }

    // === YENI EFEKTLER ===

    public void PlayExplosion(Vector3 position)
    {
        // Ana patlama
        CreateExplosionParticle(position, new Color(1f, 0.5f, 0f), 50); // Turuncu
        CreateExplosionParticle(position, new Color(1f, 0.8f, 0f), 30); // Sari
        CreateExplosionParticle(position, new Color(1f, 0.2f, 0f), 20); // Kirmizi

        // Duman
        CreateSmokeParticle(position, 15);

        // Kivilcimlar
        CreateSparkParticle(position, 25);
    }

    public void PlayDashEffect(Vector3 position, int direction)
    {
        // Dash izi efekti - neon mavisi
        CreateDashTrail(position, direction, new Color(0f, 0.8f, 1f, 0.8f));
    }

    public void PlayRollEffect(Vector3 position, int direction)
    {
        // Takla tozu efekti
        CreateRollDust(position, direction);
    }

    public void PlaySpeedBoostEffect(Vector3 position)
    {
        // Hiz efekti - cizgiler
        CreateSpeedLines(position, new Color(0f, 1f, 0.5f));
    }

    public void PlayShieldEffect(Vector3 position)
    {
        // Kalkan kirilma efekti
        CreateShieldBreak(position);
    }

    public void PlayHealEffect(Vector3 position)
    {
        // Iyilesme efekti - yesil parcaciklar yukari
        CreateHealParticle(position);
    }

    public void PlayComboEffect(Vector3 position, int comboLevel)
    {
        // Combo seviyesine gore efekt
        Color comboColor = comboLevel switch
        {
            >= 10 => new Color(1f, 0f, 1f),     // Mor - maksimum
            >= 7 => new Color(1f, 0.5f, 0f),    // Turuncu
            >= 5 => new Color(1f, 1f, 0f),      // Sari
            >= 3 => new Color(0f, 1f, 1f),      // Cyan
            _ => new Color(1f, 1f, 1f)           // Beyaz
        };

        CreateComboParticle(position, comboColor, comboLevel * 5);
    }

    public void PlayDeathEffect(Vector3 position)
    {
        // Ölüm efekti - kırmızı patlama ve parçacıklar
        CreateDeathParticle(position);
    }

    // === OZEL EFEKT OLUSTURUCULARI ===

    void CreateExplosionParticle(Vector3 position, Color color, int count)
    {
        GameObject particleObj = new GameObject("ExplosionParticle");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor = color;
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.5f, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 2f);
    }

    void CreateSmokeParticle(Vector3 position, int count)
    {
        GameObject particleObj = new GameObject("SmokeParticle");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        main.gravityModifier = -0.3f; // Yukari dogru
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(1f, 2f)
        ));

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 3f);
    }

    void CreateSparkParticle(Vector3 position, int count)
    {
        GameObject particleObj = new GameObject("SparkParticle");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(1f, 1f, 0.5f);
        main.gravityModifier = 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var trails = ps.trails;
        trails.enabled = true;
        trails.lifetime = 0.3f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        ));

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.trailMaterial = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 2f);
    }

    void CreateDashTrail(Vector3 position, int direction, Color color)
    {
        GameObject particleObj = new GameObject("DashTrail");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.3f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(-direction * 2f, -direction * 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startColor = color;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.2f, 1f, 0.1f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 1f);
    }

    void CreateRollDust(Vector3 position, int direction)
    {
        GameObject particleObj = new GameObject("RollDust");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new Color(0.6f, 0.5f, 0.4f, 0.7f);
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = -direction * 2f;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 1f);
    }

    void CreateSpeedLines(Vector3 position, Color color)
    {
        GameObject particleObj = new GameObject("SpeedLines");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(-10f, -15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor = color;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.1f, 1.5f, 0.1f);

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3f;

        Destroy(particleObj, 1f);
    }

    void CreateShieldBreak(Vector3 position)
    {
        GameObject particleObj = new GameObject("ShieldBreak");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(0.3f, 0.7f, 1f, 0.8f); // Mavi kalkan rengi
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        shape.radiusThickness = 0.1f; // Sadece kenardan

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 1.5f);
    }

    void CreateHealParticle(Vector3 position)
    {
        GameObject particleObj = new GameObject("HealParticle");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startColor = new Color(0.2f, 1f, 0.3f);
        main.gravityModifier = -1f; // Yukari dogru
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 1f, 0.3f), 0f), new GradientColorKey(new Color(0.8f, 1f, 0.8f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 2f);
    }

    void CreateComboParticle(Vector3 position, Color color, int count)
    {
        GameObject particleObj = new GameObject("ComboParticle");
        particleObj.transform.position = position + Vector3.up * 0.5f;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor = color;
        main.gravityModifier = -0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.3f;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 2f);
    }

    void CreateSimpleParticle(Vector3 position, Color color, int count)
    {
        GameObject particleObj = new GameObject("Particle");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.startSize = 0.2f;
        main.startColor = color;
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, count)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 2f);
    }

    void CreateDeathParticle(Vector3 position)
    {
        // Ana ölüm patlaması - kırmızı
        GameObject particleObj = new GameObject("DeathParticle");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor = new Color(1f, 0.2f, 0.2f); // Kırmızı
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f),
                new GradientColorKey(new Color(0.5f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Destroy(particleObj, 2f);

        // İkinci katman - turuncu kıvılcımlar
        CreateExplosionParticle(position, new Color(1f, 0.5f, 0f), 25);
    }
}
