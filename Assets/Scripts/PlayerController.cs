using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed;
    public float rotationSpeed;
    public float bulletSpeed;
    public GameObject turret;

    public GameObject playerCamera;

    public GameObject bulletPrefab;

    private bool shouldShoot = false;
    private void Update()
    {
        PlayerMovement(true);
        PlayerRotation(true);
        TurretMovement(true);


        if(Input.GetMouseButtonDown(0))
        {
            shouldShoot = true;
        }
        
    }

    private void FixedUpdate()
    {
        if(shouldShoot == true)
        {
            TurretShooter();
            shouldShoot = false;
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



    private void TurretShooter()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        float angle = GetRotationMouseTracker() + 90;
        Vector2 bulletDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        bullet.GetComponent<Rigidbody2D>().AddForce(bulletDirection * bulletSpeed);
        Debug.Log(GetRotationMouseTracker());
        Debug.Log(bulletDirection);
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
