using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextParser : EditorWindow
{
    private const string BaseFolder = "Assets/Dialogue/Parsed Dialogue";
    private const string SourceFolder = "Assets/Dialogue/Unparsed Dialogue";

    private List<CustomTextAsset> parsedAssets = new List<CustomTextAsset>();
    private Vector2 scrollPosition;
    private bool showContent = true;

    [MenuItem("Tools/Text Parser")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextParser>("Text Parser");
    }

    [System.Serializable]
    public class CustomTextAsset : ScriptableObject
    {
        public string Title;

        [TextArea(10, 20)]
        public string Content;
    }

    private void OnGUI()
    {
        GUILayout.Label("Text Parser", EditorStyles.boldLabel);

        if (GUILayout.Button("Parse Text File"))
        {
            string path = EditorUtility.OpenFilePanel("Select Text File", "", "txt");
            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                string fileContent = File.ReadAllText(path);
                CreateScriptableObject(fileName, fileContent);
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("Batch Processing", EditorStyles.boldLabel);

        if (GUILayout.Button("Parse All Files in 'Unparsed Dialogue' Folder"))
        {
            ParseAllFilesInFolder();
        }
    }

    private void CreateFolderIfNotExists(string path)
    {
        string[] folderParts = path.Split('/');
        string currentPath = folderParts[0]; // Should be "Assets"

        for (int i = 1; i < folderParts.Length; i++)
        {
            string folderName = folderParts[i];
            string newPath = $"{currentPath}/{folderName}";

            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, folderName);
            }

            currentPath = newPath;
        }
    }

    private void ParseAllFilesInFolder()
    {
        // Ensure the source folder exists
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            Debug.LogError($"Source folder '{SourceFolder}' does not exist. Creating it...");
            CreateFolderIfNotExists(SourceFolder);
        }

        // Get the full system path
        string fullPath = Path.Combine(Application.dataPath, SourceFolder.Substring(7));

        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"Directory not found even after creation attempt: {fullPath}");
            return;
        }

        // Get all text files
        string[] files = Directory.GetFiles(fullPath, "*.txt", SearchOption.AllDirectories);
        int successCount = 0;

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileContent = File.ReadAllText(filePath);

            CreateScriptableObject(fileName, fileContent);
            successCount++;
        }

        Debug.Log($"Batch processing complete. Processed {successCount} text files.");
    }

    private void CreateScriptableObject(string fileName, string fileContent)
    {
        // Extract first word for subfolder name
        string firstWord = fileName.Split(' ')[0];

        // Create folder path
        string folderPath = $"{BaseFolder}/{firstWord}";

        // Ensure base directories exist
        CreateFolderIfNotExists(BaseFolder);

        // Create asset folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = BaseFolder;
            string folderName = firstWord;
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        // Create the scriptable object
        CustomTextAsset textAsset = CreateInstance<CustomTextAsset>();
        textAsset.name = fileName;
        textAsset.Title = fileName;
        textAsset.Content = fileContent;

        // Save the asset
        string assetPath = $"{folderPath}/{fileName}.asset";
        AssetDatabase.CreateAsset(textAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created text asset at {assetPath}");

        // Select the created asset
        Selection.activeObject = textAsset;
    }
}
