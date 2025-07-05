using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool to help with debugging and managing save files
/// </summary>
public class SaveSystemDebugger : EditorWindow
{
    private Vector2 scrollPosition;
    private SaveData currentSaveData;
    private string saveFilePath;
    private string saveFileContent = "";
    private bool showRawJson = false;
    private bool showLegacyData = false;
    private bool showDetailedBachelorData = false;

    [MenuItem("Tools/Save System Debugger")]
    public static void ShowWindow()
    {
        GetWindow<SaveSystemDebugger>("Save Debugger");
    }

    private void OnEnable()
    {
        RefreshSaveData();
    }

    private void OnGUI()
    {
        GUILayout.Label("Save System Debugger", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(100)))
        {
            RefreshSaveData();
        }

        if (GUILayout.Button("Reset Save File", GUILayout.Width(120)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "Reset Save File",
                    "This will completely reset the save file. Are you sure?",
                    "Yes, Reset",
                    "Cancel"
                )
            )
            {
                SaveSystem.ResetSaveFile();
                RefreshSaveData();
            }
        }

        if (GUILayout.Button("Open Save Location", GUILayout.Width(150)))
        {
            OpenSaveFileLocation();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (currentSaveData == null)
        {
            EditorGUILayout.HelpBox(
                "No save data found or unable to load save file.",
                MessageType.Info
            );
        }
        else
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Toggle for raw JSON view
            showRawJson = EditorGUILayout.Toggle("Show Raw JSON", showRawJson);
            showLegacyData = EditorGUILayout.Toggle("Show Legacy Data", showLegacyData);
            showDetailedBachelorData = EditorGUILayout.Toggle(
                "Show Detailed Bachelor Data",
                showDetailedBachelorData
            );

            if (showRawJson)
            {
                // Show the raw JSON content
                EditorGUILayout.LabelField("Raw Save File Content:");
                EditorGUILayout.TextArea(saveFileContent, GUILayout.Height(300));
            }
            else
            {
                // Show formatted data
                EditorGUILayout.LabelField("Save File Location:", saveFilePath);

                // Show "Reset Bachelors" flag
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Reset Flags:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"  Should Reset Bachelors: {currentSaveData.ShouldResetBachelors}"
                );

                // Bachelor Dating Status Summary
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bachelor Dating Status:", EditorStyles.boldLabel);

                // Get lists from the new format
                var speedDatedBachelors = currentSaveData.GetAllSpeedDatedBachelors();
                var realDatedBachelors = currentSaveData.GetAllRealDatedBachelors();

                EditorGUILayout.LabelField($"  Speed Dated Count: {speedDatedBachelors.Count}");
                EditorGUILayout.LabelField($"  Real Dated Count: {realDatedBachelors.Count}");

                // Bachelor Preferences
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bachelor Preferences:", EditorStyles.boldLabel);

                if (
                    currentSaveData.BachelorPreferences == null
                    || currentSaveData.BachelorPreferences.Count == 0
                )
                {
                    EditorGUILayout.LabelField("  None");
                }
                else
                {
                    foreach (var bachelor in currentSaveData.BachelorPreferences)
                    {
                        // Basic info
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  • {bachelor.bachelorName}");

                        // Status indicators
                        if (bachelor.hasBeenSpeedDated)
                        {
                            EditorGUILayout.LabelField(
                                "[Speed Dated]",
                                EditorStyles.boldLabel,
                                GUILayout.Width(100)
                            );
                        }

                        if (bachelor.hasCompletedRealDate)
                        {
                            EditorGUILayout.LabelField(
                                "[Real Dated]",
                                EditorStyles.boldLabel,
                                GUILayout.Width(100)
                            );
                            if (!string.IsNullOrEmpty(bachelor.lastRealDateLocation))
                            {
                                EditorGUILayout.LabelField(
                                    $"@ {bachelor.lastRealDateLocation}",
                                    GUILayout.Width(150)
                                );
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        // Show detailed information if requested
                        if (showDetailedBachelorData)
                        {
                            EditorGUI.indentLevel++;

                            // Likes
                            EditorGUILayout.LabelField("Likes:", EditorStyles.miniBoldLabel);
                            if (
                                bachelor.discoveredLikes == null
                                || bachelor.discoveredLikes.Count == 0
                            )
                            {
                                EditorGUILayout.LabelField("    None discovered");
                            }
                            else
                            {
                                foreach (var like in bachelor.discoveredLikes)
                                {
                                    EditorGUILayout.LabelField($"    ✓ {like}");
                                }
                            }

                            // Dislikes
                            EditorGUILayout.LabelField("Dislikes:", EditorStyles.miniBoldLabel);
                            if (
                                bachelor.discoveredDislikes == null
                                || bachelor.discoveredDislikes.Count == 0
                            )
                            {
                                EditorGUILayout.LabelField("    None discovered");
                            }
                            else
                            {
                                foreach (var dislike in bachelor.discoveredDislikes)
                                {
                                    EditorGUILayout.LabelField($"    ✗ {dislike}");
                                }
                            }

                            EditorGUI.indentLevel--;
                        }
                    }
                }

                // Legacy data (if enabled)
                if (showLegacyData)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Legacy Data (Deprecated):", EditorStyles.boldLabel);

                    // Dated Bachelors (Legacy)
                    EditorGUILayout.LabelField("Legacy DatedBachelors:", EditorStyles.miniLabel);
                    if (
                        currentSaveData.DatedBachelors == null
                        || currentSaveData.DatedBachelors.Count == 0
                    )
                    {
                        EditorGUILayout.LabelField("  None");
                    }
                    else
                    {
                        foreach (var bachelor in currentSaveData.DatedBachelors)
                        {
                            EditorGUILayout.LabelField($"  • {bachelor}");
                        }
                    }

                    // Real Dated Bachelors (Legacy)
                    EditorGUILayout.LabelField(
                        "Legacy RealDatedBachelors:",
                        EditorStyles.miniLabel
                    );
                    if (
                        currentSaveData.RealDatedBachelors == null
                        || currentSaveData.RealDatedBachelors.Count == 0
                    )
                    {
                        EditorGUILayout.LabelField("  None");
                    }
                    else
                    {
                        foreach (var bachelor in currentSaveData.RealDatedBachelors)
                        {
                            EditorGUILayout.LabelField($"  • {bachelor}");
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void RefreshSaveData()
    {
        currentSaveData = SaveSystem.Deserialize();
        saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");

        if (File.Exists(saveFilePath))
        {
            try
            {
                saveFileContent = File.ReadAllText(saveFilePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error reading save file: {ex.Message}");
                saveFileContent = "Error reading save file.";
            }
        }
        else
        {
            saveFileContent = "No save file found.";
        }
    }

    private void OpenSaveFileLocation()
    {
        string folderPath = Application.persistentDataPath;
        if (Directory.Exists(folderPath))
        {
            EditorUtility.RevealInFinder(folderPath);
        }
        else
        {
            Debug.LogError($"Save folder does not exist: {folderPath}");
        }
    }
}
