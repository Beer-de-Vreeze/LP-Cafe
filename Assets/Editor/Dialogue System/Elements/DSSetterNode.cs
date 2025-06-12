using System;
using System.Collections.Generic;
using DS.Data.Save;
using DS.Enumerations;
using DS.Utilities;
using DS.Windows;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    public class DSSetterNode : NodeBase
    {
        // Events for different operation types
        public event Action<string, string> OnValueSet;
        public event Action<LoveMeterSO, int> OnLoveScoreChanged;
        public event Action<string, bool> OnBooleanChanged;

        private DropdownField operationTypeDropdown;
        private VisualElement valueContainer;
        private VisualElement loveScoreContainer;
        private VisualElement boolContainer;
        private VisualElement preferenceContainer;
        private ObjectField loveMeterObjectField;
        private ObjectField bachelorObjectField;
        private DropdownField preferenceTypeDropdown;
        private DropdownField preferenceDropdown;

        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(nodeName, dsGraphView, pos);

            m_nodeDialogueType = DSDialogueType.Setter;
            
            m_variableName = "variableName";
            m_valueToSet = "";
            m_bachelor = null;
            m_isLikePreference = true;
            m_selectedPreference = "";

            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                m_choiceTextData = "Next Dialogue",
            };

            m_nodeChoices.Add(choiceData);
        }

        public override void Draw()
        {
            #region DialogueName
            /* TITLE CONTAINER*/
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(
                m_nodeDialogueName = "Value Setter",
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

            //Input Container
            Port inputPort = this.CreatePort(
                "Dialogue Connection",
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi
            );
            inputPort.portName = "Dialogue Connection";
            inputContainer.Add(inputPort);

            // Main setter container
            var setterContainer = new VisualElement();
            setterContainer.AddToClassList("ds-node__custom-data-container");

            // Operation type dropdown
            var operationTypes = new List<string>()
            {
                "Set Value",
                "Update Love Score",
                "Update Boolean",
                "Discover Preference",
            };

            operationTypeDropdown = new DropdownField("Operation Type", operationTypes, 0);
            operationTypeDropdown.value = m_operationType.ToString();
            operationTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                switch (evt.newValue)
                {
                    case "Set Value":
                        m_operationType = SetterOperationType.SetValue;
                        break;
                    case "Update Love Score":
                        m_operationType = SetterOperationType.UpdateLoveScore;
                        break;
                    case "Update Boolean":
                        m_operationType = SetterOperationType.UpdateBoolean;
                        break;
                    case "Discover Preference":
                        m_operationType = SetterOperationType.DiscoverPreference;
                        break;
                }
                UpdateVisibleFields();
            });
            setterContainer.Add(operationTypeDropdown);

            #region Value Container
            // Container for standard value setting
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

            // Value field
            var valueField = new TextField("Value") { value = m_valueToSet };
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
            // Container for love score
            loveScoreContainer = new VisualElement();

            // Replace the label with an ObjectField for LoveMeterSO
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

            // Love amount field
            var loveAmountField = new IntegerField("Amount") { value = m_loveScoreAmount };
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
            // Container for boolean values
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

            // Boolean toggle
            var boolToggle = new Toggle("Value") { value = m_boolValue };
            boolToggle.RegisterValueChangedCallback(evt =>
            {
                m_boolValue = evt.newValue;
            });
            boolToggle.AddToClassList("ds-node__toggle");
            boolContainer.Add(boolToggle);
            #endregion
            #region Preference Container
            // Container for preference discovery
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
            preferenceTypeDropdown = new DropdownField("Type", preferenceTypes, 0);
            preferenceTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                m_isLikePreference = evt.newValue == "Like";
                PopulatePreferenceDropdown();
            });
            preferenceContainer.Add(preferenceTypeDropdown);

            // Preference dropdown to select which one to discover
            preferenceDropdown = new DropdownField("Preference", new List<string>(), 0);
            preferenceDropdown.value = m_selectedPreference;
            Debug.Log(preferenceDropdown.value);

            //Event doesnt trigger on load! NEEDS FIX!
            preferenceDropdown.RegisterValueChangedCallback(evt =>
            { 
                 m_selectedPreference = evt.newValue;
            });
            
            preferenceContainer.Add(preferenceDropdown);
            #endregion
            // Add all containers
            setterContainer.Add(valueContainer);
            setterContainer.Add(loveScoreContainer);
            setterContainer.Add(boolContainer);
            setterContainer.Add(preferenceContainer);

            extensionContainer.Add(setterContainer);

            // Initialize visible fields based on current operation type
            UpdateVisibleFields();
            // Always populate preference dropdown during initialization
            if(m_bachelor == null)
            {
                PopulatePreferenceDropdown();
            }

            //OUTPUT CONTAINER
            foreach (DSChoiceSaveData choice in m_nodeChoices)
            {
                //Instantiates a port to another node for each choice in the node.
                Port choicePort = this.CreatePort(choice.m_choiceTextData);
                choicePort.userData = choice;
                outputContainer.Add(choicePort);
            }

            //Refresh the layout of the ports.
            RefreshExpandedState();
        }

        private void UpdateVisibleFields()
        {
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

        // private void TriggerSetEvent()
        // {
        //     switch (m_operationType)
        //     {
        //         case SetterOperationType.SetValue:
        //             if (!string.IsNullOrEmpty(m_variableName))
        //             {
        //                 OnValueSet?.Invoke(m_variableName, m_valueToSet);
        //                 Debug.Log($"Set value: {m_variableName} = {m_valueToSet}");
        //             }
        //             break;

        //         case SetterOperationType.UpdateLoveScore:
        //             // Use the selected LoveMeterSO instead of a hardcoded string
        //             if (m_loveMeter != null)
        //             {
        //                 OnLoveScoreChanged?.Invoke(m_loveMeter, m_loveScoreAmount);
        //                 Debug.Log($"Love score change: {m_loveMeter.name} by {m_loveScoreAmount}");
        //             }
        //             else
        //             {
        //                 Debug.LogWarning("No Love Meter SO assigned!");
        //             }
        //             break;

        //         case SetterOperationType.UpdateBoolean:
        //             if (!string.IsNullOrEmpty(m_variableName))
        //             {
        //                 OnBooleanChanged?.Invoke(m_variableName, m_boolValue);
        //                 Debug.Log($"Boolean changed: {m_variableName} = {m_boolValue}");
        //             }
        //             break;

        //         case SetterOperationType.DiscoverPreference:
        //             if (m_bachelor != null && !string.IsNullOrEmpty(m_selectedPreference))
        //             {
        //                 if (m_isLikePreference)
        //                 {
        //                     for (int i = 0; i < m_bachelor._likes.Length; i++)
        //                     {
        //                         if (m_bachelor._likes[i].description == m_selectedPreference)
        //                         {
        //                             m_bachelor.DiscoverLike(i);
        //                             Debug.Log($"Discovered like: {m_selectedPreference}");
        //                             break;
        //                         }
        //                     }
        //                 }
        //                 else
        //                 {
        //                     for (int i = 0; i < m_bachelor._dislikes.Length; i++)
        //                     {
        //                         if (m_bachelor._dislikes[i].description == m_selectedPreference)
        //                         {
        //                             m_bachelor.DiscoverDislike(i);
        //                             Debug.Log($"Discovered dislike: {m_selectedPreference}");
        //                             break;
        //                         }
        //                     }
        //                 }
        //             }
        //             break;
        //     }
        // }

        private void PopulatePreferenceDropdown()
        {
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
                m_selectedPreference = preferences[0];
            }
            else
            {
                preferenceDropdown.choices = new List<string> { "No preferences found" };
                preferenceDropdown.index = 0;
            }
        }
    }
}
