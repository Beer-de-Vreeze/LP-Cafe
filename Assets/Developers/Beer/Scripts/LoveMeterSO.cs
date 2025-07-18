using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Scriptable Object that manages love points for a bachelor character.
/// Tracks current love value, allows increasing/decreasing, and notifies listeners of changes.
/// </summary>
[CreateAssetMenu(fileName = "New Love Meter", menuName = "Bachelor/New LoveMeter", order = 1)]
public class LoveMeterSO : ScriptableObject
{
    [Tooltip("Maximum love value this bachelor can reach")]
    public int _maxLove = 5;

    [Tooltip("Current love value")]
    [SerializeField]
    private int _currentLove = 3;

    [Tooltip("Love needed to go on a real date")]
    public int _loveNeededForRealDate;

    [System.NonSerialized]
    public UnityEvent<int> LoveChangedEvent;

    /// <summary>
    /// Public property to access the current love value
    /// </summary>
    public int CurrentLove
    {
        get { return _currentLove; }
        set
        {
            _currentLove = value;
            if (LoveChangedEvent != null)
                LoveChangedEvent.Invoke(_currentLove);
        }
    }

    public virtual void OnEnable()
    {
        // Only initialize if this is a fresh ScriptableObject with default value
        // This preserves saved values while ensuring new instances start at 3
        if (_currentLove == 0)
        {
            _currentLove = 3;
        }

        // Initialize the event if it doesn't exist yet
        if (LoveChangedEvent == null)
        {
            LoveChangedEvent = new UnityEvent<int>();
        }

        Debug.Log($"LoveMeterSO {name} enabled with love value: {_currentLove}");
    } // Note: ScriptableObjects don't have Update() method.

    // Validation is handled in the methods that modify _currentLove instead.

    /// <summary>
    /// Increase the love value by the specified amount
    /// </summary>
    public virtual void IncreaseLove(int amount)
    {
        _currentLove = Mathf.Min(_currentLove + amount, _maxLove);

        // Notify listeners about the change
        LoveChangedEvent?.Invoke(_currentLove);

        Debug.Log($"Batchelor love increased by {amount}. Total Love: {_currentLove}");
    }

    /// <summary>
    /// Decrease the love value by the specified amount
    /// </summary>
    public virtual void DecreaseLove(int amount)
    {
        _currentLove = Mathf.Max(_currentLove - amount, 0);

        // Notify listeners about the change
        LoveChangedEvent?.Invoke(_currentLove);

        Debug.Log($"Batchelor love decreased by {amount}. Total Love: {_currentLove}");
    }

    /// <summary>
    /// Set the maximum possible love value
    /// </summary>
    public virtual void SetMaxLove(int amount)
    {
        _maxLove = amount;

        // Adjust current love if it exceeds the new maximum
        if (_currentLove > _maxLove)
        {
            _currentLove = _maxLove;
            LoveChangedEvent?.Invoke(_currentLove);
        }
    }

    /// <summary>
    /// Get the current love value
    /// </summary>
    public virtual int GetCurrentLove()
    {
        return _currentLove;
    }

    /// <summary>
    /// Reset love value back to zero
    /// </summary>
    public virtual void Reset()
    {
        _currentLove = 3;

        // Ensure the event is initialized before invoking
        if (LoveChangedEvent == null)
        {
            LoveChangedEvent = new UnityEvent<int>();
        }

        LoveChangedEvent.Invoke(_currentLove);

        Debug.Log($"LoveMeter reset to {_currentLove}");
    }

    /// <summary>
    /// Set love to maximum value
    /// </summary>
    public virtual void SetToMaxLove()
    {
        _currentLove = _maxLove;
        LoveChangedEvent?.Invoke(_currentLove);
    }

    /// <summary>
    /// Check if love is at maximum value
    /// </summary>
    public virtual bool IsMaxLove()
    {
        return _currentLove >= _maxLove;
    }

    /// <summary>
    /// Check if current love meets the requirement for going on a real date
    /// </summary>
    public virtual bool CanGoOnRealDate()
    {
        return _currentLove >= _loveNeededForRealDate;
    }

    public bool IsInitialized()
    {
        try
        {
            // Check that our event system is initialized
            if (LoveChangedEvent == null)
                return false;

            // We don't strictly need bachelor to be non-null for the meter to work
            // Just check that we can get the current love value
            var currentValue = GetCurrentLove();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test method to reset love meter from the inspector
    /// </summary>
    [ContextMenu("Test Reset Love Meter")]
    private void TestReset()
    {
        Debug.Log($"Before reset - Love: {_currentLove}");
        Reset();
        Debug.Log($"After reset - Love: {_currentLove}");
    }

    /// <summary>
    /// Check if this love meter is properly reset to initial state
    /// </summary>
    public bool IsReset()
    {
        return _currentLove == 3 && LoveChangedEvent != null;
    }
}
