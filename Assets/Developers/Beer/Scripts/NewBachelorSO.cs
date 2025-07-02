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

            // Synchronize state with save data
            SynchronizeWithSaveData();

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
    /// Checks both the ScriptableObject flag and the save system for persistence
    /// </summary>
    /// <returns>True if the bachelor has been dated</returns>
    public bool HasBeenDated()
    {
        Debug.Log($"[HasBeenDated] Checking for {_name}");
        Debug.Log($"[HasBeenDated] Local flag _HasBeenSpeedDated: {_HasBeenSpeedDated}");

        // Always check the save system first (authoritative source)
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null && saveData.DatedBachelors != null)
        {
            bool savedAsDated = saveData.DatedBachelors.Contains(_name);
            Debug.Log(
                $"[HasBeenDated] Save data check - DatedBachelors contains {_name}: {savedAsDated}"
            );

            if (savedAsDated && !_HasBeenSpeedDated)
            {
                // Update the local flag to match save data
                _HasBeenSpeedDated = true;
                Debug.Log($"[HasBeenDated] Updated local flag to match save data");
            }
            return savedAsDated;
        }
        else
        {
            Debug.Log($"[HasBeenDated] No save data or DatedBachelors list found");
        }

        // Fallback to local flag if no save data
        Debug.Log($"[HasBeenDated] Returning local flag value: {_HasBeenSpeedDated}");
        return _HasBeenSpeedDated;
    }

    /// <summary>
    /// Marks this bachelor as having been dated
    /// </summary>
    public void MarkAsDated()
    {
        Debug.Log($"[MarkAsDated] Marking {_name} as speed dated");

        // Validate name before saving
        if (string.IsNullOrEmpty(_name))
        {
            Debug.LogError(
                $"[MarkAsDated] Cannot save bachelor with empty name! Bachelor object: {name}. Please set the _name field in the Inspector."
            );
            return;
        }

        _HasBeenSpeedDated = true;
        Debug.Log($"[MarkAsDated] Set local flag _HasBeenSpeedDated to true");

        // Also ensure it's saved to the save system
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            saveData = new SaveData();
            Debug.Log($"[MarkAsDated] Created new SaveData");
        }

        if (!saveData.DatedBachelors.Contains(_name))
        {
            saveData.DatedBachelors.Add(_name);
            SaveSystem.SerializeData(saveData);
            Debug.Log($"[MarkAsDated] Added {_name} to DatedBachelors in save system and saved");
        }
        else
        {
            Debug.Log($"[MarkAsDated] {_name} already exists in DatedBachelors list");
        }
    }

    /// <summary>
    /// Marks this bachelor as having completed a real date at a specific location
    /// </summary>
    /// <param name="dateLocation">The location where the real date took place (e.g., "Rooftop", "Aquarium", "Forest")</param>
    public void MarkAsRealDated(string dateLocation)
    {
        Debug.Log($"[MarkAsRealDated] Marking {_name} as real dated at location: '{dateLocation}'");

        // Validate name before saving
        if (string.IsNullOrEmpty(_name))
        {
            Debug.LogError(
                $"[MarkAsRealDated] Cannot save bachelor with empty name! Bachelor object: {name}. Please set the _name field in the Inspector."
            );
            return;
        }

        _HasBeenSpeedDated = true; // Also mark as speed dated
        _HasCompletedRealDate = true;
        _LastRealDateLocation = dateLocation;

        Debug.Log(
            $"[MarkAsRealDated] Set flags - SpeedDated: {_HasBeenSpeedDated}, RealDated: {_HasCompletedRealDate}, Location: '{_LastRealDateLocation}'"
        );

        // Also ensure it's saved to the save system
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        // Add to both lists to ensure compatibility
        if (!saveData.DatedBachelors.Contains(_name))
        {
            saveData.DatedBachelors.Add(_name);
            Debug.Log($"[MarkAsRealDated] Added {_name} to DatedBachelors list");
        }

        if (!saveData.RealDatedBachelors.Contains(_name))
        {
            saveData.RealDatedBachelors.Add(_name);
            Debug.Log($"[MarkAsRealDated] Added {_name} to RealDatedBachelors list");
        }

        SaveSystem.SerializeData(saveData);
        Debug.Log(
            $"Marked {_name} as having completed a real date at {dateLocation} and saved to system"
        );
    }

    /// <summary>
    /// Checks if this bachelor has completed a real date
    /// Checks both the ScriptableObject flag and the save system for persistence
    /// </summary>
    /// <returns>True if the bachelor has completed a real date</returns>
    public bool HasCompletedRealDate()
    {
        Debug.Log($"[HasCompletedRealDate] Checking for {_name}");
        Debug.Log(
            $"[HasCompletedRealDate] Local flag _HasCompletedRealDate: {_HasCompletedRealDate}"
        );

        // Always check the save system first (authoritative source)
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null && saveData.RealDatedBachelors != null)
        {
            bool savedAsRealDated = saveData.RealDatedBachelors.Contains(_name);
            Debug.Log(
                $"[HasCompletedRealDate] Save data check - RealDatedBachelors contains {_name}: {savedAsRealDated}"
            );

            if (savedAsRealDated && !_HasCompletedRealDate)
            {
                // Update the local flags to match save data
                _HasCompletedRealDate = true;
                _HasBeenSpeedDated = true; // Real dates also count as speed dates
                Debug.Log($"[HasCompletedRealDate] Updated local flags based on save data");
            }
            return savedAsRealDated;
        }
        else
        {
            Debug.Log($"[HasCompletedRealDate] No save data or RealDatedBachelors list found");
        }

        // Fallback to local flag if no save data
        Debug.Log($"[HasCompletedRealDate] Returning local flag value: {_HasCompletedRealDate}");
        return _HasCompletedRealDate;
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

        // Also remove from save data to ensure complete reset
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null)
        {
            bool removedFromDated = false;
            bool removedFromRealDated = false;

            if (saveData.DatedBachelors != null && saveData.DatedBachelors.Contains(_name))
            {
                saveData.DatedBachelors.Remove(_name);
                removedFromDated = true;
                Debug.Log($"[NewBachelorSO] Removed {_name} from DatedBachelors in save data");
            }

            if (saveData.RealDatedBachelors != null && saveData.RealDatedBachelors.Contains(_name))
            {
                saveData.RealDatedBachelors.Remove(_name);
                removedFromRealDated = true;
                Debug.Log($"[NewBachelorSO] Removed {_name} from RealDatedBachelors in save data");
            }

            if (removedFromDated || removedFromRealDated)
            {
                SaveSystem.SerializeData(saveData);
                Debug.Log($"[NewBachelorSO] Saved updated save data");
            }
        }

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

    /// <summary>
    /// For testing purposes - manually test real date messages
    /// </summary>
    [ContextMenu("Test Real Date Messages")]
    public void TestRealDateMessages()
    {
        Debug.Log("=== Testing Real Date Messages ===");
        Debug.Log($"Bachelor: {_name}");
        Debug.Log($"HasCompletedRealDate: {HasCompletedRealDate()}");
        Debug.Log($"LastRealDateLocation: '{_LastRealDateLocation}'");
        Debug.Log($"Love Meter Current: {(_loveMeter?.GetCurrentLove() ?? -1)}");

        string message = GetRealDateMessage();
        Debug.Log($"Current Message: {message}");
        Debug.Log("=== End Test ===");
    }

    /// <summary>
    /// For testing purposes - simulate completing a real date
    /// </summary>
    [ContextMenu("Simulate Real Date Completion")]
    public void SimulateRealDateCompletion()
    {
        string[] testLocations = { "Rooftop", "Aquarium", "Forest" };
        string randomLocation = testLocations[UnityEngine.Random.Range(0, testLocations.Length)];

        MarkAsRealDated(randomLocation);

        // Set a random love level for testing
        if (_loveMeter != null)
        {
            int randomLove = UnityEngine.Random.Range(0, 6);
            _loveMeter._currentLove = randomLove;
            Debug.Log($"Set love level to {randomLove} for testing");
        }

        Debug.Log($"Simulated real date completion at {randomLocation}");
        TestRealDateMessages();
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
        Debug.Log($"[GetRealDateMessage] Checking message for {_name}");
        Debug.Log($"[GetRealDateMessage] HasCompletedRealDate: {HasCompletedRealDate()}");
        Debug.Log($"[GetRealDateMessage] _LastRealDateLocation: '{_LastRealDateLocation}'");
        Debug.Log($"[GetRealDateMessage] _HasCompletedRealDate flag: {_HasCompletedRealDate}");

        // Check if bachelor has completed a real date
        if (!HasCompletedRealDate() || string.IsNullOrEmpty(_LastRealDateLocation))
        {
            Debug.Log($"[GetRealDateMessage] No real date completed, returning default message");
            return "Hey! How are you doing?";
        }

        // Get current love level for more nuanced responses
        int currentLove = 0;
        if (_loveMeter != null)
        {
            currentLove = _loveMeter.GetCurrentLove();
            Debug.Log($"[GetRealDateMessage] Love meter found, current love: {currentLove}");
        }
        else
        {
            Debug.LogWarning($"[GetRealDateMessage] No love meter assigned to {_name}");
        }

        string location = _LastRealDateLocation.ToLower().Trim();
        Debug.Log(
            $"[GetRealDateMessage] Processing location: '{location}' with love level: {currentLove}"
        );

        // Very bad date (love <= 0)
        if (currentLove <= 0)
        {
            Debug.Log($"[GetRealDateMessage] Returning bad date message");
            return GetBadDateMessage(location);
        }
        // Mediocre date (love 1-2)
        else if (currentLove <= 2)
        {
            Debug.Log($"[GetRealDateMessage] Returning mediocre date message");
            return GetMediocreeDateMessage(location);
        }
        // Good date (love 3-4)
        else if (currentLove <= 4)
        {
            Debug.Log($"[GetRealDateMessage] Returning good date message");
            return GetGoodDateMessage(location);
        }
        // Amazing date (love 5+)
        else
        {
            Debug.Log($"[GetRealDateMessage] Returning amazing date message");
            return GetAmazingDateMessage(location);
        }
    }

    private string GetBadDateMessage(string location)
    {
        Debug.Log($"[GetBadDateMessage] Processing bad date message for location: '{location}'");
        switch (location)
        {
            case "rooftop":
                return "That rooftop date was... awkward. The view was nice, but I don't think we clicked.";
            case "aquarium":
                return "I enjoyed looking at the fish more than our conversation at the aquarium, to be honest.";
            case "forest":
                return "Our forest walk felt more like we were strangers hiking in silence. Not great.";
            default:
                Debug.Log(
                    $"[GetBadDateMessage] Using default bad message for location: '{location}'"
                );
                return $"Look, I don't want to be rude, but our date at the {_LastRealDateLocation} didn't go well at all. I think we're just not compatible.";
        }
    }

    private string GetMediocreeDateMessage(string location)
    {
        Debug.Log(
            $"[GetMediocreeDateMessage] Processing mediocre date message for location: '{location}'"
        );
        switch (location)
        {
            case "rooftop":
                return "The rooftop had a lovely view, and you seem nice enough. Maybe we could hang out again sometime?";
            case "aquarium":
                return "The aquarium was interesting, and you had some good insights about the exhibits. Not bad!";
            case "forest":
                return "Our forest walk was peaceful. You're decent company, I'll give you that.";
            default:
                Debug.Log(
                    $"[GetMediocreeDateMessage] Using default mediocre message for location: '{location}'"
                );
                return $"Our time at the {_LastRealDateLocation} was okay. You seem like a nice person.";
        }
    }

    private string GetGoodDateMessage(string location)
    {
        Debug.Log($"[GetGoodDateMessage] Processing good date message for location: '{location}'");
        switch (location)
        {
            case "rooftop":
                return "I had such a wonderful time at the rooftop with you! The sunset was beautiful, and so was our conversation.";
            case "aquarium":
                return "The aquarium date was amazing! I loved how you got excited about the marine life. Very cute!";
            case "forest":
                return "Our forest walk was so romantic and peaceful. I felt really connected to you in that natural setting.";
            default:
                Debug.Log(
                    $"[GetGoodDateMessage] Using default good message for location: '{location}'"
                );
                return $"Hey! I had such a wonderful time at the {_LastRealDateLocation} with you.";
        }
    }

    private string GetAmazingDateMessage(string location)
    {
        Debug.Log(
            $"[GetAmazingDateMessage] Processing amazing date message for location: '{location}'"
        );
        switch (location)
        {
            case "rooftop":
                return "That rooftop date was absolutely magical! Under the stars with you... I can't stop thinking about it. ❤️";
            case "aquarium":
                return "Best. Date. Ever! The way your eyes lit up at the aquarium made my heart skip a beat. I'm totally smitten!";
            case "forest":
                return "Our forest date felt like a fairy tale! Just you, me, and nature... I think I'm falling for you. 💕";
            default:
                Debug.Log(
                    $"[GetAmazingDateMessage] Using default amazing message for location: '{location}'"
                );
                return $"I'm completely enchanted by our time at the {_LastRealDateLocation}! You're incredible, and I can't wait to see you again! 💖";
        }
    }

    /// <summary>
    /// Synchronizes the local flags with the save data to ensure consistency
    /// </summary>
    private void SynchronizeWithSaveData()
    {
        Debug.Log($"[SynchronizeWithSaveData] Synchronizing state for {_name}");

        // Validate name
        if (string.IsNullOrEmpty(_name))
        {
            Debug.LogError(
                $"[SynchronizeWithSaveData] Bachelor has empty name! Bachelor object: {name}. Please set the _name field in the Inspector."
            );
            return;
        }

        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            Debug.Log($"[SynchronizeWithSaveData] No save data found, keeping local flags");
            return;
        }

        // Check speed dating status
        bool savedAsSpeedDated =
            saveData.DatedBachelors != null && saveData.DatedBachelors.Contains(_name);
        if (savedAsSpeedDated != _HasBeenSpeedDated)
        {
            Debug.Log(
                $"[SynchronizeWithSaveData] Updating speed dated flag from {_HasBeenSpeedDated} to {savedAsSpeedDated}"
            );
            _HasBeenSpeedDated = savedAsSpeedDated;
        }

        // Check real dating status
        bool savedAsRealDated =
            saveData.RealDatedBachelors != null && saveData.RealDatedBachelors.Contains(_name);
        if (savedAsRealDated != _HasCompletedRealDate)
        {
            Debug.Log(
                $"[SynchronizeWithSaveData] Updating real dated flag from {_HasCompletedRealDate} to {savedAsRealDated}"
            );
            _HasCompletedRealDate = savedAsRealDated;

            // If they completed a real date, they must have also speed dated
            if (savedAsRealDated && !_HasBeenSpeedDated)
            {
                _HasBeenSpeedDated = true;
                Debug.Log(
                    $"[SynchronizeWithSaveData] Also set speed dated flag to true since real date was completed"
                );
            }
        }

        Debug.Log(
            $"[SynchronizeWithSaveData] Final state - SpeedDated: {_HasBeenSpeedDated}, RealDated: {_HasCompletedRealDate}"
        );
    }

    /// <summary>
    /// For testing purposes - force synchronization with save data
    /// </summary>
    [ContextMenu("Force Sync With Save Data")]
    public void ForceSyncWithSaveData()
    {
        Debug.Log($"=== Force Sync Test for {_name} ===");
        Debug.Log(
            $"Before sync - SpeedDated: {_HasBeenSpeedDated}, RealDated: {_HasCompletedRealDate}"
        );

        SynchronizeWithSaveData();

        Debug.Log(
            $"After sync - SpeedDated: {_HasBeenSpeedDated}, RealDated: {_HasCompletedRealDate}"
        );
        Debug.Log($"HasBeenDated(): {HasBeenDated()}");
        Debug.Log($"HasCompletedRealDate(): {HasCompletedRealDate()}");
        Debug.Log("=== End Sync Test ===");
    }

    /// <summary>
    /// For debugging - logs the current state of this bachelor
    /// </summary>
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== Debug State for {_name} ===");
        Debug.Log($"Local Flags:");
        Debug.Log($"  _HasBeenSpeedDated: {_HasBeenSpeedDated}");
        Debug.Log($"  _HasCompletedRealDate: {_HasCompletedRealDate}");
        Debug.Log($"  _LastRealDateLocation: '{_LastRealDateLocation}'");
        Debug.Log($"  _isLikeDiscovered: {_isLikeDiscovered}");
        Debug.Log($"  _isDislikeDiscovered: {_isDislikeDiscovered}");

        Debug.Log($"Method Results:");
        Debug.Log($"  HasBeenDated(): {HasBeenDated()}");
        Debug.Log($"  HasCompletedRealDate(): {HasCompletedRealDate()}");

        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null)
        {
            Debug.Log($"Save Data:");
            bool inDatedList =
                saveData.DatedBachelors != null && saveData.DatedBachelors.Contains(_name);
            bool inRealDatedList =
                saveData.RealDatedBachelors != null && saveData.RealDatedBachelors.Contains(_name);
            Debug.Log($"  In DatedBachelors: {inDatedList}");
            Debug.Log($"  In RealDatedBachelors: {inRealDatedList}");
        }
        else
        {
            Debug.Log($"Save Data: No save data found");
        }

        if (_loveMeter != null)
        {
            Debug.Log($"Love Meter: {_loveMeter.GetCurrentLove()}");
        }
        else
        {
            Debug.Log($"Love Meter: Not assigned");
        }

        Debug.Log("=== End Debug State ===");
    }

    /// <summary>
    /// For testing purposes - comprehensive test of the dating flow
    /// </summary>
    [ContextMenu("Test Complete Dating Flow")]
    public void TestCompleteDatingFlow()
    {
        Debug.Log($"=== Testing Complete Dating Flow for {_name} ===");

        // Step 1: Initial state check
        Debug.Log($"Step 1 - Initial State:");
        DebugCurrentState();

        // Step 2: Simulate speed dating
        Debug.Log($"\nStep 2 - Simulating Speed Date:");
        MarkAsDated();
        Debug.Log($"After MarkAsDated() - HasBeenDated(): {HasBeenDated()}");

        // Step 3: Simulate real dating
        Debug.Log($"\nStep 3 - Simulating Real Date:");
        string testLocation = "Rooftop";
        MarkAsRealDated(testLocation);
        Debug.Log(
            $"After MarkAsRealDated('{testLocation}') - HasCompletedRealDate(): {HasCompletedRealDate()}"
        );

        // Step 4: Final state check
        Debug.Log($"\nStep 4 - Final State Check:");
        DebugCurrentState();

        // Step 5: Test save persistence
        Debug.Log($"\nStep 5 - Testing Save Persistence:");
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData != null)
        {
            bool inDatedList =
                saveData.DatedBachelors != null && saveData.DatedBachelors.Contains(_name);
            bool inRealDatedList =
                saveData.RealDatedBachelors != null && saveData.RealDatedBachelors.Contains(_name);
            Debug.Log(
                $"Save Data Verification - In DatedBachelors: {inDatedList}, In RealDatedBachelors: {inRealDatedList}"
            );
        }

        Debug.Log($"=== End Complete Dating Flow Test ===");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Validates the ScriptableObject in the editor to ensure _name is set
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_name))
        {
            Debug.LogWarning(
                $"Bachelor ScriptableObject '{name}' has an empty _name field! Please set the character's name in the Inspector to prevent save issues.",
                this
            );
        }
    }
