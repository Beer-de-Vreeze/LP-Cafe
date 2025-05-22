using System.Collections.Generic;
using UnityEditor;

namespace LPCafe.Inspectors
{
    using ScriptableObjects;
    using Utilities;

    /// <summary>
    /// Custom inspector for the DSDialogue component.
    /// Provides a specialized UI for selecting and configuring dialogues.
    /// </summary>
    [CustomEditor(typeof(DSDialogue))]
    public class DSInspector : Editor
    {
        /* Dialogue Scriptable Objects */
        // References to the dialogue container, group, and dialogue scriptable objects
        private SerializedProperty m_dialogueContainerProperty;
        private SerializedProperty m_dialogueGroupProperty;
        private SerializedProperty m_dialogueProperty;

        /* Filters */
        // Properties for filtering dialogues by group and starting status
        private SerializedProperty m_groupedDialoguesProperty;
        private SerializedProperty m_startingDialoguesOnlyProperty;

        /* Indexes */
        // Tracks the selected indices for dialogue groups and dialogues in dropdown menus
        private SerializedProperty m_selectedDialogueGroupIndexProperty;
        private SerializedProperty m_selectedDialogueIndexProperty;

        /// <summary>
        /// Called when the inspector becomes enabled.
        /// Initializes all serialized properties.
        /// </summary>
        private void OnEnable()
        {
            // Initialize dialogue object references
            m_dialogueContainerProperty = serializedObject.FindProperty("m_dialogueContainer");
            m_dialogueGroupProperty = serializedObject.FindProperty("m_dialogueGroup");
            m_dialogueProperty = serializedObject.FindProperty("m_dialogue");

            // Initialize filter properties
            m_groupedDialoguesProperty = serializedObject.FindProperty("m_groupedDialogues");
            m_startingDialoguesOnlyProperty = serializedObject.FindProperty(
                "m_startingDialoguesOnly"
            );

            // Initialize selection index properties
            m_selectedDialogueGroupIndexProperty = serializedObject.FindProperty(
                "m_selectedDialogueGroupIndex"
            );
            m_selectedDialogueIndexProperty = serializedObject.FindProperty(
                "m_selectedDialogueIndex"
            );
        }

        /// <summary>
        /// Draws the custom inspector GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw the dialogue container selection field
            DrawDialogueContainerArea();

            // Get the currently selected dialogue container
            DSDialogueContainerSO currentDialogueContainer = (DSDialogueContainerSO)
                m_dialogueContainerProperty.objectReferenceValue;

            // Stop drawing if no dialogue container is selected
            if (currentDialogueContainer == null)
            {
                StopDrawing("Select a Dialogue Container to see the rest of the Inspector.");

                return;
            }

            // Draw filter options (grouped dialogues, starting dialogues only)
            DrawFiltersArea();

            // Get current filter settings
            bool currentGroupedDialoguesFilter = m_groupedDialoguesProperty.boolValue;
            bool currentStartingDialoguesOnlyFilter = m_startingDialoguesOnlyProperty.boolValue;

            List<string> dialogueNames;

            string dialogueFolderPath =
                $"Assets/DialogueSystem/Dialogues/{currentDialogueContainer.name}";

            string dialogueInfoMessage;

            // Handle grouped dialogues mode
            if (currentGroupedDialoguesFilter)
            {
                // Get and display dialogue groups from the container
                List<string> dialogueGroupNames = currentDialogueContainer.GetDialogueGroupNames();

                // Stop if there are no dialogue groups
                if (dialogueGroupNames.Count == 0)
                {
                    StopDrawing("There are no Dialogue Groups in this Dialogue Container.");

                    return;
                }

                // Draw dialogue group selection area
                DrawDialogueGroupArea(currentDialogueContainer, dialogueGroupNames);

                // Get the selected dialogue group
                DSDialogueGroupSO dialogueGroup = (DSDialogueGroupSO)
                    m_dialogueGroupProperty.objectReferenceValue;

                // Get all dialogues in the selected group, filtered by starting status if needed
                dialogueNames = currentDialogueContainer.GetGroupedDialogueNames(
                    dialogueGroup,
                    currentStartingDialoguesOnlyFilter
                );

                // Set the folder path for the current group's dialogues
                dialogueFolderPath += $"/Groups/{dialogueGroup.name}/Dialogues";

                // Prepare info message for when no dialogues are available
                dialogueInfoMessage =
                    "There are no"
                    + (currentStartingDialoguesOnlyFilter ? " Starting" : "")
                    + " Dialogues in this Dialogue Group.";
            }
            // Handle ungrouped dialogues mode
            else
            {
                // Get global (ungrouped) dialogues, filtered by starting status if needed
                dialogueNames = currentDialogueContainer.GetUngroupedDialogueNames(
                    currentStartingDialoguesOnlyFilter
                );

                // Set the folder path for global dialogues
                dialogueFolderPath += "/Global/Dialogues";

                // Prepare info message for when no dialogues are available
                dialogueInfoMessage =
                    "There are no"
                    + (currentStartingDialoguesOnlyFilter ? " Starting" : "")
                    + " Ungrouped Dialogues in this Dialogue Container.";
            }

