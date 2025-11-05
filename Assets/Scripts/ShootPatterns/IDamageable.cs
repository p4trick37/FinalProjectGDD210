using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable { void TakeDamage(float amount); }

/// <summary>
/// Base class: pooling, aim, spawn, telegraph line.
/// Accepts ANY bullet prefab (must have/gets Rigidbody2D + Trigger Collider2D + Projectile2D).
/// No overheat or homing. Derived classes only implement FirePattern().
/// </summary>
[DisallowMultipleComponent]
public abstract class ShooterBase2D : MonoBehaviour
{
    [Header("=== References ===")]
    public Transform player;
    public Rigidbody2D playerRb;
    public Transform muzzle;
    [Tooltip("Any bullet GameObject prefab. This script will ensure Rigidbody2D + Trigger Collider2D + Projectile2D exist at runtime.")]
    public GameObject bulletPrefab;

    [Header("=== Firing ===")]
    public bool enableFiring = true;
    [Tooltip("Shots per second for non-burst patterns (or base cadence for bursts).")]
    public float fireRate = 2f;

    [Header("=== Bullet ===")]
    public float bulletSpeed = 8f;
    public float damage = 5f;
    public float lifetime = 6f;
    public bool destroyOnHit = true;
    public LayerMask hitMask = ~0;

    [Header("=== Aim ===")]
    public bool autoRotateToTarget = true;
    public AimMode aimMode = AimMode.AimAtPlayer;
    public enum AimMode { AimAtPlayer, PredictiveLead, FixedDirection, Sweep }
    public float leadMultiplier = 1f;
    public float fixedAngleDegrees = 0f;
    public float sweepDegreesPerSecond = 60f;
    public float aimInaccuracyDegrees = 0f;
    [Tooltip("Aim smoothing time constant (seconds). Larger = smoother/forgiving.")]
    public float aimForgiveness = 0.25f;

    [Header("=== Telegraph (optional) ===")]
    public bool showAimLine = false;
    public float aimLineLength = 12f;
    public float lineWidthTracking = 0.025f;
    public float lineWidthFiring = 0.08f;
    public float boldDuration = 0.08f;
    public Color aimLineColor = new Color(1f, 0.3f, 0.1f, 0.9f);

    [Header("=== Pooling ===")]
    public bool usePooling = true;
    public int poolWarmup = 16;
    public int maxLiveBullets = 0;             // 0 = unlimited
    public bool reuseOldestWhenCapped = true;

    // --- private/state ---
    protected System.Random _rng;
    protected float _sweepAngle;
    protected Vector2 _smoothedAim = Vector2.right;
    protected float _nextShotTime;
    readonly Queue<GameObject> _pool = new Queue<GameObject>();
    readonly LinkedList<Projectile2D> _live = new LinkedList<Projectile2D>();
    LineRenderer _line;

    protected virtual void Awake()
    {
        _rng = new System.Random();
        if (!muzzle) muzzle = transform;

        if (showAimLine)
        {
            _line = GetOrCreateLineRenderer();
        }

        if (!bulletPrefab)
        {
            Debug.LogError($"[{name}] No bulletPrefab assigned on {GetType().Name}.");
        }

        if (usePooling && bulletPrefab)
        {
            for (int i = 0; i < Mathf.Max(0, poolWarmup); i++)
            {
                var go = Instantiate(bulletPrefab);
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }

        _sweepAngle = fixedAngleDegrees;
        _nextShotTime = Time.time;
    }

    protected virtual void Update()
    {
        if (showAimLine && _line) UpdateAimLine();
    }

    protected virtual void OnEnable()
    {
        StartCoroutine(FireLoop());
    }

    IEnumerator FireLoop()
    {
        while (enabled)
        {
            if (!enableFiring || bulletPrefab == null)
            {
                yield return null;
                continue;
            }

            if (Time.time >= _nextShotTime)
            {
                yield return FirePattern(); // derived pattern handles bursts & delays, must set _nextShotTime
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary> Derived classes implement: must set _nextShotTime when done. </summary>
    protected abstract IEnumerator FirePattern();

    // --- Common helpers ---

    protected Vector2 GetAimDirection()
    {
        Vector3 origin = muzzle ? muzzle.position : transform.position;
        Vector2 desired = Vector2.right;

        switch (aimMode)
        {
            case AimMode.AimAtPlayer:
                if (player) desired = ((Vector2)player.position - (Vector2)origin).normalized;
                break;

            case AimMode.PredictiveLead:
                if (player)
                {
                    Vector2 targetPos = player.position;
                    Vector2 targetVel = (playerRb ? playerRb.linearVelocity : Vector2.zero);
                    float s = bulletSpeed;
                    desired = PredictInterceptDirection((Vector2)origin, targetPos, targetVel, s, leadMultiplier);
                }
                break;

            case AimMode.FixedDirection:
                desired = Deg2Dir(fixedAngleDegrees);
                break;

            case AimMode.Sweep:
                _sweepAngle += sweepDegreesPerSecond * Time.deltaTime;
                desired = Deg2Dir(_sweepAngle);
                break;
        }

        // Smoothing/forgiveness
        _smoothedAim = Vector2.Lerp(_smoothedAim, desired, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, aimForgiveness)));
        Vector2 dir = _smoothedAim.normalized;

        // Mild inaccuracy
        float jitter = (float)(_rng.NextDouble() * 2 - 1) * aimInaccuracyDegrees;
        dir = Rotate(dir, jitter);

        // Rotate body
        if (autoRotateToTarget)
        {
            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, z - 90f), 0.2f);
        }

        return dir;
    }

