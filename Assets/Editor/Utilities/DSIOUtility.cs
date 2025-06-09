using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DS.Utilities
{
    using System.IO;
    using System.Linq;
    using Data.Save;
    using Elements;
    using DS.Data;
    using ScriptableObjects;
    using Windows;

    public static class DSIOUtility
    {
        //IO stands for both In- and Output
        //This script will both Save and Load the graph.

        private static DSGraphView m_graphView;

        private static string m_graphFileName;
        private static string m_containerFolderPath;

        private static List<NodeBase> m_nodes;
        private static List<DSGroup> m_groups;

        private static Dictionary<string, DSDialogueGroupSO> m_createdDialogueGroups;
        private static Dictionary<string, DSDialogueSO> m_createdDialogues;

        private static Dictionary<string, DSGroup> m_loadedGroups;
        private static Dictionary<string, NodeBase> m_loadedNodes;

        public static void Initialize(DSGraphView dsGraphView, string graphName)
        {
            m_graphView = dsGraphView;

            m_graphFileName = graphName;
            m_containerFolderPath = $"Assets/DialogueSystem/Dialogues/{m_graphFileName}";

            m_groups = new List<DSGroup>();
            m_nodes = new List<NodeBase>();

            m_createdDialogueGroups = new Dictionary<string, DSDialogueGroupSO>();
            m_createdDialogues = new Dictionary<string, DSDialogueSO>();

            m_loadedGroups = new Dictionary<string, DSGroup>();
            m_loadedNodes = new Dictionary<string, NodeBase>();
        }

        #region Save Methods
        public static void Save()
        {
            CreateDefaultFolders();

            GetElementsFromGraphView();

            DSGraphSaveDataSO graphData = CreateAsset<DSGraphSaveDataSO>(
                "Assets/Developers/Frans/Graphs",
                $"{m_graphFileName}Graph"
            );

            graphData.Initialize(m_graphFileName);

            DSDialogueContainerSO dialogueContainer = CreateAsset<DSDialogueContainerSO>(
                m_containerFolderPath,
                m_graphFileName
            );

            dialogueContainer.Initialize(m_graphFileName);

            SaveGroups(graphData, dialogueContainer);
            SaveNodes(graphData, dialogueContainer);

            SaveAsset(graphData);
            SaveAsset(dialogueContainer);
        }

        #region Groups
        private static void SaveGroups(
            DSGraphSaveDataSO graphData,
            DSDialogueContainerSO dialogueContainer
        )
        {
            List<string> groupNames = new List<string>();
            foreach (DSGroup group in m_groups)
            {
                SaveGroupToGraph(group, graphData);
                SaveGroupToScriptableObject(group, dialogueContainer);

                groupNames.Add(group.title);
            }

            UpdateOldGroups(groupNames, graphData);
        }

        private static void SaveGroupToGraph(DSGroup group, DSGraphSaveDataSO graphData)
        {
            DSGroupSaveData groupData = new DSGroupSaveData()
            {
                m_groupIDData = group.m_groupID,
                m_groupNameData = group.title,
                m_groupPositionData = group.GetPosition().position,
            };

            graphData.m_graphGroupsData.Add(groupData);
        }

        private static void SaveGroupToScriptableObject(
            DSGroup group,
            DSDialogueContainerSO dialogueContainer
        )
        {
            string groupName = group.title;

            //Creates a folder within the groups folder with the groupName.
            CreateFolder($"{m_containerFolderPath}/Groups", groupName);
            //Creates a folder within the groupname folder with the name Dialogues
            CreateFolder($"{m_containerFolderPath}/Groups/{groupName}", "Dialogues");

            //Creates a ScriptableObject with the type DSDialogueGroupSO within the folder with the groupname.
            DSDialogueGroupSO dialogueGroup = CreateAsset<DSDialogueGroupSO>(
                $"{m_containerFolderPath}/Groups/{groupName}",
                groupName
            );

            dialogueGroup.Initialize(groupName);

            m_createdDialogueGroups.Add(group.m_groupID, dialogueGroup);

            //Dialogues that belong to this group will be added to the scriptableobject dictionary.
            dialogueContainer.m_containerDialogueGroupsData.Add(
                dialogueGroup,
                new List<DSDialogueSO>()
            );

            SaveAsset(dialogueGroup);
        }

        private static void UpdateOldGroups(
            List<string> currenGroupNames,
            DSGraphSaveDataSO graphData
        )
        {
            if (
                graphData.m_graphOldGroupNamesData != null
                && graphData.m_graphOldGroupNamesData.Count != 0
            )
            {
                List<string> groupToRemove = graphData
                    .m_graphOldGroupNamesData.Except(currenGroupNames)
                    .ToList();

                foreach (string groupName in groupToRemove)
                {
                    RemoveFolder($"{m_containerFolderPath}/Groups/{groupToRemove}");
                }
            }

            graphData.m_graphOldGroupNamesData = new List<string>(currenGroupNames);
        }
        #endregion

        #region Nodes
        private static void SaveNodes(
           DSGraphSaveDataSO graphData,
           DSDialogueContainerSO dialogueContainer
        ){
            SerializableDictionary<string, List<string>> groupedNodeNames =
                new SerializableDictionary<string, List<string>>();
            List<string> ungroupedNodeNames = new List<string>();

            foreach (NodeBase node in m_nodes)
            {
                SaveNodeToGraph(node, graphData);
                SaveNodeToScriptableObject(node, dialogueContainer);

                if (node.m_nodeGroup != null)
                {
                    groupedNodeNames.AddItem(node.m_nodeGroup.title, node.m_nodeDialogueName);

                    continue;
                }

                ungroupedNodeNames.Add(node.m_nodeDialogueName);
            }

            UpdateDialogueChoicesConnections();

            UpdateOldGroupedNodes(groupedNodeNames, graphData);
            UpdateOldUngroupedNodes(ungroupedNodeNames, graphData);
        }

        private static void SaveNodeToGraph(NodeBase node, DSGraphSaveDataSO graphData)
        {
            List<DSChoiceSaveData> choiceSaveData = CloneNodeChoices(node.m_nodeChoices);

            DSNodeSaveData nodeData = new DSNodeSaveData()
            {
                m_nodeIDData = node.m_nodeID,
                m_nodeNameData = node.m_nodeDialogueName,
                m_nodeChoicesData = choiceSaveData,
                m_nodeTextData = node.m_nodeText,
                m_nodeBachelorImageData = node.m_nodeCharacterImage,
                m_nodeAudioLinesData = node.m_nodeAudio,
                m_nodeGroupIDData = node.m_nodeGroup?.m_groupID,
                m_dialogueTypeData = node.m_nodeDialogueType,
                m_nodePositionData = node.GetPosition().position,
            };

            graphData.m_graphNodesData.Add(nodeData);
        }

        private static void SaveNodeToScriptableObject(
            NodeBase node,
            DSDialogueContainerSO dialogueContainer
        )
        {
            DSDialogueSO dialogue;

            if (node.m_nodeGroup != null)
            {
                dialogue = CreateAsset<DSDialogueSO>(
                    $"{m_containerFolderPath}/Groups/{node.m_nodeGroup.title}/Dialogues",
                    node.m_nodeDialogueName
                );

                dialogueContainer.m_containerDialogueGroupsData.AddItem(
                    m_createdDialogueGroups[node.m_nodeGroup.m_groupID],
                    dialogue
                );
            }
            else
            {
                dialogue = CreateAsset<DSDialogueSO>(
                    $"{m_containerFolderPath}/Global/Dialogues",
                    node.m_nodeDialogueName
                );

                dialogueContainer.m_containerUngroupedDialoguesData.Add(dialogue);
            }

            dialogue.Initialize(
                node.m_nodeDialogueName,
                node.m_nodeText,
                ConvertNodeChoics(node.m_nodeChoices),
                node.m_nodeCharacterImage,
                node.m_nodeAudio,
                node.m_nodeDialogueType,
                node.IsStartingNode()
            );

            m_createdDialogues.Add(node.m_nodeID, dialogue);

            SaveAsset(dialogue);
        }

        private static List<DSDialogueChoiceData> ConvertNodeChoics(
            List<DSChoiceSaveData> nodeChoices
        )
        {
            List<DSDialogueChoiceData> dialogueChoices = new List<DSDialogueChoiceData>();

            foreach (DSChoiceSaveData nodeChoice in nodeChoices)
            {
                DSDialogueChoiceData choiceData = new DSDialogueChoiceData()
                {
                    m_dialogueChoiceText = nodeChoice.m_choiceTextData,
                };

                dialogueChoices.Add(choiceData);
            }

            return dialogueChoices;
        }

        private static void UpdateDialogueChoicesConnections()
        {
            foreach (NodeBase node in m_nodes)
            {
                if (
                    !m_createdDialogues.TryGetValue(node.m_nodeID, out DSDialogueSO dialogue)
                    || dialogue == null
                )
                {
                    continue;
                }

                if (dialogue.m_dialogueChoiceData == null)
                {
                    continue;
                }

                for (int choiceIndex = 0; choiceIndex < node.m_nodeChoices.Count; ++choiceIndex)
                {
                    DSChoiceSaveData nodeChoice = node.m_nodeChoices[choiceIndex];

                    if (string.IsNullOrEmpty(nodeChoice.m_choiceNodeIDData))
                    {
                        continue;
                    }

                    if (!m_createdDialogues.ContainsKey(nodeChoice.m_choiceNodeIDData))
                    {
                        continue;
                    }

                    // Check if choiceIndex is within bounds of the dialogue choice data collection
                    if (choiceIndex >= dialogue.m_dialogueChoiceData.Count)
                    {
                        continue;
                    }

                    dialogue.m_dialogueChoiceData[choiceIndex].m_nextDialogue = m_createdDialogues[
                        nodeChoice.m_choiceNodeIDData
                    ];

                    SaveAsset(dialogue);
                }
            }
        }

        private static void UpdateOldGroupedNodes(
            SerializableDictionary<string, List<string>> currentGroupedNodeNames,
            DSGraphSaveDataSO graphData
        )
        {
            if (
                graphData.m_graphOldGroupedNodeNamesData != null
                && graphData.m_graphOldGroupedNodeNamesData.Count != 0
            )
            {
                foreach (
                    KeyValuePair<
                        string,
                        List<string>
                    > oldGroupedNode in graphData.m_graphOldGroupedNodeNamesData
                )
                {
                    List<string> nodesToRemove = new List<string>();

                    if (currentGroupedNodeNames.ContainsKey(oldGroupedNode.Key))
                    {
                        nodesToRemove = oldGroupedNode
                            .Value.Except(currentGroupedNodeNames[oldGroupedNode.Key])
                            .ToList();
                    }

                    foreach (string nodeToRemove in nodesToRemove)
                    {
                        RemoveAsset(
                            $"{m_containerFolderPath}/Groups/{oldGroupedNode.Key}/Dialogues",
                            nodesToRemove
                        );
                    }
                }
            }

            graphData.m_graphOldGroupedNodeNamesData = new SerializableDictionary<
                string,
                List<string>
            >(currentGroupedNodeNames);
        }

        private static void UpdateOldUngroupedNodes(
            List<string> currentUngroupedNodeNames,
            DSGraphSaveDataSO graphData
        )
        {
            if (
                graphData.m_graphOldUngroupedNodeNamesData != null
                && graphData.m_graphOldUngroupedNodeNamesData.Count != 0
            )
            {
                List<string> nodesToRemove = graphData.m_graphOldUngroupedNodeNamesData;

                foreach (string nodeToRemove in nodesToRemove)
                {
                    RemoveAsset($"{m_containerFolderPath}/Global/Dialogues", nodesToRemove);
                }
            }

            graphData.m_graphOldUngroupedNodeNamesData = new List<string>(
                currentUngroupedNodeNames
            );
        }
        #endregion
        #endregion

        #region Load
        public static void Load()
        {
            DSGraphSaveDataSO graphData = LoadAsset<DSGraphSaveDataSO>(
                "Assets/Developers/Frans/Graphs",
                m_graphFileName
            );

            if (graphData == null)
            {
                EditorUtility.DisplayDialog(
                    "Coulnd't load the file",
                    "The file at the following path could not be found:\n\n"
                        + $"Assets/Developers/Frans/Graphs/{m_graphFileName}\n\n",
                    "Make sure you chose the right file and it's placed at the folder path mentioned above.",
                    "Thanks"
                );
                return;
            }

            DSEditorWindow.UpdateFileName(graphData.m_graphFileNameData);

            LoadGroups(graphData.m_graphGroupsData);
            LoadNodes(graphData.m_graphNodesData);
            LoadNodesConnection();
        }

        private static void LoadGroups(List<DSGroupSaveData> m_graphGroupsData)
        {
            foreach (DSGroupSaveData groupData in m_graphGroupsData)
            {
                DSGroup group = m_graphView.CreateGroup(
                    groupData.m_groupNameData,
                    groupData.m_groupPositionData
                );

                group.m_groupID = groupData.m_groupIDData;

                m_loadedGroups.Add(group.m_groupID, group);
            }
        }

        private static void LoadNodes(List<DSNodeSaveData> m_graphNodesData)
        {
            foreach (DSNodeSaveData nodeSaveData in m_graphNodesData)
            {
                List<DSChoiceSaveData> choices = CloneNodeChoices(nodeSaveData.m_nodeChoicesData);

                NodeBase node = m_graphView.CreateNode(
                    nodeSaveData.m_nodeNameData,
                    nodeSaveData.m_dialogueTypeData,
                    nodeSaveData.m_nodePositionData,
                    false
                );

                node.m_nodeID = nodeSaveData.m_nodeIDData;
                node.m_nodeAudio = nodeSaveData.m_nodeAudioLinesData;
                node.m_nodeCharacterImage = nodeSaveData.m_nodeBachelorImageData;
                node.m_nodeChoices = choices;
                node.m_nodeText = nodeSaveData.m_nodeTextData;

                node.Draw();

                m_graphView.AddElement(node);

                m_loadedNodes.Add(node.m_nodeID, node);

                if (string.IsNullOrEmpty(nodeSaveData.m_nodeGroupIDData))
                {
                    continue;
                }

                DSGroup group = m_loadedGroups[nodeSaveData.m_nodeGroupIDData];

                node.m_nodeGroup = group;

                group.AddElement(node);
            }
        }

        private static void LoadNodesConnection()
        {
            foreach (KeyValuePair<string, NodeBase> loadedNode in m_loadedNodes)
            {
                foreach (Port choicePort in loadedNode.Value.outputContainer.Children())
                {
                    DSChoiceSaveData choiceData = (DSChoiceSaveData)choicePort.userData;

                    if (string.IsNullOrEmpty(choiceData.m_choiceNodeIDData))
                    {
                        continue;
                    }

                    NodeBase nextNode = m_loadedNodes[choiceData.m_choiceNodeIDData];

                    Port nextNodeInputPort = (Port)nextNode.inputContainer.Children().First();

                    Edge edge = choicePort.ConnectTo(nextNodeInputPort);

                    m_graphView.AddElement(edge);

                    loadedNode.Value.RefreshPorts();
                }
            }
        }
        #endregion

        #region Creation Methods
        private static void CreateDefaultFolders()
        {
            CreateFolder("Assets/Editor/DialogueSystem", "Graphs");

            CreateFolder("Assets", "DialogueSystem");
            CreateFolder("Assets/DialogueSystem", "Dialogues");

            CreateFolder("Assets/DialogueSystem/Dialogues", m_graphFileName);
            CreateFolder(m_containerFolderPath, "Global");
            CreateFolder(m_containerFolderPath, "Groups");
            CreateFolder($"{m_containerFolderPath}/Global", "Dialogues");
        }
        #endregion

        #region Fetch Methods
        private static void GetElementsFromGraphView()
        {
            Type groupType = typeof(DSGroup);

            m_graphView.graphElements.ForEach(graphElement =>
            {
                if (graphElement is NodeBase node)
                {
                    m_nodes.Add(node);

                    return;
                }

                if (graphElement.GetType() == groupType)
                {
                    DSGroup group = (DSGroup)graphElement;

                    m_groups.Add(group);

                    return;
                }
            });
        }
        #endregion

        #region Utility Methods
        //To create a folder this function needs a path to were the folder needs to be made, and what the name of the folder needs to be.
        public static void CreateFolder(string parentFolderPath, string newFolderName)
        {
            //If the folder already exists it doesnt make it again
            if (AssetDatabase.IsValidFolder($"{parentFolderPath}/{newFolderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(parentFolderPath, newFolderName);
        }

        public static void RemoveFolder(string fullPath)
        {
            FileUtil.DeleteFileOrDirectory($"{fullPath}.meta");
            FileUtil.DeleteFileOrDirectory($"{fullPath}/");
        }

        //This function will create an asset with type scriptableObject.
        //With the name you want and send it to the path named when calling the method.
        public static T CreateAsset<T>(string path, string assetName)
            where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            T asset = LoadAsset<T>(path, assetName);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(asset, fullPath);
            }

            return asset;
        }

        public static T LoadAsset<T>(string path, string assetName)
            where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }

        public static void RemoveAsset(string path, List<string> assetName)
        {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        }

        //Type needs to be UnityEngine.Object for it to save it as dirty.(UnityEngine needs to be in there because System also has an Object type)
        //It needs to be saved as dirty for the save file to overwrite when necessary.
        public static void SaveAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<DSChoiceSaveData> CloneNodeChoices(List<DSChoiceSaveData> nodeChoices)
        {
            List<DSChoiceSaveData> choiceSaveData = new List<DSChoiceSaveData>();

            foreach (DSChoiceSaveData choice in nodeChoices)
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData()
                {
                    m_choiceTextData = choice.m_choiceTextData,
                    m_choiceNodeIDData = choice.m_choiceNodeIDData,
                };

                choiceSaveData.Add(choiceData);
            }

            return choiceSaveData;
        }
        #endregion
    }
}