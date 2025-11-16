using UnityEngine;
using System.Collections;
using TMPro;


public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("Damage Flash Settings")]
    public Color flashColor = Color.white;   // flash color when hit
    public float flashDuration = 0.12f;      // time each flash lasts
    public int flashCount = 2;               // number of flashes

    [Header("UI Elements")]
    public TMP_Text uiHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    private void Start()
    {
        currentHealth = maxHealth;

        // Try to get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else
            Debug.LogWarning($"{name}: EnemyHealth has no SpriteRenderer!");
    }
    private void Update()
    {
        //UI Updating enemy Health
        if(uiHealth != null)
        {
            uiHealth.text = currentHealth + "";
        }
        
    }


    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage! Current health: {currentHealth}");

        if (!isFlashing && spriteRenderer != null)
            StartCoroutine(FlashEffect());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died!");
        StopAllCoroutines();

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        // Disable the enemy so it can be pooled or reactivated later
        gameObject.SetActive(false);
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
