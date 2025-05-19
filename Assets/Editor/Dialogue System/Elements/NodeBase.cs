using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LPCafe.Elements
{
    using Enumerations;
    using Windows;
    using Utilities;

    public class NodeBase : Node
    {
        public string m_dialogueName { get; set; }
        public List<string> m_choices {  get; set; }
        public string m_text {  get; set; }

        //Needs to have an audio source and an image variable later.
        //Look at the base node video comments for further reference to fix this!

        public DSDialogueType m_dialogueType { get; set; }

        public DSGroup m_group { get; set; }

        private DSGraphView m_graphView;
        private Color m_defaultBackgroundColor;

        public virtual void Initialize(DSGraphView dsGraphView, Vector2 pos)
        {
            m_dialogueName = "DialogueName";
            m_choices = new List<string>();
            m_text = "Dialogue text.";

            m_graphView = dsGraphView;

            //Color indicator only takes value from 0/1 not uptil 255 so we need to divide the value by 255.
            m_defaultBackgroundColor = new Color(29f / 255f, 29f / 255f, 30f / 255f);

            //For the position of the nodes.
            SetPosition(new Rect(pos, Vector2.zero));

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");
        }

        public virtual void Draw()
        {
            /* TITLE CONTAINER*/
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(m_dialogueName, callback =>
            {
                if(m_group == null)
                {
                    m_graphView.RemoveUngroupedNode(this);

                    m_dialogueName = callback.newValue;

                    m_graphView.AddUngroupedNode(this);
                    
                    return;
                }

                DSGroup currentGroup = m_group;

                m_graphView.RemoveGroupedNode(this, m_group);

                m_dialogueName = callback.newValue;

                m_graphView.AddGroupedNode(this, currentGroup);
            });

            dialogueNameTextField.AddClasses
            (
               "ds-node__textfield",
               "ds-node__filename-textfield",
               "ds-node__textfield__hidden"
            );

            titleContainer.Insert(0, dialogueNameTextField);

            /*
             * Input Container 
                Orientation is for the direction in which the port point to when trying to make a connection between nodes.
                Direction can either be input or output.
                Capacity Can either be single or multiple (can one or multiple nodes connect to the node).
                Episode 8: https://www.youtube.com/watch?v=6vVqBt_5nbs&list=PL0yxB6cCkoWK38XT4stSztcLueJ_kTx5f&index=9
            */
            Port inputPort = this.CreatePort("Dialogue Connection", Orientation.Horizontal, Direction.Input, Port.Capacity.Multi);

            inputPort.portName = "Dialogue Connection";
            
            inputContainer.Add(inputPort);

            //Extension Container.
            VisualElement customDataContainer = new VisualElement();

            customDataContainer.AddToClassList("ds-node__custom-data-container");

            Foldout textFoldout = DSElementUtility.CreateFoldOut("Dialogue Text");

            TextField textTextField = DSElementUtility.CreateTextArea(m_text);

            textTextField.AddClasses
            (
                "ds-node__textfield",
                "ds-node__quote-textfield"
            );

            textFoldout.Add(textTextField);
            customDataContainer.Add(textFoldout);

            extensionContainer.Add(customDataContainer);
        }

        #region Style
        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;

        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = m_defaultBackgroundColor;
        }
        #endregion
    }
}