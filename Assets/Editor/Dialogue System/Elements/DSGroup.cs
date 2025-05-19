using UnityEngine;
using UnityEditor.Experimental.GraphView;

public class DSGroup : Group
{
    public string m_oldTitle;
    private Color m_defaultBorderColor;
    private float m_defaultBorderWidth;

    public DSGroup(string groupTitle, Vector2 groupPos)
    {
        title = groupTitle;
        m_oldTitle = groupTitle;

        SetPosition(new Rect(groupPos, Vector2.zero));

        m_defaultBorderColor = contentContainer.style.borderBottomColor.value;
        m_defaultBorderWidth = contentContainer.style.borderBottomWidth.value;
    }

    public void SetErrorStyle(Color color)
    {
        contentContainer.style.borderBottomColor = color;
        contentContainer.style.borderBottomWidth = 2f;
    }

    public void ResetStyle()
    {
        contentContainer.style.borderBottomColor = m_defaultBorderColor;
        contentContainer.style.borderBottomWidth = m_defaultBorderWidth;
    }
}
