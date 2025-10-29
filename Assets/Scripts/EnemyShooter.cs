using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Interfaces & Helpers (kept in same file for single-drop workflow)
public interface IDamageable
{
    void TakeDamage(float amount);
}
#endregion

/// <summary>
/// EnemyShooter: single drop-in 2D shooter for bullet-hell enemies.
/// - Toggle modes: Single, Shotgun/Multi, Minigun (spin-up + overheat), Spiral, Homing (missile), Soft-homing (javelin)
/// - Predictive aim with adjustable lead
/// - Burst patterns (Single, 3-shot, 5-shot, Custom) with editable pause
/// - Internal bullet pooling with optional cap and reuse-oldest policy
/// - Aim line telegraph (thin while tracking; bold during fire window)
/// - Global difficulty multipliers and per-enemy upgrade multipliers
/// - Auto-rotates to face player when firing
/// Everything needed is in this single .cs file (Projectile2D included).
/// </summary>
[DisallowMultipleComponent]
public class EnemyShooter : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Target to aim at (usually the player boat).")]
    public Transform player;

    [Tooltip("Optional: Player Rigidbody2D used for predictive lead. If null, uses Transform only.")]
    public Rigidbody2D playerRb;

    [Tooltip("Where bullets spawn from. If null, uses this.transform.position.")]
    public Transform muzzle;

    [Tooltip("Optional: Assign your own bullet prefab (with Collider2D marked as Trigger). If left empty, a simple default will be generated at runtime.")]
    public Projectile2D bulletPrefab;

    [Header("=== Global Toggles ===")]
    [Tooltip("Master switch for firing.")]
    public bool enableFiring = true;

    [Tooltip("Auto-rotate this enemy to face the target when firing (2D Z rotation).")]
    public bool autoRotateToTarget = true;

    public enum FireBlend { Sequential, Simultaneous, Random }
    [Tooltip("If multiple modes are enabled, how should they run?")]
    public FireBlend multiModeBlend = FireBlend.Sequential;

    [Header("=== Difficulty & Upgrades ===")]
    [Tooltip("Global difficulty multipliers (can be changed by level).")]
    public float globalFireRateMult = 1f;
    public float globalBulletSpeedMult = 1f;
    public float globalDamageMult = 1f;

    [Tooltip("Per-enemy upgrade multiplier (applied in addition to global).")]
    public float upgradeMult = 1f;

    [Header("=== Pooling & Limits ===")]
    [Tooltip("Enable an internal pool so we don't instantiate at runtime after warmup.")]
    public bool usePooling = true;

    [Tooltip("Initial pool size to pre-warm.")]
    public int poolWarmup = 32;

    [Tooltip("Optional hard cap on live bullets. 0 = no cap.")]
    public int maxLiveBullets = 0;

    [Tooltip("If cap is reached, reuse the oldest bullet instead of skipping new spawns.")]
    public bool reuseOldestWhenCapped = true;

    // === Fire Cadence ===
    public enum BurstMode { Single, Burst3, Burst5, Custom }

    [Header("=== Fire Cadence ===")]
    [Tooltip("Burst style for each cycle.")]
    public BurstMode burstMode = BurstMode.Single;

    [Tooltip("If BurstMode = Custom, number of shots per burst.")]
    public int customBurstCount = 4;

    [Tooltip("Time between shots within a burst.")]
    public float intraBurstDelay = 0.08f;

    [Tooltip("Pause after a burst finishes.")]
    public float postBurstPause = 0.6f;

    [Tooltip("Add random +/- jitter to postBurstPause.")]
    public float postBurstPauseJitter = 0.0f;

    [Header("=== Aim & Lead ===")]
    [Tooltip("Base aim mode for most weapons.")]
    public AimMode aimMode = AimMode.AimAtPlayer;
    public enum AimMode { AimAtPlayer, PredictiveLead, FixedDirection, Sweep, RotateTurret }

    [Tooltip("For PredictiveLead, scales the computed intercept velocity. 1 = perfect intercept (given perfect inputs).")]
    public float leadMultiplier = 1.0f;

    [Tooltip("Random spread (degrees) added to aim direction.")]
    public float aimInaccuracyDegrees = 0f;

    [Tooltip("For FixedDirection: absolute angle in degrees (world-space) to shoot.")]
    public float fixedAngleDegrees = 0f;

    [Tooltip("For Sweep/RotateTurret: degrees per second to rotate aim vector.")]
    public float rotateDegreesPerSecond = 60f;

    [Header("=== Aim Line Telegraph ===")]
    [Tooltip("Show a LineRenderer that tracks the aim. Thickens briefly when firing.")]
    public bool showAimLine = true;

    [Tooltip("Line width while tracking target.")]
    public float lineWidthTracking = 0.025f;

    [Tooltip("Line width during the fire moment (bold).")]
    public float lineWidthFiring = 0.08f;

    [Tooltip("Aim line max length (world units).")]
    public float aimLineLength = 12f;

    [Tooltip("How long to keep line bolded around each shot (seconds).")]
    public float boldDuration = 0.08f;

    [Tooltip("Aim line color.")]
    public Color aimLineColor = new Color(1f, 0.3f, 0.1f, 0.9f);

    LineRenderer _line;

    [Header("=== Base Bullet Settings (applied unless overridden by mode) ===")]
    public float baseFireRate = 2.0f;           // shots per second (for non-burst cadence)
    public float baseBulletSpeed = 7.5f;        // world units per second
    public float baseDamage = 5f;
    public float baseLifetime = 6f;
    public float baseKnockback = 0f;
    public LayerMask bulletHitMask = ~0;        // default everything
    public bool destroyOnHit = true;

    [Header("=== Modes: Toggle & Settings ===")]
    public bool modeSingle = true;

    [Space(6)]
    public bool modeShotgun = false;
    [Tooltip("How many pellets for shotgun / multi-shot.")]
    public int shotgunPellets = 5;
    [Tooltip("Total cone angle (degrees) for shotgun / multi-shot.")]
    public float shotgunConeAngle = 24f;

    [Space(6)]
    public bool modeMultiNWay = false;
    [Tooltip("N-way evenly spaced at a given spread step angle (centered on aim).")]
    public int nWayCount = 3;
    public float nWayStepDegrees = 8f;

    [Space(6)]
    public bool modeMinigun = false;
    [Tooltip("Time to spin up to max fire rate (seconds).")]
    public float minigunSpinUp = 0.75f;
    [Tooltip("Base fire rate during spin-up is scaled from a minimum to the baseFireRate * minigunFireRateMult.")]
    public float minigunMinRate = 4f;
    public float minigunFireRateMult = 3f;

    [Header("Overheat (Minigun)")]
    public bool useOverheat = true;
    public float heatPerShot = 2f;
    public float heatMax = 100f;
    public float heatCoolPerSecond = 20f;
    public float overheatLockSeconds = 1.0f;

    [Tooltip("If true, apply overheat to ALL fire modes. Otherwise only Minigun uses heat.")]
    public bool overheatAffectsAllModes = false;

    float _heat, _overheatLockTimer;

    [Space(6)]
    public bool modeSpiral = false;
    [Tooltip("Degrees per shot step for spiral pattern.")]
    public float spiralStepDegrees = 12f;
    float _spiralAngle;

    [Space(6)]
    public bool modeHomingMissile = false;
    [Tooltip("Strong homing: actively steers while seekTime > 0.")]
    public float homingSteerStrength = 720f; // deg/s turn rate-ish proxy
    public float homingSeekTime = 2.5f;

    [Space(6)]
    public bool modeSoftHomingJavelin = false;
    [Tooltip("Soft homing: only early gentle curve, then locks into straight.")]
    public float softHomingCurveStrength = 180f;
    public float softHomingDuration = 0.6f;

    [Header("=== Optional: Ballistic (top-down flavor) ===")]
    public bool modeBallistic = false;
    [Tooltip("Applies fake arc via vertical offset curve (purely visual 2D spice). 0 disables.")]
    [Range(0f, 1f)] public float ballisticArcAmount = 0.0f;

    // Private state
    Rigidbody2D _rb;
    float _nextCycleTime;
    float _localAimAngleDeg; // used by sweep/rotate
    float _spinupLerp;       // 0..1 spinup
    readonly Queue<Projectile2D> _pool = new Queue<Projectile2D>();
    readonly LinkedList<Projectile2D> _live = new LinkedList<Projectile2D>();
    System.Random _rng;
    bool _cycleRunning;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rng = new System.Random();

        // Prepare muzzle
        if (!muzzle) muzzle = this.transform;

        // Ensure a LineRenderer exists if telegraphing
        if (showAimLine)
        {
            _line = GetOrCreateLineRenderer();
        }

        // Create default bullet prefab if none provided
        if (!bulletPrefab)
        {
            bulletPrefab = CreateRuntimeDefaultBulletPrefab();
        }

        // Warm pool
        if (usePooling && bulletPrefab)
        {
            for (int i = 0; i < Mathf.Max(0, poolWarmup); i++)
            {
                var p = Instantiate(bulletPrefab);
                p.gameObject.SetActive(false);
                _pool.Enqueue(p);
            }
        }

        // Seed initial aim angle if using sweep/rotate
        _localAimAngleDeg = fixedAngleDegrees;
    }

    void Update()
    {
        // Cool heat/overheat
        if (useOverheat)
        {
            if (_overheatLockTimer > 0f) _overheatLockTimer -= Time.deltaTime;
            _heat = Mathf.Max(0f, _heat - heatCoolPerSecond * Time.deltaTime);
        }

        // Aim line update
        if (showAimLine && _line)
        {
            UpdateAimLine();
        }
    }

    void FixedUpdate()
    {
        if (!enableFiring) return;
        if (_overheatLockTimer > 0f) return;

        if (!_cycleRunning && Time.time >= _nextCycleTime)
        {
            _cycleRunning = true;               // gate re-entry
            _nextCycleTime = Time.time + 999f;  // temporary far future while we run
            StartCoroutine(FireCycle());
        }
    }

    IEnumerator FireCycle()
    {
        // Determine burst count
        int shots = burstMode switch
        {
            BurstMode.Single => 1,
            BurstMode.Burst3 => 3,
            BurstMode.Burst5 => 5,
            BurstMode.Custom => Mathf.Max(1, customBurstCount),
            _ => 1
        };

        // Compute final fire rate for cadence (used to compute time to next cycle)
        float cadenceRate = GetEffectiveFireRateForCadence();

        // Fire sequence
        for (int i = 0; i < shots; i++)
        {
            float delay = (i == 0) ? 0f : intraBurstDelay;

            // Fire according to blend
            yield return StartCoroutine(FireOnceBlend());

            // Overheat accounting
            bool applyHeat = useOverheat && (modeMinigun || overheatAffectsAllModes);
            if (applyHeat && heatPerShot > 0f)
            {
                _heat += heatPerShot;
                if (_heat >= heatMax)
                {
                    _overheatLockTimer = overheatLockSeconds;
                    _heat = heatMax * 0.65f; // drop below threshold after lock
                    break; // end burst early
                }
            }

            // Bold line briefly on shot
            if (showAimLine && _line) StartCoroutine(BoldLineFor(boldDuration));

            if (delay > 0f) yield return new WaitForSeconds(delay);
        }

        // Pause before next cycle
        float pause = Mathf.Max(0f, postBurstPause + (float)(_rng.NextDouble() * 2 - 1) * postBurstPauseJitter);

        // Schedule next cycle considering baseFireRate / cadenceRate
        float cycleGap = (cadenceRate > 0f) ? (1f / cadenceRate) : 0.5f;
        _nextCycleTime = Time.time + pause + cycleGap;
        _cycleRunning = false;
    }

    float GetEffectiveFireRateForCadence()
    {
        // Minigun: dynamic rate (spin-up)
        float rate = baseFireRate;
        if (modeMinigun)
        {
            _spinupLerp = Mathf.Clamp01(_spinupLerp + Time.fixedDeltaTime / Mathf.Max(0.0001f, minigunSpinUp));
            float minRate = Mathf.Max(0.01f, minigunMinRate);
            float maxRate = Mathf.Max(minRate, baseFireRate * minigunFireRateMult);
            rate = Mathf.Lerp(minRate, maxRate, _spinupLerp);
        }

        // Apply globals
        rate *= Mathf.Max(0.01f, globalFireRateMult) * Mathf.Max(0.01f, upgradeMult);
        return rate;
    }

    IEnumerator FireOnceBlend()
    {
        switch (multiModeBlend)
        {
            case FireBlend.Simultaneous:
                yield return StartCoroutine(FireAllEnabledOnceSimultaneous());
                break;
            case FireBlend.Random:
                yield return StartCoroutine(FireOneRandomEnabled());
                break;
            case FireBlend.Sequential:
            default:
                yield return StartCoroutine(FireNextEnabledSequential());
                break;
        }
    }

    // Keeps rotating through enabled modes in a sequence.
    int _seqIndex;
    IEnumerator FireNextEnabledSequential()
    {
        var enabledList = GetEnabledModeIndices();
        if (enabledList.Count == 0) yield break;

        _seqIndex = (_seqIndex + 1) % enabledList.Count;
        int pick = enabledList[_seqIndex];
        yield return StartCoroutine(FireByIndex(pick));
    }

    IEnumerator FireOneRandomEnabled()
    {
        var enabledList = GetEnabledModeIndices();
        if (enabledList.Count == 0) yield break;
        int pick = enabledList[_rng.Next(enabledList.Count)];
        yield return StartCoroutine(FireByIndex(pick));
    }

    IEnumerator FireAllEnabledOnceSimultaneous()
    {
        var enabledList = GetEnabledModeIndices();
        foreach (int idx in enabledList)
            StartCoroutine(FireByIndex(idx));
        yield return null;
    }

    List<int> GetEnabledModeIndices()
    {
        var list = new List<int>(6);
        if (modeSingle) list.Add(0);
        if (modeShotgun) list.Add(1);
        if (modeMultiNWay) list.Add(2);
        if (modeMinigun) list.Add(3);
        if (modeSpiral) list.Add(4);
        if (modeHomingMissile) list.Add(5);
        if (modeSoftHomingJavelin) list.Add(6);
        if (modeBallistic) list.Add(7);
        return list;
    }

    IEnumerator FireByIndex(int idx)
    {
        switch (idx)
        {
            case 0: FireSingle(); break;
            case 1: FireShotgun(); break;
            case 2: FireMultiNWay(); break;
            case 3: FireMinigun(); break;
            case 4: FireSpiral(); break;
            case 5: FireHoming(true); break;
            case 6: FireHoming(false); break;
            case 7: FireBallistic(); break;
        }
        yield return null;
    }

    #region Fire Implementations
    void FireSingle()
    {
        Vector2 dir = GetAimDirection();
        SpawnBullet(dir, baseBulletSpeed, baseDamage);
    }

    void FireShotgun()
    {
        Vector2 aim = GetAimDirection();
        int pellets = Mathf.Max(1, shotgunPellets);
        float cone = shotgunConeAngle;

        for (int i = 0; i < pellets; i++)
        {
            float t = (pellets == 1) ? 0f : (i / (float)(pellets - 1) * 2f - 1f);
            float offset = t * cone * 0.5f;
            Vector2 dir = Rotate(aim, offset);
            SpawnBullet(dir, baseBulletSpeed, baseDamage);
        }
    }

    void FireMultiNWay()
    {
        Vector2 aim = GetAimDirection();
        int n = Mathf.Max(2, nWayCount);
        float step = nWayStepDegrees;
        int half = (n - 1) / 2;

        for (int i = -half; i <= half; i++)
        {
            float offset = i * step;
            Vector2 dir = Rotate(aim, offset);
            SpawnBullet(dir, baseBulletSpeed, baseDamage);
        }
        // if even count, add the extreme on one side
        if (n % 2 == 0)
        {
            float offset = (half + 1) * nWayStepDegrees;
            Vector2 dir = Rotate(aim, offset);
            SpawnBullet(dir, baseBulletSpeed, baseDamage);
        }
    }

    void FireMinigun()
    {
        Vector2 dir = GetAimDirection();
        float speed = baseBulletSpeed * (1f + 0.1f * _spinupLerp);
        SpawnBullet(dir, speed, baseDamage);

        // Minigun tends to add a touch of inaccuracy
        aimInaccuracyDegrees = Mathf.Max(aimInaccuracyDegrees, 2f);
    }

    void FireSpiral()
    {
        _spiralAngle += spiralStepDegrees;
        Vector2 baseAim = GetAimDirection();
        Vector2 dir = Rotate(baseAim, _spiralAngle);
        SpawnBullet(dir, baseBulletSpeed, baseDamage);
    }

    void FireHoming(bool strong)
    {
        Vector2 dir = GetAimDirection();
        var b = SpawnBullet(dir, baseBulletSpeed, baseDamage);
        if (!b) return;

        if (strong)
        {
            b.homingType = Projectile2D.HomingType.Strong;
            b.homingSteerStrength = homingSteerStrength;
            b.homingSeekTime = homingSeekTime;
        }
        else
        {
            b.homingType = Projectile2D.HomingType.Soft;
            b.homingSteerStrength = softHomingCurveStrength;
            b.homingSeekTime = softHomingDuration;
        }
        b.homingTarget = player;
        b.bulletHitMask = bulletHitMask;
    }

    void FireBallistic()
    {
        Vector2 dir = GetAimDirection();
        var b = SpawnBullet(dir, baseBulletSpeed, baseDamage);
        if (!b) return;

        b.ballisticArcAmount = ballisticArcAmount;
    }
    #endregion

    #region Aim & Telegraph
