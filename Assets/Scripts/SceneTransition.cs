using UnityEngine;
using System.Collections;
public class SceneTransition : MonoBehaviour
{
    public Animator transtion;
    public float transitionTime = 1f;

    private SceneSwitcher sceneSwitcher;
    private SceneSwitcherForMenus sceneSwitcherForMenus;
    private LevelManager levelManager;

    private void Start()
    {
        sceneSwitcher = FindAnyObjectByType<SceneSwitcher>();
        if(sceneSwitcher == null)
        {
            levelManager = FindAnyObjectByType<LevelManager>();
            if(levelManager == null)
            {
                sceneSwitcherForMenus = FindAnyObjectByType<SceneSwitcherForMenus>();
            }
        }
    }


    public IEnumerator LoadTransition(string sceneName, int index)
    {
        transtion.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);

        if(sceneSwitcher != null)
        {
            if (index == -1)
            {
                sceneSwitcher.TransitionToSceneName(sceneName);
            }
            else
            {
                sceneSwitcher.TransitionToSceneIndex(index);
            }
        }
        else if(levelManager != null)
        {
            levelManager.TransitionToScene();
        }
        else if(sceneSwitcherForMenus != null)
        {
            if (index == -1)
            {
                sceneSwitcherForMenus.TransitionToSceneByName(sceneName);
            }
            else
            {
                sceneSwitcherForMenus.TransitionToSceneByIndex(index);
            }
        }
        
        

    }
}
