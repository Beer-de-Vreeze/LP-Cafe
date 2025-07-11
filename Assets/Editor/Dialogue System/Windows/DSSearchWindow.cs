using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DS
{
    using DS.Windows;
    using Elements;
    using Enumerations;
    using static UnityEngine.InputSystem.PlayerInput;

    public class DSSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DSGraphView m_graphView;
        private Texture2D m_indentationIcon;

        public void Initializaze(DSGraphView dsGraphView)
        {
            m_graphView = dsGraphView;

            m_indentationIcon = new Texture2D(1, 1);
            m_indentationIcon.SetPixel(0, 0, Color.clear);
            m_indentationIcon.Apply();
        }

        //Create the search window menu
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Element")),
                new SearchTreeGroupEntry(new GUIContent("Dialogue Node"), 1),
                new SearchTreeEntry(new GUIContent("Single Choice", m_indentationIcon))
                {
                    level = 2,
                    userData = DSDialogueType.SingleChoice,
                },
                new SearchTreeEntry(new GUIContent("Multiple Choice", m_indentationIcon))
                {
                    level = 2,
                    userData = DSDialogueType.MultipleChoice,
                },
                new SearchTreeEntry(new GUIContent("Conditional", m_indentationIcon))
                {
                    level = 2,
                    userData = DSDialogueType.Condition,
                },
                new SearchTreeEntry(new GUIContent("Setter", m_indentationIcon))
                {
                    level = 2,
                    userData = DSDialogueType.Setter,
                },
                new SearchTreeGroupEntry(new GUIContent("Dialogue Group"), 1),
                new SearchTreeEntry(new GUIContent("Single Group", m_indentationIcon))
                {
                    level = 2,
                    userData = new Group(),
                },
            };

            return searchTreeEntries;
        }

        //What needs to happen when pressed
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Vector2 localMousePos = m_graphView.GetLocalMousePosition(
                context.screenMousePosition,
                true
            );

            switch (SearchTreeEntry.userData)
            {
                case DSDialogueType.SingleChoice:
                {
                    DSSingleChoiceNode singleChoiceNode = (DSSingleChoiceNode)
                        m_graphView.CreateNode(
                            "DialogueName",
                            DSDialogueType.SingleChoice,
                            localMousePos
                        );
                    m_graphView.AddElement(singleChoiceNode);

                    return true;
                }

                case DSDialogueType.MultipleChoice:
                {
                    DSMultipleChoiceNode multipleChoiceNode = (DSMultipleChoiceNode)
                        m_graphView.CreateNode(
                            "DialogueName",
                            DSDialogueType.MultipleChoice,
                            localMousePos
                        );
                    m_graphView.AddElement(multipleChoiceNode);

                    return true;
                }

                case DSDialogueType.Condition:
                {
                    DSConditionNode setterNode = (DSConditionNode)
                        m_graphView.CreateNode(
                            "DialogueName",
                            DSDialogueType.Condition,
                            localMousePos
                        );
                    m_graphView.AddElement(setterNode);

                    return true;
                }

                case DSDialogueType.Setter:
                {
                    DSSetterNode setterNode = (DSSetterNode)
                        m_graphView.CreateNode(
                            "DialogueName",
                            DSDialogueType.Setter,
                            localMousePos
                        );
                    m_graphView.AddElement(setterNode);

                    return true;
                }

                case Group _:
                {
                    m_graphView.CreateGroup("DialogueGroup", localMousePos);

                    return true;
                }

                default:
                    return false;
            }
        }
    }
}
