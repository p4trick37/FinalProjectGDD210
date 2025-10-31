using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed;
    public float rotationSpeed;
    public GameObject turret;

    public GameObject playerCamera;
    

    private void Update()
    {
        PlayerMovement(true);
        PlayerRotation(true);
        TurretMovement(true);

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
            if(MousePosition().x < 0)
            {
                turret.transform.rotation = Quaternion.Euler(0, 0, GetRotationMouseTracker() + 90);
            }
            else
            {
                turret.transform.rotation = Quaternion.Euler(0, 0, GetRotationMouseTracker() - 90);
            }
        }
    }



    private void TurretShooter(bool shouldShoot)
    {
        if(shouldShoot == true)
        {
            
        }
    }

    //Gathers the rotation angle of the player when using the mouse
    private float GetRotationMouseTracker()
    {
        float rotationAngleRad = Mathf.Atan(MousePosition().y / MousePosition().x);
        float rotationAngleDeg = Mathf.Rad2Deg * rotationAngleRad;
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
