using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Interfaces & Helpers (kept in same file for single-drop workflow)
public interface IDamageable { void TakeDamage(float amount); }
#endregion

/// <summary>
/// EnemyShooter: single drop-in 2D shooter for bullet-hell enemies.
/// - Modes: Single, Shotgun, N-Way, Minigun (spin-up + overheat), Spiral, Homing (missile), Soft-homing (javelin), Ballistic (visual)
/// - Predictive lead, aim smoothing, burst cadence, telegraph line
/// - Internal pool (GameObject-based) + safe reuse (no destroyed refs)
/// - Accepts ANY bullet prefab (no auto-sprite drawing)
/// - Automatically includes target (player) layer in bullet hit mask so bullets can hit the player
/// </summary>
[DisallowMultipleComponent]
public class EnemyShooter : MonoBehaviour
{
    [Header("=== References ===")]
    public Transform player;
    public Rigidbody2D playerRb;
    public Transform muzzle;
    [Tooltip("Assign ANY bullet prefab (will get/ensure Rigidbody2D + Trigger Collider2D + Projectile2D).")]
    public GameObject bulletPrefab;

    [Header("=== Global Toggles ===")]
    public bool enableFiring = true;
    public bool autoRotateToTarget = true;
    public enum FireBlend { Sequential, Simultaneous, Random }
    public FireBlend multiModeBlend = FireBlend.Sequential;

    [Header("=== Difficulty & Upgrades ===")]
    public float globalFireRateMult = 1f;
    public float globalBulletSpeedMult = 1f;
    public float globalDamageMult = 1f;
    public float upgradeMult = 1f;

    [Header("=== Pooling & Limits ===")]
    public bool usePooling = true;
    public int poolWarmup = 32;
    public int maxLiveBullets = 0;
    public bool reuseOldestWhenCapped = true;
    
    public enum BurstMode { Single, Burst3, Burst5, Custom }

    [Header("=== Fire Cadence ===")]
    public BurstMode burstMode = BurstMode.Single;
    public int customBurstCount = 4;
    public float intraBurstDelay = 0.08f;
    public float postBurstPause = 0.6f;
    public float postBurstPauseJitter = 0.0f;

    public enum AimMode { AimAtPlayer, PredictiveLead, FixedDirection, Sweep, RotateTurret }

    [Header("=== Aim & Lead ===")]
    public AimMode aimMode = AimMode.AimAtPlayer;
    public float leadMultiplier = 1.0f;
    public float aimInaccuracyDegrees = 0f;
    public float fixedAngleDegrees = 0f;
    public float rotateDegreesPerSecond = 60f;

    [Header("=== Aim Line Telegraph ===")]
    public bool showAimLine = true;
    public float lineWidthTracking = 0.025f;
    public float lineWidthFiring = 0.08f;
    public float aimLineLength = 12f;
    public float boldDuration = 0.08f;
    public Color aimLineColor = new Color(1f, 0.3f, 0.1f, 0.9f);
    LineRenderer _line;

    [Header("=== Base Bullet Settings ===")]
    public float baseFireRate = 2.0f;
    public float baseBulletSpeed = 7.5f;
    public float baseDamage = 5f;
    public float baseLifetime = 6f;
    public float baseKnockback = 0f;
    public LayerMask bulletHitMask = ~0;   // default: everything
    public bool destroyOnHit = true;

    [Header("=== Modes: Toggle & Settings ===")]
    public bool modeSingle = true;

    [Space(6)]
    public bool modeShotgun = false;
    public int shotgunPellets = 5;
    public float shotgunConeAngle = 24f;

    [Space(6)]
    public bool modeMultiNWay = false;
    public int nWayCount = 3;
    public float nWayStepDegrees = 8f;

    [Space(6)]
    public bool modeMinigun = false;
    public float minigunSpinUp = 0.75f;
    public float minigunMinRate = 4f;
    public float minigunFireRateMult = 3f;

