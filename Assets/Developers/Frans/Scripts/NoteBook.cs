using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class Notebook : MonoBehaviour
{
    [SerializeField]
    private BachelorSO[] m_bachelors;
    [SerializeField]
    private TMP_Text m_name;
    [SerializeField]
    private List<TMP_Text> m_likes = new List<TMP_Text>();
    [SerializeField]
    private List<TMP_Text> m_disLikes = new List<TMP_Text>();

    private void Start()
    {
        CheckBachelor(m_bachelors[0]); 
    }

    [ContextMenu("LikeChecker")]
    public void CheckBachelor(BachelorSO bachelor)
    {
        m_name.text = bachelor.name;
        LikesChecker(bachelor);
    }

    public void LikesChecker(BachelorSO bachelor)
    {
        for (int i = 0; i < bachelor.m_likes.Count; i++)
        {
            if (bachelor.m_likesUnlocked[i])
                m_likes[i].text = bachelor.m_likes[i];
            else
            {
                m_likes[i].text = "???????";
            }
        }
        for (int i = 0; i < bachelor.m_dislikes.Count; i++)
        {
            if (bachelor.m_dislikesUnlocked[i])
                m_disLikes[i].text = bachelor.m_dislikes[i];
            else
            {
                m_disLikes[i].text = "???????";
            }
        }
    }
}
