using System;
using DS;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBachelor", menuName = "Visual Novel/Bachelor")]
public class NewBachelorSO : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField]
    public string _name;

    [SerializeField]
    public Sprite _portrait;

    [Header("Preferences")]
    [SerializeField]
    public BachelorPreference[] _likes;

    [SerializeField]
    public BachelorPreference[] _dislikes;

    [Header("State")]
    [SerializeField]
    public bool _isLikeDiscovered;

    [SerializeField]
    public bool _isDislikeDiscovered;

    [Header("References")]
    [SerializeField]
    public DSDialogue _dialogue;

    [SerializeField]
    public LoveMeterSO _loveMeter;

    [Header("Scene Transition")]
    [SerializeField]
    public string _nextSceneName;

    // Track if this SO has been initialized in play mode to prevent repeated resets
    private bool hasBeenInitializedInPlayMode = false;

    [SerializeField]
    public bool _HasBeenSpeedDated = false;

    [SerializeField]
    public bool _HasCompletedRealDate = false;

    [SerializeField]
    public string _LastRealDateLocation = "";

    public event Action<BachelorPreference> OnPreferenceDiscovered;

    [Serializable]
    public class BachelorPreference
    {
        public string description;
        public bool discovered;
        public Sprite icon;
    }

    /// <summary>
    /// Discovers a specific like by its index
    /// </summary>
    public void DiscoverLike(int index)
    {
        if (_likes != null && index >= 0 && index < _likes.Length)
        {
            _likes[index].discovered = true;
            _isLikeDiscovered = true;
            OnPreferenceDiscovered?.Invoke(_likes[index]);
        }
    }

    /// <summary>
    /// Discovers a specific dislike by its index
    /// </summary>
    public void DiscoverDislike(int index)
    {
        if (_dislikes != null && index >= 0 && index < _dislikes.Length)
        {
            _dislikes[index].discovered = true;
            _isDislikeDiscovered = true;
            OnPreferenceDiscovered?.Invoke(_dislikes[index]);
        }
    }

    /// <summary>
    /// Discovers all likes
    /// </summary>
    public void DiscoverAllLikes()
    {
        if (_likes != null)
        {
            foreach (var like in _likes)
            {
                like.discovered = true;
                OnPreferenceDiscovered?.Invoke(like);
            }
            _isLikeDiscovered = true;
        }
    }

    /// <summary>
    /// Discovers all dislikes
    /// </summary>
    public void DiscoverAllDislikes()
    {
        if (_dislikes != null)
        {
            foreach (var dislike in _dislikes)
            {
                dislike.discovered = true;
                OnPreferenceDiscovered?.Invoke(dislike);
            }
            _isDislikeDiscovered = true;
        }
    }

    /// <summary>
    /// Resets all discoveries
    /// </summary>
    public void ResetDiscoveries()
    {
        if (_likes != null)
        {
            foreach (var like in _likes)
            {
                like.discovered = false;
            }
        }

        if (_dislikes != null)
        {
            foreach (var dislike in _dislikes)
            {
                dislike.discovered = false;
            }
        }

        _isLikeDiscovered = false;
        _isDislikeDiscovered = false;
    }

    /// <summary>
    /// Ensures all preferences start as undiscovered (called during initialization)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    private void Awake()
    {
        // Ensure all preferences start as undiscovered unless explicitly set otherwise
        EnsureUndiscoveredState();
    }

    /// <summary>
    /// Called when the ScriptableObject is enabled
    /// </summary>
    private void OnEnable()
    {
        // Only reset discoveries at the very start of the game, not every time the SO is enabled
        // This prevents discovered preferences from being reset during gameplay
        if (Application.isPlaying && !hasBeenInitializedInPlayMode)
        {
            EnsureUndiscoveredState();
            hasBeenInitializedInPlayMode = true;
        }

#if !UNITY_EDITOR
        // In builds, check if we should reset based on save data
        CheckForRuntimeReset();
#endif
    }

#if !UNITY_EDITOR
    /// <summary>
    /// Checks if this bachelor should be reset based on save data flag
    /// Only used in builds where we can't modify ScriptableObject assets
    /// </summary>
    private void CheckForRuntimeReset()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null && saveData.ShouldResetBachelors)
        {
            ResetRuntimeState();
        }
    }

    /// <summary>
    /// Resets runtime state without modifying the asset
    /// Used in builds where ScriptableObject assets are read-only
    /// </summary>
    public void ResetRuntimeState()
    {
        // Reset runtime values without modifying the asset
        _HasBeenSpeedDated = false;
        _HasCompletedRealDate = false;
        _LastRealDateLocation = "";
        hasBeenInitializedInPlayMode = false;

        // Reset discovery flags
        _isLikeDiscovered = false;
        _isDislikeDiscovered = false;

        // Reset preferences discovered state
        if (_likes != null)
        {
            for (int i = 0; i < _likes.Length; i++)
            {
                _likes[i].discovered = false;
            }
        }

        if (_dislikes != null)
        {
            for (int i = 0; i < _dislikes.Length; i++)
            {
                _dislikes[i].discovered = false;
            }
        }

        // Reset love meter if it exists
        if (_loveMeter != null)
        {
            _loveMeter.Reset();
        }

        Debug.Log($"Reset runtime state for bachelor: {_name}");
    }
