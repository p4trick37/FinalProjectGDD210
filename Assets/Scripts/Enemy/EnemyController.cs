using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Movement and Rotation")]
    public float movementSpeed = 0f;
    public float rotationSpeed = 180f;
    public bool faceTarget = true;

    [Header("Bullet Speed and Spread")]
    public float bulletSpeed = 8f;
    public float bulletSpread = 8f;

    [Header("Delays for Firing Weapons")]
    public float semiAutoDelay = 0.35f;
    public float autoDelay = 0.08f;
    public float shotgunDelay = 0.8f;
    public float burstShotSpacing = 0.06f;

    [Header("Object References")]
    public Transform turret;
    public Transform muzzle;
    public GameObject bulletPrefab;
    public Transform target;
    public string playerTag = "Player";

    [Header("Weapon List and its Current Weapon")]
    public string[] weapons = new string[] { "SemiAuto", "FullAuto", "3RoundShot" };
    public int currentWeapon = 0;

    private bool[] shootCurrentWeapon = new bool[3];
    private float semiAutoTimerDelay;
    private float autoTimerDelay;
    private float shotgunTimerDelay;
    private bool canSemiShoot = true;
    private bool canAutoShoot = true;
    private bool canShotgunShoot = true;

    // NEW: cached player health so we can stop shooting when dead
    private PlayerHealth playerHealth;

    void Awake()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) target = p.transform;
        }
        if (!muzzle) muzzle = transform;

        if (target != null)
        {
            playerHealth = target.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        // NEW: if player is dead, stop everything and do not shoot
        if (playerHealth != null && playerHealth.IsDead)
        {
            shootCurrentWeapon[0] = shootCurrentWeapon[1] = shootCurrentWeapon[2] = false;
            return;
        }

        if (movementSpeed > 0f)
        {
            transform.position += transform.up * (movementSpeed * Time.deltaTime);
        }

        if (autoTimerDelay > 0f) { autoTimerDelay -= Time.deltaTime; canAutoShoot = false; } else canAutoShoot = true;
        if (semiAutoTimerDelay > 0f) { semiAutoTimerDelay -= Time.deltaTime; canSemiShoot = false; } else canSemiShoot = true;
        if (shotgunTimerDelay > 0f) { shotgunTimerDelay -= Time.deltaTime; canShotgunShoot = false; } else canShotgunShoot = true;

        float aimAngle = GetAngleToTarget();
        if (faceTarget)
        {
            float currentZ = transform.eulerAngles.z;
            float newZ = Mathf.MoveTowardsAngle(currentZ, aimAngle - 90f, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newZ);
        }
        if (turret)
        {
            turret.rotation = Quaternion.Euler(0, 0, aimAngle);
        }

        switch (currentWeapon)
        {
            case 0:
                if (canSemiShoot) shootCurrentWeapon[currentWeapon] = true;
                break;
            case 1:
                shootCurrentWeapon[currentWeapon] = true;
                break;
            case 2:
                if (canShotgunShoot) shootCurrentWeapon[currentWeapon] = true;
                break;
        }
    }

    void FixedUpdate()
    {
        // NEW: also bail out of FixedUpdate if player is dead
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }

        switch (currentWeapon)
        {
            case 0:
                if (shootCurrentWeapon[currentWeapon] && canSemiShoot)
                {
                    ShootSingleBullet();
                    semiAutoTimerDelay = semiAutoDelay;
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;

            case 1:
                if (shootCurrentWeapon[currentWeapon] && canAutoShoot)
                {
                    ShootSingleBullet();
                    autoTimerDelay = autoDelay;
                }
                break;

            case 2:
                if (shootCurrentWeapon[currentWeapon] && canShotgunShoot)
                {
                    StartCoroutine(CoShoot3Bullets());
                    shotgunTimerDelay = shotgunDelay;
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;
        }
    }

    // ======= shooting helpers (unchanged except for rotation already added) =======

    private void ShootSingleBullet()
    {
        Vector2 dir = DirToTargetNormalized();
        if (dir == Vector2.zero) dir = transform.up;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyShoot();
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        GameObject bullet = Instantiate(
            bulletPrefab ? bulletPrefab : MakeFallbackBullet(),
            muzzle ? muzzle.position : transform.position,
            Quaternion.Euler(0, 0, angle)
        );

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (!rb) rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.AddForce(dir * bulletSpeed, ForceMode2D.Impulse);
    }

    private IEnumerator CoShoot3Bullets()
    {
        ShootSingleBullet();
        yield return new WaitForSeconds(burstShotSpacing);
        ShootWithAngleOffset(+bulletSpread);
        yield return new WaitForSeconds(burstShotSpacing);
        ShootWithAngleOffset(-bulletSpread);
    }

    private void ShootWithAngleOffset(float offsetDegrees)
    {
        Vector2 dir = DirToTargetNormalized();
        if (dir == Vector2.zero) dir = transform.up;
        dir = Rotate(dir, offsetDegrees);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        GameObject bullet = Instantiate(
            bulletPrefab ? bulletPrefab : MakeFallbackBullet(),
            muzzle ? muzzle.position : transform.position,
            Quaternion.Euler(0, 0, angle)
        );

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (!rb) rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.AddForce(dir * bulletSpeed, ForceMode2D.Impulse);
    }

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
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    private GameObject MakeFallbackBullet()
    {
        var go = new GameObject("EnemyBullet_Fallback");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeDotSprite();
        sr.sortingOrder = 50;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = false;
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
            px[y * s + x] = d2 <= r2
                ? new Color32(240, 100, 40, 255)
                : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
}
