using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    private InputSystem _inputSystem;
    private Vector2 _moveVector;
    private float _rotateY = 0f;
    [SerializeField] private float _movementSpeed = 0.5f;

    private void OnEnable()
    {
        _inputSystem.Enable();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        _inputSystem.Disable();
    }


    private void Awake()
    {
        _inputSystem = new InputSystem();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            RotateUpdater();
            MoveUpdater();
        }
    }

    private void MoveUpdater()
    {
        _moveVector = _inputSystem.Player.Move.ReadValue<Vector2>();
        _rigidbody.linearVelocity = transform.forward * _moveVector.y * _movementSpeed;
    }

    private void RotateUpdater()
    {
        _rotateY += _inputSystem.Player.Rotate.ReadValue<float>();
        _rigidbody.MoveRotation(Quaternion.Euler(0, _rotateY, 0));
    }
}