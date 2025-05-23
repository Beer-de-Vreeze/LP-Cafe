using System;
using UnityEngine;


namespace DS.Data.Save
{
    [Serializable]
    public class DSChoiceSaveData
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_choiceTextData {  get; set;}
        [field: SerializeField] public string m_choiceNodeIDData {  get; set;}
    }
}