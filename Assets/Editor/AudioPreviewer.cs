using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Unity editor window for previewing audio clips in the project.
/// Provides functionality to scan for audio files, organize by folders,
/// play/stop clips, visualize waveforms, and more.
/// </summary>
public class AudioPreviewerWindow : EditorWindow
{
    /// <summary>
    /// Internal class to store data related to each audio clip
    /// including references, playback state, and visualization.
    /// </summary>
    private class ClipData
    {
        public AudioClip clip;      // Reference to the audio clip
        public bool loop;           // Whether the clip should loop when playing
        public bool isPlaying;      // Current playback state
        public string folderPath;   // Path to folder containing the clip
        public Texture2D waveformTexture;  // Generated waveform visualization
    }

    /// <summary>
    /// Settings for generating waveform visualizations of audio clips.
    /// </summary>
    private class WaveformSettings
    {
        public Color colorStart = new Color(0.2f, 0.6f, 1f, 1f);     // Color at start of waveform
        public Color colorMiddle = new Color(0.4f, 0.9f, 0.4f, 1f);  // Color at middle of waveform
        public Color colorEnd = new Color(1f, 0.6f, 0.2f, 1f);       // Color at end of waveform

        public int width = 256;   // Width of the waveform texture
        public int height = 64;   // Height of the waveform texture
    }

    private WaveformSettings waveformSettings = new WaveformSettings();  // Default waveform settings
    private Dictionary<AudioClip, Texture2D> waveformCache = new Dictionary<AudioClip, Texture2D>();  // Cache for generated waveforms
    private Dictionary<AudioClip, float[]> clipSampleCache = new Dictionary<AudioClip, float[]>();    // Cache for audio samples

    /// <summary>
    /// Represents a group of audio clips from the same folder.
    /// Used for organizing clips in the UI.
    /// </summary>
    private class FolderGroup
    {
        public string folderPath;       // Path to the folder
        public string displayName;      // Display name shown in the UI
        public bool expanded = true;    // Whether the folder group is expanded in UI
        public List<ClipData> clips = new List<ClipData>();  // Clips contained in this folder
    }

    private List<ClipData> clipDataList = new();         // Main list of all audio clips
    private List<FolderGroup> folderGroups = new();      // Organized groups of clips by folder
    private Vector2 scrollPos;                          // Scroll position for the scroll view
    private string searchFilter = "";                   // Current search term
    private AudioClip currentlyPlayingClip;             // Reference to currently playing clip
    private float playbackStartTime;                    // Time when playback started (for progress tracking)
    private bool autoLoadFromProject = true;            // Whether to auto-load clips from project

    // EditorPrefs keys for saving/loading preferences
    private const string PrefsKeyPrefix = "AudioPreviewer_";
    private const string PrefsKeyAutoLoadFromProject = PrefsKeyPrefix + "AutoLoadFromProject";

    /// <summary>
    /// Creates and shows the Audio Previewer window.
    /// </summary>
    [MenuItem("Tools/Audio Previewer")]
    public static void ShowWindow()
    {
        GetWindow<AudioPreviewerWindow>("Audio Previewer");
    }

    /// <summary>
    /// Called when the window is enabled. Loads preferences and initializes data.
    /// </summary>
    private void OnEnable()
    {
        // Load user preferences
        autoLoadFromProject = EditorPrefs.GetBool(PrefsKeyAutoLoadFromProject, true);
        LoadSavedClips();

        // Auto-scan the project if enabled and no clips are loaded
        if (autoLoadFromProject && clipDataList.Count == 0)
        {
            ScanProjectForAudioClips();
        }

        OrganizeClipsByFolder();
    }

    /// <summary>
    /// Called when the window is disabled. Saves preferences and clip data.
    /// </summary>
    private void OnDisable()
    {
        EditorPrefs.SetBool(PrefsKeyAutoLoadFromProject, autoLoadFromProject);
        SaveClips();
    }

