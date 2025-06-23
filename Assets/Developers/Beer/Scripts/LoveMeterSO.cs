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
    public int _currentLove;

    [System.NonSerialized]
    public UnityEvent<int> LoveChangedEvent;

    public virtual void OnEnable()
    {
        // Initialize the love value when the scriptable object is enabled
        _currentLove = 3;

        // Initialize the event if it doesn't exist yet
        if (LoveChangedEvent == null)
        {
            LoveChangedEvent = new UnityEvent<int>();
        }
    } // Note: ScriptableObjects don't have Update() method.

    // Validation is handled in the methods that modify _currentLove instead.

    /// <summary>
    /// Increase the love value by the specified amount
    /// </summary>
    public virtual void IncreaseLove(int amount)
    {
        _currentLove += amount;

        // Notify listeners about the change
        LoveChangedEvent.Invoke(_currentLove);

        Debug.Log($"Batchelor love increased by {amount}. Total Love: {_currentLove}");
    }

    /// <summary>
    /// Decrease the love value by the specified amount
    /// </summary>
    public virtual void DecreaseLove(int amount)
    {
        _currentLove -= amount;

        // Make sure love doesn't go below zero
        if (_currentLove < 0)
        {
            _currentLove = 0;
        }

        // Notify listeners about the change
        LoveChangedEvent.Invoke(_currentLove);

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
            LoveChangedEvent.Invoke(_currentLove);
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
        _currentLove = 0;
        LoveChangedEvent.Invoke(_currentLove);
    }

    /// <summary>
    /// Set love to maximum value
    /// </summary>
    public virtual void SetToMaxLove()
    {
        _currentLove = _maxLove;
        LoveChangedEvent.Invoke(_currentLove);
    }

    /// <summary>
    /// Check if love is at maximum value
    /// </summary>
    public virtual bool IsMaxLove()
    {
        return _currentLove >= _maxLove;
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
}