    protected void TelegraphBoldTick()
    {
        if (!showAimLine || _line == null) return;
        StartCoroutine(BoldLineFor(boldDuration));
    }

    protected float CadenceSeconds() => (fireRate > 0f) ? (1f / fireRate) : 0.5f;

    // --- Bullet spawning / pooling ---

    protected Projectile2D SpawnBullet(Vector2 dir)
    {
        // Ensure player's layer is hit
        if (player) hitMask |= (1 << player.gameObject.layer);

        // Cap live
        PruneDeadLive();
        if (maxLiveBullets > 0 && _live.Count >= maxLiveBullets)
        {
            if (reuseOldestWhenCapped && _live.Count > 0)
            {
                var oldest = PopValidOldest();
                if (oldest != null) return ReinitProjectile(oldest, dir);
                // else fall through to fresh spawn
            }
            else
            {
                return null;
            }
        }

        GameObject go = null;
        if (usePooling && _pool.Count > 0)
        {
            while (_pool.Count > 0 && go == null) go = _pool.Dequeue();
        }
        if (go == null) go = Instantiate(bulletPrefab);

        var proj = EnsureProjectileStack(go);
        go.SetActive(true);

        // hook despawn
        proj.onDespawn = (p) =>
        {
            var node = _live.Find(p);
            if (node != null) _live.Remove(node);

            if (usePooling)
            {
                p.gameObject.SetActive(false);
                _pool.Enqueue(p.gameObject);
            }
            else
            {
                Destroy(p.gameObject);
            }
        };

        _live.AddLast(proj);
        return ReinitProjectile(proj, dir);
    }

    Projectile2D EnsureProjectileStack(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
        }

        var col = go.GetComponent<Collider2D>();
        if (!col)
        {
            var cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
        }
        else col.isTrigger = true;

        var proj = go.GetComponent<Projectile2D>();
        if (!proj) proj = go.AddComponent<Projectile2D>();
        return proj;
    }

    Projectile2D ReinitProjectile(Projectile2D p, Vector2 dir)
    {
        if (p == null || p.gameObject == null) return null;

        var pos = muzzle ? muzzle.position : transform.position;
        p.transform.position = pos;
        p.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

        p.Init(dir.normalized * bulletSpeed, damage, lifetime, 0f, destroyOnHit, hitMask);
        return p;
    }

    void PruneDeadLive()
    {
        var node = _live.First;
        while (node != null)
        {
            var next = node.Next;
            if (node.Value == null || node.Value.gameObject == null) _live.Remove(node);
            node = next;
        }
    }

    Projectile2D PopValidOldest()
    {
        while (_live.Count > 0)
        {
            var oldest = _live.First.Value;
            _live.RemoveFirst();
            if (oldest != null && oldest.gameObject != null) return oldest;
        }
        return null;
    }

    // --- Aim line ---
    void UpdateAimLine()
    {
        if (_line == null) return;
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
        if (_line == null) yield break;
        float saved = _line.startWidth;
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

    // --- Math ---
    protected static Vector2 Deg2Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)).normalized;
    protected static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }
    protected static Vector2 PredictInterceptDirection(Vector2 shooterPos, Vector2 targetPos, Vector2 targetVel, float projSpeed, float leadMult)
    {
        Vector2 toT = targetPos - shooterPos;
        float a = Vector2.Dot(targetVel, targetVel) - projSpeed * projSpeed;
        float b = 2f * Vector2.Dot(toT, targetVel);
        float c = Vector2.Dot(toT, toT);
        float t;
        if (Mathf.Abs(a) < 1e-4f)
        {
            t = -c / Mathf.Max(1e-4f, b);
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
        Vector2 future = targetPos + targetVel * (t * leadMult);
        return (future - shooterPos).normalized;
    }
}
