using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject upgradeButtons;
    [SerializeField] private GameObject nextLevelButtons;
    [SerializeField] private GameObject upgradeManagerInstance;
    private UpgradeManager upgradeManager;
    private void Start()
    {
        upgradeManager = FindAnyObjectByType<UpgradeManager>();
        if(upgradeManager == null)
        {
            upgradeManager = Instantiate(upgradeManagerInstance).GetComponent<UpgradeManager>();
        }
    }
    //Functions for calling upgrades
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
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int index)
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