#endif

#if !UNITY_EDITOR
    /// <summary>
    /// Context menu method to clean up empty strings from save data
    /// Use this to fix corrupted save files that contain empty bachelor names
    /// </summary>
    [ContextMenu("Clean Save Data")]
    public void CleanSaveData()
    {
        SaveData saveData = SaveSystem.Deserialize();
        if (saveData == null)
        {
            Debug.Log("[CleanSaveData] No save data found to clean");
            return;
        }

        int removedDatedCount = 0;
        int removedRealDatedCount = 0;

        // Remove empty strings from DatedBachelors
        if (saveData.DatedBachelors != null)
        {
            var originalCount = saveData.DatedBachelors.Count;
            saveData.DatedBachelors.RemoveAll(name => string.IsNullOrEmpty(name));
            removedDatedCount = originalCount - saveData.DatedBachelors.Count;
        }

        // Remove empty strings from RealDatedBachelors
        if (saveData.RealDatedBachelors != null)
        {
            var originalCount = saveData.RealDatedBachelors.Count;
            saveData.RealDatedBachelors.RemoveAll(name => string.IsNullOrEmpty(name));
            removedRealDatedCount = originalCount - saveData.RealDatedBachelors.Count;
        }

        if (removedDatedCount > 0 || removedRealDatedCount > 0)
        {
            SaveSystem.SerializeData(saveData);
            Debug.Log(
                $"[CleanSaveData] Cleaned save data: removed {removedDatedCount} empty entries from DatedBachelors and {removedRealDatedCount} empty entries from RealDatedBachelors"
            );
        }
        else
        {
            Debug.Log("[CleanSaveData] Save data is clean - no empty entries found");
        }
    }

    /// <summary>
    /// Context menu method to completely reset save data for testing
    /// </summary>
    [ContextMenu("Reset Save Data")]
    public void ResetSaveData()
    {
        SaveData freshData = new SaveData();
        SaveSystem.SerializeData(freshData);
        Debug.Log("[ResetSaveData] Save data has been completely reset");

        // Also reset this bachelor's local flags
        _HasBeenSpeedDated = false;
        _HasCompletedRealDate = false;
        _LastRealDateLocation = "";

        Debug.Log($"[ResetSaveData] Local flags reset for {_name}");
    }
#endif

    /// <summary>
    /// Context menu method to check if this bachelor's name is properly set
    /// </summary>
    [ContextMenu("Validate Bachelor Name")]
    public void ValidateBachelorName()
    {
        if (string.IsNullOrEmpty(_name))
        {
            Debug.LogError(
                $"[ValidateBachelorName] ❌ Bachelor '{name}' has an empty _name field! This will cause save issues.",
                this
            );
        }
        else
        {
            Debug.Log($"[ValidateBachelorName] ✅ Bachelor '{_name}' has a proper name set.", this);
        }
    }
}
