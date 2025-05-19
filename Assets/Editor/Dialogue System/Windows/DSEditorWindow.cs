using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LPCafe.Windows
{
    using Utilities;
    public class DSEditorWindow : EditorWindow
    {
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

            AddStyles();
        }

        #region Elements Addition
        private void AddGraphView()
        {
            //Makes a new Graphview.
            //Make sure to style the graphview it's standard values are transparent.
            DSGraphView graphView = new DSGraphView(this);
            //Makes sure that the new Graphview is the same size as the parent (Standard size is 0!).
            graphView.StretchToParentSize();

            rootVisualElement.Add(graphView);
        }

        private void AddStyles()
        {
            rootVisualElement.AddStyleSheets("Assets/Editor/Editor Default Resources/DialogueSystemStyle/StyleVariables.uss");
        }
        #endregion
    }
}