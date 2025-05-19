using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace LPCafe.Elements
{
    using Windows;
    using Enumerations;
    using Utilities;

    public class DSSingleChoiceNode : NodeBase
    {
        public override void Initialize(DSGraphView dsGraphView, Vector2 pos)
        {
            base.Initialize(dsGraphView, pos);

            m_dialogueType = DSDialogueType.SingleChoice;

            m_choices.Add("Next Dialogue");
        }

        public override void Draw()
        {
            base.Draw();

            //OUTPUT CONTAINER.
            foreach (string choice in m_choices)
            {
                //Instantiates a port to another node for each choice in the node.
                Port choicePort = this.CreatePort(choice);

                choicePort.portName = choice;

                outputContainer.Add(choicePort);
            }

            //Calls the RefreshPorts function which Refreshes the layout of the ports.
            RefreshExpandedState();
        }
    }
}