// --- Replace entire GetAimDirection() with this version ---
Vector2 _smoothedAimDir = Vector2.right; // add near other private fields

Vector2 GetAimDirection()
{
    Vector3 muzzlePos = muzzle ? muzzle.position : transform.position;
    Vector2 desiredDir = Vector2.right;

    switch (aimMode)
    {
        case AimMode.AimAtPlayer:
            if (player)
                desiredDir = (player.position - muzzlePos).normalized;
            break;

        case AimMode.PredictiveLead:
            if (player)
            {
                Vector2 targetPos = player.position;
                Vector2 targetVel = (playerRb ? playerRb.linearVelocity : Vector2.zero);
                float speed = baseBulletSpeed * globalBulletSpeedMult * upgradeMult;
                desiredDir = PredictInterceptDirection((Vector2)muzzlePos, targetPos, targetVel, speed, leadMultiplier);
            }
            break;

        case AimMode.FixedDirection:
            desiredDir = AngleToDir(fixedAngleDegrees);
            break;

        case AimMode.Sweep:
        case AimMode.RotateTurret:
            _localAimAngleDeg += rotateDegreesPerSecond * Time.deltaTime;
            desiredDir = AngleToDir(_localAimAngleDeg);
            break;
    }

    // --- new smoothing & forgiveness ---
    float forgiveness = 0.25f;        // smaller = snappier, larger = lazier
    _smoothedAimDir = Vector2.Lerp(_smoothedAimDir, desiredDir, 1f - Mathf.Exp(-Time.deltaTime / forgiveness));
    Vector2 dir = _smoothedAimDir.normalized;

    // Add mild random offset
    float noise = (float)(_rng.NextDouble() * 2 - 1) * aimInaccuracyDegrees;
    dir = Rotate(dir, noise);

    // Optional body rotation
    if (autoRotateToTarget)
    {
        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, z - 90f), 0.2f);
    }

    return dir;
}


    void UpdateAimLine()
    {
        if (!_line) return;
        _line.startColor = aimLineColor;
        _line.endColor = aimLineColor;
        _line.startWidth = Mathf.Lerp(_line.startWidth, lineWidthTracking, 0.25f);
        _line.endWidth = _line.startWidth;

        Vector3 start = muzzle ? muzzle.position : transform.position;
        Vector3 dir = GetAimDirection();
        Vector3 end = start + dir * aimLineLength;

        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
    }

    IEnumerator BoldLineFor(float t)
    {
        if (!_line) yield break;
        _line.startWidth = lineWidthFiring;
        _line.endWidth = lineWidthFiring;
        yield return new WaitForSeconds(t);
        _line.startWidth = lineWidthTracking;
        _line.endWidth = lineWidthTracking;
    }

    LineRenderer GetOrCreateLineRenderer()
    {
        var lr = GetComponent<LineRenderer>();
        if (!lr) lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        // URP-safe shader fallback
        var shader = Shader.Find("Sprites/Default");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
        lr.material = new Material(shader);

        lr.textureMode = LineTextureMode.Stretch;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        lr.startWidth = lineWidthTracking;
        lr.endWidth = lineWidthTracking;
        lr.startColor = aimLineColor;
        lr.endColor = aimLineColor;
        lr.sortingOrder = 50;
        return lr;
    }
    #endregion

    #region Spawn & Pool
    Projectile2D SpawnBullet(Vector2 dir, float speed, float dmg)
    {
        if (!bulletPrefab) return null;

        // Limits
        if (maxLiveBullets > 0 && _live.Count >= maxLiveBullets)
        {
            if (reuseOldestWhenCapped && _live.Count > 0)
            {
                // Reuse oldest
                var oldest = _live.First.Value;
                _live.RemoveFirst();
                ReinitializeBullet(oldest, dir, speed, dmg);
                _live.AddLast(oldest);
                return oldest;
            }
            else
            {
                return null; // skip spawn
            }
        }

        Projectile2D b = null;
        if (usePooling && _pool.Count > 0)
        {
            b = _pool.Dequeue();
            b.gameObject.SetActive(true);
            ReinitializeBullet(b, dir, speed, dmg);
        }
        else
        {
            b = Instantiate(bulletPrefab);
            ReinitializeBullet(b, dir, speed, dmg);
        }

        b.onDespawn = (proj) =>
        {
            // remove from live
            var node = _live.Find(proj);
            if (node != null) _live.Remove(node);
            // return to pool if pooling
            if (usePooling)
            {
                proj.gameObject.SetActive(false);
                _pool.Enqueue(proj);
            }
            else
            {
                Destroy(proj.gameObject);
            }
        };

        _live.AddLast(b);
        return b;
    }

    void ReinitializeBullet(Projectile2D b, Vector2 dir, float speed, float dmg)
    {
        Vector3 pos = muzzle ? muzzle.position : transform.position;

        b.transform.position = pos;
        b.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

        float finalSpeed = speed * Mathf.Max(0.01f, globalBulletSpeedMult) * Mathf.Max(0.01f, upgradeMult);
        float finalDmg = dmg * Mathf.Max(0.01f, globalDamageMult) * Mathf.Max(0.01f, upgradeMult);

        b.Init(dir.normalized * finalSpeed,
               finalDmg,
               baseLifetime,
               baseKnockback,
               destroyOnHit,
               bulletHitMask);
    }

    Projectile2D CreateRuntimeDefaultBulletPrefab()
    {
        // Create a simple bullet prefab at runtime (Sprite + RB2D + Collider2D + Projectile2D)
        var go = new GameObject("DefaultBullet2D");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(0.06f);
        sr.sortingOrder = 60;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.08f;

        var rb2d = go.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 0f;
        rb2d.angularDamping = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var proj = go.AddComponent<Projectile2D>();
        DontDestroyOnLoad(go); // keep prefab-like
        go.SetActive(false);
        return proj;
    }

    Sprite CreateCircleSprite(float r)
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var col = new Color32[size * size];
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float rr = r * size;
        float rr2 = rr * rr;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x, y);
                var d2 = (p - c).sqrMagnitude;
                col[y * size + x] = (d2 <= rr2) ? new Color32(255, 200, 160, 255) : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(col);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    #endregion

    #region Math Utils
    static Vector2 AngleToDir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)).normalized;
    static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

    static Vector2 PredictInterceptDirection(Vector2 shooterPos, Vector2 targetPos, Vector2 targetVel, float projSpeed, float leadMult)
    {
        Vector2 toTarget = targetPos - shooterPos;
        float a = Vector2.Dot(targetVel, targetVel) - projSpeed * projSpeed;
        float b = 2f * Vector2.Dot(toTarget, targetVel);
        float c = Vector2.Dot(toTarget, toTarget);
        float t;
        if (Mathf.Abs(a) < 0.0001f)
        {
            // Linear solution
            t = -c / Mathf.Max(0.0001f, b);
        }
        else
        {
            float disc = b * b - 4f * a * c;
            if (disc < 0f) t = 0f;
            else
            {
                float sqrt = Mathf.Sqrt(disc);
                float t1 = (-b + sqrt) / (2f * a);
                float t2 = (-b - sqrt) / (2f * a);
                t = Mathf.Max(t1, t2);
                if (t < 0f) t = Mathf.Min(t1, t2);
                t = Mathf.Max(0f, t);
            }
        }
        Vector2 futurePos = targetPos + targetVel * (t * leadMult);
        return (futurePos - shooterPos).normalized;
    }
    #endregion
}