    [Header("Overheat (Minigun)")]
    public bool useOverheat = true;
    public float heatPerShot = 2f;
    public float heatMax = 100f;
    public float heatCoolPerSecond = 20f;
    public float overheatLockSeconds = 1.0f;
    public bool overheatAffectsAllModes = false;
    float _heat, _overheatLockTimer;

    [Space(6)]
    public bool modeSpiral = false;
    public float spiralStepDegrees = 12f;
    float _spiralAngle;

    [Space(6)]
    public bool modeHomingMissile = false;
    public float homingSteerStrength = 720f;
    public float homingSeekTime = 2.5f;

    [Space(6)]
    public bool modeSoftHomingJavelin = false;
    public float softHomingCurveStrength = 180f;
    public float softHomingDuration = 0.6f;

    [Header("=== Optional: Ballistic (top-down flavor) ===")]
    public bool modeBallistic = false;
    [Range(0f, 1f)] public float ballisticArcAmount = 0.0f;

    // Private state
    Rigidbody2D _rb;
    float _nextCycleTime;
    float _localAimAngleDeg;
    float _spinupLerp;
    readonly Queue<GameObject> _pool = new Queue<GameObject>();
    readonly LinkedList<Projectile2D> _live = new LinkedList<Projectile2D>();
    System.Random _rng;
    bool _cycleRunning;

    // Aim smoothing
    Vector2 _smoothedAimDir = Vector2.right;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rng = new System.Random();
        if (!muzzle) muzzle = this.transform;

        if (showAimLine) _line = GetOrCreateLineRenderer();
        if (!bulletPrefab)
            Debug.LogError($"[{name}] No bulletPrefab assigned. Please assign a GameObject prefab in the inspector.");

        // Warm pool
        if (usePooling && bulletPrefab)
        {
            for (int i = 0; i < Mathf.Max(0, poolWarmup); i++)
            {
                var go = Instantiate(bulletPrefab);
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }

        _localAimAngleDeg = fixedAngleDegrees;
    }

    void Update()
    {
        if (useOverheat)
        {
            if (_overheatLockTimer > 0f) _overheatLockTimer -= Time.deltaTime;
            _heat = Mathf.Max(0f, _heat - heatCoolPerSecond * Time.deltaTime);
        }
        if (showAimLine && _line) UpdateAimLine();
    }

    void FixedUpdate()
    {
        if (!enableFiring) return;
        if (_overheatLockTimer > 0f) return;

        if (!_cycleRunning && Time.time >= _nextCycleTime)
        {
            _cycleRunning = true;
            _nextCycleTime = Time.time + 999f;
            StartCoroutine(FireCycle());
        }
    }

    IEnumerator FireCycle()
    {
        int shots = burstMode switch
        {
            BurstMode.Single => 1,
            BurstMode.Burst3 => 3,
            BurstMode.Burst5 => 5,
            BurstMode.Custom => Mathf.Max(1, customBurstCount),
            _ => 1
        };

        float cadenceRate = GetEffectiveFireRateForCadence();

        for (int i = 0; i < shots; i++)
        {
            float delay = (i == 0) ? 0f : intraBurstDelay;

            yield return StartCoroutine(FireOnceBlend());

            bool applyHeat = useOverheat && (modeMinigun || overheatAffectsAllModes);
            if (applyHeat && heatPerShot > 0f)
            {
                _heat += heatPerShot;
                if (_heat >= heatMax)
                {
                    _overheatLockTimer = overheatLockSeconds;
                    _heat = heatMax * 0.65f;
                    break;
                }
            }

            if (showAimLine && _line) StartCoroutine(BoldLineFor(boldDuration));
            if (delay > 0f) yield return new WaitForSeconds(delay);
        }

        float pause = Mathf.Max(0f, postBurstPause + (float)(_rng.NextDouble() * 2 - 1) * postBurstPauseJitter);
        float cycleGap = (cadenceRate > 0f) ? (1f / cadenceRate) : 0.5f;
        _nextCycleTime = Time.time + pause + cycleGap;
        _cycleRunning = false;
    }

