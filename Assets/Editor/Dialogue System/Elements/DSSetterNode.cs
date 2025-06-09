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
    public class DSSetterNode : NodeBase
    {
        public string valueToSet { get; set; }

        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(nodeName, dsGraphView, pos);

            m_nodeDialogueType = DSDialogueType.SingleChoice;

            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                m_choiceTextData = "Next Dialogue"
            };

            m_nodeChoices.Add(choiceData);
        }

        public override void Draw()
        {
            #region DialogueName
            /* TITLE CONTAINER*/
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(m_nodeDialogueName = "Event Caller", null, callback =>
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
            });

            dialogueNameTextField.AddClasses
            (
               "ds-node__textfield",
               "ds-node__filename-textfield",
               "ds-node__textfield__hidden"
            );

            titleContainer.Insert(0, dialogueNameTextField);
            #endregion

            //Inport Container.
            Port inputPort = this.CreatePort("Dialogue Connection", Orientation.Horizontal, Direction.Input, Port.Capacity.Multi);

            inputPort.portName = "Dialogue Connection";

            inputContainer.Add(inputPort);


            // Add custom UI for setter
            var setterContainer = new VisualElement();
            setterContainer.AddToClassList("ds-node__custom-data-container");

            var valueField = new TextField("Value")
            {
                value = valueToSet
            };
            valueField.RegisterValueChangedCallback(evt => valueToSet = evt.newValue);

            valueField.AddClasses
            (
               "ds-node__textfield",
               "ds-node__filename-textfield",
               "ds-node__textfield__hidden"
            );

            setterContainer.Add(valueField);

            extensionContainer.Add(setterContainer);

            //OUTPUT CONTAINER.
            foreach (DSChoiceSaveData choice in m_nodeChoices)
            {
                //Instantiates a port to another node for each choice in the node.
                Port choicePort = this.CreatePort(choice.m_choiceTextData);

                choicePort.userData = choice;

                outputContainer.Add(choicePort);
            }

            //Calls the RefreshPorts function which Refreshes the layout of the ports.
            RefreshExpandedState();
        }
    }
}