    /// <summary>
    /// Renders the editor window GUI.
    /// </summary>
    private void OnGUI()
    {
        // Window title
        GUILayout.Label("🎧 Audio Clip Previewer", EditorStyles.boldLabel);

        // Search box
        EditorGUILayout.BeginHorizontal();
        GUI.SetNextControlName("SearchField");

        float clearButtonWidth = Mathf.Max(50, position.width * 0.08f);
        string newSearchFilter = EditorGUILayout.TextField(
            "Search:",
            searchFilter,
            EditorStyles.toolbarSearchField
        );

        // Apply search filter if changed
        if (newSearchFilter != searchFilter)
        {
            searchFilter = newSearchFilter;
            OrganizeClipsByFolder();
        }

        // Clear search button
        if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(clearButtonWidth)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
            OrganizeClipsByFolder();
        }

        EditorGUILayout.EndHorizontal();

        // Scan project button and auto-scan toggle
        EditorGUILayout.BeginHorizontal();
        float buttonWidth = Mathf.Max(120, position.width * 0.3f);

        if (GUILayout.Button("Scan Project", GUILayout.Height(30), GUILayout.Width(buttonWidth)))
        {
            ScanProjectForAudioClips();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField("Auto-scan:", GUILayout.Width(60));
        autoLoadFromProject = EditorGUILayout.Toggle(autoLoadFromProject, GUILayout.Width(20));

        EditorGUILayout.EndHorizontal();

        // Stop all playback button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        float stopButtonWidth = Mathf.Max(80, position.width * 0.1f);
        if (GUILayout.Button("Stop All", GUILayout.Width(stopButtonWidth)))
        {
            StopAllClips();
            currentlyPlayingClip = null;
            foreach (var data in clipDataList.ToList())
            {
                data.isPlaying = false;
            }
        }

        EditorGUILayout.EndHorizontal();

        // Display clip count
        int visibleCount = folderGroups.Sum(g => g.clips.Count);
        int totalCount = clipDataList.Count;
        EditorGUILayout.LabelField($"Showing {visibleCount} of {totalCount} clips");

        // Begin scrollable area for clip display
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Draw each folder group
        List<FolderGroup> folderGroupsCopy = new List<FolderGroup>(folderGroups);
        foreach (var folderGroup in folderGroupsCopy)
        {
            if (folderGroup.clips.Count == 0)
                continue;

            // Folder header with light background
            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.9f);
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;
            GUILayout.Space(2);

            // Foldout control for expanding/collapsing folder
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            folderGroup.expanded = EditorGUILayout.Foldout(
                folderGroup.expanded,
                $"📁 {folderGroup.displayName}",
                foldoutStyle
            );

            // Display clip count in folder
            GUILayout.FlexibleSpace();
            GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel);
            countStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            GUILayout.Label($"({folderGroup.clips.Count} clips)", countStyle);
            GUILayout.Space(5);

