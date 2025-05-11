using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    public InputSystem_Actions _inputActions;

    public Vector2 MovementInput => _inputActions.Player.Move.ReadValue<Vector2>();
    public Vector2 LookInput => _inputActions.Player.Look.ReadValue<Vector2>();

    // Event for dialogue advancement
    public event Action OnDialogueAdvance;

    public override void Awake()
    {
        base.Awake();
        _inputActions = new InputSystem_Actions();
        EnablePlayerControls();

        // Register for UI submit/continue action
        _inputActions.UI.Click.performed += ctx => OnDialogueAdvance?.Invoke();
    }

    private void OnEnable()
    {
        if (_inputActions == null)
            _inputActions = new InputSystem_Actions();

        EnablePlayerControls();
    }

    private void OnDisable()
    {
        DisableAllControls();
    }

    public void EnablePlayerControls()
    {
        _inputActions.Player.Enable();
        _inputActions.UI.Disable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void EnableUIControls()
    {
        _inputActions.Player.Disable();
        _inputActions.UI.Enable();

        // Unlock cursor for dialogue interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisablePlayerControls()
    {
        _inputActions.Player.Disable();
    }

    public void DisableAllControls()
    {
        _inputActions.Player.Disable();
        _inputActions.UI.Disable();
    }
}
