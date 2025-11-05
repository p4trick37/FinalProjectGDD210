using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Movement and Rotation")]
    public float movementSpeed = 0f;          // optional, set >0 if you want drift/move
    public float rotationSpeed = 180f;        // deg/sec for body rotate-to-target
    public bool faceTarget = true;            // rotate the body to face target

    [Header("Bullet Speed and Spread")]
    public float bulletSpeed = 8f;
    public float bulletSpread = 8f;           // degrees for left/right spread on 3RoundShot

    [Header("Delays for Firing Weapons")]
    public float semiAutoDelay = 0.35f;       // cooldown per single shot
    public float autoDelay = 0.08f;           // cadence for full auto
    public float shotgunDelay = 0.8f;         // re-arm delay for 3Shot burst
    public float burstShotSpacing = 0.06f;    // time between the 3 shots in burst

    [Header("Object References")]
    public Transform turret;                  // optional pivot for visual rotate
    public Transform muzzle;                  // optional spawn point; falls back to transform
    public GameObject bulletPrefab;           // assign your bullet prefab (with or without RB2D)
    public Transform target;                  // usually the player; auto-found by tag if null
    public string playerTag = "Player";       // tag used to auto-find target

    [Header("Weapon List and its Current Weapon")]
    public string[] weapons = new string[] { "SemiAuto", "FullAuto", "3RoundShot" };
    public int currentWeapon = 0;

    // internal “input-style” flags to mirror your PlayerController pattern
    private bool[] shootCurrentWeapon = new bool[3];
    private float semiAutoTimerDelay;
    private float autoTimerDelay;
    private float shotgunTimerDelay;
    private bool canSemiShoot = true;
    private bool canAutoShoot = true;
    private bool canShotgunShoot = true;

    void Awake()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) target = p.transform;
        }
        if (!muzzle) muzzle = transform;
    }

    void Update()
    {
        // Optional very simple movement (straight forward)
        if (movementSpeed > 0f)
        {
            transform.position += transform.up * (movementSpeed * Time.deltaTime);
        }

        // Timers
        if (autoTimerDelay > 0f) { autoTimerDelay -= Time.deltaTime; canAutoShoot = false; } else canAutoShoot = true;
        if (semiAutoTimerDelay > 0f) { semiAutoTimerDelay -= Time.deltaTime; canSemiShoot = false; } else canSemiShoot = true;
        if (shotgunTimerDelay > 0f) { shotgunTimerDelay -= Time.deltaTime; canShotgunShoot = false; } else canShotgunShoot = true;

        // Aim at target
        float aimAngle = GetAngleToTarget(); // degrees
        if (faceTarget)
        {
            // rotate enemy body toward target
            float currentZ = transform.eulerAngles.z;
            float newZ = Mathf.MoveTowardsAngle(currentZ, aimAngle - 90f, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newZ);
        }
        if (turret)
        {
            // rotate turret exactly toward target (snaps)
            turret.rotation = Quaternion.Euler(0, 0, aimAngle);
        }

        // Simple AI trigger, mirroring your “input → flags” flow:
        switch (currentWeapon)
        {
            case 0: // SemiAuto
                if (canSemiShoot) shootCurrentWeapon[currentWeapon] = true;
                break;

            case 1: // FullAuto
                // hold the trigger; cadence gated by autoTimerDelay in FixedUpdate
                shootCurrentWeapon[currentWeapon] = true;
                break;

            case 2: // 3RoundShot
                if (canShotgunShoot) shootCurrentWeapon[currentWeapon] = true;
                break;
        }
    }

    void FixedUpdate()
    {
        switch (currentWeapon)
        {
            case 0: // SemiAuto
                if (shootCurrentWeapon[currentWeapon] && canSemiShoot)
                {
                    ShootSingleBullet();
                    semiAutoTimerDelay = semiAutoDelay;
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;

            case 1: // FullAuto
                if (shootCurrentWeapon[currentWeapon] && canAutoShoot)
                {
                    ShootSingleBullet();
                    autoTimerDelay = autoDelay;
                }
                break;

            case 2: // 3RoundShot
                if (shootCurrentWeapon[currentWeapon] && canShotgunShoot)
                {
                    StartCoroutine(CoShoot3Bullets());
                    shotgunTimerDelay = shotgunDelay;
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;
        }
    }

    // ========= Shooting helpers (same style as your PlayerController) =========

    private void ShootSingleBullet()
    {
        Vector2 dir = DirToTargetNormalized();
        if (dir == Vector2.zero) dir = transform.up;

        GameObject bullet = Instantiate(bulletPrefab ? bulletPrefab : MakeFallbackBullet(),
                                        muzzle ? muzzle.position : transform.position,
                                        Quaternion.identity);

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (!rb) rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.AddForce(dir * bulletSpeed, ForceMode2D.Impulse);
    }

    private IEnumerator CoShoot3Bullets()
    {
        // center
        ShootSingleBullet();

        // +spread
        yield return new WaitForSeconds(burstShotSpacing);
        ShootWithAngleOffset(+bulletSpread);

        // -spread
        yield return new WaitForSeconds(burstShotSpacing);
        ShootWithAngleOffset(-bulletSpread);
    }

    private void ShootWithAngleOffset(float offsetDegrees)
    {
        Vector2 dir = DirToTargetNormalized();
        if (dir == Vector2.zero) dir = transform.up;
        dir = Rotate(dir, offsetDegrees);

        GameObject bullet = Instantiate(bulletPrefab ? bulletPrefab : MakeFallbackBullet(),
                                        muzzle ? muzzle.position : transform.position,
                                        Quaternion.identity);

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (!rb) rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.AddForce(dir * bulletSpeed, ForceMode2D.Impulse);
    }

    // ========= Aiming convenience =========

    private Vector2 DirToTargetNormalized()
    {
        if (!target) return Vector2.zero;
        Vector2 to = (Vector2)target.position - (Vector2)(muzzle ? muzzle.position : transform.position);
        if (to.sqrMagnitude < 0.0001f) return Vector2.zero;
        return to.normalized;
    }

    private float GetAngleToTarget()
    {
        Vector2 dir = DirToTargetNormalized();
        if (dir == Vector2.zero) dir = transform.up;
        // our sprites face +Y; return an angle compatible with your PlayerController
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    // ========= tiny fallback bullet so you can test instantly =========
    private GameObject MakeFallbackBullet()
    {
        var go = new GameObject("EnemyBullet_Fallback");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeDotSprite();
        sr.sortingOrder = 50;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = false; // use physics if you want collisions to bounce; set true if triggers
        return go;
    }

    private Sprite MakeDotSprite()
    {
        int s = 16;
        var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        var px = new Color32[s * s];
        Vector2 c = new Vector2(s / 2f, s / 2f);
        float r2 = (s * 0.35f) * (s * 0.35f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float d2 = ((new Vector2(x, y) - c).sqrMagnitude);
            px[y * s + x] = d2 <= r2 ? new Color32(240, 100, 40, 255) : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
}