            // Remove folder button
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            float removeButtonWidth = Mathf.Max(75, position.width * 0.1f);
            if (
                GUILayout.Button(
                    new GUIContent("🗑", "Remove all clips in this folder"),
                    GUILayout.Width(removeButtonWidth),
                    GUILayout.Height(18)
                )
            )
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Remove Folder",
                        $"Are you sure you want to remove all clips from \"{folderGroup.displayName}\"?",
                        "Yes",
                        "Cancel"
                    )
                )
                {
                    RemoveFolder(folderGroup);
                }
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();

            // If folder is expanded, show its clips
            if (folderGroup.expanded)
            {
                GUI.backgroundColor = new Color(0.97f, 0.97f, 0.97f);
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;
                EditorGUI.indentLevel++;

                List<ClipData> clipsCopy = new List<ClipData>(folderGroup.clips);
                foreach (var data in clipsCopy)
                {
                    if (data.clip == null)
                        continue;
                    DrawClipEntry(data);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndScrollView();

        // Repaint window while clip is playing to update progress
        if (currentlyPlayingClip != null)
        {
            Repaint();
        }
    }

    /// <summary>
    /// Scans the project for all audio clips and adds them to the list.
    /// </summary>
    private void ScanProjectForAudioClips()
    {
        // Find all audio clip GUIDs in the project
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            // Add clip if it's valid and not already in the list
            if (clip != null && !clipDataList.Any(c => c.clip == clip))
            {
                string folderPath = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');

                // Try to find existing saved data for this clip
                ClipData existingData = FindSavedClipData(clip);
                if (existingData != null)
                {
                    existingData.folderPath = folderPath;
                    clipDataList.Add(existingData);
                }
                else
                {
                    clipDataList.Add(new ClipData { clip = clip, folderPath = folderPath });
                }
            }
        }

        // Remove any null clips
        clipDataList = clipDataList.Where(c => c.clip != null).ToList();

        // Reorganize clips by folder
        OrganizeClipsByFolder();
    }

    /// <summary>
    /// Finds saved clip data for a specific clip.
    /// </summary>
    /// <param name="clip">The audio clip to find saved data for.</param>
    /// <returns>Saved clip data or null if none exists.</returns>
    private ClipData FindSavedClipData(AudioClip clip)
    {
        return null;
    }

    /// <summary>
    /// Organizes clips into folder groups based on their folder paths.
    /// Also applies current search filter.
    /// </summary>
    private void OrganizeClipsByFolder()
    {
        folderGroups.Clear();

        // Get filtered list of clips (apply search filter)
        var filteredClips = GetFilteredClips().Where(c => c.clip != null).ToList();
        var clipsByFolder = filteredClips.GroupBy(c => c.folderPath);

        foreach (var group in clipsByFolder)
        {
            string folderPath = group.Key;
            string displayName = folderPath;

            // Format display name
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (folderPath.StartsWith("Assets/"))
                {
                    displayName = System.IO.Path.GetFileName(folderPath);
                }
                else
                {
                    displayName = "No Folder";
                }
            }

            // Create folder group and add clips
            FolderGroup folderGroup = new FolderGroup
            {
                folderPath = folderPath,
                displayName = displayName,
                clips = group.Where(c => c.clip != null).OrderBy(c => c.clip.name).ToList(),
            };
            folderGroups.Add(folderGroup);
        }

        // Sort folders alphabetically
        folderGroups = folderGroups.OrderBy(g => g.displayName).ToList();
    }

    /// <summary>
    /// Returns filtered clips based on the current search filter.
    /// </summary>
    /// <returns>Filtered collection of clip data.</returns>
    private IEnumerable<ClipData> GetFilteredClips()
    {
        // If no search filter, return all clips
        // Otherwise, filter clips by name or folder path
        var filtered = string.IsNullOrWhiteSpace(searchFilter)
            ? clipDataList
            : clipDataList.Where(c =>
                c.clip != null
                && (
                    c.clip.name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                    || c.folderPath.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                )
            );

        return filtered;
    }

    /// <summary>
    /// Draws a single audio clip entry in the UI.
    /// </summary>
    /// <param name="data">The clip data to draw.</param>
    private void DrawClipEntry(ClipData data)
    {
        Rect entryRect = EditorGUILayout.BeginVertical("box");
        Event evt = Event.current;
        HandleClipInteractions(evt, entryRect, data);

        EditorGUILayout.BeginHorizontal();

        // Generate or display waveform visualization
        if (data.waveformTexture == null)
        {
            data.waveformTexture = GetWaveformTexture(data.clip);

            // Draw waveform in a dark box
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.1f);
            EditorGUILayout.BeginVertical(
                "box",
                GUILayout.Width(waveformSettings.width + 8),
                GUILayout.Height(waveformSettings.height + 8)
            );
            GUI.backgroundColor = Color.white;
            GUILayout.Label(
                data.waveformTexture,
                GUILayout.Width(waveformSettings.width),
                GUILayout.Height(waveformSettings.height)
            );
            EditorGUILayout.EndVertical();
        }
        else
        {
            // Draw cached waveform
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.1f);
            EditorGUILayout.BeginVertical(
                "box",
                GUILayout.Width(waveformSettings.width + 8),
                GUILayout.Height(waveformSettings.height + 8)
            );
            GUI.backgroundColor = Color.white;
            GUILayout.Label(
                data.waveformTexture,
                GUILayout.Width(waveformSettings.width),
                GUILayout.Height(waveformSettings.height)
            );
            EditorGUILayout.EndVertical();
        }

        float remainingWidth = position.width - waveformSettings.width - 40;

        // Clip info section
        EditorGUILayout.BeginVertical(GUILayout.Width(remainingWidth));
        EditorGUILayout.BeginHorizontal();

        // Show play indicator if clip is currently playing
        if (data.isPlaying)
        {
            GUIStyle playingStyle = new GUIStyle(EditorStyles.label);
            playingStyle.normal.textColor = new Color(0.0f, 0.8f, 0.0f);
            playingStyle.fontStyle = FontStyle.Bold;

            GUI.backgroundColor = new Color(0.0f, 0.6f, 0.0f, 0.2f);
            EditorGUILayout.BeginHorizontal("box", GUILayout.Width(30));
            GUI.backgroundColor = Color.white;
            GUILayout.Label("▶️", playingStyle, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Space(30);
        }

        // Clip name with styling
        GUIStyle clipNameStyle = new GUIStyle(EditorStyles.boldLabel);
        clipNameStyle.fontSize += 1;
        clipNameStyle.normal.textColor = new Color(0.1f, 0.4f, 0.7f);
        GUILayout.Label(data.clip.name, clipNameStyle);

        EditorGUILayout.EndHorizontal();

        // Help text for drag and drop
        GUIStyle tooltipStyle = new GUIStyle(EditorStyles.miniLabel);
        tooltipStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        tooltipStyle.fontSize -= 1;
        GUILayout.Space(2);
        GUILayout.Label(
            "Drag clip to Inspector fields or double-click to locate in Project",
            tooltipStyle
        );

        // Clip metadata (duration, channels, frequency)
        float duration = data.clip.length;
        string durationText = FormatTime(duration);
        EditorGUILayout.LabelField(
            $"Duration: {durationText} | Channels: {data.clip.channels} | Frequency: {data.clip.frequency}Hz"
        );

        // Playback progress bar for currently playing clip
        if (data.isPlaying)
        {
            float currentTime = GetCurrentPlaybackTime();
            float normalizedProgress = duration > 0 ? Mathf.Clamp01(currentTime / duration) : 0;
            float displayTime = Mathf.Round(currentTime * 10f) / 10f;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(FormatTime(currentTime), GUILayout.Width(40));
            
            // Position slider - allows seeking in the audio
            float newTimePosition = EditorGUILayout.Slider(
                displayTime,
                0f,
                duration,
                GUILayout.Height(15)
            );
            
            // Handle seeking if slider was changed
            if (Math.Abs(newTimePosition - displayTime) > 0.001f)
            {
                SeekToTime(data.clip, newTimePosition);
                playbackStartTime = (float)EditorApplication.timeSinceStartup - newTimePosition;
            }
            
            EditorGUILayout.LabelField(durationText, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }

        float playStopButtonWidth = Mathf.Max(60, position.width * 0.08f);

        // Play and stop buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶️ Play", GUILayout.Width(playStopButtonWidth)))
        {
            // Stop any currently playing clips
            StopAllClips();
            foreach (var clip in clipDataList.ToList())
            {
                clip.isPlaying = false;
            }
            
            // Play this clip
            PlayClip(data.clip, data.loop);
            data.isPlaying = true;
            currentlyPlayingClip = data.clip;
            playbackStartTime = (float)EditorApplication.timeSinceStartup;
        }

        GUILayout.Space(5);

        if (GUILayout.Button("⏹️ Stop", GUILayout.Width(playStopButtonWidth)))
        {
            StopAllClips();
            data.isPlaying = false;
            currentlyPlayingClip = null;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Loop toggle
        bool oldLoopValue = data.loop;
        data.loop = EditorGUILayout.Toggle("Loop", data.loop);
        if (data.isPlaying && oldLoopValue != data.loop)
        {
            SetLooping(data.clip, data.loop);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void HandleClipInteractions(Event evt, Rect entryRect, ClipData data)
    {
        switch (evt.type)
        {
            case EventType.MouseDown:
                if (entryRect.Contains(evt.mousePosition))
                {
                    if (evt.clickCount == 2)
                    {
                        EditorGUIUtility.PingObject(data.clip);
                        Selection.activeObject = data.clip;
                        evt.Use();
                    }
                }
                break;
            case EventType.MouseDrag:
                if (entryRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { data.clip };
                    DragAndDrop.StartDrag("Dragging Audio Clip");
                    evt.Use();
                }
                break;
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{minutes}:{seconds:00}";
    }

    private void SaveClips()
    {
        int count = clipDataList.Count;
        EditorPrefs.SetInt(PrefsKeyPrefix + "Count", count);

        for (int i = 0; i < count; i++)
        {
            var data = clipDataList[i];
            if (data.clip != null)
            {
                string path = AssetDatabase.GetAssetPath(data.clip);
                EditorPrefs.SetString(PrefsKeyPrefix + i + "_Path", path);
                EditorPrefs.SetBool(PrefsKeyPrefix + i + "_Loop", data.loop);
                EditorPrefs.SetString(PrefsKeyPrefix + i + "_FolderPath", data.folderPath);
            }
        }
    }

    private void LoadSavedClips()
    {
        clipDataList.Clear();
        int count = EditorPrefs.GetInt(PrefsKeyPrefix + "Count", 0);

        for (int i = 0; i < count; i++)
        {
            string path = EditorPrefs.GetString(PrefsKeyPrefix + i + "_Path", "");
            if (!string.IsNullOrEmpty(path))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clipDataList.Add(
                        new ClipData
                        {
                            clip = clip,
                            loop = EditorPrefs.GetBool(PrefsKeyPrefix + i + "_Loop", false),
                            folderPath = EditorPrefs.GetString(
                                PrefsKeyPrefix + i + "_FolderPath",
                                ""
                            ),
                        }
                    );
                }
            }
        }

        OrganizeClipsByFolder();
    }

    private static readonly Type audioUtilType = typeof(AudioImporter).Assembly.GetType(
        "UnityEditor.AudioUtil"
    );

    private void PlayClip(AudioClip clip, bool loop)
    {
        try
        {
            StopAllClips();
            var method = audioUtilType.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );

            if (method == null)
            {
                Debug.LogError("Could not find PlayPreviewClip method");
                return;
            }

            var parameters = method.GetParameters();
            if (parameters.Length >= 3)
            {
                try
                {
                    method.Invoke(null, new object[] { clip, 100, loop });
                }
                catch
                {
                    try
                    {
                        method.Invoke(null, new object[] { clip, 1.0f, loop });
                    }
                    catch
                    {
                        method.Invoke(null, new object[] { clip, loop, 1.0f });
                    }
                }
            }
            else if (parameters.Length == 2)
            {
                method.Invoke(null, new object[] { clip, loop });
            }
            else
            {
                Debug.LogError(
                    $"PlayPreviewClip has unexpected number of parameters: {parameters.Length}"
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error playing audio clip: {e.Message}");
        }
    }

    private float GetCurrentPlaybackTime()
    {
        float currentTime = 0;

        if (currentlyPlayingClip != null)
        {
            currentTime = (float)EditorApplication.timeSinceStartup - playbackStartTime;
            ClipData playingData = clipDataList.FirstOrDefault(c => c.clip == currentlyPlayingClip);
            if (playingData != null && !playingData.loop)
            {
                currentTime = Mathf.Min(currentTime, currentlyPlayingClip.length);
            }
        }

        return currentTime;
    }

    private void SeekToTime(AudioClip clip, float timeInSeconds)
    {
        try
        {
            var method = audioUtilType.GetMethod(
                "SetPreviewClipSamplePosition",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );

            if (method == null)
            {
                method = audioUtilType.GetMethod(
                    "SeekPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
                );
                if (method == null)
                {
                    Debug.LogError("Could not find method to seek in audio clip");
                    return;
                }
            }

            int samplePosition = Mathf.FloorToInt(timeInSeconds * clip.frequency) * clip.channels;
            method.Invoke(null, new object[] { clip, samplePosition });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error seeking audio clip: {e.Message}");
        }
    }

    private void StopAllClips()
    {
        try
        {
            var method = audioUtilType.GetMethod(
                "StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );

            if (method == null)
            {
                Debug.LogError("Could not find StopAllPreviewClips method");
                return;
            }

            method.Invoke(null, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error stopping audio clips: {e.Message}");
        }
    }

    private void SetLooping(AudioClip clip, bool loop)
    {
        try
        {
            var method = audioUtilType.GetMethod(
                "SetPreviewClipLoop",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );

            if (method == null)
            {
                Debug.LogError("Could not find method to set clip looping");
                return;
            }

            try
            {
                method.Invoke(null, new object[] { loop });
            }
            catch
            {
                try
                {
                    method.Invoke(null, new object[] { loop ? 1 : 0 });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error setting clip looping: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error setting clip looping: {e.Message}");
        }
    }

    private void RemoveFolder(FolderGroup folder)
    {
        bool wasPlaying = false;
        foreach (var clip in folder.clips.ToList())
        {
            if (clip.isPlaying)
            {
                wasPlaying = true;
                break;
            }
        }

        if (wasPlaying)
        {
            StopAllClips();
            currentlyPlayingClip = null;
        }

        List<ClipData> clipsToRemove = new List<ClipData>(folder.clips);
        foreach (var clip in clipsToRemove)
        {
            clipDataList.Remove(clip);
        }

        OrganizeClipsByFolder();
    }

    private Texture2D GetWaveformTexture(AudioClip clip)
    {
        if (waveformCache.TryGetValue(clip, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        Texture2D texture = GenerateWaveformTexture(clip);
        waveformCache[clip] = texture;
        return texture;
    }

    private Texture2D GenerateWaveformTexture(AudioClip clip)
    {
        int width = waveformSettings.width;
        int height = waveformSettings.height;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        }
        texture.SetPixels(clearPixels);

        float[] samples = GetAudioSamples(clip);
        if (samples == null || samples.Length == 0)
        {
            texture.Apply();
            return texture;
        }

        int channelCount = clip.channels;
        int sampleCount = samples.Length / channelCount;
        float samplesPerPixel = (float)sampleCount / width;

        for (int x = 0; x < width; x++)
        {
            float maxValue = 0f;

            int sampleOffset = (int)(x * samplesPerPixel) * channelCount;
            int samplesToCheck = Mathf.Min(
                (int)samplesPerPixel * channelCount,
                samples.Length - sampleOffset
            );

            for (int i = 0; i < samplesToCheck; i += channelCount)
            {
                float sampleValue = Mathf.Abs(samples[sampleOffset + i]);
                if (sampleValue > maxValue)
                {
                    maxValue = sampleValue;
                }
            }

            int waveformHeight = Mathf.RoundToInt(maxValue * height);

            float colorPosition = (float)x / width;
            Color waveformColor;

            if (colorPosition < 0.5f)
            {
                waveformColor = Color.Lerp(
                    waveformSettings.colorStart,
                    waveformSettings.colorMiddle,
                    colorPosition * 2f
                );
            }
            else
            {
                waveformColor = Color.Lerp(
                    waveformSettings.colorMiddle,
                    waveformSettings.colorEnd,
                    (colorPosition - 0.5f) * 2f
                );
            }

            int midPoint = height / 2;
            int minY = midPoint - waveformHeight / 2;
            int maxY = midPoint + waveformHeight / 2;

            for (int y = 0; y < height; y++)
            {
                Color pixelColor;

                if (y >= minY && y <= maxY)
                {
                    float distanceFromMiddle = Mathf.Abs(
                        (y - midPoint) / (float)(waveformHeight / 2)
                    );
                    float alpha = 1f - Mathf.Pow(distanceFromMiddle, 2);
                    pixelColor = new Color(
                        waveformColor.r,
                        waveformColor.g,
                        waveformColor.b,
                        alpha
                    );
                }
                else
                {
                    continue;
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return texture;
    }

    private float[] GetAudioSamples(AudioClip clip)
    {
        if (clipSampleCache.TryGetValue(clip, out float[] cachedSamples))
        {
            return cachedSamples;
        }

        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0))
        {
            Debug.LogWarning($"Failed to get audio data for clip: {clip.name}");
            return null;
        }

        clipSampleCache[clip] = samples;
        return samples;
    }
}
