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
    private bool _isInDialogue = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        if (_cameraTransform == null)
            _cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        // Subscribe to dialogue events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart.AddListener(OnDialogueStarted);
            DialogueManager.Instance.OnDialogueEnd.AddListener(OnDialogueEnded);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from dialogue events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart.RemoveListener(OnDialogueStarted);
            DialogueManager.Instance.OnDialogueEnd.RemoveListener(OnDialogueEnded);
        }
    }

    private void OnEnable()
    {
        InputManager.Instance._inputActions.Player.Move.performed += HandleMove;
        InputManager.Instance._inputActions.Player.Move.canceled += HandleMove;
        InputManager.Instance._inputActions.Player.Look.performed += HandleLook;
        InputManager.Instance._inputActions.Player.Look.canceled += HandleLook;
        InputManager.Instance._inputActions.Player.Interact.performed += HandleInteract;
    }

    private void OnDisable()
    {
        InputManager.Instance._inputActions.Player.Move.performed -= HandleMove;
        InputManager.Instance._inputActions.Player.Move.canceled -= HandleMove;
        InputManager.Instance._inputActions.Player.Look.performed -= HandleLook;
        InputManager.Instance._inputActions.Player.Look.canceled -= HandleLook;
        InputManager.Instance._inputActions.Player.Interact.performed -= HandleInteract;
    }

    private void HandleMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void HandleLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
        if (context.canceled)
        {
            _lookInput = Vector2.zero;
        }
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        Interact();
    }

    private void OnDialogueStarted()
    {
        _isInDialogue = true;
    }

    private void OnDialogueEnded()
    {
        _isInDialogue = false;
    }

    private void FixedUpdate()
    {
        // Skip movement if in dialogue
        if (_isInDialogue)
            return;

        Vector3 moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        moveDirection.Normalize();

        _rb.MovePosition(_rb.position + moveDirection * _speed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        // Skip camera rotation if in dialogue
        if (_isInDialogue)
            return;

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
            Debug.Log("Hit: " + hit.collider.name);
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_cameraTransform.position, _cameraTransform.forward * _interactionDistance);
    }

    void Reset()
    {
        _rb = GetComponent<Rigidbody>();
    }
}
