using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace LPCafe.Data.Save
{
    using Enumerations;
    using System;

    [Serializable]
    public class DSNodeSaveData
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_nodeIDData {  get; set; }
        [field: SerializeField] public string m_nodeNameData{ get; set;}
        [field: SerializeField] public string m_nodeTextData {  get; set;}
        [field: SerializeField] public List<DSChoiceSaveData> m_nodeChoicesData {  get; set;}
        [field: SerializeField] public string m_nodeGroupIDData {  get; set;}
        [field: SerializeField] public DSDialogueType m_dialogueTypeData { get; set;}

        [field: SerializeField] public Vector2 m_nodePositionData { get; set;}
    }
}