using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Windows
{
    using Data.Error;
    using Elements;
    using Enumerations;
    using DS.Data.Save;
    using Unity.Hierarchy;
    using Utilities;
    using static UnityEngine.GraphicsBuffer;

    public class DSGraphView : GraphView
    {
        private DSEditorWindow m_editorWindow;
        private DSSearchWindow m_searchWindow;

        private MiniMap m_miniMap;

        private SerializableDictionary<string, DSNodeErrorData> m_ungroupedNodes;
        private SerializableDictionary<string, DSGroupErrorData> m_groups;
        private SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>> m_groupedNodes;

        private int m_nameErrorsAmount;

        public int NameErrorsAmount
        {
            get
            {
                return m_nameErrorsAmount;
            }

            set
            {
                //Will hold the value the property will, have can be 0 or higher.
                m_nameErrorsAmount = value;

                //Checks if there are not repeated name errors and if so enables the use to save their data.
                if(m_nameErrorsAmount == 0)
                {
                    //Enables Save Button
                    m_editorWindow.EnableSaving();
                }

                //However if there is a repeated name error the user can't use the save button to prevent overriding previous data.
                else
                {
                    //Disables Save Button
                    m_editorWindow.DisableSaving();
                }
            }
        }

        public DSGraphView(DSEditorWindow dSEditorWindow) 
        {
            m_editorWindow = dSEditorWindow;

            m_ungroupedNodes = new SerializableDictionary<string, DSNodeErrorData>();
            m_groups = new SerializableDictionary<string, DSGroupErrorData>();
            m_groupedNodes = new SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>>();

            AddSearchWindow();
            AddManipulators();
            AddMiniMap();
            AddGridBackground();

            OnElementsDeleted();
            OnGroupElementsAdded();
            OnGroupElementsRemoved();
            OnGroupRenamed();
            OnGraphViewChanged();

            AddStyles();
            AddMiniMapStyles();
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

            //Will add a menu item for setter/checker Nodes.
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Setter Node)", DSDialogueType.Setter));
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Checker Node)", DSDialogueType.Check));

            //To make the dialogue groups.
            this.AddManipulator(CreateGroupContextualMenu());
        }

        private IManipulator CreateNodeContextualMenu(string actionTitle, DSDialogueType dialogueType)
        {
            //Will place a node at the current mouse position.
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator
            (
                menuEvent => menuEvent.menu.AppendAction(actionTitle, actionEvent => AddElement(CreateNode("DialogueName", dialogueType, GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))))
            );

            return contextualMenuManipulator;
        }

        private IManipulator CreateGroupContextualMenu()
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator
            (
                //Will place a node at the current mouse position
                menuEvent => menuEvent.menu.AppendAction("Add Group", actionEvent => CreateGroup("DialogueGroup", GetLocalMousePosition(actionEvent.eventInfo.localMousePosition)))
            );

            return contextualMenuManipulator;
        }
        #endregion

        #region Element Creation
        private void AddSearchWindow()
        {
            if (m_searchWindow == null)
            {
                //Makes an instance of the SO DSSearchWindow.
                m_searchWindow = ScriptableObject.CreateInstance<DSSearchWindow>();

                m_searchWindow.Initializaze(this);
            }

            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_searchWindow);
        }

        private void AddMiniMap()
        {
            m_miniMap = new MiniMap()
            {
                anchored = true
            };

            m_miniMap.SetPosition(new Rect(15, 50, 200, 100));

            Add(m_miniMap);

            m_miniMap.visible = false;
        }

        public DSGroup CreateGroup(string groupName, Vector2 localMousePosition)
        {
            DSGroup group = new DSGroup(groupName, localMousePosition);

            AddGroup(group);

            //Needed to activate the OnGroupElementsAdded callback.
            AddElement(group);

            //Adds selected nodes to the newly made group
            foreach(GraphElement selectedElement in selection)
            {
                if(!(selectedElement is NodeBase))
                {
                    continue;
                }

                NodeBase node = (NodeBase) selectedElement;
                
                group.AddElement(node);
            }

            return group;
        }

        public NodeBase CreateNode(string nodeName, DSDialogueType dialogueType, Vector2 nodePos, bool shouldDraw = true)
        {
            //For instantiating a node. Uses enum value to decide which type of node to instantiate.
            //$ means you can pass a variable within a string by using {}.
            Type nodeType = Type.GetType($"DS.Elements.DS{dialogueType}Node");

            NodeBase node = (NodeBase) Activator.CreateInstance(nodeType);

            node.Initialize(nodeName, this, nodePos);

            if (shouldDraw)
            {
                node.Draw();
            }

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
                Type edgeType  = typeof(Edge);

                List<DSGroup> groupsToDelete = new List<DSGroup>();
                List<NodeBase> nodesToDelete = new List<NodeBase>();
                List<Edge> edgesToDelete = new List<Edge>();

                foreach (GraphElement selectedElement in selection)
                {
                    if (selectedElement is NodeBase node)
                    {
                        //Adds the selected node to the nodes List.
                        nodesToDelete.Add(node);

                        continue;
                    }

                    if(selectedElement.GetType() == edgeType)
                    {
                        Edge edge = (Edge) selectedElement;

                        edgesToDelete.Add(edge);

                        continue;
                    }

                    if(selectedElement.GetType() != groupType)
                    {
                        continue;
                    }

                    DSGroup group = (DSGroup) selectedElement;

                    groupsToDelete.Add(group);
                }

                foreach (DSGroup group in groupsToDelete)
                {
                    //Make a list of nodes within the to be deleted group.
                    List<NodeBase> groupedNodes = new List<NodeBase>();

                    
                    foreach (GraphElement groupelement in group.containedElements)
                    {
                        //Check for each element if its not a node and if so continue.
                        if(!(groupelement is NodeBase node))
                        {
                            continue;
                        }

                        NodeBase groupNode = (NodeBase) groupelement;

                        //Add the node to the groupedNodes Serializable Dictionary.
                        groupedNodes.Add(groupNode);
                    }

                    //Then remove the element from the group so it doesnt get deleted with the group.
                    group.RemoveElements(groupedNodes);

                    RemoveGroup(group);

                    RemoveElement(group);
                }

                DeleteElements(edgesToDelete);

                foreach(NodeBase node in nodesToDelete)
                {
                    if(node.m_nodeGroup != null)
                    {
                        //Will call the elementsRemovedFromGroup callback.
                        node.m_nodeGroup.RemoveElement(node);
                    }

                    RemoveUngroupedNode(node);

                    node.DisconnectAllports();

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
                DSGroup dSGroup = (DSGroup) group;

                dSGroup.title = newTitle.RemoveWhitespaces().RemoveSpecialCharacters();

                //Checks if there is a value to make sure no empty things are saved.
                if (string.IsNullOrEmpty(dSGroup.title))
                {
                    if (!string.IsNullOrEmpty(dSGroup.m_oldTitle))
                    {
                        ++NameErrorsAmount;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(dSGroup.m_oldTitle))
                    {
                        --NameErrorsAmount;
                    }
                }

                RemoveGroup(dSGroup);

                dSGroup.m_oldTitle = dSGroup.title;

                AddGroup(dSGroup);
            };
        }

        private void OnGraphViewChanged()
        {
            //Allows us to go through all the changes and get the ones with the type edge and remove them from the graph
            graphViewChanged = (changes) =>
            {
                if(changes.edgesToCreate != null)
                {
                    foreach(Edge edge in changes.edgesToCreate)
                    {
                        NodeBase nextNode = (NodeBase) edge.input.node;

                        DSChoiceSaveData choiceData = (DSChoiceSaveData) edge.output.userData;

                        choiceData.m_choiceNodeIDData = nextNode.m_nodeID;
                    }
                }

                if(changes.elementsToRemove != null)
                {
                    Type edgeType = typeof(Edge);

                    foreach(GraphElement element in changes.elementsToRemove)
                    {
                        if(element.GetType() != edgeType )
                        {
                            continue;
                        }

                        Edge edge = (Edge) element;

                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;

                        choiceData.m_choiceNodeIDData = "";
                    }
                }

                return (changes);
            };
        }
        #endregion

        #region Repeated Elements
        #region Ungrouped
        public void AddUngroupedNode(NodeBase node)
        {
            string nodeName = node.m_nodeDialogueName.ToLower();

            //Only continues if the nodeName already exists.
            if (!m_ungroupedNodes.ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();

                nodeErrorData.m_nodesErrorData.Add(node);

                m_ungroupedNodes.Add(nodeName, nodeErrorData);
                return;
            }

            //Add the 2+ nodes with the same name to a list to check when one of the 2 is changed.
            List<NodeBase> ungroupedNodesList = m_ungroupedNodes[nodeName].m_nodesErrorData;

            ungroupedNodesList.Add(node);

            Color errorColor = m_ungroupedNodes[nodeName].m_errorData.m_color;

            //Make a random color.
            node.SetErrorStyle(errorColor);

            //If there are 2 nodes with the same nodeName change the color of the container there in to let the user know
            if (ungroupedNodesList.Count == 2)
            {
                ++NameErrorsAmount;

                ungroupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveUngroupedNode(NodeBase node)
        {
            string nodeName = node.m_nodeDialogueName.ToLower();

            List<NodeBase> ungroupedNodesList = m_ungroupedNodes[nodeName].m_nodesErrorData;

            ungroupedNodesList.Remove(node);

            node.ResetStyle();

            //If the node is the only one with that name change the color back to the original colors.
            if (ungroupedNodesList.Count == 1)
            {
                --NameErrorsAmount;

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

        private void AddGroup(DSGroup group)
        {
            string groupName = group.title.ToLower();

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

            if (groupsList.Count == 2)
            {
                ++NameErrorsAmount;

                groupsList[0].SetErrorStyle(color);
            }
        }

        private void RemoveGroup(DSGroup group)
        {
            string oldGroupName = group.m_oldTitle.ToLower();

            List<DSGroup> groupsList = m_groups[oldGroupName].m_errorDatagroups;

            groupsList.Remove(group);

            group.ResetStyle();

            if (groupsList.Count == 1)
            {
                --NameErrorsAmount;

                groupsList[0].ResetStyle();
            }

            if (groupsList.Count == 0)
            {
                m_groups.Remove(oldGroupName);
            }
        }

        #region Grouped
        public void AddGroupedNode(NodeBase node, DSGroup group)
        {
            string nodeName = node.m_nodeDialogueName.ToLower();

            node.m_nodeGroup = group;

            if (!m_groupedNodes.ContainsKey(group))
            {
                m_groupedNodes.Add(group, new SerializableDictionary<string, DSNodeErrorData>());
            }

            //Checks if the group already contains a node with that nodeName.
            if (!m_groupedNodes[group].ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();

                nodeErrorData.m_nodesErrorData.Add(node);

                m_groupedNodes[group].Add(nodeName, nodeErrorData);

                return;
            }

            List<NodeBase> groupedNodesList = m_groupedNodes[group][nodeName].m_nodesErrorData;

            groupedNodesList.Add(node);

            Color errorColor = m_groupedNodes[group][nodeName].m_errorData.m_color;

            node.SetErrorStyle(errorColor);

            if (groupedNodesList.Count == 2)
            {
                ++NameErrorsAmount;
                groupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroupedNode(NodeBase node, Group group)
        {
            string nodeName = node.m_nodeDialogueName.ToLower();

            node.m_nodeGroup = null;

            List<NodeBase> groupedNodesList = m_groupedNodes[group][nodeName].m_nodesErrorData;

            groupedNodesList.Remove(node);

            node.ResetStyle();

            if (groupedNodesList.Count == 1)
            {
                --NameErrorsAmount;

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

        private void AddMiniMapStyles()
        {
            StyleColor backgroundColor = new StyleColor(new Color32(29, 29, 30, 255));
            StyleColor borderColor = new StyleColor(new Color32(51, 51, 51, 255));

            m_miniMap.style.backgroundColor = backgroundColor;
            m_miniMap.style.borderTopColor = borderColor;
            m_miniMap.style.borderRightColor = borderColor;
            m_miniMap.style.borderBottomColor = borderColor;
            m_miniMap.style.borderLeftColor = borderColor;
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

        public void ClearGraph()
        {
            if(graphElements != null)
            {
                graphElements.ForEach(graphElements => RemoveElement(graphElements));

                m_groups.Clear();
                m_groupedNodes.Clear();
                m_ungroupedNodes.Clear();

                NameErrorsAmount = 0;
            }
        }

        public void ToggleMiniMap()
        {
            m_miniMap.visible = !m_miniMap.visible;
        }
        #endregion
    }
}