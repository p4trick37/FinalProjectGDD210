using JetBrains.Annotations;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("Player Movement and Rotation")]
    public float movementSpeed;
    public float rotationSpeed;

    [Header("Bullet Speed and Spread")]
    public float bulletSpeed;
    public float bulletSpread;

    [Header("Delays for Firing Weapons")]
    public float semiAutoDelay;
    public float autoDelay;
    public float shotgunDelay;

    [Header("Object References")]
    public GameObject turret;

    public GameObject playerCamera;

    public GameObject bulletPrefab;

    [Header("Weapon List and its Current Weapon")]
    public string[] weapons = new string[]{"SemiAuto", "FullAuto", "3RoundShot"};
    public int currentWeapon;




    private bool[] shootCurrentWeapon = new bool[3];
    

    private float semiAutoTimerDelay;
    private float autoTimerDelay;
    private float shotgunTimerDelay;
    private bool canSemiShoot;
    private bool canAutoShoot;
    private bool canShotgunShoot;
    private void Update()
    {
        PlayerMovement(true);
        PlayerRotation(true);
        TurretMovement(true);
        if(autoTimerDelay > 0)
        {
            autoTimerDelay -= Time.deltaTime;
            canAutoShoot = false;
        }
        else
        {
            canAutoShoot = true;
        }

        if(semiAutoTimerDelay > 0)
        {
            semiAutoTimerDelay -= Time.deltaTime;
            canSemiShoot = false;
        }
        else
        {
            canSemiShoot = true;
        }

        if(shotgunTimerDelay > 0)
        {
            shotgunTimerDelay -= Time.deltaTime;
            canShotgunShoot = false;
        }
        else
        {
            canShotgunShoot= true;
        }

        switch(currentWeapon)
        {
            case 0:
                if(Input.GetMouseButtonDown(0) && canSemiShoot == true)
                {
                    shootCurrentWeapon[currentWeapon] = true;
                }
                break;
            case 1:
                if(Input.GetMouseButton(0))
                {
                    shootCurrentWeapon[currentWeapon] = true;
                }
                
                if(Input.GetMouseButtonUp(0))
                {
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;
            case 2:
                if(Input.GetMouseButtonDown(0) && canShotgunShoot == true)
                {
                    shootCurrentWeapon[currentWeapon] = true;
                }
                break;
        }        
    }

    private void FixedUpdate()
    {

        switch(currentWeapon)
        {
            case 0:
                if (shootCurrentWeapon[currentWeapon] == true)
                {
                    ShootSingleBullet();
                    semiAutoTimerDelay = semiAutoDelay;
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;
            case 1:
                if (shootCurrentWeapon[currentWeapon] == true && canAutoShoot == true)
                {
                    ShootSingleBullet();
                    autoTimerDelay = autoDelay;
                }
                break;
            case 2:
                if (shootCurrentWeapon[currentWeapon] == true)
                {
                    Shoot3Bullets();
                    shotgunTimerDelay = shotgunDelay;
                    shootCurrentWeapon[currentWeapon] = false;
                }
                break;
            }
        }

    //Player Movement
    private void PlayerMovement(bool shouldMove)
    {
        if (shouldMove == true)
        {
            float movementInput = Input.GetAxisRaw("Vertical");
            transform.position += transform.up * movementInput * movementSpeed * Time.deltaTime;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
        }
    }

    //Player Rotation
    private void PlayerRotation(bool shouldRotate)
    {
        if(shouldRotate == true)
        {
            playerCamera.transform.rotation  = Quaternion.Euler(0, 0, 0);
            transform.Rotate(0, 0, -(Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime * 30));
        }
    }

    //Movement of the turret with the mouse
    private void TurretMovement(bool shouldMove)
    {
        if(shouldMove == true)
        {
            turret.transform.rotation = Quaternion.Euler(0, 0, GetRotationMouseTracker());
        }
    }


    //Shoots a bullet in the direciton of the mouse
    private void ShootSingleBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        float angle = GetRotationMouseTracker() + 90;
        Vector2 bulletDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        bullet.GetComponent<Rigidbody2D>().AddForce(bulletDirection * bulletSpeed);
    }

    private void Shoot3Bullets()
    {
        GameObject bullet1 = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        GameObject bullet2 = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        GameObject bullet3 = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        float angle1 = GetRotationMouseTracker() + 90;
        float angle2 = GetRotationMouseTracker() + 90 + bulletSpread;
        float angle3 = GetRotationMouseTracker() + 90 - bulletSpread;
        Vector2 bulletDirection1 = new Vector2(Mathf.Cos(angle1 * Mathf.Deg2Rad), Mathf.Sin(angle1 * Mathf.Deg2Rad));
        Vector2 bulletDirection2 = new Vector2(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad));
        Vector2 bulletDirection3 = new Vector2(Mathf.Cos(angle3 * Mathf.Deg2Rad), Mathf.Sin(angle3 * Mathf.Deg2Rad));
        bullet1.GetComponent<Rigidbody2D>().AddForce(bulletDirection1 * bulletSpeed);
        bullet2.GetComponent<Rigidbody2D>().AddForce(bulletDirection2 * bulletSpeed);
        bullet3.GetComponent<Rigidbody2D>().AddForce(bulletDirection3 * bulletSpeed);

    }


    //Gathers the rotation angle of the player when using the mouse
    private float GetRotationMouseTracker()
    {
        float rotationAngleRad = Mathf.Atan(MousePosition().y / MousePosition().x);
        float rotationAngleDeg = Mathf.Rad2Deg * rotationAngleRad;
        
        if(MousePosition().x < 0)
        {
            rotationAngleDeg += 90;
        }
        else
        {
            rotationAngleDeg -= 90;
        }
        return rotationAngleDeg;
    }

    //Gathers the mouse position with its origen at the center of the screen
    private Vector2 MousePosition()
    {
        Vector2 centerOfScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        Vector2 mousePositionInput = Input.mousePosition;
        Vector2 mousePosition = mousePositionInput - centerOfScreen;
        return mousePosition;
    }
}
