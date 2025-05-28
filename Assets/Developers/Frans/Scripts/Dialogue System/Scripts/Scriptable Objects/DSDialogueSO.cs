using DS.Enumerations;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DS.ScriptableObjects
{
    using Data;

    public class DSDialogueSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField] public string m_dialogueNameData { get; set;}
        [field: SerializeField][field: TextArea()] public string m_dialogueTextData { get; set;}
        [field: SerializeField] public List<DSDialogueChoiceData> m_dialogueChoiceData {  get; set;}
        [field: SerializeField] public Sprite m_bachelorImageData { get; set; }
        [field: SerializeField]public AudioClip m_dialogueAudioData { get; set; }
        [field: SerializeField] public DSDialogueType m_dialogueTypeData { get; set;}
        [field: SerializeField] public bool m_isStartingDialogueData {  get; set;}

        public void Initialize(string dialogueName, string text, List<DSDialogueChoiceData> choices, Sprite nodeImage, AudioClip nodeAudio, DSDialogueType dialogueType, bool isStartingDialogue)
        {
            m_dialogueNameData = dialogueName;
            m_dialogueTextData = text;
            m_dialogueChoiceData = choices;
            m_bachelorImageData = nodeImage;
            m_dialogueAudioData = nodeAudio;
            m_dialogueTypeData = dialogueType;
            m_isStartingDialogueData = isStartingDialogue;
        }
    }
}