    float GetEffectiveFireRateForCadence()
    {
        float rate = baseFireRate;
        if (modeMinigun)
        {
            _spinupLerp = Mathf.Clamp01(_spinupLerp + Time.fixedDeltaTime / Mathf.Max(0.0001f, minigunSpinUp));
            float minRate = Mathf.Max(0.01f, minigunMinRate);
            float maxRate = Mathf.Max(minRate, baseFireRate * minigunFireRateMult);
            rate = Mathf.Lerp(minRate, maxRate, _spinupLerp);
        }
        rate *= Mathf.Max(0.01f, globalFireRateMult) * Mathf.Max(0.01f, upgradeMult);
        return rate;
    }

    IEnumerator FireOnceBlend()
    {
        switch (multiModeBlend)
        {
            case FireBlend.Simultaneous: yield return StartCoroutine(FireAllEnabledOnceSimultaneous()); break;
            case FireBlend.Random:       yield return StartCoroutine(FireOneRandomEnabled()); break;
            case FireBlend.Sequential:
            default:                     yield return StartCoroutine(FireNextEnabledSequential()); break;
        }
    }

    int _seqIndex;
    IEnumerator FireNextEnabledSequential()
    {
        var enabledList = GetEnabledModeIndices();
        if (enabledList.Count == 0) yield break;
        _seqIndex = (_seqIndex + 1) % enabledList.Count;
        yield return StartCoroutine(FireByIndex(enabledList[_seqIndex]));
    }

    IEnumerator FireOneRandomEnabled()
    {
        var enabledList = GetEnabledModeIndices();
        if (enabledList.Count == 0) yield break;
        int pick = _rng.Next(enabledList.Count);
        yield return StartCoroutine(FireByIndex(enabledList[pick]));
    }

    IEnumerator FireAllEnabledOnceSimultaneous()
    {
        var enabledList = GetEnabledModeIndices();
        foreach (int idx in enabledList) StartCoroutine(FireByIndex(idx));
        yield return null;
    }

