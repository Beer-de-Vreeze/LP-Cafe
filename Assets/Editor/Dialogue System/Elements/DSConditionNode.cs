using System;
using System.Collections.Generic;
using System.Linq;
using DS.Data.Save;
using DS.Elements;
using DS.Enumerations;
using DS.Utilities;
using DS.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    /// <summary>
    /// Represents a conditional branching node in the dialogue system.
    /// This node evaluates a condition based on a property's value and provides different dialogue paths.
    /// </summary>
    public class DSConditionNode : NodeBase
    {


        // Collection of properties that can be checked in conditions
        /// <summary>
        /// Static list of property names that can be used in conditions.
        /// This list can be modified at runtime using the Register/Unregister methods.
        /// </summary>
        private static List<string> availableProperties = new List<string>
        {
            "Love",
            "LikeDiscovered",
            "DislikeDiscovered",
            "NotebookLikeEntry",
            "NotebookDislikeEntry",
        };

        /// <summary>
        /// Adds a new property to the list of available properties for condition nodes.
        /// </summary>
        /// <param name="propertyName">The name of the property to register</param>
        public static void RegisterProperty(string propertyName)
        {
            if (!availableProperties.Contains(propertyName))
            {
                availableProperties.Add(propertyName);
            }
        }

        /// <summary>
        /// Removes a property from the list of available properties for condition nodes.
        /// </summary>
        /// <param name="propertyName">The name of the property to unregister</param>
        public static void UnregisterProperty(string propertyName)
        {
            availableProperties.Remove(propertyName);
        }

        /// <summary>
        /// Gets a copy of the current available properties list.
        /// </summary>
        /// <returns>A new list containing all available property names</returns>
        public static List<string> GetAvailableProperties()
        {
            return new List<string>(availableProperties);
        }

        /// <summary>
        /// Initializes the condition node with default values and creates an initial choice.
        /// </summary>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="dsGraphView">Reference to the graph view</param>
        /// <param name="pos">Initial position of the node in the graph</param>
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(nodeName, dsGraphView, pos);

            // Set node type to single choice for the condition node
            m_nodeDialogueType = DSDialogueType.Condition;

            // Create a default "Next Dialogue" choice path
            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                m_choiceTextData = "Next Dialogue",
            };

            m_nodeChoices.Add(choiceData);
        }

        /// <summary>
        /// Draws the UI components of the condition node in the graph editor.
        /// Includes property selection, comparison operator, and value input fields.
        /// </summary>
        public override void Draw()
        {
            #region DialogueName
            /* TITLE CONTAINER*/
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(
                m_nodeDialogueName = "Condition Node",
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
            #endregion

            //Inport Container.
            Port inputPort = this.CreatePort(
                "Dialogue Connection",
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi
            );

            inputPort.portName = "Dialogue Connection";

            inputContainer.Add(inputPort);

            // Set the node title
            m_nodeDialogueName = "Condition Node";

            // Create container for the condition UI elements
            var conditionContainer = new VisualElement();
            conditionContainer.AddToClassList("ds-node__custom-data-container");

            // Create dropdown for property selection
            var propertyField = new PopupField<string>(
                GetAvailableProperties(),
                propertyToCheck != null && GetAvailableProperties().Contains(propertyToCheck)
                    ? propertyToCheck
                    : GetAvailableProperties().FirstOrDefault() ?? "Love"
            );
            propertyField.label = "Property to Check";
            propertyField.RegisterValueChangedCallback(evt => propertyToCheck = evt.newValue);
            conditionContainer.Add(propertyField);

            // Create dropdown for comparison operator
            var comparisonField = new PopupField<string>(
                comparisonTypes.ToList(),
                comparisonType != string.Empty ? comparisonType : ">="
            );

            comparisonField.label = "Comparison";
            comparisonField.RegisterValueChangedCallback(evt => comparisonType = evt.newValue);
            conditionContainer.Add(comparisonField);
            

            // Create text field for the comparison value
            var valueCompareField = new TextField("Value") { value = comparisonValue };
            valueCompareField.RegisterValueChangedCallback(evt => comparisonValue = evt.newValue);
            conditionContainer.Add(valueCompareField);

            extensionContainer.Add(conditionContainer);

            // Create output ports for each choice path
            foreach (DSChoiceSaveData choice in m_nodeChoices)
            {
                // Create a port for this choice path
                Port choicePort = this.CreatePort(choice.m_choiceTextData);

                // Store the choice data in the port's userData for reference
                choicePort.userData = choice;

                outputContainer.Add(choicePort);
            }

            // Update the visual state of the node
            RefreshExpandedState();
        }
    }
}