            // Stop if no dialogues are available to select
            if (dialogueNames.Count == 0)
            {
                StopDrawing(dialogueInfoMessage);

                return;
            }

            // Draw dialogue selection area
            DrawDialogueArea(dialogueNames, dialogueFolderPath);

            // Apply any changes made in the inspector
            serializedObject.ApplyModifiedProperties();
        }

        #region Drawing Methods

        /// <summary>
        /// Draws the dialogue container selection field with a header.
        /// </summary>
        private void DrawDialogueContainerArea()
        {
            DSInspectorUtility.DrawHeader("Dialogue Container");

            m_dialogueContainerProperty.DrawPropertyField();

            DSInspectorUtility.DrawSpace();
        }

        /// <summary>
        /// Draws the filter options for displaying dialogues.
        /// </summary>
        private void DrawFiltersArea()
        {
            DSInspectorUtility.DrawHeader("Filters");

            m_groupedDialoguesProperty.DrawPropertyField(); // Toggle for grouped/ungrouped dialogues
            m_startingDialoguesOnlyProperty.DrawPropertyField(); // Toggle for showing only starting dialogues

            DSInspectorUtility.DrawSpace();
        }

        /// <summary>
        /// Draws the dialogue group selection area.
        /// </summary>
        /// <param name="dialogueContainer">The current dialogue container</param>
        /// <param name="dialogueGroupNames">List of available dialogue group names</param>
        private void DrawDialogueGroupArea(
            DSDialogueContainerSO dialogueContainer,
            List<string> dialogueGroupNames
        )
        {
            DSInspectorUtility.DrawHeader("Dialogue Group");

            // Store current state before any changes
            int oldSelectedDialogueGroupIndex = m_selectedDialogueGroupIndexProperty.intValue;
            DSDialogueGroupSO oldDialogueGroup = (DSDialogueGroupSO)
                m_dialogueGroupProperty.objectReferenceValue;
            bool isOldDialogueGroupNull = oldDialogueGroup == null;
            string oldDialogueGroupName = isOldDialogueGroupNull ? "" : oldDialogueGroup.name;

            // Update index if the list of groups has changed
            UpdateIndexOnNamesListUpdate(
                dialogueGroupNames,
                m_selectedDialogueGroupIndexProperty,
                oldSelectedDialogueGroupIndex,
                oldDialogueGroupName,
                isOldDialogueGroupNull
            );

            // Draw dropdown for selecting dialogue group
            m_selectedDialogueGroupIndexProperty.intValue = DSInspectorUtility.DrawPopup(
                "Dialogue Group",
                m_selectedDialogueGroupIndexProperty,
                dialogueGroupNames.ToArray()
            );

            // Get the selected dialogue group name and load the corresponding asset
            string selectedDialogueGroupName = dialogueGroupNames[
                m_selectedDialogueGroupIndexProperty.intValue
            ];
            DSDialogueGroupSO selectedDialogueGroup = DSIOUtility.LoadAsset<DSDialogueGroupSO>(
                $"Assets/DialogueSystem/Dialogues/{dialogueContainer.name}/Groups/{selectedDialogueGroupName}",
                selectedDialogueGroupName
            );

            // Update the dialogue group property
            m_dialogueGroupProperty.objectReferenceValue = selectedDialogueGroup;

            // Draw the dialogue group field (disabled for display only)
            DSInspectorUtility.DrawDisabledFields(
                () => m_dialogueGroupProperty.DrawPropertyField()
            );

            DSInspectorUtility.DrawSpace();
        }

