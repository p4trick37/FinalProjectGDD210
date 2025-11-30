using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public static float maxHealth;
    public float currentHealth;

    [Header("Damage Flash Settings")]
    public Color flashColor = Color.white;      // color to flash when hit
    public float flashDuration = 0.15f;         // time each flash lasts
    public int flashCount = 2;                  // number of flashes

    [Header("UI Elements")]
    [SerializeField] private TMP_Text uiHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    private void Start()
    {
        currentHealth = maxHealth;

        // Try to get the player’s sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            Debug.LogWarning("PlayerHealth: No SpriteRenderer found on this GameObject!");
        }
    }


    public void TakeDamage(float amount)
    {
        GetComponent<PlayerController>()?.OnHitByEnemy();

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage! Current health: {currentHealth}");

        // Trigger flash
        if (!isFlashing && spriteRenderer != null)
            StartCoroutine(FlashEffect());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // Add your death logic here (respawn, game over, etc.)
    }

    private IEnumerator FlashEffect()
    {
        isFlashing = true;
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