#endif

    /// <summary>
    /// Ensures all preferences start in an undiscovered state
    /// </summary>
    public void EnsureUndiscoveredState()
    {
        if (_likes != null)
        {
            foreach (var like in _likes)
            {
                like.discovered = false;
            }
        }

        if (_dislikes != null)
        {
            foreach (var dislike in _dislikes)
            {
                dislike.discovered = false;
            }
        }

        _isLikeDiscovered = false;
        _isDislikeDiscovered = false;
    }

    /// <summary>
    /// Checks if this bachelor has been dated (speed dated)
    /// </summary>
    /// <returns>True if the bachelor has been dated</returns>
    public bool HasBeenDated()
    {
        return _HasBeenSpeedDated;
    }

    /// <summary>
    /// Marks this bachelor as having been dated
    /// </summary>
    public void MarkAsDated()
    {
        _HasBeenSpeedDated = true;
    }

    /// <summary>
    /// Marks this bachelor as having completed a real date at a specific location
    /// </summary>
    /// <param name="dateLocation">The location where the real date took place (e.g., "Rooftop", "Aquarium", "Forest")</param>
    public void MarkAsRealDated(string dateLocation)
    {
        _HasBeenSpeedDated = true; // Also mark as speed dated
        _HasCompletedRealDate = true;
        _LastRealDateLocation = dateLocation;
        Debug.Log($"Marked {_name} as having completed a real date at {dateLocation}");
    }

    /// <summary>
    /// Checks if this bachelor has completed a real date
    /// </summary>
    /// <returns>True if the bachelor has completed a real date</returns>
    public bool HasCompletedRealDate()
    {
        return _HasCompletedRealDate;
    }

    /// <summary>
    /// Gets the location of the last real date
    /// </summary>
    /// <returns>The name of the last real date location, or empty string if no real date completed</returns>
    public string GetLastRealDateLocation()
    {
        return _LastRealDateLocation;
    }

    /// <summary>
    /// Gets a personalized message about the completed real date
    /// </summary>
    /// <returns>A message about the real date experience</returns>
    public string GetRealDateMessage()
    {
        if (!_HasCompletedRealDate || string.IsNullOrEmpty(_LastRealDateLocation))
        {
            return "Hey! How are you doing?";
        }

        // Check love meter to determine the tone of the message
        if (_loveMeter != null && _loveMeter.GetCurrentLove() <= 0)
        {
            return $"Look, I don't want to be rude, but our date at the {_LastRealDateLocation} didn't go well at all. I think we're just not compatible.";
        }

        return $"Hey! I had such a wonderful time at the {_LastRealDateLocation} with you.";
    }

    /// <summary>
    /// Completely resets this bachelor to initial state
    /// </summary>
    public void ResetToInitialState()
    {
        Debug.Log($"[NewBachelorSO] Starting reset for bachelor: {_name}");

        // Store previous values for logging
        bool prevSpeedDated = _HasBeenSpeedDated;
        bool prevRealDated = _HasCompletedRealDate;
        string prevDateLocation = _LastRealDateLocation;
        bool prevLikeDiscovered = _isLikeDiscovered;
        bool prevDislikeDiscovered = _isDislikeDiscovered;

        // Reset dating state
        _HasBeenSpeedDated = false;
        _HasCompletedRealDate = false;
        _LastRealDateLocation = "";

        // Reset all preferences to undiscovered
        EnsureUndiscoveredState();
        ResetDiscoveries();

        // Reset love meter if it exists
        if (_loveMeter != null)
        {
            Debug.Log($"[NewBachelorSO] Resetting love meter for {_name}");
            _loveMeter.Reset();
        }

        // Reset the play mode initialization flag
        hasBeenInitializedInPlayMode = false;

        Debug.Log(
            $"[NewBachelorSO] Reset complete for {_name}. SpeedDated: {prevSpeedDated} -> {_HasBeenSpeedDated}, "
                + $"RealDated: {prevRealDated} -> {_HasCompletedRealDate}, DateLocation: '{prevDateLocation}' -> '{_LastRealDateLocation}', "
                + $"LikeDiscovered: {prevLikeDiscovered} -> {_isLikeDiscovered}, "
                + $"DislikeDiscovered: {prevDislikeDiscovered} -> {_isDislikeDiscovered}"
        );

        Debug.Log($"Bachelor {_name} has been completely reset to initial state");
    }

    /// <summary>
    /// For testing purposes - discovers all preferences
    /// </summary>
    [ContextMenu("Discover All Preferences (Testing)")]
    public void DiscoverAllPreferencesForTesting()
    {
        DiscoverAllLikes();
        DiscoverAllDislikes();
    }

    /// <summary>
    /// For testing purposes - reset this bachelor completely
    /// </summary>
    [ContextMenu("Test Reset Bachelor")]
    public void TestResetBachelor()
    {
        Debug.Log(
            $"Before reset - {_name}: SpeedDated={_HasBeenSpeedDated}, LoveMetr={(_loveMeter?.GetCurrentLove() ?? -1)}"
        );
        ResetToInitialState();
        Debug.Log(
            $"After reset - {_name}: SpeedDated={_HasBeenSpeedDated}, LoveMeter={(_loveMeter?.GetCurrentLove() ?? -1)}"
        );
    }
}
