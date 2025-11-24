using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string nextSceneName;     // Set this in the inspector

    private List<TowerUnit> towers = new List<TowerUnit>();

    void Start()
    {
        // Find all towers in the scene
        towers.AddRange(FindObjectsByType<TowerUnit>(FindObjectsSortMode.None));
        Debug.Log($"LevelManager: Found {towers.Count} towers in this level.");
    }

    /// <summary>
    /// Called from EnemyHealth / TowerUnit when a tower dies.
    /// </summary>
    public void ReportTowerDestroyed(TowerUnit deadTower)
    {
        if (deadTower != null && towers.Contains(deadTower))
        {
            towers.Remove(deadTower);
        }

        Debug.Log($"Tower destroyed. Towers left: {towers.Count}");

        if (towers.Count <= 0)
        {
            Debug.Log("All towers defeated! Loading next scene...");
            SceneManager.LoadScene(nextSceneName);


        }
    }
}
