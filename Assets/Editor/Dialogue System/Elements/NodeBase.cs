using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace DS.Elements
{
    using System.Linq;
    using DS.Data.Save;
    using Enumerations;
    using UnityEditor.Search;
    using Utilities;
    using Windows;

    public class NodeBase : Node
    {
        //Base node
        public string m_nodeID { get; set; }
        public string m_nodeDialogueName { get; set; }
        public List<DSChoiceSaveData> m_nodeChoices { get; set; }
        public string m_nodeText { get; set; }
        public Sprite m_nodeCharacterImage { get; set; }
        public AudioClip m_nodeAudio { get; set; }
        public DSDialogueType m_nodeDialogueType { get; set; }

        public DSGroup m_nodeGroup { get; set; }

        protected DSGraphView m_graphView;
        private Color m_defaultBackgroundColor;

        //Condition Node
        // Properties that define the condition to be evaluated
        /// <summary>Property name that will be checked during condition evaluation</summary>
        public string m_propertyToCheck { get; set; } = "Love"; // Default to checking love value

        /// <summary>Comparison operator used to evaluate the condition</summary>
        public string m_comparisonType { get; set; } = ">="; // Default to greater than or equal

        /// <summary>The value to compare the property against</summary>
        public string m_comparisonValue { get; set; } = "0"; // Default value to compare against

        // Available comparison operators for the condition
        /// <summary>Array of available comparison operators for condition evaluation</summary>
        public string[] m_comparisonTypes = { "==", "!=", ">", "<", ">=", "<=" };

        //End Condition Node

        //Setter Node
        public string m_valueToSet { get; set; }
        public string m_variableName { get; set; }
        public SetterOperationType m_operationType { get; set; }
        public int m_loveScoreAmount { get; set; }
        public bool m_boolValue { get; set; }
        public LoveMeterSO m_loveMeter { get; set; }
        public NewBachelorSO m_bachelor { get; set; }
        public bool m_isLikePreference { get; set; }
        public string m_selectedPreference { get; set; }
        //End Setter Node

        public virtual void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            m_nodeID = Guid.NewGuid().ToString();
            m_nodeDialogueName = nodeName;
            m_nodeChoices = new List<DSChoiceSaveData>();
            m_nodeText = "Dialogue text.";

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
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(
                m_nodeDialogueName,
                null,
                callback =>
                {
                    TextField target = (TextField)callback.target;

                    //No spaces or special characters in filenames.
                    target.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();

                    //Checks if there is a value to make sure no empty things are saved.
                    if (string.IsNullOrEmpty(target.value))
                    {
                        if (!string.IsNullOrEmpty(m_nodeDialogueName))
                        {
                            ++m_graphView.NameErrorsAmount;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(m_nodeDialogueName))
                        {
                            --m_graphView.NameErrorsAmount;
                        }
                    }

                    if (m_nodeGroup == null)
                    {
                        m_graphView.RemoveUngroupedNode(this);

                        m_nodeDialogueName = target.value;

                        m_graphView.AddUngroupedNode(this);

                        return;
                    }

                    DSGroup currentGroup = m_nodeGroup;

                    m_graphView.RemoveGroupedNode(this, m_nodeGroup);

                    m_nodeDialogueName = target.value;

                    m_graphView.AddGroupedNode(this, currentGroup);
                }
            );

            dialogueNameTextField.AddClasses(
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

            //Inport Container.
            Port inputPort = this.CreatePort(
                "Dialogue Connection",
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi
            );

            inputPort.portName = "Dialogue Connection";

            inputContainer.Add(inputPort);

            //Extension Container.
            VisualElement customDataContainer = new VisualElement();

            customDataContainer.AddToClassList("ds-node__custom-data-container");

            Foldout textFoldout = DSElementUtility.CreateFoldOut("Dialogue Text");

            TextField textTextField = DSElementUtility.CreateTextArea(
                m_nodeText,
                null,
                callback =>
                {
                    m_nodeText = callback.newValue;
                }
            );

            ObjectField imageField = new ObjectField("Bachelor Image")
            {
                objectType = typeof(Sprite),
                value = m_nodeCharacterImage
            };

            imageField.RegisterValueChangedCallback(evt =>
            {
                m_nodeCharacterImage = evt.newValue as Sprite;
            });

            ObjectField audioField = new ObjectField("Audio Lines")
            {
                objectType = typeof(AudioClip),
                value = m_nodeAudio
            };
            audioField.RegisterValueChangedCallback(evt =>
            {
                m_nodeAudio = evt.newValue as AudioClip;
            });

            textTextField.AddClasses("ds-node__textfield", "ds-node__quote-textfield");

            textFoldout.Add(textTextField);
            customDataContainer.Add(audioField);
            customDataContainer.Add(imageField);
            customDataContainer.Add(textFoldout);

            extensionContainer.Add(customDataContainer);
        }

        #region Overrided Methods
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            //Makes a context menu action which deletes all input port edges of the selected node.
            evt.menu.AppendAction(
                "Disconnect Input Ports",
                actionEvent =>
                {
                    DisConnectInputPorts();
                }
            );
            //Makes a context menu action which deletes all output port edges of the selected node.
            evt.menu.AppendAction(
                "Disconnect Output Ports",
                actionEvent =>
                {
                    DisConnectOutputPorts();
                }
            );

            //Makes sure that you can still delete all the edges from both the input and output ports in one go.
            base.BuildContextualMenu(evt);
        }
        #endregion

        #region Utility Methos
        #region Ports
        public void DisconnectAllports()
        {
            DisConnectInputPorts();
            DisConnectOutputPorts();
        }

        private void DisConnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        }

        private void DisConnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        }

        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected)
                {
                    continue;
                }

                List<Edge> portConnections = new List<Edge>(port.connections);

                m_graphView.DeleteElements(portConnections);
            }
        }
        #endregion
        public bool IsStartingNode()
        {
            Port inputPort = (Port)inputContainer.Children().First();

            return inputPort.connected;
        }
        #endregion

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
