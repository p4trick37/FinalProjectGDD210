using UnityEngine;
using UnityEngine.Rendering;

public class UpgradeManager : MonoBehaviour
{
    [Header("Bullet Reference")]
    [SerializeField] private SpriteRenderer bulletSPR;
    [Header("Values of the player when game starts")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float bulletDamage;
    [Space(15)]
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

    [Header("Defense Attributes")]
    [SerializeField] private float upMovementSpeed;
    [SerializeField] private float upPlayerHealth;

    [Header("Damage Attributes")]
    [SerializeField] private float upBulletDamage;
    public Color upBulletColor;
    public static bool shouldBulletColorChange = false;

    [Header("Attack Speed Attributes")]
    [SerializeField] private float downBulletDamage;
    [SerializeField] private float upMaxSemiUse;
    [SerializeField] private float upMaxAutoUse;
    [SerializeField] private float upMaxShotgunUse;
    [SerializeField] private float downHeatDelaySemi;
    [SerializeField] private float downHeatDelayAuto;
    [SerializeField] private float downHeatDelayShotgun;
    [SerializeField] private float upBulletSpeed;

    


    private void Awake()
    {
        DontDestroyOnLoad(this);

    }

    public void StartValue()
    {
        PlayerHealth.maxHealth = maxHealth;
        Bullet.damage = bulletDamage;

        PlayerController.maxSemiUse = maxSemiUse;
        PlayerController.maxAutoUse = maxAutoUse;
        PlayerController.maxShotgunUse = maxShotgunUse;

        PlayerController.heatDrainSemi = heatDrainSemi;
        PlayerController.heatDrainAuto = heatDrainAuto;
        PlayerController.heatDrainShotgun = heatDrainShotgun; 

        PlayerController.heatAddSemi = heatAddSemi;
        PlayerController.heatAddAuto = heatAddSemi;
        PlayerController.heatAddShotgun = heatAddShotgun;

        PlayerController.heatDelaySemi = heatDelaySemi;
        PlayerController.heatDelayAuto = heatDelayAuto;
        PlayerController.heatDelayShotgun = heatDelayShotgun;

        PlayerController.movementSpeed = movementSpeed;
        PlayerController.rotationSpeed = rotationSpeed;

        PlayerController.bulletSpeed = bulletSpeed;
        PlayerController.bulletSpread = bulletSpread;

        PlayerController.semiAutoDelay = semiAutoDelay;
        PlayerController.autoDelay = autoDelay;
        PlayerController.shotgunDelay = shotgunDelay;
    }
    public void DefenseUpgrade()
    {
        PlayerController.movementSpeed += upMovementSpeed;
        PlayerHealth.maxHealth += upPlayerHealth;
    }
    public void DamageUpgrade()
    {
        Bullet.damage += upBulletDamage;
        shouldBulletColorChange = true;
    }
    public void AttackSpeedUpgrade()
    {
        PlayerController.upgradedToAuto = true;
        Bullet.damage -= downBulletDamage;

        PlayerController.maxSemiUse += upMaxSemiUse;
        PlayerController.maxAutoUse += upMaxAutoUse;
        PlayerController.maxShotgunUse += upMaxShotgunUse;
        PlayerController.heatDelaySemi -= downHeatDelaySemi;
        PlayerController.heatDelayAuto -= downHeatDelayAuto;
        PlayerController.heatDelayShotgun -= downHeatDelayShotgun;
        PlayerController.bulletSpeed += upBulletSpeed;
    }
}
