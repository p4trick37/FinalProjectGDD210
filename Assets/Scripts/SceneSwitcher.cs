using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using static System.TimeZoneInfo;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject upgradeButtons;
    public GameObject nextLevelButtons;
    [SerializeField] private GameObject upgradeManagerInstance;
    private UpgradeManager upgradeManager;

    [SerializeField] private SceneTransition sceneTransition;

    //Functions for calling upgrades
    private void Awake()
    {
        upgradeManager = FindAnyObjectByType<UpgradeManager>();
        if (upgradeManager == null)
        {
            upgradeManager = Instantiate(upgradeManagerInstance).GetComponent<UpgradeManager>();
        }
    }
    public void DefenseUpgrade()
    {
        upgradeManager.DefenseUpgrade();
    }
    public void DamageUpgrade()
    {
        upgradeManager.DamageUpgrade();
    }
    public void AttackSpeedUpgrade()
    {
        upgradeManager.AttackSpeedUpgrade();
    }
    public void LoadNextButtons()
    {
        upgradeButtons.SetActive(false);
        nextLevelButtons.SetActive(true);
    }



    //Loading and Reloading Scenes
    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(sceneTransition.LoadTransition(sceneName, -1));
    }

    public void TransitionToSceneName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }


    public void LoadSceneByIndex(int index)
    {
        StartCoroutine(sceneTransition.LoadTransition("null", index));
    }
    public void TransitionToSceneIndex(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void ReloadCurrent()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
