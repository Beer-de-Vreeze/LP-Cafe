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
}
