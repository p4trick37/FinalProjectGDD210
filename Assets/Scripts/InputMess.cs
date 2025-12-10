using UnityEngine;
using UnityEngine.InputSystem;

public class InputMess : MonoBehaviour
{

    ControllerInput controls;
    Vector2 move;
    private float triggerInput;
    private void Awake()
    {
        controls = new ControllerInput();

        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => move = Vector2.zero;

        triggerInput = controls.Player.Shoot.ReadValue<float>();
    }

    private void Update()
    {
        triggerInput = controls.Player.Shoot.ReadValue<float>();
        Debug.Log(triggerInput);
        transform.position += new Vector3(move.x, move.y, 0) * 4 * Time.deltaTime;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
}
