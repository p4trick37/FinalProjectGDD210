using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] PlayerController player;

    private void Update()
    {
        OverHeatUI();
        
    }
    private void OverHeatUI()
    {
        if(player.usingSemi == true)
        {
            slider.maxValue = PlayerController.maxSemiUse;
            slider.value = player.heatSemi;
        }
        else if(player.usingAuto == true)
        {
            slider.maxValue = PlayerController.maxAutoUse;
            slider.value = player.heatAuto;
        }
        else if(player.usingShotgun == true)
        {
            slider.maxValue = PlayerController.maxShotgunUse;
            slider.value = player.heatShotgun;
        }
    }
}
