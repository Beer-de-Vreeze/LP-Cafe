using UnityEditor;
using UnityEngine.UIElements;

namespace DS.Utilities
{
    //https://www.youtube.com/watch?v=gtZAN-vzuh4&list=PL0yxB6cCkoWK38XT4stSztcLueJ_kTx5f&index=17
    public static class DSStyleUtility
    {
        //Only use this when you need to add multiple classes to an element for readability.
        public static VisualElement AddClasses(this VisualElement element, params string[] classNames)
        {
            foreach(string className in classNames)
            {
                element.AddToClassList(className);
            }

            return element;
        }


        //For params to work it needs to be an array and needs to be the last variable.
        public static VisualElement AddStyleSheets(this VisualElement element, params string[] styleSheetsNames)
        {
            foreach (string styleSheetName in styleSheetsNames)
            {
                //Loads in the StyleSheets.
                StyleSheet styleSheet = (StyleSheet)EditorGUIUtility.Load(styleSheetName);

                //Graphview inherits from visual elements so you can just simply add a style sheet this way!
                element.styleSheets.Add(styleSheet);
            }

            return element;
        }
    }
}

