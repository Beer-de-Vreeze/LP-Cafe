using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace DS.ScriptableObjects
{
    public class DSDialogueContainerSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField]
        public string m_containerFileNameData { get; set; }

        [field: SerializeField]
        public SerializableDictionary<
            DSDialogueGroupSO,
            List<DSDialogueSO>
        > m_containerDialogueGroupsData { get; set; }

        [field: SerializeField]
        public List<DSDialogueSO> m_containerUngroupedDialoguesData { get; set; }

        public void Initialize(string fileName)
        {
            m_containerFileNameData = fileName;

            m_containerDialogueGroupsData =
                new SerializableDictionary<DSDialogueGroupSO, List<DSDialogueSO>>();
            m_containerUngroupedDialoguesData = new List<DSDialogueSO>();
        }

        public List<string> GetDialogueGroupNames()
        {
            List<string> dialogueGroupNames = new List<string>();

            foreach (DSDialogueGroupSO dialogueGroup in m_containerDialogueGroupsData.Keys)
            {
                dialogueGroupNames.Add(dialogueGroup.name);
            }

            return dialogueGroupNames;
        }

        public List<string> GetGroupedDialogueNames(
            DSDialogueGroupSO dialogueGroup,
            bool startingDialoguesOnly
        )
        {
            List<DSDialogueSO> groupedDialogues = m_containerDialogueGroupsData[dialogueGroup];

            List<string> groupedDialogueNames = new List<string>();

            foreach (DSDialogueSO groupedDialogue in groupedDialogues)
            {
                if (startingDialoguesOnly && !groupedDialogue.m_isStartingDialogueData)
                {
                    continue;
                }

                groupedDialogueNames.Add(groupedDialogue.name);
            }

            return groupedDialogueNames;
        }

        public List<string> GetUngroupedDialogueNames(bool startingDialoguesOnly)
        {
            List<string> ungroupedDialogueNames = new List<string>();

            foreach (DSDialogueSO ungroupedDialogue in m_containerUngroupedDialoguesData)
            {
                if (startingDialoguesOnly && !ungroupedDialogue.m_isStartingDialogueData)
                {
                    continue;
                }

                ungroupedDialogueNames.Add(ungroupedDialogue.name);
            }

            return ungroupedDialogueNames;
        }
    }
}