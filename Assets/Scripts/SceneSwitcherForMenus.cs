using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcherForMenus : MonoBehaviour
{
    [SerializeField] private SceneTransition sceneTransition;
    //Loading and Reloading Scenes
    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(sceneTransition.LoadTransition(sceneName, -1));
    }
    public void TransitionToSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int index)
    {
        StartCoroutine(sceneTransition.LoadTransition("null", index));
    }
    public void TransitionToSceneByIndex(int index)
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

    public void ReturnToCurrentLevel()
    {
        int index;
        if(LevelManager.onLevel1)
        {
            index = 1;
        }
        else if(LevelManager.onLevel2)
        {
            index = 3;
        }
        else if(LevelManager.onLevel3)
        {
            index = 5;
        }
        else
        {
            index = 1;
        }
        StartCoroutine(sceneTransition.LoadTransition("null", index));
    }
}
