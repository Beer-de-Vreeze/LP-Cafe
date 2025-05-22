using UnityEngine;

namespace LPCafe.Data
{
    using ScriptableObjects;
    using System;

    [Serializable]
    public class DSDialogueChoiceData
    {
        [field: SerializeField] public string m_dialogueChoiceText { get; set;}
        [field: SerializeField] public DSDialogueSO m_nextDialogue { get; set;}
    }
}

