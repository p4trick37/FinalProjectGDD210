using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathTransition : MonoBehaviour
{
    [SerializeField] private Animator transition;
    [SerializeField] private float transitionTime;
    [SerializeField] private int deathSceneIndex;
 

   
    public IEnumerator Transition()
    {
        transition.SetTrigger("Dead");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(deathSceneIndex);
    }
}
