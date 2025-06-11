using System.Collections.Generic;
using DS.Enumerations;

using NUnit.Framework;
using UnityEngine;

namespace DS.ScriptableObjects
{
    using Data;
    using System;

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
        public SetterOperationType m_operationType { get; set; }

        [field: SerializeField]
        public string m_variableName { get; set; }

        [field: SerializeField]
        public string m_valueToSet { get; set; }

        [field: SerializeField]
        public int m_loveScoreAmount { get; set; }

        [field: SerializeField]
        public bool m_boolValue { get; set; }

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
            string dialogueName,
            List<DSDialogueChoiceData> choices,
            string valueToSet,
            string variableName,
            SetterOperationType operationType,
            int loveScoreAmount,
            bool boolValue,
            DSDialogueType dialogueType,
            bool isStartingDialogue
        )
        {
            m_dialogueNameData = dialogueName;
            m_dialogueChoiceData = choices;
            m_valueToSet = valueToSet;
            m_variableName = variableName;
            m_operationType = operationType;
            m_loveScoreAmount = loveScoreAmount;
            m_boolValue = boolValue;
            m_dialogueTypeData = dialogueType;
            m_isStartingDialogueData = isStartingDialogue;
        }
    }
}