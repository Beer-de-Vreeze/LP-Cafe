using System.Runtime.CompilerServices;
using DS.ScriptableObjects;
using UnityEngine;

namespace DS
{
    public class DSDialogue : MonoBehaviour
    {
        //Dialogue ScriptableObject
        [SerializeField]
        public DSDialogueContainerSO m_dialogueContainer;

        [SerializeField]
        public DSDialogueGroupSO m_dialogueGroup;

        [SerializeField]
        public DSDialogueSO m_dialogue;

        // Filters
        [SerializeField]
        public bool m_groupedDialogues;

        [SerializeField]
        public bool m_startingDialoguesOnly;

        // Indexes
        [SerializeField]
        public int m_selectedDialogueGroupIndex;

        [SerializeField]
        public int m_selectedDialogueIndex;
    }
}
