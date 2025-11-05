using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AnimalController : MonoBehaviour
{
    [Header("Target Settings")]
    private Transform player;
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask playerLayer;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float lungeForce = 12f;
    public float waitBeforeLunge = 0.75f;
    public float lungeDuration = 0.4f;     // how long the lunge lasts
    public float lungeCooldown = 2f;

    [Header("Physics")]
    public float dragDuringLunge = 6f;     // slows down after attack
    public float normalDrag = 0f;          // normal drag for smooth glide

    [Header("Combat")]
    public float contactDamage = 10f;      // damage applied to player on contact
    public float hitCooldown = 0.6f;       // per-hit cooldown so player isn't spammed

    [Header("Debug Options")]
    public bool showDetectionRange = true;

    private Rigidbody2D rb;
    private bool isLunging = false;
    private bool isOnCooldown = false;

    // internal hit timer (prevents repeated hits)
    float _nextHitTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = normalDrag;

        if (!player)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found) player = found.transform;
        }
    }

    void FixedUpdate()
    {
        if (isLunging || isOnCooldown || !player) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            StartCoroutine(LungeAtPlayer());
        }
        else if (distance <= detectionRange)
        {
            // Move toward player
            Vector2 dir = (player.position - transform.position).normalized;
            rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

            float zRot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = zRot;
        }
    }

    IEnumerator LungeAtPlayer()
    {
        isLunging = true;
        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = normalDrag;

        // Pause briefly before lunging
        yield return new WaitForSeconds(waitBeforeLunge);

        if (player)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearDamping = 0f; // no drag while lunging
            rb.AddForce(dir * lungeForce, ForceMode2D.Impulse);
        }

        // Let lunge happen for a short duration (we can apply damage during collisions)
        yield return new WaitForSeconds(lungeDuration);

        // Apply strong drag to stop after lunging
        rb.linearDamping = dragDuringLunge;

        // Reset after cooldown
        yield return new WaitForSeconds(lungeCooldown);
        rb.linearDamping = normalDrag;
        isLunging = false;
        isOnCooldown = false;
    }

    // Handle both trigger and collision setups — calls TryDamage when we hit the player
    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void TryDamage(GameObject hit)
    {
        if (Time.time < _nextHitTime) return;        // per-animal cooldown before next hit
        if (hit == null) return;

        // Check tag first (requires your Player to be tagged "Player")
        if (!hit.CompareTag("Player")) return;

        // Try to find a PlayerHealth component (your existing script)
        var ph = hit.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(contactDamage);
        }
        else
        {
            // If you don't have PlayerHealth, try a generic fallback that looks for a method named "TakeDamage"
            var method = hit.GetType().GetMethod("TakeDamage");
            if (method != null)
            {
                // Attempt to call TakeDamage(float) via reflection (safe-guarded)
                try
                {
                    method.Invoke(hit, new object[] { contactDamage });
                }
                catch { /* ignore if signature mismatch */ }
            }
        }

        // set next allowed hit time
        _nextHitTime = Time.time + hitCooldown;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDetectionRange) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
