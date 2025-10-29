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

    private void TurretMovement(bool shouldMove)
    {
        Vector2 centerOfScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        Vector2 mousePositionInput = Input.mousePosition;
        Vector2 mousePosition = mousePositionInput - centerOfScreen;
        float rotationAngleRad = Mathf.Atan(mousePosition.y / mousePosition.x);
        float rotationAngleDeg = Mathf.Rad2Deg * rotationAngleRad;
        turret.transform.rotation = Quaternion.Euler(0, 0, rotationAngleDeg - 90);
        Debug.Log(rotationAngleDeg);

    }


    
}
