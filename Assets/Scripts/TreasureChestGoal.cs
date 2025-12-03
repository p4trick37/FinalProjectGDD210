using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TreasureChestGoal : MonoBehaviour
{
    private LevelManager levelManager;

    void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (levelManager == null) return;

        levelManager.LoadNextScene();
    }
}
