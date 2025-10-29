using UnityEngine;

/// <summary>
/// Simple 2D player controller for top-down boat-style movement.
/// Uses Rigidbody2D for smooth physics and damping.
/// - Move with WASD / Arrow keys
/// - Optional rotation to face movement direction
/// - Speed, acceleration, and damping adjustable in inspector
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBoatController2D : MonoBehaviour
{
    [Header("=== Movement Settings ===")]
    [Tooltip("Maximum movement speed (units per second).")]
    public float moveSpeed = 6f;

    [Tooltip("How fast the boat accelerates to target velocity.")]
    public float acceleration = 12f;

    [Tooltip("How fast the boat slows down when not pressing movement keys.")]
    public float damping = 8f;

    [Tooltip("Rotate the boat to face movement direction?")]
    public bool rotateToFaceMovement = true;

    [Tooltip("Rotation smoothing (higher = snappier).")]
    public float rotationLerpSpeed = 10f;

    [Header("=== Input ===")]
    [Tooltip("Name of horizontal input axis (usually 'Horizontal').")]
    public string horizontalAxis = "Horizontal";
    [Tooltip("Name of vertical input axis (usually 'Vertical').")]
    public string verticalAxis = "Vertical";

    Rigidbody2D _rb;
    Vector2 _input;
    Vector2 _velocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
    }

    void Update()
    {
        // Read input from keyboard (WASD or arrows)
        _input = new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis)).normalized;
    }

    void FixedUpdate()
    {
        Vector2 desiredVelocity = _input * moveSpeed;
        _velocity = Vector2.MoveTowards(_velocity, desiredVelocity, acceleration * Time.fixedDeltaTime);

        // Apply damping if no input
        if (_input.sqrMagnitude < 0.01f)
        {
            _velocity = Vector2.MoveTowards(_velocity, Vector2.zero, damping * Time.fixedDeltaTime);
        }

        _rb.linearVelocity = _velocity;

        // Optional rotate to face movement direction
        if (rotateToFaceMovement && _velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            _rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRot, rotationLerpSpeed * Time.fixedDeltaTime));
        }
    }
}
