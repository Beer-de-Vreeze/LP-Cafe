using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float _speed = 5f;

    [SerializeField]
    private float _lookSensitivity = 2f;

    [Header("Interaction")]
    [SerializeField]
    private float _interactionDistance = 2f;

    [SerializeField]
    private Transform _cameraTransform;

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private float _cameraRotation = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        if (_cameraTransform == null)
            _cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        InputManager.Instance._inputActions.Player.Move.performed += OnMove;
        InputManager.Instance._inputActions.Player.Move.canceled += OnMove;
        InputManager.Instance._inputActions.Player.Look.performed += OnLook;
        InputManager.Instance._inputActions.Player.Look.canceled += OnLook;
        InputManager.Instance._inputActions.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        InputManager.Instance._inputActions.Player.Move.performed -= OnMove;
        InputManager.Instance._inputActions.Player.Move.canceled -= OnMove;
        InputManager.Instance._inputActions.Player.Look.performed -= OnLook;
        InputManager.Instance._inputActions.Player.Look.canceled -= OnLook;
        InputManager.Instance._inputActions.Player.Interact.performed -= OnInteract;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Interact();
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        moveDirection.Normalize();

        _rb.MovePosition(_rb.position + moveDirection * _speed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        float mouseX = _lookInput.x * _lookSensitivity;
        float mouseY = _lookInput.y * _lookSensitivity;

        transform.Rotate(Vector3.up, mouseX);
        _cameraRotation -= mouseY;
        _cameraRotation = Mathf.Clamp(_cameraRotation, -90f, 90f);
        _cameraTransform.localRotation = Quaternion.Euler(_cameraRotation, 0f, 0f);
    }

    private void Interact()
    {
        RaycastHit hit;
        if (
            Physics.Raycast(
                _cameraTransform.position,
                _cameraTransform.forward,
                out hit,
                _interactionDistance
            )
        )
        {
            Interfaces.IInteractable interactable =
                hit.collider.GetComponent<Interfaces.IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
            else
            {
                Debug.Log("No interactable found in range.");
            }
        }
    }

    void Reset()
    {
        _rb = GetComponent<Rigidbody>();
    }
}
