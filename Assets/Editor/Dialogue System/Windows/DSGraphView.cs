using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LPCafe.Windows
{
    using Data.Error;
    using Elements;
    using Enumerations;
    using Utilities;

    public class DSGraphView : GraphView
    {
        private DSEditorWindow m_editorWindow;
        private DSSearchWindow m_searchWindow;

        private SerializableDictionary<string, DSNodeErrorData> m_ungroupedNodes;
        private SerializableDictionary<string, DSGroupErrorData> m_groups;
        private SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>> m_groupedNodes;

        public DSGraphView(DSEditorWindow dSEditorWindow) 
        {
            m_editorWindow = dSEditorWindow;

            m_ungroupedNodes = new SerializableDictionary<string, DSNodeErrorData>();
            m_groups = new SerializableDictionary<string, DSGroupErrorData>();
            m_groupedNodes = new SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>>();

            AddSearchWindow();
            AddManipulators();
            AddGridBackground();

            OnElementsDeleted();
            OnGroupElementsAdded();
            OnGroupElementsRemoved();
            OnGroupRenamed();

            AddStyles();
        }

        #region Ports
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) 
        {        
            List<Port> compatiblePorts = new List<Port>();

            //Ports variable holds all the ports currently on the graph.
            ports.ForEach(port =>
            {
                if (startPort == port) 
                { 
                    return;
                }

                if (startPort.node == port.node)
                {
                    return;
                }

                if (startPort.direction == port.direction)
                {
                    return;
                }

                //If the statements above this are not true the port is connectable.
                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }
        #endregion
        
        #region Manipulators
        private void AddManipulators()
        {
            //Allows you to zoom in and out in the Graphview.
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            //Allows you to move inside the graphview using the middle mouse button.
            this.AddManipulator(new ContentDragger());

            //For dragging selected nodes around (Has to be before the rectangle selector otherwise it doesn't work).
            this.AddManipulator(new SelectionDragger());

            //To make a selection of nodes that you want to move around.
            this.AddManipulator(new RectangleSelector());

            //Will add a menu item to make a Single/Multiple Choice Node.
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Single Choice)", DSDialogueType.SingleChoice));
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Multiple Choice)", DSDialogueType.MultipleChoice));

            //To make the dialogue groups.
            this.AddManipulator(CreateGroupContextualMenu());
        }

        private IManipulator CreateNodeContextualMenu(string actionTitle, DSDialogueType dialogueType)
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator
            (
                //Will place a node at the current mouse position.

                menuEvent => menuEvent.menu.AppendAction(actionTitle, actionEvent => AddElement(CreateNode(dialogueType, GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))))
            );

            return contextualMenuManipulator;
        }

        private IManipulator CreateGroupContextualMenu()
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator
            (
                //Will place a node at the current mouse position
                menuEvent => menuEvent.menu.AppendAction("Add Group", actionEvent => AddElement(CreateGroup("DialogueGroup", GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))))
            );

            return contextualMenuManipulator;
        }


        #endregion

        #region Element Creation
        private void AddSearchWindow()
        {
            if (m_searchWindow == null)
            {
                m_searchWindow = ScriptableObject.CreateInstance<DSSearchWindow>();

                m_searchWindow.Initializaze(this);
            }

            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_searchWindow);
        }

        public DSGroup CreateGroup(string groupName, Vector2 localMousePosition)
        {
            DSGroup group = new DSGroup(groupName, localMousePosition);

            AddGroup(group);

            return group;
        }

        public NodeBase CreateNode(DSDialogueType dialogueType, Vector2 nodePos)
        {
            //For instantiating a node. Uses enum value to decide which type of node to instantiate.

            //$ means you can pass a type within a string by using {}.
            Type nodeType = Type.GetType($"LPCafe.Elements.DS{dialogueType}Node");

            NodeBase node = (NodeBase) Activator.CreateInstance(nodeType);

            node.Initialize(this, nodePos);
            node.Draw();

            AddUngroupedNode(node);
            return node;
        }
        #endregion

        #region Callbacks

        private void OnElementsDeleted()
        {
            deleteSelection = (operationName, askUser) =>
            {
                Type groupType = typeof(DSGroup);

                List<DSGroup> groupsToDelete = new List<DSGroup>();
                List<NodeBase> nodesToDelete = new List<NodeBase>();
                
                foreach (GraphElement element in selection)
                {
                    if (element is NodeBase node)
                    {
                        nodesToDelete.Add((NodeBase)element);

                        continue;
                    }

                    if(element.GetType() != groupType)
                    {
                        continue;
                    }

                    DSGroup group = (DSGroup) element;

                    RemoveGroup(group);

                    groupsToDelete.Add(group);
                }

                foreach (DSGroup group in groupsToDelete)
                {
                    RemoveElement(group);
                }

                foreach(NodeBase node in nodesToDelete)
                {
                    if(node.m_group != null)
                    {
                        //Will call the elementsRemovedFromGroup callback.
                        node.m_group.RemoveElement(node);
                    }
                    RemoveUngroupedNode(node);

                    RemoveElement(node);
                }
            };
        }

        private void OnGroupElementsAdded()
        {
            elementsAddedToGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (!(element is NodeBase))
                    {
                        continue;
                    }

                    DSGroup nodeGroup = (DSGroup) group;
                    NodeBase node = (NodeBase) element;

                    RemoveUngroupedNode(node);

                    AddGroupedNode(node, nodeGroup);
                }
            };
        }

        private void OnGroupElementsRemoved()
        {
            elementsRemovedFromGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (!(element is NodeBase))
                    {
                        continue;
                    }

                    NodeBase node = (NodeBase)element;

                    //Remove node from group.
                    //Then adds node to m_ungroupedNodes Dictionary.

                    RemoveGroupedNode(node, group);
                    AddUngroupedNode(node);
                }
            };
        }

        private void OnGroupRenamed()
        {
            groupTitleChanged = (group, newTitle) =>
            {
                DSGroup dsGroup = (DSGroup) group;

                RemoveGroup(dsGroup);

                dsGroup.m_oldTitle = newTitle;

                AddGroup(dsGroup);
            };
        }
        #endregion

        #region Repeated Elements
        #region Ungrouped
        public void AddUngroupedNode(NodeBase node)
        {
            string nodeName = node.m_dialogueName;

            //Only continues if the nodeName already exists.
            if (!m_ungroupedNodes.ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();

                nodeErrorData.m_nodes.Add(node);

                m_ungroupedNodes.Add(nodeName, nodeErrorData);
                return;
            }

            //Add the 2+ nodes with the same name to a list to check when one of the 2 is changed.
            List<NodeBase> ungroupedNodesList = m_ungroupedNodes[nodeName].m_nodes;

            ungroupedNodesList.Add(node);

            Color errorColor = m_ungroupedNodes[nodeName].m_errorData.m_color;

            //Make a random color.
            node.SetErrorStyle(errorColor);

            //If there are 2 nodes with the same nodeName change the color of the container there in to let the user know
            if (ungroupedNodesList.Count == 2)
            {
                ungroupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveUngroupedNode(NodeBase node)
        {
            string nodeName = node.m_dialogueName;

            List<NodeBase> ungroupedNodesList = m_ungroupedNodes[nodeName].m_nodes;

            ungroupedNodesList.Remove(node);

            node.ResetStyle();

            //If the node is the only one with that name change the color back to the original colors.
            if (ungroupedNodesList.Count == 1)
            {
                //Will reset the style of the first item in the array(m_ungroupedNodes) when theyre are no dialogues with the same name.
                ungroupedNodesList[0].ResetStyle();

                return;
            }

            if (ungroupedNodesList.Count == 0)
            {
                //Removes the dictionary item with the name nodeName.
                m_ungroupedNodes.Remove(nodeName);
            }
        }
        #endregion
        #region Grouped
        public void AddGroupedNode(NodeBase node, DSGroup group)
        {
            string nodeName = node.m_dialogueName;

            node.m_group = group;

            if (!m_groupedNodes.ContainsKey(group))
            {
                m_groupedNodes.Add(group, new SerializableDictionary<string, DSNodeErrorData>());
            }

            //Checks if the group already contains a node with that nodeName.
            if (!m_groupedNodes[group].ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();

                nodeErrorData.m_nodes.Add(node);

                m_groupedNodes[group].Add(nodeName, nodeErrorData);

                return;
            }

            List<NodeBase> groupedNodesList = m_groupedNodes[group][nodeName].m_nodes;

            groupedNodesList.Add(node);

            Color errorColor = m_groupedNodes[group][nodeName].m_errorData.m_color;

            node.SetErrorStyle(errorColor);

            if (groupedNodesList.Count == 2)
            {
                groupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroupedNode(NodeBase node, Group group)
        {
            string nodeName = node.m_dialogueName;

            node.m_group = null;

            List<NodeBase> groupedNodesList = m_groupedNodes[group][nodeName].m_nodes;

            groupedNodesList.Remove(node);

            node.ResetStyle();

            if (groupedNodesList.Count == 1)
            {
                groupedNodesList[0].ResetStyle();

                return;
            }

            if (groupedNodesList.Count == 0)
            {
                m_groupedNodes[group].Remove(nodeName);

                if (m_groupedNodes[group].Count == 0)
                {
                    m_groupedNodes.Remove(group);
                }
            }
        }
        #endregion

        private void AddGroup(DSGroup group)
        {
            string groupName = group.title;

            if (!m_groups.ContainsKey(groupName))
            {
                DSGroupErrorData groupErrorData = new DSGroupErrorData();

                groupErrorData.m_errorDatagroups.Add(group);

                m_groups.Add(groupName, groupErrorData);

                return;
            }

            List<DSGroup> groupsList = m_groups[groupName].m_errorDatagroups;

            groupsList.Add(group);

            Color color = m_groups[groupName].m_errorData.m_color;

            group.SetErrorStyle(color);

            if(groupsList.Count == 2)
            {
                groupsList[0].SetErrorStyle(color);
            }
        }

        private void RemoveGroup(DSGroup group)
        {
            string oldGroupName = group.m_oldTitle;

            List<DSGroup> groupsList = m_groups[oldGroupName].m_errorDatagroups;

            groupsList.Remove(group);

            group.ResetStyle();

            if (groupsList.Count == 2)
            {
                groupsList[0].ResetStyle();
            }

            if (groupsList.Count == 0)
            {
                m_groups.Remove(oldGroupName);
            }
        }
        #endregion

        #region Element Addition
        private void AddGridBackground()
        {
            //Also has a default size of 0!
            GridBackground gridBackground = new GridBackground();

            //Same as in DSEditorWindow.
            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        private void AddStyles()
        {
            this.AddStyleSheets
            (
                "Assets/Editor/Editor Default Resources/DialogueSystemStyle/GraphViewStyleSheet.uss",
                "Assets/Editor/Editor Default Resources/DialogueSystemStyle/DSNodeStyles.uss"
            );
        }
        #endregion

        #region Utility
        //For world to graph position!
        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
        {
            Vector2 worldMousePos = mousePosition;

            if (isSearchWindow)
            {
                worldMousePos -= m_editorWindow.position.position;
                
            }

            Vector2 localMousePos = contentViewContainer.WorldToLocal(worldMousePos);

            return localMousePos;
        }
        #endregion
    }
}