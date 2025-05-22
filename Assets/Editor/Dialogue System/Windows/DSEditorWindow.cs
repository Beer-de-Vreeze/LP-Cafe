using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LPCafe.Windows
{

    using Utilities;
    public class DSEditorWindow : EditorWindow
    {
        private DSGraphView m_graphView;

        private readonly string defaultFileName = "DialogueFileName";

        private TextField m_fileNameTextField;
        private Button m_saveButton;


        //To show within the window tab in Unity.
        [MenuItem("Window/LPCafe/DSEditorWindow")]
        public static void ShowExample()
        {
            //The title of the in editor window.
            DSEditorWindow wnd = GetWindow<DSEditorWindow>("Dialogue Graph");
        }

        private void CreateGUI()
        {
            AddGraphView();
            AddToolBar();

            AddStyles();
        }

        #region Elements Addition
        private void AddGraphView()
        {
            //Makes a new Graphview.
            //Make sure to style the graphview it's standard values are transparent.
            m_graphView = new DSGraphView(this);
            //Makes sure that the new Graphview is the same size as the parent (Standard size is 0!).
            m_graphView.StretchToParentSize();

            rootVisualElement.Add(m_graphView);
        }

        private void AddToolBar()
        {
            Toolbar toolbar = new Toolbar();

            m_fileNameTextField = DSElementUtility.CreateTextField(defaultFileName, "File Name:", callback =>
            {
                m_fileNameTextField.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();
            });

            m_saveButton = DSElementUtility.CreateButton("Save", () =>
            {
                Save();
            });

            toolbar.Add(m_fileNameTextField);
            toolbar.Add(m_saveButton);

            toolbar.AddStyleSheets("Assets/Editor/Editor Default Resources/DialogueSystemStyle/DSToolbarStyle.uss");

            rootVisualElement.Add(toolbar);
        }

        private void AddStyles()
        {
            rootVisualElement.AddStyleSheets("Assets/Editor/Editor Default Resources/DialogueSystemStyle/StyleVariables.uss");
        }
        #endregion

        #region Toolbar Actions
        private void Save()
        {
            if (string.IsNullOrEmpty(m_fileNameTextField.value))
            {
                EditorUtility.DisplayDialog(
                    "Invalid file Name",
                    "Please ensure the file name you've typed in is valid",
                    "Roger!");

                return;
            }

            DSIOUtility.Initialize(m_graphView, m_fileNameTextField.value);
            DSIOUtility.Save();
        }
        #endregion

        #region Saving
        public void EnableSaving()
        {
            m_saveButton.SetEnabled(true);
        }

        public void DisableSaving()
        {
            m_saveButton.SetEnabled(false);
        }
        #endregion
    }
}