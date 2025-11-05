using UnityEngine;

public class EnemyShooterSimple : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;           // optional; auto-find by tag if null
    public Transform muzzle;           // optional; fires from this if set
    public GameObject bulletPrefab;    // optional; fallback bullet created at runtime if null

    [Header("Fire")]
    public float fireRate = 1.5f;      // shots per second
    public float bulletSpeed = 8f;
    public float bulletLifetime = 4f;

    [Header("Aim")]
    public bool faceTarget = true;     // rotate enemy to face player
    public float aimSmooth = 0.15f;    // 0 = instant, higher = smoother

    float _nextFire;
    Vector2 _smoothedDir = Vector2.right;

    void Awake()
    {
        // Auto-find player by tag if not assigned
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!muzzle) muzzle = transform;
    }

    void Update()
    {
        if (!player) return;

        // Smooth aim
        Vector2 desired = ((Vector2)player.position - (Vector2)muzzle.position).normalized;
        float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, aimSmooth));
        _smoothedDir = Vector2.Lerp(_smoothedDir, desired, k);
        Vector2 dir = (_smoothedDir.sqrMagnitude > 0.0001f) ? _smoothedDir.normalized : Vector2.right;

        // Rotate body to face
        if (faceTarget)
        {
            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, z);
        }

        // Fire
        if (Time.time >= _nextFire)
        {
            _nextFire = Time.time + 1f / Mathf.Max(0.01f, fireRate);
            Shoot(dir);
        }
    }

    void Shoot(Vector2 dir)
    {
        GameObject bullet = bulletPrefab ? Instantiate(bulletPrefab) : CreateFallbackBullet();
        bullet.transform.position = muzzle ? muzzle.position : transform.position;

        // Make sure bullet can move
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (!rb) rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = dir * bulletSpeed;

        // Optional lifetime destroy
        Destroy(bullet, bulletLifetime);

        // Ensure it can hit things
        if (!bullet.TryGetComponent<Collider2D>(out var col))
            col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true; // keep it simple
    }

    GameObject CreateFallbackBullet()
    {
        var go = new GameObject("Bullet_Fallback");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeDotSprite();
        sr.sortingOrder = 50;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        return go;
    }

    Sprite MakeDotSprite()
    {
        int s = 16;
        var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        var px = new Color32[s * s];
        Vector2 c = new Vector2(s / 2f, s / 2f);
        float r2 = (s * 0.35f) * (s * 0.35f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            var d2 = ((new Vector2(x, y) - c).sqrMagnitude);
            px[y * s + x] = d2 <= r2 ? new Color32(255, 120, 60, 255) : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), 100f);
    }
}
