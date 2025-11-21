using UnityEngine;
using UnityEngine.Rendering;

public class UpgradeManager : MonoBehaviour
{
    [Header("Bullet Reference")]
    [SerializeField] private SpriteRenderer bulletSPR;
    [Header("Values of the player when game starts")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float bulletDamage;
    [SerializeField] private float bulletSize;
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

    [Header("Upgrade 1 Attributes")]
    [SerializeField] private float upMovementSpeed;
    [SerializeField] private float upPlayerHealth;

    [Header("Upgrade 2 Attributes")]
    [SerializeField] private float upBulletDamage;
    [SerializeField] private Color upBulletColor;

    [Header("Uprage 3 Attributes")]
    [SerializeField] private float upMaxSemiUse;
    [SerialzieField] private float upMaxAutoUse;
    []


    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void StartValue()
    {
        PlayerHealth.maxHealth = maxHealth;

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
        Bullet.damage = upBulletDamage;
        bulletSPR.color = upBulletColor;
    }
    public void AttackSpeedUpgrade()
    {

    }
}
