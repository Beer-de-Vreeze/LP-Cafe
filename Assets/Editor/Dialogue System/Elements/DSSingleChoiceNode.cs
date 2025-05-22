using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace LPCafe.Elements
{
    using Windows;
    using Enumerations;
    using Utilities;
    using LPCafe.Data.Save;

    public class DSSingleChoiceNode : NodeBase
    {
        public override void Initialize(string nodeName,DSGraphView dsGraphView, Vector2 pos)
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
            base.Draw();

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