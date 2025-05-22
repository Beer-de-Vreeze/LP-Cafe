using LPCafe.Enumerations;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace LPCafe.ScriptableObjects
{
    using Data;

    public class DSDialogueSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_dialogueNameData { get; set;}
        [field: SerializeField][field: TextArea()] public string m_dialogueTextData { get; set;}
        [field: SerializeField] public List<DSDialogueChoiceData> m_dialogueChoiceData {  get; set;}
        [field: SerializeField] public DSDialogueType m_dialogueTypeData { get; set;}
        [field: SerializeField] public bool m_isStartingDialogueData {  get; set;}

        public void Initialize(string dialogueName, string text, List<DSDialogueChoiceData> choices, DSDialogueType dialogueType, bool isStartingDialogue)
        {
            dialogueName = m_dialogueNameData;
            text = m_dialogueTextData;
            choices = m_dialogueChoiceData;
            dialogueType = m_dialogueTypeData;
            isStartingDialogue = m_isStartingDialogueData;
        }
    }
}