/// <summary>
/// Minimal 2D projectile with optional homing and fake ballistic arc.
/// Lives entirely in this same file for drop-in simplicity.
/// </summary>
public class Projectile2D : MonoBehaviour
{
    public enum HomingType { None, Strong, Soft }
    [HideInInspector] public HomingType homingType = HomingType.None;
    [HideInInspector] public float homingSteerStrength = 0f; // deg/s-ish
    [HideInInspector] public float homingSeekTime = 0f;
    [HideInInspector] public Transform homingTarget;

    [HideInInspector] public float ballisticArcAmount = 0f; // purely visual offset

    [HideInInspector] public float damage;
    [HideInInspector] public float lifetime;
    [HideInInspector] public float knockback;
    [HideInInspector] public bool destroyOnHit = true;
    [HideInInspector] public LayerMask bulletHitMask = ~0;

    public System.Action<Projectile2D> onDespawn;

    Rigidbody2D _rb;
    Vector2 _vel;
    float _timer;
    float _seekTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 velocity, float dmg, float life, float kb, bool killOnHit, LayerMask hitMask)
    {
        _vel = velocity;
        damage = dmg;
        lifetime = life;
        knockback = kb;
        destroyOnHit = killOnHit;
        bulletHitMask = hitMask;

        _timer = 0f;
        _seekTimer = homingSeekTime;
        if (_rb) _rb.linearVelocity = _vel;
    }

    void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;

        // Homing
        if (homingType != HomingType.None && homingTarget && _seekTimer > 0f)
        {
            _seekTimer -= Time.fixedDeltaTime;

            Vector2 to = (homingTarget.position - transform.position);
            if (to.sqrMagnitude > 0.001f)
            {
                float currentAngle = Mathf.Atan2(_vel.y, _vel.x) * Mathf.Rad2Deg;
                float targetAngle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(currentAngle, targetAngle);

                float maxTurn = homingSteerStrength * Time.fixedDeltaTime;
                float clamped = Mathf.Clamp(delta, -maxTurn, maxTurn);

                float newAngle = currentAngle + clamped;
                float speed = _vel.magnitude;
                _vel = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad)) * speed;
            }
        }

        if (_rb) _rb.linearVelocity = _vel;

        // Face velocity
        if (_vel.sqrMagnitude > 0.0001f)
        {
            float z = Mathf.Atan2(_vel.y, _vel.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, z);
        }

        // Lifetime
        if (_timer >= lifetime)
        {
            Despawn();
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & bulletHitMask) == 0) return;

        // Damage
        var d = other.GetComponent<IDamageable>();
        if (d != null) d.TakeDamage(damage);

        // Optional knockback if they have Rigidbody2D
        if (knockback > 0f)
        {
            var rb = other.attachedRigidbody;
            if (rb)
            {
                Vector2 dir = _vel.normalized;
                rb.AddForce(dir * knockback, ForceMode2D.Impulse);
            }
        }

        if (destroyOnHit) Despawn();
    }

    void Despawn()
    {
        onDespawn?.Invoke(this);
    }

    // (Optional) purely visual fake arc (debug visualization only)
    void OnDrawGizmosSelected()
    {
        if (ballisticArcAmount <= 0f) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, 0.12f + Mathf.Sin(Time.time * 8f) * ballisticArcAmount * 0.1f);
    }
}
