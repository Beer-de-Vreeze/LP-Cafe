using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace LPCafe.Elements
{
    using Data.Save;
    using Windows;
    using Enumerations;
    using Utilities;

    public class DSMultipleChoiceNode : NodeBase
    {
        public override void Initialize(DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(dsGraphView, pos);

            m_nodeDialogueType = DSDialogueType.MultipleChoice;

            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                m_choiceTextData = "Next Dialogue"
            };


            m_nodeChoices.Add(choiceData);
        }

        public override void Draw()
        {
            base.Draw();

            /* Main Container */
            Button addChoiceButton = DSElementUtility.CreateButton("Add Choice", () =>
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData()
                {
                    m_choiceTextData = "Next Dialogue"
                };

                m_nodeChoices.Add(choiceData);
                Port choicePort = CreateChoicePort(choiceData);

                outputContainer.Add(choicePort);
            });

            addChoiceButton.AddToClassList("ds-node__button");

            mainContainer.Insert(1, addChoiceButton);

            //OUTPUT CONTAINER.
            foreach (DSChoiceSaveData choice in m_nodeChoices)
            {
                Port choicePort = CreateChoicePort(choice);
                outputContainer.Add(choicePort);
            }

            //Calls the RefreshPorts function which Refreshes the layout of the ports.
            RefreshExpandedState();
        }

        #region Element Creation
        public Port CreateChoicePort(object userData)
        {
            //Instantiates a port to another node for each choice in the node.
            Port choicePort = this.CreatePort();

            choicePort.userData = userData;

            DSChoiceSaveData choiceData = (DSChoiceSaveData) userData;

            //Will be able to delete choices.
            Button deleteChoiceButton = DSElementUtility.CreateButton("X", () =>
            {
                if(m_nodeChoices.Count == 1)
                {
                    return;
                }

                if (choicePort.connected)
                {
                    m_graphView.DeleteElements(choicePort.connections);
                }

                m_nodeChoices.Remove(choiceData);

                m_graphView.RemoveElement(choicePort);
            });

            deleteChoiceButton.AddToClassList("ds-node__button");

            TextField choiceTextField = DSElementUtility.CreateTextField(choiceData.m_choiceTextData, null, callback =>
            {
                choiceData.m_choiceTextData = callback.newValue;
            });

            choiceTextField.AddClasses
            (
                "ds-node__textfield",
                "ds-node__choice-textfield",
                "ds-node__textfield__hidden"
            );

            //Makes the Button and TextField visable in the editor when the node is made.
            choicePort.Add(choiceTextField);
            choicePort.Add(deleteChoiceButton);

            return choicePort;
        }
        #endregion
    }
}