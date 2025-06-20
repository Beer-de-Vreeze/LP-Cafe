using System;
using System.Collections.Generic;
using System.Linq;
using DS.Data.Save;
using DS.Enumerations;
using DS.Utilities;
using DS.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    /// <summary>
    /// Represents a conditional branching node in the dialogue system.
    /// This node evaluates various conditions and provides different dialogue paths based on the result.
    /// </summary>
    public class DSConditionNode : NodeBase
    {
        // UI Elements for different condition types
        private DropdownField operationTypeDropdown;
        private VisualElement valueContainer;
        private VisualElement loveScoreContainer;
        private VisualElement boolContainer;
        private VisualElement preferenceContainer;
        private ObjectField loveMeterObjectField;
        private ObjectField bachelorObjectField;
        private DropdownField preferenceTypeDropdown;
        private DropdownField preferenceDropdown;

        /// <summary>
        /// Initializes the condition node with default values and creates an initial choice.
        /// </summary>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="dsGraphView">Reference to the graph view</param>
        /// <param name="pos">Initial position of the node in the graph</param>
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(nodeName, dsGraphView, pos);

            // Set node type to condition node
            m_nodeDialogueType = DSDialogueType.Condition;

            // Initialize condition-specific fields
            m_operationType = SetterOperationType.SetValue;
            m_variableName = "variableName";
            m_valueToSet = "";
            m_loveScoreAmount = 0;
            m_boolValue = false;
            m_bachelor = null;
            m_isLikePreference = true;
            m_selectedPreference = "";

            // Create a default "Next Dialogue" choice path
            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                m_choiceTextData = "Next Dialogue",
            };

            m_nodeChoices.Add(choiceData);
        }

        /// <summary>
        /// Draws the UI components of the condition node in the graph editor.
        /// Includes all condition types: value check, boolean check, love score check, and preference discovery check.
        /// </summary>
        public override void Draw()
        {
            #region DialogueName
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
            #endregion

            //Input Container.
            Port inputPort = this.CreatePort(
                "Dialogue Connection",
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi
            );

            inputPort.portName = "Dialogue Connection";

            inputContainer.Add(inputPort);

            // Create main condition container
            var conditionContainer = new VisualElement();
            conditionContainer.AddToClassList("ds-node__custom-data-container");

            // Operation type dropdown
            var operationTypes = new List<string>()
            {
                "Check Value",
                "Check Love Score",
                "Check Boolean",
                "Check Preference Discovery",
            };

            operationTypeDropdown = new DropdownField("Condition Type", operationTypes, 0);
            operationTypeDropdown.value = GetConditionTypeString(m_operationType);
            operationTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                switch (evt.newValue)
                {
                    case "Check Value":
                        m_operationType = SetterOperationType.SetValue;
                        break;
                    case "Check Love Score":
                        m_operationType = SetterOperationType.UpdateLoveScore;
                        break;
                    case "Check Boolean":
                        m_operationType = SetterOperationType.UpdateBoolean;
                        break;
                    case "Check Preference Discovery":
                        m_operationType = SetterOperationType.DiscoverPreference;
                        break;
                }
                UpdateVisibleFields();
            });
            conditionContainer.Add(operationTypeDropdown);

            #region Value Container
            // Container for standard value checking
            valueContainer = new VisualElement();

            // Variable name field
            var variableField = new TextField("Variable Name") { value = m_variableName };
            variableField.RegisterValueChangedCallback(evt =>
            {
                m_variableName = evt.newValue;
            });
            variableField.AddClasses(
                "ds-node__textfield",
                "ds-node__filename-textfield",
                "ds-node__textfield__hidden"
            );
            valueContainer.Add(variableField);

            // Expected value field
            var valueField = new TextField("Expected Value") { value = m_valueToSet };
            valueField.RegisterValueChangedCallback(evt =>
            {
                m_valueToSet = evt.newValue;
            });
            valueField.AddClasses(
                "ds-node__textfield",
                "ds-node__filename-textfield",
                "ds-node__textfield__hidden"
            );
            valueContainer.Add(valueField);
            #endregion

            #region Love Container
            // Container for love score checking
            loveScoreContainer = new VisualElement();

            // Love Meter object field
            loveMeterObjectField = new ObjectField("Love Meter")
            {
                objectType = typeof(LoveMeterSO),
                value = m_loveMeter,
            };
            loveMeterObjectField.RegisterValueChangedCallback(evt =>
            {
                m_loveMeter = evt.newValue as LoveMeterSO;
            });
            loveScoreContainer.Add(loveMeterObjectField);

            // Minimum love amount field
            var loveAmountField = new IntegerField("Minimum Score") { value = m_loveScoreAmount };
            loveAmountField.RegisterValueChangedCallback(evt =>
            {
                m_loveScoreAmount = evt.newValue;
            });
            loveAmountField.AddClasses(
                "ds-node__textfield",
                "ds-node__filename-textfield",
                "ds-node__textfield__hidden"
            );
            loveScoreContainer.Add(loveAmountField);
            #endregion

            #region Boolean Container
            // Container for boolean checking
            boolContainer = new VisualElement();

            // Bool variable name field
            var boolVarField = new TextField("Boolean Name") { value = m_variableName };
            boolVarField.RegisterValueChangedCallback(evt =>
            {
                m_variableName = evt.newValue;
            });
            boolVarField.AddClasses(
                "ds-node__textfield",
                "ds-node__filename-textfield",
                "ds-node__textfield__hidden"
            );
            boolContainer.Add(boolVarField);

            // Expected boolean value dropdown
            var boolValueDropdown = new DropdownField(
                "Expected Value",
                new List<string> { "True", "False" },
                m_boolValue ? 0 : 1
            );
            boolValueDropdown.value = m_boolValue ? "True" : "False";
            boolValueDropdown.RegisterValueChangedCallback(evt =>
            {
                m_boolValue = evt.newValue == "True";
            });
            boolValueDropdown.AddToClassList("ds-node__dropdown");
            boolContainer.Add(boolValueDropdown);
            #endregion

            #region Preference Container
            // Container for preference discovery checking
            preferenceContainer = new VisualElement();

            // Bachelor object field
            bachelorObjectField = new ObjectField("Bachelor")
            {
                objectType = typeof(NewBachelorSO),
                value = m_bachelor,
            };
            bachelorObjectField.RegisterValueChangedCallback(evt =>
            {
                m_bachelor = evt.newValue as NewBachelorSO;
                PopulatePreferenceDropdown();
            });
            preferenceContainer.Add(bachelorObjectField);

            // Preference type dropdown (like or dislike)
            var preferenceTypes = new List<string>() { "Like", "Dislike" };
            preferenceTypeDropdown = new DropdownField("Preference Type", preferenceTypes, 0);
            preferenceTypeDropdown.value = m_isLikePreference ? "Like" : "Dislike";
            preferenceTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                m_isLikePreference = evt.newValue == "Like";
                PopulatePreferenceDropdown();
            });
            preferenceContainer.Add(preferenceTypeDropdown);

            // Preference dropdown to select which one to check
            preferenceDropdown = new DropdownField("Preference to Check", new List<string>(), 0);
            preferenceDropdown.value = m_selectedPreference;
            preferenceDropdown.RegisterValueChangedCallback(evt =>
            {
                m_selectedPreference = evt.newValue;
            });
            preferenceContainer.Add(preferenceDropdown);
            #endregion

            // Add all containers
            conditionContainer.Add(valueContainer);
            conditionContainer.Add(loveScoreContainer);
            conditionContainer.Add(boolContainer);
            conditionContainer.Add(preferenceContainer);

            extensionContainer.Add(conditionContainer);

            // Initialize visible fields based on current operation type
            UpdateVisibleFields();

            // Initialize preference dropdown
            PopulatePreferenceDropdown();

            // Create output ports for each choice path
            outputContainer.Clear();
            // Always create two output ports: True and False, for all condition types
            // But allow the node to function if only one is present (user can disconnect one in the editor)
            var existingPorts = outputContainer.Children().ToList();
            bool hasTrue = existingPorts.Any(p => (p as Port)?.portName == "True");
            bool hasFalse = existingPorts.Any(p => (p as Port)?.portName == "False");

            if (!hasTrue)
            {
                var trueChoice = new DSChoiceSaveData { m_choiceTextData = "True" };
                Port truePort = this.CreatePort("True");
                truePort.userData = trueChoice;
                outputContainer.Add(truePort);
            }
            if (!hasFalse)
            {
                var falseChoice = new DSChoiceSaveData { m_choiceTextData = "False" };
                Port falsePort = this.CreatePort("False");
                falsePort.userData = falseChoice;
                outputContainer.Add(falsePort);
            }
            // If user removed one port, only the remaining port will be present and functional

            // Update the visual state of the node
            RefreshExpandedState();
        }

        /// <summary>
        /// Populates the preference dropdown based on the selected bachelor and preference type.
        /// </summary>
        private void PopulatePreferenceDropdown()
        {
            if (preferenceDropdown == null)
                return;

            if (m_bachelor == null)
            {
                preferenceDropdown.choices = new List<string> { "No bachelor selected" };
                preferenceDropdown.index = 0;
                return;
            }

            List<string> preferences = new List<string>();
            if (m_isLikePreference && m_bachelor._likes != null)
            {
                foreach (var like in m_bachelor._likes)
                {
                    preferences.Add(like.description);
                }
            }
            else if (!m_isLikePreference && m_bachelor._dislikes != null)
            {
                foreach (var dislike in m_bachelor._dislikes)
                {
                    preferences.Add(dislike.description);
                }
            }

            if (preferences.Count > 0)
            {
                preferenceDropdown.choices = preferences;
                preferenceDropdown.index = 0;
                if (
                    string.IsNullOrEmpty(m_selectedPreference)
                    || !preferences.Contains(m_selectedPreference)
                )
                {
                    m_selectedPreference = preferences[0];
                }
                else
                {
                    preferenceDropdown.index = preferences.IndexOf(m_selectedPreference);
                }
            }
            else
            {
                preferenceDropdown.choices = new List<string> { "No preferences found" };
                preferenceDropdown.index = 0;
                m_selectedPreference = "";
            }
        }

        /// <summary>
        /// Updates which UI containers are visible based on the current operation type.
        /// </summary>
        private void UpdateVisibleFields()
        {
            if (
                valueContainer == null
                || loveScoreContainer == null
                || boolContainer == null
                || preferenceContainer == null
            )
                return;

            // Hide all containers first
            valueContainer.style.display = DisplayStyle.None;
            loveScoreContainer.style.display = DisplayStyle.None;
            boolContainer.style.display = DisplayStyle.None;
            preferenceContainer.style.display = DisplayStyle.None;

            // Show the appropriate container based on operation type
            switch (m_operationType)
            {
                case SetterOperationType.SetValue:
                    valueContainer.style.display = DisplayStyle.Flex;
                    break;
                case SetterOperationType.UpdateLoveScore:
                    loveScoreContainer.style.display = DisplayStyle.Flex;
                    break;
                case SetterOperationType.UpdateBoolean:
                    boolContainer.style.display = DisplayStyle.Flex;
                    break;
                case SetterOperationType.DiscoverPreference:
                    preferenceContainer.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        /// <summary>
        /// Converts the operation type enum to a string for the dropdown.
        /// </summary>
        private string GetConditionTypeString(SetterOperationType operationType)
        {
            switch (operationType)
            {
                case SetterOperationType.SetValue:
                    return "Check Value";
                case SetterOperationType.UpdateLoveScore:
                    return "Check Love Score";
                case SetterOperationType.UpdateBoolean:
                    return "Check Boolean";
                case SetterOperationType.DiscoverPreference:
                    return "Check Preference Discovery";
                default:
                    return "Check Value";
            }
        }
    }
}
