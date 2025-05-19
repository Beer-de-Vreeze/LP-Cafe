using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Bachelor", menuName = "BaseBachelor")]
public class BachelorSO : ScriptableObject
{
    public int m_bachelorNumber;
    public string m_name;

    /*    public Dictionary<string, bool> m_likesDictionary;
        public Dictionary<string, bool> m_dislikesDictionary;
    */
    public List<string> m_dialogue = new List<string>();
    public List<string> m_likes = new List<string>();
    public List<string> m_dislikes = new List<string>();
    public List<bool> m_likesUnlocked = new List<bool>();
    public List<bool> m_dislikesUnlocked = new List<bool>();
    private int m_loveMeter;

    public void ArrayCheck(string dialogue)
    {
        if (m_likes.Contains(dialogue))
        {
            IncreaseLove();
            int LikeIndex = m_likes.IndexOf(dialogue);
            UnlockLikeBool(LikeIndex);
        }
        else if (m_dislikes.Contains(dialogue))
        {
            DecreaseLove();
            int dislikeIndex = m_dislikes.IndexOf(dialogue);
            UnlockDislikeBool(dislikeIndex);
        }
    }

    private void UnlockLikeBool(int likeBoolToUnlock)
    {
        m_likesUnlocked[likeBoolToUnlock] = true;
        Notebook.Instance.LikesChecker(this);
    }

    private void UnlockDislikeBool(int dislikeBoolToUnlock)
    {
        m_dislikesUnlocked[dislikeBoolToUnlock] = true;
        Notebook.Instance.LikesChecker(this);
    }

    private void IncreaseLove()
    {
        m_loveMeter += 10;
    }

    private void DecreaseLove()
    {
        m_loveMeter -= 5;
    }
}
