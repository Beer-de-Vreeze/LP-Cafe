using TMPro;
using UnityEngine;

public class TestLoveMeter : MonoBehaviour
{
    [Header("Love Meter Settings")]
    [Tooltip("The LoveMeterSO to monitor and display")]
    public LoveMeterSO loveMeter;

    [Header("UI Components")]
    [Tooltip("TextMeshPro component to display love value")]
    public TextMeshProUGUI loveValueText;

    [Header("Display Format")]
    [Tooltip("Format string for displaying love value (use {0} for current, {1} for max)")]
    public string displayFormat = "Love: {0}/{1}";

    void Start()
    {
        // Initialize the display
        if (loveMeter != null)
        {
            // Subscribe to love change events
            loveMeter.LoveChangedEvent.AddListener(OnLoveChanged);

            // Update display with initial value
            UpdateDisplay();
        }
        else
        {
            Debug.LogWarning("TestLoveMeter: No LoveMeterSO assigned!");
        }

        // Check if TextMeshPro is assigned
        if (loveValueText == null)
        {
            Debug.LogWarning("TestLoveMeter: No TextMeshProUGUI assigned!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (loveMeter != null)
        {
            loveMeter.LoveChangedEvent.RemoveListener(OnLoveChanged);
        }
    }

    /// <summary>
    /// Called when the love value changes
    /// </summary>
    /// <param name="newValue">The new love value</param>
    private void OnLoveChanged(int newValue)
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Updates the TextMeshPro display with current love value
    /// </summary>
    private void UpdateDisplay()
    {
        if (loveValueText != null && loveMeter != null)
        {
            string displayText = string.Format(
                displayFormat,
                loveMeter.GetCurrentLove(),
                loveMeter._maxLove
            );
            loveValueText.text = displayText;
        }
    }

    // Test methods you can call from other scripts or buttons
    [ContextMenu("Test Increase Love")]
    public void TestIncreaseLove()
    {
        if (loveMeter != null)
        {
            loveMeter.IncreaseLove(10);
        }
    }

    [ContextMenu("Test Decrease Love")]
    public void TestDecreaseLove()
    {
        if (loveMeter != null)
        {
            loveMeter.DecreaseLove(5);
        }
    }

    [ContextMenu("Test Reset Love")]
    public void TestResetLove()
    {
        if (loveMeter != null)
        {
            loveMeter.Reset();
        }
    }
}
