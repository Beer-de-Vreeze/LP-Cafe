using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Windows
{
    using System;
    using Utilities;

    public class DSEditorWindow : EditorWindow
    {
        private DSGraphView m_graphView;

        private readonly string defaultFileName = "DialogueFileName";

        private static TextField m_fileNameTextField;
        private Button m_saveButton;
        private Button m_miniMapButton;

        //To show within the window tab in Unity.
        [MenuItem("Window/DS/DSEditorWindow")]
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

            m_fileNameTextField = DSElementUtility.CreateTextField(
                defaultFileName,
                "File Name:",
                callback =>
                {
                    m_fileNameTextField.value = callback
                        .newValue.RemoveWhitespaces()
                        .RemoveSpecialCharacters();
                }
            );

            m_saveButton = DSElementUtility.CreateButton(
                "Save",
                () =>
                {
                    Save();
                }
            );

            Button loadedButton = DSElementUtility.CreateButton("LoadGraph", () => LoadGraph());
            Button clearButton = DSElementUtility.CreateButton("Clear", () => Clear());
            Button resetButton = DSElementUtility.CreateButton("Reset", () => Reset());
            m_miniMapButton = DSElementUtility.CreateButton("Minimap", () => ToggleMiniMap());

            toolbar.Add(m_fileNameTextField);
            toolbar.Add(m_saveButton);
            toolbar.Add(loadedButton);
            toolbar.Add(clearButton);
            toolbar.Add(resetButton);
            toolbar.Add(m_miniMapButton);

            toolbar.AddStyleSheets(
                "Assets/Editor/Editor Default Resources/DialogueSystemStyle/DSToolbarStyle.uss"
            );

            rootVisualElement.Add(toolbar);
        }

        private void ToggleMiniMap()
        {
            m_graphView.ToggleMiniMap();
            m_miniMapButton.ToggleInClassList("ds-toolbar__button__selected");
        }

        private void AddStyles()
        {
            rootVisualElement.AddStyleSheets(
                "Assets/Editor/Editor Default Resources/DialogueSystemStyle/StyleVariables.uss"
            );
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
                    "Roger!"
                );

                return;
            }

            DSIOUtility.Initialize(m_graphView, m_fileNameTextField.value);
            DSIOUtility.Save();
        }

        private void LoadGraph()
        {
            string filePath = EditorUtility.OpenFilePanel(
                "Dialogue Graphs",
                "Assets/Developers/Frans/Graphs",
                "asset"
            );

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Clear();

            DSIOUtility.Initialize(m_graphView, Path.GetFileNameWithoutExtension(filePath));
            DSIOUtility.Load();
        }

        private void Clear()
        {
            if(m_graphView != null)
            {
                m_graphView.ClearGraph();
            }
        }

        private void Reset()
        {
            Clear();

            UpdateFileName(defaultFileName);
        }
        #endregion

        #region Utility Methods
        public static void UpdateFileName(string newFileName)
        {
            m_fileNameTextField.value = newFileName;
        }

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
        #endregion
    }
}
