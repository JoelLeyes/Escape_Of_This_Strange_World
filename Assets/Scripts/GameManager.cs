using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string menuSceneName = "Menú";
    [SerializeField] private string gameplaySceneName = "Level1";
    [SerializeField] private string gameOverSceneName = "PantallaNivelPerdido";

    [Header("Mensajes")]
    [SerializeField] private float checkpointMessageDuration = 2f;
    [SerializeField] private float checkpointMessageTopOffset = 24f;

    private const string CheckpointSavedMessage = "Punto salvado";

    private bool isPaused;
    private bool hasCheckpoint;
    private Vector3 checkpointPosition;
    private string checkpointSceneName;
    private readonly HashSet<string> activatedWorldObjectIds = new HashSet<string>();
    private string activeCheckpointMessage;
    private float activeCheckpointMessageEndTime;
    private GUIStyle checkpointMessageStyle;

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
        ClearWorldObjectStates();
        ClearCheckpointMessage();
        Player.ResetPersistentInstance();
        LoadScene(gameplaySceneName);
    }

    public void BackToMenu()
    {
        ClearCheckpoint();
        ClearWorldObjectStates();
        ClearCheckpointMessage();
        Player.ResetPersistentInstance();
        LoadScene(menuSceneName);
    }

    public void GameOver()
    {
        ClearCheckpointMessage();
        LoadScene(gameOverSceneName);
    }

    public void ContinueFromCheckpoint()
    {
        Debug.Log($"ContinueFromCheckpoint llamado. Has checkpoint: {hasCheckpoint}, posición: {checkpointPosition}");
        if (hasCheckpoint && !string.IsNullOrWhiteSpace(checkpointSceneName))
        {
            if (Player.Instance != null)
            {
                Player.Instance.ReviveFromCheckpoint();
            }

            LoadScene(checkpointSceneName);
            return;
        }

        Player.ResetPersistentInstance();
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

        ClearCheckpointMessage();
        ResumeGame();
        SceneManager.LoadScene(sceneName);
    }

    private void OnGUI()
    {
        if (string.IsNullOrWhiteSpace(activeCheckpointMessage) || Time.unscaledTime > activeCheckpointMessageEndTime)
        {
            return;
        }

        EnsureCheckpointMessageStyle();

        float width = Mathf.Min(Screen.width - 40f, 320f);
        float height = 36f;
        float x = (Screen.width - width) * 0.5f;
        float y = checkpointMessageTopOffset;
        Rect rect = new Rect(x, y, width, height);

        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(rect, GUIContent.none);
        GUI.backgroundColor = previousBackgroundColor;

        GUI.Label(rect, activeCheckpointMessage, checkpointMessageStyle);
    }

    private void ShowCheckpointSavedMessage()
    {
        activeCheckpointMessage = CheckpointSavedMessage;
        activeCheckpointMessageEndTime = Time.unscaledTime + checkpointMessageDuration;
    }

    private void ClearCheckpointMessage()
    {
        activeCheckpointMessage = string.Empty;
        activeCheckpointMessageEndTime = 0f;
    }

    private void EnsureCheckpointMessageStyle()
    {
        if (checkpointMessageStyle != null)
        {
            return;
        }

        checkpointMessageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            padding = new RectOffset(12, 12, 6, 6)
        };
        checkpointMessageStyle.normal.textColor = Color.white;
    }

    public void RegisterCheckpoint(Vector3 position, string sceneName)
    {
        checkpointPosition = position;
        checkpointSceneName = sceneName;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint guardado en posición {position} en escena {sceneName}");
        ShowCheckpointSavedMessage();
    }

    public bool IsWorldObjectActivated(string worldObjectId)
    {
        return !string.IsNullOrWhiteSpace(worldObjectId) && activatedWorldObjectIds.Contains(worldObjectId);
    }

    public void RegisterWorldObjectActivated(string worldObjectId)
    {
        if (string.IsNullOrWhiteSpace(worldObjectId))
        {
            return;
        }

        activatedWorldObjectIds.Add(worldObjectId);
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

        if (rigidbody2D != null)
        {
            rigidbody2D.position = positionWithZero;
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }

        target.position = positionWithZero;
        Physics2D.SyncTransforms();

        if (rigidbody2D != null)
        {
            rigidbody2D.WakeUp();
        }
    }

    private void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointPosition = default;
        checkpointSceneName = string.Empty;
    }

    private void ClearWorldObjectStates()
    {
        activatedWorldObjectIds.Clear();
    }
}
