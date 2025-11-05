using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("Damage Flash Settings")]
    public Color flashColor = Color.white;   // flash color when hit
    public float flashDuration = 0.12f;      // time each flash lasts
    public int flashCount = 2;               // number of flashes
    public bool useEmission = true;          // toggle to flash emission instead of base color

    private MeshRenderer meshRenderer;
    private Material materialInstance;
    private Color originalColor;
    private bool isFlashing = false;

    private void Start()
    {
        currentHealth = maxHealth;

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // Create an instance so the material isn't shared globally
            materialInstance = meshRenderer.material;
            originalColor = useEmission && materialInstance.HasProperty("_EmissionColor")
                ? materialInstance.GetColor("_EmissionColor")
                : materialInstance.color;
        }
        else
        {
            Debug.LogWarning($"{name}: EnemyHealth has no MeshRenderer!");
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage! Current health: {currentHealth}");

        if (!isFlashing && meshRenderer != null)
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

        if (materialInstance != null)
        {
            if (useEmission && materialInstance.HasProperty("_EmissionColor"))
                materialInstance.SetColor("_EmissionColor", originalColor);
            else
                materialInstance.color = originalColor;
        }

        // Disable this enemy (for pooling or cleanup)
        gameObject.SetActive(false);
    }

    private IEnumerator FlashEffect()
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            if (materialInstance != null)
            {
                if (useEmission && materialInstance.HasProperty("_EmissionColor"))
                    materialInstance.SetColor("_EmissionColor", flashColor);
                else
                    materialInstance.color = flashColor;
            }

            yield return new WaitForSeconds(flashDuration);

            if (materialInstance != null)
            {
                if (useEmission && materialInstance.HasProperty("_EmissionColor"))
                    materialInstance.SetColor("_EmissionColor", originalColor);
                else
                    materialInstance.color = originalColor;
            }

            yield return new WaitForSeconds(flashDuration);
        }

        isFlashing = false;
    }
}
