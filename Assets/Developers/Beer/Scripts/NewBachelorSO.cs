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
    }

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
    /// For testing purposes - discovers all preferences
    /// </summary>
    [ContextMenu("Discover All Preferences (Testing)")]
    public void DiscoverAllPreferencesForTesting()
    {
        DiscoverAllLikes();
        DiscoverAllDislikes();
    }
}
