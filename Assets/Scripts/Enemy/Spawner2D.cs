using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Spawner2D : MonoBehaviour
{
    [Header("Spawnable Prefabs")]
    [Tooltip("Add your enemy boats and animals here.")]
    public List<GameObject> spawnPrefabs = new List<GameObject>();

    [Header("Include Area (big outer box)")]
    [Tooltip("Center of the allowed spawn area. Defaults to this transform if empty.")]
    public Transform includeCenter;
    [Tooltip("Width (X) and Height (Y) of the allowed spawn region.")]
    public Vector2 includeAreaSize = new Vector2(80f, 50f);

    [System.Serializable]
    public class ExclusionZone
    {
        [Tooltip("Center of the do-not-spawn box (e.g., your island).")]
        public Transform center;
        [Tooltip("Width (X) and Height (Y) of this forbidden region.")]
        public Vector2 size = new Vector2(20f, 12f);
    }

    [Header("Do-Not-Spawn Boxes (add 1+ for islands, bases, etc.)")]
    public List<ExclusionZone> exclusions = new List<ExclusionZone>();

    [Header("Spawn Timing")]
    [Tooltip("Seconds between spawns.")]
    public float spawnInterval = 2.5f;
    [Tooltip("Max alive at once. 0 = no limit.")]
    public int maxSpawnedObjects = 15;
    [Tooltip("Random extra delay added to each spawn interval.")]
    public Vector2 randomDelayRange = new Vector2(0f, 1.25f);

    [Header("Behaviour")]
    public bool autoStart = true;
    [Tooltip("Tries this many times to find a legal spot before giving up this tick.")]
    public int maxPlacementAttempts = 25;

    [Header("Gizmos")]
    public bool drawAreas = true;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private bool _running;

    void Start()
    {
        if (!includeCenter) includeCenter = transform;
        if (autoStart) StartSpawning();
    }

    public void StartSpawning()
    {
        if (_running) return;
        _running = true;
        StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        _running = false;
        StopAllCoroutines();
    }

    IEnumerator SpawnLoop()
    {
        while (_running)
        {
            // Enforce max alive
            if (maxSpawnedObjects > 0)
            {
                _spawned.RemoveAll(x => x == null);
                if (_spawned.Count >= maxSpawnedObjects)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
            }

            // Wait (interval + jitter)
            float wait = spawnInterval + Random.Range(randomDelayRange.x, randomDelayRange.y);
            if (wait > 0f) yield return new WaitForSeconds(wait);

            // Spawn attempt
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        if (spawnPrefabs.Count == 0) return;

        Vector3? pos = FindLegalSpawnPosition();
        if (!pos.HasValue) return; // no legal spot found this tick

        GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Count)];
        if (!prefab) return;

        GameObject obj = Instantiate(prefab, pos.Value, Quaternion.identity);
        _spawned.Add(obj);
    }

    Vector3? FindLegalSpawnPosition()
    {
        Vector3 center = includeCenter ? includeCenter.position : transform.position;

        for (int attempt = 0; attempt < Mathf.Max(1, maxPlacementAttempts); attempt++)
        {
            // Random point in include box
            Vector2 half = includeAreaSize * 0.5f;
            Vector3 candidate = new Vector3(
                center.x + Random.Range(-half.x, half.x),
                center.y + Random.Range(-half.y, half.y),
                0f
            );

            if (PointInsideAnyExclusion(candidate)) continue; // reject

            return candidate; // legal
        }

        return null; // no legal point found
    }

    bool PointInsideAnyExclusion(Vector3 point)
    {
        for (int i = 0; i < exclusions.Count; i++)
        {
            var ex = exclusions[i];
            if (ex == null || ex.center == null) continue;

            Vector2 half = ex.size * 0.5f;
            Vector3 c = ex.center.position;

            bool inside =
                point.x >= c.x - half.x && point.x <= c.x + half.x &&
                point.y >= c.y - half.y && point.y <= c.y + half.y;

            if (inside) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawAreas) return;

        // Include box
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.18f);
        Vector3 ic = includeCenter ? includeCenter.position : transform.position;
        Gizmos.DrawCube(ic, new Vector3(includeAreaSize.x, includeAreaSize.y, 0f));
        Gizmos.color = new Color(0f, 0.9f, 0.2f, 1f);
        Gizmos.DrawWireCube(ic, new Vector3(includeAreaSize.x, includeAreaSize.y, 0f));

        // Exclusions
        if (exclusions != null)
        {
            foreach (var ex in exclusions)
            {
                if (ex == null || ex.center == null) continue;
                Gizmos.color = new Color(1f, 0f, 0f, 0.18f);
                Gizmos.DrawCube(ex.center.position, new Vector3(ex.size.x, ex.size.y, 0f));
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
                Gizmos.DrawWireCube(ex.center.position, new Vector3(ex.size.x, ex.size.y, 0f));
            }
        }
    }
}
