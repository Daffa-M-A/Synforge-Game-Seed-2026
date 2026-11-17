using System.Collections.Generic;
using UnityEngine;

public class GameVfxManager : MonoBehaviour
{
    public static GameVfxManager Instance { get; private set; }

    [Header("Capture Burst Colors")]
    [SerializeField] private Color patternCaptureColor = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private Color adjacentCaptureColor = new Color(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color releaseColor = new Color(0.35f, 1f, 0.55f, 1f);
    [SerializeField] private Color rechargeNeonColor = new Color(0.1f, 1f, 0.45f, 1f);
    [SerializeField] private Color dustColor = new Color(0.72f, 0.62f, 0.48f, 0.65f);

    [Header("Character Blink Highlights")]
    public Color rechargeBlink = new Color(0.1f, 1f, 0.45f, 1f); // Warna Hijau (Recharge)
    public Color sacrificeApproachBlink = new Color(1f, 0.92f, 0.15f, 1f); // Kuning
    public Color sacrificePrisonBlink = new Color(1f, 0.18f, 0.18f, 1f); // Merah

    private readonly Queue<ParticleSystem> burstPool = new Queue<ParticleSystem>();
    private readonly Queue<ParticleSystem> dustPool = new Queue<ParticleSystem>();
    private Transform poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        poolRoot = new GameObject("VfxPool").transform;
        poolRoot.SetParent(transform);
    }

