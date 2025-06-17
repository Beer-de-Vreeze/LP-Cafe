using System;
using UnityEngine;

namespace DS.Data.Save
{
    [Serializable]
    public class DSGroupSaveData
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_groupIDData { get; set;}
        [field: SerializeField] public string m_groupNameData { get; set;}
        [field: SerializeField] public Vector2 m_groupPositionData { get; set;}
    }
}