using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string nextSceneName;

    [Header("Gate Settings")]
    public Transform leftGate;
    public Transform rightGate;
    public float gateOpenAngle = 90f;
    public float gateOpenDuration = 1.0f;

    [Header("Treasure Chest")]
    public GameObject treasureChest; // always visible, no enabling/disabling
    [SerializeField] private Sprite chestClosed;
    [SerializeField] private Sprite chestOpen;

    private List<TowerUnit> towers = new List<TowerUnit>();
    private bool gatesOpened = false;
    [SerializeField] private SceneTransition sceneTransition;

    public static bool onLevel1;
    public static bool onLevel2;
    public static bool onLevel3;
    private int sceneIndex;

    void Start()
    {
        // Find all towers in the scene
        towers.AddRange(FindObjectsByType<TowerUnit>(FindObjectsSortMode.None));
        Debug.Log($"LevelManager: Found {towers.Count} towers in this level.");
        sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if(sceneIndex == 1)
        {
            onLevel1 = true;
            onLevel2 = false;
            onLevel3 = false;
        }
        else if(sceneIndex == 3)
        {
            onLevel1 = false;
            onLevel2 = true;
            onLevel3 = false;
        }
        else if(sceneIndex == 5)
        {
            onLevel1 = false;
            onLevel2 = false;
            onLevel3 = true;
        }
    }


    public void ReportTowerDestroyed(TowerUnit deadTower)
    {
        if (deadTower != null && towers.Contains(deadTower))
        {
            towers.Remove(deadTower);
        }

        Debug.Log($"Tower destroyed. Towers left: {towers.Count}");

        if (!gatesOpened && towers.Count <= 0)
        {
            Debug.Log("All towers defeated. Opening gates.");
            StartCoroutine(OpenGates());
            treasureChest.GetComponent<SpriteRenderer>().sprite = chestOpen;
        }
        else 
        {
            treasureChest.GetComponent<SpriteRenderer>().sprite = chestClosed;
        }
    }

    private IEnumerator OpenGates()
    {
        gatesOpened = true;

        Quaternion leftStart = leftGate ? leftGate.rotation : Quaternion.identity;
        Quaternion rightStart = rightGate ? rightGate.rotation : Quaternion.identity;

        Quaternion leftTarget = leftStart * Quaternion.Euler(0f, 0f, gateOpenAngle);
        Quaternion rightTarget = rightStart * Quaternion.Euler(0f, 0f, -gateOpenAngle);

        float elapsed = 0f;

        while (elapsed < gateOpenDuration)
        {
            float t = elapsed / gateOpenDuration;

            if (leftGate != null)
                leftGate.rotation = Quaternion.Slerp(leftStart, leftTarget, t);

            if (rightGate != null)
                rightGate.rotation = Quaternion.Slerp(rightStart, rightTarget, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final rotation
        if (leftGate != null)  leftGate.rotation = leftTarget;
        if (rightGate != null) rightGate.rotation = rightTarget;
    }

    public void LoadNextScene()
    {
        StartCoroutine(sceneTransition.LoadTransition("null", -1));
    }

    public void TransitionToScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("LevelManager: nextSceneName not set.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}

