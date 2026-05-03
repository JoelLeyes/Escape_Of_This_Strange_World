using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string menuSceneName = "Menú";
    [SerializeField] private string gameplaySceneName = "Level1";
    [SerializeField] private string gameOverSceneName = "PantallaNivelPerdido";

    private bool isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject gameManagerObject = new GameObject(nameof(GameManager));
        gameManagerObject.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResumeGame();
    }

    public void StartGame()
    {
        LoadScene(gameplaySceneName);
    }

    public void BackToMenu()
    {
        LoadScene(menuSceneName);
    }

    public void GameOver()
    {
        LoadScene(gameOverSceneName);
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        if (isPaused || Time.timeScale == 0f)
        {
            ResumeGame();
            return;
        }

        PauseGame();
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("GameManager: scene name is empty.", this);
            return;
        }

        ResumeGame();
        SceneManager.LoadScene(sceneName);
    }
}
