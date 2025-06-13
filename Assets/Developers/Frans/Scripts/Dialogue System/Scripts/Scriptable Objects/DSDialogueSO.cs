using System.Collections.Generic;
using DS.Enumerations;
using NUnit.Framework;
using UnityEngine;

namespace DS.ScriptableObjects
{
    using System;
    using Data;
    using UnityEngine.UIElements;
    using static NewBachelorSO;

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
        public string m_propertyToCheckData { get; set; }

        [field: SerializeField]
        public string m_comparisonTypeData { get; set; }

        [field: SerializeField]
        public string m_comparisonValueData { get; set; }

        // New fields for setter nodes
        [field: SerializeField]
        public SetterOperationType m_operationTypeData { get; set; }

        [field: SerializeField]
        public string m_variableNameData { get; set; }

        [field: SerializeField]
        public string m_valueToSetData { get; set; }

        [field: SerializeField]
        public int m_loveScoreAmountData { get; set; }

        [field: SerializeField]
        public bool m_boolValueData { get; set; }

        [field: SerializeField]
        public LoveMeterSO m_loveMeterData { get; set; }
        [field: SerializeField]
        public NewBachelorSO m_bachelorData { get; set; }
        [field: SerializeField]
        public bool m_isLikePreferenceData { get; set; }
        [field: SerializeField]
        public string m_selectedPreferenceData { get; set; }
        public int m_enumSetterData { get; set; }

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
            m_propertyToCheckData = propertyToCheck;
            m_comparisonTypeData = comparisonType;
            m_comparisonValueData = comparisonValue;
            m_dialogueChoiceData = choices;
            m_dialogueTypeData = dialogueType;
            m_isStartingDialogueData = isStartingDialogue;
        }

        public void InitializeSetterNode(
            SetterOperationType operationType,
            string dialogueName,
            List<DSDialogueChoiceData> choices,
            string valueToSet,
            string variableName,
            int loveScoreAmount,
            bool boolValue,
            DSDialogueType dialogueType,
            LoveMeterSO loveMeterData,
            NewBachelorSO bachelorData,
            bool isLikePreferenceData,
            string selectedPreferenceData,
            int enumSetter,
            bool isStartingDialogue
        )
        {
            m_operationTypeData = operationType;
            m_dialogueNameData = dialogueName;
            m_dialogueChoiceData = choices;
            m_valueToSetData = valueToSet;
            m_variableNameData = variableName;
            m_loveScoreAmountData = loveScoreAmount;
            m_boolValueData = boolValue;
            m_dialogueTypeData = dialogueType;
            m_loveMeterData = loveMeterData;
            m_bachelorData = bachelorData;
            m_isLikePreferenceData = isLikePreferenceData;
            m_selectedPreferenceData = selectedPreferenceData;
            m_enumSetterData = enumSetter;
            m_isStartingDialogueData = isStartingDialogue;
        }
    }
}