        /// <summary>
        /// Draws the dialogue selection area.
        /// </summary>
        /// <param name="dialogueNames">List of available dialogue names</param>
        /// <param name="dialogueFolderPath">Path to the folder containing the dialogues</param>
        private void DrawDialogueArea(List<string> dialogueNames, string dialogueFolderPath)
        {
            DSInspectorUtility.DrawHeader("Dialogue");

            // Store current state before any changes
            int oldSelectedDialogueIndex = m_selectedDialogueIndexProperty.intValue;
            DSDialogueSO oldDialogue = (DSDialogueSO)m_dialogueProperty.objectReferenceValue;
            bool isOldDialogueNull = oldDialogue == null;
            string oldDialogueName = isOldDialogueNull ? "" : oldDialogue.name;

            // Update index if the list of dialogues has changed
            UpdateIndexOnNamesListUpdate(
                dialogueNames,
                m_selectedDialogueIndexProperty,
                oldSelectedDialogueIndex,
                oldDialogueName,
                isOldDialogueNull
            );

            // Draw dropdown for selecting dialogue
            m_selectedDialogueIndexProperty.intValue = DSInspectorUtility.DrawPopup(
                "Dialogue",
                m_selectedDialogueIndexProperty,
                dialogueNames.ToArray()
            );

            // Get the selected dialogue name and load the corresponding asset
            string selectedDialogueName = dialogueNames[m_selectedDialogueIndexProperty.intValue];
            DSDialogueSO selectedDialogue = DSIOUtility.LoadAsset<DSDialogueSO>(
                dialogueFolderPath,
                selectedDialogueName
            );

            // Update the dialogue property
            m_dialogueProperty.objectReferenceValue = selectedDialogue;

            // Draw the dialogue field (disabled for display only)
            DSInspectorUtility.DrawDisabledFields(() => m_dialogueProperty.DrawPropertyField());
        }

        /// <summary>
        /// Stops drawing the inspector and displays a message to the user.
        /// </summary>
        /// <param name="reason">Message explaining why drawing stopped</param>
        /// <param name="messageType">Type of message to display</param>
        private void StopDrawing(string reason, MessageType messageType = MessageType.Info)
        {
            // Display the reason for stopping
            DSInspectorUtility.DrawHelpBox(reason, messageType);
            DSInspectorUtility.DrawSpace();

            // Display a warning about needing a dialogue for runtime functionality
            DSInspectorUtility.DrawHelpBox(
                "You need to select a Dialogue for this component to work properly at Runtime!",
                MessageType.Warning
            );

            // Apply any changes made before stopping
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Index Update Methods

        /// <summary>
        /// Updates the selected index when the list of options changes.
        /// Maintains selection when possible or resets to a valid option.
        /// </summary>
        /// <param name="optionNames">List of current option names</param>
        /// <param name="indexProperty">The index property to update</param>
        /// <param name="oldSelectedPropertyIndex">Previous selected index</param>
        /// <param name="oldPropertyName">Previous selected name</param>
        /// <param name="isOldPropertyNull">Whether the previous selection was null</param>
        private void UpdateIndexOnNamesListUpdate(
            List<string> optionNames,
            SerializedProperty indexProperty,
            int oldSelectedPropertyIndex,
            string oldPropertyName,
            bool isOldPropertyNull
        )
        {
            // If the previous selection was null, select the first option
            if (isOldPropertyNull)
            {
                indexProperty.intValue = 0;

                return;
            }

            // Check if the old index is still valid and the name hasn't changed
            bool oldIndexIsOutOfBoundsOfNamesListCount =
                oldSelectedPropertyIndex > optionNames.Count - 1;
            bool oldNameIsDifferentThanSelectedName =
                oldIndexIsOutOfBoundsOfNamesListCount
                || oldPropertyName != optionNames[oldSelectedPropertyIndex];

            // If the selection is no longer valid, try to find the old name in the new list
            if (oldNameIsDifferentThanSelectedName)
            {
                if (optionNames.Contains(oldPropertyName))
                {
                    // If the old name still exists, select it
                    indexProperty.intValue = optionNames.IndexOf(oldPropertyName);

                    return;
                }

                // If the old name doesn't exist, select the first option
                indexProperty.intValue = 0;
            }
        }
    }
        #endregion
}
