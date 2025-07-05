using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauzeScreen : MonoBehaviour
{
    [Header("🎮 PAUSE SCREEN UI")]
    [SerializeField]
    private GameObject _pausePanel;

    [SerializeField]
    private Button _resumeButton;

    [SerializeField]
    private Button _mainMenuButton;

    [SerializeField]
    private Button _quitButton;

    [Header("🔘 PAUSE BUTTON")]
    [SerializeField]
    private Button _pauseButton;

    private bool _isPaused = false;
    private DialogueDisplay _dialogueDisplay;

    void Start()
    {
        // Get reference to DialogueDisplay component
        _dialogueDisplay = FindFirstObjectByType<DialogueDisplay>();

        // Initialize pause screen as hidden
        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        // Set initial pause button visibility
        UpdatePauseButtonVisibility();

        // Setup button listeners
        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(ResumeGame);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(QuitGame);

        if (_pauseButton != null)
            _pauseButton.onClick.AddListener(PauseGame);
    }

    void Update()
    {
        // Toggle pause with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // Update pause button visibility based on dialogue state
        UpdatePauseButtonVisibility();
    }

    /// <summary>
    /// Pauses the game and shows the pause menu
    /// </summary>
    public void PauseGame()
    {
        if (_isPaused)
            return;

        _isPaused = true;
        Time.timeScale = 0f;

        if (_pausePanel != null)
            _pausePanel.SetActive(true);

        if (_pauseButton != null)
            _pauseButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Resumes the game and hides the pause menu
    /// </summary>
    public void ResumeGame()
    {
        if (!_isPaused)
            return;

        _isPaused = false;
        Time.timeScale = 1f;

        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        UpdatePauseButtonVisibility();
    }

    /// <summary>
    /// Returns to the main menu scene
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu"); // Adjust scene name as needed
    }

    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Updates the visibility of the pause button based on dialogue state
    /// </summary>
    private void UpdatePauseButtonVisibility()
    {
        if (_pauseButton == null || _isPaused)
            return;

        bool shouldShowPauseButton = true;

        // Hide pause button during dialogue
        if (_dialogueDisplay != null && _dialogueDisplay.IsInDialogue())
        {
            shouldShowPauseButton = false;
        }

        _pauseButton.gameObject.SetActive(shouldShowPauseButton);
    }

    /// <summary>
    /// Public property to check if game is currently paused
    /// </summary>
    public bool IsPaused => _isPaused;
}
