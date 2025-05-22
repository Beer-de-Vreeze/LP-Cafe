using UnityEngine;

namespace LPCafe.ScriptableObjects
{
    public class DSDialogueGroupSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_groupNameData {  get; set;}

        public void Initialize(string groupName)
        {
            m_groupNameData = groupName;
        }
    }
}