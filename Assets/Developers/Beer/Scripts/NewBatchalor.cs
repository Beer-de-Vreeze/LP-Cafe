using DS;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBachelor", menuName = "Visual Novel/NewBachelor")]
public class NewBachelorSO : ScriptableObject
{
    public string _name;
    public string[] _knownLikes;
    public string[] _knownDislikes;
    public bool _isLikeDiscovered;
    public bool _isDislikeDiscovered;
    public DSDialogue _dialogue;
    public int _LoveValue;
}
