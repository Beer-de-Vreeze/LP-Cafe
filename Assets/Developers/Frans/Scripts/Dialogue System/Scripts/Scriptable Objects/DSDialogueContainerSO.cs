using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace LPCafe.ScriptableObjects
{
    public class DSDialogueContainerSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_containerFileNameData { get; set; }
        [field: SerializeField] public SerializableDictionary<DSDialogueGroupSO, List<DSDialogueSO>> m_containerDialogueGroupsData { get; set; }
        [field: SerializeField] public List<DSDialogueSO> m_containerUngroupedDialoguesData { get; set; }

        public void Initialize(string fileName)
        {
            m_containerFileNameData = fileName;

            m_containerDialogueGroupsData = new SerializableDictionary<DSDialogueGroupSO, List<DSDialogueSO>>();
            m_containerUngroupedDialoguesData = new List<DSDialogueSO>();
        }
    }
}