    public static GameVfxManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameVfxManager existing = FindFirstObjectByType<GameVfxManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject go = new GameObject("GameVfxManager");
        return go.AddComponent<GameVfxManager>();
    }

    public void PlayCaptureVfx(CaptureConditionType type, Vector3 worldPosition, int team)
    {
        switch (type)
        {
            case CaptureConditionType.Pattern:
                PlayBurst(worldPosition, patternCaptureColor, 18, 0.55f, 0.35f, 1.1f);
                break;
            case CaptureConditionType.Adjacent:
                PlayBurst(worldPosition, adjacentCaptureColor, 24, 0.45f, 0.28f, 0.95f);
                PlayRingBurst(worldPosition, adjacentCaptureColor, 12, 0.7f);
                break;
        }
    }

    public void PlayPrisonerReleaseVfx(Vector3 worldPosition, int team)
    {
        Color tint = team == 0 ? releaseColor : new Color(0.55f, 0.95f, 1f, 1f);
        PlayBurst(worldPosition, tint, 28, 0.65f, 0.4f, 1.25f);
        PlayRingBurst(worldPosition, tint, 16, 0.85f);
    }

    public void PlayRechargeRankVfx(Vector3 worldPosition)
    {
        PlayBurst(worldPosition, rechargeNeonColor, 16, 0.45f, 0.22f, 0.75f);
        PlayRingBurst(worldPosition, rechargeNeonColor, 10, 0.45f);
    }

    public void SpawnDustTrail(Vector3 worldPosition)
    {
        ParticleSystem ps = GetFromPool(dustPool, CreateDustSystem, 0.75f);
        ps.transform.position = worldPosition + Vector3.up * 0.05f;
        ps.Play(true);
    }

    private void PlayBurst(Vector3 position, Color color, int count, float lifetime, float startSize, float startSpeed)
    {
        ParticleSystem ps = GetFromPool(burstPool, () => CreateBurstSystem(false), lifetime + 0.35f);
        ConfigureBurst(ps, color, count, lifetime, startSize, startSpeed, 360f);
        ps.transform.position = position + Vector3.up * 0.25f;
        ps.Play(true);
    }

    private void PlayRingBurst(Vector3 position, Color color, int count, float radius)
    {
        const float lifetime = 0.55f;
        ParticleSystem ps = GetFromPool(burstPool, () => CreateBurstSystem(true), lifetime + 0.35f);
        var main = ps.main;
        main.startColor = color;
        main.startLifetime = 0.55f;
        main.startSize = 0.18f;
        main.startSpeed = 0.01f;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0f;

        ps.transform.position = position + Vector3.up * 0.35f;
        ps.Play(true);
    }

    private ParticleSystem GetFromPool(Queue<ParticleSystem> pool, System.Func<ParticleSystem> factory, float recycleDelay)
    {
        ParticleSystem ps;
        if (pool.Count > 0)
        {
            ps = pool.Dequeue();
            ps.gameObject.SetActive(true);
        }
        else
        {
            ps = factory();
        }

        StartCoroutine(ReturnToPoolAfter(ps, pool, recycleDelay));
        return ps;
    }

    private System.Collections.IEnumerator ReturnToPoolAfter(ParticleSystem ps, Queue<ParticleSystem> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps == null) yield break;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
    }

    private ParticleSystem CreateBurstSystem(bool ring)
    {
        GameObject go = new GameObject(ring ? "RingBurstVfx" : "CaptureBurstVfx");
        go.transform.SetParent(poolRoot);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        return ps;
    }

    private ParticleSystem CreateDustSystem()
    {
        GameObject go = new GameObject("DustTrailVfx");
        go.transform.SetParent(poolRoot);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = dustColor;
        main.startLifetime = 0.35f;
        main.startSize = 0.22f;
        main.startSpeed = 0.6f;
        main.gravityModifier = -0.15f;
        main.maxParticles = 6;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        return ps;
    }

    private void ConfigureBurst(ParticleSystem ps, Color color, int count, float lifetime, float startSize, float startSpeed, float coneAngle)
    {
        var main = ps.main;
        main.startColor = color;
        main.startLifetime = lifetime;
        main.startSize = startSize;
        main.startSpeed = startSpeed;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle * 0.5f;
        shape.radius = 0.08f;
    }

    // ---- Zone Domination aura ----
    [Header("Zone Domination Aura")]
    [Tooltip("Warna glow + asap bidak yang berada di zona musuh.")]
    public Color zoneAuraColor = new Color(0.62f, 0.32f, 1f, 1f); // ungu
    [Tooltip("Jarak jangkau cahaya glow di sekitar bidak.")]
    public float zoneAuraLightRange = 1.6f;
    public float zoneAuraLightIntensity = 2.2f;
    [Tooltip("Jumlah kepulan asap per detik.")]
    public float zoneAuraSmokeRate = 7f;

    // Membuat objek aura (asap ngepul + point light ungu) sebagai child bidak. Objek dikembalikan dalam keadaan aktif.
    public GameObject CreateZoneAura(Transform parent)
    {
        GameObject root = new GameObject("ZoneAuraVfx");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;

        // Glow: point light berwarna di sekitar bidak
        GameObject lightGo = new GameObject("ZoneGlowLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = zoneAuraColor;
        light.range = zoneAuraLightRange;
        light.intensity = zoneAuraLightIntensity;
        light.shadows = LightShadows.None;

        // Asap: partikel lembut yang naik perlahan dan memudar
        GameObject smokeGo = new GameObject("ZoneSmoke");
        smokeGo.transform.SetParent(root.transform, false);
        smokeGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        ParticleSystem ps = smokeGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var renderer = smokeGo.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial();
        renderer.sortingFudge = -10f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.06f;
        main.maxParticles = 40;
        Color smokeStart = zoneAuraColor; smokeStart.a = 0.55f;
        Color smokeEnd = Color.Lerp(zoneAuraColor, Color.white, 0.5f); smokeEnd.a = 0.35f;
        main.startColor = new ParticleSystem.MinMaxGradient(smokeStart, smokeEnd);

        var emission = ps.emission;
        emission.rateOverTime = zoneAuraSmokeRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.32f;
        shape.radiusThickness = 1f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

        ps.Play(true);
        return root;
    }

    private static Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.color = Color.white;
        return mat;
    }
}
