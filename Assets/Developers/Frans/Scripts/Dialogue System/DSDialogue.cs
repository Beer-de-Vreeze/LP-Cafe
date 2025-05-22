using System.Runtime.CompilerServices;
using LPCafe.ScriptableObjects;
using UnityEngine;

namespace LPCafe
{
    public class DSDialogue : MonoBehaviour
    {
        //Dialogue ScriptableObject
        [SerializeField]
        private DSDialogueContainerSO m_dialogueContainer;

        [SerializeField]
        private DSDialogueGroupSO m_dialogueGroup;

        [SerializeField]
        private DSDialogueSO m_dialogue;

        // Filters
        [SerializeField]
        private bool m_groupedDialogues;

        [SerializeField]
        private bool m_startingDialoguesOnly;

        // Indexes
        [SerializeField]
        private int m_selectedDialogueGroupIndex;

        [SerializeField]
        private int m_selectedDialogueIndex;
    }
}
