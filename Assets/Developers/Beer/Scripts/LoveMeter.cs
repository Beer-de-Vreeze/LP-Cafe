using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated love meter UI component that displays a crescent meter with a rotating dial.
/// The dial animates between 25-75 degrees based on the LoveMeterSO values.
/// </summary>
public class LoveMeter : MonoBehaviour
{
    [Header("Love Meter Configuration")]
    [SerializeField]
    private LoveMeterSO loveMeterData;

    [Header("UI References")]
    [SerializeField]
    private Image crescentMeter;

    [SerializeField]
    private RectTransform dialTransform;

    [SerializeField]
    private Image dialImage;

    [Header("Animation Settings")]
    [SerializeField]
    private float animationDuration = 1.0f;

    [SerializeField]
    private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private bool animateOnStart = true;

    [Header("Visual Settings")]
    [SerializeField]
    private Color crescentColor = Color.white;

    [SerializeField]
    private Color dialColor = Color.red;

    [SerializeField]
    private float dialOffset = 0f;

    [Header("Movement Settings")]
    // Animation range (in degrees)
    private const float MIN_ANGLE = 80f;
    private const float MAX_ANGLE = -80f;
    private float currentAngle;
    private Vector2 currentPosition;
    private Vector2 initialDialPosition;
    private int lastLoveValue = -1;
    private Tween currentTween;

    [Header("Fade Animation Settings")]
    [SerializeField]
    private float fadeInDuration = 0.5f;

    [SerializeField]
    private float stayDuration = 2.0f;

    [SerializeField]
    private float fadeOutDuration = 0.5f;

    [SerializeField]
    private CanvasGroup canvasGroup;

    void Start()
    {
        InitializeLoveMeter();
    }

    void OnDestroy()
    {
        // Clean up DOTween animation
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        if (loveMeterData != null && loveMeterData.LoveChangedEvent != null)
        {
            loveMeterData.LoveChangedEvent.RemoveListener(OnLoveValueChanged);
        }
    }

    /// <summary>
    /// Initialize the love meter UI components and event listeners
    /// </summary>
    private void InitializeLoveMeter()
    {
        // Validate required components
        if (!ValidateComponents())
            return;

        // Set up visual appearance and base position
        SetupVisualElements();
        InitializeDialPosition();

        // Subscribe to love meter events
        if (loveMeterData != null)
        {
            // Check if the love meter data is properly initialized
            if (!loveMeterData.IsInitialized())
            {
                Debug.LogWarning(
                    $"LoveMeterSO '{loveMeterData.name}' is not properly initialized!"
                );
                return;
            }

            loveMeterData.LoveChangedEvent.AddListener(OnLoveValueChanged); // Set initial value
            if (animateOnStart)
            {
                // Start from minimum angle and animate to current value
                currentAngle = MIN_ANGLE;
                SetDialTransform(currentAngle);
                OnLoveValueChanged(loveMeterData.GetCurrentLove());
            }
            else
            {
                // Set directly to current value without animation
                UpdateDialPosition(loveMeterData.GetCurrentLove(), false);
            }
        }
    }

