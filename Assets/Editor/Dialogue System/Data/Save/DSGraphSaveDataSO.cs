using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DS.Data.Save
{
    public class DSGraphSaveDataSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_graphFileNameData { get; set; }
        [field: SerializeField] public List<DSGroupSaveData> m_graphGroupsData {  get; set;}
        [field: SerializeField] public List<DSNodeSaveData> m_graphNodesData { get; set;}
        [field: SerializeField] public List<string> m_graphOldGroupNamesData { get; set;}
        [field: SerializeField] public List<string> m_graphOldUngroupedNodeNamesData { get; set;}
        [field: SerializeField] public SerializableDictionary<string, List<string>> m_graphOldGroupedNodeNamesData { get; set;}

        public void Initialize(string fileName)
        {
            m_graphFileNameData = fileName;

            m_graphGroupsData = new List<DSGroupSaveData>();
            m_graphNodesData = new List<DSNodeSaveData>();
        }
    }
}