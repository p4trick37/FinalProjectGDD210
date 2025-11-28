using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ControllerMenu : MonoBehaviour
{
    [SerializeField] private GameObject upgradeFirstButton;
    [SerializeField] private GameObject nextLevelFirstButton;
    [SerializeField] private SceneSwitcher sceneSwitcher;

    private bool lastState = false;

    private void Start()
    {
        // ensure EventSystem exists
        if (EventSystem.current == null)
            Debug.LogError("No EventSystem in scene!");
    }

    private void Update()
    {
        bool upgradesActive = sceneSwitcher.upgradeButtons.activeInHierarchy;

        if (upgradesActive != lastState)
        {
            lastState = upgradesActive;
            if (upgradesActive)
                SelectButton(upgradeFirstButton);
            else
                SelectButton(nextLevelFirstButton);
        }
    }

    private void SelectButton(GameObject button)
    {
        if (button == null)
        {
            Debug.LogWarning("SelectButton called with null.");
            return;
        }

        // Start coroutine to set selection next frame (allows Unity to finish enabling UI)
        StartCoroutine(SelectNextFrame(button));
    }

    private IEnumerator SelectNextFrame(GameObject button)
    {
        // clear current selection first (helps avoid leftover selection)
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // wait one frame so the UI can fully activate
        yield return null;

        // If still null or inactive, bail
        if (!button.activeInHierarchy)
        {
            Debug.LogWarning("Button not active when trying to select it: " + button.name);
            yield break;
        }

        // Prefer calling Select on the Selectable if available (highly reliable)
        var selectable = button.GetComponent<UnityEngine.UI.Selectable>();
        if (selectable != null)
        {
            selectable.Select();
            // still set EventSystem selected object explicitly
            EventSystem.current.SetSelectedGameObject(button);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(button);
        }
    }
}