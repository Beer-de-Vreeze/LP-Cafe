using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace DS.Data.Save
{
    using DS.Elements;
    using Enumerations;
    using System;

    [Serializable]
    public class DSNodeSaveData
    {
        //Base Node
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_nodeIDData {  get; set;}
        [field: SerializeField] public string m_nodeNameData{ get; set;}
        [field: SerializeField] public Sprite m_nodeBachelorImageData { get; set;}
        [field: SerializeField] public AudioClip m_nodeAudioLinesData { get; set;}
        [field: SerializeField] public string m_nodeTextData {  get; set;}
        [field: SerializeField] public List<DSChoiceSaveData> m_nodeChoicesData {  get; set;}
        [field: SerializeField] public string m_nodeGroupIDData {  get; set;}
        [field: SerializeField] public DSDialogueType m_dialogueTypeData { get; set;}
        [field: SerializeField] public Vector2 m_nodePositionData { get; set;}

        //Condition Node
        [field: SerializeField] public string m_nodePropertyToCheckData { get; set; }
        [field: SerializeField] public string m_nodeComparisonTypeData { get; set; }
        [field: SerializeField] public string m_nodeComparisonValueData { get; set; }
        [field: SerializeField] public string m_nodeOperationTypeData { get; set; }

        //Setter Node
        [field: SerializeField] public string m_nodeValueToSetData { get; set; }
        [field: SerializeField] public string m_nodeVariableNameData { get; set; }
        [field: SerializeField] public SetterOperationType m_nodeSetterOperationTypeData { get; set; }
        [field: SerializeField] public int m_nodeLoveScoreAmountData { get; set; }
        [field: SerializeField] public bool m_nodeBoolValueData { get; set; }
        [field: SerializeField] public LoveMeterSO m_nodeLoveMeterData { get; set; }
        [field: SerializeField] public NewBachelorSO m_bachelorData { get; set; }
        [field: SerializeField] public bool m_isLikePreference { get; set; }
        [field: SerializeField] public string m_selectedPreference { get; set; }

    }
}