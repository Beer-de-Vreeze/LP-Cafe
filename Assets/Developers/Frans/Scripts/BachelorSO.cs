using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Bachelor", menuName = "BaseBachelor")]
public class BachelorSO : ScriptableObject
{
    public int m_loveMeter;
    public string m_name;
    public List<string> m_dialogue = new List<string>();

    [SerializeField]
    private List<string> m_likes = new List<string>();
    [SerializeField]
    private List<string> m_dislikes = new List<string>();




    public void ArrayCheck(string dialogue)
    {
        if (m_likes.Contains(dialogue))
        {
            IncreaseLove();
        }

        else if (m_dislikes.Contains(dialogue))
        {
            DecreaseLove();
        }
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
