using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Slider heatSlider;
    [SerializeField] PlayerController player;
    [SerializeField] Slider healthSlider;
    [SerializeField] PlayerHealth playerHealth;

    private void Update()
    {
        OverHeatUI();
        HealthUI();
    }
    private void OverHeatUI()
    {
        if(player.usingSemi == true)
        {
            heatSlider.maxValue = PlayerController.maxSemiUse;
            heatSlider.value = player.heatSemi;
        }
        else if(player.usingAuto == true)
        {
            heatSlider.maxValue = PlayerController.maxAutoUse;
            heatSlider.value = player.heatAuto;
        }
        else if(player.usingShotgun == true)
        {
            heatSlider.maxValue = PlayerController.maxShotgunUse;
            heatSlider.value = player.heatShotgun;
        }
    }
    private void HealthUI()
    {
        healthSlider.maxValue = PlayerHealth.maxHealth;
        healthSlider.value = playerHealth.currentHealth;
    }
}
