using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Values of the player when game starts")]
    [SerializeField] private float maxSemiUse;
    [SerializeField] private float maxAutoUse;
    [SerializeField] private float maxShotgunUse;
    [Space(15)]

    //The time that it takes for the heat to lower before over heated
    [SerializeField] private float heatDrainSemi;
    [SerializeField] private float heatDrainAuto;
    [SerializeField] private float heatDrainShotgun;
    [Space(15)]

    //The heat that gets added when taking a shot
    [SerializeField] private float heatAddSemi;
    [SerializeField] private float heatAddAuto;
    [SerializeField] private float heatAddShotgun;

    [Space(15)]
    //The time that it takes for the weapon to be done heating
    [SerializeField] private float heatDelaySemi;
    [SerializeField] private float heatDelayAuto;
    [SerializeField] private float heatDelayShotgun;
    [Space(15)]

    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    [Space(15)]

    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletSpread;
    [Space(15)]

    [SerializeField] private float semiAutoDelay;
    [SerializeField] private float autoDelay;
    [SerializeField] private float shotgunDelay;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void StartValue()
    {
        PlayerController.maxSemiUse = maxSemiUse;
        PlayerController.maxAutoUse = maxAutoUse;
        PlayerController.maxShotgunUse = maxShotgunUse;

        
    }
}