    /// <summary>
    /// Validate that all required UI components are assigned
    /// </summary>
    private bool ValidateComponents()
    {
        bool isValid = true;

        if (loveMeterData == null)
        {
            Debug.LogError($"LoveMeter '{gameObject.name}': LoveMeterSO reference is missing!");
            isValid = false;
        }

        if (crescentMeter == null)
        {
            Debug.LogError(
                $"LoveMeter '{gameObject.name}': Crescent meter Image reference is missing!"
            );
            isValid = false;
        }

        if (dialTransform == null)
        {
            Debug.LogError(
                $"LoveMeter '{gameObject.name}': Dial RectTransform reference is missing!"
            );
            isValid = false;
        }

        if (dialImage == null)
        {
            Debug.LogError($"LoveMeter '{gameObject.name}': Dial Image reference is missing!");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Set up the visual appearance of the love meter components
    /// </summary>
    private void SetupVisualElements()
    {
        if (crescentMeter != null)
        {
            crescentMeter.color = crescentColor;
        }

        if (dialImage != null)
        {
            dialImage.color = dialColor;
        }
    }

    /// <summary>
    /// Called when the love value changes in the LoveMeterSO
    /// </summary>
    /// <param name="newLoveValue">The new love value</param>
    private void OnLoveValueChanged(int newLoveValue)
    {
        if (lastLoveValue == newLoveValue)
            return;

        lastLoveValue = newLoveValue;
        UpdateDialPosition(newLoveValue, true);
    }

    /// <summary>
    /// Update the dial rotation based on the love value
    /// </summary>
    /// <param name="loveValue">Current love value</param>
    /// <param name="animate">Whether to animate the transition</param>
    private void UpdateDialPosition(int loveValue, bool animate)
    {
        if (loveMeterData == null || dialTransform == null)
            return;

        // Calculate the target angle based on love percentage
        float lovePercentage = Mathf.Clamp01((float)loveValue / loveMeterData._maxLove);
        float targetAngle = Mathf.Lerp(MIN_ANGLE, MAX_ANGLE, lovePercentage) + dialOffset;

        if (animate && gameObject.activeInHierarchy)
        {
            AnimateToAngle(targetAngle);
        }
        else
        {
            currentAngle = targetAngle;
            SetDialTransform(currentAngle);
        }
    }

    /// <summary>
    /// Animate the dial to the target angle using DOTween
    /// </summary>
    /// <param name="targetAngle">The target rotation angle</param>
    private void AnimateToAngle(float targetAngle)
    {
        // Kill any existing animation
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // Store starting angle
        float startAngle = currentAngle;

        // Create new DOTween animation for rotation only
        currentTween = DOTween
            .To(
                () => 0f,
                (progress) =>
                {
                    // Interpolate only the angle
                    currentAngle = Mathf.Lerp(startAngle, targetAngle, progress);
                    SetDialTransform(currentAngle);
                },
                1f,
                animationDuration
            )
            .SetEase(animationCurve);
    }

    /// <summary>
    /// Smooth animation to target angle with easing
    /// </summary>
    /// <param name="targetAngle">The target rotation angle</param>
    private void AnimateToAngleSmooth(float targetAngle)
    {
        // Kill any existing dial animation
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // Store starting angle
        float startAngle = currentAngle;

        // Create smooth DOTween animation with easing
        currentTween = DOTween
            .To(
                () => 0f,
                (progress) =>
                {
                    // Interpolate the angle with smooth easing
                    currentAngle = Mathf.Lerp(startAngle, targetAngle, progress);
                    SetDialTransform(currentAngle);
                },
                1f,
                animationDuration
            )
            .SetEase(Ease.OutBack, 1.2f); // Bouncy effect for more appealing animation
    }

    /// <summary>
    /// Animated sequence to fade in the love meter, animate the dial, stay visible, then fade out
    /// </summary>
    /// <param name="targetLoveValue">The love value to animate the dial to</param>
    /// <param name="onComplete">Optional callback when the entire sequence is complete</param>
    public void ShowLoveMeterWithAnimation(int targetLoveValue, System.Action onComplete = null)
    {
        if (loveMeterData == null || !gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        // Ensure we have a CanvasGroup for fading
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Kill any existing fade animations
        DOTween.Kill(canvasGroup);

        // Calculate target angle for the dial
        float lovePercentage = Mathf.Clamp01((float)targetLoveValue / loveMeterData._maxLove);
        float targetAngle = Mathf.Lerp(MIN_ANGLE, MAX_ANGLE, lovePercentage) + dialOffset;

        // Create the animation sequence
        Sequence fadeSequence = DOTween.Sequence();

        // Start invisible
        canvasGroup.alpha = 0f;

        // 1. Fade in
        fadeSequence.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));

        // 2. Animate dial to target position (concurrent with fade in completion)
        fadeSequence.AppendCallback(() =>
        {
            AnimateToAngleSmooth(targetAngle);
        });

        // 3. Stay visible for specified duration
        fadeSequence.AppendInterval(stayDuration);

        // 4. Fade out
        fadeSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

        // 5. Complete callback
        fadeSequence.OnComplete(() =>
        {
            onComplete?.Invoke();
        });

        // Play the sequence
        fadeSequence.Play();
    }

    /// <summary>
    /// Quick show love meter animation using current love value
    /// </summary>
    /// <param name="onComplete">Optional callback when animation completes</param>
    public void ShowCurrentLoveWithAnimation(System.Action onComplete = null)
    {
        if (loveMeterData != null)
        {
            ShowLoveMeterWithAnimation(loveMeterData.GetCurrentLove(), onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Instantly hide the love meter (sets alpha to 0)
    /// </summary>
    public void HideLoveMeter()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Instantly show the love meter (sets alpha to 1)
    /// </summary>
    public void ShowLoveMeter()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Set the dial rotation to the specified angle (legacy method - also updates position)
    /// </summary>
    /// <param name="angle">Rotation angle in degrees</param>
    private void SetDialRotation(float angle)
    {
        SetDialTransform(angle);
    }

    /// <summary>
    /// Manually set the love meter data reference
    /// </summary>
    /// <param name="newLoveMeterData">The new LoveMeterSO to use</param>
    public void SetLoveMeterData(LoveMeterSO newLoveMeterData)
    {
        SetLoveMeterDataMaintainPosition(newLoveMeterData, false);
    }

    /// <summary>
    /// Set love meter data while maintaining the current dial position if possible.
    /// Useful when switching between bachelors with the same love level.
    /// </summary>
    /// <param name="newLoveMeterData">The new LoveMeterSO to use</param>
    /// <param name="forceRefresh">If true, always refresh the dial position even if love values match</param>
    public void SetLoveMeterDataMaintainPosition(
        LoveMeterSO newLoveMeterData,
        bool forceRefresh = false
    )
    {
        // If the new data is the same as current, no need to change anything
        if (loveMeterData == newLoveMeterData && !forceRefresh)
            return; // Store current dial angle and position to potentially maintain them
        float currentDialAngle = currentAngle;
        Vector2 currentDialPosition = currentPosition;
        int currentLoveValue = -1;
        bool hadPreviousData = loveMeterData != null;

        if (hadPreviousData)
        {
            currentLoveValue = loveMeterData.GetCurrentLove();
        }

        // Unsubscribe from old data
        if (loveMeterData != null && loveMeterData.LoveChangedEvent != null)
        {
            loveMeterData.LoveChangedEvent.RemoveListener(OnLoveValueChanged);
        }

        // Set new data
        loveMeterData = newLoveMeterData;

        // Check if we should maintain position
        bool shouldMaintainPosition =
            !forceRefresh
            && hadPreviousData
            && newLoveMeterData != null
            && newLoveMeterData.GetCurrentLove() == currentLoveValue;

        if (shouldMaintainPosition)
        {
            // Maintain current position and angle
            lastLoveValue = currentLoveValue;
            currentAngle = currentDialAngle;
            currentPosition = currentDialPosition;
        }
        else
        {
            // Reset to force update with new value
            lastLoveValue = -1;
        }

        // Subscribe to new data and update
        if (gameObject.activeInHierarchy && newLoveMeterData != null)
        {
            // Subscribe to the new love meter's events
            if (newLoveMeterData.LoveChangedEvent != null)
            {
                newLoveMeterData.LoveChangedEvent.AddListener(OnLoveValueChanged);
            }

            // Update visuals
            SetupVisualElements();

            // Update position if needed
            if (!shouldMaintainPosition)
            {
                UpdateDialPosition(newLoveMeterData.GetCurrentLove(), animateOnStart);
            }
        }
    }

    /// <summary>
    /// Force refresh the dial position (useful for testing or manual updates)
    /// </summary>
    public void RefreshMeter()
    {
        if (loveMeterData != null)
        {
            OnLoveValueChanged(loveMeterData.GetCurrentLove());
        }
    }

    /// <summary>
    /// Force a complete refresh of the love meter, ignoring current state
    /// </summary>
    public void ForceRefresh()
    {
        if (loveMeterData != null)
        {
            SetLoveMeterDataMaintainPosition(loveMeterData, true);
        }
    }

    /// <summary>
    /// Smoothly transition to a new love meter data, maintaining position if values match
    /// </summary>
    /// <param name="newLoveMeterData">The new love meter data</param>
    /// <param name="animateTransition">Whether to animate any required position changes</param>
    public void TransitionToLoveMeterData(
        LoveMeterSO newLoveMeterData,
        bool animateTransition = true
    )
    {
        bool previousAnimateOnStart = animateOnStart;
        animateOnStart = animateTransition;

        SetLoveMeterDataMaintainPosition(newLoveMeterData, false);

        animateOnStart = previousAnimateOnStart;
    }

    /// <summary>
    /// Check if the love meter currently has valid data
    /// </summary>
    /// <returns>True if the love meter has valid LoveMeterSO data</returns>
    public bool HasValidData()
    {
        return loveMeterData != null && loveMeterData.IsInitialized();
    }

    /// <summary>
    /// Get the current love meter data reference
    /// </summary>
    /// <returns>The current LoveMeterSO, or null if none is set</returns>
    public LoveMeterSO GetCurrentLoveMeterData()
    {
        return loveMeterData;
    }

    /// <summary>
    /// Test method to simulate love value changes (for debugging)
    /// </summary>
    [ContextMenu("Test Love Increase")]
    private void TestLoveIncrease()
    {
        if (loveMeterData != null)
        {
            loveMeterData.IncreaseLove(10);
        }
    }

    /// <summary>
    /// Test method to simulate love value changes (for debugging)
    /// </summary>
    [ContextMenu("Test Love Decrease")]
    private void TestLoveDecrease()
    {
        if (loveMeterData != null)
        {
            loveMeterData.DecreaseLove(10);
        }
    }

    /// <summary>
    /// Initialize the dial position using the dial's current position in the scene
    /// </summary>
    private void InitializeDialPosition()
    {
        if (dialTransform != null)
        {
            // Store the dial's initial position (only needs to be done once)
            initialDialPosition = dialTransform.anchoredPosition;

            // Set current position to initial position
            currentPosition = initialDialPosition;
        }
    }

    /// <summary>
    /// Get the position for a given angle on the crescent meter
    /// </summary>
    /// <param name="angle">The angle in degrees</param>
    /// <returns>The position as Vector2</returns>
    private Vector2 GetPositionForAngle(float angle)
    {
        // For now, return the initial position since we're only rotating the dial
        // This can be expanded later if you want the dial to move along a circular path
        return initialDialPosition;
    }

    /// <summary>
    /// Set both the dial rotation and position
    /// </summary>
    /// <param name="angle">Rotation angle in degrees</param>
    /// <param name="position">Position as Vector2</param>
    private void SetDialTransform(float angle, Vector2 position)
    {
        if (dialTransform != null)
        {
            dialTransform.rotation = Quaternion.Euler(0, 0, angle);
            dialTransform.anchoredPosition = position;
            currentPosition = position;
        }
    }

    /// <summary>
    /// Set only the dial rotation
    /// </summary>
    /// <param name="angle">Rotation angle in degrees</param>
    private void SetDialTransform(float angle)
    {
        if (dialTransform != null)
        {
            dialTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// Get the current movement offset from the initial position
    /// </summary>
    /// <returns>The horizontal offset from initial position</returns>
    public float GetCurrentMovementOffset()
    {
        // Ensure initialDialPosition is set
        if (initialDialPosition == Vector2.zero && dialTransform != null)
        {
            initialDialPosition = dialTransform.anchoredPosition;
        }

        return currentPosition.x - initialDialPosition.x;
    }

    /// <summary>
    /// Test method to visualize movement range (for debugging)
    /// </summary>
    [ContextMenu("Test Movement Range")]
    private void TestMovementRange()
    {
        if (loveMeterData != null)
        {
            StartCoroutine(TestMovementCoroutine());
        }
    }

    /// <summary>
    /// Coroutine to demonstrate the movement range
    /// </summary>
    private System.Collections.IEnumerator TestMovementCoroutine()
    {
        int originalLove = loveMeterData.GetCurrentLove();

        // Move to minimum
        loveMeterData.Reset();
        yield return new WaitForSeconds(animationDuration + 0.5f);

        // Move to maximum
        loveMeterData.SetToMaxLove();
        yield return new WaitForSeconds(animationDuration + 0.5f);

        // Return to original
        loveMeterData.Reset();
        loveMeterData.IncreaseLove(originalLove);
    }

    /// <summary>
    /// Reset the initial dial position to the current position
    /// Useful if the dial's position changes in the editor or at runtime
    /// </summary>
    public void ResetInitialPosition()
    {
        if (dialTransform != null)
        {
            initialDialPosition = dialTransform.anchoredPosition;
            currentPosition = initialDialPosition;
        }
    }
}
