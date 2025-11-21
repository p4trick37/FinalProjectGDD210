using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float bulletDecay = 3f;
    public static float damage = 10f;
    public string targetTag = "Player"; // set to "Enemy" for player bullets

    private float timer;

    private void Start()
    {
        timer = bulletDecay;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Damage the right target type
        if (other.CompareTag(targetTag))
        {
            if (targetTag == "Player")
            {
                var player = other.GetComponent<PlayerHealth>();
                if (player) player.TakeDamage(damage);
            }
            else if (targetTag == "Enemy")
            {
                var enemy = other.GetComponent<EnemyHealth>();
                if (enemy) enemy.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
