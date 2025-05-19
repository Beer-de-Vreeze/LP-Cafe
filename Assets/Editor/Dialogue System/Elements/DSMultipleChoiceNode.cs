using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace LPCafe.Elements
{
    using Windows;
    using Enumerations;
    using Utilities;

    public class DSMultipleChoiceNode : NodeBase
    {
        public override void Initialize(DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(dsGraphView, pos);

            m_dialogueType = DSDialogueType.MultipleChoice;

            m_choices.Add("New Choice");
        }

        public override void Draw()
        {
            base.Draw();

            /* Main Container */
            Button addChoiceButton = DSElementUtility.CreateButton("Add Choice", () =>
            {
                m_choices.Add("New Choice");
                Port choicePort = CreateChoicePort("New Choice");

                outputContainer.Add(choicePort);

            });

            addChoiceButton.AddToClassList("ds-node__button");

            mainContainer.Insert(1, addChoiceButton);

            //OUTPUT CONTAINER.
            foreach (string choice in m_choices)
            {
                Port choicePort = CreateChoicePort(choice);
                outputContainer.Add(choicePort);
            }

            //Calls the RefreshPorts function which Refreshes the layout of the ports.
            RefreshExpandedState();
        }

        #region Element Creation
        public Port CreateChoicePort(string choice)
        {
            //Instantiates a port to another node for each choice in the node.
            Port choicePort = this.CreatePort();

            //portName will bed decided by how its named in the editor.
            choicePort.portName = "";

            //Will be able to delete choices.
            Button deleteChoiceButton = DSElementUtility.CreateButton("X");

            deleteChoiceButton.AddToClassList("ds-node__button");

            TextField choiceTextField = DSElementUtility.CreateTextField(choice);

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