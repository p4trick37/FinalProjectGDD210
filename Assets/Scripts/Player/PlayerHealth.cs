using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public static float maxHealth;
    public float currentHealth;

    [Header("Fog Damage")]
    [SerializeField] private float fogDamage;
    [SerializeField] private float fogDamageSpeed;

    [Header("Damage Flash Settings")]
    public Color flashColor = Color.white;      // color to flash when hit
    public float flashDuration = 0.15f;         // time each flash lasts
    public int flashCount = 2;                  // number of flashes

    [Header("UI Elements")]
    [SerializeField] private TMP_Text uiHealth;

    [SerializeField] private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    [SerializeField] private DeathTransition deathTransition;

    // NEW: track death state so we can stop further damage / SFX
    private bool isDead = false;
    public bool IsDead => isDead;   // EnemyController will read this

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Optional extra safety if something else reduces health directly
        if (!isDead && currentHealth <= 0f)
        {
            isDead = true;
            OnDie();
        }
    }

    public void TakeDamage(float amount)
    {
        // If already dead, ignore any further hits (no more hit sounds, no more knockbacks)
        if (isDead) return;

        GetComponent<PlayerController>()?.OnHitByEnemy();

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage! Current health: {currentHealth}");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerHit();
        }

        // Trigger flash
        if (!isFlashing && spriteRenderer != null)
            StartCoroutine(FlashEffect());

        // If this hit killed the player, handle death once
        if (!isDead && currentHealth <= 0f)
        {
            isDead = true;
            OnDie();
        }
    }

    private void OnDie()
    {
        Debug.Log("Dead! The player is dead!");

        var pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.playerDead = true;
        }

        // Play death sound once
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerDie();
        }

        // Start your existing fade / transition
        if (deathTransition != null)
        {
            StartCoroutine(deathTransition.Transition());
        }
    }

    private IEnumerator FlashEffect()
    {
        isFlashing = true;
        originalColor = spriteRenderer.color;

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
}
