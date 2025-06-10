using System.Collections.Generic;
using DS.Enumerations;
using NUnit.Framework;
using UnityEngine;

namespace DS.ScriptableObjects
{
    using Data;

    public class DSDialogueSO : ScriptableObject
    {
        //Add Data to the variable name to know its for saving Data!
        [field: SerializeField]
        public string m_dialogueNameData { get; set; }

        [field: SerializeField]
        [field: TextArea()]
        public string m_dialogueTextData { get; set; }

        [field: SerializeField]
        public List<DSDialogueChoiceData> m_dialogueChoiceData { get; set; }

        [field: SerializeField]
        public Sprite m_bachelorImageData { get; set; }

        [field: SerializeField]
        public AudioClip m_dialogueAudioData { get; set; }

        [field: SerializeField]
        public DSDialogueType m_dialogueTypeData { get; set; }

        [field: SerializeField]
        public bool m_isStartingDialogueData { get; set; }

        // New fields for condition nodes
        [field: SerializeField]
        public string m_propertyToCheck { get; set; }

        [field: SerializeField]
        public string m_comparisonType { get; set; }

        [field: SerializeField]
        public string m_comparisonValue { get; set; }

        // New fields for setter nodes
        [field: SerializeField]
        public string operationType { get; set; }

        [field: SerializeField]
        public string variableName { get; set; }

        [field: SerializeField]
        public string valueToSet { get; set; }

        [field: SerializeField]
        public string loveScoreAmount { get; set; }

        [field: SerializeField]
        public string boolValue { get; set; }

        public void Initialize(
            string dialogueName,
            string text,
            List<DSDialogueChoiceData> choices,
            Sprite nodeImage,
            AudioClip nodeAudio,
            DSDialogueType dialogueType,
            bool isStartingDialogue
        )
        {
            m_dialogueNameData = dialogueName;
            m_dialogueTextData = text;
            m_dialogueChoiceData = choices;
            m_bachelorImageData = nodeImage;
            m_dialogueAudioData = nodeAudio;
            m_dialogueTypeData = dialogueType;
            m_isStartingDialogueData = isStartingDialogue;
        }

        // Additional initialize methods for different node types
        public void InitializeConditionNode(
            string dialogueName,
            string propertyToCheck,
            string comparisonType,
            string comparisonValue,
            List<DSDialogueChoiceData> choices,
            DSDialogueType dialogueType,
            bool isStartingDialogue
        )
        {
            m_dialogueNameData = dialogueName;
            m_propertyToCheck = propertyToCheck;
            m_comparisonType = comparisonType;
            m_comparisonValue = comparisonValue;
            m_dialogueChoiceData = choices;
            m_dialogueTypeData = dialogueType;
            m_isStartingDialogueData = isStartingDialogue;
        }

        public void InitializeSetterNode(
            string operationType,
            string variableName,
            string valueToSet,
            string loveScoreAmount,
            string boolValue
        )
        {
            this.operationType = operationType;
            this.variableName = variableName;
            this.valueToSet = valueToSet;
            this.loveScoreAmount = loveScoreAmount;
            this.boolValue = boolValue;
        }
    }
}