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
    private bool hasCheckpoint;
    private Vector3 checkpointPosition;
    private string checkpointSceneName;

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
        ClearCheckpoint();
        LoadScene(gameplaySceneName);
    }

    public void BackToMenu()
    {
        ClearCheckpoint();
        LoadScene(menuSceneName);
    }

    public void GameOver()
    {
        LoadScene(gameOverSceneName);
    }

    public void ContinueFromCheckpoint()
    {
        Debug.Log($"ContinueFromCheckpoint llamado. Has checkpoint: {hasCheckpoint}, posición: {checkpointPosition}");
        if (hasCheckpoint && !string.IsNullOrWhiteSpace(checkpointSceneName))
        {
            LoadScene(checkpointSceneName);
            return;
        }

        LoadScene(gameplaySceneName);
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

    public bool IsGamePaused()
    {
        return isPaused || Time.timeScale == 0f;
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

    public void RegisterCheckpoint(Vector3 position, string sceneName)
    {
        checkpointPosition = position;
        checkpointSceneName = sceneName;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint guardado en posición {position} en escena {sceneName}");
    }

    public void ApplyCheckpoint(Transform target, Rigidbody2D rigidbody2D)
    {
        if (!hasCheckpoint || target == null)
        {
            Debug.Log($"ApplyCheckpoint: No se aplicó. HasCheckpoint: {hasCheckpoint}, Target null: {target == null}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(checkpointSceneName) && SceneManager.GetActiveScene().name != checkpointSceneName)
        {
            Debug.Log($"ApplyCheckpoint: Escenas no coinciden. Guardada: {checkpointSceneName}, Actual: {SceneManager.GetActiveScene().name}");
            return;
        }

        Debug.Log($"Aplicando checkpoint en posición {checkpointPosition}");
        Vector3 positionWithZero = new Vector3(checkpointPosition.x, checkpointPosition.y, 0f);
        target.position = positionWithZero;

        if (rigidbody2D != null)
        {
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }
    }

    private void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointPosition = default;
        checkpointSceneName = string.Empty;
    }
}
