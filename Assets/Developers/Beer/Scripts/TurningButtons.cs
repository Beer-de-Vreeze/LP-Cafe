using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls camera movement between predefined positions using UI buttons.
/// Camera moves and rotates smoothly between positions when buttons are clicked.
/// </summary>
public class TurningButtons : MonoBehaviour
{
    [SerializeField]
    List<Transform> Positions; // List of transform positions for the camera to move through

    [SerializeField]
    private Button leftButton; // Reference to the UI button for navigating left/previous position

    [SerializeField]
    private Button rightButton; // Reference to the UI button for navigating right/next position

    [SerializeField]
    private Camera mainCamera; // Reference to the camera that will be moved

    [SerializeField]
    private float moveSpeed = 2.0f; // Speed multiplier for camera position movement

    [SerializeField]
    private float rotationSpeed = 2.0f; // Speed multiplier for camera rotation

    private int currentPositionIndex = 0; // Index of the current/target position in the Positions list
    private Vector3 targetPosition; // The target position the camera is moving towards
    private Quaternion targetRotation; // The target rotation the camera is rotating towards
    private bool isTransitioning = false; // Flag to track if camera is currently moving between positions

    /// <summary>
    /// Initializes buttons, camera reference, and sets initial camera position.
    /// </summary>
    private void Start()
    {
        // Set up button click listeners
        if (leftButton != null)
            leftButton.onClick.AddListener(OnLeftButtonClick);

        if (rightButton != null)
            rightButton.onClick.AddListener(OnRightButtonClick);

        // If no camera is assigned, use the main camera
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Set initial camera position if positions are available
        if (Positions.Count > 0)
        {
            currentPositionIndex = 0;
            SetTargetTransform(currentPositionIndex);
        }
    }

    /// <summary>
    /// Handles smooth camera movement and rotation towards target transform.
    /// Called every frame when camera is in transition.
    /// </summary>
    private void Update()
    {
        if (isTransitioning)
        {
            // Move camera to target position using linear interpolation
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // Rotate camera to target rotation using spherical interpolation
            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Check if we've reached the target position and rotation (or close enough)
            if (
                Vector3.Distance(mainCamera.transform.position, targetPosition) < 0.01f
                && Quaternion.Angle(mainCamera.transform.rotation, targetRotation) < 0.1f
            )
            {
                isTransitioning = false; // Stop transitioning when target is reached
            }
        }

        switch (currentPositionIndex)
        {
            case 0:
                leftButton.gameObject.SetActive(true);
                rightButton.gameObject.SetActive(true);
                break;
            case 1:
                leftButton.gameObject.SetActive(true);
                rightButton.gameObject.SetActive(false);
                break;
            case 2:
                leftButton.gameObject.SetActive(false);
                rightButton.gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Handles left button click to move camera to previous position.
    /// </summary>
    private void OnLeftButtonClick()
    {
        // Prevent clicks during transition
        if (isTransitioning)
            return;

        // Decrease index and wrap around to end if needed
        currentPositionIndex--;
        if (currentPositionIndex < 0)
            currentPositionIndex = Positions.Count - 1;

        SetTargetTransform(currentPositionIndex);
    }

    /// <summary>
    /// Handles right button click to move camera to next position.
    /// </summary>
    private void OnRightButtonClick()
    {
        // Prevent clicks during transition
        if (isTransitioning)
            return;

        // Increase index and wrap around to beginning if needed
        currentPositionIndex++;
        if (currentPositionIndex >= Positions.Count)
            currentPositionIndex = 0;

        SetTargetTransform(currentPositionIndex);
    }

    /// <summary>
    /// Sets target position and rotation based on the selected transform index.
    /// </summary>
    /// <param name="index">Index of the target transform in the Positions list</param>
    private void SetTargetTransform(int index)
    {
        // Validate index and ensure we have positions
        if (Positions.Count == 0 || index < 0 || index >= Positions.Count)
            return;

        Transform targetTransform = Positions[index];
        targetPosition = targetTransform.position;

        // Set camera rotation to match transform's forward direction
        targetRotation = targetTransform.rotation;

        // Begin transition to new position
        isTransitioning = true;
    }
}