    List<int> GetEnabledModeIndices()
    {
        var list = new List<int>(8);
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
    Vector2 GetAimDirection()
    {
        Vector3 muzzlePos = muzzle ? muzzle.position : transform.position;
        Vector2 desiredDir = Vector2.right;

        switch (aimMode)
        {
            case AimMode.AimAtPlayer:
                if (player) desiredDir = ((Vector2)player.position - (Vector2)muzzlePos).normalized;
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

        // smoothing / forgiveness
        float forgiveness = 0.25f;
        _smoothedAimDir = Vector2.Lerp(_smoothedAimDir, desiredDir, 1f - Mathf.Exp(-Time.deltaTime / forgiveness));
        Vector2 dir = _smoothedAimDir.normalized;

        // random inaccuracy
        float noise = (float)(_rng.NextDouble() * 2 - 1) * aimInaccuracyDegrees;
        dir = Rotate(dir, noise);

        // rotate body
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

    #region Pooling / Spawn (hardened against destroyed refs)
    void PruneDeadLive()
    {
        var node = _live.First;
        while (node != null)
        {
            var next = node.Next;
            if (node.Value == null || node.Value.gameObject == null)
                _live.Remove(node);
            node = next;
        }
    }

    Projectile2D SpawnBullet(Vector2 dir, float speed, float dmg)
    {
        if (!bulletPrefab) return null;

        // Always clean live list to avoid destroyed refs
        PruneDeadLive();

        // Optional max cap
        if (maxLiveBullets > 0 && _live.Count >= maxLiveBullets)
        {
            if (reuseOldestWhenCapped && _live.Count > 0)
            {
                // find first valid oldest
                Projectile2D oldest = null;
                while (_live.Count > 0 && (oldest == null || oldest.gameObject == null))
                {
                    oldest = _live.First.Value;
                    _live.RemoveFirst();
                }

                if (oldest != null && oldest.gameObject != null)
                {
                    if (!oldest.gameObject.activeSelf) oldest.gameObject.SetActive(true);
                    ReinitializeBullet(oldest, dir, speed, dmg);
                    _live.AddLast(oldest);
                    return oldest;
                }
                // fall through to fresh spawn
            }
            else
            {
                return null;
            }
        }

        // Get pooled or instantiate
        GameObject go = null;
        if (usePooling && _pool.Count > 0)
        {
            // pull first non-null
            while (_pool.Count > 0 && (go == null || go == null))
                go = _pool.Dequeue();
            if (go == null) go = Instantiate(bulletPrefab);
        }
        else
        {
            go = Instantiate(bulletPrefab);
        }

        // Ensure components
        var proj = go.GetComponent<Projectile2D>();
        if (!proj) proj = go.AddComponent<Projectile2D>();

        var rb2d = go.GetComponent<Rigidbody2D>();
        if (!rb2d)
        {
            rb2d = go.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 0f;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb2d.linearDamping = 0f;
            rb2d.angularDamping = 0f;
        }

        var col = go.GetComponent<Collider2D>();
        if (!col)
        {
            var cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        go.SetActive(true);

        // Hook despawn safely (single-fire)
        proj.onDespawn = (p) =>
        {
            if (p == null || p.gameObject == null) return;

            var node = _live.Find(p);
            if (node != null) _live.Remove(node);

            if (usePooling)
            {
                var g = p.gameObject;
                g.SetActive(false);
                _pool.Enqueue(g);
            }
            else
            {
                Destroy(p.gameObject);
            }
        };

        ReinitializeBullet(proj, dir, speed, dmg);
        _live.AddLast(proj);
        return proj;
    }

    void ReinitializeBullet(Projectile2D b, Vector2 dir, float speed, float dmg)
    {
        if (b == null || b.gameObject == null) return; // destroyed? bail

        // Always include player's current layer so bullets can hit the player
        if (player) bulletHitMask |= (1 << player.gameObject.layer);

        Vector3 pos = muzzle ? muzzle.position : transform.position;

        var tr = b.transform; // could be MissingRef if destroyed
        if (tr == null) return;

        tr.position = pos;
        tr.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

        float finalSpeed = speed * Mathf.Max(0.01f, globalBulletSpeedMult) * Mathf.Max(0.01f, upgradeMult);
        float finalDmg = dmg   * Mathf.Max(0.01f, globalDamageMult)   * Mathf.Max(0.01f, upgradeMult);

        b.homingTarget = null; // will be set by FireHoming variants
        b.Init(dir.normalized * finalSpeed,
               finalDmg,
               baseLifetime,
               baseKnockback,
               destroyOnHit,
               bulletHitMask);
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

/// <summary> Minimal 2D projectile with optional homing. </summary>
public class Projectile2D : MonoBehaviour
{
    public enum HomingType { None, Strong, Soft }
    [HideInInspector] public HomingType homingType = HomingType.None;
    [HideInInspector] public float homingSteerStrength = 0f;
    [HideInInspector] public float homingSeekTime = 0f;
    [HideInInspector] public Transform homingTarget;

    [HideInInspector] public float ballisticArcAmount = 0f;

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

    void Awake() { _rb = GetComponent<Rigidbody2D>(); }

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

            Vector2 to = (Vector2)(homingTarget.position - transform.position);
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
        // Respect hit mask
        if (((1 << other.gameObject.layer) & bulletHitMask) == 0) return;

        // Damage
        var d = other.GetComponent<IDamageable>();
        if (d != null) d.TakeDamage(damage);

        // Knockback
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
        var cb = onDespawn;   // make idempotent
        onDespawn = null;
        cb?.Invoke(this);
    }
}
