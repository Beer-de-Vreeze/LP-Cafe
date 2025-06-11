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
        private ObjectField loveMeterObjectField;

        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(nodeName, dsGraphView, pos);

            m_nodeDialogueType = DSDialogueType.Setter;

            m_operationType = SetterOperationType.SetValue;
            m_variableName = "variableName";
            m_valueToSet = "";
            m_loveScoreAmount = 0;
            m_boolValue = false;
            m_loveMeter = null;

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
            };

            operationTypeDropdown = new DropdownField("Operation Type", operationTypes, 0);
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
                }
                UpdateVisibleFields();
            });
            setterContainer.Add(operationTypeDropdown);

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

            // Add all containers
            setterContainer.Add(valueContainer);
            setterContainer.Add(loveScoreContainer);
            setterContainer.Add(boolContainer);

            extensionContainer.Add(setterContainer);

            // Initialize visible fields based on current operation type
            UpdateVisibleFields();

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
            }
        }

        private void TriggerSetEvent()
        {
            switch (m_operationType)
            {
                case SetterOperationType.SetValue:
                    if (!string.IsNullOrEmpty(m_variableName))
                    {
                        OnValueSet?.Invoke(m_variableName, m_valueToSet);
                        Debug.Log($"Set value: {m_variableName} = {m_valueToSet}");
                    }
                    break;

                case SetterOperationType.UpdateLoveScore:
                    // Use the selected LoveMeterSO instead of a hardcoded string
                    if (m_loveMeter != null)
                    {
                        OnLoveScoreChanged?.Invoke(m_loveMeter, m_loveScoreAmount);
                        Debug.Log($"Love score change: {m_loveMeter.name} by {m_loveScoreAmount}");
                    }
                    else
                    {
                        Debug.LogWarning("No Love Meter SO assigned!");
                    }
                    break;

                case SetterOperationType.UpdateBoolean:
                    if (!string.IsNullOrEmpty(m_variableName))
                    {
                        OnBooleanChanged?.Invoke(m_variableName, m_boolValue);
                        Debug.Log($"Boolean changed: {m_variableName} = {m_boolValue}");
                    }
                    break;
            }
        }
    }
}
